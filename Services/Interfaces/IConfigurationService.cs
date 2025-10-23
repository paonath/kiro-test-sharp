using BoltWebAPI.Models.Domain;

namespace BoltWebAPI.Services.Interfaces;

/// <summary>
/// Service for managing Bolt configuration
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the current Bolt configuration
    /// </summary>
    /// <returns>Bolt configuration</returns>
    /// <exception cref="FileNotFoundException">Thrown when configuration file is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration cannot be parsed</exception>
    Task<BoltConfiguration> GetConfigurationAsync();

    /// <summary>
    /// Updates the Bolt configuration from YAML content
    /// </summary>
    /// <param name="request">Update configuration request</param>
    /// <param name="userId">User performing the operation</param>
    /// <returns>Updated configuration</returns>
    /// <exception cref="ArgumentException">Thrown when YAML is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration cannot be updated</exception>
    Task<BoltConfiguration> UpdateConfigurationAsync(UpdateConfigurationRequest request, string userId);

    /// <summary>
    /// Gets transport configuration for a specific transport
    /// </summary>
    /// <param name="transport">Transport name (ssh, winrm, local, etc.)</param>
    /// <returns>Transport configuration</returns>
    /// <exception cref="KeyNotFoundException">Thrown when transport is not configured</exception>
    /// <exception cref="FileNotFoundException">Thrown when configuration file is not found</exception>
    Task<TransportConfig> GetTransportConfigAsync(string transport);

    /// <summary>
    /// Updates transport configuration for a specific transport
    /// </summary>
    /// <param name="request">Update transport request</param>
    /// <param name="userId">User performing the operation</param>
    /// <returns>Updated transport configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration cannot be updated</exception>
    Task<TransportConfig> UpdateTransportConfigAsync(UpdateTransportRequest request, string userId);

    /// <summary>
    /// Validates configuration YAML content without saving
    /// </summary>
    /// <param name="yamlContent">YAML content to validate</param>
    /// <returns>Validation result</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(string yamlContent);

    /// <summary>
    /// Gets the modulepath configuration
    /// </summary>
    /// <returns>List of module paths</returns>
    /// <exception cref="FileNotFoundException">Thrown when configuration file is not found</exception>
    Task<IEnumerable<string>> GetModulepathAsync();

    /// <summary>
    /// Updates the modulepath configuration
    /// </summary>
    /// <param name="modulepath">List of module paths</param>
    /// <param name="userId">User performing the operation</param>
    /// <returns>Updated modulepath</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration cannot be updated</exception>
    Task<IEnumerable<string>> UpdateModulepathAsync(List<string> modulepath, string userId);
}
