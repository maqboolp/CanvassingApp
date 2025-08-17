using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/phonebanking")]
    [Authorize]
    public class SimplifiedPhoneBankingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITwilioService _twilioService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SimplifiedPhoneBankingController> _logger;

        public SimplifiedPhoneBankingController(
            ApplicationDbContext context,
            ITwilioService twilioService,
            IConfiguration configuration,
            ILogger<SimplifiedPhoneBankingController> logger)
        {
            _context = context;
            _twilioService = twilioService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generate a simple token for authentication
        /// For production, use proper Twilio Access Tokens with SDK
        /// </summary>
        [HttpGet("token")]
        public async Task<IActionResult> GetSimpleToken()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
                var user = await _context.Volunteers.FindAsync(userId);
                
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Get Twilio configuration from database or environment
                var twilioConfig = await _context.TwilioConfigurations
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                string accountSid = twilioConfig?.AccountSid;
                string authToken = twilioConfig?.AuthToken;
                string fromPhoneNumber = twilioConfig?.FromPhoneNumber;

                // Fall back to environment variables if not in database
                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
                {
                    accountSid = _configuration["Twilio:AccountSid"];
                    authToken = _configuration["Twilio:AuthToken"];
                    fromPhoneNumber = _configuration["Twilio:FromPhoneNumber"];
                }

                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
                {
                    return BadRequest(new { 
                        error = "Twilio is not configured",
                        message = "The phone system is not set up yet. Please contact an administrator to configure Twilio settings.",
                        isConfigured = false
                    });
                }

                // For browser-based calling, we'll return configuration
                // The actual implementation would use Twilio's Voice SDK
                return Ok(new
                {
                    twilioAccountSid = accountSid,
                    twilioNumber = fromPhoneNumber,
                    userId = userId,
                    userName = $"{user.FirstName} {user.LastName}",
                    // In production, generate a proper JWT token here
                    token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userId}:{DateTime.UtcNow.Ticks}"))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token");
                return StatusCode(500, new { error = "Failed to generate token" });
            }
        }

        /// <summary>
        /// Initiate a call using click-to-call pattern
        /// This calls the volunteer first, then connects to voter
        /// </summary>
        [HttpPost("call")]
        public async Task<IActionResult> InitiateCall([FromBody] CallRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
                var user = await _context.Volunteers.FindAsync(userId);
                
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Get Twilio configuration
                var twilioConfig = await _context.TwilioConfigurations
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                if (twilioConfig == null)
                {
                    return BadRequest("Twilio is not configured");
                }

                // Get voter
                var voter = await _context.Voters
                    .FirstOrDefaultAsync(v => v.LalVoterId == request.VoterId);

                if (voter == null || string.IsNullOrEmpty(voter.CellPhone))
                {
                    return BadRequest("Voter not found or has no phone number");
                }

                // Initialize Twilio
                TwilioClient.Init(twilioConfig.AccountSid, twilioConfig.AuthToken);

                // Format phone numbers
                var voterPhone = _twilioService.FormatPhoneNumber(voter.CellPhone);
                
                // Use volunteer's phone if provided, otherwise expect browser-based call
                string volunteerPhone = null;
                if (!string.IsNullOrEmpty(request.VolunteerPhone))
                {
                    volunteerPhone = _twilioService.FormatPhoneNumber(request.VolunteerPhone);
                }
                else if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    volunteerPhone = _twilioService.FormatPhoneNumber(user.PhoneNumber);
                }

                if (string.IsNullOrEmpty(volunteerPhone))
                {
                    return BadRequest("Volunteer phone number is required. Please update your profile.");
                }

                // Create TwiML for connecting the calls
                var twimlUrl = $"{_configuration["AppSettings:BaseUrl"]}/api/phonebanking/connect-twiml?voterPhone={Uri.EscapeDataString(voterPhone)}&voterName={Uri.EscapeDataString($"{voter.FirstName} {voter.LastName}")}";

                // Initiate call to volunteer first
                var call = await CallResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber(volunteerPhone),
                    from: new Twilio.Types.PhoneNumber(twilioConfig.FromPhoneNumber),
                    url: new Uri(twimlUrl),
                    statusCallback: new Uri($"{_configuration["AppSettings:BaseUrl"]}/api/phonebanking/status-callback"),
                    method: Twilio.Http.HttpMethod.Post
                );

                // Save call record
                var callRecord = new PhoneBankingCall
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    VoterId = request.VoterId,
                    VolunteerPhone = volunteerPhone,
                    VoterPhone = voterPhone,
                    TwilioCallSid = call.Sid,
                    Status = "initiated",
                    CreatedAt = DateTime.UtcNow,
                    TwimlContent = ""
                };

                _context.PhoneBankingCalls.Add(callRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Initiated call {call.Sid} from volunteer {userId} to voter {request.VoterId}");

                return Ok(new
                {
                    success = true,
                    callSid = call.Sid,
                    message = "Your phone will ring shortly. Once you answer, we'll connect you to the voter."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
                return StatusCode(500, new { error = "Failed to initiate call", details = ex.Message });
            }
        }

        /// <summary>
        /// TwiML endpoint for connecting calls
        /// </summary>
        [HttpPost("connect-twiml")]
        [AllowAnonymous]
        public IActionResult ConnectTwiml([FromQuery] string voterPhone, [FromQuery] string voterName)
        {
            var response = new VoiceResponse();
            
            if (string.IsNullOrEmpty(voterPhone))
            {
                response.Say("Error: No phone number provided");
                response.Hangup();
            }
            else
            {
                response.Say($"Connecting you to {voterName ?? "the voter"}. Please wait...");
                
                var dial = new Dial(
                    record: Dial.RecordEnum.RecordFromAnswerDual,
                    recordingStatusCallback: new Uri($"{_configuration["AppSettings:BaseUrl"]}/api/phonebanking/recording-callback")
                );
                dial.Number(voterPhone);
                response.Append(dial);
                
                response.Say("The call has ended. Thank you.");
            }

            return Content(response.ToString(), "application/xml");
        }

        /// <summary>
        /// Status callback for call events
        /// </summary>
        [HttpPost("status-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> StatusCallback([FromForm] TwilioStatusCallback callback)
        {
            try
            {
                var callRecord = await _context.PhoneBankingCalls
                    .FirstOrDefaultAsync(c => c.TwilioCallSid == callback.CallSid);

                if (callRecord != null)
                {
                    callRecord.Status = callback.CallStatus;
                    if (callback.CallStatus == "completed")
                    {
                        callRecord.EndedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing status callback for call {callback.CallSid}");
                return Ok(); // Return OK to prevent Twilio retries
            }
        }

        /// <summary>
        /// Recording callback
        /// </summary>
        [HttpPost("recording-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> RecordingCallback([FromForm] TwilioRecordingCallback callback)
        {
            try
            {
                _logger.LogInformation($"Recording completed: {callback.RecordingSid} for call {callback.CallSid}");
                // Here you could save the recording URL to the database
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing recording callback");
                return Ok();
            }
        }
    }

    public class CallRequest
    {
        public string VoterId { get; set; } = "";
        public string? VolunteerPhone { get; set; }
        public bool RecordCall { get; set; } = true;
    }

    public class TwilioStatusCallback
    {
        public string CallSid { get; set; } = "";
        public string CallStatus { get; set; } = "";
        public string? CallDuration { get; set; }
    }

    public class TwilioRecordingCallback
    {
        public string RecordingSid { get; set; } = "";
        public string CallSid { get; set; } = "";
        public string RecordingUrl { get; set; } = "";
        public string RecordingDuration { get; set; } = "";
    }
}