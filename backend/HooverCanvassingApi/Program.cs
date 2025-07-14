using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Server.IIS;
using System.Text;
using System.Collections;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using HooverCanvassingApi.Services.EmailTemplates;
using HooverCanvassingApi.Middleware;
using HooverCanvassingApi.Configuration;
using HooverCanvassingApi;
using HooverCanvassingApi.Filters;
using Amazon.S3;
using Amazon;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(options =>
{
    // Use camelCase for JSON property names to match JavaScript conventions
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Add memory cache
builder.Services.AddMemoryCache();

// Configure request size limits for file uploads
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get Frontend URL early for use in CORS and other configurations
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? builder.Configuration["Frontend:BaseUrl"];

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Production CORS - use environment variable or configuration
            var allowedOrigins = new List<string>();
            
            // Add frontend URL if available
            if (!string.IsNullOrEmpty(frontendUrl))
            {
                allowedOrigins.Add(frontendUrl);
            }
            
            // Add any additional CORS origins from environment variable (comma-separated)
            var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
            if (!string.IsNullOrEmpty(corsOrigins))
            {
                allowedOrigins.AddRange(corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
            
            // Add configured origins from appsettings
            var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (configuredOrigins != null && configuredOrigins.Length > 0)
            {
                allowedOrigins.AddRange(configuredOrigins);
            }
            
            // Fallback to allow any origin if none configured (not recommended for production)
            if (allowedOrigins.Count == 0)
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(allowedOrigins.Distinct().ToArray())
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
        }
    });
});

// Add environment variables explicitly
builder.Configuration.AddEnvironmentVariables();

// Configure Campaign Settings
builder.Services.Configure<CampaignSettings>(options =>
{
    // First bind from Campaign section in appsettings.json
    builder.Configuration.GetSection("Campaign").Bind(options);
    
    // Then override with REACT_APP_ environment variables if they exist (for consistency with frontend)
    var candidateName = builder.Configuration["REACT_APP_CANDIDATE_NAME"];
    if (!string.IsNullOrEmpty(candidateName))
        options.CandidateName = candidateName;
    
    var campaignName = builder.Configuration["REACT_APP_CAMPAIGN_NAME"];
    if (!string.IsNullOrEmpty(campaignName))
        options.CampaignName = campaignName;
    
    var campaignTitle = builder.Configuration["REACT_APP_CAMPAIGN_TITLE"];
    if (!string.IsNullOrEmpty(campaignTitle))
        options.CampaignTitle = campaignTitle;
        
    // For office, we can derive it from title or use a separate env var
    var office = builder.Configuration["REACT_APP_OFFICE"];
    if (!string.IsNullOrEmpty(office))
        options.Office = office;
    else if (!string.IsNullOrEmpty(campaignTitle) && campaignTitle.Contains(" for "))
    {
        // Extract office from title like "Cindy Myrex for Alabama House"
        var parts = campaignTitle.Split(" for ");
        if (parts.Length > 1)
            options.Office = parts[1];
    }
});

// Add configuration validation service
builder.Services.AddHostedService<ConfigurationValidationService>();

// Use standard .NET configuration for connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Fix truncated sslmode parameter if needed
if (!string.IsNullOrEmpty(connectionString) && connectionString.EndsWith("?sslmode"))
{
    connectionString += "=require";
    Console.WriteLine("Fixed truncated sslmode parameter");
}

Console.WriteLine($"Connection string from config: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");
Console.WriteLine($"Connection String Length: {connectionString?.Length}");
Console.WriteLine($"Connection string available: {!string.IsNullOrEmpty(connectionString)}");

// Check Google API key availability
var googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_GEOCODING_API_KEY");
Console.WriteLine($"Google API Key: {(string.IsNullOrEmpty(googleApiKey) ? "MISSING" : "PRESENT")} (Length: {googleApiKey?.Length ?? 0})");

// Check Twilio configuration
var twilioAccountSid = builder.Configuration["Twilio:AccountSid"];
var twilioAuthToken = builder.Configuration["Twilio:AuthToken"];
var twilioFromPhone = builder.Configuration["Twilio:FromPhoneNumber"];
var twilioSmsPhone = builder.Configuration["Twilio:SmsPhoneNumber"];
var twilioMessagingSid = builder.Configuration["Twilio:MessagingServiceSid"];

