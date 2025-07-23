using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class PhoneContact
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string VoterId { get; set; } = string.Empty;
        public string VolunteerId { get; set; } = string.Empty;
        public PhoneContactStatus Status { get; set; }
        public VoterSupport? VoterSupport { get; set; }
        public string? Notes { get; set; }
        public string? AudioFileUrl { get; set; }
        public int? AudioDurationSeconds { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int? CallDurationSeconds { get; set; }
        public string? PhoneNumberUsed { get; set; } // Track which phone number was used for the call
        
        public Voter Voter { get; set; } = null!;
        public Volunteer Volunteer { get; set; } = null!;
    }

    public enum PhoneContactStatus
    {
        Reached,          // Answered and spoke with voter
        NoAnswer,         // No answer after multiple attempts
        VoiceMail,        // Left a voicemail
        WrongNumber,      // Wrong number
        Disconnected,     // Number disconnected
        Refused,          // Refused to talk
        Callback,         // Requested callback later
        DoNotCall         // Added to do not call list
    }
}