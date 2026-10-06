using System.Diagnostics;
using Serilog.Context;

namespace Leyi.Api.Observability;

/// <summary>
/// Accepts an optional <c>X-Correlation-Id</c> request header (API Contract §1). When absent or invalid, the
/// W3C trace id is used. The value is echoed in the response, attached to the current trace and added to
/// every log event of the request.
/// </summary>
internal sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const string LogPropertyName = "CorrelationId";
    private const string ActivityTagName = "leyi.correlation_id";
    private const int MaxLength = 64;

    private readonly RequestDelegate next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        Activity.Current?.SetTag(ActivityTagName, correlationId);

        // Set on response start so the header survives the exception handler clearing the response.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        if (IsValid(incoming))
        {
            return incoming;
        }

        return Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
    }

    private static bool IsValid(string value) =>
        value.Length is > 0 and <= MaxLength
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
}
