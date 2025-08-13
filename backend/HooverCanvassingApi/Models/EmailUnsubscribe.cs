using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models;

public class EmailUnsubscribe
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string? VoterId { get; set; }
    
    public DateTime UnsubscribedAt { get; set; }
    
    public string? UnsubscribeToken { get; set; }
    
    public string? Reason { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    // Track which campaign triggered the unsubscribe
    public int? CampaignId { get; set; }
    public virtual Campaign? Campaign { get; set; }
    
    // Navigation property
    public virtual Voter? Voter { get; set; }
}