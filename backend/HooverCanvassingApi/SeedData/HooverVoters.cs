using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Seeding
{
    public static class HooverVoters
    {
        public static async Task SeedHooverVotersAsync(ApplicationDbContext context)
        {
            // Check if we already have Hoover voters
            var existingHooverVoters = context.Voters.Any(v => v.City == "Hoover" || v.City == "HOOVER");
            if (existingHooverVoters)
            {
                Console.WriteLine("Hoover voters already exist, skipping seeding.");
                return;
            }

            Console.WriteLine("Adding Hoover, Alabama sample voters...");

            var hooverVoters = new List<Voter>
            {
                // Spanish Trace area
                new Voter
                {
                    LalVoterId = "HOV001",
                    FirstName = "Michael",
                    LastName = "Johnson",
                    AddressLine = "1540 Spanish Trace",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 45,
                    Gender = "Male",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.4056,
                    Longitude = -86.8269,
                    CellPhone = "(205) 555-0101",
                    Email = "michael.johnson@example.com",
                    IsContacted = false
                },
                new Voter
                {
                    LalVoterId = "HOV002",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    AddressLine = "1540 Spanish Trace",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 42,
                    Gender = "Female",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.4056,
                    Longitude = -86.8269,
                    CellPhone = "(205) 555-0102",
                    Email = "sarah.johnson@example.com",
                    IsContacted = false
                },
                
                // Trace Crossings area
                new Voter
                {
                    LalVoterId = "HOV003",
                    FirstName = "David",
                    LastName = "Martinez",
                    AddressLine = "2105 Trace Crossings Way",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 38,
                    Gender = "Male",
                    PartyAffiliation = "Democrat",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.4089,
                    Longitude = -86.8234,
                    CellPhone = "(205) 555-0103",
                    Email = "david.martinez@example.com",
                    IsContacted = false
                },
                new Voter
                {
                    LalVoterId = "HOV004",
                    FirstName = "Jennifer",
                    LastName = "Martinez",
                    AddressLine = "2105 Trace Crossings Way",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 36,
                    Gender = "Female",
                    PartyAffiliation = "Democrat",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.4089,
                    Longitude = -86.8234,
                    CellPhone = "(205) 555-0104",
                    Email = "jennifer.martinez@example.com",
                    IsContacted = false
                },

                // Stadium Trace area
                new Voter
                {
                    LalVoterId = "HOV005",
                    FirstName = "Robert",
                    LastName = "Williams",
                    AddressLine = "1820 Stadium Trace Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 52,
                    Gender = "Male",
                    PartyAffiliation = "Independent",
                    VoteFrequency = VoteFrequency.Infrequent,
                    Latitude = 33.4021,
                    Longitude = -86.8158,
                    CellPhone = "(205) 555-0105",
                    Email = "robert.williams@example.com",
                    IsContacted = false
                },
                new Voter
                {
                    LalVoterId = "HOV006",
                    FirstName = "Lisa",
                    LastName = "Williams",
                    AddressLine = "1820 Stadium Trace Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 49,
                    Gender = "Female",
                    PartyAffiliation = "Independent",
                    VoteFrequency = VoteFrequency.Infrequent,
                    Latitude = 33.4021,
                    Longitude = -86.8158,
                    CellPhone = "(205) 555-0106",
                    Email = "lisa.williams@example.com",
                    IsContacted = false
                },

                // Riverchase Parkway area
                new Voter
                {
                    LalVoterId = "HOV007",
                    FirstName = "Christopher",
                    LastName = "Brown",
                    AddressLine = "3025 Riverchase Pkwy",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 34,
                    Gender = "Male",
                    PartyAffiliation = "Democrat",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3963,
                    Longitude = -86.8089,
                    CellPhone = "(205) 555-0107",
                    Email = "christopher.brown@example.com",
                    IsContacted = false
                },

                // Greystone area
                new Voter
                {
                    LalVoterId = "HOV008",
                    FirstName = "Amanda",
                    LastName = "Davis",
                    AddressLine = "1250 Greystone Crest Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35242",
                    Age = 41,
                    Gender = "Female",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3789,
                    Longitude = -86.7923,
                    CellPhone = "(205) 555-0108",
                    Email = "amanda.davis@example.com",
                    IsContacted = false
                },
                new Voter
                {
                    LalVoterId = "HOV009",
                    FirstName = "James",
                    LastName = "Davis",
                    AddressLine = "1250 Greystone Crest Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35242",
                    Age = 44,
                    Gender = "Male",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3789,
                    Longitude = -86.7923,
                    CellPhone = "(205) 555-0109",
                    Email = "james.davis@example.com",
                    IsContacted = false
                },

                // Bluff Park area
                new Voter
                {
                    LalVoterId = "HOV010",
                    FirstName = "Michelle",
                    LastName = "Wilson",
                    AddressLine = "2145 Bluff Park Rd",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35226",
                    Age = 39,
                    Gender = "Female",
                    PartyAffiliation = "Democrat",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3845,
                    Longitude = -86.8456,
                    CellPhone = "(205) 555-0110",
                    Email = "michelle.wilson@example.com",
                    IsContacted = false
                },

                // Galleria area
                new Voter
                {
                    LalVoterId = "HOV011",
                    FirstName = "Kevin",
                    LastName = "Moore",
                    AddressLine = "1745 Montgomery Hwy",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 29,
                    Gender = "Male",
                    PartyAffiliation = "Independent",
                    VoteFrequency = VoteFrequency.Infrequent,
                    Latitude = 33.4145,
                    Longitude = -86.8067,
                    CellPhone = "(205) 555-0111",
                    Email = "kevin.moore@example.com",
                    IsContacted = false
                },

                // Preserve area
                new Voter
                {
                    LalVoterId = "HOV012",
                    FirstName = "Jessica",
                    LastName = "Taylor",
                    AddressLine = "6040 Preserve Pass",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 33,
                    Gender = "Female",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3923,
                    Longitude = -86.8245,
                    CellPhone = "(205) 555-0112",
                    Email = "jessica.taylor@example.com",
                    IsContacted = false
                },
                new Voter
                {
                    LalVoterId = "HOV013",
                    FirstName = "Ryan",
                    LastName = "Taylor",
                    AddressLine = "6040 Preserve Pass",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 35,
                    Gender = "Male",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3923,
                    Longitude = -86.8245,
                    CellPhone = "(205) 555-0113",
                    Email = "ryan.taylor@example.com",
                    IsContacted = false
                },

                // Lake Cyrus area
                new Voter
                {
                    LalVoterId = "HOV014",
                    FirstName = "Daniel",
                    LastName = "Anderson",
                    AddressLine = "1055 Lake Cyrus Club Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 47,
                    Gender = "Male",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3856,
                    Longitude = -86.8178,
                    CellPhone = "(205) 555-0114",
                    Email = "daniel.anderson@example.com",
                    IsContacted = false
                },
                new Voter
                {
                    LalVoterId = "HOV015",
                    FirstName = "Nicole",
                    LastName = "Anderson",
                    AddressLine = "1055 Lake Cyrus Club Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 45,
                    Gender = "Female",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3856,
                    Longitude = -86.8178,
                    CellPhone = "(205) 555-0115",
                    Email = "nicole.anderson@example.com",
                    IsContacted = false
                },

                // Highland Lakes area
                new Voter
                {
                    LalVoterId = "HOV016",
                    FirstName = "Brian",
                    LastName = "Thomas",
                    AddressLine = "2340 Highland Lakes Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 40,
                    Gender = "Male",
                    PartyAffiliation = "Democrat",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.4012,
                    Longitude = -86.8201,
                    CellPhone = "(205) 555-0116",
                    Email = "brian.thomas@example.com",
                    IsContacted = false
                },

                // Riverchase Country Club area
                new Voter
                {
                    LalVoterId = "HOV017",
                    FirstName = "Stephanie",
                    LastName = "Jackson",
                    AddressLine = "1965 Riverchase Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 37,
                    Gender = "Female",
                    PartyAffiliation = "Independent",
                    VoteFrequency = VoteFrequency.Infrequent,
                    Latitude = 33.3978,
                    Longitude = -86.8123,
                    CellPhone = "(205) 555-0117",
                    Email = "stephanie.jackson@example.com",
                    IsContacted = false
                },

                // Patton Creek area
                new Voter
                {
                    LalVoterId = "HOV018",
                    FirstName = "Matthew",
                    LastName = "White",
                    AddressLine = "4520 Preserve Pkwy",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 31,
                    Gender = "Male",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3934,
                    Longitude = -86.8289,
                    CellPhone = "(205) 555-0118",
                    Email = "matthew.white@example.com",
                    IsContacted = false
                },
                new Voter
                {
                    LalVoterId = "HOV019",
                    FirstName = "Ashley",
                    LastName = "White",
                    AddressLine = "4520 Preserve Pkwy",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 29,
                    Gender = "Female",
                    PartyAffiliation = "Republican",
                    VoteFrequency = VoteFrequency.Frequent,
                    Latitude = 33.3934,
                    Longitude = -86.8289,
                    CellPhone = "(205) 555-0119",
                    Email = "ashley.white@example.com",
                    IsContacted = false
                },

                // Hoover High School area
                new Voter
                {
                    LalVoterId = "HOV020",
                    FirstName = "Andrew",
                    LastName = "Harris",
                    AddressLine = "1000 Buccaneer Dr",
                    City = "Hoover",
                    State = "AL",
                    Zip = "35244",
                    Age = 28,
                    Gender = "Male",
                    PartyAffiliation = "Democrat",
                    VoteFrequency = VoteFrequency.Infrequent,
                    Latitude = 33.4034,
                    Longitude = -86.8345,
                    CellPhone = "(205) 555-0120",
                    Email = "andrew.harris@example.com",
                    IsContacted = false
                }
            };

            // Set additional properties for all voters
            foreach (var voter in hooverVoters)
            {
                voter.Ethnicity = GetRandomEthnicity();
                voter.Religion = GetRandomReligion();
                voter.Income = GetRandomIncome();
                voter.CallCount = 0;
                voter.SmsCount = 0;
                voter.TotalCampaignContacts = 0;
                voter.SmsConsentStatus = SmsConsentStatus.Unknown;
            }

            await context.Voters.AddRangeAsync(hooverVoters);
            await context.SaveChangesAsync();

            Console.WriteLine($"Successfully added {hooverVoters.Count} Hoover voters!");
        }

        private static string GetRandomEthnicity()
        {
            var ethnicities = new[] { "Caucasian", "African American", "Hispanic", "Asian", "Native American", "Other" };
            return ethnicities[Random.Shared.Next(ethnicities.Length)];
        }

        private static string GetRandomReligion()
        {
            var religions = new[] { "Christian", "Baptist", "Methodist", "Catholic", "Presbyterian", "Jewish", "Muslim", "Other", "None" };
            return religions[Random.Shared.Next(religions.Length)];
        }

        private static string GetRandomIncome()
        {
            var incomes = new[] { "$25,000-$49,999", "$50,000-$74,999", "$75,000-$99,999", "$100,000-$149,999", "$150,000+" };
            return incomes[Random.Shared.Next(incomes.Length)];
        }
    }
}