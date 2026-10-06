using System.Text;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.Logout;

/// <summary>
/// Ends the device session identified by a refresh token (Logout.md). <paramref name="UserId"/> comes from the
/// authenticated principal, never from the request body.
/// </summary>
public sealed record LogoutCommand(Guid UserId, string RefreshToken) : ICommand<Result<Unit>>
{
    /// <summary>The refresh token is never written by <see cref="object.ToString"/> (it may end up in logs).</summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("UserId = ").Append(UserId).Append(", RefreshToken = ***");
        return true;
    }
}
