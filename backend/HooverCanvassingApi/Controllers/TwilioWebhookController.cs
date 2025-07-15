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

                _logger.LogInformation($"Call Status callback: SID={callSid}, Status={callStatus}");

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

                    // Track phone stats based on the From number
                    if (!string.IsNullOrEmpty(fromNumber))
                    {
                        // Look up the phone number in our pool
                        var phoneNumbers = await _phoneNumberPool.GetAllNumbersAsync();
                        var phoneNumber = phoneNumbers.FirstOrDefault(p => p.Number == fromNumber);
                        if (phoneNumber != null)
                        {
                            var success = campaignMessage.Status == MessageStatus.Completed;
                            await _phoneNumberPool.IncrementCallCountAsync(phoneNumber.Id, success);
                            _logger.LogInformation($"Updated phone stats for {fromNumber} (ID: {phoneNumber.Id}), Success: {success}");
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
                    
                    // Add a fallback message in case the audio fails to play
                    twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Play loop=""1"">{System.Security.SecurityElement.Escape(audioUrl)}</Play>
    <Say voice=""alice"">If you did not hear the message, please contact us. Thank you.</Say>
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

                    // Generate TwiML for the robo call with text-to-speech
                    twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say voice=""alice"">{message}</Say>
    <Pause length=""1""/>
    <Say voice=""alice"">Thank you for your time. Goodbye.</Say>
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
    }
}