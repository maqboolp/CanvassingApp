using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PhoneNumberPoolService> _logger;
        private int _lastUsedIndex = -1;
        private readonly object _indexLock = new object();

        public PhoneNumberPoolService(ApplicationDbContext context, ILogger<PhoneNumberPoolService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TwilioPhoneNumber?> GetNextAvailableNumberAsync()
        {
            var allNumbers = await _context.TwilioPhoneNumbers.ToListAsync();
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
            lock (_indexLock)
            {
                _lastUsedIndex = (_lastUsedIndex + 1) % activeNumbers.Count;
            }

            var selectedNumber = activeNumbers[_lastUsedIndex];
            
            // Update usage tracking
            selectedNumber.CurrentActiveCalls++;
            selectedNumber.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Allocated phone number {selectedNumber.Number} (ID: {selectedNumber.Id}) - Round robin index: {_lastUsedIndex}");
            
            return selectedNumber;
        }

        public async Task ReleaseNumberAsync(int phoneNumberId)
        {
            try
            {
                var number = await _context.TwilioPhoneNumbers.FindAsync(phoneNumberId);
                if (number != null)
                {
                    var previousCalls = number.CurrentActiveCalls;
                    number.CurrentActiveCalls = Math.Max(0, number.CurrentActiveCalls - 1);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Released phone number {number.Number} (ID: {phoneNumberId}). " +
                        $"Active calls: {previousCalls} -> {number.CurrentActiveCalls}");
                }
                else
                {
                    _logger.LogWarning($"Attempted to release non-existent phone number ID: {phoneNumberId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error releasing phone number {phoneNumberId}");
            }
        }

        public async Task<List<TwilioPhoneNumber>> GetAllNumbersAsync()
        {
            return await _context.TwilioPhoneNumbers
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TwilioPhoneNumber>> AddPhoneNumbersAsync(string phoneNumbers)
        {
            var addedNumbers = new List<TwilioPhoneNumber>();
            var numberList = phoneNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct();

            foreach (var phoneNumber in numberList)
            {
                // Check if number already exists
                var exists = await _context.TwilioPhoneNumbers
                    .AnyAsync(n => n.Number == phoneNumber);
                
                if (!exists)
                {
                    var twilioNumber = new TwilioPhoneNumber
                    {
                        Number = phoneNumber,
                        IsActive = true,
                        MaxConcurrentCalls = 50, // Default to 50 concurrent calls per Twilio limits
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.TwilioPhoneNumbers.Add(twilioNumber);
                    addedNumbers.Add(twilioNumber);
                    _logger.LogInformation($"Added new phone number to pool: {phoneNumber}");
                }
                else
                {
                    _logger.LogWarning($"Phone number {phoneNumber} already exists in pool");
                }
            }
            
            await _context.SaveChangesAsync();
            return addedNumbers;
        }

        public async Task<bool> RemovePhoneNumberAsync(int id)
        {
            var number = await _context.TwilioPhoneNumbers.FindAsync(id);
            if (number == null)
                return false;

            _context.TwilioPhoneNumbers.Remove(number);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Removed phone number from pool: {number.Number}");
            return true;
        }

        public async Task<bool> UpdatePhoneNumberAsync(int id, bool isActive, int maxConcurrentCalls)
        {
            var number = await _context.TwilioPhoneNumbers.FindAsync(id);
            if (number == null)
                return false;

            number.IsActive = isActive;
            number.MaxConcurrentCalls = maxConcurrentCalls;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Updated phone number {number.Number}: Active={isActive}, MaxConcurrent={maxConcurrentCalls}");
            return true;
        }

        public async Task IncrementCallCountAsync(int phoneNumberId, bool success)
        {
            var number = await _context.TwilioPhoneNumbers.FindAsync(phoneNumberId);
            if (number != null)
            {
                var previousTotal = number.TotalCallsMade;
                var previousFailed = number.TotalCallsFailed;
                
                number.TotalCallsMade++;
                if (!success)
                {
                    number.TotalCallsFailed++;
                }
                await _context.SaveChangesAsync();
                
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
    }
}