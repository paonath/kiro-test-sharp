using BoltWebAPI.Models.Domain;
using FluentValidation;

namespace BoltWebAPI.Validators;

/// <summary>
/// Validator for plan execution requests
/// </summary>
public class PlanExecutionRequestValidator : AbstractValidator<PlanExecutionRequest>
{
    public PlanExecutionRequestValidator()
    {
        RuleFor(x => x.PlanName)
            .NotEmpty()
            .WithMessage("Plan name is required")
            .Matches(@"^[a-zA-Z0-9_:]+$")
            .WithMessage("Plan name must contain only alphanumeric characters, underscores, and colons");

        RuleFor(x => x.Parameters)
            .NotNull()
            .WithMessage("Parameters dictionary is required (can be empty)");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .WithMessage("Timeout must be greater than 0 seconds")
            .LessThanOrEqualTo(3600)
            .WithMessage("Timeout must not exceed 3600 seconds (1 hour)")
            .When(x => x.TimeoutSeconds.HasValue);
    }
}
