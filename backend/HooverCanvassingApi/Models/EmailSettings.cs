namespace HooverCanvassingApi.Models;

public class EmailSettings
{
    // SendGrid settings
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    
    // Legacy SMTP settings (kept for backward compatibility)
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    
    // Email provider selector
    public string Provider { get; set; } = "SendGrid"; // "SendGrid" or "SMTP"
}