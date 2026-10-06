using FluentValidation;
using Leyi.Application.Features.Identity.RefreshAccessToken;

namespace Leyi.Application.Features.Identity.Logout;

/// <summary>Input guard only; whether the token exists or belongs to the user is never reported (Logout.md §4).</summary>
internal sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MaximumLength(RefreshAccessTokenValidator.RefreshTokenMaxLength)
            .WithMessage($"Refresh token must be at most {RefreshAccessTokenValidator.RefreshTokenMaxLength} characters.")
            .OverridePropertyName("refreshToken");
    }
}
