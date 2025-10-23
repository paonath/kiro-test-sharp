using BoltWebAPI.Models.Domain;
using FluentValidation;

namespace BoltWebAPI.Validators;

/// <summary>
/// Validator for ExecutionHistoryQuery
/// </summary>
public class ExecutionHistoryQueryValidator : AbstractValidator<ExecutionHistoryQuery>
{
    private static readonly string[] ValidExecutionTypes = { "Command", "Task", "Plan" };
    private static readonly string[] ValidStates = { "Queued", "Running", "Completed", "Failed", "Cancelled" };
    private static readonly string[] ValidSortFields = { "StartedAt", "CompletedAt", "ExecutionName", "State", "DurationMs" };
    private static readonly string[] ValidSortDirections = { "asc", "desc" };

    public ExecutionHistoryQueryValidator()
    {
        RuleFor(x => x.ExecutionType)
            .Must(t => string.IsNullOrEmpty(t) || ValidExecutionTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.ExecutionType))
            .WithMessage($"ExecutionType must be one of: {string.Join(", ", ValidExecutionTypes)}");

        RuleFor(x => x.State)
            .Must(s => string.IsNullOrEmpty(s) || ValidStates.Contains(s, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.State))
            .WithMessage($"State must be one of: {string.Join(", ", ValidStates)}");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("PageNumber must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.SortBy)
            .Must(s => ValidSortFields.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortFields)}");

        RuleFor(x => x.SortDirection)
            .Must(d => ValidSortDirections.Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be 'asc' or 'desc'");

        RuleFor(x => x)
            .Must(x => !x.StartDateFrom.HasValue || !x.StartDateTo.HasValue || x.StartDateFrom.Value <= x.StartDateTo.Value)
            .WithMessage("StartDateFrom must be less than or equal to StartDateTo")
            .When(x => x.StartDateFrom.HasValue && x.StartDateTo.HasValue);
    }
}
