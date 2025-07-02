using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HooverCanvassingApi.Models
{
    public class VoiceRecording
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public string Url { get; set; } = string.Empty;
        
        public string? FileName { get; set; }
        
        public long? FileSizeBytes { get; set; }
        
        public int? DurationSeconds { get; set; }
        
        [Required]
        public string CreatedById { get; set; } = string.Empty;
        
        public Volunteer? CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? LastUsedAt { get; set; }
        
        public int UsageCount { get; set; }
        
        // Navigation property for campaigns using this recording
        public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    }
}