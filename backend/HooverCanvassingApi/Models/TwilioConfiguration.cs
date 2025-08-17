using System;
using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class TwilioConfiguration
    {
        [Key]
        public int Id { get; set; }
        
        [MaxLength(100)]
        public string AccountSid { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string AuthToken { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? FromPhoneNumber { get; set; }
        
        [MaxLength(20)]
        public string? SmsPhoneNumber { get; set; }
        
        [MaxLength(100)]
        public string? MessagingServiceSid { get; set; }
        
        [MaxLength(100)]
        public string? AppSid { get; set; }
        
        [MaxLength(100)]
        public string? ApiKeySid { get; set; }
        
        [MaxLength(100)]
        public string? ApiKeySecret { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Additional fields for future use
        [MaxLength(50)]
        public string? Region { get; set; } = "US";
        
        public bool IsActive { get; set; } = true;
    }
}