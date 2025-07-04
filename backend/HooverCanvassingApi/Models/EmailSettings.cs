namespace HooverCanvassingApi.Models;

public class EmailSettings
{
    // SendGrid settings
    public string SendGridApiKey { get; set; } = "[EmailSettings__SendGridApiKey]";
    public string FromEmail { get; set; } = "[EmailSettings__FromEmail]";
    public string FromName { get; set; } = "[EmailSettings__FromName]";
    
    // Legacy SMTP settings (kept for backward compatibility)
    public string SmtpServer { get; set; } = "[EmailSettings__SmtpServer]";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "[EmailSettings__Username]";
    public string Password { get; set; } = "[EmailSettings__Password]";
    public bool EnableSsl { get; set; } = true;
    
    // Email provider selector
    public string Provider { get; set; } = "SendGrid"; // "SendGrid" or "SMTP"
    
    // Frontend URL for email links
    public string FrontendBaseUrl { get; set; } = "[Frontend__BaseUrl]";
}