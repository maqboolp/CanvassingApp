using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class Contact
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string VoterId { get; set; } = string.Empty;
        public string VolunteerId { get; set; } = string.Empty;
        public ContactStatus Status { get; set; }
        public VoterSupport? VoterSupport { get; set; }
        public string? Notes { get; set; }
        public string? AudioFileUrl { get; set; }
        public int? AudioDurationSeconds { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double? LocationLatitude { get; set; }
        public double? LocationLongitude { get; set; }
        
        public Voter Voter { get; set; } = null!;
        public Volunteer Volunteer { get; set; } = null!;
    }
}