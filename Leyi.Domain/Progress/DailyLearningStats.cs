using Leyi.Domain.Common;

namespace Leyi.Domain.Progress;

/// <summary>
/// Per-user, per-local-day learning aggregate (ERD §6.23). Updated synchronously in the transaction of the use case
/// that causes the change (C10). <see cref="LocalDate"/> is the learner-local date of the event.
/// </summary>
public sealed class DailyLearningStats : Entity
{
    private DailyLearningStats()
    {
    }

    private DailyLearningStats(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; private set; }

    public DateOnly LocalDate { get; private set; }

    /// <summary>Learning time excluding idle/background, including sessions that were not completed.</summary>
    public int MeaningfulLearningSeconds { get; private set; }

    public short CompletedSessions { get; private set; }

    public int CompletedActivities { get; private set; }

    public int NewKnowledgeCount { get; private set; }

    public int StrengthenedKnowledgeCount { get; private set; }

    public int MasteredKnowledgeCount { get; private set; }

    public int CorrectCount { get; private set; }

    public int IncorrectCount { get; private set; }

    public static DailyLearningStats Create(Guid userId, DateOnly localDate) => new(NewId())
    {
        UserId = Guard.NotEmpty(userId),
        LocalDate = localDate,
    };

    public void AddLearningSeconds(int seconds) =>
        MeaningfulLearningSeconds = checked(MeaningfulLearningSeconds + Guard.NotNegative(seconds));

    public void RecordCompletedActivity() => CompletedActivities = checked(CompletedActivities + 1);

    public void RecordAssessmentResult(bool correct)
    {
        if (correct)
        {
            CorrectCount = checked(CorrectCount + 1);
        }
        else
        {
            IncorrectCount = checked(IncorrectCount + 1);
        }
    }

    /// <summary>A UserKnowledgeMastery was created for the first time (exposure or first assessment).</summary>
    public void RecordNewKnowledge() => NewKnowledgeCount = checked(NewKnowledgeCount + 1);

    /// <summary>A review level was promoted.</summary>
    public void RecordStrengthenedKnowledge() => StrengthenedKnowledgeCount = checked(StrengthenedKnowledgeCount + 1);

    /// <summary>A knowledge unit entered the MASTERED state.</summary>
    public void RecordMasteredKnowledge() => MasteredKnowledgeCount = checked(MasteredKnowledgeCount + 1);

    public void RecordCompletedSession() => CompletedSessions = checked((short)(CompletedSessions + 1));
}
