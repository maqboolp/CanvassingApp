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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Login tracking fields
        public int LoginCount { get; set; } = 0;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastActivity { get; set; }
        
        // Password management
        public bool ForcePasswordChange { get; set; } = false;
        
        public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    }

    public enum VolunteerRole
    {
        Volunteer,
        Admin,
        SuperAdmin
    }
}