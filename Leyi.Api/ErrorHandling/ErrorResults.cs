using Leyi.Application.Abstractions.Results;

namespace Leyi.Api.ErrorHandling;

/// <summary>Maps an application <see cref="Error"/> to a ProblemDetails response carrying the stable <c>code</c>.</summary>
internal static class ErrorResults
{
    public const string DetailsKey = "errors";

    public static IResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Unknown error type."),
        };

        var extensions = new Dictionary<string, object?> { [ProblemDetailsCodes.CodeKey] = error.Code };

        if (error.Type == ErrorType.Validation)
        {
            return TypedResults.ValidationProblem(
                error.Details.ToDictionary(detail => detail.Key, detail => detail.Value),
                title: error.Message,
                extensions: extensions);
        }

        // Non-validation errors keep their own status; details use the same "errors" shape as validation problems.
        if (error.Details.Count > 0)
        {
            extensions[DetailsKey] = error.Details;
        }

        return TypedResults.Problem(title: error.Message, statusCode: statusCode, extensions: extensions);
    }
}
