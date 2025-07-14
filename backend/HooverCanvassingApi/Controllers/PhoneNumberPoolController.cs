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
        public async Task<ActionResult<List<TwilioPhoneNumber>>> GetPhoneNumbers()
        {
            var numbers = await _phoneNumberPool.GetAllNumbersAsync();
            return Ok(numbers);
        }

        [HttpPost]
        public async Task<ActionResult<List<TwilioPhoneNumber>>> AddPhoneNumbers([FromBody] AddPhoneNumbersRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var phoneNumbers = await _phoneNumberPool.AddPhoneNumbersAsync(request.PhoneNumbers);
                
                _logger.LogInformation($"Added {phoneNumbers.Count} phone numbers to pool");
                return Ok(phoneNumbers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding phone number");
                return StatusCode(500, new { message = "Error adding phone numbers" });
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

    public class AddPhoneNumbersRequest
    {
        [Required]
        public string PhoneNumbers { get; set; } = string.Empty;
    }

    public class UpdatePhoneNumberRequest
    {
        public bool IsActive { get; set; }
        
        [Range(1, 50)]
        public int MaxConcurrentCalls { get; set; }
    }
}