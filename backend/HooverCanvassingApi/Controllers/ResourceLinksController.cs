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
    public class ResourceLinksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResourceLinksController> _logger;

        public ResourceLinksController(ApplicationDbContext context, ILogger<ResourceLinksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ResourceLinks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResourceLink>>> GetResourceLinks([FromQuery] ResourceCategory? category = null)
        {
            try
            {
                var query = _context.ResourceLinks
                    .Where(rl => rl.IsActive)
                    .OrderBy(rl => rl.Category)
                    .ThenBy(rl => rl.DisplayOrder)
                    .ThenBy(rl => rl.Title);

                if (category.HasValue)
                {
                    query = query.Where(rl => rl.Category == category.Value)
                        .OrderBy(rl => rl.DisplayOrder)
                        .ThenBy(rl => rl.Title);
                }

                var links = await query.Select(rl => new
                {
                    id = rl.Id,
                    title = rl.Title,
                    url = rl.Url,
                    description = rl.Description,
                    category = rl.Category.ToString(),
                    displayOrder = rl.DisplayOrder,
                    isActive = rl.IsActive,
                    createdAt = rl.CreatedAt,
                    updatedAt = rl.UpdatedAt
                }).ToListAsync();

                return Ok(links);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching resource links");
                return StatusCode(500, new { error = "Failed to fetch resource links" });
            }
        }

        // GET: api/ResourceLinks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ResourceLink>> GetResourceLink(string id)
        {
            try
            {
                var resourceLink = await _context.ResourceLinks.FindAsync(id);

                if (resourceLink == null)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    id = resourceLink.Id,
                    title = resourceLink.Title,
                    url = resourceLink.Url,
                    description = resourceLink.Description,
                    category = resourceLink.Category.ToString(),
                    displayOrder = resourceLink.DisplayOrder,
                    isActive = resourceLink.IsActive,
                    createdAt = resourceLink.CreatedAt,
                    updatedAt = resourceLink.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching resource link {Id}", id);
                return StatusCode(500, new { error = "Failed to fetch resource link" });
            }
        }

        // POST: api/ResourceLinks
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<ResourceLink>> CreateResourceLink([FromBody] CreateResourceLinkRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var resourceLink = new ResourceLink
                {
                    Title = request.Title,
                    Url = request.Url,
                    Description = request.Description,
                    Category = request.Category,
                    DisplayOrder = request.DisplayOrder ?? 0,
                    IsActive = request.IsActive ?? true,
                    CreatedByUserId = currentUserId
                };

                _context.ResourceLinks.Add(resourceLink);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Resource link '{Title}' created by user {UserId}", resourceLink.Title, currentUserId);

                return CreatedAtAction(nameof(GetResourceLink), new { id = resourceLink.Id }, new
                {
                    id = resourceLink.Id,
                    title = resourceLink.Title,
                    url = resourceLink.Url,
                    description = resourceLink.Description,
                    category = resourceLink.Category.ToString(),
                    displayOrder = resourceLink.DisplayOrder,
                    isActive = resourceLink.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating resource link");
                return StatusCode(500, new { error = "Failed to create resource link" });
            }
        }

        // PUT: api/ResourceLinks/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateResourceLink(string id, [FromBody] UpdateResourceLinkRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var resourceLink = await _context.ResourceLinks.FindAsync(id);
                if (resourceLink == null)
                {
                    return NotFound();
                }

                resourceLink.Title = request.Title;
                resourceLink.Url = request.Url;
                resourceLink.Description = request.Description;
                resourceLink.Category = request.Category;
                resourceLink.DisplayOrder = request.DisplayOrder ?? resourceLink.DisplayOrder;
                resourceLink.IsActive = request.IsActive ?? resourceLink.IsActive;
                resourceLink.UpdatedAt = DateTime.UtcNow;
                resourceLink.UpdatedByUserId = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Resource link '{Title}' updated by user {UserId}", resourceLink.Title, currentUserId);

                return Ok(new
                {
                    id = resourceLink.Id,
                    title = resourceLink.Title,
                    url = resourceLink.Url,
                    description = resourceLink.Description,
                    category = resourceLink.Category.ToString(),
                    displayOrder = resourceLink.DisplayOrder,
                    isActive = resourceLink.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating resource link {Id}", id);
                return StatusCode(500, new { error = "Failed to update resource link" });
            }
        }

        // DELETE: api/ResourceLinks/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteResourceLink(string id)
        {
            try
            {
                var resourceLink = await _context.ResourceLinks.FindAsync(id);
                if (resourceLink == null)
                {
                    return NotFound();
                }

                _context.ResourceLinks.Remove(resourceLink);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Resource link '{Title}' deleted", resourceLink.Title);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting resource link {Id}", id);
                return StatusCode(500, new { error = "Failed to delete resource link" });
            }
        }

        // PUT: api/ResourceLinks/reorder
        [HttpPut("reorder")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ReorderResourceLinks([FromBody] ReorderResourceLinksRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                foreach (var item in request.Items)
                {
                    var resourceLink = await _context.ResourceLinks.FindAsync(item.Id);
                    if (resourceLink != null)
                    {
                        resourceLink.DisplayOrder = item.DisplayOrder;
                        resourceLink.UpdatedAt = DateTime.UtcNow;
                        resourceLink.UpdatedByUserId = currentUserId;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Resource links reordered by user {UserId}", currentUserId);

                return Ok(new { message = "Resource links reordered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering resource links");
                return StatusCode(500, new { error = "Failed to reorder resource links" });
            }
        }
    }

    public class CreateResourceLinkRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ResourceCategory Category { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateResourceLinkRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ResourceCategory Category { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ReorderResourceLinksRequest
    {
        public List<ReorderItem> Items { get; set; } = new List<ReorderItem>();
    }

    public class ReorderItem
    {
        public string Id { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}