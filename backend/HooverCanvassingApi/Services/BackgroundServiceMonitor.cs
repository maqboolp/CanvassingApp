using System.Collections.Concurrent;

namespace HooverCanvassingApi.Services
{
    public interface IBackgroundServiceMonitor
    {
        void StartOperation(string operationName, string description);
        void CompleteOperation(string operationName);
        void FailOperation(string operationName, string error);
        Dictionary<string, OperationInfo> GetActiveOperations();
    }

    public class BackgroundServiceMonitor : IBackgroundServiceMonitor
    {
        private readonly ConcurrentDictionary<string, OperationInfo> _activeOperations = new();
        private readonly ILogger<BackgroundServiceMonitor> _logger;

        public BackgroundServiceMonitor(ILogger<BackgroundServiceMonitor> logger)
        {
            _logger = logger;
        }

        public void StartOperation(string operationName, string description)
        {
            var operation = new OperationInfo
            {
                Name = operationName,
                Description = description,
                StartTime = DateTime.UtcNow,
                Status = "Running"
            };

            _activeOperations[operationName] = operation;
            _logger.LogInformation("Started operation: {OperationName} - {Description}", operationName, description);
        }

        public void CompleteOperation(string operationName)
        {
            if (_activeOperations.TryRemove(operationName, out var operation))
            {
                var duration = DateTime.UtcNow - operation.StartTime;
                _logger.LogInformation("Completed operation: {OperationName} after {Duration}ms", 
                    operationName, duration.TotalMilliseconds);
            }
        }

        public void FailOperation(string operationName, string error)
        {
            if (_activeOperations.TryGetValue(operationName, out var operation))
            {
                operation.Status = "Failed";
                operation.Error = error;
                operation.EndTime = DateTime.UtcNow;
                
                var duration = operation.EndTime.Value - operation.StartTime;
                _logger.LogError("Failed operation: {OperationName} after {Duration}ms - {Error}", 
                    operationName, duration.TotalMilliseconds, error);
            }
        }

        public Dictionary<string, OperationInfo> GetActiveOperations()
        {
            // Clean up stale operations (older than 24 hours)
            var staleThreshold = DateTime.UtcNow.AddHours(-24);
            var staleOperations = _activeOperations
                .Where(kvp => kvp.Value.StartTime < staleThreshold)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in staleOperations)
            {
                _activeOperations.TryRemove(key, out _);
            }

            return new Dictionary<string, OperationInfo>(_activeOperations);
        }
    }

    public class OperationInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = "Running";
        public string? Error { get; set; }
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
    }
}