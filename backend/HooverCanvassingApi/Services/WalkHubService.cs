using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Hubs;
using System.Security.Claims;

namespace HooverCanvassingApi.Services
{
    public interface IWalkHubService
    {
        Task NotifyHouseClaimed(int houseId, string address, int volunteerId, string volunteerName);
        Task NotifyHouseReleased(int houseId, string address);
        Task NotifyCanvasserLocationUpdate(int volunteerId, string name, double latitude, double longitude);
        Task NotifyHouseCompleted(int houseId, string address, int volunteerId, string volunteerName);
        Task NotifyCanvasserJoined(int volunteerId, string name, double latitude, double longitude);
        Task NotifyCanvasserLeft(int volunteerId, string name);
        Task AddToWalkGroup(string connectionId, int volunteerId);
        Task RemoveFromWalkGroup(string connectionId, int volunteerId);
        Task<List<ActiveCanvasser>> GetNearbyCanvassers(double latitude, double longitude, double radiusKm = 2.0);
    }

    public class WalkHubService : IWalkHubService
    {
        private readonly IHubContext<WalkHub> _hubContext;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WalkHubService> _logger;
        
        // Track active canvassers and their locations
        private readonly ConcurrentDictionary<int, CanvasserLocation> _canvasserLocations = new();
        private readonly ConcurrentDictionary<string, int> _connectionToVolunteer = new();

        public WalkHubService(IHubContext<WalkHub> hubContext, ApplicationDbContext context, ILogger<WalkHubService> logger)
        {
            _hubContext = hubContext;
            _context = context;
            _logger = logger;
        }

        public async Task NotifyHouseClaimed(int houseId, string address, int volunteerId, string volunteerName)
        {
            var message = new
            {
                Type = "HouseClaimed",
                HouseId = houseId,
                Address = address,
                VolunteerId = volunteerId,
                VolunteerName = volunteerName,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("WalkGroup").SendAsync("HouseStatusUpdate", message);
            _logger.LogInformation($"House {address} claimed by {volunteerName}");
        }

        public async Task NotifyHouseReleased(int houseId, string address)
        {
            var message = new
            {
                Type = "HouseReleased",
                HouseId = houseId,
                Address = address,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("WalkGroup").SendAsync("HouseStatusUpdate", message);
            _logger.LogInformation($"House {address} released");
        }

        public async Task NotifyCanvasserLocationUpdate(int volunteerId, string name, double latitude, double longitude)
        {
            var location = new CanvasserLocation
            {
                VolunteerId = volunteerId,
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdate = DateTime.UtcNow
            };

            _canvasserLocations.AddOrUpdate(volunteerId, location, (key, oldValue) => location);

            var message = new
            {
                Type = "CanvasserLocationUpdate",
                VolunteerId = volunteerId,
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("WalkGroup").SendAsync("CanvasserLocationUpdate", message);
        }

        public async Task NotifyHouseCompleted(int houseId, string address, int volunteerId, string volunteerName)
        {
            var message = new
            {
                Type = "HouseCompleted",
                HouseId = houseId,
                Address = address,
                VolunteerId = volunteerId,
                VolunteerName = volunteerName,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("WalkGroup").SendAsync("HouseStatusUpdate", message);
            _logger.LogInformation($"House {address} completed by {volunteerName}");
        }

        public async Task NotifyCanvasserJoined(int volunteerId, string name, double latitude, double longitude)
        {
            var location = new CanvasserLocation
            {
                VolunteerId = volunteerId,
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdate = DateTime.UtcNow
            };

            _canvasserLocations.AddOrUpdate(volunteerId, location, (key, oldValue) => location);

            var message = new
            {
                Type = "CanvasserJoined",
                VolunteerId = volunteerId,
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("WalkGroup").SendAsync("CanvasserUpdate", message);
            _logger.LogInformation($"Canvasser {name} joined walk session");
        }

        public async Task NotifyCanvasserLeft(int volunteerId, string name)
        {
            _canvasserLocations.TryRemove(volunteerId, out _);

            var message = new
            {
                Type = "CanvasserLeft",
                VolunteerId = volunteerId,
                Name = name,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("WalkGroup").SendAsync("CanvasserUpdate", message);
            _logger.LogInformation($"Canvasser {name} left walk session");
        }

        public async Task AddToWalkGroup(string connectionId, int volunteerId)
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, "WalkGroup");
            _connectionToVolunteer.TryAdd(connectionId, volunteerId);
        }

        public async Task RemoveFromWalkGroup(string connectionId, int volunteerId)
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, "WalkGroup");
            _connectionToVolunteer.TryRemove(connectionId, out _);
        }

        public async Task<List<ActiveCanvasser>> GetNearbyCanvassers(double latitude, double longitude, double radiusKm = 2.0)
        {
            var nearbyCanvassers = new List<ActiveCanvasser>();
            
            foreach (var kvp in _canvasserLocations)
            {
                var canvasser = kvp.Value;
                var distance = CalculateDistance(latitude, longitude, canvasser.Latitude, canvasser.Longitude);
                
                if (distance <= radiusKm * 1000) // Convert km to meters
                {
                    // Get houses visited count from database
                    var housesVisited = await _context.WalkActivities
                        .Include(wa => wa.WalkSession)
                        .Where(wa => wa.WalkSession.VolunteerId == canvasser.VolunteerId.ToString() && 
                                   wa.ActivityType == WalkActivityType.DepartedHouse &&
                                   wa.Timestamp >= DateTime.UtcNow.Date)
                        .CountAsync();

                    nearbyCanvassers.Add(new ActiveCanvasser
                    {
                        VolunteerId = canvasser.VolunteerId,
                        Name = canvasser.Name,
                        Latitude = canvasser.Latitude,
                        Longitude = canvasser.Longitude,
                        DistanceMeters = distance,
                        HousesVisited = housesVisited,
                        LastUpdateTime = canvasser.LastUpdate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    });
                }
            }

            return nearbyCanvassers.OrderBy(c => c.DistanceMeters).ToList();
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3; // Earth's radius in meters
            var φ1 = lat1 * Math.PI / 180;
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
    }

    public class CanvasserLocation
    {
        public int VolunteerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class ActiveCanvasser
    {
        public int VolunteerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceMeters { get; set; }
        public int HousesVisited { get; set; }
        public string LastUpdateTime { get; set; } = string.Empty;
    }
}