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

        _logger.LogInformation("=== EMAIL SERVICE: Sending contact notification to {Email} ===", email);
        _logger.LogInformation("Email subject: {Subject}", subject);
        
        var result = await SendEmailAsync(email, subject, htmlContent, textContent);
        
        _logger.LogInformation("Email service result for {Email}: {Result}", email, result ? "SUCCESS" : "FAILED");
        return result;
    }

    public async Task<bool> SendInvitationEmailAsync(string email, string inviterName, string registrationUrl, string role)
    {
        var subject = "You're Invited to Join Tanveer for Hoover Campaign";
        var htmlContent = GenerateInvitationHtml(email, inviterName, registrationUrl, role);
        var textContent = GenerateInvitationText(email, inviterName, registrationUrl, role);

        _logger.LogInformation("Sending invitation email to {Email} for {Role} role", email, role);
        return await SendEmailAsync(email, subject, htmlContent, textContent);
    }

    public async Task<bool> SendRegistrationApprovalNotificationAsync(string adminEmail, PendingRegistrationData data)
    {
        var subject = "New Volunteer Registration Awaiting Approval";
        var htmlContent = GenerateRegistrationApprovalHtml(data);
        var textContent = GenerateRegistrationApprovalText(data);

        _logger.LogInformation("Sending registration approval notification to admin {Email}", adminEmail);
        return await SendEmailAsync(adminEmail, subject, htmlContent, textContent);
    }

    public async Task<bool> SendRegistrationStatusEmailAsync(string email, string firstName, bool approved, string? adminNotes = null)
    {
        var subject = approved 
            ? "Welcome to Tanveer for Hoover Campaign Team!" 
            : "Registration Update - Tanveer for Hoover Campaign";
        var htmlContent = GenerateRegistrationStatusHtml(firstName, approved, adminNotes);
        var textContent = GenerateRegistrationStatusText(firstName, approved, adminNotes);

        _logger.LogInformation("Sending registration status email to {Email}: {Status}", email, approved ? "APPROVED" : "REJECTED");
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
            _logger.LogInformation("=== SENDGRID: Starting email send process ===");
            _logger.LogInformation("SendGrid API Key configured: {HasKey}", !string.IsNullOrEmpty(_emailSettings.SendGridApiKey));
            
            // For development, we'll log instead of actually sending email if no API key is configured
            if (string.IsNullOrEmpty(_emailSettings.SendGridApiKey))
            {
                _logger.LogWarning("=== DEVELOPMENT MODE: No SendGrid API Key configured ===");
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

    private string GenerateInvitationHtml(string email, string inviterName, string registrationUrl, string role)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .button {{ display: inline-block; padding: 15px 30px; background-color: #673ab7; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
        .welcome {{ background-color: #e8f5e8; border: 1px solid #4caf50; color: #2e7d32; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='color: #673ab7; margin: 0;'>🎉 You're Invited!</h1>
            <p style='color: #666; margin: 5px 0 0 0;'>Join the Tanveer for Hoover Campaign Team</p>
        </div>
        
        <div class='content'>
            <div class='welcome'>
                <strong>Great news!</strong> You've been invited to join our campaign as a <strong>{role}</strong>.
            </div>
            
            <p>Hello!</p>
            
            <p><strong>{inviterName}</strong> has invited you to join the Tanveer for Hoover campaign team. We're building a grassroots movement to bring positive change to our community, and we'd love to have you on board!</p>
            
            <p>As a <strong>{role.ToLower()}</strong>, you'll be able to:</p>
            <ul>
                <li>Help with voter outreach and canvassing</li>
                <li>Track contacts and campaign progress</li>
                <li>Connect with other volunteers</li>
                <li>Make a real difference in our community</li>
            </ul>
            
            <p>To complete your registration and join the team, please click the button below:</p>
            
            <div style='text-align: center;'>
                <a href='{registrationUrl}' class='button'>Complete Your Registration</a>
            </div>
            
            <p><strong>Important:</strong> This invitation link will expire in 7 days for security reasons.</p>
            
            <p>If the button above doesn't work, copy and paste this link into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 3px; font-family: monospace;'>{registrationUrl}</p>
            
            <p>We're excited to have you join our team and work together towards a better future for Hoover!</p>
            
            <p>Best regards,<br>
            The Tanveer for Hoover Campaign Team</p>
        </div>
        
        <div class='footer'>
            <p>Tanveer Patel for Hoover City Council<br>
            August 26, 2025 Election<br>
            Paid for by Tanveer for Hoover</p>
            
            <p>This invitation was sent to {email}. If you received this in error, please ignore this message.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateInvitationText(string email, string inviterName, string registrationUrl, string role)
    {
        return $@"
TANVEER FOR HOOVER CAMPAIGN - TEAM INVITATION

You're Invited to Join Our Campaign!

Hello!

{inviterName} has invited you to join the Tanveer for Hoover campaign team as a {role}.

We're building a grassroots movement to bring positive change to our community, and we'd love to have you on board!

As a {role.ToLower()}, you'll be able to:
- Help with voter outreach and canvassing
- Track contacts and campaign progress  
- Connect with other volunteers
- Make a real difference in our community

To complete your registration and join the team, visit this link:
{registrationUrl}

IMPORTANT: This invitation link will expire in 7 days for security reasons.

We're excited to have you join our team and work together towards a better future for Hoover!

Best regards,
The Tanveer for Hoover Campaign Team

---
Tanveer Patel for Hoover City Council
August 26, 2025 Election
Paid for by Tanveer for Hoover

This invitation was sent to {email}. If you received this in error, please ignore this message.
";
    }

    private string GenerateRegistrationApprovalHtml(PendingRegistrationData data)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .registration-card {{ background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 8px; padding: 20px; margin: 20px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; margin: 10px 0; padding: 5px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: bold; color: #555; }}
        .detail-value {{ color: #333; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
        .alert {{ background-color: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #673ab7; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 10px 5px; }}
        .button-approve {{ background-color: #28a745; }}
        .button-reject {{ background-color: #dc3545; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='color: #673ab7; margin: 0;'>👋 New Volunteer Registration</h1>
            <p style='color: #666; margin: 5px 0 0 0;'>Awaiting Your Approval</p>
        </div>
        
        <div class='content'>
            <div class='alert'>
                <strong>Action Required:</strong> A new volunteer has registered and is awaiting approval to join the campaign.
            </div>
            
            <div class='registration-card'>
                <h3 style='margin-top: 0; color: #673ab7;'>Registration Details</h3>
                
                <div class='detail-row'>
                    <span class='detail-label'>👤 Name:</span>
                    <span class='detail-value'>{data.FirstName} {data.LastName}</span>
                </div>
                
                <div class='detail-row'>
                    <span class='detail-label'>📧 Email:</span>
                    <span class='detail-value'>{data.Email}</span>
                </div>
                
                <div class='detail-row'>
                    <span class='detail-label'>📱 Phone:</span>
                    <span class='detail-value'>{data.PhoneNumber}</span>
                </div>
                
                <div class='detail-row'>
                    <span class='detail-label'>🎯 Requested Role:</span>
                    <span class='detail-value'>{data.RequestedRole}</span>
                </div>
                
                <div class='detail-row'>
                    <span class='detail-label'>⏰ Registration Time:</span>
                    <span class='detail-value'>{data.RegistrationTime:MMM dd, yyyy 'at' h:mm tt} UTC</span>
                </div>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <p><strong>Please review this registration and take action:</strong></p>
                <a href='https://t4h-canvas-2uwxt.ondigitalocean.app/admin/pending-volunteers' class='button'>
                    Review in Admin Dashboard
                </a>
            </div>
            
            <p>This volunteer is waiting for approval to join the campaign. Please log into the admin dashboard to approve or reject this registration.</p>
            
            <p>Keep building our amazing volunteer team!</p>
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

    private string GenerateRegistrationApprovalText(PendingRegistrationData data)
    {
        return $@"
TANVEER FOR HOOVER CAMPAIGN - NEW VOLUNTEER REGISTRATION

Action Required: A new volunteer has registered and is awaiting approval.

REGISTRATION DETAILS:
===================
Name: {data.FirstName} {data.LastName}
Email: {data.Email}
Phone: {data.PhoneNumber}
Requested Role: {data.RequestedRole}
Registration Time: {data.RegistrationTime:MMM dd, yyyy 'at' h:mm tt} UTC

Please review this registration in the admin dashboard:
https://t4h-canvas-2uwxt.ondigitalocean.app/admin/pending-volunteers

This volunteer is waiting for approval to join the campaign. Please log into the admin dashboard to approve or reject this registration.

Keep building our amazing volunteer team!

---
Tanveer Patel for Hoover City Council
August 26, 2025 Election
Paid for by Tanveer for Hoover

This is an automated notification. Please do not reply to this email.
";
    }

    private string GenerateRegistrationStatusHtml(string firstName, bool approved, string? adminNotes)
    {
        var statusColor = approved ? "#28a745" : "#dc3545";
        var statusText = approved ? "Approved" : "Rejected";
        var statusIcon = approved ? "🎉" : "❌";
        
        var notesSection = !string.IsNullOrEmpty(adminNotes) ? $@"
            <div style='background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 5px; padding: 15px; margin: 20px 0;'>
                <h4 style='margin-top: 0; color: #673ab7;'>Message from Admin:</h4>
                <p style='margin-bottom: 0; font-style: italic;'>""{adminNotes}""</p>
            </div>" : "";

        var mainContent = approved ? $@"
            <p>Congratulations! Your registration has been approved and your account is now active. You can log in to the campaign portal and start making a difference in our community.</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://t4h-canvas-2uwxt.ondigitalocean.app/login' class='button'>
                    Log In to Campaign Portal
                </a>
            </div>
            
            <p>Welcome to the team! We're excited to work with you to bring positive change to Hoover.</p>" : $@"
            <p>Thank you for your interest in joining the Tanveer for Hoover campaign. Unfortunately, your registration was not approved at this time.</p>
            
            <p>If you have questions about this decision or would like to discuss other ways to support the campaign, please feel free to reach out to our team.</p>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .button {{ display: inline-block; padding: 15px 30px; background-color: #673ab7; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
        .status {{ background-color: {statusColor}; color: white; padding: 15px; border-radius: 5px; margin: 20px 0; text-align: center; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='color: #673ab7; margin: 0;'>{statusIcon} Registration {statusText}</h1>
            <p style='color: #666; margin: 5px 0 0 0;'>Tanveer for Hoover Campaign</p>
        </div>
        
        <div class='content'>
            <div class='status'>
                Your registration has been {statusText.ToLower()}
            </div>
            
            <p>Hello {firstName},</p>
            
            {mainContent}
            
            {notesSection}
            
            <p>Thank you for your interest in supporting Tanveer Patel's campaign for Hoover City Council.</p>
            
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

    private string GenerateRegistrationStatusText(string firstName, bool approved, string? adminNotes)
    {
        var statusText = approved ? "APPROVED" : "REJECTED";
        var notesSection = !string.IsNullOrEmpty(adminNotes) ? $@"

MESSAGE FROM ADMIN:
{adminNotes}" : "";

        var mainContent = approved ? @"
Congratulations! Your registration has been approved and your account is now active. You can log in to the campaign portal and start making a difference in our community.

Log in here: https://t4h-canvas-2uwxt.ondigitalocean.app/login

Welcome to the team! We're excited to work with you to bring positive change to Hoover." : @"
Thank you for your interest in joining the Tanveer for Hoover campaign. Unfortunately, your registration was not approved at this time.

If you have questions about this decision or would like to discuss other ways to support the campaign, please feel free to reach out to our team.";

        return $@"
TANVEER FOR HOOVER CAMPAIGN - REGISTRATION {statusText}

Hello {firstName},

{mainContent}{notesSection}

Thank you for your interest in supporting Tanveer Patel's campaign for Hoover City Council.

Best regards,
The Tanveer for Hoover Campaign Team

---
Tanveer Patel for Hoover City Council
August 26, 2025 Election
Paid for by Tanveer for Hoover

This is an automated message. Please do not reply to this email.
";
    }
}