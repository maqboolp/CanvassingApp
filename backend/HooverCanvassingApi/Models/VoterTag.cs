using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class VoterTag
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string TagName { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        [MaxLength(7)] // For hex color codes like #FF5733
        public string? Color { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(450)]
        public string? CreatedById { get; set; }
        
        // Navigation properties
        public virtual Volunteer? CreatedBy { get; set; }
        public virtual ICollection<VoterTagAssignment> VoterAssignments { get; set; } = new List<VoterTagAssignment>();
    }
}