using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using System.Text;

namespace HooverCanvassingApi.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class OptOutController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OptOutController> _logger;

        public OptOutController(ApplicationDbContext context, ILogger<OptOutController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OptOutRecordDto>>> GetOptOuts([FromQuery] OptOutFilterRequest request)
        {
            try
            {
                var query = _context.OptOutRecords
                    .Include(o => o.Voter)
                    .AsQueryable();

                // Filter by type if specified
                if (request.Type.HasValue)
                {
                    query = query.Where(o => o.Type == request.Type.Value);
                }

                // Filter by method if specified
                if (request.Method.HasValue)
                {
                    query = query.Where(o => o.Method == request.Method.Value);
                }

                // Filter by date range
                if (request.StartDate.HasValue)
                {
                    query = query.Where(o => o.OptedOutAt >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(o => o.OptedOutAt <= request.EndDate.Value);
                }

                // Search by phone number
                if (!string.IsNullOrEmpty(request.SearchPhone))
                {
                    var normalizedSearch = NormalizePhoneNumber(request.SearchPhone);
                    query = query.Where(o => o.PhoneNumber.Contains(normalizedSearch));
                }

                // Sorting
                query = request.SortBy?.ToLower() switch
                {
                    "phone" => request.SortDescending ? query.OrderByDescending(o => o.PhoneNumber) : query.OrderBy(o => o.PhoneNumber),
                    "type" => request.SortDescending ? query.OrderByDescending(o => o.Type) : query.OrderBy(o => o.Type),
                    "method" => request.SortDescending ? query.OrderByDescending(o => o.Method) : query.OrderBy(o => o.Method),
                    _ => request.SortDescending ? query.OrderByDescending(o => o.OptedOutAt) : query.OrderBy(o => o.OptedOutAt)
                };

                // Pagination
                var totalCount = await query.CountAsync();
                var pageSize = request.PageSize ?? 50;
                var pageNumber = request.PageNumber ?? 1;
                
                var optOuts = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OptOutRecordDto
                    {
                        Id = o.Id,
                        PhoneNumber = FormatPhoneNumber(o.PhoneNumber),
                        Type = o.Type,
                        Method = o.Method,
                        OptedOutAt = o.OptedOutAt,
                        Reason = o.Reason,
                        VoterId = o.VoterId,
                        VoterName = o.Voter != null ? $"{o.Voter.FirstName} {o.Voter.LastName}" : null,
                        VoterAddress = o.Voter != null ? $"{o.Voter.AddressLine}, {o.Voter.City}, {o.Voter.State} {o.Voter.Zip}" : null
                    })
                    .ToListAsync();

                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                Response.Headers.Append("X-Page-Number", pageNumber.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(optOuts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving opt-out records");
                return StatusCode(500, new { error = "Failed to retrieve opt-out records" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OptOutRecordDto>> GetOptOut(int id)
        {
            try
            {
                var optOut = await _context.OptOutRecords
                    .Include(o => o.Voter)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (optOut == null)
                    return NotFound();

                var dto = new OptOutRecordDto
                {
                    Id = optOut.Id,
                    PhoneNumber = FormatPhoneNumber(optOut.PhoneNumber),
                    Type = optOut.Type,
                    Method = optOut.Method,
                    OptedOutAt = optOut.OptedOutAt,
                    Reason = optOut.Reason,
                    VoterId = optOut.VoterId,
                    VoterName = optOut.Voter != null ? $"{optOut.Voter.FirstName} {optOut.Voter.LastName}" : null,
                    VoterAddress = optOut.Voter != null ? $"{optOut.Voter.AddressLine}, {optOut.Voter.City}, {optOut.Voter.State} {optOut.Voter.Zip}" : null
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving opt-out record {id}");
                return StatusCode(500, new { error = "Failed to retrieve opt-out record" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<OptOutRecordDto>> AddOptOut(AddOptOutRequest request)
        {
            try
            {
                var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
                
                // Check if already opted out
                var existing = await _context.OptOutRecords
                    .FirstOrDefaultAsync(o => o.PhoneNumber == normalizedPhone);
                
                if (existing != null)
                {
                    return BadRequest(new { error = "This phone number is already opted out" });
                }

                // Try to find matching voter
                var phoneVariants = GetPhoneVariants(normalizedPhone);
                var voter = await _context.Voters
                    .FirstOrDefaultAsync(v => phoneVariants.Contains(v.CellPhone));

                var optOut = new OptOutRecord
                {
                    PhoneNumber = normalizedPhone,
                    Type = request.Type,
                    Method = OptOutMethod.Manual,
                    OptedOutAt = DateTime.UtcNow,
                    Reason = request.Reason,
                    VoterId = voter?.LalVoterId
                };

                _context.OptOutRecords.Add(optOut);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Manually added opt-out for {normalizedPhone}");

                var dto = new OptOutRecordDto
                {
                    Id = optOut.Id,
                    PhoneNumber = FormatPhoneNumber(optOut.PhoneNumber),
                    Type = optOut.Type,
                    Method = optOut.Method,
                    OptedOutAt = optOut.OptedOutAt,
                    Reason = optOut.Reason,
                    VoterId = optOut.VoterId,
                    VoterName = voter != null ? $"{voter.FirstName} {voter.LastName}" : null
                };

                return CreatedAtAction(nameof(GetOptOut), new { id = optOut.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding opt-out record");
                return StatusCode(500, new { error = "Failed to add opt-out record" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> RemoveOptOut(int id)
        {
            try
            {
                var optOut = await _context.OptOutRecords.FindAsync(id);
                
                if (optOut == null)
                    return NotFound();

                _context.OptOutRecords.Remove(optOut);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Removed opt-out for {optOut.PhoneNumber}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing opt-out record {id}");
                return StatusCode(500, new { error = "Failed to remove opt-out record" });
            }
        }

        [HttpGet("export")]
        public async Task<ActionResult> ExportOptOuts([FromQuery] OptOutFilterRequest request)
        {
            try
            {
                var query = _context.OptOutRecords
                    .Include(o => o.Voter)
                    .AsQueryable();

                // Apply same filters as GetOptOuts
                if (request.Type.HasValue)
                    query = query.Where(o => o.Type == request.Type.Value);

                if (request.Method.HasValue)
                    query = query.Where(o => o.Method == request.Method.Value);

                if (request.StartDate.HasValue)
                    query = query.Where(o => o.OptedOutAt >= request.StartDate.Value);

                if (request.EndDate.HasValue)
                    query = query.Where(o => o.OptedOutAt <= request.EndDate.Value);

                var optOuts = await query.OrderBy(o => o.OptedOutAt).ToListAsync();

                // Generate CSV
                var csv = new StringBuilder();
                csv.AppendLine("Phone Number,Type,Method,Opted Out Date,Reason,Voter Name,Voter Address");

                foreach (var optOut in optOuts)
                {
                    var voterName = optOut.Voter != null ? $"{optOut.Voter.FirstName} {optOut.Voter.LastName}" : "";
                    var voterAddress = optOut.Voter != null ? $"{optOut.Voter.AddressLine} {optOut.Voter.City} {optOut.Voter.State} {optOut.Voter.Zip}" : "";
                    
                    csv.AppendLine($"{FormatPhoneNumber(optOut.PhoneNumber)},{optOut.Type},{optOut.Method},{optOut.OptedOutAt:yyyy-MM-dd HH:mm:ss},\"{optOut.Reason ?? ""}\",\"{voterName}\",\"{voterAddress}\"");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"opt-outs-{DateTime.Now:yyyy-MM-dd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting opt-out records");
                return StatusCode(500, new { error = "Failed to export opt-out records" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<OptOutStats>> GetOptOutStats()
        {
            try
            {
                var stats = new OptOutStats
                {
                    TotalOptOuts = await _context.OptOutRecords.CountAsync(),
                    RoboCallOptOuts = await _context.OptOutRecords.CountAsync(o => o.Type == OptOutType.RoboCalls),
                    SmsOptOuts = await _context.OptOutRecords.CountAsync(o => o.Type == OptOutType.SMS),
                    AllOptOuts = await _context.OptOutRecords.CountAsync(o => o.Type == OptOutType.All),
                    PhoneOptOuts = await _context.OptOutRecords.CountAsync(o => o.Method == OptOutMethod.Phone),
                    SmsMethodOptOuts = await _context.OptOutRecords.CountAsync(o => o.Method == OptOutMethod.SMS),
                    ManualOptOuts = await _context.OptOutRecords.CountAsync(o => o.Method == OptOutMethod.Manual),
                    WebOptOuts = await _context.OptOutRecords.CountAsync(o => o.Method == OptOutMethod.Web),
                    Last30Days = await _context.OptOutRecords.CountAsync(o => o.OptedOutAt >= DateTime.UtcNow.AddDays(-30)),
                    Last7Days = await _context.OptOutRecords.CountAsync(o => o.OptedOutAt >= DateTime.UtcNow.AddDays(-7)),
                    Today = await _context.OptOutRecords.CountAsync(o => o.OptedOutAt >= DateTime.UtcNow.Date)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving opt-out statistics");
                return StatusCode(500, new { error = "Failed to retrieve opt-out statistics" });
            }
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;
                
            // Remove all non-numeric characters
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            
            // If it's 11 digits and starts with 1, remove the 1
            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                digitsOnly = digitsOnly.Substring(1);
            }
            
            // Return just the 10-digit number for consistent comparison
            return digitsOnly;
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length != 10)
                return phoneNumber;

            return $"({phoneNumber.Substring(0, 3)}) {phoneNumber.Substring(3, 3)}-{phoneNumber.Substring(6)}";
        }

        private List<string> GetPhoneVariants(string normalizedPhone)
        {
            if (string.IsNullOrEmpty(normalizedPhone) || normalizedPhone.Length != 10)
                return new List<string> { normalizedPhone };

            return new List<string>
            {
                normalizedPhone,                                    // 2055551234
                $"1{normalizedPhone}",                             // 12055551234
                $"+1{normalizedPhone}",                            // +12055551234
                $"({normalizedPhone.Substring(0, 3)}) {normalizedPhone.Substring(3, 3)}-{normalizedPhone.Substring(6)}", // (205) 555-1234
                $"{normalizedPhone.Substring(0, 3)}-{normalizedPhone.Substring(3, 3)}-{normalizedPhone.Substring(6)}"    // 205-555-1234
            };
        }
    }

    public class OptOutRecordDto
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public OptOutType Type { get; set; }
        public OptOutMethod Method { get; set; }
        public DateTime OptedOutAt { get; set; }
        public string? Reason { get; set; }
        public string? VoterId { get; set; }
        public string? VoterName { get; set; }
        public string? VoterAddress { get; set; }
    }

    public class OptOutFilterRequest
    {
        public OptOutType? Type { get; set; }
        public OptOutMethod? Method { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchPhone { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }

    public class AddOptOutRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public OptOutType Type { get; set; } = OptOutType.All;
        public string? Reason { get; set; }
    }

    public class OptOutStats
    {
        public int TotalOptOuts { get; set; }
        public int RoboCallOptOuts { get; set; }
        public int SmsOptOuts { get; set; }
        public int AllOptOuts { get; set; }
        public int PhoneOptOuts { get; set; }
        public int SmsMethodOptOuts { get; set; }
        public int ManualOptOuts { get; set; }
        public int WebOptOuts { get; set; }
        public int Last30Days { get; set; }
        public int Last7Days { get; set; }
        public int Today { get; set; }
    }
}