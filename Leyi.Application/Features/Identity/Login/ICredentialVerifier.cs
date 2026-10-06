namespace Leyi.Application.Features.Identity.Login;

/// <summary>
/// Verifies email/password credentials with the identity provider, including account status and brute-force lockout.
/// Implementations must not reveal through the outcome whether an unknown email or a wrong password failed.
/// </summary>
public interface ICredentialVerifier
{
    Task<CredentialVerification> VerifyAsync(string email, string password, CancellationToken cancellationToken);
}

public enum CredentialVerificationStatus
{
    Authenticated,

    /// <summary>Unknown email or wrong password (indistinguishable for the caller).</summary>
    InvalidCredentials,

    /// <summary>Account status is not active, or the account is locked out.</summary>
    AccountDisabled,
}

public sealed record CredentialVerification(CredentialVerificationStatus Status, Guid UserId)
{
    public static CredentialVerification Authenticated(Guid userId) => new(CredentialVerificationStatus.Authenticated, userId);

    public static CredentialVerification InvalidCredentials() => new(CredentialVerificationStatus.InvalidCredentials, Guid.Empty);

    public static CredentialVerification AccountDisabled() => new(CredentialVerificationStatus.AccountDisabled, Guid.Empty);
}
