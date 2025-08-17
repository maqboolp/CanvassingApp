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
using Twilio.Jwt.AccessToken;

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

                // Get Twilio configuration from database or environment
                var twilioConfig = await _context.TwilioConfigurations
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                string accountSid = null;
                string authToken = null;
                string fromPhone = null;
                string appSid = null;
                string apiKeySid = null;
                string apiKeySecret = null;

                if (twilioConfig != null)
                {
                    accountSid = twilioConfig.AccountSid;
                    authToken = twilioConfig.AuthToken;
                    fromPhone = twilioConfig.FromPhoneNumber;
                    appSid = twilioConfig.AppSid;
                }

                // Fall back to environment variables if not in database
                if (string.IsNullOrEmpty(accountSid))
                {
                    accountSid = _configuration["Twilio:AccountSid"];
                    authToken = _configuration["Twilio:AuthToken"];
                    fromPhone = _configuration["Twilio:FromPhoneNumber"];
                }
                
                // Check for API Key configuration (for JWT signing)
                apiKeySid = _configuration["Twilio:ApiKeySid"];
                apiKeySecret = _configuration["Twilio:ApiKeySecret"];

                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
                {
                    return BadRequest("Twilio is not configured. Please contact an administrator.");
                }

                // Initialize Twilio
                TwilioClient.Init(accountSid, authToken);

                // Create or get TwiML App if needed
                if (string.IsNullOrEmpty(appSid))
                {
                    // Get the base URL, ensuring it's absolute
                    var baseUrl = _configuration["AppSettings:BaseUrl"];
                    if (string.IsNullOrEmpty(baseUrl))
                    {
                        // Fallback to the request URL if BaseUrl is not configured
                        baseUrl = $"{Request.Scheme}://{Request.Host}";
                    }
                    
                    var app = await ApplicationResource.CreateAsync(
                        voiceUrl: new Uri($"{baseUrl}/api/browser-call/voice"),
                        voiceMethod: Twilio.Http.HttpMethod.Post,
                        friendlyName: "Phone Banking Browser App"
                    );
                    
                    appSid = app.Sid;
                    
                    // Save to database if we have a config record
                    if (twilioConfig != null)
                    {
                        twilioConfig.AppSid = app.Sid;
                        await _context.SaveChangesAsync();
                    }
                    
                    _logger.LogInformation($"Created TwiML App: {app.Sid}");
                }

                // Generate a proper Twilio Access Token using the SDK
                var identity = $"volunteer_{userId}";
                
                // Create Voice grant with outgoing application
                var voiceGrant = new VoiceGrant
                {
                    OutgoingApplicationSid = appSid,
                    IncomingAllow = true
                };

                var grants = new HashSet<IGrant> { voiceGrant };
                
                // Create access token with Twilio's JWT library
                // Use API Keys if available, otherwise fall back to Account SID/Auth Token
                string signingKeySid;
                string signingKeySecret;
                
                if (!string.IsNullOrEmpty(apiKeySid) && !string.IsNullOrEmpty(apiKeySecret))
                {
                    // Use API Keys for signing (recommended)
                    signingKeySid = apiKeySid;
                    signingKeySecret = apiKeySecret;
                    _logger.LogInformation("Using API Keys for JWT token generation");
                }
                else
                {
                    // Fall back to using Account SID (less secure, may not work)
                    signingKeySid = accountSid;
                    signingKeySecret = authToken;
                    _logger.LogWarning("API Keys not configured. Using Account SID for JWT signing (this may not work). Please configure Twilio API Keys.");
                }
                
                var token = new Token(
                    accountSid,
                    signingKeySid,
                    signingKeySecret,
                    identity,
                    expiration: DateTime.UtcNow.AddHours(4),
                    nbf: DateTime.UtcNow,
                    grants: grants,
                    region: null
                );

                return Ok(new
                {
                    token = token.ToJwt(),
                    identity = identity
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

                string fromPhoneNumber = twilioConfig?.FromPhoneNumber;
                
                // Fall back to environment variables if not in database
                if (string.IsNullOrEmpty(fromPhoneNumber))
                {
                    fromPhoneNumber = _configuration["Twilio:FromPhoneNumber"];
                }

                if (string.IsNullOrEmpty(fromPhoneNumber))
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
                    
                    _logger.LogInformation($"Calling voter {voterId}: {voterName}");
                    _logger.LogInformation($"Raw phone from DB: {voter.CellPhone}");
                    _logger.LogInformation($"Formatted phone: {phoneNumber}");
                    
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
                
                // Use TwilioService to format the phone number properly
                var formattedNumber = _twilioService.FormatPhoneNumber(phoneNumber);
                _logger.LogInformation($"Original number: {phoneNumber}, Formatted number: {formattedNumber}");
                
                // Validate the formatted number
                if (!await _twilioService.ValidatePhoneNumberAsync(formattedNumber))
                {
                    _logger.LogWarning($"Phone number validation failed for: {formattedNumber}");
                    response.Say("Invalid phone number. Please verify the number and try again.");
                    response.Hangup();
                    return Content(response.ToString(), "application/xml");
                }
                
                var dial = new Dial(
                    callerId: fromPhoneNumber,
                    record: Dial.RecordEnum.RecordFromAnswerDual,
                    recordingStatusCallback: new Uri($"{_configuration["AppSettings:BaseUrl"]}/api/browser-call/recording-callback"),
                    timeout: 30,  // Wait 30 seconds for answer
                    action: new Uri($"{_configuration["AppSettings:BaseUrl"]}/api/browser-call/dial-callback"),  // Callback after dial completes
                    method: Twilio.Http.HttpMethod.Post
                );
                
                _logger.LogInformation($"Dialing formatted number: {formattedNumber} with caller ID: {fromPhoneNumber}");
                dial.Number(formattedNumber);
                response.Append(dial);

                response.Say("The call has ended. Thank you.");

                // Log the TwiML response for debugging
                var twimlResponse = response.ToString();
                _logger.LogInformation($"TwiML Response: {twimlResponse}");

                return Content(twimlResponse, "application/xml");
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
        /// Callback after dial completes to handle the result
        /// </summary>
        [HttpPost("dial-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> DialCallback()
        {
            try
            {
                var dialCallStatus = Request.Form["DialCallStatus"].ToString();
                var dialCallDuration = Request.Form["DialCallDuration"].ToString();
                var callSid = Request.Form["CallSid"].ToString();
                var dialCallSid = Request.Form["DialCallSid"].ToString();
                var recordingUrl = Request.Form["RecordingUrl"].ToString();
                
                // Log all form data for debugging
                _logger.LogInformation($"Dial callback - CallSid: {callSid}, DialCallSid: {dialCallSid}, Status: {dialCallStatus}, Duration: {dialCallDuration}");
                foreach (var key in Request.Form.Keys)
                {
                    _logger.LogDebug($"Form[{key}] = {Request.Form[key]}");
                }
                
                // Update phone banking call record
                if (!string.IsNullOrEmpty(callSid))
                {
                    var callRecord = await _context.PhoneBankingCalls
                        .FirstOrDefaultAsync(c => c.TwilioCallSid == callSid);
                    
                    if (callRecord != null)
                    {
                        callRecord.Status = dialCallStatus?.ToLower() ?? "unknown";
                        // Store duration in TwimlContent temporarily as we don't have a DurationSeconds field
                        if (!string.IsNullOrEmpty(dialCallDuration))
                        {
                            callRecord.TwimlContent = $"Duration: {dialCallDuration} seconds";
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                
                var response = new VoiceResponse();
                
                // Provide feedback based on call result
                switch (dialCallStatus?.ToLower())
                {
                    case "completed":
                        response.Say("Call completed successfully.");
                        break;
                    case "busy":
                        response.Say("The line is busy. Please try again later.");
                        break;
                    case "no-answer":
                        response.Say("No answer. Please try again later.");
                        break;
                    case "failed":
                        response.Say("The call could not be connected. Please check the number and try again.");
                        _logger.LogWarning($"Call failed - CallSid: {callSid}, DialCallSid: {dialCallSid}");
                        break;
                    case "canceled":
                        response.Say("The call was canceled.");
                        break;
                    default:
                        response.Say("The call ended.");
                        _logger.LogDebug($"Call ended with status: {dialCallStatus}");
                        break;
                }
                
                response.Hangup();
                return Content(response.ToString(), "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in dial callback");
                var errorResponse = new VoiceResponse();
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
                // First check database configuration
                var twilioConfig = await _context.TwilioConfigurations
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                bool isConfigured = false;
                bool hasAppSid = false;

                if (twilioConfig != null)
                {
                    // Check if properly configured in database
                    isConfigured = !string.IsNullOrEmpty(twilioConfig.AccountSid) 
                        && !string.IsNullOrEmpty(twilioConfig.AuthToken)
                        && !string.IsNullOrEmpty(twilioConfig.FromPhoneNumber);
                    hasAppSid = !string.IsNullOrEmpty(twilioConfig.AppSid);
                }

                // If not configured in database, check environment variables
                if (!isConfigured)
                {
                    var envAccountSid = _configuration["Twilio:AccountSid"];
                    var envAuthToken = _configuration["Twilio:AuthToken"];
                    var envFromPhone = _configuration["Twilio:FromPhoneNumber"];

                    isConfigured = !string.IsNullOrEmpty(envAccountSid) 
                        && !string.IsNullOrEmpty(envAuthToken)
                        && !string.IsNullOrEmpty(envFromPhone);
                }

                return Ok(new
                {
                    isConfigured = isConfigured,
                    hasAppSid = hasAppSid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting config");
                // Return false configuration instead of error
                return Ok(new
                {
                    isConfigured = false,
                    hasAppSid = false
                });
            }
        }
    }
}