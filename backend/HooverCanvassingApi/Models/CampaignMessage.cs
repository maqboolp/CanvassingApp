using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class CampaignMessage
    {
        [Key]
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public string VoterId { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string? TwilioSid { get; set; }
        public MessageStatus Status { get; set; } = MessageStatus.Pending;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public decimal? Cost { get; set; }
        
        // Call specific fields
        public int? CallDuration { get; set; } // in seconds
        public string? CallStatus { get; set; }
        public string? RecordingUrl { get; set; }
        
        public Campaign Campaign { get; set; } = null!;
        public Voter Voter { get; set; } = null!;
    }

    public enum MessageStatus
    {
        Pending,
        Queued,
        Sending,
        Sent,
        Delivered,
        Failed,
        Undelivered,
        Busy,
        NoAnswer,
        Completed,
        Cancelled
    }
}