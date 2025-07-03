using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using HooverCanvassingApi.Data;

namespace HooverCanvassingApi.Services
{
    public class CsvStagingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CsvStagingService> _logger;
        private readonly string _connectionString;

        public CsvStagingService(ApplicationDbContext context, ILogger<CsvStagingService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<CsvImportResult> ImportCsvToStagingAsync(Stream csvStream, string fileName)
        {
            var result = new CsvImportResult
            {
                FileName = fileName,
                ImportedAt = DateTime.UtcNow
            };

            try
            {
                using var reader = new StreamReader(csvStream);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null
                });

                // Read the header to get column names
                await csv.ReadAsync();
                csv.ReadHeader();
                var headers = csv.HeaderRecord ?? throw new InvalidOperationException("CSV file has no headers");
                
                // Generate a unique table name for this import
                var baseTableName = $"voter_import_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
                var tableName = baseTableName.Length > 63 ? baseTableName.Substring(0, 63) : baseTableName;
                result.StagingTableName = tableName;
                
                // Create the staging table
                await CreateStagingTableAsync(tableName, headers);
                
                // Import the data
                var records = new List<dynamic>();
                var recordCount = 0;
                
                while (await csv.ReadAsync())
                {
                    var record = new ExpandoObject() as IDictionary<string, object?>;
                    foreach (var header in headers)
                    {
                        try
                        {
                            record[header] = csv.GetField(header);
                        }
                        catch
                        {
                            record[header] = null;
                        }
                    }
                    records.Add(record);
                    recordCount++;
                    
                    // Batch insert every 500 records for better performance
                    if (records.Count >= 500)
                    {
                        await InsertRecordsAsync(tableName, headers, records);
                        records.Clear();
                        
                        // Log progress for large imports
                        if (recordCount % 5000 == 0)
                        {
                            _logger.LogInformation("Imported {Count} records to staging table {TableName}", 
                                recordCount, tableName);
                        }
                    }
                }
                
                // Insert remaining records
                if (records.Any())
                {
                    await InsertRecordsAsync(tableName, headers, records);
                }
                
                result.TotalRecords = recordCount;
                result.Success = true;
                result.Columns = headers.ToList();
                
                _logger.LogInformation("Successfully imported {Count} records to staging table {TableName}", 
                    recordCount, tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CSV to staging");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }

        private async Task CreateStagingTableAsync(string tableName, string[] headers)
        {
            var sql = new StringBuilder($"CREATE TABLE \"{tableName}\" (\n");
            sql.AppendLine("    id SERIAL PRIMARY KEY,");
            sql.AppendLine("    imported_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,");
            
            for (int i = 0; i < headers.Length; i++)
            {
                var columnName = SanitizeColumnName(headers[i]);
                sql.Append($"    \"{columnName}\" TEXT");
                if (i < headers.Length - 1)
                {
                    sql.AppendLine(",");
                }
                else
                {
                    sql.AppendLine();
                }
            }
            
            sql.AppendLine(");");
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql.ToString(), connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task InsertRecordsAsync(string tableName, string[] headers, List<dynamic> records)
        {
            if (!records.Any()) return;
            
            var columns = string.Join(", ", headers.Select(h => $"\"{SanitizeColumnName(h)}\""));
            var parameters = string.Join(", ", Enumerable.Range(0, headers.Length).Select(i => $"@p{i}"));
            var sql = $"INSERT INTO \"{tableName}\" ({columns}) VALUES ({parameters})";
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var record in records)
                {
                    using var command = new NpgsqlCommand(sql, connection, transaction);
                    var dict = record as IDictionary<string, object?>;
                    
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var value = dict?[headers[i]];
                        command.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
                    }
                    
                    await command.ExecuteNonQueryAsync();
                }
                
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<string>> GetStagingTablesAsync()
        {
            var sql = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name LIKE 'voter_import_%' 
                ORDER BY table_name DESC";
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            var tables = new List<string>();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
            
            return tables;
        }

        public async Task<StagingTableInfo> GetStagingTableInfoAsync(string tableName)
        {
            var info = new StagingTableInfo { TableName = tableName };
            
            // Get column info
            var columnSql = @"
                SELECT column_name, data_type 
                FROM information_schema.columns 
                WHERE table_name = @tableName 
                AND column_name NOT IN ('id', 'imported_at')
                ORDER BY ordinal_position";
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // Get columns first and close the reader
            var columns = new List<string>();
            using (var columnCommand = new NpgsqlCommand(columnSql, connection))
            {
                columnCommand.Parameters.AddWithValue("@tableName", tableName);
                using (var columnReader = await columnCommand.ExecuteReaderAsync())
                {
                    while (await columnReader.ReadAsync())
                    {
                        columns.Add(columnReader.GetString(0));
                    }
                }
            }
            info.Columns = columns;
            
            // Get record count
            var countSql = $"SELECT COUNT(*) FROM \"{tableName}\"";
            using (var countCommand = new NpgsqlCommand(countSql, connection))
            {
                info.RecordCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
            }
            
            // Get sample data (first 5 rows)
            var sampleSql = $"SELECT * FROM \"{tableName}\" LIMIT 5";
            var sampleData = new List<Dictionary<string, object?>>();
            using (var sampleCommand = new NpgsqlCommand(sampleSql, connection))
            {
                using (var sampleReader = await sampleCommand.ExecuteReaderAsync())
                {
                    while (await sampleReader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>();
                        for (int i = 0; i < sampleReader.FieldCount; i++)
                        {
                            var columnName = sampleReader.GetName(i);
                            row[columnName] = sampleReader.IsDBNull(i) ? null : sampleReader.GetValue(i);
                        }
                        sampleData.Add(row);
                    }
                }
            }
            info.SampleData = sampleData;
            
            return info;
        }

        private string SanitizeColumnName(string columnName)
        {
            // Remove or replace invalid characters for PostgreSQL column names
            return columnName
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .Replace("/", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(",", "")
                .Replace("'", "")
                .Replace("\"", "")
                .ToLower();
        }

        public async Task<List<StagingTableMetadata>> GetStagingTablesAsync()
        {
            var tables = new List<StagingTableMetadata>();
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // Get all staging tables
            var sql = @"
                SELECT 
                    table_name,
                    obj_description(c.oid) as description
                FROM information_schema.tables t
                JOIN pg_class c ON c.relname = t.table_name
                WHERE table_schema = 'public' 
                AND table_name LIKE 'voter_import_%'
                ORDER BY table_name DESC";
            
            using var command = new NpgsqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            var tableNames = new List<(string name, string? description)>();
            while (await reader.ReadAsync())
            {
                tableNames.Add((
                    reader.GetString(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1)
                ));
            }
            reader.Close();
            
            // Get metadata for each table
            foreach (var (tableName, description) in tableNames)
            {
                var metadata = new StagingTableMetadata
                {
                    TableName = tableName
                };
                
                // Parse metadata from description if available
                if (!string.IsNullOrEmpty(description))
                {
                    try
                    {
                        var parts = description.Split('|');
                        if (parts.Length >= 3)
                        {
                            metadata.UploadedBy = parts[0];
                            if (DateTime.TryParse(parts[1], out var createdAt))
                                metadata.CreatedAt = createdAt;
                            metadata.FileName = parts[2];
                            if (parts.Length > 3 && bool.TryParse(parts[3], out var imported))
                                metadata.IsImported = imported;
                            if (parts.Length > 4 && DateTime.TryParse(parts[4], out var importedAt))
                                metadata.ImportedAt = importedAt;
                        }
                    }
                    catch { }
                }
                
                // Get row count
                var countSql = $"SELECT COUNT(*) FROM \"{tableName}\"";
                using var countCmd = new NpgsqlCommand(countSql, connection);
                metadata.RowCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                
                tables.Add(metadata);
            }
            
            return tables;
        }

        public async Task<bool> DeleteStagingTableAsync(string tableName)
        {
            // Validate table name
            if (!tableName.StartsWith("voter_import_") || tableName.Contains(";"))
            {
                throw new ArgumentException("Invalid staging table name");
            }
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            try
            {
                var sql = $"DROP TABLE IF EXISTS \"{tableName}\"";
                using var command = new NpgsqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staging table {TableName}", tableName);
                return false;
            }
        }
    }

    public class CsvImportResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StagingTableName { get; set; }
        public string? FileName { get; set; }
        public int TotalRecords { get; set; }
        public DateTime ImportedAt { get; set; }
        public List<string> Columns { get; set; } = new();
    }

    public class StagingTableInfo
    {
        public string TableName { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public int RecordCount { get; set; }
        public List<Dictionary<string, object?>> SampleData { get; set; } = new();
    }

    public class StagingTableMetadata
    {
        public string TableName { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UploadedBy { get; set; }
        public string? FileName { get; set; }
        public bool IsImported { get; set; }
        public DateTime? ImportedAt { get; set; }
    }
}