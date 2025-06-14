namespace HooverCanvassingApi.Services;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string email, string firstName, string resetToken, string resetUrl);
    Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? textContent = null);
}