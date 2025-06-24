using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using System.Security.Claims;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VolunteerResourcesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VolunteerResourcesController> _logger;

        public VolunteerResourcesController(ApplicationDbContext context, ILogger<VolunteerResourcesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VolunteerResourceDto>>> GetResources()
        {
            try
            {
                var resources = await _context.VolunteerResources
                    .OrderBy(r => r.ResourceType)
                    .ToListAsync();

                var resourceDtos = resources.Select(r => new VolunteerResourceDto
                {
                    ResourceType = r.ResourceType,
                    Content = r.Content,
                    UpdatedAt = r.UpdatedAt
                }).ToList();

                return Ok(resourceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving volunteer resources");
                return StatusCode(500, new { error = "Failed to retrieve resources" });
            }
        }

        [HttpGet("{resourceType}")]
        public async Task<ActionResult<VolunteerResourceDto>> GetResource(string resourceType)
        {
            try
            {
                var resource = await _context.VolunteerResources
                    .FirstOrDefaultAsync(r => r.ResourceType == resourceType);

                if (resource == null)
                {
                    // Return default content for new resources
                    var defaultContent = resourceType.ToLower() switch
                    {
                        "quicktips" => "• Always wear your volunteer badge\n• Be respectful and polite\n• Don't argue with voters\n• Use the app to log all contacts\n• Ask for help if you need it",
                        "script" => "Hi, my name is [Your Name] and I'm a volunteer for Tanveer Patel's campaign for Hoover City Council.\n\nI'd like to take just a moment to talk to you about the upcoming election. Tanveer is running to bring fresh perspectives and innovative solutions to our community.\n\nAre you planning to vote in the upcoming election?",
                        _ => ""
                    };

                    return Ok(new VolunteerResourceDto
                    {
                        ResourceType = resourceType,
                        Content = defaultContent,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                return Ok(new VolunteerResourceDto
                {
                    ResourceType = resource.ResourceType,
                    Content = resource.Content,
                    UpdatedAt = resource.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resource {ResourceType}", resourceType);
                return StatusCode(500, new { error = "Failed to retrieve resource" });
            }
        }

        [HttpPut("{resourceType}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<VolunteerResourceDto>> UpdateResource(string resourceType, [FromBody] UpdateResourceRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                var resource = await _context.VolunteerResources
                    .FirstOrDefaultAsync(r => r.ResourceType == resourceType);

                if (resource == null)
                {
                    // Create new resource
                    resource = new VolunteerResource
                    {
                        ResourceType = resourceType,
                        Content = request.Content,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastUpdatedBy = currentUserId
                    };
                    _context.VolunteerResources.Add(resource);
                }
                else
                {
                    // Update existing resource
                    resource.Content = request.Content;
                    resource.UpdatedAt = DateTime.UtcNow;
                    resource.LastUpdatedBy = currentUserId;
                }

                await _context.SaveChangesAsync();

                return Ok(new VolunteerResourceDto
                {
                    ResourceType = resource.ResourceType,
                    Content = resource.Content,
                    UpdatedAt = resource.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating resource {ResourceType}", resourceType);
                return StatusCode(500, new { error = "Failed to update resource" });
            }
        }
    }

    public class VolunteerResourceDto
    {
        public string ResourceType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateResourceRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}