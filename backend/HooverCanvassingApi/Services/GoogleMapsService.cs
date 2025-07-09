using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace HooverCanvassingApi.Services
{
    public interface IGoogleMapsService
    {
        Task<DistanceResult?> GetTravelDistanceAsync(double originLat, double originLng, double destLat, double destLng, string mode = "driving");
        Task<List<DistanceResult>> GetBatchTravelDistancesAsync(double originLat, double originLng, List<(double lat, double lng)> destinations, string mode = "driving");
    }

    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleMapsService> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;

        public GoogleMapsService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleMapsService> logger, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
            
            _apiKey = Environment.GetEnvironmentVariable("GOOGLE_GEOCODING_API_KEY") 
                ?? _configuration["GoogleGeocodingApiKey"] 
                ?? throw new InvalidOperationException("Google Maps API key not configured");
        }

        public async Task<DistanceResult?> GetTravelDistanceAsync(double originLat, double originLng, double destLat, double destLng, string mode = "driving")
        {
            try
            {
                var origin = $"{originLat},{originLng}";
                var destination = $"{destLat},{destLng}";
                
                var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origin}&destinations={destination}&mode={mode}&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Google Maps API request failed with status {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DistanceMatrixResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Status != "OK" || result.Rows?.FirstOrDefault()?.Elements?.FirstOrDefault()?.Status != "OK")
                {
                    _logger.LogWarning("Google Maps API returned non-OK status: {Status}", result?.Status);
                    return null;
                }

                var element = result.Rows[0].Elements[0];
                return new DistanceResult
                {
                    DistanceInMeters = element.Distance.Value,
                    DistanceText = element.Distance.Text,
                    DurationInSeconds = element.Duration.Value,
                    DurationText = element.Duration.Text
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting travel distance from Google Maps API");
                return null;
            }
        }

        public async Task<List<DistanceResult>> GetBatchTravelDistancesAsync(double originLat, double originLng, List<(double lat, double lng)> destinations, string mode = "driving")
        {
            var results = new List<DistanceResult>();
            var uncachedIndices = new List<int>();
            var uncachedDestinations = new List<(double lat, double lng)>();
            
            // Check cache first
            for (int i = 0; i < destinations.Count; i++)
            {
                var cacheKey = $"distance_{originLat:F6}_{originLng:F6}_{destinations[i].lat:F6}_{destinations[i].lng:F6}_{mode}";
                if (_cache.TryGetValue<DistanceResult>(cacheKey, out var cachedResult))
                {
                    results.Add(cachedResult);
                }
                else
                {
                    results.Add(null); // Placeholder
                    uncachedIndices.Add(i);
                    uncachedDestinations.Add(destinations[i]);
                }
            }
            
            if (uncachedDestinations.Count == 0)
            {
                _logger.LogInformation("All {Count} distances retrieved from cache", destinations.Count);
                return results;
            }
            
            _logger.LogInformation("Found {CachedCount} cached distances, fetching {UncachedCount} from API", 
                destinations.Count - uncachedDestinations.Count, uncachedDestinations.Count);
            
            try
            {
                // Google Distance Matrix API allows up to 25 destinations per request
                const int batchSize = 25;
                var apiResults = new List<DistanceResult>();
                
                for (int i = 0; i < uncachedDestinations.Count; i += batchSize)
                {
                    var batch = uncachedDestinations.Skip(i).Take(batchSize).ToList();
                    var origin = $"{originLat},{originLng}";
                    var destinationsStr = string.Join("|", batch.Select(d => $"{d.lat},{d.lng}"));
                    
                    var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origin}&destinations={destinationsStr}&mode={mode}&key={_apiKey}";
                    
                    _logger.LogInformation("Making Google Maps API request for {DestinationCount} destinations (batch {BatchNumber}/{TotalBatches})", 
                        batch.Count, 
                        (i / batchSize) + 1, 
                        (int)Math.Ceiling((double)uncachedDestinations.Count / batchSize));
                    
                    var response = await _httpClient.GetAsync(url);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Google Maps API batch request failed with status {StatusCode}", response.StatusCode);
                        // Add null results for this batch
                        apiResults.AddRange(Enumerable.Repeat<DistanceResult?>(null, batch.Count));
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<DistanceMatrixResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result?.Status == "OK" && result.Rows?.FirstOrDefault()?.Elements != null)
                    {
                        for (int j = 0; j < batch.Count && j < result.Rows[0].Elements.Count; j++)
                        {
                            var element = result.Rows[0].Elements[j];
                            if (element.Status == "OK")
                            {
                                var distanceResult = new DistanceResult
                                {
                                    DistanceInMeters = element.Distance.Value,
                                    DistanceText = element.Distance.Text,
                                    DurationInSeconds = element.Duration.Value,
                                    DurationText = element.Duration.Text
                                };
                                
                                // Log first few results for debugging
                                if (apiResults.Count < 5)
                                {
                                    _logger.LogInformation("Distance result {Index}: {DistanceKm}km ({DistanceText}), Duration: {DurationText}, To: {DestLat},{DestLng}", 
                                        apiResults.Count + 1, 
                                        distanceResult.DistanceInKm.ToString("F2"), 
                                        distanceResult.DistanceText,
                                        distanceResult.DurationText,
                                        batch[j].lat.ToString("F6"),
                                        batch[j].lng.ToString("F6"));
                                }
                                
                                apiResults.Add(distanceResult);
                                
                                // Cache the result for 24 hours
                                var cacheKey = $"distance_{originLat:F6}_{originLng:F6}_{batch[j].lat:F6}_{batch[j].lng:F6}_{mode}";
                                _cache.Set(cacheKey, distanceResult, TimeSpan.FromHours(24));
                            }
                            else
                            {
                                _logger.LogWarning("Distance Matrix element {Index} returned status: {Status} for destination {DestLat},{DestLng}", 
                                    j + 1, 
                                    element.Status,
                                    batch[j].lat.ToString("F6"),
                                    batch[j].lng.ToString("F6"));
                                apiResults.Add(null);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Distance Matrix API returned non-OK status: {Status} for batch", result?.Status ?? "null result");
                        // Add null results for this batch
                        apiResults.AddRange(Enumerable.Repeat<DistanceResult?>(null, batch.Count));
                    }
                    
                    // Rate limiting - Google allows 1000 elements per second
                    if (i + batchSize < uncachedDestinations.Count)
                    {
                        await Task.Delay(100); // Small delay between batches
                    }
                }
                
                // Merge API results back into the main results list
                for (int i = 0; i < uncachedIndices.Count && i < apiResults.Count; i++)
                {
                    results[uncachedIndices[i]] = apiResults[i];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch travel distances from Google Maps API");
            }
            
            return results;
        }
    }

    public class DistanceResult
    {
        public int DistanceInMeters { get; set; }
        public string DistanceText { get; set; }
        public int DurationInSeconds { get; set; }
        public string DurationText { get; set; }
        
        public double DistanceInKm => DistanceInMeters / 1000.0;
        public double DistanceInMiles => DistanceInMeters / 1609.344;
    }

    // DTOs for Google Maps API response
    public class DistanceMatrixResponse
    {
        public string Status { get; set; }
        public List<Row> Rows { get; set; }
    }

    public class Row
    {
        public List<Element> Elements { get; set; }
    }

    public class Element
    {
        public string Status { get; set; }
        public Distance Distance { get; set; }
        public Duration Duration { get; set; }
    }

    public class Distance
    {
        public string Text { get; set; }
        public int Value { get; set; }
    }

    public class Duration
    {
        public string Text { get; set; }
        public int Value { get; set; }
    }
}