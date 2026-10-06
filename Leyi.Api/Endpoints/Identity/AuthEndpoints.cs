using System.Globalization;
using System.Security.Claims;
using System.Text;
using Leyi.Api.Authentication;
using Leyi.Api.ErrorHandling;
using Leyi.Api.RateLimiting;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;
using Leyi.Application.Features.Identity.Login;
using Leyi.Application.Features.Identity.Logout;
using Leyi.Application.Features.Identity.RefreshAccessToken;
using Leyi.Application.Features.Identity.RegisterAccount;

namespace Leyi.Api.Endpoints.Identity;

/// <summary>Authentication endpoints (API Contract §2). Register/login/refresh are anonymous; logout/logout-all require an access token.</summary>
internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/auth").WithTags("Authentication");

        auth.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingDependencyInjection.RegisterPolicy)
            .WithName("RegisterAccount")
            .Produces<RegisterAccountResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingDependencyInjection.LoginPolicy)
            .WithName("Login")
            .Produces<TokenPairResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingDependencyInjection.RefreshPolicy)
            .WithName("RefreshAccessToken")
            .Produces<TokenPairResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        // Authenticated with the access token; the user id comes from its "sub" claim, never from the body.
        auth.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/logout-all", LogoutAllAsync)
            .RequireAuthorization()
            .WithName("LogoutAll")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterAccountRequest request,
        ICommandHandler<RegisterAccountCommand, Result<RegisterAccountResult>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RegisterAccountCommand(request.Email ?? string.Empty, request.Password ?? string.Empty), cancellationToken);

        // No resource URL exists for an account yet, so 201 carries no Location header.
        return result.IsSuccess
            ? TypedResults.Created((string?)null, new RegisterAccountResponse(result.Value.UserId, result.Value.Email))
            : result.Error.ToProblem();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ICommandHandler<LoginCommand, Result<LoginResult>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LoginCommand(request.Email ?? string.Empty, request.Password ?? string.Empty, request.DeviceId), cancellationToken);

        return result.IsSuccess
            ? TokenPair(httpContext, new TokenPairResponse(result.Value.AccessToken, result.Value.AccessTokenExpiresAtUtc, result.Value.RefreshToken))
            : result.Error.ToProblem();
    }

    private static async Task<IResult> RefreshAsync(
        RefreshAccessTokenRequest request,
        ICommandHandler<RefreshAccessTokenCommand, Result<RefreshAccessTokenResult>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RefreshAccessTokenCommand(request.RefreshToken ?? string.Empty), cancellationToken);

        return result.IsSuccess
            ? TokenPair(httpContext, new TokenPairResponse(result.Value.AccessToken, result.Value.AccessTokenExpiresAtUtc, result.Value.RefreshToken))
            : result.Error.ToProblem();
    }

    private static async Task<IResult> LogoutAsync(
        LogoutRequest request,
        ClaimsPrincipal user,
        ICommandHandler<LogoutCommand, Result<Unit>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LogoutCommand(user.GetRequiredUserId(), request.RefreshToken ?? string.Empty), cancellationToken);

        // 204 whether or not the token existed or was the caller's (Logout.md §4: no information leak).
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<IResult> LogoutAllAsync(
        ClaimsPrincipal user,
        ICommandHandler<LogoutAllCommand, Result<Unit>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LogoutAllCommand(user.GetRequiredUserId()), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    /// <summary>200 with the token pair. Token responses must not be stored by caches or intermediaries.</summary>
    private static IResult TokenPair(HttpContext httpContext, TokenPairResponse response)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        return TypedResults.Ok(response);
    }
}

/// <summary>POST /auth/register request. Explicit contract; never bound to the Identity user.</summary>
public sealed record RegisterAccountRequest(string? Email, string? Password)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Email = ").Append(Email).Append(", Password = ***");
        return true;
    }
}

/// <summary>POST /auth/register 201 response (API Contract §2). Exposes no Identity internals.</summary>
public sealed record RegisterAccountResponse(Guid UserId, string Email);

/// <summary>POST /auth/login request (API Contract §2). <c>deviceId</c> is an optional, informational installation id.</summary>
public sealed record LoginRequest(string? Email, string? Password, string? DeviceId)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Email = ").Append(Email).Append(", Password = ***, DeviceId = ").Append(DeviceId);
        return true;
    }
}

/// <summary>
/// POST /auth/login and /auth/refresh 200 response (API Contract §2, same shape): the token pair;
/// <c>expiresAtUtc</c> is the access token expiry.
/// Token values are never written by <see cref="object.ToString"/>.
/// </summary>
public sealed record TokenPairResponse(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("AccessToken = ***, ExpiresAtUtc = ")
            .Append(ExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture))
            .Append(", RefreshToken = ***");
        return true;
    }
}

/// <summary>POST /auth/refresh request (API Contract §2). Explicit contract; the token is never written by <see cref="object.ToString"/>.</summary>
public sealed record RefreshAccessTokenRequest(string? RefreshToken)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("RefreshToken = ***");
        return true;
    }
}

/// <summary>POST /auth/logout request (API Contract §2). The token is never written by <see cref="object.ToString"/>.</summary>
public sealed record LogoutRequest(string? RefreshToken)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("RefreshToken = ***");
        return true;
    }
}
