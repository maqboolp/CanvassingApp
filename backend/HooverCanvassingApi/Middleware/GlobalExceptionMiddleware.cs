using System.Net;
using System.Text.Json;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse();
            var correlationId = Guid.NewGuid().ToString();
            errorResponse.CorrelationId = correlationId;
            errorResponse.Timestamp = DateTime.UtcNow;

            switch (exception)
            {
                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = validationEx.Message;
                    errorResponse.Type = "ValidationError";
                    errorResponse.Errors = validationEx.Errors;
                    _logger.LogWarning(validationEx, "Validation error occurred. CorrelationId: {CorrelationId}", correlationId);
                    break;

                case NotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = notFoundEx.Message;
                    errorResponse.Type = "NotFoundError";
                    _logger.LogWarning(notFoundEx, "Resource not found. CorrelationId: {CorrelationId}", correlationId);
                    break;

                case UnauthorizedException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = unauthorizedEx.Message;
                    errorResponse.Type = "UnauthorizedError";
                    _logger.LogWarning(unauthorizedEx, "Unauthorized access attempt. CorrelationId: {CorrelationId}", correlationId);
                    break;

                case ForbiddenException forbiddenEx:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.Message = forbiddenEx.Message;
                    errorResponse.Type = "ForbiddenError";
                    _logger.LogWarning(forbiddenEx, "Forbidden access attempt. CorrelationId: {CorrelationId}", correlationId);
                    break;

                case ConflictException conflictEx:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    errorResponse.Message = conflictEx.Message;
                    errorResponse.Type = "ConflictError";
                    _logger.LogWarning(conflictEx, "Conflict error occurred. CorrelationId: {CorrelationId}", correlationId);
                    break;

                case ExternalServiceException externalEx:
                    response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    errorResponse.Message = "An external service is temporarily unavailable. Please try again later.";
                    errorResponse.Type = "ExternalServiceError";
                    errorResponse.ServiceName = externalEx.ServiceName;
                    _logger.LogError(externalEx, "External service error ({ServiceName}). CorrelationId: {CorrelationId}", 
                        externalEx.ServiceName, correlationId);
                    break;

                case OperationCanceledException:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse.Message = "The operation was cancelled or timed out.";
                    errorResponse.Type = "TimeoutError";
                    _logger.LogWarning("Operation cancelled. CorrelationId: {CorrelationId}", correlationId);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "An unexpected error occurred. Please try again later.";
                    errorResponse.Type = "InternalServerError";
                    
                    // Log the full exception details
                    _logger.LogError(exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);
                    
                    // Include stack trace in development
                    if (_env.IsDevelopment())
                    {
                        errorResponse.Message = exception.Message;
                        errorResponse.Details = exception.StackTrace;
                    }
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(jsonResponse);
        }
    }

    // Error response model
    public class ErrorResponse
    {
        public string Type { get; set; } = "Error";
        public string Message { get; set; } = "An error occurred";
        public string? Details { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
        public string? ServiceName { get; set; }
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Custom exception classes
    public class ValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        public ValidationException(string message) : base(message)
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(string message, Dictionary<string, string[]> errors) : base(message)
        {
            Errors = errors;
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string entityName, object key) 
            : base($"{entityName} with id '{key}' was not found.") { }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message = "Unauthorized access") : base(message) { }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "Access forbidden") : base(message) { }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }

    public class ExternalServiceException : Exception
    {
        public string ServiceName { get; }

        public ExternalServiceException(string serviceName, string message) : base(message)
        {
            ServiceName = serviceName;
        }

        public ExternalServiceException(string serviceName, string message, Exception innerException) 
            : base(message, innerException)
        {
            ServiceName = serviceName;
        }
    }
}