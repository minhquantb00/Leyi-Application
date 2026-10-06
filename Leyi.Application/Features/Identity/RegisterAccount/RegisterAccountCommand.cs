using System.Text;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.RegisterAccount;

/// <summary>Creates a learner account with email/password (RegisterAccount.md). Issues no token.</summary>
public sealed record RegisterAccountCommand(string Email, string Password) : ICommand<Result<RegisterAccountResult>>
{
    /// <summary>The password is never written by <see cref="object.ToString"/> (it may end up in logs).</summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Email = ").Append(Email).Append(", Password = ***");
        return true;
    }
}
