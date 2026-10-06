using FluentValidation.Results;

namespace Leyi.Application.Abstractions.Results;

internal static class ValidationResultExtensions
{
    /// <summary>Groups FluentValidation failures by property into a VALIDATION_ERROR (property names are set by validators).</summary>
    public static Error ToError(this ValidationResult validationResult) =>
        Error.Validation(validationResult.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray(), StringComparer.Ordinal));
}
