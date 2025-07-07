using System;
using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    /// <summary>
    /// Tracks activities during a walk session
    /// </summary>
    public class WalkActivity
    {
        public int Id { get; set; }
        
        // Walk session this activity belongs to
        public int WalkSessionId { get; set; }
        public virtual WalkSession WalkSession { get; set; } = null!;
        
        // Activity type
        public WalkActivityType ActivityType { get; set; }
        
        // Location where activity occurred
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        // Optional reference to a house claim
        public int? HouseClaimId { get; set; }
        public virtual HouseClaim? HouseClaim { get; set; }
        
        // Additional context
        [MaxLength(500)]
        public string? Description { get; set; }
        
        // JSON data for activity-specific information
        public string? Data { get; set; }
        
        // When it happened
        public DateTime Timestamp { get; set; }
    }
    
    public enum WalkActivityType
    {
        SessionStarted,
        RouteGenerated,
        HouseClaimed,
        HouseReleased,
        ArrivedAtHouse,
        DepartedHouse,
        ContactMade,
        SessionPaused,
        SessionResumed,
        SessionEnded
    }
}