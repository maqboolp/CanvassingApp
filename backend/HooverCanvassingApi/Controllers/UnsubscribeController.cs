using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace HooverCanvassingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnsubscribeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnsubscribeController> _logger;
    private readonly IConfiguration _configuration;

    public UnsubscribeController(
        ApplicationDbContext context,
        ILogger<UnsubscribeController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> GetUnsubscribeInfo(string token)
    {
        try
        {
            // Decode the token to get email and campaign info
            var (email, campaignId, voterId) = DecodeUnsubscribeToken(token);
            
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Invalid unsubscribe token" });
            }

            // Check if already unsubscribed
            var existingUnsubscribe = await _context.EmailUnsubscribes
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUnsubscribe != null)
            {
                return Ok(new 
                { 
                    email = email,
                    alreadyUnsubscribed = true,
                    unsubscribedAt = existingUnsubscribe.UnsubscribedAt
                });
            }

            // Get voter info if available
            Voter? voter = null;
            if (!string.IsNullOrEmpty(voterId))
            {
                voter = await _context.Voters.FindAsync(voterId);
            }

            // Get campaign info if available
            Campaign? campaign = null;
            if (campaignId.HasValue)
            {
                campaign = await _context.Campaigns.FindAsync(campaignId.Value);
            }

            return Ok(new
            {
                email = email,
                alreadyUnsubscribed = false,
                voterName = voter != null ? $"{voter.FirstName} {voter.LastName}" : null,
                campaignName = campaign?.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unsubscribe info");
            return BadRequest(new { error = "Invalid unsubscribe token" });
        }
    }

    [HttpPost("{token}")]
    public async Task<IActionResult> Unsubscribe(string token, [FromBody] UnsubscribeRequest? request = null)
    {
        try
        {
            // Decode the token to get email and campaign info
            var (email, campaignId, voterId) = DecodeUnsubscribeToken(token);
            
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Invalid unsubscribe token" });
            }

            // Check if already unsubscribed
            var existingUnsubscribe = await _context.EmailUnsubscribes
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUnsubscribe != null)
            {
                return Ok(new 
                { 
                    success = true, 
                    message = "You have already been unsubscribed from our email list.",
                    alreadyUnsubscribed = true 
                });
            }

            // Create unsubscribe record
            var unsubscribe = new EmailUnsubscribe
            {
                Email = email,
                VoterId = voterId,
                CampaignId = campaignId,
                UnsubscribedAt = DateTime.UtcNow,
                UnsubscribeToken = token,
                Reason = request?.Reason,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            _context.EmailUnsubscribes.Add(unsubscribe);

            // Update voter record if exists
            if (!string.IsNullOrEmpty(voterId))
            {
                var voter = await _context.Voters.FindAsync(voterId);
                if (voter != null)
                {
                    voter.EmailOptOut = true;
                    voter.EmailOptOutDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Email {email} unsubscribed successfully");

            return Ok(new 
            { 
                success = true, 
                message = "You have been successfully unsubscribed from our email list." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing unsubscribe");
            return StatusCode(500, new { error = "An error occurred while processing your unsubscribe request" });
        }
    }

    [HttpPost("resubscribe/{token}")]
    public async Task<IActionResult> Resubscribe(string token)
    {
        try
        {
            var (email, _, voterId) = DecodeUnsubscribeToken(token);
            
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Invalid token" });
            }

            // Remove unsubscribe record
            var unsubscribe = await _context.EmailUnsubscribes
                .FirstOrDefaultAsync(u => u.Email == email);

            if (unsubscribe != null)
            {
                _context.EmailUnsubscribes.Remove(unsubscribe);
            }

            // Update voter record if exists
            if (!string.IsNullOrEmpty(voterId))
            {
                var voter = await _context.Voters.FindAsync(voterId);
                if (voter != null)
                {
                    voter.EmailOptOut = false;
                    voter.EmailOptOutDate = null;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Email {email} resubscribed successfully");

            return Ok(new 
            { 
                success = true, 
                message = "You have been successfully resubscribed to our email list." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing resubscribe");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpGet("check/{email}")]
    public async Task<IActionResult> CheckUnsubscribeStatus(string email)
    {
        try
        {
            var isUnsubscribed = await _context.EmailUnsubscribes
                .AnyAsync(u => u.Email == email);

            return Ok(new { email, isUnsubscribed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking unsubscribe status for {email}");
            return StatusCode(500, new { error = "An error occurred while checking unsubscribe status" });
        }
    }

    public static string GenerateUnsubscribeToken(string email, int? campaignId = null, string? voterId = null, string? secretKey = null)
    {
        // Create a composite string with email, campaign ID, and voter ID
        var data = $"{email}|{campaignId ?? 0}|{voterId ?? ""}|{DateTime.UtcNow.Ticks}";
        
        // Use secret key or default
        var key = secretKey ?? "default-unsubscribe-key-change-in-production";
        
        // Create HMAC for security
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var hashString = Convert.ToBase64String(hash);
            
            // Combine data and hash
            var token = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(data))}:{hashString}";
            
            // Make URL-safe
            return token.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }

    private (string? email, int? campaignId, string? voterId) DecodeUnsubscribeToken(string token)
    {
        try
        {
            // Restore original Base64 format
            token = token.Replace("-", "+").Replace("_", "/");
            
            // Add padding if necessary
            while (token.Length % 4 != 0 && !token.Contains(':'))
            {
                token += "=";
            }
            
            // Split data and hash
            var parts = token.Split(':');
            if (parts.Length != 2)
            {
                _logger.LogWarning($"Invalid token format: {token}");
                return (null, null, null);
            }
            
            // Decode the data part
            var dataBase64 = parts[0];
            while (dataBase64.Length % 4 != 0)
            {
                dataBase64 += "=";
            }
            
            var dataBytes = Convert.FromBase64String(dataBase64);
            var data = Encoding.UTF8.GetString(dataBytes);
            
            // Parse the data
            var dataParts = data.Split('|');
            if (dataParts.Length < 3)
            {
                _logger.LogWarning($"Invalid token data format: {data}");
                return (null, null, null);
            }
            
            var email = dataParts[0];
            var campaignId = int.TryParse(dataParts[1], out var cId) && cId > 0 ? cId : (int?)null;
            var voterId = string.IsNullOrEmpty(dataParts[2]) ? null : dataParts[2];
            
            // TODO: Validate HMAC hash for security in production
            
            return (email, campaignId, voterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decoding unsubscribe token");
            return (null, null, null);
        }
    }
}

public class UnsubscribeRequest
{
    public string? Reason { get; set; }
}