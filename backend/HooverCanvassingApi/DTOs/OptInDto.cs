using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.DTOs
{
    public class OptInRequest
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        public bool ConsentGiven { get; set; }
        
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? ZipCode { get; set; }
    }
    
    public class OptInResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? VoterId { get; set; }
    }
    
    public class SmsWebhookRequest
    {
        public string MessageSid { get; set; } = string.Empty;
        public string AccountSid { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int NumMedia { get; set; }
    }
    
    public class OptInStatusResponse
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string ConsentStatus { get; set; } = string.Empty;
        public DateTime? OptInDate { get; set; }
        public DateTime? OptOutDate { get; set; }
        public string? OptInMethod { get; set; }
    }
}