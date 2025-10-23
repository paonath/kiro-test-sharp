using BoltWebAPI.Models.Domain;
using FluentValidation;

namespace BoltWebAPI.Validators;

/// <summary>
/// Validator for command execution requests
/// </summary>
public class CommandExecutionRequestValidator : AbstractValidator<CommandExecutionRequest>
{
    public CommandExecutionRequestValidator()
    {
        RuleFor(x => x.Command)
            .NotEmpty()
            .WithMessage("Command is required")
            .MaximumLength(500)
            .WithMessage("Command must not exceed 500 characters");

        RuleFor(x => x.Arguments)
            .NotNull()
            .WithMessage("Arguments array is required (can be empty)");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .WithMessage("Timeout must be greater than 0 seconds")
            .LessThanOrEqualTo(3600)
            .WithMessage("Timeout must not exceed 3600 seconds (1 hour)")
            .When(x => x.TimeoutSeconds.HasValue);

        // Security validation: prevent command injection attempts
        RuleFor(x => x.Command)
            .Must(command => !ContainsUnsafeCharacters(command))
            .WithMessage("Command contains potentially unsafe characters (&&, ||, ;, |, >, <)")
            .When(x => !string.IsNullOrWhiteSpace(x.Command));
    }

    private static bool ContainsUnsafeCharacters(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        // Check for common command injection patterns
        var unsafePatterns = new[] { "&&", "||", ";", "|", ">", "<", "`", "$(" };

        return unsafePatterns.Any(pattern => command.Contains(pattern));
    }
}
