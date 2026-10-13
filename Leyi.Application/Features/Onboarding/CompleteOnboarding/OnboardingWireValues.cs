using Leyi.Domain.LearningProfile;

namespace Leyi.Application.Features.Onboarding.CompleteOnboarding;

/// <summary>Enum values exactly as the API contract spells them (API §3, ERD §6.2), in requests and responses.</summary>
public static class OnboardingWireValues
{
    private static readonly Dictionary<string, SelfReportedLevel> SelfReportedLevels = new(StringComparer.Ordinal)
    {
        ["ZERO"] = SelfReportedLevel.Zero,
        ["BEGINNER"] = SelfReportedLevel.Beginner,
        ["HSK1"] = SelfReportedLevel.Hsk1,
        ["HSK2"] = SelfReportedLevel.Hsk2,
        ["HSK3"] = SelfReportedLevel.Hsk3,
        ["HSK4_PLUS"] = SelfReportedLevel.Hsk4Plus,
        ["UNSURE"] = SelfReportedLevel.Unsure,
    };

    private static readonly Dictionary<string, PrimaryGoal> PrimaryGoals = new(StringComparer.Ordinal)
    {
        ["HSK"] = PrimaryGoal.Hsk,
        ["COMMUNICATION"] = PrimaryGoal.Communication,
        ["WORK"] = PrimaryGoal.Work,
        ["TRAVEL"] = PrimaryGoal.Travel,
        ["STUDY"] = PrimaryGoal.Study,
        ["INTERPRETATION"] = PrimaryGoal.Interpretation,
        ["LIVING_IN_CHINA"] = PrimaryGoal.LivingInChina,
        ["OTHER"] = PrimaryGoal.Other,
    };

    public static bool TryParseSelfReportedLevel(string? value, out SelfReportedLevel level) =>
        SelfReportedLevels.TryGetValue(value ?? string.Empty, out level);

    public static bool TryParsePrimaryGoal(string? value, out PrimaryGoal goal) =>
        PrimaryGoals.TryGetValue(value ?? string.Empty, out goal);

    public static string ToWire(SelfReportedLevel level) =>
        SelfReportedLevels.Single(pair => pair.Value == level).Key;

    public static string ToWire(PrimaryGoal goal) =>
        PrimaryGoals.Single(pair => pair.Value == goal).Key;
}
