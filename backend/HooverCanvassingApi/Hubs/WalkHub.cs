using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HooverCanvassingApi.Services;

namespace HooverCanvassingApi.Hubs
{
    // [Authorize] // Temporarily disabled for testing
    public class WalkHub : Hub
    {
        private readonly IWalkHubService _walkHubService;
        private readonly ILogger<WalkHub> _logger;

        public WalkHub(IWalkHubService walkHubService, ILogger<WalkHub> logger)
        {
            _walkHubService = walkHubService;
            _logger = logger;
        }

        public async Task JoinWalkSession(double latitude, double longitude)
        {
            var volunteerIdClaim = Context.User?.FindFirst("VolunteerId")?.Value;
            var nameClaim = Context.User?.FindFirst("Name")?.Value;

            if (int.TryParse(volunteerIdClaim, out int volunteerId) && !string.IsNullOrEmpty(nameClaim))
            {
                await _walkHubService.AddToWalkGroup(Context.ConnectionId, volunteerId);
                await _walkHubService.NotifyCanvasserJoined(volunteerId, nameClaim, latitude, longitude);
                
                _logger.LogInformation($"Volunteer {nameClaim} joined walk session");
            }
            else
            {
                _logger.LogWarning("Invalid volunteer credentials in JoinWalkSession");
            }
        }

        public async Task LeaveWalkSession()
        {
            var volunteerIdClaim = Context.User?.FindFirst("VolunteerId")?.Value;
            var nameClaim = Context.User?.FindFirst("Name")?.Value;

            if (int.TryParse(volunteerIdClaim, out int volunteerId) && !string.IsNullOrEmpty(nameClaim))
            {
                await _walkHubService.RemoveFromWalkGroup(Context.ConnectionId, volunteerId);
                await _walkHubService.NotifyCanvasserLeft(volunteerId, nameClaim);
                
                _logger.LogInformation($"Volunteer {nameClaim} left walk session");
            }
        }

        public async Task UpdateLocation(double latitude, double longitude)
        {
            var volunteerIdClaim = Context.User?.FindFirst("VolunteerId")?.Value;
            var nameClaim = Context.User?.FindFirst("Name")?.Value;

            if (int.TryParse(volunteerIdClaim, out int volunteerId) && !string.IsNullOrEmpty(nameClaim))
            {
                await _walkHubService.NotifyCanvasserLocationUpdate(volunteerId, nameClaim, latitude, longitude);
            }
        }

        public async Task GetNearbyCanvassers(double latitude, double longitude, double radiusKm = 2.0)
        {
            var nearbyCanvassers = await _walkHubService.GetNearbyCanvassers(latitude, longitude, radiusKm);
            await Clients.Caller.SendAsync("NearbyCanvassers", nearbyCanvassers);
        }

        public override async Task OnConnectedAsync()
        {
            var volunteerIdClaim = Context.User?.FindFirst("VolunteerId")?.Value;
            var nameClaim = Context.User?.FindFirst("Name")?.Value;

            _logger.LogInformation($"Client connected: {Context.ConnectionId}, Volunteer: {nameClaim}");
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var volunteerIdClaim = Context.User?.FindFirst("VolunteerId")?.Value;
            var nameClaim = Context.User?.FindFirst("Name")?.Value;

            if (int.TryParse(volunteerIdClaim, out int volunteerId) && !string.IsNullOrEmpty(nameClaim))
            {
                await _walkHubService.RemoveFromWalkGroup(Context.ConnectionId, volunteerId);
                await _walkHubService.NotifyCanvasserLeft(volunteerId, nameClaim);
                
                _logger.LogInformation($"Volunteer {nameClaim} disconnected from walk session");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}