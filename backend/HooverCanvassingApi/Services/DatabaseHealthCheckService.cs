using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using System.Data;

namespace HooverCanvassingApi.Services
{
    public class DatabaseHealthCheckService : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseHealthCheckService> _logger;

        public DatabaseHealthCheckService(IConfiguration configuration, ILogger<DatabaseHealthCheckService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var data = new Dictionary<string, object>();

                // Test query
                using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken);

                // Get database connection statistics from PostgreSQL
                using var statsCommand = new NpgsqlCommand(@"
                    SELECT 
                        count(*) as total_connections,
                        count(*) FILTER (WHERE state = 'active') as active_connections,
                        count(*) FILTER (WHERE state = 'idle') as idle_connections,
                        count(*) FILTER (WHERE state = 'idle in transaction') as idle_in_transaction,
                        max_conn.setting::int as max_connections
                    FROM pg_stat_activity
                    CROSS JOIN (SELECT setting FROM pg_settings WHERE name = 'max_connections') max_conn
                    WHERE datname = current_database()", connection);

                using var reader = await statsCommand.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var totalConnections = reader.GetInt64(0);
                    var activeConnections = reader.GetInt64(1);
                    var idleConnections = reader.GetInt64(2);
                    var idleInTransaction = reader.GetInt64(3);
                    var maxConnections = reader.GetInt32(4);

                    data["TotalConnections"] = totalConnections;
                    data["ActiveConnections"] = activeConnections;
                    data["IdleConnections"] = idleConnections;
                    data["IdleInTransaction"] = idleInTransaction;
                    data["MaxConnections"] = maxConnections;
                    data["UtilizationPercent"] = Math.Round((double)totalConnections / maxConnections * 100, 2);

                    _logger.LogInformation("Database connection stats - Total: {Total}, Active: {Active}, Idle: {Idle}, Max: {Max}", 
                        totalConnections, activeConnections, idleConnections, maxConnections);

                    // Determine health status based on connection usage
                    var utilizationPercent = (double)totalConnections / maxConnections;
                    
                    if (utilizationPercent > 0.9) // Critical threshold at 90%
                    {
                        return HealthCheckResult.Unhealthy(
                            $"Critical database connection usage: {totalConnections}/{maxConnections} connections ({utilizationPercent:P0})", 
                            data: data);
                    }
                    else if (utilizationPercent > 0.75) // Warning threshold at 75%
                    {
                        return HealthCheckResult.Degraded(
                            $"High database connection usage: {totalConnections}/{maxConnections} connections ({utilizationPercent:P0})", 
                            data: data);
                    }

                    return HealthCheckResult.Healthy(
                        $"Database is healthy. Connections: {totalConnections}/{maxConnections} ({utilizationPercent:P0})", 
                        data: data);
                }

                return HealthCheckResult.Healthy("Database is healthy", data: data);
            }
            catch (NpgsqlException ex) when (ex.SqlState == "53300")
            {
                _logger.LogError(ex, "Database connection pool exhausted");
                return HealthCheckResult.Unhealthy(
                    "Database connection pool is exhausted. Too many connections.", 
                    exception: ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy(
                    "Database connection failed", 
                    exception: ex);
            }
        }
    }
}