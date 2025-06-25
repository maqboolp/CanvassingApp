using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class ConsentRecord
    {
        [Key]
        public int Id { get; set; }
        
        public string VoterId { get; set; } = string.Empty;
        public Voter Voter { get; set; } = null!;
        
        public ConsentAction Action { get; set; }
        public ConsentMethod Method { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Source { get; set; } // IP address for web, phone number for SMS
        public string? Details { get; set; } // Additional details about the consent action
        public string? RawMessage { get; set; } // Original SMS message if applicable
        
        // For web opt-ins, track additional compliance data
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? FormUrl { get; set; }
        public bool ConsentLanguageShown { get; set; } = true;
        public string? ConsentLanguage { get; set; } // The exact consent text shown to user
    }
    
    public enum ConsentAction
    {
        OptIn,
        OptOut,
        OptInReminder,  // Sent opt-in invitation
        InvalidRequest  // Invalid opt-in/out request
    }
}