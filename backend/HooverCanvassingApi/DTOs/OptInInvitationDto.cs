using System.ComponentModel.DataAnnotations;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.DTOs
{
    public class SendOptInInvitationRequest
    {
        [Required]
        public List<string> VoterIds { get; set; } = new List<string>();
        
        public string? CustomMessage { get; set; }
        
        // Optional: Filter criteria to select voters
        public OptInInvitationFilter? Filter { get; set; }
    }
    
    public class OptInInvitationFilter
    {
        public List<string>? ZipCodes { get; set; }
        public VoteFrequency? VoteFrequency { get; set; }
        public bool? ExcludeOptedOut { get; set; } = true;
        public bool? ExcludeOptedIn { get; set; } = true;
        public int? MaxRecipients { get; set; }
    }
    
    public class SendOptInInvitationResponse
    {
        public bool Success { get; set; }
        public int TotalSelected { get; set; }
        public int SuccessfullySent { get; set; }
        public int Failed { get; set; }
        public int AlreadyOptedIn { get; set; }
        public int AlreadyOptedOut { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string>? FailedVoterIds { get; set; }
    }
    
    public class PreviewOptInInvitationResponse
    {
        public string MessagePreview { get; set; } = string.Empty;
        public int EstimatedRecipients { get; set; }
        public List<VoterPreview> SampleRecipients { get; set; } = new List<VoterPreview>();
    }
    
    public class VoterPreview
    {
        public string VoterId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ConsentStatus { get; set; } = string.Empty;
    }
}