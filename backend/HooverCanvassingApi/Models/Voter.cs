using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class Voter
    {
        [Key]
        public string LalVoterId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Ethnicity { get; set; }
        public string? Religion { get; set; }
        public string? Income { get; set; }
        public string Gender { get; set; } = string.Empty;
        public VoteFrequency VoteFrequency { get; set; }
        public string? PartyAffiliation { get; set; }
        public string? CellPhone { get; set; }
        public string? Email { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsContacted { get; set; } = false;
        public ContactStatus? LastContactStatus { get; set; }
        public VoterSupport? VoterSupport { get; set; }
        
        // Campaign communication tracking
        public DateTime? LastCampaignContactAt { get; set; }
        public int? LastCampaignId { get; set; } // Track which campaign last contacted this voter
        public int TotalCampaignContacts { get; set; } = 0;
        public DateTime? LastSmsAt { get; set; }
        public int? LastSmsCampaignId { get; set; } // Track which campaign sent the last SMS
        public DateTime? LastCallAt { get; set; }
        public int? LastCallCampaignId { get; set; } // Track which campaign made the last call
        public int SmsCount { get; set; } = 0;
        public int CallCount { get; set; } = 0;
        
        // SMS Opt-in/Opt-out tracking
        public SmsConsentStatus SmsConsentStatus { get; set; } = SmsConsentStatus.Unknown;
        public DateTime? SmsOptInAt { get; set; }
        public DateTime? SmsOptOutAt { get; set; }
        public ConsentMethod? SmsOptInMethod { get; set; }
        public string? SmsOptInSource { get; set; } // IP address or phone number source
        
        public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public ICollection<CampaignMessage> CampaignMessages { get; set; } = new List<CampaignMessage>();
        public ICollection<VoterTagAssignment> TagAssignments { get; set; } = new List<VoterTagAssignment>();
        public ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();
    }

    public enum VoteFrequency
    {
        NonVoter,
        Infrequent,
        Frequent
    }

    public enum ContactStatus
    {
        Reached,
        NotHome,
        Refused,
        NeedsFollowUp
    }

    public enum VoterSupport
    {
        StrongYes,      // Strong yes - will Vote for Tanveer
        LeaningYes,     // Leaning yes - May vote for Tanveer - but hadn't heard of her before, or was a little softer enthusiasm
        Undecided,      // Undecided - they won't share or say they need to do research (even if they seem somewhat positive)
        LeaningNo,      // Leaning against - Not into Tanveer
        StrongNo        // Strong no - Definitely not voting for Tanveer
    }

    public enum SmsConsentStatus
    {
        Unknown,        // No explicit consent or opt-out recorded
        OptedIn,        // User has explicitly opted in to receive SMS
        OptedOut        // User has opted out of SMS communications
    }

    public enum ConsentMethod
    {
        WebForm,        // Opted in via website form
        TextMessage,    // Opted in via SMS (JOIN keyword)
        Import,         // Imported with existing consent
        Manual          // Manually added by campaign staff
    }
}