using System.ComponentModel.DataAnnotations;

namespace HooverCanvassingApi.Models
{
    public class VoterImportRaw
    {
        [Key]
        public int Id { get; set; }
        public int? ImportBatchId { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public bool IsProcessed { get; set; } = false;
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessingError { get; set; }
        
        // Core voter fields
        public string? LALVOTERID { get; set; }
        public string? Voters_FirstName { get; set; }
        public string? Voters_MiddleName { get; set; }
        public string? Voters_LastName { get; set; }
        public string? Voters_Age { get; set; }
        public string? Voters_Gender { get; set; }
        public string? Voters_BirthDate { get; set; }
        
        // Address fields
        public string? Residence_Addresses_AddressLine { get; set; }
        public string? Residence_Addresses_ExtraAddressLine { get; set; }
        public string? Residence_Addresses_City { get; set; }
        public string? Residence_Addresses_State { get; set; }
        public string? Residence_Addresses_Zip { get; set; }
        public string? Residence_Addresses_ZipPlus4 { get; set; }
        
        // Contact fields
        public string? VoterTelephones_CellPhoneFormatted { get; set; }
        public string? VoterTelephones_LandlineFormatted { get; set; }
        public string? Voters_Email { get; set; }
        
        // Political data
        public string? Parties_Description { get; set; }
        public string? VotingPerformanceEvenYearGeneral { get; set; }
        public string? VotingPerformanceEvenYearPrimary { get; set; }
        public string? Vote_Frequency { get; set; }
        
        // Census/Demographic data
        public string? Ethnic_Description { get; set; }
        public string? ConsumerData_Education_of_Person { get; set; }
        public string? ConsumerData_Occupation_of_Person { get; set; }
        public string? ConsumerData_Estimated_Income_Range { get; set; }
        
        // Store the entire row as JSON for fields we don't map
        public string? RawData { get; set; }
    }

    public class VoterImportBatch
    {
        [Key]
        public int Id { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public string? ImportedBy { get; set; }
        public string? FileName { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        public string? Status { get; set; } // Pending, Processing, Completed, Failed
        public string? Notes { get; set; }
        
        public ICollection<VoterImportRaw> RawRecords { get; set; } = new List<VoterImportRaw>();
    }
}