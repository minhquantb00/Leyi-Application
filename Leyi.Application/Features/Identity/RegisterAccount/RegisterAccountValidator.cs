using FluentValidation;

namespace Leyi.Application.Features.Identity.RegisterAccount;

/// <summary>
/// Input validation only. Email uniqueness and the password policy belong to ASP.NET Core Identity
/// (see <see cref="IAccountRegistrar"/>); they are not duplicated here.
/// </summary>
internal sealed class RegisterAccountValidator : AbstractValidator<RegisterAccountCommand>
{
    /// <summary>Identity column length of <c>Users.Email</c> (ERD §6.1).</summary>
    public const int EmailMaxLength = 256;

    /// <summary>Input length guard for the password (security rules: limit input length).</summary>
    public const int PasswordMaxLength = 128;

    public RegisterAccountValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(EmailMaxLength).WithMessage($"Email must be at most {EmailMaxLength} characters.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .OverridePropertyName("email");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(PasswordMaxLength).WithMessage($"Password must be at most {PasswordMaxLength} characters.")
            .OverridePropertyName("password");
    }
}
