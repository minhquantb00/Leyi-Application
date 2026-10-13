using Leyi.Domain.Common;

namespace Leyi.Domain.Sessions.Scoring;

/// <summary>
/// The five PriorityScore signals of a (KU, Dimension) candidate (ASE §11–18), each on the 0–100 scale. Produced by
/// <see cref="SessionPriorityCalculator"/>'s signal methods from already-loaded state.
/// </summary>
public sealed record PriorityComponents
{
    public PriorityComponents(decimal reviewUrgency, decimal dimensionWeakness, decimal recentError, decimal curriculumRelevance, decimal reinforcementNeed)
    {
        ReviewUrgency = Guard.InRange(reviewUrgency, 0m, 100m);
        DimensionWeakness = Guard.InRange(dimensionWeakness, 0m, 100m);
        RecentError = Guard.InRange(recentError, 0m, 100m);
        CurriculumRelevance = Guard.InRange(curriculumRelevance, 0m, 100m);
        ReinforcementNeed = Guard.InRange(reinforcementNeed, 0m, 100m);
    }

    public decimal ReviewUrgency { get; }

    public decimal DimensionWeakness { get; }

    public decimal RecentError { get; }

    public decimal CurriculumRelevance { get; }

    public decimal ReinforcementNeed { get; }
}
