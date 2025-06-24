using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class VolunteerResource
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string ResourceType { get; set; } = string.Empty; // "QuickTips" or "Script"
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(450)]
        public string? LastUpdatedBy { get; set; } // User ID who last updated
    }
}