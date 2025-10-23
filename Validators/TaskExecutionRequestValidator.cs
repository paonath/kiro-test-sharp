using BoltWebAPI.Models.Domain;
using FluentValidation;

namespace BoltWebAPI.Validators;

/// <summary>
/// Validator for task execution requests
/// </summary>
public class TaskExecutionRequestValidator : AbstractValidator<TaskExecutionRequest>
{
    public TaskExecutionRequestValidator()
    {
        RuleFor(x => x.TaskName)
            .NotEmpty()
            .WithMessage("Task name is required")
            .Matches(@"^[a-zA-Z0-9_:]+$")
            .WithMessage("Task name must contain only alphanumeric characters, underscores, and colons");

        RuleFor(x => x.Targets)
            .NotEmpty()
            .WithMessage("At least one target must be specified");

        RuleFor(x => x.Targets)
            .Must(targets => targets != null && targets.All(t => !string.IsNullOrWhiteSpace(t)))
            .WithMessage("All target values must be non-empty")
            .When(x => x.Targets != null);

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
