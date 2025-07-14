using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class TwilioPhoneNumber
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Number { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public bool IsMainNumber { get; set; } = false;
        
        public int MaxConcurrentCalls { get; set; } = 1;
        
        public int CurrentActiveCalls { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastUsedAt { get; set; }
        
        // Track usage statistics
        public int TotalCallsMade { get; set; } = 0;
        
        public int TotalCallsFailed { get; set; } = 0;
        
        // For rate limiting
        public DateTime? RateLimitResetAt { get; set; }
        
        public string? Notes { get; set; }
    }
}