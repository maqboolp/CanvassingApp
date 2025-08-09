using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class OptOutRecord
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        public OptOutType Type { get; set; }
        
        public DateTime OptedOutAt { get; set; }
        
        public string? Reason { get; set; }
        
        // Track how they opted out
        public OptOutMethod Method { get; set; }
        
        // Optional: Link to voter if we can match
        public string? VoterId { get; set; }
        public Voter? Voter { get; set; }
    }
    
    public enum OptOutType
    {
        All,        // Opt out of everything
        RoboCalls,  // Only opt out of robocalls
        SMS,        // Only opt out of SMS
    }
    
    public enum OptOutMethod
    {
        SMS,        // Via SMS STOP command
        Phone,      // Via phone call
        Web,        // Via web form
        Manual      // Manually added by admin
    }
}