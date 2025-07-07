using System;
using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    /// <summary>
    /// Represents a claim on a house to prevent duplicate visits
    /// </summary>
    public class HouseClaim
    {
        public int Id { get; set; }
        
        // Walk session this claim belongs to
        public int WalkSessionId { get; set; }
        public virtual WalkSession WalkSession { get; set; } = null!;
        
        // House information (denormalized for performance)
        [Required]
        [MaxLength(200)]
        public string Address { get; set; } = string.Empty;
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        // Claim timing
        public DateTime ClaimedAt { get; set; }
        public DateTime ExpiresAt { get; set; } // Auto-expires if not visited
        
        // Visit status
        public ClaimStatus Status { get; set; } = ClaimStatus.Claimed;
        public DateTime? VisitedAt { get; set; }
        
        // Results
        public int VotersContacted { get; set; }
        public int VotersHome { get; set; }
        
        // Link to actual contact records created
        public string? ContactIds { get; set; } // Comma-separated contact IDs
    }
    
    public enum ClaimStatus
    {
        Claimed,      // Reserved by canvasser
        Visiting,     // Canvasser is at the house
        Visited,      // Successfully visited
        Expired,      // Claim expired without visit
        Released      // Manually released by canvasser
    }
}