using System.Text;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.RefreshAccessToken;

/// <summary>Rotates a refresh token and issues a new access token (RefreshAccessToken.md).</summary>
public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<Result<RefreshAccessTokenResult>>
{
    /// <summary>The refresh token is never written by <see cref="object.ToString"/> (it may end up in logs).</summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("RefreshToken = ***");
        return true;
    }
}
