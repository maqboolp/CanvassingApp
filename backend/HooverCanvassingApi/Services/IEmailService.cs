namespace HooverCanvassingApi.Services;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string email, string firstName, string resetToken, string resetUrl);
    Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? textContent = null);
    Task<bool> SendContactNotificationEmailAsync(string email, ContactNotificationData data);
}

public class ContactNotificationData
{
    public string VolunteerName { get; set; } = string.Empty;
    public string VolunteerEmail { get; set; } = string.Empty;
    public string VoterName { get; set; } = string.Empty;
    public string VoterAddress { get; set; } = string.Empty;
    public string ContactStatus { get; set; } = string.Empty;
    public string? VoterSupport { get; set; }
    public string? Notes { get; set; }
    public DateTime ContactTime { get; set; }
    public string? Location { get; set; }
}