using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Services
{
    public class VoterMappingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VoterMappingService> _logger;
        private readonly string _connectionString;

        public VoterMappingService(ApplicationDbContext context, ILogger<VoterMappingService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<MappingResult> MapAndImportVotersAsync(string stagingTableName, ColumnMapping mapping)
        {
            var result = new MappingResult
            {
                StartTime = DateTime.UtcNow,
                StagingTableName = stagingTableName
            };

            try
            {
                // Build the SQL query based on mapping
                var sql = BuildMappingQuery(stagingTableName, mapping);
                
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new NpgsqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var voters = new List<Voter>();
                var errors = new List<string>();
                var rowNumber = 0;

                while (await reader.ReadAsync())
                {
                    rowNumber++;
                    try
                    {
                        var voter = MapReaderToVoter(reader, mapping);
                        if (voter != null)
                        {
                            // Check for duplicates
                            var existingVoter = await _context.Voters
                                .FirstOrDefaultAsync(v => 
                                    (v.LalVoterId == voter.LalVoterId && !string.IsNullOrEmpty(voter.LalVoterId)) ||
                                    (v.FirstName == voter.FirstName && 
                                     v.LastName == voter.LastName && 
                                     v.AddressLine == voter.AddressLine));
                            
                            if (existingVoter == null)
                            {
                                voters.Add(voter);
                                result.ProcessedCount++;
                            }
                            else
                            {
                                result.SkippedCount++;
                                _logger.LogDebug("Skipping duplicate voter at row {Row}", rowNumber);
                            }
                        }
                        else
                        {
                            result.SkippedCount++;
                            errors.Add($"Row {rowNumber}: Missing required fields");
                        }

                        // Batch save every 100 records
                        if (voters.Count >= 100)
                        {
                            await _context.Voters.AddRangeAsync(voters);
                            await _context.SaveChangesAsync();
                            result.ImportedCount += voters.Count;
                            voters.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        errors.Add($"Row {rowNumber}: {ex.Message}");
                        _logger.LogWarning(ex, "Error processing row {Row}", rowNumber);
                    }
                }

                // Save remaining voters
                if (voters.Any())
                {
                    await _context.Voters.AddRangeAsync(voters);
                    await _context.SaveChangesAsync();
                    result.ImportedCount += voters.Count;
                }

                result.Success = true;
                result.Errors = errors;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation("Import completed. Imported: {Imported}, Skipped: {Skipped}, Errors: {Errors}",
                    result.ImportedCount, result.SkippedCount, result.ErrorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during voter mapping");
                result.Success = false;
                result.Errors = new List<string> { $"Fatal error: {ex.Message}" };
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        private string BuildMappingQuery(string tableName, ColumnMapping mapping)
        {
            var columns = new List<string>();
            
            // Add mapped columns
            if (!string.IsNullOrEmpty(mapping.VoterIdColumn))
                columns.Add($"\"{mapping.VoterIdColumn}\" as voter_id");
            if (!string.IsNullOrEmpty(mapping.FirstNameColumn))
                columns.Add($"\"{mapping.FirstNameColumn}\" as first_name");
            if (!string.IsNullOrEmpty(mapping.MiddleNameColumn))
                columns.Add($"\"{mapping.MiddleNameColumn}\" as middle_name");
            if (!string.IsNullOrEmpty(mapping.LastNameColumn))
                columns.Add($"\"{mapping.LastNameColumn}\" as last_name");
            if (!string.IsNullOrEmpty(mapping.AddressColumn))
                columns.Add($"\"{mapping.AddressColumn}\" as address");
            if (!string.IsNullOrEmpty(mapping.CityColumn))
                columns.Add($"\"{mapping.CityColumn}\" as city");
            if (!string.IsNullOrEmpty(mapping.StateColumn))
                columns.Add($"\"{mapping.StateColumn}\" as state");
            if (!string.IsNullOrEmpty(mapping.ZipColumn))
                columns.Add($"\"{mapping.ZipColumn}\" as zip");
            if (!string.IsNullOrEmpty(mapping.AgeColumn))
                columns.Add($"\"{mapping.AgeColumn}\" as age");
            if (!string.IsNullOrEmpty(mapping.GenderColumn))
                columns.Add($"\"{mapping.GenderColumn}\" as gender");
            if (!string.IsNullOrEmpty(mapping.PhoneColumn))
                columns.Add($"\"{mapping.PhoneColumn}\" as phone");
            if (!string.IsNullOrEmpty(mapping.EmailColumn))
                columns.Add($"\"{mapping.EmailColumn}\" as email");
            if (!string.IsNullOrEmpty(mapping.PartyColumn))
                columns.Add($"\"{mapping.PartyColumn}\" as party");
            if (!string.IsNullOrEmpty(mapping.VoteFrequencyColumn))
                columns.Add($"\"{mapping.VoteFrequencyColumn}\" as vote_frequency");
            if (!string.IsNullOrEmpty(mapping.EthnicityColumn))
                columns.Add($"\"{mapping.EthnicityColumn}\" as ethnicity");
            if (!string.IsNullOrEmpty(mapping.ReligionColumn))
                columns.Add($"\"{mapping.ReligionColumn}\" as religion");
            if (!string.IsNullOrEmpty(mapping.IncomeColumn))
                columns.Add($"\"{mapping.IncomeColumn}\" as income");
            if (!string.IsNullOrEmpty(mapping.LatitudeColumn))
                columns.Add($"\"{mapping.LatitudeColumn}\" as latitude");
            if (!string.IsNullOrEmpty(mapping.LongitudeColumn))
                columns.Add($"\"{mapping.LongitudeColumn}\" as longitude");
            if (!string.IsNullOrEmpty(mapping.VoterSupportColumn))
                columns.Add($"\"{mapping.VoterSupportColumn}\" as voter_support");
            if (!string.IsNullOrEmpty(mapping.LastContactStatusColumn))
                columns.Add($"\"{mapping.LastContactStatusColumn}\" as last_contact_status");
            if (!string.IsNullOrEmpty(mapping.SmsConsentStatusColumn))
                columns.Add($"\"{mapping.SmsConsentStatusColumn}\" as sms_consent_status");
            
            var columnList = string.Join(", ", columns);
            return $"SELECT {columnList} FROM \"{tableName}\"";
        }

        private Voter? MapReaderToVoter(NpgsqlDataReader reader, ColumnMapping mapping)
        {
            // Require at least first name, last name, and address
            var firstName = GetStringValue(reader, "first_name");
            var lastName = GetStringValue(reader, "last_name");
            var address = GetStringValue(reader, "address");
            
            if (string.IsNullOrWhiteSpace(firstName) || 
                string.IsNullOrWhiteSpace(lastName) || 
                string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            var voter = new Voter
            {
                FirstName = firstName,
                LastName = lastName,
                AddressLine = address,
                MiddleName = GetStringValue(reader, "middle_name"),
                City = GetStringValue(reader, "city") ?? "Unknown",
                State = GetStringValue(reader, "state") ?? "AL",
                Zip = GetStringValue(reader, "zip") ?? "",
                Gender = GetStringValue(reader, "gender") ?? "Unknown",
                Email = GetStringValue(reader, "email"),
                CellPhone = FormatPhoneNumber(GetStringValue(reader, "phone")),
                PartyAffiliation = GetStringValue(reader, "party"),
                Ethnicity = GetStringValue(reader, "ethnicity"),
                Religion = GetStringValue(reader, "religion"),
                Income = GetStringValue(reader, "income"),
                IsContacted = false,
                SmsConsentStatus = SmsConsentStatus.Unknown,
                TotalCampaignContacts = 0,
                SmsCount = 0,
                CallCount = 0
            };

            // Set voter ID
            var voterId = GetStringValue(reader, "voter_id");
            if (!string.IsNullOrEmpty(voterId))
            {
                voter.LalVoterId = voterId;
            }
            else
            {
                // Generate ID if not provided
                voter.LalVoterId = $"IMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20);
            }

            // Parse age
            var ageStr = GetStringValue(reader, "age");
            if (!string.IsNullOrEmpty(ageStr) && int.TryParse(ageStr, out var age))
            {
                voter.Age = age;
            }
            else
            {
                voter.Age = 0;
            }

            // Parse vote frequency
            var voteFreq = GetStringValue(reader, "vote_frequency");
            voter.VoteFrequency = ParseVoteFrequency(voteFreq);

            // Parse latitude
            var latStr = GetStringValue(reader, "latitude");
            if (!string.IsNullOrEmpty(latStr) && double.TryParse(latStr, out var lat))
            {
                voter.Latitude = lat;
            }

            // Parse longitude
            var lonStr = GetStringValue(reader, "longitude");
            if (!string.IsNullOrEmpty(lonStr) && double.TryParse(lonStr, out var lon))
            {
                voter.Longitude = lon;
            }

            // Parse voter support
            var supportStr = GetStringValue(reader, "voter_support");
            if (!string.IsNullOrEmpty(supportStr))
            {
                voter.VoterSupport = ParseVoterSupport(supportStr);
            }

            // Parse last contact status
            var contactStatusStr = GetStringValue(reader, "last_contact_status");
            if (!string.IsNullOrEmpty(contactStatusStr))
            {
                voter.LastContactStatus = ParseContactStatus(contactStatusStr);
            }

            // Parse SMS consent status
            var smsConsentStr = GetStringValue(reader, "sms_consent_status");
            if (!string.IsNullOrEmpty(smsConsentStr))
            {
                voter.SmsConsentStatus = ParseSmsConsentStatus(smsConsentStr);
            }

            return voter;
        }

        private string? GetStringValue(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return null;
                return reader.GetString(ordinal)?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private VoteFrequency ParseVoteFrequency(string? frequency)
        {
            if (string.IsNullOrEmpty(frequency))
                return VoteFrequency.NonVoter;
                
            var normalized = frequency.ToLower().Trim();
            if (normalized.Contains("frequent") || normalized.Contains("always"))
                return VoteFrequency.Frequent;
            if (normalized.Contains("infrequent") || normalized.Contains("sometimes"))
                return VoteFrequency.Infrequent;
                
            return VoteFrequency.NonVoter;
        }

        private VoterSupport? ParseVoterSupport(string? support)
        {
            if (string.IsNullOrEmpty(support))
                return null;

            var normalized = support.ToLower().Trim();
            
            if (normalized.Contains("strong") && normalized.Contains("yes"))
                return VoterSupport.StrongYes;
            if (normalized.Contains("leaning") && normalized.Contains("yes"))
                return VoterSupport.LeaningYes;
            if (normalized.Contains("undecided"))
                return VoterSupport.Undecided;
            if (normalized.Contains("leaning") && normalized.Contains("no"))
                return VoterSupport.LeaningNo;
            if (normalized.Contains("strong") && normalized.Contains("no"))
                return VoterSupport.StrongNo;

            // Try to parse enum directly
            if (Enum.TryParse<VoterSupport>(support, true, out var result))
                return result;

            return null;
        }

        private ContactStatus? ParseContactStatus(string? status)
        {
            if (string.IsNullOrEmpty(status))
                return null;

            var normalized = status.ToLower().Trim();
            
            if (normalized.Contains("reached"))
                return ContactStatus.Reached;
            if (normalized.Contains("not") && normalized.Contains("home"))
                return ContactStatus.NotHome;
            if (normalized.Contains("refused"))
                return ContactStatus.Refused;
            if (normalized.Contains("follow"))
                return ContactStatus.NeedsFollowUp;

            // Try to parse enum directly
            if (Enum.TryParse<ContactStatus>(status, true, out var result))
                return result;

            return null;
        }

        private SmsConsentStatus ParseSmsConsentStatus(string? status)
        {
            if (string.IsNullOrEmpty(status))
                return SmsConsentStatus.Unknown;

            var normalized = status.ToLower().Trim();
            
            if (normalized.Contains("opted") && normalized.Contains("in"))
                return SmsConsentStatus.OptedIn;
            if (normalized.Contains("opted") && normalized.Contains("out"))
                return SmsConsentStatus.OptedOut;

            // Try to parse enum directly
            if (Enum.TryParse<SmsConsentStatus>(status, true, out var result))
                return result;

            return SmsConsentStatus.Unknown;
        }

        private string? FormatPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
            {
                return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
            }
            
            return phone.Trim();
        }

        public async Task<List<string>> GetAvailableColumnsAsync(string stagingTableName)
        {
            var sql = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = @tableName 
                AND column_name NOT IN ('id', 'imported_at')
                ORDER BY ordinal_position";
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tableName", stagingTableName);
            using var reader = await command.ExecuteReaderAsync();
            
            var columns = new List<string>();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }
            
            return columns;
        }
    }

    public class ColumnMapping
    {
        public string? VoterIdColumn { get; set; }
        public string? FirstNameColumn { get; set; }
        public string? MiddleNameColumn { get; set; }
        public string? LastNameColumn { get; set; }
        public string? AddressColumn { get; set; }
        public string? CityColumn { get; set; }
        public string? StateColumn { get; set; }
        public string? ZipColumn { get; set; }
        public string? AgeColumn { get; set; }
        public string? GenderColumn { get; set; }
        public string? PhoneColumn { get; set; }
        public string? EmailColumn { get; set; }
        public string? PartyColumn { get; set; }
        public string? VoteFrequencyColumn { get; set; }
        public string? EthnicityColumn { get; set; }
        public string? ReligionColumn { get; set; }
        public string? IncomeColumn { get; set; }
        public string? LatitudeColumn { get; set; }
        public string? LongitudeColumn { get; set; }
        public string? VoterSupportColumn { get; set; }
        public string? LastContactStatusColumn { get; set; }
        public string? SmsConsentStatusColumn { get; set; }
    }

    public class MappingResult
    {
        public bool Success { get; set; }
        public string StagingTableName { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
    }
}