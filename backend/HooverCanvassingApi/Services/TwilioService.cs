using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace HooverCanvassingApi.Services
{
    public class TwilioService : ITwilioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TwilioService> _logger;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;

        public TwilioService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<TwilioService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            
            _accountSid = _configuration["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio AccountSid not configured");
            _authToken = _configuration["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio AuthToken not configured");
            _fromPhoneNumber = _configuration["Twilio:FromPhoneNumber"] ?? throw new InvalidOperationException("Twilio FromPhoneNumber not configured");
            
            TwilioClient.Init(_accountSid, _authToken);
        }

        public async Task<bool> SendSmsAsync(string toPhoneNumber, string message, int campaignMessageId)
        {
            try
            {
                var formattedNumber = FormatPhoneNumber(toPhoneNumber);
                
                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_fromPhoneNumber),
                    to: new PhoneNumber(formattedNumber)
                );

                // Update campaign message with Twilio SID and status
                var campaignMessage = await _context.CampaignMessages
                    .FirstOrDefaultAsync(cm => cm.Id == campaignMessageId);
                
                if (campaignMessage != null)
                {
                    campaignMessage.TwilioSid = messageResource.Sid;
                    campaignMessage.Status = MapTwilioStatusToMessageStatus(messageResource.Status.ToString());
                    campaignMessage.SentAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"SMS sent successfully to {formattedNumber}, SID: {messageResource.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {toPhoneNumber}");
                
                // Update campaign message with error
                var campaignMessage = await _context.CampaignMessages
                    .FirstOrDefaultAsync(cm => cm.Id == campaignMessageId);
                
                if (campaignMessage != null)
                {
                    campaignMessage.Status = MessageStatus.Failed;
                    campaignMessage.ErrorMessage = ex.Message;
                    campaignMessage.FailedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                }
                
                return false;
            }
        }

        public async Task<bool> MakeRoboCallAsync(string toPhoneNumber, string voiceUrl, int campaignMessageId)
        {
            try
            {
                var formattedNumber = FormatPhoneNumber(toPhoneNumber);
                
                var call = await CallResource.CreateAsync(
                    url: new Uri(voiceUrl),
                    to: new PhoneNumber(formattedNumber),
                    from: new PhoneNumber(_fromPhoneNumber),
                    timeout: 60,
                    record: false // Set to true if you want to record calls
                );

                // Update campaign message with Twilio SID and status
                var campaignMessage = await _context.CampaignMessages
                    .FirstOrDefaultAsync(cm => cm.Id == campaignMessageId);
                
                if (campaignMessage != null)
                {
                    campaignMessage.TwilioSid = call.Sid;
                    campaignMessage.Status = MapTwilioCallStatusToMessageStatus(call.Status.ToString());
                    campaignMessage.SentAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Robo call initiated to {formattedNumber}, SID: {call.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to make robo call to {toPhoneNumber}");
                
                // Update campaign message with error
                var campaignMessage = await _context.CampaignMessages
                    .FirstOrDefaultAsync(cm => cm.Id == campaignMessageId);
                
                if (campaignMessage != null)
                {
                    campaignMessage.Status = MessageStatus.Failed;
                    campaignMessage.ErrorMessage = ex.Message;
                    campaignMessage.FailedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                }
                
                return false;
            }
        }

        public async Task<CampaignMessage?> GetMessageStatusAsync(string twilioSid)
        {
            try
            {
                var campaignMessage = await _context.CampaignMessages
                    .FirstOrDefaultAsync(cm => cm.TwilioSid == twilioSid);
                
                if (campaignMessage == null)
                    return null;

                // Check if it's an SMS or Call
                if (campaignMessage.Campaign.Type == CampaignType.SMS)
                {
                    var message = await MessageResource.FetchAsync(twilioSid);
                    campaignMessage.Status = MapTwilioStatusToMessageStatus(message.Status.ToString());
                    campaignMessage.Cost = message.Price != null ? decimal.Parse(message.Price.ToString()) : null;
                }
                else
                {
                    var call = await CallResource.FetchAsync(twilioSid);
                    campaignMessage.Status = MapTwilioCallStatusToMessageStatus(call.Status.ToString());
                    campaignMessage.CallDuration = call.Duration != null ? int.Parse(call.Duration.ToString()) : null;
                    campaignMessage.CallStatus = call.Status.ToString();
                    campaignMessage.Cost = call.Price != null ? decimal.Parse(call.Price.ToString()) : null;
                }

                await _context.SaveChangesAsync();
                return campaignMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get status for Twilio SID: {twilioSid}");
                return null;
            }
        }

        public Task<bool> ValidatePhoneNumberAsync(string phoneNumber)
        {
            try
            {
                var formattedNumber = FormatPhoneNumber(phoneNumber);
                // Simple validation - you could use Twilio Lookup API for more advanced validation
                return Task.FromResult(Regex.IsMatch(formattedNumber, @"^\+1[2-9]\d{9}$"));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public string FormatPhoneNumber(string phoneNumber)
        {
            // Remove all non-digits
            var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "");
            
            // Add country code if missing
            if (digitsOnly.Length == 10)
            {
                digitsOnly = "1" + digitsOnly;
            }
            
            // Add + prefix
            return "+" + digitsOnly;
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