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
using HooverCanvassingApi.Middleware;
using HooverCanvassingApi;
using HooverCanvassingApi.Configuration;
using Amazon.S3;
using Amazon;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

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
            // Production CORS - allow the deployed app domains
            policy.WithOrigins(
                    "https://t4h-canvas-2uwxt.ondigitalocean.app",
                    "https://t4happ.com",
                    "https://www.t4happ.com"
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Add environment variables explicitly
builder.Configuration.AddEnvironmentVariables();

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
var twilioMessagingSid = builder.Configuration["Twilio:MessagingServiceSid"];

Console.WriteLine($"Twilio AccountSid: {(string.IsNullOrEmpty(twilioAccountSid) ? "MISSING" : $"***{twilioAccountSid.Substring(Math.Max(0, twilioAccountSid.Length - 4))}")} (Length: {twilioAccountSid?.Length ?? 0})");
Console.WriteLine($"Twilio AuthToken: {(string.IsNullOrEmpty(twilioAuthToken) ? "MISSING" : "***CONFIGURED")} (Length: {twilioAuthToken?.Length ?? 0})");
Console.WriteLine($"Twilio FromPhone: {(string.IsNullOrEmpty(twilioFromPhone) ? "MISSING" : twilioFromPhone)} (Length: {twilioFromPhone?.Length ?? 0})");
Console.WriteLine($"Twilio MessagingSid: {(string.IsNullOrEmpty(twilioMessagingSid) ? "NOT SET" : $"***{twilioMessagingSid.Substring(Math.Max(0, twilioMessagingSid.Length - 4))}")} (Length: {twilioMessagingSid?.Length ?? 0})");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("WARNING: Could not build connection string!");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        if (builder.Environment.IsProduction())
        {
            // Enforce SSL in production
            npgsqlOptions.RemoteCertificateValidationCallback((sender, certificate, chain, errors) => true);
        }
    });
});

// Configure Identity
builder.Services.AddIdentity<Volunteer, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"];
if (string.IsNullOrEmpty(secret))
{
    throw new InvalidOperationException("JWT Secret not configured");
}

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add custom services
builder.Services.AddScoped<VoterImportService>();
builder.Services.AddScoped<ITwilioService, TwilioService>();
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

// Add audio cleanup service (runs in background)
builder.Services.AddHostedService<AudioCleanupService>();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<VoterImportService>();

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
});
builder.Services.AddTransient<IEmailService, EmailService>();

// Configure Opt-In Settings
builder.Services.Configure<OptInSettings>(builder.Configuration.GetSection("OptInSettings"));

var app = builder.Build();


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve static files (for uploaded audio)
app.UseStaticFiles();

// Add request logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"REQUEST: {context.Request.Method} {context.Request.Path} from {context.Request.Headers["User-Agent"].FirstOrDefault()}");
    await next();
    Console.WriteLine($"RESPONSE: {context.Response.StatusCode}");
});

app.UseCors();

app.UseAuthentication();
app.UseMiddleware<ActivityTrackingMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint (public for monitoring)
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

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
