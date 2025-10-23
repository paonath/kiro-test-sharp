using BoltWebAPI.Models.Domain;
using FluentValidation;

namespace BoltWebAPI.Validators;

/// <summary>
/// Validator for AddNodeRequest
/// </summary>
public class AddNodeRequestValidator : AbstractValidator<AddNodeRequest>
{
    public AddNodeRequestValidator()
    {
        RuleFor(x => x.Uri)
            .NotEmpty()
            .WithMessage("Node URI is required")
            .MaximumLength(500)
            .WithMessage("Node URI must not exceed 500 characters");

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Node name must not exceed 200 characters");

        RuleFor(x => x.Groups)
            .NotNull()
            .WithMessage("Groups list cannot be null");

        RuleForEach(x => x.Groups)
            .NotEmpty()
            .WithMessage("Group names cannot be empty")
            .MaximumLength(100)
            .WithMessage("Group name must not exceed 100 characters");
    }
}

/// <summary>
/// Validator for UpsertGroupRequest
/// </summary>
public class UpsertGroupRequestValidator : AbstractValidator<UpsertGroupRequest>
{
    public UpsertGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Group name is required")
            .MaximumLength(100)
            .WithMessage("Group name must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9_\-]+$")
            .WithMessage("Group name can only contain letters, numbers, underscores, and hyphens");

        RuleFor(x => x.Nodes)
            .NotNull()
            .WithMessage("Nodes list cannot be null");

        RuleForEach(x => x.Nodes)
            .NotEmpty()
            .WithMessage("Node URIs cannot be empty");

        RuleFor(x => x.Groups)
            .NotNull()
            .WithMessage("Groups list cannot be null");

        RuleForEach(x => x.Groups)
            .NotEmpty()
            .WithMessage("Group names cannot be empty");
    }
}

/// <summary>
/// Validator for UpdateInventoryRequest
/// </summary>
public class UpdateInventoryRequestValidator : AbstractValidator<UpdateInventoryRequest>
{
    public UpdateInventoryRequestValidator()
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
