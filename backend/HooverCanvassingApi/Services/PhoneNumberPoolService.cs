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
        Task<TwilioPhoneNumber> AddPhoneNumberAsync(string phoneNumber, string? friendlyName = null);
        Task<bool> RemovePhoneNumberAsync(int id);
        Task<bool> UpdatePhoneNumberAsync(int id, bool isActive, int maxConcurrentCalls);
        Task IncrementCallCountAsync(int phoneNumberId, bool success);
    }

    public class PhoneNumberPoolService : IPhoneNumberPoolService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PhoneNumberPoolService> _logger;
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _numberSemaphores = new();
        private int _lastUsedIndex = -1;
        private readonly object _indexLock = new object();

        public PhoneNumberPoolService(ApplicationDbContext context, ILogger<PhoneNumberPoolService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TwilioPhoneNumber?> GetNextAvailableNumberAsync()
        {
            var activeNumbers = await _context.TwilioPhoneNumbers
                .Where(n => n.IsActive && n.CurrentActiveCalls < n.MaxConcurrentCalls)
                .OrderBy(n => n.Id)
                .ToListAsync();

            if (!activeNumbers.Any())
            {
                _logger.LogWarning("No available phone numbers in pool");
                return null;
            }

            // Round-robin selection
            lock (_indexLock)
            {
                _lastUsedIndex = (_lastUsedIndex + 1) % activeNumbers.Count;
            }

            var selectedNumber = activeNumbers[_lastUsedIndex];
            
            // Try to acquire the number
            var semaphore = _numberSemaphores.GetOrAdd(selectedNumber.Id, _ => new SemaphoreSlim(selectedNumber.MaxConcurrentCalls));
            
            if (await semaphore.WaitAsync(0)) // Non-blocking check
            {
                // Update active call count
                selectedNumber.CurrentActiveCalls++;
                selectedNumber.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Allocated phone number {selectedNumber.PhoneNumber} (ID: {selectedNumber.Id})");
                return selectedNumber;
            }

            // If the selected number is at capacity, try others
            foreach (var number in activeNumbers.Where(n => n.Id != selectedNumber.Id))
            {
                semaphore = _numberSemaphores.GetOrAdd(number.Id, _ => new SemaphoreSlim(number.MaxConcurrentCalls));
                
                if (await semaphore.WaitAsync(0))
                {
                    number.CurrentActiveCalls++;
                    number.LastUsedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Allocated phone number {number.PhoneNumber} (ID: {number.Id})");
                    return number;
                }
            }

            _logger.LogWarning("All phone numbers are at capacity");
            return null;
        }

        public async Task ReleaseNumberAsync(int phoneNumberId)
        {
            try
            {
                var number = await _context.TwilioPhoneNumbers.FindAsync(phoneNumberId);
                if (number != null)
                {
                    number.CurrentActiveCalls = Math.Max(0, number.CurrentActiveCalls - 1);
                    await _context.SaveChangesAsync();
                    
                    if (_numberSemaphores.TryGetValue(phoneNumberId, out var semaphore))
                    {
                        semaphore.Release();
                    }
                    
                    _logger.LogInformation($"Released phone number {number.PhoneNumber} (ID: {phoneNumberId})");
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

        public async Task<TwilioPhoneNumber> AddPhoneNumberAsync(string phoneNumber, string? friendlyName = null)
        {
            var twilioNumber = new TwilioPhoneNumber
            {
                PhoneNumber = phoneNumber,
                FriendlyName = friendlyName ?? phoneNumber,
                IsActive = true,
                MaxConcurrentCalls = 1, // Default to 1 concurrent call
                CreatedAt = DateTime.UtcNow
            };

            _context.TwilioPhoneNumbers.Add(twilioNumber);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Added new phone number to pool: {phoneNumber}");
            return twilioNumber;
        }

        public async Task<bool> RemovePhoneNumberAsync(int id)
        {
            var number = await _context.TwilioPhoneNumbers.FindAsync(id);
            if (number == null)
                return false;

            _context.TwilioPhoneNumbers.Remove(number);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Removed phone number from pool: {number.PhoneNumber}");
            return true;
        }

        public async Task<bool> UpdatePhoneNumberAsync(int id, bool isActive, int maxConcurrentCalls)
        {
            var number = await _context.TwilioPhoneNumbers.FindAsync(id);
            if (number == null)
                return false;

            number.IsActive = isActive;
            number.MaxConcurrentCalls = maxConcurrentCalls;
            
            // Update semaphore if it exists
            if (_numberSemaphores.TryGetValue(id, out var oldSemaphore))
            {
                _numberSemaphores.TryRemove(id, out _);
                oldSemaphore.Dispose();
                
                if (isActive)
                {
                    _numberSemaphores.TryAdd(id, new SemaphoreSlim(maxConcurrentCalls));
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Updated phone number {number.PhoneNumber}: Active={isActive}, MaxConcurrent={maxConcurrentCalls}");
            return true;
        }

        public async Task IncrementCallCountAsync(int phoneNumberId, bool success)
        {
            var number = await _context.TwilioPhoneNumbers.FindAsync(phoneNumberId);
            if (number != null)
            {
                number.TotalCallsMade++;
                if (!success)
                {
                    number.TotalCallsFailed++;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}