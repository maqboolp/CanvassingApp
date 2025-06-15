using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class InvitationToken
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string Email { get; set; } = string.Empty;
        
        public VolunteerRole Role { get; set; } = VolunteerRole.Volunteer;
        
        public string Token { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7); // 7 day expiration
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime? UsedAt { get; set; }
        
        public string? CreatedByUserId { get; set; }
        
        public string? CompletedByUserId { get; set; }
        
        // Navigation properties
        public Volunteer? CreatedBy { get; set; }
        public Volunteer? CompletedBy { get; set; }
    }

    public class PendingVolunteer
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string FirstName { get; set; } = string.Empty;
        
        public string LastName { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public string PhoneNumber { get; set; } = string.Empty;
        
        public string HashedPassword { get; set; } = string.Empty;
        
        public VolunteerRole RequestedRole { get; set; } = VolunteerRole.Volunteer;
        
        public PendingVolunteerStatus Status { get; set; } = PendingVolunteerStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReviewedAt { get; set; }
        
        public string? ReviewedByUserId { get; set; }
        
        public string? ReviewNotes { get; set; }
        
        // Navigation property
        public Volunteer? ReviewedBy { get; set; }
    }

    public enum PendingVolunteerStatus
    {
        Pending,
        Approved,
        Rejected
    }
}