using BoltWebAPI.Data;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Models.Entities;
using BoltWebAPI.Services.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BoltWebAPI.Services.Implementations;

/// <summary>
/// Service for managing Bolt configuration with YAML parsing
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly BoltDbContext _dbContext;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IDeserializer _yamlDeserializer;
    private readonly ISerializer _yamlSerializer;

    public ConfigurationService(
        IConfiguration configuration,
        BoltDbContext dbContext,
        ILogger<ConfigurationService> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    private string GetConfigFilePath()
    {
        var configPath = _configuration["Bolt:ConfigFile"] ?? "bolt-project.yaml";
        return Path.GetFullPath(configPath);
    }

    public async Task<BoltConfiguration> GetConfigurationAsync()
    {
        var configPath = GetConfigFilePath();

        if (!File.Exists(configPath))
        {
            _logger.LogWarning("Configuration file not found at {Path}, returning default configuration", configPath);
            return new BoltConfiguration();
        }

        try
        {
            var yamlContent = await File.ReadAllTextAsync(configPath);
            var config = _yamlDeserializer.Deserialize<BoltConfiguration>(yamlContent);

            _logger.LogDebug("Loaded Bolt configuration from {Path}", configPath);

            return config ?? new BoltConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse configuration file at {Path}", configPath);
            throw new InvalidOperationException($"Failed to parse configuration file: {ex.Message}", ex);
        }
    }

    public async Task<BoltConfiguration> UpdateConfigurationAsync(UpdateConfigurationRequest request, string userId)
    {
        // Validate YAML first
        var validationResult = await ValidateConfigurationAsync(request.YamlContent);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid configuration YAML: {string.Join(", ", validationResult.Errors)}");
        }

        if (request.ValidateOnly)
        {
            // Just return the parsed configuration without saving
            var validatedConfig = _yamlDeserializer.Deserialize<BoltConfiguration>(request.YamlContent);
            return validatedConfig ?? new BoltConfiguration();
        }

        var config = _yamlDeserializer.Deserialize<BoltConfiguration>(request.YamlContent);
        if (config == null)
        {
            throw new ArgumentException("Failed to parse configuration YAML");
        }

        var configPath = GetConfigFilePath();
        await File.WriteAllTextAsync(configPath, request.YamlContent);

        await LogConfigurationChangeAsync(userId, "bolt_config", "Updated entire configuration from YAML");

        _logger.LogInformation("Updated Bolt configuration from YAML (User: {UserId})", userId);

        return config;
    }

    public async Task<TransportConfig> GetTransportConfigAsync(string transport)
    {
        var config = await GetConfigurationAsync();

        if (!config.Transport.TryGetValue(transport, out var transportConfig))
        {
            throw new KeyNotFoundException($"Transport '{transport}' not configured");
        }

        return transportConfig;
    }

    public async Task<TransportConfig> UpdateTransportConfigAsync(UpdateTransportRequest request, string userId)
    {
        var config = await GetConfigurationAsync();

        // Update or add transport configuration
        config.Transport[request.Transport] = request.Config;

        await SaveConfigurationAsync(config, userId, $"Updated transport configuration: {request.Transport}");

        _logger.LogInformation("Updated transport {Transport} configuration (User: {UserId})", request.Transport, userId);

        return request.Config;
    }

    public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(string yamlContent)
    {
        var result = new ConfigurationValidationResult { IsValid = true };

        try
        {
            var config = _yamlDeserializer.Deserialize<BoltConfiguration>(yamlContent);

            if (config == null)
            {
                result.IsValid = false;
                result.Errors.Add("Failed to parse configuration YAML");
                return result;
            }

            // Validate format version
            if (config.FormatVersion != 2)
            {
                result.Warnings.Add($"Configuration format version is {config.FormatVersion}, expected 2");
            }

            // Validate concurrency value
            if (config.Concurrency.HasValue && config.Concurrency.Value < 1)
            {
                result.IsValid = false;
                result.Errors.Add("Concurrency must be greater than 0");
            }

            // Validate modulepath exists
            if (config.Modulepath.Any())
            {
                foreach (var path in config.Modulepath)
                {
                    if (!Directory.Exists(path))
                    {
                        result.Warnings.Add($"Modulepath directory does not exist: {path}");
                    }
                }
            }

            // Validate transport configurations
            foreach (var (transportName, transportConfig) in config.Transport)
            {
                if (transportConfig.Port.HasValue && (transportConfig.Port.Value < 1 || transportConfig.Port.Value > 65535))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Invalid port for transport {transportName}: {transportConfig.Port}");
                }

                if (transportConfig.ConnectTimeout.HasValue && transportConfig.ConnectTimeout.Value < 1)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Invalid connect timeout for transport {transportName}: {transportConfig.ConnectTimeout}");
                }
            }

            // Validate log configuration
            if (config.Log != null)
            {
                var validLogLevels = new[] { "trace", "debug", "info", "warn", "error", "fatal" };
                if (config.Log.Console != null && !validLogLevels.Contains(config.Log.Console.ToLower()))
                {
                    result.Warnings.Add($"Unknown log level: {config.Log.Console}");
                }
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"YAML parsing error: {ex.Message}");
        }

        return result;
    }

    public async Task<IEnumerable<string>> GetModulepathAsync()
    {
        var config = await GetConfigurationAsync();
        return config.Modulepath;
    }

    public async Task<IEnumerable<string>> UpdateModulepathAsync(List<string> modulepath, string userId)
    {
        var config = await GetConfigurationAsync();
        config.Modulepath = modulepath;

        await SaveConfigurationAsync(config, userId, "Updated modulepath configuration");

        _logger.LogInformation("Updated modulepath configuration (User: {UserId})", userId);

        return modulepath;
    }

    private async Task SaveConfigurationAsync(BoltConfiguration config, string userId, string changeDescription)
    {
        var configPath = GetConfigFilePath();
        var yamlContent = _yamlSerializer.Serialize(config);

        await File.WriteAllTextAsync(configPath, yamlContent);

        await LogConfigurationChangeAsync(userId, "bolt_config", changeDescription);
    }

    private async Task LogConfigurationChangeAsync(string userId, string configType, string changeDescription)
    {
        var change = new ConfigurationChangeEntity
        {
            UserId = userId,
            ConfigurationType = configType,
            ChangeDescription = changeDescription,
            ChangedAt = DateTime.UtcNow
        };

        _dbContext.ConfigurationChanges.Add(change);
        await _dbContext.SaveChangesAsync();
    }
}
