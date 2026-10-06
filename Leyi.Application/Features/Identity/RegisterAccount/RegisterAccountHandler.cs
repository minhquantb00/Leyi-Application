using FluentValidation;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.RegisterAccount;

/// <summary>
/// Validates input, then creates the identity account. Registration does not complete onboarding, create a
/// learning goal/plan, assign privileged roles or issue tokens (RegisterAccount.md, C8).
/// </summary>
internal sealed class RegisterAccountHandler : ICommandHandler<RegisterAccountCommand, Result<RegisterAccountResult>>
{
    private readonly IValidator<RegisterAccountCommand> validator;
    private readonly IAccountRegistrar accountRegistrar;

    public RegisterAccountHandler(IValidator<RegisterAccountCommand> validator, IAccountRegistrar accountRegistrar)
    {
        this.validator = validator;
        this.accountRegistrar = accountRegistrar;
    }

    public async Task<Result<RegisterAccountResult>> HandleAsync(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedInput = command with { Email = command.Email?.Trim() ?? string.Empty };

        var validation = await validator.ValidateAsync(normalizedInput, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var registration = await accountRegistrar.RegisterAsync(normalizedInput.Email, normalizedInput.Password, cancellationToken);

        return registration.Status switch
        {
            AccountRegistrationStatus.Registered => new RegisterAccountResult(registration.UserId, registration.Email),
            AccountRegistrationStatus.EmailAlreadyExists => RegisterAccountErrors.EmailAlreadyExists,
            AccountRegistrationStatus.InvalidEmail => RegisterAccountErrors.InvalidEmail(),
            AccountRegistrationStatus.PasswordRejected => RegisterAccountErrors.PasswordInvalid(registration.UnmetPasswordRequirements),
            _ => throw new InvalidOperationException($"Unknown registration status {registration.Status}."),
        };
    }
}
