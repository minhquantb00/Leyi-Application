using Leyi.Domain.Common;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Policies;

/// <summary>
/// Mean mastery score of one dimension over eligible samples. Neutral value type used by
/// <see cref="WeakestDimensionPolicy"/> (Today) and <see cref="SessionPolicy.WeakSkills"/> (ASE §54).
/// </summary>
public sealed record DimensionAverage(MasteryDimension Dimension, decimal AverageScore)
{
    public MasteryDimension Dimension { get; } = Guard.Defined(Dimension);

    public decimal AverageScore { get; } = Guard.InRange(AverageScore, 0m, 100m);
}
