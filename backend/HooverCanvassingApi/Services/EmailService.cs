using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email, string firstName, string resetToken, string resetUrl)
    {
        var subject = "Reset Your Password - Tanveer for Hoover Campaign";
        var htmlContent = GeneratePasswordResetHtml(firstName, resetUrl);
        var textContent = GeneratePasswordResetText(firstName, resetUrl);

        return await SendEmailAsync(email, subject, htmlContent, textContent);
    }

    public async Task<bool> SendContactNotificationEmailAsync(string email, ContactNotificationData data)
    {
        var subject = "New Voter Contact - Tanveer for Hoover Campaign";
        var htmlContent = GenerateContactNotificationHtml(data);
        var textContent = GenerateContactNotificationText(data);

        return await SendEmailAsync(email, subject, htmlContent, textContent);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? textContent = null)
    {
        try
        {
            if (_emailSettings.Provider == "SendGrid")
            {
                return await SendEmailViaSendGridAsync(to, subject, htmlContent, textContent);
            }
            else
            {
                return await SendEmailViaSmtpAsync(to, subject, htmlContent, textContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {to}");
            return false;
        }
    }

    private async Task<bool> SendEmailViaSendGridAsync(string to, string subject, string htmlContent, string? textContent = null)
    {
        try
        {
            // For development, we'll log instead of actually sending email if no API key is configured
            if (string.IsNullOrEmpty(_emailSettings.SendGridApiKey))
            {
                _logger.LogInformation($"SendGrid email would be sent to {to}:");
                _logger.LogInformation($"Subject: {subject}");
                _logger.LogInformation($"HTML Content: {htmlContent}");
                _logger.LogInformation($"Text Content: {textContent}");
                return true;
            }

            var client = new SendGridClient(_emailSettings.SendGridApiKey);
            var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
            var toAddress = new EmailAddress(to);
            
            var msg = MailHelper.CreateSingleEmail(
                from, 
                toAddress, 
                subject, 
                textContent ?? htmlContent, // Use text content or fallback to HTML
                htmlContent
            );

            var response = await client.SendEmailAsync(msg);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"SendGrid email sent successfully to {to}");
                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError($"SendGrid API error: {response.StatusCode} - {responseBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email via SendGrid to {to}");
            return false;
        }
    }

    private async Task<bool> SendEmailViaSmtpAsync(string to, string subject, string htmlContent, string? textContent = null)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            
            if (!string.IsNullOrEmpty(textContent))
            {
                bodyBuilder.TextBody = textContent;
            }
            
            bodyBuilder.HtmlBody = htmlContent;
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // For development, we'll log instead of actually sending email if no SMTP credentials are configured
            if (string.IsNullOrEmpty(_emailSettings.Username) || string.IsNullOrEmpty(_emailSettings.Password))
            {
                _logger.LogInformation($"SMTP email would be sent to {to}:");
                _logger.LogInformation($"Subject: {subject}");
                _logger.LogInformation($"HTML Content: {htmlContent}");
                _logger.LogInformation($"Text Content: {textContent}");
                return true;
            }

            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.EnableSsl);
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"SMTP email sent successfully to {to}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email via SMTP to {to}");
            return false;
        }
    }

    private string GeneratePasswordResetHtml(string firstName, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ width: 200px; height: auto; margin-bottom: 20px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #673ab7; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 10px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='color: #673ab7; margin: 0;'>Tanveer for Hoover Campaign</h1>
            <p style='color: #666; margin: 5px 0 0 0;'>Canvassing Portal</p>
        </div>
        
        <div class='content'>
            <h2>Password Reset Request</h2>
            <p>Hello {firstName},</p>
            
            <p>We received a request to reset your password for your Tanveer for Hoover canvassing account. If you made this request, click the button below to reset your password:</p>
            
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Reset Password</a>
            </div>
            
            <div class='warning'>
                <strong>Important:</strong> This link will expire in 24 hours for security reasons.
            </div>
            
            <p>If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.</p>
            
            <p>If the button above doesn't work, copy and paste this link into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 3px; font-family: monospace;'>{resetUrl}</p>
            
            <p>If you have any questions or need assistance, please contact our support team.</p>
            
            <p>Thank you for your dedication to the campaign!</p>
            
            <p>Best regards,<br>
            The Tanveer for Hoover Campaign Team</p>
        </div>
        
        <div class='footer'>
            <p>Tanveer Patel for Hoover City Council<br>
            August 26, 2025 Election<br>
            Paid for by Tanveer for Hoover</p>
            
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetText(string firstName, string resetUrl)
    {
        return $@"
Tanveer for Hoover Campaign - Password Reset Request

Hello {firstName},

We received a request to reset your password for your Tanveer for Hoover canvassing account.

To reset your password, visit this link:
{resetUrl}

This link will expire in 24 hours for security reasons.

If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.

If you have any questions or need assistance, please contact our support team.

Thank you for your dedication to the campaign!

Best regards,
The Tanveer for Hoover Campaign Team

---
Tanveer Patel for Hoover City Council
August 26, 2025 Election
Paid for by Tanveer for Hoover

This is an automated message. Please do not reply to this email.
";
    }

    private string GenerateContactNotificationHtml(ContactNotificationData data)
    {
        var supportBadge = !string.IsNullOrEmpty(data.VoterSupport) 
            ? $"<span style='background-color: {GetSupportColor(data.VoterSupport)}; color: white; padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: bold;'>{data.VoterSupport.ToUpper()}</span>"
            : "";

        var statusBadge = $"<span style='background-color: {GetStatusColor(data.ContactStatus)}; color: white; padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: bold;'>{data.ContactStatus.ToUpper()}</span>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .contact-card {{ background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 8px; padding: 20px; margin: 20px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; margin: 10px 0; padding: 5px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: bold; color: #555; }}
        .detail-value {{ color: #333; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
        .alert {{ background-color: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='color: #673ab7; margin: 0;'>📞 New Voter Contact</h1>
            <p style='color: #666; margin: 5px 0 0 0;'>Tanveer for Hoover Campaign</p>
        </div>
        
        <div class='content'>
            <div class='alert'>
                <strong>📋 Contact Alert:</strong> A new voter contact has been logged in the canvassing system.
            </div>
            
            <div class='contact-card'>
                <h3 style='margin-top: 0; color: #673ab7;'>Contact Details</h3>
                
                <div class='detail-row'>
                    <span class='detail-label'>👤 Volunteer:</span>
                    <span class='detail-value'>{data.VolunteerName} ({data.VolunteerEmail})</span>
                </div>
                
                <div class='detail-row'>
                    <span class='detail-label'>🗳️ Voter:</span>
                    <span class='detail-value'>{data.VoterName}</span>
                </div>
                
                <div class='detail-row'>
                    <span class='detail-label'>📍 Address:</span>
                    <span class='detail-value'>{data.VoterAddress}</span>
                </div>
                
                <div class='detail-row'>
                    <span class='detail-label'>📞 Contact Status:</span>
                    <span class='detail-value'>{statusBadge}</span>
                </div>
                
                {(!string.IsNullOrEmpty(data.VoterSupport) ? $@"
                <div class='detail-row'>
                    <span class='detail-label'>💪 Voter Support:</span>
                    <span class='detail-value'>{supportBadge}</span>
                </div>" : "")}
                
                <div class='detail-row'>
                    <span class='detail-label'>⏰ Contact Time:</span>
                    <span class='detail-value'>{data.ContactTime:MMM dd, yyyy 'at' h:mm tt} UTC</span>
                </div>
                
                {(!string.IsNullOrEmpty(data.Location) ? $@"
                <div class='detail-row'>
                    <span class='detail-label'>📍 Location:</span>
                    <span class='detail-value'>{data.Location}</span>
                </div>" : "")}
                
                {(!string.IsNullOrEmpty(data.Notes) ? $@"
                <div style='margin-top: 15px; padding-top: 15px; border-top: 1px solid #ddd;'>
                    <div class='detail-label'>📝 Notes:</div>
                    <div style='margin-top: 8px; padding: 10px; background-color: #fff; border: 1px solid #ddd; border-radius: 4px; font-style: italic;'>
                        ""{data.Notes}""
                    </div>
                </div>" : "")}
            </div>
            
            <p>This contact has been automatically logged in the campaign management system. You can view detailed analytics and contact history in the admin dashboard.</p>
            
            <p>Keep up the great work! Every contact brings us closer to victory on Election Day.</p>
        </div>
        
        <div class='footer'>
            <p>Tanveer Patel for Hoover City Council<br>
            August 26, 2025 Election<br>
            Paid for by Tanveer for Hoover</p>
            
            <p>This is an automated notification. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateContactNotificationText(ContactNotificationData data)
    {
        var supportText = !string.IsNullOrEmpty(data.VoterSupport) ? $"\nVoter Support: {data.VoterSupport.ToUpper()}" : "";
        var locationText = !string.IsNullOrEmpty(data.Location) ? $"\nLocation: {data.Location}" : "";
        var notesText = !string.IsNullOrEmpty(data.Notes) ? $"\nNotes: \"{data.Notes}\"" : "";

        return $@"
TANVEER FOR HOOVER CAMPAIGN - NEW VOTER CONTACT

Contact Alert: A new voter contact has been logged in the canvassing system.

CONTACT DETAILS:
===============
Volunteer: {data.VolunteerName} ({data.VolunteerEmail})
Voter: {data.VoterName}
Address: {data.VoterAddress}
Contact Status: {data.ContactStatus.ToUpper()}
Contact Time: {data.ContactTime:MMM dd, yyyy 'at' h:mm tt} UTC{supportText}{locationText}{notesText}

This contact has been automatically logged in the campaign management system. You can view detailed analytics and contact history in the admin dashboard.

Keep up the great work! Every contact brings us closer to victory on Election Day.

---
Tanveer Patel for Hoover City Council
August 26, 2025 Election
Paid for by Tanveer for Hoover

This is an automated notification. Please do not reply to this email.
";
    }

    private string GetSupportColor(string support)
    {
        return support.ToLower() switch
        {
            "strong" => "#28a745",      // Green
            "lean" => "#ffc107",        // Yellow
            "undecided" => "#6c757d",   // Gray
            "opposed" => "#dc3545",     // Red
            _ => "#6c757d"              // Default gray
        };
    }

    private string GetStatusColor(string status)
    {
        return status.ToLower() switch
        {
            "contacted" => "#28a745",        // Green
            "not_home" => "#ffc107",         // Yellow
            "refused" => "#dc3545",          // Red
            "moved" => "#6c757d",            // Gray
            "callback" => "#17a2b8",         // Blue
            _ => "#6c757d"                   // Default gray
        };
    }
}