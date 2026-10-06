using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Leyi.Api.ErrorHandling;

/// <summary>
/// Last-resort handler for unhandled exceptions. Logs the exception and returns a generic
/// 500 ProblemDetails without exposing internal details to the client.
/// </summary>
internal sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        this.problemDetailsService = problemDetailsService;
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Unreadable requests are client errors with a stable code per status (400 VALIDATION_ERROR,
        // 408 REQUEST_TIMEOUT, 413 REQUEST_BODY_TOO_LARGE, 431 REQUEST_HEADERS_TOO_LARGE, other 4xx REQUEST_REJECTED),
        // not server failures. The exception message is not returned because it may echo request content.
        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.StatusCode = badRequest.StatusCode;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = badRequest.StatusCode,
                    Title = "The request could not be read.",
                    Extensions = { [ProblemDetailsCodes.CodeKey] = ProblemDetailsCodes.ForStatus(badRequest.StatusCode) },
                },
            });
        }

        LogUnhandledException(logger, httpContext.Request.Method, httpContext.Request.Path, exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, string method, PathString path, Exception exception);
}
