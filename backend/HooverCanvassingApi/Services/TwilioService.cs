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
        private readonly IPhoneNumberPoolService _phoneNumberPool;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;
        private readonly string? _smsPhoneNumber;
        private readonly string? _messagingServiceSid;

        public TwilioService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<TwilioService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IPhoneNumberPoolService phoneNumberPool)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _phoneNumberPool = phoneNumberPool;
            
            // Try to load settings from database first
            var dbSettings = context.TwilioConfigurations
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefault();
            
            string? accountSid;
            string? authToken;
            string? fromPhone;
            string? smsPhone;
            string? messagingSid;
            
            if (dbSettings != null)
            {
                _logger.LogDebug("Loading Twilio configuration from database");
                accountSid = dbSettings.AccountSid;
                authToken = dbSettings.AuthToken;
                fromPhone = dbSettings.FromPhoneNumber;
                smsPhone = dbSettings.SmsPhoneNumber;
                messagingSid = dbSettings.MessagingServiceSid;
            }
            else
            {
                _logger.LogDebug("Loading Twilio configuration from appsettings.json");
                accountSid = _configuration["Twilio:AccountSid"];
                authToken = _configuration["Twilio:AuthToken"];
                fromPhone = _configuration["Twilio:FromPhoneNumber"];
                smsPhone = _configuration["Twilio:SmsPhoneNumber"];
                messagingSid = _configuration["Twilio:MessagingServiceSid"];
            }
            
            _logger.LogDebug($"Twilio Config - AccountSid: {(string.IsNullOrEmpty(accountSid) ? "MISSING" : $"***{accountSid.Substring(Math.Max(0, accountSid.Length - 4))}")}");
            _logger.LogDebug($"Twilio Config - AuthToken: {(string.IsNullOrEmpty(authToken) ? "MISSING" : "***CONFIGURED")}");
            _logger.LogDebug($"Twilio Config - FromPhone: {(string.IsNullOrEmpty(fromPhone) ? "MISSING" : fromPhone)}");
            _logger.LogDebug($"Twilio Config - SmsPhone: {(string.IsNullOrEmpty(smsPhone) ? "NOT SET (will use FromPhone)" : smsPhone)}");
            _logger.LogDebug($"Twilio Config - MessagingServiceSid: {(string.IsNullOrEmpty(messagingSid) ? "NOT SET" : $"***{messagingSid.Substring(Math.Max(0, messagingSid.Length - 4))}")}");
            
            _accountSid = accountSid ?? throw new InvalidOperationException("Twilio AccountSid not configured");
            _authToken = authToken ?? throw new InvalidOperationException("Twilio AuthToken not configured");
            _fromPhoneNumber = fromPhone ?? throw new InvalidOperationException("Twilio FromPhoneNumber not configured");
            _smsPhoneNumber = smsPhone; // Optional - will fall back to FromPhoneNumber if not set
            _messagingServiceSid = messagingSid; // Optional for bulk SMS
            
            TwilioClient.Init(_accountSid, _authToken);
            _logger.LogDebug("TwilioClient initialized successfully");
        }

        public async Task<bool> SendSmsAsync(string toPhoneNumber, string message, int campaignMessageId, bool overrideOptIn = false)
        {
            try
            {
                _logger.LogInformation($"Attempting to send SMS to {toPhoneNumber} for campaign message {campaignMessageId}");
                var formattedNumber = FormatPhoneNumber(toPhoneNumber);
                
                // Check opt-in status unless override is specified
                if (!overrideOptIn && !await CheckOptInStatusAsync(formattedNumber))
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
                        to: new Twilio.Types.PhoneNumber(formattedNumber)
                    );
                }
                else
                {
                    // Use SMS-specific phone number if configured, otherwise fall back to default
                    var smsFromNumber = !string.IsNullOrEmpty(_smsPhoneNumber) ? _smsPhoneNumber : _fromPhoneNumber;
                    _logger.LogInformation($"Using From Phone Number: {smsFromNumber}");
                    messageResource = await MessageResource.CreateAsync(
                        body: message,
                        from: new Twilio.Types.PhoneNumber(smsFromNumber),
                        to: new Twilio.Types.PhoneNumber(formattedNumber)
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
            TwilioPhoneNumber? phoneNumber = null;
            bool callInitiated = false;
            try
            {
                _logger.LogInformation($"Starting robo call for campaign message {campaignMessageId} to {toPhoneNumber}");
                var formattedNumber = FormatPhoneNumber(toPhoneNumber);
                _logger.LogInformation($"Formatted phone number: {formattedNumber}");
                
                // Get an available phone number from the pool
                phoneNumber = await _phoneNumberPool.GetNextAvailableNumberAsync();
                if (phoneNumber == null)
                {
                    _logger.LogError("No available phone numbers in pool. Ensure phone numbers are added to the pool.");
                    throw new InvalidOperationException("No phone numbers available in the pool");
                }
                
                _logger.LogInformation($"Using phone number {phoneNumber.Number} from pool for call to {formattedNumber}");
                
                // Construct the status callback URL
                var baseUrl = voiceUrl.Substring(0, voiceUrl.IndexOf("/api/"));
                var statusCallbackUrl = $"{baseUrl}/api/TwilioWebhook/call-status";
                _logger.LogInformation($"Setting status callback URL: {statusCallbackUrl}");
                
                var poolCall = await CallResource.CreateAsync(
                    url: new Uri(voiceUrl),
                    to: new Twilio.Types.PhoneNumber(formattedNumber),
                    from: new Twilio.Types.PhoneNumber(phoneNumber.Number),
                    timeout: 60,
                    record: false,
                    statusCallback: new Uri(statusCallbackUrl),
                    statusCallbackEvent: new List<string> 
                    { 
                        "initiated",
                        "ringing",
                        "answered",
                        "completed"
                    }
                );

                await UpdateCampaignMessageWithCall(campaignMessageId, poolCall);
                
                // Release the phone number immediately after initiating the call
                // Twilio will handle queueing if the number is busy
                await _phoneNumberPool.ReleaseNumberAsync(phoneNumber.Id);
                _logger.LogInformation($"Robo call initiated to {formattedNumber} from {phoneNumber.Number}, SID: {poolCall.Sid}");
                callInitiated = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to make robo call to {toPhoneNumber}. Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Track failed call if we got a phone number
                if (phoneNumber != null)
                {
                    await _phoneNumberPool.IncrementCallCountAsync(phoneNumber.Id, false);
                }
                
                // Update campaign message with error
                using (var errorScope = _serviceScopeFactory.CreateScope())
                {
                    var errorContext = errorScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var campaignMessage = await errorContext.CampaignMessages
                        .FirstOrDefaultAsync(cm => cm.Id == campaignMessageId);
                    
                    if (campaignMessage != null)
                    {
                        campaignMessage.Status = MessageStatus.Failed;
                        campaignMessage.ErrorMessage = ex.Message;
                        campaignMessage.FailedAt = DateTime.UtcNow;
                        
                        await errorContext.SaveChangesAsync();
                    }
                }
                
                return false;
            }
            finally
            {
                // Release phone number if call failed to initiate
                if (phoneNumber != null && !callInitiated)
                {
                    try
                    {
                        await _phoneNumberPool.ReleaseNumberAsync(phoneNumber.Id);
                        _logger.LogInformation($"Released phone number {phoneNumber.Number} due to failed call initiation");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error releasing phone number {phoneNumber.Number}");
                    }
                }
            }
        }
        
        private async Task UpdateCampaignMessageWithCall(int campaignMessageId, CallResource call)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var campaignMessage = await scopedContext.CampaignMessages
                .FirstOrDefaultAsync(cm => cm.Id == campaignMessageId);
            
            if (campaignMessage != null)
            {
                campaignMessage.TwilioSid = call.Sid;
                campaignMessage.Status = MapTwilioCallStatusToMessageStatus(call.Status.ToString());
                campaignMessage.SentAt = DateTime.UtcNow;
                
                await scopedContext.SaveChangesAsync();
            }
        }

        public async Task<CampaignMessage?> GetMessageStatusAsync(string twilioSid)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var campaignMessage = await scopedContext.CampaignMessages
                    .Include(cm => cm.Campaign)
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

                await scopedContext.SaveChangesAsync();
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

        public async Task<List<bool>> SendBulkSmsAsync(List<(string phoneNumber, string message, int campaignMessageId)> messages, bool overrideOptIn = false)
        {
            _logger.LogInformation($"Starting bulk SMS for {messages.Count} messages with overrideOptIn={overrideOptIn}");
            _logger.LogInformation($"Twilio AccountSid configured: {!string.IsNullOrEmpty(_accountSid)}");
            _logger.LogInformation($"Twilio AuthToken configured: {!string.IsNullOrEmpty(_authToken)}");
            _logger.LogInformation($"Twilio FromPhone configured: {!string.IsNullOrEmpty(_fromPhoneNumber)}");
            
            var results = new List<bool>();
            var semaphore = new SemaphoreSlim(10, 10); // Limit concurrent requests to 10
            var tasks = new List<Task<bool>>();

            foreach (var (phoneNumber, message, campaignMessageId) in messages)
            {
                _logger.LogInformation($"Queuing SMS for {phoneNumber}, message ID: {campaignMessageId}");
                tasks.Add(SendSmsWithSemaphoreAsync(phoneNumber, message, campaignMessageId, semaphore, overrideOptIn));
            }

            var taskResults = await Task.WhenAll(tasks);
            results.AddRange(taskResults);

            _logger.LogInformation($"Bulk SMS completed: {results.Count(r => r)} successful, {results.Count(r => !r)} failed");
            return results;
        }

        private async Task<bool> SendSmsWithSemaphoreAsync(string phoneNumber, string message, int campaignMessageId, SemaphoreSlim semaphore, bool overrideOptIn = false)
        {
            await semaphore.WaitAsync();
            try
            {
                // Small delay to avoid hitting rate limits
                await Task.Delay(100);
                return await SendSmsAsync(phoneNumber, message, campaignMessageId, overrideOptIn);
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
                        to: new Twilio.Types.PhoneNumber(formattedNumber)
                    );
                }
                else
                {
                    // Use SMS-specific phone number if configured, otherwise fall back to default
                    var smsFromNumber = !string.IsNullOrEmpty(_smsPhoneNumber) ? _smsPhoneNumber : _fromPhoneNumber;
                    _logger.LogInformation($"Using From Phone Number: {smsFromNumber}");
                    messageResource = await MessageResource.CreateAsync(
                        body: message,
                        from: new Twilio.Types.PhoneNumber(smsFromNumber),
                        to: new Twilio.Types.PhoneNumber(formattedNumber)
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