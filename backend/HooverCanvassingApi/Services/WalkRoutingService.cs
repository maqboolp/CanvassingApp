using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Services
{
    public class WalkRoutingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WalkRoutingService> _logger;
        private readonly TspSolver _tspSolver;

        public WalkRoutingService(ApplicationDbContext context, ILogger<WalkRoutingService> logger, TspSolver tspSolver)
        {
            _context = context;
            _logger = logger;
            _tspSolver = tspSolver;
        }

        /// <summary>
        /// Get available houses near a location that haven't been visited recently
        /// </summary>
        public async Task<List<AvailableHouse>> GetAvailableHousesAsync(
            double latitude, 
            double longitude, 
            double radiusKm = 1.0,
            int limit = 50)
        {
            var radiusMeters = radiusKm * 1000;
            
            // Get all active claims
            var activeClaims = await _context.HouseClaims
                .Where(c => c.Status == ClaimStatus.Claimed || c.Status == ClaimStatus.Visiting)
                .Where(c => c.ExpiresAt > DateTime.UtcNow)
                .Select(c => c.Address)
                .ToListAsync();

            // Get recently visited addresses (last 7 days)
            // Temporarily disabled due to column name case issues
            var recentlyVisited = new List<string>();
            // var recentlyVisited = await _context.Contacts
            //     .Include(c => c.Voter)
            //     .Where(c => c.Timestamp > DateTime.UtcNow.AddDays(-7))
            //     .Where(c => c.Voter != null)
            //     .Select(c => c.Voter.AddressLine)
            //     .Distinct()
            //     .ToListAsync();

            // Find available voters within radius
            var availableVoters = await _context.Voters
                .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                .Where(v => !activeClaims.Contains(v.AddressLine))
                // .Where(v => !recentlyVisited.Contains(v.AddressLine))
                .ToListAsync();

            // Calculate distances and filter by radius
            var availableHouses = availableVoters
                .Select(v => new
                {
                    Voter = v,
                    Distance = CalculateDistance(latitude, longitude, v.Latitude!.Value, v.Longitude!.Value)
                })
                .Where(x => x.Distance <= radiusMeters)
                .GroupBy(x => x.Voter.AddressLine)
                .Select(g => new AvailableHouse
                {
                    Address = g.Key,
                    Latitude = g.First().Voter.Latitude!.Value,
                    Longitude = g.First().Voter.Longitude!.Value,
                    DistanceMeters = g.First().Distance,
                    VoterCount = g.Count(),
                    Voters = g.Select(x => new AvailableHouseVoter
                    {
                        VoterId = x.Voter.LalVoterId,
                        Name = $"{x.Voter.FirstName} {x.Voter.LastName}",
                        Age = x.Voter.Age,
                        PartyAffiliation = x.Voter.PartyAffiliation,
                        VoteFrequency = x.Voter.VoteFrequency
                    }).ToList()
                })
                .OrderBy(h => h.DistanceMeters)
                .Take(limit)
                .ToList();

            return availableHouses;
        }

        /// <summary>
        /// Generate an optimized route through selected houses
        /// </summary>
        public async Task<OptimizedRoute> GenerateOptimizedRouteAsync(
            double startLatitude,
            double startLongitude,
            List<string> targetAddresses)
        {
            if (!targetAddresses.Any())
            {
                return new OptimizedRoute
                {
                    Houses = new List<RouteHouse>(),
                    TotalDistanceMeters = 0,
                    EstimatedDurationMinutes = 0
                };
            }

            // Get house coordinates
            var houses = await _context.Voters
                .Where(v => targetAddresses.Contains(v.AddressLine))
                .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                .GroupBy(v => v.AddressLine)
                .Select(g => new
                {
                    Address = g.Key,
                    Latitude = g.First().Latitude!.Value,
                    Longitude = g.First().Longitude!.Value,
                    VoterCount = g.Count()
                })
                .ToListAsync();

            if (!houses.Any())
            {
                return new OptimizedRoute
                {
                    Houses = new List<RouteHouse>(),
                    TotalDistanceMeters = 0,
                    EstimatedDurationMinutes = 0
                };
            }

            // Add starting point to the list of locations
            var allLocations = new List<(string Address, double Latitude, double Longitude, int VoterCount)>
            {
                ("START", startLatitude, startLongitude, 0)
            };
            allLocations.AddRange(houses.Select(h => (h.Address, h.Latitude, h.Longitude, h.VoterCount)));

            // Build distance matrix
            var n = allLocations.Count;
            var distanceMatrix = new double[n, n];
            
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        distanceMatrix[i, j] = 0;
                    }
                    else
                    {
                        distanceMatrix[i, j] = CalculateDistance(
                            allLocations[i].Latitude,
                            allLocations[i].Longitude,
                            allLocations[j].Latitude,
                            allLocations[j].Longitude
                        );
                    }
                }
            }

            // Use 2-opt algorithm for route optimization (good balance of speed and quality)
            var optimizedTour = TspSolver.Solve2Opt(distanceMatrix);
            
            // Build the route from the optimized tour
            var route = new List<RouteHouse>();
            var totalDistance = 0.0;
            
            for (int i = 1; i < optimizedTour.Count; i++)
            {
                var locationIndex = optimizedTour[i];
                var location = allLocations[locationIndex];
                
                if (location.Address == "START") continue; // Skip the starting point in the output
                
                var previousIndex = optimizedTour[i - 1];
                var distance = distanceMatrix[previousIndex, locationIndex];
                totalDistance += distance;

                route.Add(new RouteHouse
                {
                    Address = location.Address,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Order = route.Count + 1,
                    DistanceFromPreviousMeters = distance,
                    VoterCount = location.VoterCount
                });
            }

            // Add distance back to start if we have houses
            if (route.Any())
            {
                var lastLocationIndex = optimizedTour.Last();
                totalDistance += distanceMatrix[lastLocationIndex, 0]; // Distance back to start
            }

            // Estimate duration (2 minutes per house + walking time at 5 km/h)
            var walkingTimeMinutes = (totalDistance / 1000.0) / 5.0 * 60.0;
            var houseTimeMinutes = route.Count * 2;

            _logger.LogInformation($"Generated optimized route for {route.Count} houses with total distance {totalDistance:F0}m using 2-opt algorithm");

            return new OptimizedRoute
            {
                Houses = route,
                TotalDistanceMeters = totalDistance,
                EstimatedDurationMinutes = walkingTimeMinutes + houseTimeMinutes
            };
        }

        /// <summary>
        /// Claim houses for a walk session to prevent duplicates
        /// </summary>
        public async Task<List<HouseClaim>> ClaimHousesAsync(
            int walkSessionId,
            List<string> addresses,
            int claimDurationMinutes = 30)
        {
            var claims = new List<HouseClaim>();
            var now = DateTime.UtcNow;

            // Check which addresses are already claimed
            var existingClaims = await _context.HouseClaims
                .Where(c => addresses.Contains(c.Address))
                .Where(c => c.Status == ClaimStatus.Claimed || c.Status == ClaimStatus.Visiting)
                .Where(c => c.ExpiresAt > now)
                .Select(c => c.Address)
                .ToListAsync();
            var existingClaimsSet = new HashSet<string>(existingClaims);

            // Get house details for unclaimed addresses
            var availableHouses = await _context.Voters
                .Where(v => addresses.Contains(v.AddressLine))
                .Where(v => !existingClaimsSet.Contains(v.AddressLine))
                .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                .GroupBy(v => v.AddressLine)
                .Select(g => new
                {
                    Address = g.Key,
                    Latitude = g.First().Latitude!.Value,
                    Longitude = g.First().Longitude!.Value
                })
                .ToListAsync();

            // Create claims for available houses
            foreach (var house in availableHouses)
            {
                var claim = new HouseClaim
                {
                    WalkSessionId = walkSessionId,
                    Address = house.Address,
                    Latitude = house.Latitude,
                    Longitude = house.Longitude,
                    ClaimedAt = now,
                    ExpiresAt = now.AddMinutes(claimDurationMinutes),
                    Status = ClaimStatus.Claimed
                };

                claims.Add(claim);
                _context.HouseClaims.Add(claim);
            }

            await _context.SaveChangesAsync();
            return claims;
        }

        // Haversine formula for distance calculation
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth's radius in meters
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;
    }

    // DTOs for the service
    public class AvailableHouse
    {
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceMeters { get; set; }
        public int VoterCount { get; set; }
        public List<AvailableHouseVoter> Voters { get; set; } = new();
    }

    public class AvailableHouseVoter
    {
        public string VoterId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? PartyAffiliation { get; set; }
        public VoteFrequency VoteFrequency { get; set; }
    }

    public class OptimizedRoute
    {
        public List<RouteHouse> Houses { get; set; } = new();
        public double TotalDistanceMeters { get; set; }
        public double EstimatedDurationMinutes { get; set; }
    }

    public class RouteHouse
    {
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Order { get; set; }
        public double DistanceFromPreviousMeters { get; set; }
        public int VoterCount { get; set; }
    }
}