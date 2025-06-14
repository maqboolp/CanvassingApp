using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Middleware
{
    public class ActivityTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ActivityTrackingMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ActivityTrackingMiddleware(RequestDelegate next, ILogger<ActivityTrackingMiddleware> logger, IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Continue processing the request
            await _next(context);

            // Only track activity for authenticated users making successful API calls
            if (context.User.Identity?.IsAuthenticated == true && 
                context.Request.Path.StartsWithSegments("/api") &&
                context.Response.StatusCode < 400) // Only track successful requests
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Update last activity in background with proper scope
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = _serviceProvider.CreateScope();
                                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Volunteer>>();
                                
                                var user = await userManager.FindByIdAsync(userId);
                                if (user != null && user.IsActive)
                                {
                                    user.LastActivity = DateTime.UtcNow;
                                    await userManager.UpdateAsync(user);
                                    
                                    _logger.LogDebug("Updated last activity for user {UserId} at {Time}", 
                                        userId, DateTime.UtcNow);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to update last activity for user {UserId}", userId);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in activity tracking middleware");
                }
            }
        }
    }
}