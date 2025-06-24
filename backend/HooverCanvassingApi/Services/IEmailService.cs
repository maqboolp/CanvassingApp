namespace HooverCanvassingApi.Services;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string email, string firstName, string resetToken, string resetUrl);
    Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? textContent = null);
    Task<bool> SendContactNotificationEmailAsync(string email, ContactNotificationData data);
    Task<bool> SendContactDeletionNotificationEmailAsync(string email, ContactDeletionNotificationData data);
    Task<bool> SendInvitationEmailAsync(string email, string inviterName, string registrationUrl, string role);
    Task<bool> SendRegistrationApprovalNotificationAsync(string adminEmail, PendingRegistrationData data);
    Task<bool> SendRegistrationStatusEmailAsync(string email, string firstName, bool approved, string? adminNotes = null);
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

public class ContactDeletionNotificationData
{
    public string DeletedByName { get; set; } = string.Empty;
    public string DeletedByEmail { get; set; } = string.Empty;
    public string VolunteerName { get; set; } = string.Empty;
    public string VolunteerEmail { get; set; } = string.Empty;
    public string VoterName { get; set; } = string.Empty;
    public string VoterAddress { get; set; } = string.Empty;
    public string ContactStatus { get; set; } = string.Empty;
    public string? VoterSupport { get; set; }
    public string? Notes { get; set; }
    public DateTime OriginalContactTime { get; set; }
    public DateTime DeletionTime { get; set; }
    public string? Location { get; set; }
    public string VoterNewStatus { get; set; } = string.Empty;
}

public class PendingRegistrationData
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string RequestedRole { get; set; } = string.Empty;
    public DateTime RegistrationTime { get; set; }
    public string PendingVolunteerId { get; set; } = string.Empty;
}