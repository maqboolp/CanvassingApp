using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace HooverCanvassingApi.Services
{
    public interface IPhoneNumberPoolService
    {
        Task<AdditionalPhoneNumber?> GetNextAvailableNumberAsync();
        Task ReleaseNumberAsync(int phoneNumberId);
        Task<List<AdditionalPhoneNumber>> GetAllNumbersAsync();
        Task<AdditionalPhoneNumber> AddPhoneNumberAsync(string phoneNumber, string? friendlyName = null);
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

        public async Task<AdditionalPhoneNumber?> GetNextAvailableNumberAsync()
        {
            var allNumbers = await _context.AdditionalPhoneNumbers.ToListAsync();
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
            
            _logger.LogInformation($"Allocated phone number {selectedNumber.PhoneNumber} (ID: {selectedNumber.Id}) - Round robin index: {_lastUsedIndex}");
            
            return selectedNumber;
        }

        public async Task ReleaseNumberAsync(int phoneNumberId)
        {
            try
            {
                var number = await _context.AdditionalPhoneNumbers.FindAsync(phoneNumberId);
                if (number != null)
                {
                    var previousCalls = number.CurrentActiveCalls;
                    number.CurrentActiveCalls = Math.Max(0, number.CurrentActiveCalls - 1);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Released phone number {number.PhoneNumber} (ID: {phoneNumberId}). " +
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

        public async Task<List<AdditionalPhoneNumber>> GetAllNumbersAsync()
        {
            return await _context.AdditionalPhoneNumbers
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<AdditionalPhoneNumber> AddPhoneNumberAsync(string phoneNumber, string? friendlyName = null)
        {
            var twilioNumber = new AdditionalPhoneNumber
            {
                PhoneNumber = phoneNumber,
                FriendlyName = friendlyName ?? phoneNumber,
                IsActive = true,
                MaxConcurrentCalls = 1, // Default to 1 concurrent call
                CreatedAt = DateTime.UtcNow
            };

            _context.AdditionalPhoneNumbers.Add(twilioNumber);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Added new phone number to pool: {phoneNumber}");
            return twilioNumber;
        }

        public async Task<bool> RemovePhoneNumberAsync(int id)
        {
            var number = await _context.AdditionalPhoneNumbers.FindAsync(id);
            if (number == null)
                return false;

            _context.AdditionalPhoneNumbers.Remove(number);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Removed phone number from pool: {number.PhoneNumber}");
            return true;
        }

        public async Task<bool> UpdatePhoneNumberAsync(int id, bool isActive, int maxConcurrentCalls)
        {
            var number = await _context.AdditionalPhoneNumbers.FindAsync(id);
            if (number == null)
                return false;

            number.IsActive = isActive;
            number.MaxConcurrentCalls = maxConcurrentCalls;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Updated phone number {number.PhoneNumber}: Active={isActive}, MaxConcurrent={maxConcurrentCalls}");
            return true;
        }

        public async Task IncrementCallCountAsync(int phoneNumberId, bool success)
        {
            var number = await _context.AdditionalPhoneNumbers.FindAsync(phoneNumberId);
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
                
                _logger.LogInformation($"Updated call stats for {number.PhoneNumber} (ID: {phoneNumberId}). " +
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