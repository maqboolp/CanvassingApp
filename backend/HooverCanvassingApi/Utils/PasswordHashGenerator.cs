using Microsoft.AspNetCore.Identity;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Utils
{
    /// <summary>
    /// Utility class to generate password hashes for default admin accounts
    /// This should be run once to generate the hashes for the SQL script
    /// </summary>
    public class PasswordHashGenerator
    {
        public static void GenerateDefaultPasswordHashes()
        {
            var hasher = new PasswordHasher<Volunteer>();
            var dummyUser = new Volunteer { Email = "dummy@example.com" };
            
            // Generate hash for SuperAdmin password
            var superAdminHash = hasher.HashPassword(dummyUser, "SuperAdmin123!");
            Console.WriteLine($"SuperAdmin Password Hash: {superAdminHash}");
            
            // Generate hash for Admin password
            var adminHash = hasher.HashPassword(dummyUser, "Admin123!");
            Console.WriteLine($"Admin Password Hash: {adminHash}");
            
            // These hashes should be used in the CONSOLIDATED_SCHEMA.sql file
            // Replace the placeholder hashes with these actual values
        }
        
        /// <summary>
        /// Call this method from Program.cs if you need to regenerate hashes
        /// Example: PasswordHashGenerator.GenerateDefaultPasswordHashes();
        /// </summary>
        public static (string SuperAdminHash, string AdminHash) GetDefaultPasswordHashes()
        {
            var hasher = new PasswordHasher<Volunteer>();
            var dummyUser = new Volunteer { Email = "dummy@example.com" };
            
            var superAdminHash = hasher.HashPassword(dummyUser, "SuperAdmin123!");
            var adminHash = hasher.HashPassword(dummyUser, "Admin123!");
            
            return (superAdminHash, adminHash);
        }
    }
}