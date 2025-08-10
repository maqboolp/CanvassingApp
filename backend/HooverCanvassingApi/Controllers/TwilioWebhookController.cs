using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HooverCanvassingApi.Services;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class TwilioWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TwilioWebhookController> _logger;
        private readonly IPhoneNumberPoolService _phoneNumberPool;

        public TwilioWebhookController(ApplicationDbContext context, ILogger<TwilioWebhookController> logger, IPhoneNumberPoolService phoneNumberPool)
        {
            _context = context;
            _logger = logger;
            _phoneNumberPool = phoneNumberPool;
        }

        [HttpPost("sms-status")]
        public async Task<IActionResult> SmsStatusCallback()
        {
            try
            {
                var messageSid = Request.Form["MessageSid"].ToString();
                var messageStatus = Request.Form["MessageStatus"].ToString();
                var errorCode = Request.Form["ErrorCode"].ToString();
                var errorMessage = Request.Form["ErrorMessage"].ToString();

                _logger.LogInformation($"SMS Status callback: SID={messageSid}, Status={messageStatus}");

                var campaignMessage = await _context.CampaignMessages
                    .FirstOrDefaultAsync(cm => cm.TwilioSid == messageSid);

                if (campaignMessage != null)
                {
                    campaignMessage.Status = MapTwilioStatusToMessageStatus(messageStatus);
                    
                    if (!string.IsNullOrEmpty(errorCode) && !string.IsNullOrEmpty(errorMessage))
                    {
                        campaignMessage.ErrorMessage = $"Error {errorCode}: {errorMessage}";
                        campaignMessage.Status = MessageStatus.Failed;
                        campaignMessage.FailedAt = DateTime.UtcNow;
                    }
                    else if (campaignMessage.Status == MessageStatus.Delivered)
                    {
                        campaignMessage.DeliveredAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    await UpdateCampaignStats(campaignMessage.CampaignId);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SMS status callback");
                return StatusCode(500);
            }
        }

        [HttpPost("call-status")]
        public async Task<IActionResult> CallStatusCallback()
        {
            try
            {
                var callSid = Request.Form["CallSid"].ToString();
                var callStatus = Request.Form["CallStatus"].ToString();
                var callDuration = Request.Form["CallDuration"].ToString();
                var recordingUrl = Request.Form["RecordingUrl"].ToString();
                var fromNumber = Request.Form["From"].ToString();

                _logger.LogInformation($"Call Status callback: SID={callSid}, Status={callStatus}, From={fromNumber}");

                var campaignMessage = await _context.CampaignMessages
                    .FirstOrDefaultAsync(cm => cm.TwilioSid == callSid);

                if (campaignMessage != null)
                {
                    campaignMessage.Status = MapTwilioCallStatusToMessageStatus(callStatus);
                    campaignMessage.CallStatus = callStatus;
                    
                    if (int.TryParse(callDuration, out int duration))
                    {
                        campaignMessage.CallDuration = duration;
                    }

                    if (!string.IsNullOrEmpty(recordingUrl))
                    {
                        campaignMessage.RecordingUrl = recordingUrl;
                    }

                    if (campaignMessage.Status == MessageStatus.Completed || 
                        campaignMessage.Status == MessageStatus.Failed ||
                        campaignMessage.Status == MessageStatus.Busy ||
                        campaignMessage.Status == MessageStatus.NoAnswer)
                    {
                        campaignMessage.DeliveredAt = DateTime.UtcNow;
                    }

                    // Track phone stats based on the From number - ONLY for final statuses
                    if (!string.IsNullOrEmpty(fromNumber) && 
                        (campaignMessage.Status == MessageStatus.Completed || 
                         campaignMessage.Status == MessageStatus.Failed ||
                         campaignMessage.Status == MessageStatus.Busy ||
                         campaignMessage.Status == MessageStatus.NoAnswer ||
                         campaignMessage.Status == MessageStatus.Cancelled))
                    {
                        // Normalize the phone number for comparison
                        var normalizedFromNumber = NormalizePhoneNumber(fromNumber);
                        _logger.LogInformation($"Looking up phone number: Original={fromNumber}, Normalized={normalizedFromNumber}");
                        
                        // Look up the phone number in our pool
                        var phoneNumbers = await _phoneNumberPool.GetAllNumbersAsync();
                        var phoneNumber = phoneNumbers.FirstOrDefault(p => 
                            NormalizePhoneNumber(p.Number) == normalizedFromNumber ||
                            p.Number == fromNumber);
                            
                        if (phoneNumber != null)
                        {
                            var success = campaignMessage.Status == MessageStatus.Completed;
                            await _phoneNumberPool.IncrementCallCountAsync(phoneNumber.Id, success);
                            _logger.LogInformation($"Updated phone stats for {fromNumber} (ID: {phoneNumber.Id}), Success: {success}, Final Status: {campaignMessage.Status}");
                        }
                        else
                        {
                            _logger.LogWarning($"Phone number not found in pool: {fromNumber} (normalized: {normalizedFromNumber})");
                            _logger.LogInformation($"Available numbers in pool: {string.Join(", ", phoneNumbers.Select(p => p.Number))}");
                        }
                    }

                    await _context.SaveChangesAsync();
                    await UpdateCampaignStats(campaignMessage.CampaignId);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing call status callback");
                return StatusCode(500);
            }
        }

        [HttpPost("voice")]
        [HttpGet("voice")]
        public IActionResult VoiceResponse([FromQuery] string message = "", [FromQuery] string audioUrl = "")
        {
            try
            {
                string twiml;

                // If audio URL is provided, use that instead of text-to-speech
                if (!string.IsNullOrEmpty(audioUrl))
                {
                    _logger.LogInformation($"Voice response using audio file (raw): {audioUrl}");
                    
                    // Parse and properly encode the URL for XML/HTTP usage
                    // ASP.NET Core automatically decodes query parameters, so we need to re-encode for Twilio
                    try 
                    {
                        var uri = new Uri(audioUrl);
                        // This will properly encode spaces as %20 and other special characters
                        var properlyEncodedUrl = uri.AbsoluteUri;
                        _logger.LogInformation($"Properly encoded URL: {properlyEncodedUrl}");
                        audioUrl = properlyEncodedUrl;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to parse/encode audio URL: {audioUrl}");
                        // Try basic encoding as fallback
                        audioUrl = audioUrl.Replace(" ", "%20");
                        _logger.LogInformation($"Fallback encoded URL: {audioUrl}");
                    }
                    
                    // Play the audio file with repeat option
                    twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Gather numDigits=""1"" action=""/api/TwilioWebhook/handle-playback-input"" method=""POST"" timeout=""3"">
        <Play loop=""1"">{System.Security.SecurityElement.Escape(audioUrl)}</Play>
        <Say voice=""alice"">Press 1 to listen again, Press 9 to hang up.</Say>
    </Gather>
    <Hangup/>
</Response>";
                }
                else
                {
                    // Default message if none provided
                    if (string.IsNullOrEmpty(message))
                    {
                        message = "Hello, this is a message from the campaign. Thank you for your time.";
                    }

                    // Escape XML special characters
                    message = System.Security.SecurityElement.Escape(message);

                    // Generate TwiML for the robo call with text-to-speech and repeat option
                    twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Gather numDigits=""1"" action=""/api/TwilioWebhook/handle-playback-input"" method=""POST"" timeout=""3"">
        <Say voice=""alice"">{message}</Say>
        <Say voice=""alice"">Press 1 to listen again, Press 9 to hang up.</Say>
    </Gather>
    <Hangup/>
</Response>";

                    _logger.LogInformation($"Voice response generated for message: {message}");
                }

                _logger.LogInformation($"Returning TwiML: {twiml}");
                
                return Content(twiml, "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating voice response");
                
                // Return a safe error response
                var errorTwiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say voice=""alice"">We apologize, but there was an error processing this call. Goodbye.</Say>
</Response>";
                
                return Content(errorTwiml, "application/xml");
            }
        }

        private async Task UpdateCampaignStats(int campaignId)
        {
            var campaign = await _context.Campaigns
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == campaignId);

            if (campaign != null)
            {
                campaign.SuccessfulDeliveries = campaign.Messages.Count(m => 
                    m.Status == MessageStatus.Delivered || 
                    m.Status == MessageStatus.Completed);
                
                campaign.FailedDeliveries = campaign.Messages.Count(m => 
                    m.Status == MessageStatus.Failed ||
                    m.Status == MessageStatus.Undelivered ||
                    m.Status == MessageStatus.Busy ||
                    m.Status == MessageStatus.NoAnswer);
                
                campaign.PendingDeliveries = campaign.Messages.Count(m => 
                    m.Status == MessageStatus.Pending ||
                    m.Status == MessageStatus.Queued ||
                    m.Status == MessageStatus.Sending);

                // Update campaign status if all messages are processed
                if (campaign.PendingDeliveries == 0 && campaign.Status == CampaignStatus.Sending)
                {
                    campaign.Status = CampaignStatus.Completed;
                }

                await _context.SaveChangesAsync();
            }
        }

        private MessageStatus MapTwilioStatusToMessageStatus(string twilioStatus)
        {
            return twilioStatus.ToLower() switch
            {
                "queued" => MessageStatus.Queued,
                "sending" => MessageStatus.Sending,
                "sent" => MessageStatus.Sent,
                "delivered" => MessageStatus.Delivered,
                "failed" => MessageStatus.Failed,
                "undelivered" => MessageStatus.Undelivered,
                _ => MessageStatus.Pending
            };
        }

        private MessageStatus MapTwilioCallStatusToMessageStatus(string twilioCallStatus)
        {
            return twilioCallStatus.ToLower() switch
            {
                "queued" => MessageStatus.Queued,
                "ringing" => MessageStatus.Sending,
                "in-progress" => MessageStatus.Sending,
                "completed" => MessageStatus.Completed,
                "busy" => MessageStatus.Busy,
                "no-answer" => MessageStatus.NoAnswer,
                "failed" => MessageStatus.Failed,
                "canceled" => MessageStatus.Cancelled,
                _ => MessageStatus.Pending
            };
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;
                
            // Remove all non-numeric characters
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            
            // If it's 11 digits and starts with 1, remove the 1
            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                digitsOnly = digitsOnly.Substring(1);
            }
            
            // Return just the 10-digit number for consistent comparison
            return digitsOnly;
        }

        [HttpPost("incoming-call")]
        [HttpGet("incoming-call")]
        public IActionResult IncomingCall()
        {
            try
            {
                var fromNumber = Request.Form["From"].ToString();
                if (string.IsNullOrEmpty(fromNumber) && Request.Query.ContainsKey("From"))
                {
                    fromNumber = Request.Query["From"].ToString();
                }

                _logger.LogInformation($"Incoming call from: {fromNumber}");

                // Generate TwiML for IVR menu
                var twiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Gather numDigits=""1"" action=""/api/TwilioWebhook/handle-ivr-input"" method=""POST"" timeout=""10"">
        <Say voice=""alice"">Thank you for calling. Press 1 to be removed from our calling list.</Say>
    </Gather>
    <Say voice=""alice"">Goodbye.</Say>
</Response>";

                return Content(twiml, "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling incoming call");
                
                var errorTwiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say voice=""alice"">We're sorry, but we cannot process your request at this time. Please try again later.</Say>
</Response>";
                
                return Content(errorTwiml, "application/xml");
            }
        }

        [HttpPost("handle-playback-input")]
        [HttpGet("handle-playback-input")]
        public IActionResult HandlePlaybackInput([FromQuery] string message = "", [FromQuery] string audioUrl = "")
        {
            try
            {
                var digits = Request.Form["Digits"].ToString();
                if (string.IsNullOrEmpty(digits) && Request.Query.ContainsKey("Digits"))
                {
                    digits = Request.Query["Digits"].ToString();
                }
                
                // Get the original message or audio URL from the query parameters
                if (string.IsNullOrEmpty(message) && Request.Form.ContainsKey("message"))
                {
                    message = Request.Form["message"].ToString();
                }
                if (string.IsNullOrEmpty(audioUrl) && Request.Form.ContainsKey("audioUrl"))
                {
                    audioUrl = Request.Form["audioUrl"].ToString();
                }
                
                _logger.LogInformation($"Playback input received: {digits}");

                string twiml;

                if (digits == "1")
                {
                    // Repeat the message
                    return VoiceResponse(message, audioUrl);
                }
                else if (digits == "9")
                {
                    // Hang up
                    twiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say voice=""alice"">Goodbye.</Say>
    <Hangup/>
</Response>";
                }
                else
                {
                    // Invalid input, just hang up
                    twiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Hangup/>
</Response>";
                }

                return Content(twiml, "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling playback input");
                
                var errorTwiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Hangup/>
</Response>";
                
                return Content(errorTwiml, "application/xml");
            }
        }

        [HttpPost("handle-ivr-input")]
        public async Task<IActionResult> HandleIvrInput()
        {
            try
            {
                var digits = Request.Form["Digits"].ToString();
                var fromNumber = Request.Form["From"].ToString();
                
                _logger.LogInformation($"IVR input from {fromNumber}: {digits}");

                string twiml;

                if (digits == "1")
                {
                    // Opt-out selected
                    await ProcessOptOut(fromNumber, OptOutMethod.Phone);
                    
                    twiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say voice=""alice"">You have been successfully removed from our calling list. You will not receive any more calls from us. Thank you and goodbye.</Say>
</Response>";
                }
                else
                {
                    // Invalid selection or no selection
                    twiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say voice=""alice"">Goodbye.</Say>
</Response>";
                }

                return Content(twiml, "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling IVR input");
                
                var errorTwiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say voice=""alice"">We're sorry, but we cannot process your request at this time.</Say>
</Response>";
                
                return Content(errorTwiml, "application/xml");
            }
        }

        [HttpPost("incoming-sms")]
        public async Task<IActionResult> IncomingSms()
        {
            try
            {
                var fromNumber = Request.Form["From"].ToString();
                var body = Request.Form["Body"].ToString().Trim().ToUpper();
                
                _logger.LogInformation($"Incoming SMS from {fromNumber}: {body}");

                string responseMessage = null;

                // Check for opt-out keywords
                var optOutKeywords = new[] { "STOP", "UNSUBSCRIBE", "CANCEL", "END", "QUIT", "STOPALL", "STOP ALL" };
                
                if (optOutKeywords.Contains(body))
                {
                    await ProcessOptOut(fromNumber, OptOutMethod.SMS);
                    responseMessage = "You have been unsubscribed and will no longer receive messages from us. Reply START to resubscribe.";
                }
                else if (body == "START" || body == "SUBSCRIBE")
                {
                    await RemoveOptOut(fromNumber);
                    responseMessage = "You have been resubscribed to receive messages from us. Reply STOP to unsubscribe.";
                }

                // Generate TwiML response if we have a message to send back
                if (!string.IsNullOrEmpty(responseMessage))
                {
                    var twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Message>{System.Security.SecurityElement.Escape(responseMessage)}</Message>
</Response>";
                    
                    return Content(twiml, "application/xml");
                }

                // Empty response if we don't need to reply
                return Content(@"<?xml version=""1.0"" encoding=""UTF-8""?><Response></Response>", "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling incoming SMS");
                return Content(@"<?xml version=""1.0"" encoding=""UTF-8""?><Response></Response>", "application/xml");
            }
        }

        private async Task ProcessOptOut(string phoneNumber, OptOutMethod method)
        {
            try
            {
                var normalizedNumber = NormalizePhoneNumber(phoneNumber);
                
                // Check if already opted out
                var existingOptOut = await _context.OptOutRecords
                    .FirstOrDefaultAsync(o => o.PhoneNumber == normalizedNumber);
                
                if (existingOptOut == null)
                {
                    // Try to find the voter by matching different phone formats
                    // normalizedNumber is just 10 digits, e.g., "2055551234"
                    var phoneVariants = new List<string>
                    {
                        normalizedNumber,                                    // 2055551234
                        $"1{normalizedNumber}",                             // 12055551234
                        $"+1{normalizedNumber}",                            // +12055551234
                        $"({normalizedNumber.Substring(0, 3)}) {normalizedNumber.Substring(3, 3)}-{normalizedNumber.Substring(6)}", // (205) 555-1234
                        $"{normalizedNumber.Substring(0, 3)}-{normalizedNumber.Substring(3, 3)}-{normalizedNumber.Substring(6)}"    // 205-555-1234
                    };
                    
                    var voter = await _context.Voters
                        .FirstOrDefaultAsync(v => phoneVariants.Contains(v.CellPhone));
                    
                    var optOut = new OptOutRecord
                    {
                        PhoneNumber = normalizedNumber,
                        Type = method == OptOutMethod.SMS ? OptOutType.SMS : OptOutType.All,
                        Method = method,
                        OptedOutAt = DateTime.UtcNow,
                        VoterId = voter?.LalVoterId
                    };
                    
                    _context.OptOutRecords.Add(optOut);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Phone number {normalizedNumber} opted out via {method}");
                }
                else
                {
                    _logger.LogInformation($"Phone number {normalizedNumber} was already opted out");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing opt-out for {phoneNumber}");
            }
        }

        private async Task RemoveOptOut(string phoneNumber)
        {
            try
            {
                var normalizedNumber = NormalizePhoneNumber(phoneNumber);
                
                var optOut = await _context.OptOutRecords
                    .FirstOrDefaultAsync(o => o.PhoneNumber == normalizedNumber);
                
                if (optOut != null)
                {
                    _context.OptOutRecords.Remove(optOut);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Phone number {normalizedNumber} resubscribed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing opt-out for {phoneNumber}");
            }
        }
    }
}