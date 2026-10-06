namespace Leyi.Application.Abstractions.Results;

/// <summary>Category of an expected use-case failure; the API maps it to an HTTP status.</summary>
public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
}

/// <summary>
/// Expected use-case failure with a stable code from <c>0.Document/05.API/ErrorCodeCatalog.md</c>.
/// <see cref="Details"/> holds field-level messages (camelCase field names) for validation failures.
/// Messages are written for clients and must never contain secrets or raw provider errors.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public const string ValidationErrorCode = "VALIDATION_ERROR";

    private static readonly IReadOnlyDictionary<string, string[]> NoDetails = new Dictionary<string, string[]>();

    public IReadOnlyDictionary<string, string[]> Details { get; init; } = NoDetails;

    public static Error Validation(IReadOnlyDictionary<string, string[]> details) =>
        new(ValidationErrorCode, "One or more validation errors occurred.", ErrorType.Validation) { Details = details };
}
