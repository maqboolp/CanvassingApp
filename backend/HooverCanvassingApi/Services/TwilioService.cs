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
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;
        private readonly string? _messagingServiceSid;

        public TwilioService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<TwilioService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            
            // Debug logging for Twilio configuration
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var fromPhone = _configuration["Twilio:FromPhoneNumber"];
            var messagingSid = _configuration["Twilio:MessagingServiceSid"];
            
            _logger.LogInformation($"Twilio Config - AccountSid: {(string.IsNullOrEmpty(accountSid) ? "MISSING" : $"***{accountSid.Substring(Math.Max(0, accountSid.Length - 4))}")}");
            _logger.LogInformation($"Twilio Config - AuthToken: {(string.IsNullOrEmpty(authToken) ? "MISSING" : "***CONFIGURED")}");
            _logger.LogInformation($"Twilio Config - FromPhone: {(string.IsNullOrEmpty(fromPhone) ? "MISSING" : fromPhone)}");
            _logger.LogInformation($"Twilio Config - MessagingServiceSid: {(string.IsNullOrEmpty(messagingSid) ? "NOT SET" : $"***{messagingSid.Substring(Math.Max(0, messagingSid.Length - 4))}")}");
            
            _accountSid = accountSid ?? throw new InvalidOperationException("Twilio AccountSid not configured");
            _authToken = authToken ?? throw new InvalidOperationException("Twilio AuthToken not configured");
            _fromPhoneNumber = fromPhone ?? throw new InvalidOperationException("Twilio FromPhoneNumber not configured");
            _messagingServiceSid = messagingSid; // Optional for bulk SMS
            
            TwilioClient.Init(_accountSid, _authToken);
            _logger.LogInformation("TwilioClient initialized successfully");
        }

        public async Task<bool> SendSmsAsync(string toPhoneNumber, string message, int campaignMessageId)
        {
            try
            {
                _logger.LogInformation($"Attempting to send SMS to {toPhoneNumber} for campaign message {campaignMessageId}");
                var formattedNumber = FormatPhoneNumber(toPhoneNumber);
                
                // Check opt-in status
                if (!await CheckOptInStatusAsync(formattedNumber))
                {
                    _logger.LogWarning($"Cannot send SMS to {formattedNumber} - user is not opted in");
                    
                    // Update campaign message as failed due to opt-out
                    using var optInScope = _serviceScopeFactory.CreateScope();
                    var optInContext = optInScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var optInCampaignMessage = await optInContext.CampaignMessages
                        .FirstOrDefaultAsync(cm => cm.Id == campaignMessageId);
                    
                    if (optInCampaignMessage != null)
                    {
                        optInCampaignMessage.Status = MessageStatus.Failed;
                        optInCampaignMessage.ErrorMessage = "Recipient has not opted in to receive SMS messages";
                        optInCampaignMessage.FailedAt = DateTime.UtcNow;
                        await optInContext.SaveChangesAsync();
                    }
                    
                    return false;
                }
                _logger.LogInformation($"Formatted phone number: {formattedNumber}");
                
                MessageResource messageResource;
                
                // Use Messaging Service if available for better bulk performance
                if (!string.IsNullOrEmpty(_messagingServiceSid))
                {
                    _logger.LogInformation($"Using Messaging Service SID: ***{_messagingServiceSid.Substring(Math.Max(0, _messagingServiceSid.Length - 4))}");
                    messageResource = await MessageResource.CreateAsync(
                        body: message,
                        messagingServiceSid: _messagingServiceSid,
                        to: new PhoneNumber(formattedNumber)
                    );
                }
                else
                {
                    _logger.LogInformation($"Using From Phone Number: {_fromPhoneNumber}");
                    messageResource = await MessageResource.CreateAsync(
                        body: message,
                        from: new PhoneNumber(_fromPhoneNumber),
                        to: new PhoneNumber(formattedNumber)
                    );
                }

                // Update campaign message with Twilio SID and status using scoped context
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var campaignMessage = await scopedContext.CampaignMessages
                    .FirstOrDefaultAsync(cm => cm.Id == campaignMessageId);
                
                if (campaignMessage != null)
                {
                    campaignMessage.TwilioSid = messageResource.Sid;
                    campaignMessage.Status = MapTwilioStatusToMessageStatus(messageResource.Status.ToString());
                    campaignMessage.SentAt = DateTime.UtcNow;
                    
                    await scopedContext.SaveChangesAsync();
                }

                _logger.LogInformation($"SMS sent successfully to {formattedNumber}, SID: {messageResource.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {toPhoneNumber}");
                
                // Update campaign message with error using scoped context
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var campaignMessage = await scopedContext.CampaignMessages
                    .FirstOrDefaultAsync(cm => cm.Id == campaignMessageId);
                
                if (campaignMessage != null)
                {
                    campaignMessage.Status = MessageStatus.Failed;
                    campaignMessage.ErrorMessage = ex.Message;
                    campaignMessage.FailedAt = DateTime.UtcNow;
                    
                    await scopedContext.SaveChangesAsync();
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

        public async Task<List<bool>> SendBulkSmsAsync(List<(string phoneNumber, string message, int campaignMessageId)> messages)
        {
            _logger.LogInformation($"Starting bulk SMS for {messages.Count} messages");
            _logger.LogInformation($"Twilio AccountSid configured: {!string.IsNullOrEmpty(_accountSid)}");
            _logger.LogInformation($"Twilio AuthToken configured: {!string.IsNullOrEmpty(_authToken)}");
            _logger.LogInformation($"Twilio FromPhone configured: {!string.IsNullOrEmpty(_fromPhoneNumber)}");
            
            var results = new List<bool>();
            var semaphore = new SemaphoreSlim(10, 10); // Limit concurrent requests to 10
            var tasks = new List<Task<bool>>();

            foreach (var (phoneNumber, message, campaignMessageId) in messages)
            {
                _logger.LogInformation($"Queuing SMS for {phoneNumber}, message ID: {campaignMessageId}");
                tasks.Add(SendSmsWithSemaphoreAsync(phoneNumber, message, campaignMessageId, semaphore));
            }

            var taskResults = await Task.WhenAll(tasks);
            results.AddRange(taskResults);

            _logger.LogInformation($"Bulk SMS completed: {results.Count(r => r)} successful, {results.Count(r => !r)} failed");
            return results;
        }

        private async Task<bool> SendSmsWithSemaphoreAsync(string phoneNumber, string message, int campaignMessageId, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                // Small delay to avoid hitting rate limits
                await Task.Delay(100);
                return await SendSmsAsync(phoneNumber, message, campaignMessageId);
            }
            finally
            {
                semaphore.Release();
            }
        }
        
        // Overload for non-campaign messages (welcome messages, etc.)
        public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
        {
            try
            {
                _logger.LogInformation($"Attempting to send non-campaign SMS to {toPhoneNumber}");
                var formattedNumber = FormatPhoneNumber(toPhoneNumber);
                _logger.LogInformation($"Formatted phone number: {formattedNumber}");
                
                MessageResource messageResource;
                
                // Use Messaging Service if available for better bulk performance
                if (!string.IsNullOrEmpty(_messagingServiceSid))
                {
                    _logger.LogInformation($"Using Messaging Service SID: ***{_messagingServiceSid.Substring(Math.Max(0, _messagingServiceSid.Length - 4))}");
                    messageResource = await MessageResource.CreateAsync(
                        body: message,
                        messagingServiceSid: _messagingServiceSid,
                        to: new PhoneNumber(formattedNumber)
                    );
                }
                else
                {
                    _logger.LogInformation($"Using From Phone Number: {_fromPhoneNumber}");
                    messageResource = await MessageResource.CreateAsync(
                        body: message,
                        from: new PhoneNumber(_fromPhoneNumber),
                        to: new PhoneNumber(formattedNumber)
                    );
                }

                _logger.LogInformation($"SMS sent successfully to {formattedNumber}, SID: {messageResource.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {toPhoneNumber}");
                return false;
            }
        }
        
        public async Task<bool> CheckOptInStatusAsync(string phoneNumber)
        {
            try
            {
                var formattedNumber = FormatPhoneNumber(phoneNumber);
                
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var voter = await scopedContext.Voters
                    .FirstOrDefaultAsync(v => v.CellPhone == formattedNumber);
                
                if (voter == null)
                {
                    _logger.LogDebug($"No voter found with phone number {formattedNumber}");
                    return false; // No voter record means not opted in
                }
                
                return voter.SmsConsentStatus == SmsConsentStatus.OptedIn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking opt-in status for {phoneNumber}");
                return false; // Fail safe - don't send if we can't verify opt-in
            }
        }
    }
}