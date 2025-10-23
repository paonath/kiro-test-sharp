namespace BoltWebAPI.Models.Domain;

/// <summary>
/// Bolt configuration structure
/// </summary>
public class BoltConfiguration
{
    /// <summary>
    /// Bolt format version
    /// </summary>
    public int FormatVersion { get; set; } = 2;

    /// <summary>
    /// Modulepath for Bolt modules
    /// </summary>
    public List<string> Modulepath { get; set; } = new();

    /// <summary>
    /// Inventory file path
    /// </summary>
    public string? InventoryFile { get; set; }

    /// <summary>
    /// Concurrency settings
    /// </summary>
    public int? Concurrency { get; set; }

    /// <summary>
    /// Transport configuration (SSH, WinRM, etc.)
    /// </summary>
    public Dictionary<string, TransportConfig> Transport { get; set; } = new();

    /// <summary>
    /// Plugin configuration
    /// </summary>
    public Dictionary<string, object> Plugins { get; set; } = new();

    /// <summary>
    /// Log configuration
    /// </summary>
    public LogConfig? Log { get; set; }

    /// <summary>
    /// Apply settings configuration
    /// </summary>
    public Dictionary<string, object> ApplySettings { get; set; } = new();

    /// <summary>
    /// Color output setting
    /// </summary>
    public bool? Color { get; set; }

    /// <summary>
    /// Save rerun file setting
    /// </summary>
    public bool? SaveRerun { get; set; }

    /// <summary>
    /// Trusted external command setting
    /// </summary>
    public List<string> TrustedExternalCommands { get; set; } = new();
}

/// <summary>
/// Transport configuration (SSH, WinRM, etc.)
/// </summary>
public class TransportConfig
{
    /// <summary>
    /// Connection user
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Connection password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Private key path
    /// </summary>
    public string? PrivateKey { get; set; }

    /// <summary>
    /// Connection port
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// Connection timeout
    /// </summary>
    public int? ConnectTimeout { get; set; }

    /// <summary>
    /// Run as user (sudo)
    /// </summary>
    public string? RunAs { get; set; }

    /// <summary>
    /// Temporary directory
    /// </summary>
    public string? Tmpdir { get; set; }

    /// <summary>
    /// Host key check setting
    /// </summary>
    public bool? HostKeyCheck { get; set; }

    /// <summary>
    /// SSL setting (for WinRM)
    /// </summary>
    public bool? Ssl { get; set; }

    /// <summary>
    /// SSL verify setting (for WinRM)
    /// </summary>
    public bool? SslVerify { get; set; }

    /// <summary>
    /// Additional transport-specific options
    /// </summary>
    public Dictionary<string, object> Extensions { get; set; } = new();
}

/// <summary>
/// Log configuration
/// </summary>
public class LogConfig
{
    /// <summary>
    /// Console log level
    /// </summary>
    public string? Console { get; set; }

    /// <summary>
    /// File log path
    /// </summary>
    public string? File { get; set; }

    /// <summary>
    /// Append to log file
    /// </summary>
    public bool? Append { get; set; }
}

/// <summary>
/// Request to update Bolt configuration
/// </summary>
public class UpdateConfigurationRequest
{
    /// <summary>
    /// Configuration YAML content
    /// </summary>
    public string YamlContent { get; set; } = string.Empty;

    /// <summary>
    /// Optional validation only (don't save)
    /// </summary>
    public bool ValidateOnly { get; set; } = false;
}

/// <summary>
/// Request to update transport configuration
/// </summary>
public class UpdateTransportRequest
{
    /// <summary>
    /// Transport name (ssh, winrm, local, docker, etc.)
    /// </summary>
    public string Transport { get; set; } = string.Empty;

    /// <summary>
    /// Transport configuration
    /// </summary>
    public TransportConfig Config { get; set; } = new();
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Is configuration valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
