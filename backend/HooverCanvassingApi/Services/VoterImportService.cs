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

        private static VoteFrequency ParseVoteFrequency(string? voteFrequency)
        {
            if (string.IsNullOrWhiteSpace(voteFrequency))
                return VoteFrequency.NonVoter;

            // Parse based on common patterns in voter data
            var freq = voteFrequency.Trim().ToLower();
            
            // Look for numeric patterns (e.g., "3", "4+", etc.)
            if (int.TryParse(freq.Replace("+", ""), out var number))
            {
                return number >= 3 ? VoteFrequency.Frequent : 
                       number >= 1 ? VoteFrequency.Infrequent : 
                       VoteFrequency.NonVoter;
            }

            // Look for text patterns
            return freq switch
            {
                var f when f.Contains("frequent") || f.Contains("high") || f.Contains("regular") => VoteFrequency.Frequent,
                var f when f.Contains("occasional") || f.Contains("sometimes") || f.Contains("moderate") => VoteFrequency.Infrequent,
                var f when f.Contains("never") || f.Contains("none") || f.Contains("non") => VoteFrequency.NonVoter,
                _ => VoteFrequency.NonVoter
            };
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
                // Use Nominatim (OpenStreetMap) for free geocoding
                var fullAddress = $"{address}, {city}, {state} {zip}";
                var encodedAddress = Uri.EscapeDataString(fullAddress);
                var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "HooverCanvassingApp/1.0 (tanveer4hoover@gmail.com)");

                _logger.LogDebug("Geocoding address: {FullAddress}", fullAddress);

                var httpResponse = await _httpClient.GetAsync(url);
                var response = await httpResponse.Content.ReadAsStringAsync();
                
                _logger.LogDebug("Geocoding response status: {StatusCode}", httpResponse.StatusCode);
                _logger.LogDebug("Geocoding response: {Response}", response);
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Geocoding API returned error status {StatusCode}: {Response}", httpResponse.StatusCode, response);
                    return null;
                }
                
                // Check if response looks like HTML (error page)
                if (response.TrimStart().StartsWith("<!"))
                {
                    _logger.LogError("Geocoding API returned HTML instead of JSON: {Response}", response.Substring(0, Math.Min(200, response.Length)));
                    return null;
                }
                
                using var document = JsonDocument.Parse(response);
                var results = document.RootElement;

                if (results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("lat", out var latElement) &&
                        firstResult.TryGetProperty("lon", out var lonElement) &&
                        double.TryParse(latElement.GetString(), out var lat) &&
                        double.TryParse(lonElement.GetString(), out var lon))
                    {
                        _logger.LogDebug("Successfully geocoded {Address} to {Lat}, {Lon}", fullAddress, lat, lon);
                        return (lat, lon);
                    }
                    else
                    {
                        _logger.LogWarning("Geocoding response missing lat/lon for address: {Address}", fullAddress);
                    }
                }
                else
                {
                    _logger.LogWarning("No geocoding results found for address: {Address}", fullAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding address: {Address}", $"{address}, {city}, {state} {zip}");
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

            var batchSize = 100;
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

                        // Add delay after each request to respect Nominatim rate limits (1 req/sec)
                        await Task.Delay(1100);

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
}