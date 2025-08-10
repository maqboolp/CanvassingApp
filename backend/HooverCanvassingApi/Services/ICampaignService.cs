using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Services
{
    public interface ICampaignService
    {
        Task<Campaign> CreateCampaignAsync(Campaign campaign);
        Task<Campaign?> GetCampaignAsync(int id);
        Task<IEnumerable<Campaign>> GetCampaignsAsync();
        Task<Campaign> UpdateCampaignAsync(Campaign campaign);
        Task<bool> DeleteCampaignAsync(int id);
        Task<bool> SendCampaignAsync(int campaignId, bool overrideOptIn = false);
        Task<bool> ScheduleCampaignAsync(int campaignId, DateTime scheduledTime);
        Task<bool> CancelCampaignAsync(int campaignId);
        Task<IEnumerable<Voter>> GetCampaignRecipientsAsync(int campaignId);
        Task<CampaignStats> GetCampaignStatsAsync(int campaignId);
        Task<int> PreviewAudienceCountAsync(string? filterZipCodes);
        Task<int> GetRecipientCountAsync(string? filterZipCodes, VoteFrequency? filterVoteFrequency, int? filterMinAge, int? filterMaxAge, VoterSupport? filterVoterSupport, List<int>? filterTagIds = null);
        Task<RecipientCountResult> GetRecipientCountWithOptOutsAsync(CampaignType campaignType, string? filterZipCodes, VoteFrequency? filterVoteFrequency, int? filterMinAge, int? filterMaxAge, VoterSupport? filterVoterSupport, List<int>? filterTagIds = null);
        Task<IEnumerable<string>> GetAvailableZipCodesAsync();
        Task ProcessScheduledCampaignsAsync();
        Task<bool> RetryFailedMessagesAsync(int campaignId, bool overrideOptIn = false);
        Task<bool> SealCampaignIfCompleteAsync(int campaignId);
        Task<VoiceRecording?> GetVoiceRecordingAsync(int id);
        Task<Campaign?> DuplicateCampaignAsync(int campaignId, string userId);
        Task<List<Campaign>> CheckAndResumeStuckCampaignsAsync();
        Task<bool> ForceStopCampaignAsync(int campaignId);
    }

    public class CampaignStats
    {
        public int TotalRecipients { get; set; }
        public int Sent { get; set; }
        public int Delivered { get; set; }
        public int Failed { get; set; }
        public int Pending { get; set; }
        public int Remaining { get; set; } // Messages not yet processed (pending/queued)
        public decimal TotalCost { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
    }
    
    public class RecipientCountResult
    {
        public int TotalMatching { get; set; }  // Total voters matching filters
        public int OptedOut { get; set; }        // Number opted out
        public int Eligible { get; set; }        // Final eligible count (TotalMatching - OptedOut)
    }
}