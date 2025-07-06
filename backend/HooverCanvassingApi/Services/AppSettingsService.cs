using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using Microsoft.Extensions.Caching.Memory;

namespace HooverCanvassingApi.Services;

public interface IAppSettingsService
{
    Task<string> GetSettingAsync(string key, string defaultValue = "");
    Task<Dictionary<string, string>> GetPublicSettingsAsync();
    Task<Dictionary<string, string>> GetAllSettingsAsync();
    Task<(bool success, string value)> TryGetSettingAsync(string key);
    void ClearCache();
}

public class AppSettingsService : IAppSettingsService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppSettingsService> _logger;
    private const string CacheKeyPrefix = "AppSetting_";
    private const string AllSettingsCacheKey = "AllAppSettings";
    private const string PublicSettingsCacheKey = "PublicAppSettings";
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public AppSettingsService(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<AppSettingsService> logger)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetSettingAsync(string key, string defaultValue = "")
    {
        if (string.IsNullOrEmpty(key))
            return defaultValue;

        var cacheKey = $"{CacheKeyPrefix}{key}";
        
        if (_cache.TryGetValue(cacheKey, out string cachedValue))
        {
            return cachedValue;
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var setting = await context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        string value = setting?.Value ?? _configuration[key] ?? defaultValue;

        _cache.Set(cacheKey, value, _cacheExpiration);

        return value;
    }

    public async Task<Dictionary<string, string>> GetPublicSettingsAsync()
    {
        if (_cache.TryGetValue(PublicSettingsCacheKey, out Dictionary<string, string> cachedSettings))
        {
            return cachedSettings;
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var settings = await context.AppSettings
            .Where(s => s.IsPublic)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        _cache.Set(PublicSettingsCacheKey, settings, _cacheExpiration);

        return settings;
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        if (_cache.TryGetValue(AllSettingsCacheKey, out Dictionary<string, string> cachedSettings))
        {
            return cachedSettings;
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var settings = await context.AppSettings
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        _cache.Set(AllSettingsCacheKey, settings, _cacheExpiration);

        return settings;
    }

    public async Task<(bool success, string value)> TryGetSettingAsync(string key)
    {
        var value = await GetSettingAsync(key);
        return (!string.IsNullOrEmpty(value), value);
    }

    public void ClearCache()
    {
        _cache.Remove(AllSettingsCacheKey);
        _cache.Remove(PublicSettingsCacheKey);
        
        _logger.LogInformation("AppSettings cache cleared");
    }
}