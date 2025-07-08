using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class ResourceLink
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [Url]
        public string Url { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public ResourceCategory Category { get; set; }
        
        public int DisplayOrder { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public string? CreatedByUserId { get; set; }
        
        public string? UpdatedByUserId { get; set; }
        
        // Navigation properties
        public Volunteer? CreatedBy { get; set; }
        public Volunteer? UpdatedBy { get; set; }
    }
    
    public enum ResourceCategory
    {
        VoterResources,
        CampaignInformation,
        VolunteerResources,
        GeneralResources
    }
}