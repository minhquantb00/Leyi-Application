using Leyi.Domain.Knowledge;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Policies;

/// <summary>
/// Mastery configuration (Domain Model §14, Learning Model §3, §15–20). Data only: the MasteryCalculator and
/// KnowledgeStateEvaluator consume it. Values in <see cref="V1"/> are V1 defaults (open decision N1: calibration).
/// </summary>
public sealed record MasteryPolicy
{
    public static MasteryPolicy V1 { get; } = new()
    {
        Version = "mastery-v1.0",
        ApplicableDimensions = new Dictionary<KnowledgeType, IReadOnlyList<MasteryDimension>>
        {
            [KnowledgeType.Vocabulary] = [MasteryDimension.Recognition, MasteryDimension.Recall, MasteryDimension.Listening, MasteryDimension.Usage],
            [KnowledgeType.Grammar] = [MasteryDimension.Usage],
        },
        AlphaTiers = [new AlphaTier(0, 0.35m), new AlphaTier(3, 0.25m), new AlphaTier(10, 0.15m)],
        AlphaPenalty = 0.35m,
        DimensionTargets = new Dictionary<MasteryDimension, decimal>
        {
            [MasteryDimension.Recognition] = 85m,
            [MasteryDimension.Recall] = 80m,
            [MasteryDimension.Listening] = 75m,
            [MasteryDimension.Usage] = 70m,
        },
        RepeatedErrorThreshold = 2,
        ConsecutiveFailureThreshold = 2,
        WeakScoreThreshold = 40m,
        WeakScoreMinEvidenceCount = 3,
        MasteredMinReviewLevel = 6,
        SeedValues = new Dictionary<MasteryDimension, decimal>
        {
            [MasteryDimension.Recognition] = 75m,
            [MasteryDimension.Recall] = 65m,
            [MasteryDimension.Listening] = 60m,
        },
        OverallWeights = new Dictionary<KnowledgeType, IReadOnlyDictionary<MasteryDimension, decimal>>
        {
            [KnowledgeType.Vocabulary] = new Dictionary<MasteryDimension, decimal>
            {
                [MasteryDimension.Recognition] = 0.25m,
                [MasteryDimension.Recall] = 0.30m,
                [MasteryDimension.Listening] = 0.25m,
                [MasteryDimension.Usage] = 0.20m,
            },
            [KnowledgeType.Grammar] = new Dictionary<MasteryDimension, decimal>
            {
                [MasteryDimension.Usage] = 1.00m,
            },
        },
    };

    public required string Version { get; init; }

    /// <summary>Dimensions tracked per knowledge type (V1: grammar = USAGE only).</summary>
    public required IReadOnlyDictionary<KnowledgeType, IReadOnlyList<MasteryDimension>> ApplicableDimensions { get; init; }

    /// <summary>BaseAlpha by the dimension's evidence count before the update, ordered by <see cref="AlphaTier.MinPriorEvidenceCount"/>.</summary>
    public required IReadOnlyList<AlphaTier> AlphaTiers { get; init; }

    /// <summary>Minimum alpha applied to Q0 evidence.</summary>
    public required decimal AlphaPenalty { get; init; }

    /// <summary>Per-dimension targets: thresholds for STRONG/MASTERED and the Dimension Gap of the session engine.</summary>
    public required IReadOnlyDictionary<MasteryDimension, decimal> DimensionTargets { get; init; }

    /// <summary>Recent errors (14-day window) on one dimension that make knowledge WEAK.</summary>
    public required int RepeatedErrorThreshold { get; init; }

    public required int ConsecutiveFailureThreshold { get; init; }

    public required decimal WeakScoreThreshold { get; init; }

    public required int WeakScoreMinEvidenceCount { get; init; }

    public required byte MasteredMinReviewLevel { get; init; }

    /// <summary>Seed scores for Placement / Quick Verification on dimensions without evidence (never 100).</summary>
    public required IReadOnlyDictionary<MasteryDimension, decimal> SeedValues { get; init; }

    public required IReadOnlyDictionary<KnowledgeType, IReadOnlyDictionary<MasteryDimension, decimal>> OverallWeights { get; init; }

    public IReadOnlyList<MasteryDimension> DimensionsFor(KnowledgeType knowledgeType) =>
        ApplicableDimensions.TryGetValue(knowledgeType, out var dimensions)
            ? dimensions
            : throw new InvalidOperationException($"Mastery policy {Version} defines no dimensions for {knowledgeType}.");

    public void Validate()
    {
        PolicyValidation.RequireVersion(Version);
        PolicyValidation.RequireEveryValue(ApplicableDimensions.Keys, nameof(ApplicableDimensions));

        foreach (var (type, dimensions) in ApplicableDimensions)
        {
            PolicyValidation.Require(dimensions.Count > 0 && dimensions.Distinct().Count() == dimensions.Count, $"Dimensions of {type} must be a non-empty set.");
            PolicyValidation.Require(OverallWeights.TryGetValue(type, out var weights), $"Overall weights are missing for {type}.");
            PolicyValidation.Require(weights!.Keys.ToHashSet().SetEquals(dimensions), $"Overall weights of {type} must cover exactly its dimensions.");
            PolicyValidation.RequireSumIsOne(weights.Values, $"Overall weights of {type}");
        }

        PolicyValidation.Require(AlphaTiers.Count > 0 && AlphaTiers[0].MinPriorEvidenceCount == 0, "Alpha tiers must start at evidence count 0.");
        PolicyValidation.Require(AlphaTiers.Zip(AlphaTiers.Skip(1)).All(pair => pair.First.MinPriorEvidenceCount < pair.Second.MinPriorEvidenceCount), "Alpha tiers must be ordered.");
        PolicyValidation.Require(AlphaTiers.All(tier => tier.Alpha is > 0m and <= 1m), "Alpha must be in (0, 1].");
        PolicyValidation.Require(AlphaPenalty is > 0m and <= 1m, "Alpha penalty must be in (0, 1].");
        PolicyValidation.RequireEveryValue(DimensionTargets.Keys, nameof(DimensionTargets));
        PolicyValidation.Require(DimensionTargets.Values.All(PolicyValidation.IsScore), "Dimension targets must be scores.");
        PolicyValidation.Require(SeedValues.Values.All(value => value is >= 0m and < 100m), "Seed values must be below 100.");
        PolicyValidation.Require(PolicyValidation.IsScore(WeakScoreThreshold), "Weak score threshold must be a score.");
        PolicyValidation.Require(RepeatedErrorThreshold > 0 && ConsecutiveFailureThreshold > 0 && WeakScoreMinEvidenceCount > 0, "Weak thresholds must be positive.");
        PolicyValidation.Require(MasteredMinReviewLevel is >= 1 and <= Mastery.ReviewLevel.MaxValue, "Mastered review level must be 1–7.");
    }
}

/// <summary>BaseAlpha applied when the dimension already has at least <see cref="MinPriorEvidenceCount"/> evidence.</summary>
public sealed record AlphaTier(int MinPriorEvidenceCount, decimal Alpha);
