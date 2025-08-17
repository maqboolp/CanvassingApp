using System;
using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class VoterLock
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string VoterId { get; set; } = "";
        
        [Required]
        public string UserId { get; set; } = "";
        
        public string UserName { get; set; } = "";
        
        public DateTime LockedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5); // Auto-expire after 5 minutes
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual Voter? Voter { get; set; }
        public virtual Volunteer? User { get; set; }
    }
}