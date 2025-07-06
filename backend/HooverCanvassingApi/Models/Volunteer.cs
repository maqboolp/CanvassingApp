using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HooverCanvassingApi.Models
{
    public class Volunteer : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public VolunteerRole Role { get; set; } = VolunteerRole.Volunteer;
        public bool IsActive { get; set; } = true;
        public bool IsSystemUser { get; set; } = false; // God-level admin that cannot be deleted
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Login tracking fields
        public int LoginCount { get; set; } = 0;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastActivity { get; set; }
        
        public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    }

    public enum VolunteerRole
    {
        Volunteer,
        Admin,
        SuperAdmin
    }
}