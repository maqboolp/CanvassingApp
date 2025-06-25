using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.DTOs;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using HooverCanvassingApi.Configuration;
using System.Text.RegularExpressions;
using Twilio.TwiML;
using Twilio.TwiML.Messaging;
using Microsoft.Extensions.Options;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OptInController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITwilioService _twilioService;
        private readonly ILogger<OptInController> _logger;
        private readonly OptInSettings _optInSettings;
        
        private const string OPT_IN_KEYWORDS = "^(JOIN|START|YES|SUBSCRIBE|SIGNUP)$";
        private const string OPT_OUT_KEYWORDS = "^(STOP|UNSUBSCRIBE|CANCEL|END|QUIT|STOPALL|STOP ALL)$";
        
        public OptInController(
            ApplicationDbContext context,
            ITwilioService twilioService,
            ILogger<OptInController> logger,
            IOptions<OptInSettings> optInSettings)
        {
            _context = context;
            _twilioService = twilioService;
            _logger = logger;
            _optInSettings = optInSettings.Value;
        }
        
        [HttpPost("web-form")]
        public async Task<ActionResult<OptInResponse>> OptInViaWebForm([FromBody] OptInRequest request)
        {
            try
            {
                if (!request.ConsentGiven)
                {
                    return BadRequest(new OptInResponse
                    {
                        Success = false,
                        Message = "Consent must be given to opt-in for SMS communications."
                    });
                }
                
                // Format phone number
                var formattedPhone = _twilioService.FormatPhoneNumber(request.PhoneNumber);
                if (string.IsNullOrEmpty(formattedPhone))
                {
                    return BadRequest(new OptInResponse
                    {
                        Success = false,
                        Message = "Invalid phone number format."
                    });
                }
                
                // Find or create voter
                var voter = await _context.Voters
                    .FirstOrDefaultAsync(v => v.CellPhone == formattedPhone);
                
                if (voter == null && !string.IsNullOrEmpty(request.ZipCode))
                {
                    // Try to find by name and zip if provided
                    voter = await _context.Voters
                        .FirstOrDefaultAsync(v => 
                            v.FirstName.ToLower() == (request.FirstName ?? "").ToLower() &&
                            v.LastName.ToLower() == (request.LastName ?? "").ToLower() &&
                            v.Zip == request.ZipCode);
                    
                    if (voter != null && string.IsNullOrEmpty(voter.CellPhone))
                    {
                        voter.CellPhone = formattedPhone;
                    }
                }
                
                // Create new voter if not found
                if (voter == null)
                {
                    voter = new Voter
                    {
                        LalVoterId = $"WEB_{Guid.NewGuid():N}",
                        FirstName = request.FirstName ?? "Unknown",
                        LastName = request.LastName ?? "Unknown",
                        CellPhone = formattedPhone,
                        Email = request.Email,
                        AddressLine = "Unknown",
                        City = "Hoover",
                        State = "AL",
                        Zip = request.ZipCode ?? "00000",
                        Gender = "Unknown",
                        VoteFrequency = VoteFrequency.NonVoter,
                        Age = 0
                    };
                    _context.Voters.Add(voter);
                }
                
                // Update opt-in status
                voter.SmsConsentStatus = SmsConsentStatus.OptedIn;
                voter.SmsOptInAt = DateTime.UtcNow;
                voter.SmsOptInMethod = ConsentMethod.WebForm;
                voter.SmsOptInSource = HttpContext.Connection.RemoteIpAddress?.ToString();
                voter.SmsOptOutAt = null;
                
                // Create consent record
                var consentRecord = new ConsentRecord
                {
                    VoterId = voter.LalVoterId,
                    Action = ConsentAction.OptIn,
                    Method = ConsentMethod.WebForm,
                    Timestamp = DateTime.UtcNow,
                    Source = formattedPhone,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    FormUrl = Request.Headers["Referer"].ToString(),
                    ConsentLanguageShown = true,
                    ConsentLanguage = "I agree to receive texts and robocalls from Tanveer for Hoover. Message and data rates may apply. Reply STOP to opt out."
                };
                _context.ConsentRecords.Add(consentRecord);
                
                await _context.SaveChangesAsync();
                
                // Send welcome message
                try
                {
                    await _twilioService.SendSmsAsync(
                        formattedPhone,
                        _optInSettings.FormatMessage(_optInSettings.WelcomeMessage)
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome SMS to {PhoneNumber}", formattedPhone);
                }
                
                return Ok(new OptInResponse
                {
                    Success = true,
                    Message = "Successfully opted in to receive SMS updates.",
                    VoterId = voter.LalVoterId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing web form opt-in");
                return StatusCode(500, new OptInResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your request."
                });
            }
        }
        
        [HttpPost("sms-webhook")]
        public async Task<IActionResult> HandleIncomingSms([FromForm] SmsWebhookRequest request)
        {
            try
            {
                var response = new MessagingResponse();
                var formattedPhone = _twilioService.FormatPhoneNumber(request.From);
                var messageBody = request.Body.Trim().ToUpper();
                
                _logger.LogInformation("Received SMS from {PhoneNumber}: {Message}", formattedPhone, messageBody);
                
                // Find voter by phone number
                var voter = await _context.Voters
                    .FirstOrDefaultAsync(v => v.CellPhone == formattedPhone);
                
                // Check for opt-in keywords
                if (Regex.IsMatch(messageBody, OPT_IN_KEYWORDS, RegexOptions.IgnoreCase))
                {
                    await HandleOptIn(voter, formattedPhone, messageBody);
                    response.Message(_optInSettings.FormatMessage(_optInSettings.WelcomeMessage));
                }
                // Check for opt-out keywords
                else if (Regex.IsMatch(messageBody, OPT_OUT_KEYWORDS, RegexOptions.IgnoreCase))
                {
                    await HandleOptOut(voter, formattedPhone, messageBody);
                    response.Message(_optInSettings.FormatMessage(_optInSettings.OptOutMessage));
                }
                // Handle HELP keyword
                else if (messageBody == "HELP" || messageBody == "INFO")
                {
                    response.Message(_optInSettings.FormatMessage(_optInSettings.HelpMessage));
                }
                // Invalid command
                else
                {
                    // Create consent record for invalid request
                    if (voter != null)
                    {
                        var invalidRecord = new ConsentRecord
                        {
                            VoterId = voter.LalVoterId,
                            Action = ConsentAction.InvalidRequest,
                            Method = ConsentMethod.TextMessage,
                            Timestamp = DateTime.UtcNow,
                            Source = formattedPhone,
                            RawMessage = request.Body,
                            Details = "Unrecognized command"
                        };
                        _context.ConsentRecords.Add(invalidRecord);
                        await _context.SaveChangesAsync();
                    }
                    
                    response.Message($"Invalid command. Reply JOIN to subscribe, STOP to unsubscribe, or HELP for support.");
                }
                
                return Content(response.ToString(), "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling incoming SMS");
                var errorResponse = new MessagingResponse();
                errorResponse.Message("An error occurred processing your request. Please try again later.");
                return Content(errorResponse.ToString(), "application/xml");
            }
        }
        
        [HttpGet("status/{phoneNumber}")]
        public async Task<ActionResult<OptInStatusResponse>> GetOptInStatus(string phoneNumber)
        {
            try
            {
                var formattedPhone = _twilioService.FormatPhoneNumber(phoneNumber);
                if (string.IsNullOrEmpty(formattedPhone))
                {
                    return BadRequest("Invalid phone number format.");
                }
                
                var voter = await _context.Voters
                    .FirstOrDefaultAsync(v => v.CellPhone == formattedPhone);
                
                if (voter == null)
                {
                    return Ok(new OptInStatusResponse
                    {
                        PhoneNumber = formattedPhone,
                        ConsentStatus = SmsConsentStatus.Unknown.ToString()
                    });
                }
                
                return Ok(new OptInStatusResponse
                {
                    PhoneNumber = formattedPhone,
                    ConsentStatus = voter.SmsConsentStatus.ToString(),
                    OptInDate = voter.SmsOptInAt,
                    OptOutDate = voter.SmsOptOutAt,
                    OptInMethod = voter.SmsOptInMethod?.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking opt-in status");
                return StatusCode(500, "An error occurred while checking opt-in status.");
            }
        }
        
        private async Task HandleOptIn(Voter? voter, string formattedPhone, string rawMessage)
        {
            if (voter == null)
            {
                // Create new voter record for opt-in
                voter = new Voter
                {
                    LalVoterId = $"SMS_{Guid.NewGuid():N}",
                    FirstName = "SMS",
                    LastName = "Subscriber",
                    CellPhone = formattedPhone,
                    AddressLine = "Unknown",
                    City = "Hoover",
                    State = "AL",
                    Zip = "00000",
                    Gender = "Unknown",
                    VoteFrequency = VoteFrequency.NonVoter,
                    Age = 0
                };
                _context.Voters.Add(voter);
            }
            
            // Update opt-in status
            voter.SmsConsentStatus = SmsConsentStatus.OptedIn;
            voter.SmsOptInAt = DateTime.UtcNow;
            voter.SmsOptInMethod = ConsentMethod.TextMessage;
            voter.SmsOptInSource = formattedPhone;
            voter.SmsOptOutAt = null;
            
            // Create consent record
            var consentRecord = new ConsentRecord
            {
                VoterId = voter.LalVoterId,
                Action = ConsentAction.OptIn,
                Method = ConsentMethod.TextMessage,
                Timestamp = DateTime.UtcNow,
                Source = formattedPhone,
                RawMessage = rawMessage,
                Details = $"Opted in via SMS keyword: {rawMessage}"
            };
            _context.ConsentRecords.Add(consentRecord);
            
            await _context.SaveChangesAsync();
        }
        
        private async Task HandleOptOut(Voter? voter, string formattedPhone, string rawMessage)
        {
            if (voter != null)
            {
                // Update opt-out status
                voter.SmsConsentStatus = SmsConsentStatus.OptedOut;
                voter.SmsOptOutAt = DateTime.UtcNow;
                
                // Create consent record
                var consentRecord = new ConsentRecord
                {
                    VoterId = voter.LalVoterId,
                    Action = ConsentAction.OptOut,
                    Method = ConsentMethod.TextMessage,
                    Timestamp = DateTime.UtcNow,
                    Source = formattedPhone,
                    RawMessage = rawMessage,
                    Details = $"Opted out via SMS keyword: {rawMessage}"
                };
                _context.ConsentRecords.Add(consentRecord);
                
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Received opt-out from unknown number: {PhoneNumber}", formattedPhone);
            }
        }
    }
}