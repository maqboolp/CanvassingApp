using Microsoft.AspNetCore.Identity;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        
        var userManager = services.GetRequiredService<UserManager<Volunteer>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Create roles if they don't exist
        var roles = new[] { "Admin", "Volunteer", "SuperAdmin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create default super admin user if no users exist
        if (!userManager.Users.Any())
        {
            var superAdminUser = new Volunteer
            {
                UserName = "superadmin@tanveer4hoover.com",
                Email = "superadmin@tanveer4hoover.com",
                FirstName = "Super",
                LastName = "Admin",
                Role = VolunteerRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(superAdminUser, "SuperAdmin123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
                Console.WriteLine("Default super admin user created:");
                Console.WriteLine($"Email: superadmin@tanveer4hoover.com");
                Console.WriteLine($"Password: SuperAdmin123");
                Console.WriteLine("Please change this password after first login!");
            }
            else
            {
                Console.WriteLine($"Failed to create super admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Also create a regular admin user
            var adminUser = new Volunteer
            {
                UserName = "admin@tanveer4hoover.com",
                Email = "admin@tanveer4hoover.com",
                FirstName = "Admin",
                LastName = "User",
                Role = VolunteerRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var adminResult = await userManager.CreateAsync(adminUser, "Admin123");
            if (adminResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Default admin user created:");
                Console.WriteLine($"Email: admin@tanveer4hoover.com");
                Console.WriteLine($"Password: Admin123");
            }
            else
            {
                Console.WriteLine($"Failed to create admin user: {string.Join(", ", adminResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}