namespace Leyi.Domain.LearningProfile;

/// <summary>
/// Onboarding current level (C8): ZERO / BEGINNER / HSK1 / HSK2 / HSK3 / HSK4_PLUS / UNSURE.
/// The mapping to a starting curriculum/lesson is an open decision (N4) and is not modelled.
/// </summary>
public enum SelfReportedLevel
{
    Zero,
    Beginner,
    Hsk1,
    Hsk2,
    Hsk3,
    Hsk4Plus,
    Unsure,
}
