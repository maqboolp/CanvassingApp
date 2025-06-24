using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class VoterTagAssignment
    {
        [Required]
        public string VoterId { get; set; } = string.Empty;
        
        [Required]
        public int TagId { get; set; }
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(450)]
        public string? AssignedById { get; set; }
        
        // Navigation properties
        public virtual Voter Voter { get; set; } = null!;
        public virtual VoterTag Tag { get; set; } = null!;
        public virtual Volunteer? AssignedBy { get; set; }
    }
}