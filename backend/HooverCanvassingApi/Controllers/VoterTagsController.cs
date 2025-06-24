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
    public class VoterTagsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VoterTagsController> _logger;

        public VoterTagsController(ApplicationDbContext context, ILogger<VoterTagsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/votertags
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VoterTagDto>>> GetTags()
        {
            try
            {
                var tags = await _context.VoterTags
                    .Include(t => t.CreatedBy)
                    .Select(t => new VoterTagDto
                    {
                        Id = t.Id,
                        TagName = t.TagName,
                        Description = t.Description,
                        Color = t.Color,
                        VoterCount = t.VoterAssignments.Count(),
                        CreatedAt = t.CreatedAt,
                        CreatedBy = t.CreatedBy != null ? $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}" : null
                    })
                    .OrderBy(t => t.TagName)
                    .ToListAsync();

                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving voter tags");
                return StatusCode(500, new { error = "Failed to retrieve tags" });
            }
        }

        // GET: api/votertags/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VoterTagDetailDto>> GetTag(int id)
        {
            try
            {
                var tag = await _context.VoterTags
                    .Include(t => t.CreatedBy)
                    .Include(t => t.VoterAssignments)
                        .ThenInclude(va => va.Voter)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tag == null)
                {
                    return NotFound();
                }

                var tagDto = new VoterTagDetailDto
                {
                    Id = tag.Id,
                    TagName = tag.TagName,
                    Description = tag.Description,
                    Color = tag.Color,
                    CreatedAt = tag.CreatedAt,
                    CreatedBy = tag.CreatedBy != null ? $"{tag.CreatedBy.FirstName} {tag.CreatedBy.LastName}" : null,
                    VoterCount = tag.VoterAssignments.Count,
                    RecentVoters = tag.VoterAssignments
                        .OrderByDescending(va => va.AssignedAt)
                        .Take(10)
                        .Select(va => new VoterSummaryDto
                        {
                            LalVoterId = va.Voter.LalVoterId,
                            FirstName = va.Voter.FirstName,
                            LastName = va.Voter.LastName,
                            AddressLine = va.Voter.AddressLine,
                            AssignedAt = va.AssignedAt
                        })
                        .ToList()
                };

                return Ok(tagDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tag {TagId}", id);
                return StatusCode(500, new { error = "Failed to retrieve tag" });
            }
        }

        // POST: api/votertags
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<VoterTagDto>> CreateTag([FromBody] CreateTagRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                // Check if tag name already exists
                var existingTag = await _context.VoterTags
                    .FirstOrDefaultAsync(t => t.TagName == request.TagName);

                if (existingTag != null)
                {
                    return BadRequest(new { error = "A tag with this name already exists" });
                }

                var tag = new VoterTag
                {
                    TagName = request.TagName,
                    Description = request.Description,
                    Color = request.Color ?? "#2196F3", // Default blue color
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = currentUserId
                };

                _context.VoterTags.Add(tag);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, new VoterTagDto
                {
                    Id = tag.Id,
                    TagName = tag.TagName,
                    Description = tag.Description,
                    Color = tag.Color,
                    VoterCount = 0,
                    CreatedAt = tag.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag");
                return StatusCode(500, new { error = "Failed to create tag" });
            }
        }

        // PUT: api/votertags/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateTagRequest request)
        {
            try
            {
                var tag = await _context.VoterTags.FindAsync(id);
                if (tag == null)
                {
                    return NotFound();
                }

                // Check if new name conflicts with existing tag
                if (tag.TagName != request.TagName)
                {
                    var existingTag = await _context.VoterTags
                        .FirstOrDefaultAsync(t => t.TagName == request.TagName && t.Id != id);

                    if (existingTag != null)
                    {
                        return BadRequest(new { error = "A tag with this name already exists" });
                    }
                }

                tag.TagName = request.TagName;
                tag.Description = request.Description;
                tag.Color = request.Color;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag {TagId}", id);
                return StatusCode(500, new { error = "Failed to update tag" });
            }
        }

        // DELETE: api/votertags/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                var tag = await _context.VoterTags
                    .Include(t => t.VoterAssignments)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tag == null)
                {
                    return NotFound();
                }

                _context.VoterTags.Remove(tag);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag {TagId}", id);
                return StatusCode(500, new { error = "Failed to delete tag" });
            }
        }

        // POST: api/votertags/5/voters
        [HttpPost("{tagId}/voters")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AddVotersToTag(int tagId, [FromBody] AddVotersToTagRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                var tag = await _context.VoterTags.FindAsync(tagId);
                if (tag == null)
                {
                    return NotFound(new { error = "Tag not found" });
                }

                // Get existing assignments to avoid duplicates
                var existingAssignments = await _context.VoterTagAssignments
                    .Where(vta => vta.TagId == tagId && request.VoterIds.Contains(vta.VoterId))
                    .Select(vta => vta.VoterId)
                    .ToListAsync();

                var newVoterIds = request.VoterIds.Except(existingAssignments).ToList();

                if (newVoterIds.Any())
                {
                    var assignments = newVoterIds.Select(voterId => new VoterTagAssignment
                    {
                        VoterId = voterId,
                        TagId = tagId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedById = currentUserId
                    });

                    _context.VoterTagAssignments.AddRange(assignments);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { addedCount = newVoterIds.Count, skippedCount = existingAssignments.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding voters to tag {TagId}", tagId);
                return StatusCode(500, new { error = "Failed to add voters to tag" });
            }
        }

        // DELETE: api/votertags/5/voters
        [HttpDelete("{tagId}/voters")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> RemoveVotersFromTag(int tagId, [FromBody] RemoveVotersFromTagRequest request)
        {
            try
            {
                var assignments = await _context.VoterTagAssignments
                    .Where(vta => vta.TagId == tagId && request.VoterIds.Contains(vta.VoterId))
                    .ToListAsync();

                if (assignments.Any())
                {
                    _context.VoterTagAssignments.RemoveRange(assignments);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { removedCount = assignments.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing voters from tag {TagId}", tagId);
                return StatusCode(500, new { error = "Failed to remove voters from tag" });
            }
        }
    }

    // DTOs
    public class VoterTagDto
    {
        public int Id { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public int VoterCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class VoterTagDetailDto : VoterTagDto
    {
        public List<VoterSummaryDto> RecentVoters { get; set; } = new List<VoterSummaryDto>();
    }

    public class VoterSummaryDto
    {
        public string LalVoterId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class CreateTagRequest
    {
        public string TagName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
    }

    public class UpdateTagRequest
    {
        public string TagName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
    }

    public class AddVotersToTagRequest
    {
        public List<string> VoterIds { get; set; } = new List<string>();
    }

    public class RemoveVotersFromTagRequest
    {
        public List<string> VoterIds { get; set; } = new List<string>();
    }
}