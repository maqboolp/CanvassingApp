using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    /// <summary>
    /// Represents a canvassing walk session by a volunteer
    /// </summary>
    public class WalkSession
    {
        public int Id { get; set; }
        
        // Volunteer doing the walking
        public string VolunteerId { get; set; } = string.Empty;
        public virtual Volunteer Volunteer { get; set; } = null!;
        
        // Session timing
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        
        // Current status
        public WalkSessionStatus Status { get; set; } = WalkSessionStatus.Active;
        
        // Statistics
        public int HousesVisited { get; set; }
        public int VotersContacted { get; set; }
        public double TotalDistanceMeters { get; set; }
        public int DurationMinutes { get; set; }
        
        // Starting location (optional)
        public double? StartLatitude { get; set; }
        public double? StartLongitude { get; set; }
        
        // Navigation properties
        public virtual ICollection<HouseClaim> HouseClaims { get; set; } = new List<HouseClaim>();
        public virtual ICollection<WalkActivity> Activities { get; set; } = new List<WalkActivity>();
    }
    
    public enum WalkSessionStatus
    {
        Active,
        Paused,
        Completed,
        Abandoned
    }
}