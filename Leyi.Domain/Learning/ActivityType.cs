namespace Leyi.Domain.Learning;

/// <summary>
/// Session activity types V1 (ERD §6.19, Learning Model §28): six assessment types plus the
/// non-assessment NEW_VOCABULARY_EXPOSURE. CORRECTION is a <see cref="Sessions.SelectionReason"/>, not an activity type.
/// </summary>
public enum ActivityType
{
    HanziToMeaning,
    MeaningToHanzi,
    MeaningToTypeHanzi,
    ListeningToHanzi,
    ListeningToMeaning,
    ContextFill,
    NewVocabularyExposure,
}

public static class ActivityTypeExtensions
{
    /// <summary>
    /// Assessment activities accept attempts and produce at most one evidence each.
    /// Non-assessment activities (NEW_VOCABULARY_EXPOSURE) are completed without attempts or evidence.
    /// </summary>
    public static bool IsAssessment(this ActivityType activityType) => activityType switch
    {
        ActivityType.NewVocabularyExposure => false,
        ActivityType.HanziToMeaning
            or ActivityType.MeaningToHanzi
            or ActivityType.MeaningToTypeHanzi
            or ActivityType.ListeningToHanzi
            or ActivityType.ListeningToMeaning
            or ActivityType.ContextFill => true,
        _ => throw new ArgumentOutOfRangeException(nameof(activityType), activityType, "Undefined activity type."),
    };
}
