using Leyi.Application.Abstractions.Results;
using Leyi.Application.Features.Identity.Login;

namespace Leyi.Application.Features.Identity.RefreshAccessToken;

/// <summary>Errors of RefreshAccessToken (ErrorCodeCatalog: Authentication).</summary>
public static class RefreshAccessTokenErrors
{
    public const string InvalidCode = "AUTH_REFRESH_TOKEN_INVALID";
    public const string RevokedCode = "AUTH_REFRESH_TOKEN_REVOKED";

    /// <summary>Unknown or expired token. Does not say which.</summary>
    public static readonly Error Invalid = new(InvalidCode, "The refresh token is invalid or expired.", ErrorType.Unauthorized);

    /// <summary>Revoked token, including reuse of a rotated token (the family is revoked). Does not say why.</summary>
    public static readonly Error Revoked = new(RevokedCode, "The refresh token has been revoked.", ErrorType.Unauthorized);

    public static readonly Error AccountDisabled = new(LoginErrors.AccountDisabledCode, "The account is disabled.", ErrorType.Forbidden);
}
