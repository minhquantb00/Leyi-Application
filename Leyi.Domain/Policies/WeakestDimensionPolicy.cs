using Leyi.Domain.Learning;

namespace Leyi.Domain.Policies;

/// <summary>
/// <c>weakestDimension</c> on Today (TD-G3): a dimension is reported only when the learner's user-wide skill profile shows
/// it is clearly weaker. Samples are dimension masteries with at least <see cref="MinEvidenceCount"/> evidence (MEDIUM
/// confidence; unmeasured and low-evidence dimensions are excluded, never scored 0); each dimension is the plain mean
/// of its samples. No result for a Bootstrap learner, fewer than <see cref="MinDimensions"/> dimensions, a spread
/// (max − min) below <see cref="ImbalanceThreshold"/>, or a tie at the minimum. Values in <see cref="V1"/> are V1
/// defaults (N1) taken from <see cref="ConfidencePolicy"/> and <see cref="SessionPolicy"/>.
/// </summary>
public sealed record WeakestDimensionPolicy
{
    public static WeakestDimensionPolicy V1 { get; } = new()
    {
        Version = "weakest-dimension-v1.0",
        MinEvidenceCount = ConfidencePolicy.V1.MediumMinEvidenceCount,
        MinDimensions = 2,
        ImbalanceThreshold = SessionPolicy.V1.SkillImbalanceThreshold,
    };

    public required string Version { get; init; }

    /// <summary>Evidence a dimension mastery needs to count as a sample.</summary>
    public required int MinEvidenceCount { get; init; }

    public required int MinDimensions { get; init; }

    /// <summary>Minimum spread between the strongest and weakest dimension averages (score points).</summary>
    public required decimal ImbalanceThreshold { get; init; }

    /// <param name="isBootstrap">Learner in the Bootstrap cold-start phase.</param>
    /// <param name="averages">Mean score per dimension over eligible samples (at most one entry per dimension).</param>
    public MasteryDimension? Select(bool isBootstrap, IReadOnlyCollection<DimensionAverage> averages)
    {
        ArgumentNullException.ThrowIfNull(averages);
        if (averages.Select(average => average.Dimension).Distinct().Count() != averages.Count)
        {
            throw new ArgumentException("Each dimension may appear only once.", nameof(averages));
        }

        if (isBootstrap || averages.Count < MinDimensions)
        {
            return null;
        }

        var min = averages.Min(average => average.AverageScore);
        var max = averages.Max(average => average.AverageScore);
        if (max - min < ImbalanceThreshold)
        {
            return null;
        }

        var weakest = averages.Where(average => average.AverageScore == min).ToList();
        return weakest is [var single] ? single.Dimension : null;
    }

    public void Validate()
    {
        PolicyValidation.RequireVersion(Version);
        PolicyValidation.Require(MinEvidenceCount >= 1, "A sample needs at least one evidence.");
        PolicyValidation.Require(MinDimensions >= 2, "A weakest dimension needs at least two dimensions to compare.");
        PolicyValidation.Require(ImbalanceThreshold > 0m && PolicyValidation.IsScore(ImbalanceThreshold), "The imbalance threshold must be a positive score.");
    }
}
