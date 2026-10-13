namespace Leyi.Domain.LearningProfile;

/// <summary>
/// Where a learner starts after onboarding (N4, OB-G3). The self-reported level is a conservative curriculum starting
/// point, not a claim that the level was completed: HSK2 starts at the beginning of HSK2. Levels before the starting
/// point are not marked learned, and no mastery is seeded (OB-G10). The single place that maps self-reported levels.
/// </summary>
public static class OnboardingStartPolicy
{
    public const string FoundationLevelCode = "FOUNDATION";
    public const string Hsk1LevelCode = "HSK1";
    public const string Hsk2LevelCode = "HSK2";
    public const string Hsk3LevelCode = "HSK3";
    public const string Hsk4LevelCode = "HSK4";

    public static OnboardingStart For(SelfReportedLevel selfReportedLevel) => selfReportedLevel switch
    {
        SelfReportedLevel.Zero => new(FoundationLevelCode, PlanType.Foundation, PlacementStatus.NotApplicable),
        SelfReportedLevel.Beginner => Standard(Hsk1LevelCode),
        SelfReportedLevel.Hsk1 => Standard(Hsk1LevelCode),
        SelfReportedLevel.Hsk2 => Standard(Hsk2LevelCode),
        SelfReportedLevel.Hsk3 => Standard(Hsk3LevelCode),
        SelfReportedLevel.Hsk4Plus => Standard(Hsk4LevelCode),
        SelfReportedLevel.Unsure => Standard(Hsk1LevelCode),
        _ => throw new ArgumentOutOfRangeException(nameof(selfReportedLevel), selfReportedLevel, "Undefined self-reported level."),
    };

    // Every learner who did not choose ZERO: placement is recommended (V1.1) but never blocks learning.
    private static OnboardingStart Standard(string levelCode) => new(levelCode, PlanType.Standard, PlacementStatus.Recommended);
}

/// <summary>Starting point chosen by <see cref="OnboardingStartPolicy"/>.</summary>
/// <param name="RequiredLevelCode"><c>CurriculumLevel.Code</c> that must exist in the active curriculum.</param>
/// <param name="PlanType">Plan type of the new learning plan.</param>
/// <param name="PlacementStatus">Placement marker of the new learning plan.</param>
public sealed record OnboardingStart(string RequiredLevelCode, PlanType PlanType, PlacementStatus PlacementStatus);
