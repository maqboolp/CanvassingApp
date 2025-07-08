using Microsoft.AspNetCore.Identity;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HooverCanvassingApi;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        
        var userManager = services.GetRequiredService<UserManager<Volunteer>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        Console.WriteLine("Initializing seed data...");

        // Create roles if they don't exist
        var roles = new[] { "Admin", "Volunteer", "SuperAdmin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Console.WriteLine($"Created role: {role}");
            }
        }

        // Create default super admin user if no users exist
        if (!userManager.Users.Any())
        {
            Console.WriteLine("No users found, creating default admin users...");
            var superAdminUser = new Volunteer
            {
                UserName = "superadmin@campaign.local",
                Email = "superadmin@campaign.local",
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
                Console.WriteLine($"Email: superadmin@campaign.local");
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
                UserName = "admin@campaign.local",
                Email = "admin@campaign.local",
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
                Console.WriteLine($"Email: admin@campaign.local");
                Console.WriteLine($"Password: Admin123");
            }
            else
            {
                Console.WriteLine($"Failed to create admin user: {string.Join(", ", adminResult.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine("Users already exist, skipping user creation.");
        }

        // Create sample voter data if no voters exist
        if (!await context.Voters.AnyAsync())
        {
            Console.WriteLine("No voters found, creating sample voter data...");
            var sampleVoters = new List<Voter>
            {
                // Birmingham area ZIP codes
                new Voter
                {
                    LalVoterId = "12345001",
                    FirstName = "John",
                    LastName = "Smith",
                    AddressLine = "123 Main St",
                    City = "Birmingham",
                    State = "AL",
                    Zip = "35244",
                    Age = 44,
                    Gender = "M",
                    CellPhone = "205-555-0101",
                    VoteFrequency = VoteFrequency.Frequent,
                    VoterSupport = VoterSupport.StrongYes
                },
                new Voter
                {
                    LalVoterId = "12345002",
                    FirstName = "Mary",
                    LastName = "Johnson",
                    AddressLine = "456 Oak Ave",
                    City = "Birmingham",
                    State = "AL",
                    Zip = "35244",
                    Age = 49,
                    Gender = "F",
                    CellPhone = "205-555-0102",
                    VoteFrequency = VoteFrequency.Infrequent,
                    VoterSupport = VoterSupport.LeaningYes
                },
                new Voter
                {
                    LalVoterId = "12345003",
                    FirstName = "Robert",
                    LastName = "Williams",
                    AddressLine = "789 Pine St",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35216",
                    CellPhone = "205-555-0103",
                    VoteFrequency = VoteFrequency.Frequent,
                    VoterSupport = VoterSupport.Undecided,
                    Age = 39,
                    Gender = "M"
                },
                new Voter
                {
                    LalVoterId = "12345004",
                    FirstName = "Lisa",
                    LastName = "Brown",
                    AddressLine = "321 Elm Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35216",
                    CellPhone = "205-555-0104",
                    VoteFrequency = VoteFrequency.Frequent,
                    VoterSupport = VoterSupport.StrongYes,
                    Age = 34,
                    Gender = "F"
                },
                new Voter
                {
                    LalVoterId = "12345005",
                    FirstName = "Michael",
                    LastName = "Davis",
                    AddressLine = "654 Cedar Ln",
                    City = "Vestavia Hills",
                    State = "AL",
                    Zip = "35226",
                    CellPhone = "205-555-0105",
                    VoteFrequency = VoteFrequency.Infrequent,
                    VoterSupport = VoterSupport.LeaningNo,
                    Age = 52,
                    Gender = "M"
                },
                new Voter
                {
                    LalVoterId = "12345006",
                    FirstName = "Sarah",
                    LastName = "Wilson",
                    AddressLine = "987 Maple Way",
                    City = "Vestavia Hills",
                    State = "AL",
                    Zip = "35226",
                    CellPhone = "205-555-0106",
                    VoteFrequency = VoteFrequency.Frequent,
                    VoterSupport = VoterSupport.StrongYes,
                    Age = 36,
                    Gender = "F"
                },
                new Voter
                {
                    LalVoterId = "12345007",
                    FirstName = "David",
                    LastName = "Miller",
                    AddressLine = "147 Birch St",
                    City = "Mountain Brook",
                    State = "AL",
                    Zip = "35213",
                    CellPhone = "205-555-0107",
                    VoteFrequency = VoteFrequency.NonVoter,
                    VoterSupport = VoterSupport.Undecided,
                    Age = 29,
                    Gender = "M"
                },
                new Voter
                {
                    LalVoterId = "12345008",
                    FirstName = "Jennifer",
                    LastName = "Garcia",
                    AddressLine = "258 Willow Ct",
                    City = "Mountain Brook",
                    State = "AL",
                    Zip = "35213",
                    CellPhone = "205-555-0108",
                    VoteFrequency = VoteFrequency.Frequent,
                    VoterSupport = VoterSupport.LeaningYes,
                    Age = 41,
                    Gender = "F"
                },
                new Voter
                {
                    LalVoterId = "12345009",
                    FirstName = "James",
                    LastName = "Anderson",
                    AddressLine = "369 Spruce Ave",
                    City = "Homewood",
                    State = "AL",
                    Zip = "35209",
                    CellPhone = "205-555-0109",
                    VoteFrequency = VoteFrequency.Frequent,
                    VoterSupport = VoterSupport.StrongNo,
                    Age = 46,
                    Gender = "M"
                },
                new Voter
                {
                    LalVoterId = "12345010",
                    FirstName = "Amanda",
                    LastName = "Taylor",
                    AddressLine = "741 Poplar Rd",
                    City = "Homewood",
                    State = "AL",
                    Zip = "35209",
                    CellPhone = "205-555-0110",
                    VoteFrequency = VoteFrequency.Infrequent,
                    VoterSupport = VoterSupport.Undecided,
                    Age = 32,
                    Gender = "F"
                }
            };

            context.Voters.AddRange(sampleVoters);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Created {sampleVoters.Count} sample voters across ZIP codes: 35244, 35216, 35226, 35213, 35209");
        }
        else
        {
            Console.WriteLine("Voters already exist, skipping voter creation.");
        }

        // Create sample resource links if none exist
        if (!context.ResourceLinks.Any())
        {
            Console.WriteLine("Creating sample resource links...");
            
            var sampleResourceLinks = new List<ResourceLink>
            {
                // Voter Resources
                new ResourceLink
                {
                    Title = "Check Your Voter Registration",
                    Url = "https://myinfo.alabamavotes.gov/voterview",
                    Description = "Verify your voter registration status and polling location",
                    Category = ResourceCategory.VoterResources,
                    DisplayOrder = 1,
                    IsActive = true
                },
                new ResourceLink
                {
                    Title = "Register to Vote",
                    Url = "https://www.alabamavotes.gov/registertovote",
                    Description = "Register to vote online in Alabama",
                    Category = ResourceCategory.VoterResources,
                    DisplayOrder = 2,
                    IsActive = true
                },
                new ResourceLink
                {
                    Title = "Absentee Ballot Application",
                    Url = "https://www.alabamavotes.gov/absenteevoting",
                    Description = "Apply for an absentee ballot",
                    Category = ResourceCategory.VoterResources,
                    DisplayOrder = 3,
                    IsActive = true
                },
                new ResourceLink
                {
                    Title = "Sample Ballot",
                    Url = "https://myinfo.alabamavotes.gov/voterview",
                    Description = "View your sample ballot for upcoming elections",
                    Category = ResourceCategory.VoterResources,
                    DisplayOrder = 4,
                    IsActive = true
                },
                
                // Campaign Information
                new ResourceLink
                {
                    Title = "Campaign Website",
                    Url = "https://tanveer4hoover.com",
                    Description = "Official campaign website for Tanveer Patel",
                    Category = ResourceCategory.CampaignInformation,
                    DisplayOrder = 1,
                    IsActive = true
                },
                new ResourceLink
                {
                    Title = "Campaign Facebook",
                    Url = "https://facebook.com/tanveer4hoover",
                    Description = "Follow us on Facebook for campaign updates",
                    Category = ResourceCategory.CampaignInformation,
                    DisplayOrder = 2,
                    IsActive = true
                },
                new ResourceLink
                {
                    Title = "Campaign Twitter",
                    Url = "https://twitter.com/tanveer4hoover",
                    Description = "Follow us on Twitter for real-time updates",
                    Category = ResourceCategory.CampaignInformation,
                    DisplayOrder = 3,
                    IsActive = true
                },
                new ResourceLink
                {
                    Title = "Donate to Campaign",
                    Url = "https://secure.actblue.com/donate/tanveer4hoover",
                    Description = "Support our campaign with a donation",
                    Category = ResourceCategory.CampaignInformation,
                    DisplayOrder = 4,
                    IsActive = true
                },
                
                // General Resources
                new ResourceLink
                {
                    Title = "City of Hoover",
                    Url = "https://www.hooveral.org",
                    Description = "Official City of Hoover website",
                    Category = ResourceCategory.GeneralResources,
                    DisplayOrder = 1,
                    IsActive = true
                },
                new ResourceLink
                {
                    Title = "Hoover City Council",
                    Url = "https://www.hooveral.org/149/City-Council",
                    Description = "Current City Council information and meeting schedules",
                    Category = ResourceCategory.GeneralResources,
                    DisplayOrder = 2,
                    IsActive = true
                },
                new ResourceLink
                {
                    Title = "Alabama Secretary of State",
                    Url = "https://www.sos.alabama.gov",
                    Description = "Alabama Secretary of State official website",
                    Category = ResourceCategory.GeneralResources,
                    DisplayOrder = 3,
                    IsActive = true
                }
            };

            context.ResourceLinks.AddRange(sampleResourceLinks);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Created {sampleResourceLinks.Count} sample resource links");
        }
        else
        {
            Console.WriteLine("Resource links already exist, skipping resource link creation.");
        }

        Console.WriteLine("Seed data initialization completed.");
    }
}