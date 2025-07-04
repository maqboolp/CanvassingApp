using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HooverCanvassingApi.Services;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class VoterImportController : ControllerBase
    {
        private readonly CsvStagingService _stagingService;
        private readonly VoterMappingService _mappingService;
        private readonly ILogger<VoterImportController> _logger;

        public VoterImportController(
            CsvStagingService stagingService, 
            VoterMappingService mappingService, 
            ILogger<VoterImportController> logger)
        {
            _stagingService = stagingService;
            _mappingService = mappingService;
            _logger = logger;
        }

        [HttpPost("stage")]
        [RequestSizeLimit(104857600)] // 100MB limit
        public async Task<IActionResult> UploadToStaging(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only CSV files are supported");
            }

            try
            {
                // For large files, process in background and return immediately
                if (file.Length > 10 * 1024 * 1024) // 10MB
                {
                    var tempFile = Path.GetTempFileName();
                    using (var writeStream = new FileStream(tempFile, FileMode.Create))
                    {
                        await file.CopyToAsync(writeStream);
                    }

                    // Start background processing
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var readStream = new FileStream(tempFile, FileMode.Open);
                            await _stagingService.ImportCsvToStagingAsync(readStream, file.FileName);
                        }
                        finally
                        {
                            if (System.IO.File.Exists(tempFile))
                            {
                                System.IO.File.Delete(tempFile);
                            }
                        }
                    });

                    return Accepted(new
                    {
                        success = true,
                        message = "Large file import started. Check staging tables in a few moments.",
                        fileName = file.FileName,
                        fileSize = file.Length
                    });
                }

                // For smaller files, process synchronously
                using var stream = file.OpenReadStream();
                var result = await _stagingService.ImportCsvToStagingAsync(stream, file.FileName);
                
                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        stagingTableName = result.StagingTableName,
                        fileName = result.FileName,
                        totalRecords = result.TotalRecords,
                        columns = result.Columns,
                        message = $"Successfully imported {result.TotalRecords} records to staging table"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading CSV to staging");
                return StatusCode(500, "An error occurred while processing the file");
            }
        }


        [HttpGet("staging-tables/{tableName}")]
        public async Task<IActionResult> GetStagingTableInfo(string tableName)
        {
            try
            {
                var info = await _stagingService.GetStagingTableInfoAsync(tableName);
                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staging table info");
                return StatusCode(500, "An error occurred while retrieving table information");
            }
        }

        [HttpGet("staging-tables/{tableName}/columns")]
        public async Task<IActionResult> GetAvailableColumns(string tableName)
        {
            try
            {
                var columns = await _mappingService.GetAvailableColumnsAsync(tableName);
                return Ok(columns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting columns");
                return StatusCode(500, "An error occurred while retrieving columns");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportFromStaging([FromBody] ImportRequest request)
        {
            if (string.IsNullOrEmpty(request.StagingTableName))
            {
                return BadRequest("Staging table name is required");
            }

            if (request.Mapping == null)
            {
                return BadRequest("Column mapping is required");
            }

            // Validate required mappings
            if (string.IsNullOrEmpty(request.Mapping.FirstNameColumn) ||
                string.IsNullOrEmpty(request.Mapping.LastNameColumn) ||
                string.IsNullOrEmpty(request.Mapping.AddressColumn))
            {
                return BadRequest("First name, last name, and address columns are required");
            }

            try
            {
                var result = await _mappingService.MapAndImportVotersAsync(
                    request.StagingTableName, 
                    request.Mapping);
                
                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        imported = result.ImportedCount,
                        skipped = result.SkippedCount,
                        errors = result.ErrorCount,
                        duration = result.Duration.TotalSeconds,
                        message = $"Successfully imported {result.ImportedCount} voters",
                        errorDetails = result.Errors.Take(10) // Return first 10 errors
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing from staging");
                return StatusCode(500, "An error occurred while importing voters");
            }
        }

        [HttpGet("staging-tables")]
        public async Task<IActionResult> GetStagingTables()
        {
            try
            {
                var tables = await _stagingService.GetStagingTablesWithMetadataAsync();
                
                return Ok(new
                {
                    success = true,
                    tables = tables.Select(t => new
                    {
                        tableName = t.TableName,
                        rowCount = t.RowCount,
                        createdAt = t.CreatedAt,
                        uploadedBy = t.UploadedBy,
                        isImported = t.IsImported,
                        importedAt = t.ImportedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staging tables");
                return StatusCode(500, "An error occurred while retrieving staging tables");
            }
        }

        [HttpPost("remap")]
        public async Task<IActionResult> RemapAndImport([FromBody] ImportRequest request)
        {
            // This endpoint allows remapping an existing staging table
            // It uses the same logic as the import endpoint but is explicitly named for remapping
            return await ImportFromStaging(request);
        }

        [HttpDelete("staging-tables/{tableName}")]
        public async Task<IActionResult> DeleteStagingTable(string tableName)
        {
            try
            {
                // Validate table name to prevent SQL injection
                if (!tableName.StartsWith("voter_import_") || tableName.Contains(";"))
                {
                    return BadRequest("Invalid staging table name");
                }

                var deleted = await _stagingService.DeleteStagingTableAsync(tableName);
                
                if (deleted)
                {
                    return Ok(new { success = true, message = "Staging table deleted successfully" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Staging table not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staging table {TableName}", tableName);
                return StatusCode(500, "An error occurred while deleting the staging table");
            }
        }
    }

    public class ImportRequest
    {
        public string StagingTableName { get; set; } = string.Empty;
        public ColumnMapping Mapping { get; set; } = new();
    }
}