namespace Leyi.Api.ErrorHandling;

/// <summary>
/// General error codes from <c>0.Document/05.API/ErrorCodeCatalog.md</c>.
/// Feature-specific codes are defined with their features.
/// </summary>
internal static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string RequestTimeout = "REQUEST_TIMEOUT";
    public const string RequestBodyTooLarge = "REQUEST_BODY_TOO_LARGE";
    public const string RequestHeadersTooLarge = "REQUEST_HEADERS_TOO_LARGE";
    public const string RequestRejected = "REQUEST_REJECTED";
}
