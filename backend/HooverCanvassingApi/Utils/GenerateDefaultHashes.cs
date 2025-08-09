using Microsoft.AspNetCore.Identity;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Utils
{
    /// <summary>
    /// Generates password hashes for default admin accounts
    /// Run this in Program.cs to get the hashes for SQL scripts
    /// </summary>
    public static class GenerateDefaultHashes
    {
        public static void GenerateAndPrintHashes()
        {
            var hasher = new PasswordHasher<Volunteer>();
            var dummyUser = new Volunteer { Email = "dummy@example.com" };
            
            // Generate hash for default SuperAdmin password
            var superAdminHash = hasher.HashPassword(dummyUser, "SuperAdmin123!");
            
            // Generate hash for default Admin password  
            var adminHash = hasher.HashPassword(dummyUser, "Admin123!");
            
            Console.WriteLine("-- Password hashes for default accounts:");
            Console.WriteLine("-- SuperAdmin (SuperAdmin123!):");
            Console.WriteLine($"-- {superAdminHash}");
            Console.WriteLine();
            Console.WriteLine("-- Admin (Admin123!):");
            Console.WriteLine($"-- {adminHash}");
        }
    }
}