Console.WriteLine($"Twilio AccountSid: {(string.IsNullOrEmpty(twilioAccountSid) ? "MISSING" : $"***{twilioAccountSid.Substring(Math.Max(0, twilioAccountSid.Length - 4))}")} (Length: {twilioAccountSid?.Length ?? 0})");
Console.WriteLine($"Twilio AuthToken: {(string.IsNullOrEmpty(twilioAuthToken) ? "MISSING" : "***CONFIGURED")} (Length: {twilioAuthToken?.Length ?? 0})");
Console.WriteLine($"Twilio FromPhone: {(string.IsNullOrEmpty(twilioFromPhone) ? "MISSING" : twilioFromPhone)} (Length: {twilioFromPhone?.Length ?? 0})");
Console.WriteLine($"Twilio SmsPhone: {(string.IsNullOrEmpty(twilioSmsPhone) ? "NOT SET (will use FromPhone)" : twilioSmsPhone)} (Length: {twilioSmsPhone?.Length ?? 0})");
Console.WriteLine($"Twilio MessagingSid: {(string.IsNullOrEmpty(twilioMessagingSid) ? "NOT SET" : $"***{twilioMessagingSid.Substring(Math.Max(0, twilioMessagingSid.Length - 4))}")} (Length: {twilioMessagingSid?.Length ?? 0})");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("WARNING: Could not build connection string!");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Add connection pooling parameters to the connection string
    var enhancedConnectionString = connectionString;
    if (!string.IsNullOrEmpty(enhancedConnectionString))
    {
        // Add pooling parameters if not already present
        if (!enhancedConnectionString.Contains("Pooling="))
            enhancedConnectionString += ";Pooling=true";
        if (!enhancedConnectionString.Contains("Maximum Pool Size="))
            enhancedConnectionString += ";Maximum Pool Size=10";
        if (!enhancedConnectionString.Contains("Minimum Pool Size="))
            enhancedConnectionString += ";Minimum Pool Size=2";
        if (!enhancedConnectionString.Contains("Connection Lifetime="))
            enhancedConnectionString += ";Connection Lifetime=300"; // 5 minutes
        if (!enhancedConnectionString.Contains("Connection Idle Lifetime="))
            enhancedConnectionString += ";Connection Idle Lifetime=60"; // 1 minute
        if (!enhancedConnectionString.Contains("Timeout="))
            enhancedConnectionString += ";Timeout=30"; // 30 seconds command timeout
        if (!enhancedConnectionString.Contains("Command Timeout="))
            enhancedConnectionString += ";Command Timeout=30";
    }
    
    options.UseNpgsql(enhancedConnectionString, npgsqlOptions =>
    {
        if (builder.Environment.IsProduction())
        {
            // Enforce SSL in production
            npgsqlOptions.RemoteCertificateValidationCallback((sender, certificate, chain, errors) => true);
        }
        
        // Add resilience settings
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
            
        // Set command timeout
        npgsqlOptions.CommandTimeout(30);
    });
    
    // Enable service provider validation in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
}, ServiceLifetime.Scoped);

// Configure Identity
builder.Services.AddIdentity<Volunteer, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings["Secret"];
if (string.IsNullOrEmpty(secret))
{
    throw new InvalidOperationException("JWT Secret not configured");
}

// Get JWT issuer/audience (use frontend URL as default)
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? jwtSettings["Issuer"] ?? frontendUrl;
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSettings["Audience"] ?? frontendUrl;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add custom services
builder.Services.AddScoped<VoterImportService>();
builder.Services.AddScoped<ITwilioService, TwilioService>();
builder.Services.AddScoped<IPhoneNumberPoolService, PhoneNumberPoolService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IOptInInvitationService, OptInInvitationService>();
builder.Services.AddScoped<IAudioConversionService, AudioConversionService>();
builder.Services.AddScoped<CsvStagingService>();
builder.Services.AddScoped<VoterMappingService>();

// Configure file storage based on settings
var useS3 = builder.Configuration.GetValue<bool>("AWS:S3:UseS3");
if (useS3)
{
    // Register AWS S3 client for DigitalOcean Spaces
    var awsConfig = builder.Configuration.GetSection("AWS:S3");
    var accessKey = awsConfig["AccessKey"];
    var secretKey = awsConfig["SecretKey"];
    var serviceUrl = awsConfig["ServiceUrl"];
    
    if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey) && !string.IsNullOrEmpty(serviceUrl))
    {
        builder.Services.AddSingleton<IAmazonS3>(sp =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true
            };
            return new AmazonS3Client(accessKey, secretKey, config);
        });
    }
    
    builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
}

// Add background service monitoring
builder.Services.AddSingleton<IBackgroundServiceMonitor, BackgroundServiceMonitor>();

// Add audio cleanup service (runs in background)
builder.Services.AddHostedService<AudioCleanupService>();

// Add campaign monitor service to resume stuck campaigns
builder.Services.AddHostedService<CampaignMonitorService>();

// Add campaign scheduler service to start scheduled campaigns
builder.Services.AddHostedService<CampaignSchedulerService>();
builder.Services.AddHostedService<PhoneNumberPoolInitializer>();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<VoterImportService>();
builder.Services.AddHttpClient<GoogleMapsService>();
builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheckService>("database", tags: new[] { "db", "sql" });

