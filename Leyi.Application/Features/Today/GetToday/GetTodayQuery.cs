using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Today.GetToday;

/// <summary>Today dashboard of the authenticated learner (GetToday.md). <paramref name="UserId"/> comes from the principal.</summary>
public sealed record GetTodayQuery(Guid UserId) : IQuery<Result<GetTodayResult>>;
