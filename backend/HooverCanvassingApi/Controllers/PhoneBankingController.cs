using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PhoneBankingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITwilioService _twilioService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PhoneBankingController> _logger;

        public PhoneBankingController(
            ApplicationDbContext context,
            ITwilioService twilioService,
            IConfiguration configuration,
            ILogger<PhoneBankingController> logger)
        {
            _context = context;
            _twilioService = twilioService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("initiate-call")]
        public async Task<IActionResult> InitiateCall([FromBody] InitiateCallRequest request)
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
                    return BadRequest("Twilio is not configured. Please contact an administrator.");
                }

                // Validate voter exists
                var voter = await _context.Voters
                    .FirstOrDefaultAsync(v => v.LalVoterId == request.VoterId);

                if (voter == null)
                {
                    return NotFound("Voter not found");
                }

                if (string.IsNullOrEmpty(voter.CellPhone))
                {
                    return BadRequest("Voter does not have a phone number");
                }

                // Format phone numbers
                var volunteerPhone = _twilioService.FormatPhoneNumber(request.VolunteerPhone);
                var voterPhone = _twilioService.FormatPhoneNumber(voter.CellPhone);

                // Initialize Twilio client
                TwilioClient.Init(twilioConfig.AccountSid, twilioConfig.AuthToken);

                // Create TwiML to connect the volunteer to the voter
                var response = new VoiceResponse();
                response.Say($"Connecting you to {voter.FirstName} {voter.LastName}. Please wait...");
                
                var dial = new Twilio.TwiML.Voice.Dial(
                    callerId: twilioConfig.FromPhoneNumber,
                    record: request.RecordCall ? Twilio.TwiML.Voice.Dial.RecordEnum.RecordFromAnswerDual : (Twilio.TwiML.Voice.Dial.RecordEnum?)null,
                    recordingStatusCallback: request.RecordCall ? 
                        new Uri($"{_configuration["AppSettings:BaseUrl"]}/api/twilio/recording-callback") : null);
                dial.Number(voterPhone);
                response.Append(dial);

                // Generate TwiML URL
                var twimlUrl = $"{_configuration["AppSettings:BaseUrl"]}/api/phonebanking/twiml/{request.CallId}";
                
                // Store TwiML in cache or database for retrieval
                var callData = new PhoneBankingCall
                {
                    Id = request.CallId,
                    UserId = userId,
                    VoterId = request.VoterId,
                    VolunteerPhone = volunteerPhone,
                    VoterPhone = voterPhone,
                    TwimlContent = response.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Status = "initiating"
                };

                _context.PhoneBankingCalls.Add(callData);
                await _context.SaveChangesAsync();

                // Initiate the call to the volunteer first
                var call = await CallResource.CreateAsync(
                    to: new PhoneNumber(volunteerPhone),
                    from: new PhoneNumber(twilioConfig.FromPhoneNumber),
                    url: new Uri(twimlUrl),
                    statusCallback: new Uri($"{_configuration["AppSettings:BaseUrl"]}/api/twilio/status-callback"),
                    statusCallbackEvent: new List<string> { "initiated", "ringing", "answered", "completed" },
                    statusCallbackMethod: Twilio.Http.HttpMethod.Post
                );

                // Update call with Twilio SID
                callData.TwilioCallSid = call.Sid;
                callData.Status = "initiated";
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Initiated phone banking call {call.Sid} from volunteer {userId} to voter {request.VoterId}");

                return Ok(new
                {
                    success = true,
                    callSid = call.Sid,
                    message = "Call initiated successfully. Your phone will ring shortly."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating phone banking call");
                return StatusCode(500, new { error = "Failed to initiate call", details = ex.Message });
            }
        }

        [HttpGet("twiml/{callId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTwiml(string callId)
        {
            try
            {
                var callData = await _context.PhoneBankingCalls
                    .FirstOrDefaultAsync(c => c.Id == callId);

                if (callData == null)
                {
                    var errorResponse = new VoiceResponse();
                    errorResponse.Say("Call configuration not found. Please try again.");
                    errorResponse.Hangup();
                    return Content(errorResponse.ToString(), "application/xml");
                }

                return Content(callData.TwimlContent, "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving TwiML for call {callId}");
                var errorResponse = new VoiceResponse();
                errorResponse.Say("An error occurred. Please try again.");
                errorResponse.Hangup();
                return Content(errorResponse.ToString(), "application/xml");
            }
        }

        [HttpPost("end-call")]
        public async Task<IActionResult> EndCall([FromBody] EndCallRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
                
                var callData = await _context.PhoneBankingCalls
                    .FirstOrDefaultAsync(c => c.TwilioCallSid == request.CallSid && c.UserId == userId);

                if (callData == null)
                {
                    return NotFound("Call not found");
                }

                // Get Twilio configuration
                var twilioConfig = await _context.TwilioConfigurations
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                if (twilioConfig == null)
                {
                    return BadRequest("Twilio is not configured");
                }

                // Initialize Twilio client
                TwilioClient.Init(twilioConfig.AccountSid, twilioConfig.AuthToken);

                // End the call
                var call = await CallResource.UpdateAsync(
                    pathSid: request.CallSid,
                    status: CallResource.UpdateStatusEnum.Completed
                );

                callData.Status = "completed";
                callData.EndedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Call ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call");
                return StatusCode(500, new { error = "Failed to end call", details = ex.Message });
            }
        }

        [HttpGet("call-status/{callSid}")]
        public async Task<IActionResult> GetCallStatus(string callSid)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
                
                var callData = await _context.PhoneBankingCalls
                    .FirstOrDefaultAsync(c => c.TwilioCallSid == callSid && c.UserId == userId);

                if (callData == null)
                {
                    return NotFound("Call not found");
                }

                // Get Twilio configuration
                var twilioConfig = await _context.TwilioConfigurations
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                if (twilioConfig == null)
                {
                    return BadRequest("Twilio is not configured");
                }

                // Initialize Twilio client
                TwilioClient.Init(twilioConfig.AccountSid, twilioConfig.AuthToken);

                // Get call details from Twilio
                var call = await CallResource.FetchAsync(pathSid: callSid);

                return Ok(new
                {
                    status = call.Status.ToString(),
                    duration = call.Duration,
                    startTime = call.StartTime,
                    endTime = call.EndTime,
                    direction = call.Direction,
                    answeredBy = call.AnsweredBy
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting call status for {callSid}");
                return StatusCode(500, new { error = "Failed to get call status" });
            }
        }
    }

    public class InitiateCallRequest
    {
        public string VoterId { get; set; } = "";
        public string VolunteerPhone { get; set; } = "";
        public string CallId { get; set; } = Guid.NewGuid().ToString();
        public bool RecordCall { get; set; } = false;
    }

    public class EndCallRequest
    {
        public string CallSid { get; set; } = "";
    }
}