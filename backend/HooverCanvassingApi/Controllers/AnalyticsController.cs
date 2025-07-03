using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using System.Linq;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(ApplicationDbContext context, ILogger<AnalyticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("voter-demographics")]
        public async Task<ActionResult<VoterDemographicsResponse>> GetVoterDemographics()
        {
            try
            {
                // Get total voters
                var totalVoters = await _context.Voters.CountAsync();

                // Gender breakdown
                var genderStats = await _context.Voters
                    .GroupBy(v => v.Gender)
                    .Select(g => new GenderStat
                    {
                        Gender = g.Key ?? "Unknown",
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / totalVoters * 100, 2)
                    })
                    .OrderByDescending(g => g.Count)
                    .ToListAsync();

                // Age group breakdown
                var ageGroups = await _context.Voters
                    .Select(v => v.Age)
                    .ToListAsync();

                var ageGroupStats = new List<AgeGroupStat>
                {
                    new AgeGroupStat 
                    { 
                        AgeGroup = "18-24", 
                        Count = ageGroups.Count(a => a >= 18 && a <= 24),
                        Percentage = Math.Round((double)ageGroups.Count(a => a >= 18 && a <= 24) / totalVoters * 100, 2)
                    },
                    new AgeGroupStat 
                    { 
                        AgeGroup = "25-34", 
                        Count = ageGroups.Count(a => a >= 25 && a <= 34),
                        Percentage = Math.Round((double)ageGroups.Count(a => a >= 25 && a <= 34) / totalVoters * 100, 2)
                    },
                    new AgeGroupStat 
                    { 
                        AgeGroup = "35-44", 
                        Count = ageGroups.Count(a => a >= 35 && a <= 44),
                        Percentage = Math.Round((double)ageGroups.Count(a => a >= 35 && a <= 44) / totalVoters * 100, 2)
                    },
                    new AgeGroupStat 
                    { 
                        AgeGroup = "45-54", 
                        Count = ageGroups.Count(a => a >= 45 && a <= 54),
                        Percentage = Math.Round((double)ageGroups.Count(a => a >= 45 && a <= 54) / totalVoters * 100, 2)
                    },
                    new AgeGroupStat 
                    { 
                        AgeGroup = "55-64", 
                        Count = ageGroups.Count(a => a >= 55 && a <= 64),
                        Percentage = Math.Round((double)ageGroups.Count(a => a >= 55 && a <= 64) / totalVoters * 100, 2)
                    },
                    new AgeGroupStat 
                    { 
                        AgeGroup = "65+", 
                        Count = ageGroups.Count(a => a >= 65),
                        Percentage = Math.Round((double)ageGroups.Count(a => a >= 65) / totalVoters * 100, 2)
                    },
                    new AgeGroupStat 
                    { 
                        AgeGroup = "Unknown", 
                        Count = ageGroups.Count(a => a == 0 || a < 18),
                        Percentage = Math.Round((double)ageGroups.Count(a => a == 0 || a < 18) / totalVoters * 100, 2)
                    }
                };

                // Party affiliation breakdown
                var partyStats = await _context.Voters
                    .GroupBy(v => v.PartyAffiliation ?? "Unknown")
                    .Select(g => new PartyAffiliationStat
                    {
                        Party = g.Key,
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / totalVoters * 100, 2)
                    })
                    .OrderByDescending(p => p.Count)
                    .ToListAsync();

                // Vote frequency breakdown - fetch data first then transform
                var voteFrequencyData = await _context.Voters
                    .GroupBy(v => v.VoteFrequency)
                    .Select(g => new { Frequency = g.Key, Count = g.Count() })
                    .ToListAsync();

                var voteFrequencyStats = voteFrequencyData
                    .Select(vf => new VoteFrequencyStat
                    {
                        Frequency = vf.Frequency.ToString(),
                        Count = vf.Count,
                        Percentage = Math.Round((double)vf.Count / totalVoters * 100, 2)
                    })
                    .OrderByDescending(v => v.Count)
                    .ToList();

                // Ethnicity breakdown (if available)
                var ethnicityStats = await _context.Voters
                    .Where(v => v.Ethnicity != null)
                    .GroupBy(v => v.Ethnicity)
                    .Select(g => new EthnicityStat
                    {
                        Ethnicity = g.Key ?? "Unknown",
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / totalVoters * 100, 2)
                    })
                    .OrderByDescending(e => e.Count)
                    .ToListAsync();

                // Religion breakdown (if available)
                var religionStats = await _context.Voters
                    .Where(v => v.Religion != null)
                    .GroupBy(v => v.Religion)
                    .Select(g => new ReligionStat
                    {
                        Religion = g.Key ?? "Unknown",
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / totalVoters * 100, 2)
                    })
                    .OrderByDescending(r => r.Count)
                    .ToListAsync();

                // Income breakdown (if available)
                var incomeStats = await _context.Voters
                    .Where(v => v.Income != null)
                    .GroupBy(v => v.Income)
                    .Select(g => new IncomeStat
                    {
                        Income = g.Key ?? "Unknown",
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / totalVoters * 100, 2)
                    })
                    .OrderByDescending(i => i.Count)
                    .ToListAsync();

                // Geographic breakdown by zip code (top 10)
                var zipCodeStats = await _context.Voters
                    .Where(v => !string.IsNullOrEmpty(v.Zip))
                    .GroupBy(v => v.Zip)
                    .Select(g => new ZipCodeStat
                    {
                        ZipCode = g.Key,
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / totalVoters * 100, 2)
                    })
                    .OrderByDescending(z => z.Count)
                    .Take(10)
                    .ToListAsync();

                // Contact status breakdown
                var contactedCount = await _context.Voters.CountAsync(v => v.IsContacted);
                var notContactedCount = totalVoters - contactedCount;

                var contactStats = new ContactStatusStats
                {
                    Contacted = contactedCount,
                    ContactedPercentage = Math.Round((double)contactedCount / totalVoters * 100, 2),
                    NotContacted = notContactedCount,
                    NotContactedPercentage = Math.Round((double)notContactedCount / totalVoters * 100, 2)
                };

                // Voter support breakdown - fetch data first then transform
                var voterSupportData = await _context.Voters
                    .Where(v => v.VoterSupport.HasValue)
                    .GroupBy(v => v.VoterSupport!.Value)
                    .Select(g => new { Support = g.Key, Count = g.Count() })
                    .ToListAsync();

                var voterSupportStats = voterSupportData
                    .Select(vs => new VoterSupportStat
                    {
                        Support = vs.Support.ToString(),
                        Count = vs.Count,
                        Percentage = Math.Round((double)vs.Count / totalVoters * 100, 2)
                    })
                    .OrderBy(v => v.Support)
                    .ToList();

                var response = new VoterDemographicsResponse
                {
                    TotalVoters = totalVoters,
                    GenderStats = genderStats,
                    AgeGroupStats = ageGroupStats.Where(a => a.Count > 0).ToList(),
                    PartyAffiliationStats = partyStats,
                    VoteFrequencyStats = voteFrequencyStats,
                    EthnicityStats = ethnicityStats,
                    ReligionStats = religionStats,
                    IncomeStats = incomeStats,
                    ZipCodeStats = zipCodeStats,
                    ContactStats = contactStats,
                    VoterSupportStats = voterSupportStats,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating voter demographics");
                return StatusCode(500, new { error = "An error occurred while generating analytics" });
            }
        }

        [HttpGet("contact-analytics")]
        public async Task<ActionResult<ContactAnalyticsResponse>> GetContactAnalytics()
        {
            try
            {
                var totalContacts = await _context.Contacts.CountAsync();
                
                // Contacts by status - fetch data first then transform
                var contactsByStatusData = await _context.Contacts
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var contactsByStatus = contactsByStatusData
                    .Select(cs => new ContactStatusStat
                    {
                        Status = cs.Status.ToString(),
                        Count = cs.Count,
                        Percentage = totalContacts > 0 ? Math.Round((double)cs.Count / totalContacts * 100, 2) : 0
                    })
                    .ToList();

                // Contacts by volunteer (top 10)
                var contactsByVolunteer = await _context.Contacts
                    .Include(c => c.Volunteer)
                    .GroupBy(c => new { c.VolunteerId, c.Volunteer.FirstName, c.Volunteer.LastName })
                    .Select(g => new VolunteerContactStat
                    {
                        VolunteerId = g.Key.VolunteerId,
                        VolunteerName = $"{g.Key.FirstName} {g.Key.LastName}",
                        ContactCount = g.Count()
                    })
                    .OrderByDescending(v => v.ContactCount)
                    .Take(10)
                    .ToListAsync();

                // Contacts over time (last 30 days)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var contactsOverTime = await _context.Contacts
                    .Where(c => c.Timestamp >= thirtyDaysAgo)
                    .GroupBy(c => c.Timestamp.Date)
                    .Select(g => new ContactsOverTimeStat
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(c => c.Date)
                    .ToListAsync();

                var response = new ContactAnalyticsResponse
                {
                    TotalContacts = totalContacts,
                    ContactsByStatus = contactsByStatus,
                    ContactsByVolunteer = contactsByVolunteer,
                    ContactsOverTime = contactsOverTime,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contact analytics");
                return StatusCode(500, new { error = "An error occurred while generating analytics" });
            }
        }
    }

    // Response DTOs
    public class VoterDemographicsResponse
    {
        public int TotalVoters { get; set; }
        public List<GenderStat> GenderStats { get; set; } = new();
        public List<AgeGroupStat> AgeGroupStats { get; set; } = new();
        public List<PartyAffiliationStat> PartyAffiliationStats { get; set; } = new();
        public List<VoteFrequencyStat> VoteFrequencyStats { get; set; } = new();
        public List<EthnicityStat> EthnicityStats { get; set; } = new();
        public List<ReligionStat> ReligionStats { get; set; } = new();
        public List<IncomeStat> IncomeStats { get; set; } = new();
        public List<ZipCodeStat> ZipCodeStats { get; set; } = new();
        public ContactStatusStats ContactStats { get; set; } = new();
        public List<VoterSupportStat> VoterSupportStats { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class GenderStat
    {
        public string Gender { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class AgeGroupStat
    {
        public string AgeGroup { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class PartyAffiliationStat
    {
        public string Party { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class VoteFrequencyStat
    {
        public string Frequency { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class EthnicityStat
    {
        public string Ethnicity { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class ReligionStat
    {
        public string Religion { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class IncomeStat
    {
        public string Income { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class ZipCodeStat
    {
        public string ZipCode { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class ContactStatusStats
    {
        public int Contacted { get; set; }
        public double ContactedPercentage { get; set; }
        public int NotContacted { get; set; }
        public double NotContactedPercentage { get; set; }
    }

    public class VoterSupportStat
    {
        public string Support { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class ContactAnalyticsResponse
    {
        public int TotalContacts { get; set; }
        public List<ContactStatusStat> ContactsByStatus { get; set; } = new();
        public List<VolunteerContactStat> ContactsByVolunteer { get; set; } = new();
        public List<ContactsOverTimeStat> ContactsOverTime { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class ContactStatusStat
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class VolunteerContactStat
    {
        public string VolunteerId { get; set; } = string.Empty;
        public string VolunteerName { get; set; } = string.Empty;
        public int ContactCount { get; set; }
    }

    public class ContactsOverTimeStat
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}