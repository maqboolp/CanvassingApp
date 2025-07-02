using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using System.Security.Claims;

namespace HooverCanvassingApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceRecordingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VoiceRecordingsController> _logger;
        private readonly IFileStorageService _fileStorageService;

        public VoiceRecordingsController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<VoiceRecordingsController> logger,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VoiceRecordingDto>>> GetVoiceRecordings()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var query = _context.VoiceRecordings.AsQueryable();

            // Non-SuperAdmins can only see their own recordings
            if (userRole != "SuperAdmin")
            {
                query = query.Where(vr => vr.CreatedById == userId);
            }

            var recordings = await query
                .OrderByDescending(vr => vr.CreatedAt)
                .Select(vr => new VoiceRecordingDto
                {
                    Id = vr.Id,
                    Name = vr.Name,
                    Description = vr.Description,
                    Url = vr.Url,
                    FileName = vr.FileName,
                    FileSizeBytes = vr.FileSizeBytes,
                    DurationSeconds = vr.DurationSeconds,
                    CreatedAt = vr.CreatedAt,
                    LastUsedAt = vr.LastUsedAt,
                    UsageCount = vr.UsageCount,
                    CreatedBy = vr.CreatedBy != null ? vr.CreatedBy.FirstName + " " + vr.CreatedBy.LastName : "Unknown"
                })
                .ToListAsync();

            return Ok(recordings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VoiceRecordingDto>> GetVoiceRecording(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var recording = await _context.VoiceRecordings
                .Include(vr => vr.CreatedBy)
                .FirstOrDefaultAsync(vr => vr.Id == id);

            if (recording == null)
                return NotFound();

            // Check access permissions
            if (userRole != "SuperAdmin" && recording.CreatedById != userId)
                return Forbid();

            var dto = new VoiceRecordingDto
            {
                Id = recording.Id,
                Name = recording.Name,
                Description = recording.Description,
                Url = recording.Url,
                FileName = recording.FileName,
                FileSizeBytes = recording.FileSizeBytes,
                DurationSeconds = recording.DurationSeconds,
                CreatedAt = recording.CreatedAt,
                LastUsedAt = recording.LastUsedAt,
                UsageCount = recording.UsageCount,
                CreatedBy = recording.CreatedBy != null ? recording.CreatedBy.FirstName + " " + recording.CreatedBy.LastName : "Unknown"
            };

            return Ok(dto);
        }

        [HttpPost("upload")]
        public async Task<ActionResult<VoiceRecordingDto>> UploadVoiceRecording([FromForm] UploadVoiceRecordingRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded");

            // Validate file type
            var allowedExtensions = new[] { ".mp3", ".wav", ".m4a", ".ogg" };
            var extension = Path.GetExtension(request.File.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid file type. Allowed types: MP3, WAV, M4A, OGG");

            // Validate file size (max 10MB)
            if (request.File.Length > 10 * 1024 * 1024)
                return BadRequest("File too large. Maximum size is 10MB");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            try
            {
                // Upload to storage service
                using var stream = request.File.OpenReadStream();
                var fileUrl = await _fileStorageService.UploadAudioAsync(stream, request.File.FileName);

                // Create database record
                var voiceRecording = new VoiceRecording
                {
                    Name = request.Name ?? Path.GetFileNameWithoutExtension(request.File.FileName),
                    Description = request.Description,
                    Url = fileUrl,
                    FileName = request.File.FileName,
                    FileSizeBytes = request.File.Length,
                    DurationSeconds = request.DurationSeconds,
                    CreatedById = userId!,
                    CreatedAt = DateTime.UtcNow,
                    UsageCount = 0
                };

                _context.VoiceRecordings.Add(voiceRecording);
                await _context.SaveChangesAsync();

                var dto = new VoiceRecordingDto
                {
                    Id = voiceRecording.Id,
                    Name = voiceRecording.Name,
                    Description = voiceRecording.Description,
                    Url = voiceRecording.Url,
                    FileName = voiceRecording.FileName,
                    FileSizeBytes = voiceRecording.FileSizeBytes,
                    DurationSeconds = voiceRecording.DurationSeconds,
                    CreatedAt = voiceRecording.CreatedAt,
                    LastUsedAt = voiceRecording.LastUsedAt,
                    UsageCount = voiceRecording.UsageCount,
                    CreatedBy = "You"
                };

                _logger.LogInformation($"Voice recording uploaded: {voiceRecording.Name} by user {userId}");
                return CreatedAtAction(nameof(GetVoiceRecording), new { id = voiceRecording.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading voice recording");
                return StatusCode(500, "Error uploading file");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateVoiceRecording(int id, UpdateVoiceRecordingRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var recording = await _context.VoiceRecordings.FindAsync(id);
            if (recording == null)
                return NotFound();

            // Check access permissions
            if (userRole != "SuperAdmin" && recording.CreatedById != userId)
                return Forbid();

            recording.Name = request.Name;
            recording.Description = request.Description;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVoiceRecording(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var recording = await _context.VoiceRecordings
                .Include(vr => vr.Campaigns)
                .FirstOrDefaultAsync(vr => vr.Id == id);
                
            if (recording == null)
                return NotFound();

            // Check access permissions
            if (userRole != "SuperAdmin" && recording.CreatedById != userId)
                return Forbid();

            // Check if recording is in use
            if (recording.Campaigns.Any())
                return BadRequest("Cannot delete recording that is used in campaigns");

            try
            {
                // Delete from storage
                await _fileStorageService.DeleteAudioAsync(recording.Url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to delete file from storage: {recording.Url}");
            }

            _context.VoiceRecordings.Remove(recording);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Voice recording deleted: {recording.Name} by user {userId}");
            return NoContent();
        }
    }

    public class VoiceRecordingDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public int? DurationSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public int UsageCount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UploadVoiceRecordingRequest
    {
        public IFormFile File { get; set; } = null!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? DurationSeconds { get; set; }
    }

    public class UpdateVoiceRecordingRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}