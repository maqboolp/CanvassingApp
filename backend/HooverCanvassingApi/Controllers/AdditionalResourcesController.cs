using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdditionalResourcesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdditionalResourcesController> _logger;

    public AdditionalResourcesController(ApplicationDbContext context, ILogger<AdditionalResourcesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdditionalResource>>> GetResources()
    {
        var resources = await _context.AdditionalResources
            .Where(r => r.IsActive)
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Title)
            .ToListAsync();

        return Ok(resources);
    }

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<AdditionalResource>>> GetAllResources()
    {
        var resources = await _context.AdditionalResources
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Title)
            .ToListAsync();

        return Ok(resources);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<AdditionalResource>> GetResource(int id)
    {
        var resource = await _context.AdditionalResources.FindAsync(id);
        
        if (resource == null)
        {
            return NotFound();
        }

        return Ok(resource);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<AdditionalResource>> CreateResource(AdditionalResource resource)
    {
        resource.CreatedAt = DateTime.UtcNow;
        resource.UpdatedAt = DateTime.UtcNow;
        resource.CreatedBy = User.Identity?.Name ?? "Unknown";
        resource.UpdatedBy = User.Identity?.Name ?? "Unknown";

        _context.AdditionalResources.Add(resource);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new additional resource: {Title} by {User}", resource.Title, User.Identity?.Name);

        return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, resource);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateResource(int id, AdditionalResource resource)
    {
        if (id != resource.Id)
        {
            return BadRequest();
        }

        var existingResource = await _context.AdditionalResources.FindAsync(id);
        if (existingResource == null)
        {
            return NotFound();
        }

        existingResource.Title = resource.Title;
        existingResource.Url = resource.Url;
        existingResource.Description = resource.Description;
        existingResource.Category = resource.Category;
        existingResource.IsActive = resource.IsActive;
        existingResource.DisplayOrder = resource.DisplayOrder;
        existingResource.UpdatedAt = DateTime.UtcNow;
        existingResource.UpdatedBy = User.Identity?.Name ?? "Unknown";

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated additional resource: {Title} by {User}", resource.Title, User.Identity?.Name);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ResourceExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteResource(int id)
    {
        var resource = await _context.AdditionalResources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }

        _context.AdditionalResources.Remove(resource);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted additional resource: {Title} by {User}", resource.Title, User.Identity?.Name);

        return NoContent();
    }

    private bool ResourceExists(int id)
    {
        return _context.AdditionalResources.Any(e => e.Id == id);
    }
}