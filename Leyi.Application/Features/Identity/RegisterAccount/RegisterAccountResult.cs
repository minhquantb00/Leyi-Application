namespace Leyi.Application.Features.Identity.RegisterAccount;

/// <summary>Created account (API Contract §2: <c>userId</c>, <c>email</c>).</summary>
public sealed record RegisterAccountResult(Guid UserId, string Email);
