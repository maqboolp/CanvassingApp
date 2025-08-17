namespace HooverCanvassingApi.Models
{
    public class PhoneBankingCall
    {
        public string Id { get; set; } = "";
        public string UserId { get; set; } = "";
        public string VoterId { get; set; } = "";
        public string VolunteerPhone { get; set; } = "";
        public string VoterPhone { get; set; } = "";
        public string TwimlContent { get; set; } = "";
        public string? TwilioCallSid { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        
        // Navigation properties
        public virtual Volunteer? User { get; set; }
        public virtual Voter? Voter { get; set; }
    }
}