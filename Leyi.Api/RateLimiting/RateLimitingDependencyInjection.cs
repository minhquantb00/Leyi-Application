using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Leyi.Api.RateLimiting;

/// <summary>Fixed-window limit configuration for one endpoint group, partitioned by client IP.</summary>
public sealed class FixedWindowLimit
{
    public int PermitLimit { get; set; }

    public TimeSpan Window { get; set; }

    public bool IsValid => PermitLimit > 0 && Window > TimeSpan.Zero;
}

/// <summary>
/// Rate limits for anonymous authentication endpoints (security rules: strongly consider for register/login/refresh).
/// Bound to the <c>RateLimiting</c> section; values are operational configuration, not business policy.
/// </summary>
public sealed class AuthRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowLimit Register { get; set; } = new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) };

    public FixedWindowLimit Login { get; set; } = new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) };

    public FixedWindowLimit Refresh { get; set; } = new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) };
}

internal static class RateLimitingDependencyInjection
{
    public const string RegisterPolicy = "auth-register";
    public const string LoginPolicy = "auth-login";
    public const string RefreshPolicy = "auth-refresh";

    private const string UnknownClient = "unknown";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthRateLimitingOptions>()
            .Bind(configuration.GetSection(AuthRateLimitingOptions.SectionName))
            .Validate(options => options.Register.IsValid, "RateLimiting:Register must have a positive PermitLimit and Window.")
            .Validate(options => options.Login.IsValid, "RateLimiting:Login must have a positive PermitLimit and Window.")
            .Validate(options => options.Refresh.IsValid, "RateLimiting:Refresh must have a positive PermitLimit and Window.")
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 429 responses use ProblemDetails; ProblemDetailsCodes adds code RATE_LIMITED.
            limiter.OnRejected = (context, cancellationToken) =>
            {
                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                return problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                    },
                });
            };

            limiter.AddPerClientFixedWindowPolicy(RegisterPolicy, options => options.Register);
            limiter.AddPerClientFixedWindowPolicy(LoginPolicy, options => options.Login);
            limiter.AddPerClientFixedWindowPolicy(RefreshPolicy, options => options.Refresh);
        });

        return services;
    }

    /// <summary>
    /// Fixed window per client IP. Behind a reverse proxy this requires ForwardedHeaders with known proxies
    /// (deployment requirement, Technology Stack spec).
    /// </summary>
    private static void AddPerClientFixedWindowPolicy(this RateLimiterOptions limiter, string policyName, Func<AuthRateLimitingOptions, FixedWindowLimit> selectLimit)
    {
        limiter.AddPolicy(policyName, httpContext =>
        {
            var limit = selectLimit(httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitingOptions>>().Value);
            return RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit.PermitLimit,
                    Window = limit.Window,
                    QueueLimit = 0,
                });
        });
    }
}
