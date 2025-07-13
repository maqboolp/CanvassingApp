using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class Campaign
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public CampaignType Type { get; set; }
        public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
        public DateTime? ScheduledTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string? VoiceUrl { get; set; } // For robo calls
        public string? RecordingUrl { get; set; } // For robo calls
        
        // Voice recording reference for RoboCall campaigns
        public int? VoiceRecordingId { get; set; }
        public VoiceRecording? VoiceRecording { get; set; }
        
        // Filtering criteria
        public string? FilterZipCodes { get; set; } // JSON array of zip codes
        public VoteFrequency? FilterVoteFrequency { get; set; }
        public int? FilterMinAge { get; set; }
        public int? FilterMaxAge { get; set; }
        public VoterSupport? FilterVoterSupport { get; set; }
        public string? FilterTags { get; set; } // JSON array of tag IDs
        
        // Campaign stats
        public int TotalRecipients { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public int PendingDeliveries { get; set; }
        
        // Calling hours settings (for RoboCall campaigns)
        public bool EnforceCallingHours { get; set; } = true;
        public int StartHour { get; set; } = 9;  // 9 AM
        public int EndHour { get; set; } = 20;    // 8 PM
        public bool IncludeWeekends { get; set; } = false;
        
        // Duplicate message prevention
        public bool PreventDuplicateMessages { get; set; } = false;
        
        public ICollection<CampaignMessage> Messages { get; set; } = new List<CampaignMessage>();
    }

    public enum CampaignType
    {
        SMS,
        RoboCall
    }

    public enum CampaignStatus
    {
        Draft,
        Scheduled,
        Sending,
        Completed,
        Failed,
        Cancelled,
        Sealed  // Campaign is fully completed and cannot be modified
    }
}