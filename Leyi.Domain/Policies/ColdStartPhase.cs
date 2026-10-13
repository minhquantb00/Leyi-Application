namespace Leyi.Domain.Policies;

/// <summary>Cold-start phase (ASE §25, B2) by meaningful sessions and total evidence; see <see cref="ColdStartRules.PhaseOf"/>.</summary>
public enum ColdStartPhase
{
    Bootstrap,
    Ramp,
    Adaptive,
}
