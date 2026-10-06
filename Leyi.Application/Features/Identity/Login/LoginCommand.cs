using System.Text;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.Login;

/// <summary>Authenticates with email/password and starts a new device session (Login.md).</summary>
public sealed record LoginCommand(string Email, string Password, string? DeviceId) : ICommand<Result<LoginResult>>
{
    /// <summary>The password is never written by <see cref="object.ToString"/> (it may end up in logs).</summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Email = ").Append(Email).Append(", Password = ***, DeviceId = ").Append(DeviceId);
        return true;
    }
}