// Configure Email Service
builder.Services.Configure<EmailSettings>(options =>
{
    builder.Configuration.GetSection("EmailSettings").Bind(options);
    // Override with environment variable if provided
    var sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
    if (!string.IsNullOrEmpty(sendGridApiKey))
    {
        options.SendGridApiKey = sendGridApiKey;
    }
    
    // Use the frontend URL we already determined
    if (!string.IsNullOrEmpty(frontendUrl))
    {
        options.FrontendBaseUrl = frontendUrl;
    }
    
    // Use campaign name for FromName if not explicitly set
    var campaignName = builder.Configuration["Campaign:CampaignName"] ?? builder.Configuration["REACT_APP_CAMPAIGN_NAME"];
    if (!string.IsNullOrEmpty(campaignName) && (string.IsNullOrEmpty(options.FromName) || options.FromName.Contains("Tanveer")))
    {
        options.FromName = campaignName;
    }
    
    // Override FromEmail with environment variable if provided
    var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM_ADDRESS");
    if (!string.IsNullOrEmpty(fromEmail))
    {
        options.FromEmail = fromEmail;
    }
});
builder.Services.AddTransient<IEmailTemplateService, FluidEmailTemplateService>();
builder.Services.AddTransient<IEmailService, EmailService>();

// Configure Opt-In Settings
builder.Services.Configure<OptInSettings>(builder.Configuration.GetSection("OptInSettings"));

// Configure Calling Hours Settings
builder.Services.Configure<CallingHoursSettings>(options =>
{
    builder.Configuration.GetSection("CallingHours").Bind(options);
    
    // Override with environment variables if provided
    var enforceHours = Environment.GetEnvironmentVariable("CALLING_HOURS_ENFORCE");
    if (!string.IsNullOrEmpty(enforceHours))
        options.EnforceCallingHours = bool.Parse(enforceHours);
        
    var startHour = Environment.GetEnvironmentVariable("CALLING_HOURS_START");
    if (!string.IsNullOrEmpty(startHour))
        options.StartHour = int.Parse(startHour);
        
    var endHour = Environment.GetEnvironmentVariable("CALLING_HOURS_END");
    if (!string.IsNullOrEmpty(endHour))
        options.EndHour = int.Parse(endHour);
        
    var includeWeekends = Environment.GetEnvironmentVariable("CALLING_HOURS_WEEKENDS");
    if (!string.IsNullOrEmpty(includeWeekends))
        options.IncludeWeekends = bool.Parse(includeWeekends);
        
    var timeZone = Environment.GetEnvironmentVariable("CALLING_HOURS_TIMEZONE");
    if (!string.IsNullOrEmpty(timeZone))
        options.TimeZone = timeZone;
});

var app = builder.Build();


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add global exception handling middleware (must be early in pipeline)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Serve static files (for uploaded audio)
app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();
app.UseMiddleware<ActivityTrackingMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/api/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                data = x.Value.Data,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Simple health check endpoint (public for monitoring)
app.MapGet("/api/health/simple", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Background operations monitoring endpoint (admin only)
app.MapGet("/api/admin/background-operations", (IBackgroundServiceMonitor monitor) =>
{
    var operations = monitor.GetActiveOperations();
    return Results.Ok(new 
    { 
        activeOperations = operations.Values.OrderByDescending(o => o.StartTime),
        count = operations.Count,
        timestamp = DateTime.UtcNow
    });
}).RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"));

// Debug endpoint to list all routes (SuperAdmin only)
app.MapGet("/api/debug/routes", (IServiceProvider services) =>
{
    var endpointDataSource = services.GetRequiredService<EndpointDataSource>();
    var routes = endpointDataSource.Endpoints
        .OfType<RouteEndpoint>()
        .Select(e => new { 
            Pattern = e.RoutePattern.RawText,
            Methods = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods ?? new[] { "Unknown" }
        })
        .ToList();
    return Results.Ok(routes);
}).RequireAuthorization(policy => policy.RequireRole("SuperAdmin"));


// Apply migrations and seed data before running
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    try
    {
        Console.WriteLine("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Database migrations applied successfully.");
        
        // Seed initial data (roles and default users)
        await SeedData.InitializeAsync(app.Services);
        
        // Ensure S3 bucket exists if using S3
        if (useS3)
        {
            try
            {
                var s3Service = scope.ServiceProvider.GetService<IFileStorageService>() as S3FileStorageService;
                if (s3Service != null)
                {
                    await s3Service.EnsureBucketExistsAsync();
                    Console.WriteLine("S3 bucket configured successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not configure S3 bucket: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during startup: {ex.Message}");
        Console.WriteLine($"Full error: {ex}");
    }
}

app.Run();
