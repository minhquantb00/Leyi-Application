using FluentValidation;
using Leyi.Application.Features.Identity.RegisterAccount;

namespace Leyi.Application.Features.Identity.Login;

/// <summary>
/// Input validation only. The email format is not checked here: any non-matching input is simply an invalid
/// credential, verified by ASP.NET Core Identity.
/// </summary>
internal sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Column length of <c>RefreshTokens.DeviceId</c> (ERD §6.27).</summary>
    public const int DeviceIdMaxLength = 200;

    public LoginValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(RegisterAccountValidator.EmailMaxLength).WithMessage($"Email must be at most {RegisterAccountValidator.EmailMaxLength} characters.")
            .OverridePropertyName("email");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(RegisterAccountValidator.PasswordMaxLength).WithMessage($"Password must be at most {RegisterAccountValidator.PasswordMaxLength} characters.")
            .OverridePropertyName("password");

        RuleFor(command => command.DeviceId)
            .MaximumLength(DeviceIdMaxLength).WithMessage($"Device id must be at most {DeviceIdMaxLength} characters.")
            .OverridePropertyName("deviceId");
    }
}
