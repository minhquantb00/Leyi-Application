namespace Leyi.Application.Features.Identity.RegisterAccount;

/// <summary>
/// Port for creating an identity account. Implemented in Infrastructure with ASP.NET Core Identity, which owns
/// email normalization, uniqueness and password hashing/policy. The database unique index is the final guard
/// against concurrent registrations of the same email.
/// </summary>
public interface IAccountRegistrar
{
    /// <summary>Creates the account with <c>EmailConfirmed = false</c> (I3). The password is never stored in clear text.</summary>
    Task<AccountRegistration> RegisterAsync(string email, string password, CancellationToken cancellationToken);
}

public enum AccountRegistrationStatus
{
    Registered,
    EmailAlreadyExists,
    InvalidEmail,
    PasswordRejected,
}

/// <summary>Password policy requirements reported by the identity provider (no provider-specific text).</summary>
public enum PasswordRequirement
{
    MinimumLength,
    Digit,
    Lowercase,
    Uppercase,
    NonAlphanumeric,
    UniqueCharacters,
}

/// <summary>Outcome of <see cref="IAccountRegistrar.RegisterAsync"/>.</summary>
public sealed record AccountRegistration
{
    private AccountRegistration(AccountRegistrationStatus status)
    {
        Status = status;
    }

    public AccountRegistrationStatus Status { get; }

    public Guid UserId { get; private init; }

    public string Email { get; private init; } = string.Empty;

    public IReadOnlyList<PasswordRequirement> UnmetPasswordRequirements { get; private init; } = [];

    public static AccountRegistration Registered(Guid userId, string email) =>
        new(AccountRegistrationStatus.Registered) { UserId = userId, Email = email };

    public static AccountRegistration EmailAlreadyExists() => new(AccountRegistrationStatus.EmailAlreadyExists);

    public static AccountRegistration InvalidEmail() => new(AccountRegistrationStatus.InvalidEmail);

    public static AccountRegistration PasswordRejected(IReadOnlyList<PasswordRequirement> unmetRequirements) =>
        new(AccountRegistrationStatus.PasswordRejected) { UnmetPasswordRequirements = unmetRequirements };
}
