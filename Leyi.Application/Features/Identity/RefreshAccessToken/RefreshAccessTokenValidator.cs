using FluentValidation;

namespace Leyi.Application.Features.Identity.RefreshAccessToken;

/// <summary>Input guard only; whether the token exists, is active or was reused is decided against the database.</summary>
internal sealed class RefreshAccessTokenValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    /// <summary>Generous bound on input length (issued tokens are 43 base64url characters).</summary>
    public const int RefreshTokenMaxLength = 256;

    public RefreshAccessTokenValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MaximumLength(RefreshTokenMaxLength).WithMessage($"Refresh token must be at most {RefreshTokenMaxLength} characters.")
            .OverridePropertyName("refreshToken");
    }
}
