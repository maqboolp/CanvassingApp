using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Services
{
    public class VoterImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VoterImportService> _logger;
        private readonly HttpClient _httpClient;

        public VoterImportService(ApplicationDbContext context, ILogger<VoterImportService> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<ImportResult> ImportVotersFromCsvAsync(Stream csvStream, bool enableGeocoding = true)
        {
            var result = new ImportResult();
            var voters = new List<Voter>();

            try
            {
                using var reader = new StreamReader(csvStream);
                
                // Peek at the header to determine format
                var firstLine = reader.ReadLine();
                if (string.IsNullOrEmpty(firstLine))
                {
                    result.Errors.Add("CSV file is empty");
                    return result;
                }
                
                // Reset stream
                csvStream.Position = 0;
                reader.DiscardBufferedData();
                
                // Check if it's the simplified format
                if (firstLine.Contains("FirstName") && firstLine.Contains("LastName") && !firstLine.Contains("LALVOTERID"))
                {
                    return await ImportSimplifiedCsvAsync(csvStream, enableGeocoding);
                }
                
                // Otherwise use the original format
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<VoterCsvMap>();

                await foreach (var record in csv.GetRecordsAsync<VoterCsvRecord>())
                {
                    try
                    {
                        var voter = await ConvertCsvRecordToVoter(record, enableGeocoding);
                        if (voter != null)
                        {
                            voters.Add(voter);
                            result.ProcessedCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process voter record: {VoterId}", record.LALVOTERID);
                        result.ErrorCount++;
                        result.Errors.Add($"Row {result.ProcessedCount + result.SkippedCount + result.ErrorCount}: {ex.Message}");
                    }

                    // Add delay for geocoding rate limiting
                    if (enableGeocoding && result.ProcessedCount % 10 == 0)
                    {
                        await Task.Delay(1000); // 1 second delay every 10 records
                    }
                }

                // Batch insert voters
                if (voters.Any())
                {
                    await _context.Voters.AddRangeAsync(voters);
                    await _context.SaveChangesAsync();
                    result.ImportedCount = voters.Count;
                }

                _logger.LogInformation("Voter import completed. Imported: {ImportedCount}, Errors: {ErrorCount}, Skipped: {SkippedCount}",
                    result.ImportedCount, result.ErrorCount, result.SkippedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during voter import");
                result.Errors.Add($"Fatal error: {ex.Message}");
                throw;
            }

            return result;
        }

        private async Task<ImportResult> ImportSimplifiedCsvAsync(Stream csvStream, bool enableGeocoding)
        {
            var result = new ImportResult();
            var voters = new List<Voter>();

            try
            {
                using var reader = new StreamReader(csvStream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                csv.Context.RegisterClassMap<SimplifiedVoterCsvMap>();

                await foreach (var record in csv.GetRecordsAsync<SimplifiedVoterCsvRecord>())
                {
                    try
                    {
                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(record.FirstName) ||
                            string.IsNullOrWhiteSpace(record.LastName) ||
                            string.IsNullOrWhiteSpace(record.AddressLine) ||
                            string.IsNullOrWhiteSpace(record.City) ||
                            string.IsNullOrWhiteSpace(record.State) ||
                            string.IsNullOrWhiteSpace(record.Zip))
                        {
                            result.SkippedCount++;
                            result.Errors.Add($"Row {result.ProcessedCount + result.SkippedCount + result.ErrorCount}: Missing required fields");
                            continue;
                        }

                        // Generate unique voter ID
                        var voterId = $"IMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
                        
                        // Check if a voter with same name and address already exists
                        var existingVoter = await _context.Voters.FirstOrDefaultAsync(v => 
                            v.FirstName == record.FirstName && 
                            v.LastName == record.LastName && 
                            v.AddressLine == record.AddressLine);
                            
                        if (existingVoter != null)
                        {
                            result.SkippedCount++;
                            _logger.LogInformation("Skipping duplicate voter: {FirstName} {LastName} at {Address}", 
                                record.FirstName, record.LastName, record.AddressLine);
                            continue;
                        }

                        var voter = new Voter
                        {
                            LalVoterId = voterId,
                            FirstName = record.FirstName,
                            LastName = record.LastName,
                            AddressLine = record.AddressLine,
                            City = record.City,
                            State = record.State,
                            Zip = record.Zip,
                            Age = record.Age ?? 0,
                            Gender = record.Gender ?? "Unknown",
                            CellPhone = record.CellPhone,
                            Email = record.Email,
                            VoteFrequency = ParseVoteFrequency(record.VoteFrequency),
                            PartyAffiliation = record.PartyAffiliation,
                            IsContacted = false,
                            SmsConsentStatus = SmsConsentStatus.Unknown,
                            TotalCampaignContacts = 0,
                            SmsCount = 0,
                            CallCount = 0
                        };

                        // Geocode if enabled
                        if (enableGeocoding && !string.IsNullOrEmpty(voter.AddressLine))
                        {
                            var coordinates = await GeocodeAddress(voter.AddressLine, voter.City, voter.State, voter.Zip);
                            if (coordinates != null)
                            {
                                voter.Latitude = coordinates.Value.Latitude;
                                voter.Longitude = coordinates.Value.Longitude;
                            }
                        }

                        voters.Add(voter);
                        result.ProcessedCount++;

                        // Add delay for geocoding rate limiting
                        if (enableGeocoding && result.ProcessedCount % 10 == 0)
                        {
                            await Task.Delay(1000); // 1 second delay every 10 records
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process voter record: {FirstName} {LastName}", 
                            record.FirstName, record.LastName);
                        result.ErrorCount++;
                        result.Errors.Add($"Row {result.ProcessedCount + result.SkippedCount + result.ErrorCount}: {ex.Message}");
                    }
                }

                // Batch insert voters
                if (voters.Any())
                {
                    await _context.Voters.AddRangeAsync(voters);
                    await _context.SaveChangesAsync();
                    result.ImportedCount = voters.Count;
                }

                _logger.LogInformation("Simplified CSV import completed. Imported: {ImportedCount}, Errors: {ErrorCount}, Skipped: {SkippedCount}",
                    result.ImportedCount, result.ErrorCount, result.SkippedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during simplified CSV import");
                result.Errors.Add($"Fatal error: {ex.Message}");
                throw;
            }

            return result;
        }

        private VoteFrequency ParseVoteFrequency(string? frequency)
        {
            if (string.IsNullOrEmpty(frequency))
                return VoteFrequency.NonVoter;
                
            return frequency.ToLower() switch
            {
                "frequent" => VoteFrequency.Frequent,
                "infrequent" => VoteFrequency.Infrequent,
                "nonvoter" => VoteFrequency.NonVoter,
                "non-voter" => VoteFrequency.NonVoter,
                _ => VoteFrequency.NonVoter
            };
        }

        private async Task<Voter?> ConvertCsvRecordToVoter(VoterCsvRecord record, bool enableGeocoding)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(record.LALVOTERID) ||
                string.IsNullOrWhiteSpace(record.Voters_FirstName) ||
                string.IsNullOrWhiteSpace(record.Voters_LastName) ||
                string.IsNullOrWhiteSpace(record.Residence_Addresses_AddressLine))
            {
                return null;
            }

            // Check if voter already exists
            var existingVoter = await _context.Voters.FirstOrDefaultAsync(v => v.LalVoterId == record.LALVOTERID);
            if (existingVoter != null)
            {
                return null; // Skip duplicates
            }

            var voter = new Voter
            {
                LalVoterId = record.LALVOTERID,
                FirstName = record.Voters_FirstName?.Trim() ?? string.Empty,
                MiddleName = string.IsNullOrWhiteSpace(record.Voters_MiddleName) ? null : record.Voters_MiddleName.Trim(),
                LastName = record.Voters_LastName?.Trim() ?? string.Empty,
                AddressLine = record.Residence_Addresses_AddressLine?.Trim() ?? string.Empty,
                City = record.Residence_Addresses_City?.Trim() ?? "Hoover",
                State = record.Residence_Addresses_State?.Trim() ?? "AL",
                Zip = record.Residence_Addresses_Zip?.Trim() ?? string.Empty,
                Age = record.Voters_Age ?? 0,
                Ethnicity = string.IsNullOrWhiteSpace(record.EthnicGroups_EthnicGroup1Desc) ? null : record.EthnicGroups_EthnicGroup1Desc.Trim(),
                Gender = record.Voters_Gender?.Trim() ?? "Unknown",
                VoteFrequency = ParseVoteFrequency(record.Vote_Frequency),
                CellPhone = FormatPhoneNumber(record.VoterTelephones_CellPhoneUnformatted),
                Email = string.IsNullOrWhiteSpace(record.email) ? null : record.email.Trim()
            };

            // Geocode address if enabled
            if (enableGeocoding)
            {
                var coordinates = await GeocodeAddress(voter.AddressLine, voter.City, voter.State, voter.Zip);
                if (coordinates != null)
                {
                    voter.Latitude = coordinates.Value.Latitude;
                    voter.Longitude = coordinates.Value.Longitude;
                }
            }

            return voter;
        }


        private static string? FormatPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Remove all non-digit characters
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            // Format as (XXX) XXX-XXXX if 10 digits
            if (digits.Length == 10)
            {
                return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
            }

            // Return original if not 10 digits
            return phone.Trim();
        }

        private async Task<(double Latitude, double Longitude)?> GeocodeAddress(string address, string city, string state, string zip)
        {
            try
            {
                // Add delay to respect rate limits (1 request per second)
                await Task.Delay(1000);

                // Try with original city first
                var result = await TryGeocode(address, city, state, zip);
                if (result.HasValue)
                {
                    return result;
                }

                // If original city fails and it's Birmingham, try with Hoover as fallback
                if (city.Equals("Birmingham", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Trying Hoover as fallback for Birmingham address: {Address}", $"{address}, {city}, {state} {zip}");
                    result = await TryGeocode(address, "Hoover", state, zip);
                    if (result.HasValue)
                    {
                        return result;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while geocoding address: {Address}", $"{address}, {city}, {state} {zip}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while geocoding address: {Address}", $"{address}, {city}, {state} {zip}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while geocoding address: {Address}", $"{address}, {city}, {state} {zip}");
            }

            return null;
        }

        private async Task<(double Latitude, double Longitude)?> TryGeocode(string address, string city, string state, string zip)
        {
            try
            {
                // Use Google Geocoding API for better accuracy and speed
                var googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_GEOCODING_API_KEY");
                _logger.LogInformation("Google API Key check: Key is {Status}, Length: {Length}", 
                    string.IsNullOrEmpty(googleApiKey) ? "MISSING" : "PRESENT", 
                    googleApiKey?.Length ?? 0);
                    
                if (string.IsNullOrEmpty(googleApiKey))
                {
                    _logger.LogError("Google Geocoding API key not configured. Set GOOGLE_GEOCODING_API_KEY environment variable.");
                    return null;
                }

                var fullAddress = $"{address}, {city}, {state} {zip}";
                var encodedAddress = Uri.EscapeDataString(fullAddress);
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={googleApiKey}";

                _httpClient.DefaultRequestHeaders.Clear();

                _logger.LogDebug("Geocoding address with Google: {FullAddress}", fullAddress);

                var httpResponse = await _httpClient.GetAsync(url);
                var response = await httpResponse.Content.ReadAsStringAsync();
                
                _logger.LogDebug("Google geocoding response status: {StatusCode}", httpResponse.StatusCode);
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Google Geocoding API returned error status {StatusCode}: {Response}", httpResponse.StatusCode, response);
                    return null;
                }
                
                using var document = JsonDocument.Parse(response);
                var root = document.RootElement;

                if (root.TryGetProperty("status", out var statusElement))
                {
                    var status = statusElement.GetString();
                    
                    if (status == "OK" && root.TryGetProperty("results", out var resultsElement) && resultsElement.GetArrayLength() > 0)
                    {
                        var firstResult = resultsElement[0];
                        if (firstResult.TryGetProperty("geometry", out var geometry) &&
                            geometry.TryGetProperty("location", out var location) &&
                            location.TryGetProperty("lat", out var latElement) &&
                            location.TryGetProperty("lng", out var lngElement))
                        {
                            var lat = latElement.GetDouble();
                            var lng = lngElement.GetDouble();
                            
                            _logger.LogDebug("Successfully geocoded {Address} to {Lat}, {Lng}", fullAddress, lat, lng);
                            return (lat, lng);
                        }
                        else
                        {
                            _logger.LogWarning("Google geocoding response missing geometry for address: {Address}", fullAddress);
                        }
                    }
                    else if (status == "ZERO_RESULTS")
                    {
                        _logger.LogWarning("No geocoding results found for address: {Address}", fullAddress);
                    }
                    else if (status == "OVER_QUERY_LIMIT")
                    {
                        _logger.LogError("Google Geocoding API quota exceeded");
                        return null;
                    }
                    else
                    {
                        _logger.LogWarning("Google geocoding returned status {Status} for address: {Address}", status, fullAddress);
                    }
                }
                else
                {
                    _logger.LogError("Invalid response format from Google Geocoding API: {Response}", response.Substring(0, Math.Min(200, response.Length)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding address with Google: {Address}", $"{address}, {city}, {state} {zip}");
            }

            return null;
        }

        public async Task<GeocodingResult> GeocodeExistingVotersAsync()
        {
            var result = new GeocodingResult();
            
            var votersToGeocode = await _context.Voters
                .Where(v => !v.Latitude.HasValue || !v.Longitude.HasValue)
                .ToListAsync();

            result.ProcessedCount = votersToGeocode.Count;
            _logger.LogInformation("Starting geocoding for {Count} voters without coordinates", votersToGeocode.Count);

            var batchSize = 5;
            var processed = 0;

            for (int i = 0; i < votersToGeocode.Count; i += batchSize)
            {
                var batch = votersToGeocode.Skip(i).Take(batchSize).ToList();
                
                foreach (var voter in batch)
                {
                    try
                    {
                        var coordinates = await GeocodeAddress(voter.AddressLine, voter.City, voter.State, voter.Zip);
                        if (coordinates != null)
                        {
                            voter.Latitude = coordinates.Value.Latitude;
                            voter.Longitude = coordinates.Value.Longitude;
                            result.GeocodedCount++;
                        }
                        else
                        {
                            result.FailedCount++;
                        }

                        processed++;

                        // Add small delay for Google API (can handle ~50 req/sec)
                        await Task.Delay(50);

                        // Progress update every 10 records
                        if (processed % 10 == 0)
                        {
                            _logger.LogInformation("Geocoded {Processed}/{Total} voters ({Percentage:F1}%)", 
                                processed, votersToGeocode.Count, (double)processed / votersToGeocode.Count * 100);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error geocoding voter {VoterId}", voter.LalVoterId);
                        result.FailedCount++;
                    }
                }

                // Save batch
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved batch {BatchStart}-{BatchEnd}", i + 1, Math.Min(i + batchSize, votersToGeocode.Count));
            }

            _logger.LogInformation("Geocoding completed. Processed: {Processed}, Success: {Success}, Failed: {Failed}",
                result.ProcessedCount, result.GeocodedCount, result.FailedCount);

            return result;
        }
    }

    public class VoterCsvRecord
    {
        public string LALVOTERID { get; set; } = string.Empty;
        public string? Voters_FirstName { get; set; }
        public string? Voters_MiddleName { get; set; }
        public string? Voters_LastName { get; set; }
        public string? Residence_Addresses_AddressLine { get; set; }
        public string? Residence_Addresses_City { get; set; }
        public string? Residence_Addresses_State { get; set; }
        public string? Residence_Addresses_Zip { get; set; }
        public int? Voters_Age { get; set; }
        public string? EthnicGroups_EthnicGroup1Desc { get; set; }
        public string? Voters_Gender { get; set; }
        public string? Vote_Frequency { get; set; }
        public string? VoterTelephones_CellPhoneUnformatted { get; set; }
        public string? email { get; set; }
    }

    public class VoterCsvMap : ClassMap<VoterCsvRecord>
    {
        public VoterCsvMap()
        {
            Map(m => m.LALVOTERID).Name("LALVOTERID");
            Map(m => m.Voters_FirstName).Name("Voters_FirstName");
            Map(m => m.Voters_MiddleName).Name("Voters_MiddleName");
            Map(m => m.Voters_LastName).Name("Voters_LastName");
            Map(m => m.Residence_Addresses_AddressLine).Name("Residence_Addresses_AddressLine");
            Map(m => m.Residence_Addresses_City).Name("Residence_Addresses_City");
            Map(m => m.Residence_Addresses_State).Name("Residence_Addresses_State");
            Map(m => m.Residence_Addresses_Zip).Name("Residence_Addresses_Zip");
            Map(m => m.Voters_Age).Name("Voters_Age");
            Map(m => m.EthnicGroups_EthnicGroup1Desc).Name("EthnicGroups_EthnicGroup1Desc");
            Map(m => m.Voters_Gender).Name("Voters_Gender");
            Map(m => m.Vote_Frequency).Name("Vote_Frequency");
            Map(m => m.VoterTelephones_CellPhoneUnformatted).Name("VoterTelephones_CellPhoneUnformatted");
            Map(m => m.email).Name("email");
        }
    }

    public class ImportResult
    {
        public int ProcessedCount { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
    }

    public class GeocodingResult
    {
        public int ProcessedCount { get; set; }
        public int GeocodedCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
    }

    public class SimplifiedVoterCsvRecord
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? CellPhone { get; set; }
        public string? Email { get; set; }
        public string? VoteFrequency { get; set; }
        public string? PartyAffiliation { get; set; }
    }

    public class SimplifiedVoterCsvMap : ClassMap<SimplifiedVoterCsvRecord>
    {
        public SimplifiedVoterCsvMap()
        {
            Map(m => m.FirstName).Name("FirstName");
            Map(m => m.LastName).Name("LastName");
            Map(m => m.AddressLine).Name("AddressLine");
            Map(m => m.City).Name("City");
            Map(m => m.State).Name("State");
            Map(m => m.Zip).Name("Zip");
            Map(m => m.Age).Name("Age");
            Map(m => m.Gender).Name("Gender");
            Map(m => m.CellPhone).Name("CellPhone");
            Map(m => m.Email).Name("Email");
            Map(m => m.VoteFrequency).Name("VoteFrequency");
            Map(m => m.PartyAffiliation).Name("PartyAffiliation");
        }
    }
}