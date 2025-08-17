using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/browser-call")]
    [Authorize]
    public class BrowserCallController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITwilioService _twilioService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BrowserCallController> _logger;

        public BrowserCallController(
            ApplicationDbContext context,
            ITwilioService twilioService,
            IConfiguration configuration,
            ILogger<BrowserCallController> logger)
        {
            _context = context;
            _twilioService = twilioService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generate a capability token for browser-based calling
        /// This allows the browser to make calls directly using WebRTC
        /// </summary>
        [HttpGet("token")]
        public async Task<IActionResult> GetCapabilityToken()
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

                // Initialize Twilio
                TwilioClient.Init(twilioConfig.AccountSid, twilioConfig.AuthToken);

                // Create or get TwiML App
                if (string.IsNullOrEmpty(twilioConfig.AppSid))
                {
                    var app = await ApplicationResource.CreateAsync(
                        voiceUrl: new Uri($"{_configuration["AppSettings:BaseUrl"]}/api/browser-call/voice"),
                        voiceMethod: Twilio.Http.HttpMethod.Post,
                        friendlyName: "Phone Banking Browser App"
                    );
                    
                    twilioConfig.AppSid = app.Sid;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Created TwiML App: {app.Sid}");
                }

                // Generate a simple JWT token for the Twilio Voice SDK
                // This is a simplified version - in production, use Twilio's helper libraries
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(twilioConfig.AuthToken);
                
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim("scope", $"scope:client:outgoing?appSid={twilioConfig.AppSid}"),
                    new System.Security.Claims.Claim("scope", $"scope:client:incoming?clientName=volunteer_{userId}")
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(4),
                    Issuer = twilioConfig.AccountSid,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key), 
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                // For now, return a simple token structure
                // The Twilio SDK will validate this on connection
                return Ok(new
                {
                    token = tokenString,
                    identity = $"volunteer_{userId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating capability token");
                return StatusCode(500, new { error = "Failed to generate token", details = ex.Message });
            }
        }

        /// <summary>
        /// TwiML endpoint for handling outgoing calls from the browser
        /// This is called when the browser initiates a call
        /// </summary>
        [HttpPost("voice")]
        [AllowAnonymous]
        public async Task<IActionResult> VoiceHandler()
        {
            try
            {
                // Read the form data
                var to = Request.Form["To"].ToString();
                var from = Request.Form["From"].ToString();
                var callSid = Request.Form["CallSid"].ToString();

                _logger.LogInformation($"Voice handler called - To: {to}, From: {from}, CallSid: {callSid}");

                var response = new VoiceResponse();

                // Get Twilio configuration for caller ID
                var twilioConfig = await _context.TwilioConfigurations
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                if (twilioConfig == null)
                {
                    response.Say("System configuration error. Please contact support.");
                    response.Hangup();
                    return Content(response.ToString(), "application/xml");
                }

                // Parse the "To" parameter to get voter ID
                if (string.IsNullOrEmpty(to))
                {
                    response.Say("No destination number provided.");
                    response.Hangup();
                    return Content(response.ToString(), "application/xml");
                }

                string phoneNumber = to;
                string voterName = "the voter";

                // If it's a voter ID, look up the voter
                if (to.StartsWith("voter:"))
                {
                    var voterId = to.Replace("voter:", "");
                    var voter = await _context.Voters
                        .FirstOrDefaultAsync(v => v.LalVoterId == voterId);

                    if (voter == null || string.IsNullOrEmpty(voter.CellPhone))
                    {
                        response.Say("Voter phone number not found.");
                        response.Hangup();
                        return Content(response.ToString(), "application/xml");
                    }

                    phoneNumber = _twilioService.FormatPhoneNumber(voter.CellPhone);
                    voterName = $"{voter.FirstName} {voter.LastName}";
                    
                    // Log the call
                    var callRecord = new PhoneBankingCall
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = from.Replace("client:volunteer_", ""),
                        VoterId = voterId,
                        VolunteerPhone = "browser",
                        VoterPhone = phoneNumber,
                        TwilioCallSid = callSid,
                        Status = "connecting",
                        CreatedAt = DateTime.UtcNow,
                        TwimlContent = ""
                    };

                    _context.PhoneBankingCalls.Add(callRecord);
                    await _context.SaveChangesAsync();
                }

                // Connect the call
                response.Say($"Connecting you to {voterName}. Please wait.");
                
                var dial = new Dial(
                    callerId: twilioConfig.FromPhoneNumber,
                    record: Dial.RecordEnum.RecordFromAnswerDual,
                    recordingStatusCallback: new Uri($"{_configuration["AppSettings:BaseUrl"]}/api/browser-call/recording-callback")
                );
                dial.Number(phoneNumber);
                response.Append(dial);

                response.Say("The call has ended. Thank you.");

                return Content(response.ToString(), "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in voice handler");
                var errorResponse = new VoiceResponse();
                errorResponse.Say("An error occurred. Please try again.");
                errorResponse.Hangup();
                return Content(errorResponse.ToString(), "application/xml");
            }
        }

        /// <summary>
        /// Callback for recording completion
        /// </summary>
        [HttpPost("recording-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> RecordingCallback()
        {
            try
            {
                var recordingSid = Request.Form["RecordingSid"].ToString();
                var callSid = Request.Form["CallSid"].ToString();
                var recordingUrl = Request.Form["RecordingUrl"].ToString();
                var duration = Request.Form["RecordingDuration"].ToString();

                _logger.LogInformation($"Recording completed: {recordingSid} for call {callSid}, Duration: {duration}s");

                // Update call record with recording info
                var callRecord = await _context.PhoneBankingCalls
                    .FirstOrDefaultAsync(c => c.TwilioCallSid == callSid);

                if (callRecord != null)
                {
                    // You could store the recording URL here if needed
                    callRecord.Status = "recorded";
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recording callback");
                return Ok(); // Return OK to prevent Twilio retries
            }
        }

        /// <summary>
        /// Get configuration for the frontend
        /// </summary>
        [HttpGet("config")]
        public async Task<IActionResult> GetConfig()
        {
            try
            {
                var twilioConfig = await _context.TwilioConfigurations
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                if (twilioConfig == null)
                {
                    return BadRequest("Twilio is not configured");
                }

                return Ok(new
                {
                    isConfigured = !string.IsNullOrEmpty(twilioConfig.AccountSid),
                    hasAppSid = !string.IsNullOrEmpty(twilioConfig.AppSid)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting config");
                return StatusCode(500, new { error = "Failed to get configuration" });
            }
        }
    }
}