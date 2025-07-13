using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HooverCanvassingApi.Services;
using HooverCanvassingApi.Models;
using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class PhoneNumberPoolController : ControllerBase
    {
        private readonly IPhoneNumberPoolService _phoneNumberPool;
        private readonly ILogger<PhoneNumberPoolController> _logger;

        public PhoneNumberPoolController(
            IPhoneNumberPoolService phoneNumberPool,
            ILogger<PhoneNumberPoolController> logger)
        {
            _phoneNumberPool = phoneNumberPool;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<AdditionalPhoneNumber>>> GetPhoneNumbers()
        {
            var numbers = await _phoneNumberPool.GetAllNumbersAsync();
            return Ok(numbers);
        }

        [HttpPost]
        public async Task<ActionResult<AdditionalPhoneNumber>> AddPhoneNumber([FromBody] AddPhoneNumberRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var phoneNumber = await _phoneNumberPool.AddPhoneNumberAsync(
                    request.PhoneNumber, 
                    request.FriendlyName);
                
                _logger.LogInformation($"Added phone number {request.PhoneNumber} to pool");
                return Ok(phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding phone number");
                return StatusCode(500, new { message = "Error adding phone number" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePhoneNumber(int id, [FromBody] UpdatePhoneNumberRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _phoneNumberPool.UpdatePhoneNumberAsync(
                id, 
                request.IsActive, 
                request.MaxConcurrentCalls);
            
            if (!success)
                return NotFound();
            
            _logger.LogInformation($"Updated phone number {id}");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> RemovePhoneNumber(int id)
        {
            var success = await _phoneNumberPool.RemovePhoneNumberAsync(id);
            if (!success)
                return NotFound();
            
            _logger.LogInformation($"Removed phone number {id} from pool");
            return NoContent();
        }
    }

    public class AddPhoneNumberRequest
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        
        public string? FriendlyName { get; set; }
    }

    public class UpdatePhoneNumberRequest
    {
        public bool IsActive { get; set; }
        
        [Range(1, 10)]
        public int MaxConcurrentCalls { get; set; }
    }
}