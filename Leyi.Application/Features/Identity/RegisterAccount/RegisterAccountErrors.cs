using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.RegisterAccount;

/// <summary>Errors of RegisterAccount (ErrorCodeCatalog: Authentication).</summary>
public static class RegisterAccountErrors
{
    public const string EmailAlreadyExistsCode = "AUTH_EMAIL_ALREADY_EXISTS";
    public const string PasswordInvalidCode = "AUTH_PASSWORD_INVALID";

    public static readonly Error EmailAlreadyExists = new(EmailAlreadyExistsCode, "Email already registered.", ErrorType.Conflict);

    public static Error InvalidEmail() => Error.Validation(new Dictionary<string, string[]>
    {
        ["email"] = ["Email is not a valid email address."],
    });

    public static Error PasswordInvalid(IEnumerable<PasswordRequirement> unmetRequirements) =>
        new(PasswordInvalidCode, "Password does not meet the password policy.", ErrorType.Validation)
        {
            Details = new Dictionary<string, string[]>
            {
                ["password"] = unmetRequirements.Distinct().Select(Describe).ToArray(),
            },
        };

    private static string Describe(PasswordRequirement requirement) => requirement switch
    {
        PasswordRequirement.MinimumLength => "Password is too short.",
        PasswordRequirement.Digit => "Password must contain a digit.",
        PasswordRequirement.Lowercase => "Password must contain a lowercase letter.",
        PasswordRequirement.Uppercase => "Password must contain an uppercase letter.",
        PasswordRequirement.NonAlphanumeric => "Password must contain a non-alphanumeric character.",
        PasswordRequirement.UniqueCharacters => "Password must contain more distinct characters.",
        _ => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, "Unknown password requirement."),
    };
}
