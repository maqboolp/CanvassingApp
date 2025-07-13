using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Services
{
    public interface ITwilioService
    {
        Task<bool> SendSmsAsync(string toPhoneNumber, string message, int campaignMessageId, bool overrideOptIn = false);
        Task<bool> SendSmsAsync(string toPhoneNumber, string message); // Overload for non-campaign messages
        Task<List<bool>> SendBulkSmsAsync(List<(string phoneNumber, string message, int campaignMessageId)> messages, bool overrideOptIn = false);
        Task<bool> MakeRoboCallAsync(string toPhoneNumber, string voiceUrl, int campaignMessageId, int? expectedDurationSeconds = null);
        Task<CampaignMessage?> GetMessageStatusAsync(string twilioSid);
        Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
        string FormatPhoneNumber(string phoneNumber);
        Task<bool> CheckOptInStatusAsync(string phoneNumber); // Check if phone is opted in
    }
}