using Microsoft.AspNetCore.Http;

namespace Leyi.Api.ErrorHandling;

/// <summary>
/// Ensures every ProblemDetails response carries a stable <c>code</c>.
/// Responses that already set a feature-specific code are left untouched.
/// </summary>
internal static class ProblemDetailsCodes
{
    public const string CodeKey = "code";

    public static void Apply(ProblemDetailsContext context)
    {
        var extensions = context.ProblemDetails.Extensions;
        if (extensions.ContainsKey(CodeKey))
        {
            return;
        }

        extensions[CodeKey] = ForStatus(context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode);
    }

    /// <summary>
    /// Generic code for a status. Unmapped client errors (e.g. 405, 415) get <c>REQUEST_REJECTED</c> rather than a
    /// server error code; anything else unmapped is a server failure.
    /// </summary>
    public static string ForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ErrorCodes.ValidationError,
        StatusCodes.Status401Unauthorized => ErrorCodes.Unauthorized,
        StatusCodes.Status403Forbidden => ErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => ErrorCodes.ResourceNotFound,
        StatusCodes.Status408RequestTimeout => ErrorCodes.RequestTimeout,
        StatusCodes.Status409Conflict => ErrorCodes.Conflict,
        StatusCodes.Status413PayloadTooLarge => ErrorCodes.RequestBodyTooLarge,
        StatusCodes.Status429TooManyRequests => ErrorCodes.RateLimited,
        StatusCodes.Status431RequestHeaderFieldsTooLarge => ErrorCodes.RequestHeadersTooLarge,
        StatusCodes.Status503ServiceUnavailable => ErrorCodes.ServiceUnavailable,
        >= 400 and < 500 => ErrorCodes.RequestRejected,
        _ => ErrorCodes.InternalError,
    };
}
