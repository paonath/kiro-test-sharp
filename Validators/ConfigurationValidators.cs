using BoltWebAPI.Models.Domain;
using FluentValidation;

namespace BoltWebAPI.Validators;

/// <summary>
/// Validator for UpdateConfigurationRequest
/// </summary>
public class UpdateConfigurationRequestValidator : AbstractValidator<UpdateConfigurationRequest>
{
    public UpdateConfigurationRequestValidator()
    {
        RuleFor(x => x.YamlContent)
            .NotEmpty()
            .WithMessage("YAML content is required")
            .Must(BeValidYaml)
            .WithMessage("YAML content must be valid");
    }

    private bool BeValidYaml(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            return false;

        try
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            deserializer.Deserialize<object>(yamlContent);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for UpdateTransportRequest
/// </summary>
public class UpdateTransportRequestValidator : AbstractValidator<UpdateTransportRequest>
{
    private static readonly string[] ValidTransports = { "ssh", "winrm", "local", "docker", "remote", "pcp" };

    public UpdateTransportRequestValidator()
    {
        RuleFor(x => x.Transport)
            .NotEmpty()
            .WithMessage("Transport name is required")
            .Must(t => ValidTransports.Contains(t.ToLower()))
            .WithMessage($"Transport must be one of: {string.Join(", ", ValidTransports)}");

        RuleFor(x => x.Config)
            .NotNull()
            .WithMessage("Transport configuration is required");

        RuleFor(x => x.Config.Port)
            .InclusiveBetween(1, 65535)
            .When(x => x.Config?.Port.HasValue == true)
            .WithMessage("Port must be between 1 and 65535");

        RuleFor(x => x.Config.ConnectTimeout)
            .GreaterThan(0)
            .When(x => x.Config?.ConnectTimeout.HasValue == true)
            .WithMessage("Connect timeout must be greater than 0");

        RuleFor(x => x.Config.User)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Config?.User))
            .WithMessage("User must not exceed 100 characters");

        RuleFor(x => x.Config.Tmpdir)
            .Must(BeValidPath)
            .When(x => !string.IsNullOrEmpty(x.Config?.Tmpdir))
            .WithMessage("Tmpdir must be a valid path");
    }

    private bool BeValidPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        try
        {
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
