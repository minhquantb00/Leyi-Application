using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.Login;

/// <summary>Errors of Login (ErrorCodeCatalog: Authentication).</summary>
public static class LoginErrors
{
    public const string InvalidCredentialsCode = "AUTH_INVALID_CREDENTIALS";
    public const string AccountDisabledCode = "AUTH_ACCOUNT_DISABLED";

    /// <summary>Same error for an unknown email and a wrong password (no account enumeration).</summary>
    public static readonly Error InvalidCredentials = new(InvalidCredentialsCode, "Invalid email or password.", ErrorType.Unauthorized);

    public static readonly Error AccountDisabled = new(AccountDisabledCode, "The account is disabled.", ErrorType.Forbidden);
}
