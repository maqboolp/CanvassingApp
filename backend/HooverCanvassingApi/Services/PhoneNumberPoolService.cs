using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace HooverCanvassingApi.Services
{
    public interface IPhoneNumberPoolService
    {
        Task<TwilioPhoneNumber?> GetNextAvailableNumberAsync();
        Task ReleaseNumberAsync(int phoneNumberId);
        Task<List<TwilioPhoneNumber>> GetAllNumbersAsync();
        Task<List<TwilioPhoneNumber>> AddPhoneNumbersAsync(string phoneNumbers);
        Task<bool> RemovePhoneNumberAsync(int id);
        Task<bool> UpdatePhoneNumberAsync(int id, bool isActive, int maxConcurrentCalls);
        Task IncrementCallCountAsync(int phoneNumberId, bool success);
    }

    public class PhoneNumberPoolService : IPhoneNumberPoolService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PhoneNumberPoolService> _logger;
        private static int _lastUsedIndex = -1;
        private static readonly object _indexLock = new object();

        public PhoneNumberPoolService(IServiceScopeFactory scopeFactory, ILogger<PhoneNumberPoolService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<TwilioPhoneNumber?> GetNextAvailableNumberAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var allNumbers = await context.TwilioPhoneNumbers.ToListAsync();
            _logger.LogInformation($"Phone pool status - Total numbers: {allNumbers.Count}, " +
                $"Active: {allNumbers.Count(n => n.IsActive)}, " +
                $"Inactive: {allNumbers.Count(n => !n.IsActive)}");
            
            var activeNumbers = allNumbers
                .Where(n => n.IsActive)
                .OrderBy(n => n.Id)
                .ToList();

            if (!activeNumbers.Any())
            {
                _logger.LogWarning($"No active phone numbers in pool");
                return null;
            }

            // Simple round-robin selection without capacity checks
            // Let Twilio handle queueing multiple calls per number
            int currentIndex;
            lock (_indexLock)
            {
                _lastUsedIndex = (_lastUsedIndex + 1) % activeNumbers.Count;
                currentIndex = _lastUsedIndex;
            }

            var selectedNumber = activeNumbers[currentIndex];
            
            // Update usage tracking
            selectedNumber.LastUsedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            _logger.LogInformation($"Allocated phone number {selectedNumber.Number} (ID: {selectedNumber.Id}) - Round robin index: {currentIndex}");
            
            return selectedNumber;
        }

        public async Task ReleaseNumberAsync(int phoneNumberId)
        {
            // This method is now a no-op since we don't track active calls
            // Kept for backward compatibility
            _logger.LogDebug($"ReleaseNumberAsync called for phone number ID: {phoneNumberId} (no-op)");
        }

        public async Task<List<TwilioPhoneNumber>> GetAllNumbersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            return await context.TwilioPhoneNumbers
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TwilioPhoneNumber>> AddPhoneNumbersAsync(string phoneNumbers)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var addedNumbers = new List<TwilioPhoneNumber>();
            var numberList = phoneNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct();

            foreach (var phoneNumber in numberList)
            {
                // Check if number already exists
                var exists = await context.TwilioPhoneNumbers
                    .AnyAsync(n => n.Number == phoneNumber);
                
                if (!exists)
                {
                    // Ensure phone number is properly formatted
                    var formattedNumber = FormatPhoneNumber(phoneNumber);
                    
                    var twilioNumber = new TwilioPhoneNumber
                    {
                        Number = formattedNumber,
                        IsActive = true,
                        MaxConcurrentCalls = 50, // Default to 50 concurrent calls per Twilio limits
                        CreatedAt = DateTime.UtcNow
                    };

                    context.TwilioPhoneNumbers.Add(twilioNumber);
                    addedNumbers.Add(twilioNumber);
                    _logger.LogInformation($"Added new phone number to pool: {phoneNumber}");
                }
                else
                {
                    _logger.LogWarning($"Phone number {phoneNumber} already exists in pool");
                }
            }
            
            await context.SaveChangesAsync();
            return addedNumbers;
        }

        public async Task<bool> RemovePhoneNumberAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var number = await context.TwilioPhoneNumbers.FindAsync(id);
            if (number == null)
                return false;

            context.TwilioPhoneNumbers.Remove(number);
            await context.SaveChangesAsync();
            
            _logger.LogInformation($"Removed phone number from pool: {number.Number}");
            return true;
        }

        public async Task<bool> UpdatePhoneNumberAsync(int id, bool isActive, int maxConcurrentCalls)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var number = await context.TwilioPhoneNumbers.FindAsync(id);
            if (number == null)
                return false;

            number.IsActive = isActive;
            number.MaxConcurrentCalls = maxConcurrentCalls;
            await context.SaveChangesAsync();
            
            _logger.LogInformation($"Updated phone number {number.Number}: IsActive={isActive}, MaxConcurrentCalls={maxConcurrentCalls}");
            return true;
        }

        public async Task IncrementCallCountAsync(int phoneNumberId, bool success)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var number = await context.TwilioPhoneNumbers.FindAsync(phoneNumberId);
            if (number != null)
            {
                var previousTotal = number.TotalCallsMade;
                var previousFailed = number.TotalCallsFailed;
                
                number.TotalCallsMade++;
                if (!success)
                {
                    number.TotalCallsFailed++;
                }
                await context.SaveChangesAsync();
                
                _logger.LogInformation($"Updated call stats for {number.Number} (ID: {phoneNumberId}). " +
                    $"Total calls: {previousTotal} -> {number.TotalCallsMade}. " +
                    $"Failed calls: {previousFailed} -> {number.TotalCallsFailed}. " +
                    $"Success rate: {((number.TotalCallsMade - number.TotalCallsFailed) / (double)number.TotalCallsMade * 100):F1}%");
            }
            else
            {
                _logger.LogWarning($"Attempted to increment call count for non-existent phone number ID: {phoneNumberId}");
            }
        }
        
        private string FormatPhoneNumber(string phoneNumber)
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
    }
}