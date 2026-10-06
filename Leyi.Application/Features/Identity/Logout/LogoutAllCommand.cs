using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.Logout;

/// <summary>Ends every device session of the authenticated user (Logout.md). No request body.</summary>
public sealed record LogoutAllCommand(Guid UserId) : ICommand<Result<Unit>>;
