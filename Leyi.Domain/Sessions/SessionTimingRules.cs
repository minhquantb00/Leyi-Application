using Leyi.Domain.Common;

namespace Leyi.Domain.Sessions;

/// <summary>
/// Time rules applied by <see cref="LearningSession"/> (values come from <see cref="Policies.SessionPolicy"/>):
/// active-time idle cap, resume continue threshold and open-session expiry threshold.
/// </summary>
public sealed record SessionTimingRules
{
    public SessionTimingRules(TimeSpan idleCap, TimeSpan continueThreshold, TimeSpan expiryThreshold)
    {
        IdleCap = Guard.Positive(idleCap);
        ContinueThreshold = Guard.Positive(continueThreshold);
        ExpiryThreshold = Guard.Positive(expiryThreshold);

        if (continueThreshold >= expiryThreshold)
        {
            throw new ArgumentException("The continue threshold must be shorter than the expiry threshold.", nameof(continueThreshold));
        }
    }

    /// <summary>Maximum active time credited for one gap between interactions (V1: 120 seconds).</summary>
    public TimeSpan IdleCap { get; }

    public TimeSpan ContinueThreshold { get; }

    public TimeSpan ExpiryThreshold { get; }
}
