using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Server.IIS;
using System.Text;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using HooverCanvassingApi;

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
            // Production CORS - allow the deployed app domain
            policy.WithOrigins("https://t4h-canvas-2uwxt.ondigitalocean.app")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Configure Entity Framework
var connectionString = builder.Configuration["CUSTOM_DATABASE_CONNECTION"] ?? builder.Configuration.GetConnectionString("DefaultConnection");
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
builder.Services.AddHttpClient();

var app = builder.Build();


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add request logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"REQUEST: {context.Request.Method} {context.Request.Path} from {context.Request.Headers["User-Agent"].FirstOrDefault()}");
    await next();
    Console.WriteLine($"RESPONSE: {context.Response.StatusCode}");
});

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Test POST endpoint
app.MapPost("/api/test", (dynamic body) => Results.Ok(new { message = "POST works!", received = body }));

// Debug endpoint to list all routes
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
});

// Apply migrations and seed data before running
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Database migrations applied successfully.");
        
        // Seed initial data (roles and default users)
        await SeedData.InitializeAsync(app.Services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during startup: {ex.Message}");
    }
}

app.Run();
