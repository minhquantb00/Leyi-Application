using Leyi.Domain.Common;

namespace Leyi.Domain.Sessions;

/// <summary>Explainability record: why an activity was selected (ERD §6.20).</summary>
public sealed class SessionActivityReason : Entity
{
    private SessionActivityReason()
    {
    }

    private SessionActivityReason(Guid id, Guid sessionActivityId, SelectionReason reason, decimal? scoreContribution)
        : base(id)
    {
        SessionActivityId = sessionActivityId;
        Reason = reason;
        ScoreContribution = scoreContribution;
    }

    public Guid SessionActivityId { get; private set; }

    public SelectionReason Reason { get; private set; }

    /// <summary>Contribution before final normalization (weighted signal or modifier).</summary>
    public decimal? ScoreContribution { get; private set; }

    internal static SessionActivityReason Create(Guid sessionActivityId, ActivitySelectionReason reason) =>
        new(NewId(), sessionActivityId, Guard.Defined(reason.Reason), reason.ScoreContribution);
}
