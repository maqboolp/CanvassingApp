using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Services
{
    public interface ITwilioService
    {
        Task<bool> SendSmsAsync(string toPhoneNumber, string message, int campaignMessageId);
        Task<bool> MakeRoboCallAsync(string toPhoneNumber, string voiceUrl, int campaignMessageId);
        Task<CampaignMessage?> GetMessageStatusAsync(string twilioSid);
        Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
        string FormatPhoneNumber(string phoneNumber);
    }
}