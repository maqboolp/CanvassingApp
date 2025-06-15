using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class PendingVolunteer
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        public string HashedPassword { get; set; } = string.Empty;
        
        [Required]
        public VolunteerRole RequestedRole { get; set; } = VolunteerRole.Volunteer;
        
        [Required]
        public PendingVolunteerStatus Status { get; set; } = PendingVolunteerStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReviewedAt { get; set; }
        
        public string? ReviewedByUserId { get; set; }
        
        public string? ReviewNotes { get; set; }
        
        // Navigation properties
        public Volunteer? ReviewedBy { get; set; }
    }

    public enum PendingVolunteerStatus
    {
        Pending,
        Approved,
        Rejected
    }
}