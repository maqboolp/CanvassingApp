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
        Task<bool> SendCampaignAsync(int campaignId, bool overrideOptIn = false, int? batchSize = null, int? batchDelayMinutes = null);
        Task<bool> ScheduleCampaignAsync(int campaignId, DateTime scheduledTime);
        Task<bool> CancelCampaignAsync(int campaignId);
        Task<IEnumerable<Voter>> GetCampaignRecipientsAsync(int campaignId);
        Task<CampaignStats> GetCampaignStatsAsync(int campaignId);
        Task<int> PreviewAudienceCountAsync(string? filterZipCodes);
        Task<int> GetRecipientCountAsync(string? filterZipCodes, VoteFrequency? filterVoteFrequency, int? filterMinAge, int? filterMaxAge, VoterSupport? filterVoterSupport, List<int>? filterTagIds = null);
        Task<IEnumerable<string>> GetAvailableZipCodesAsync();
        Task ProcessScheduledCampaignsAsync();
        Task<bool> RetryFailedMessagesAsync(int campaignId, bool overrideOptIn = false, int? batchSize = null, int? batchDelayMinutes = null);
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
        public decimal TotalCost { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
    }
}