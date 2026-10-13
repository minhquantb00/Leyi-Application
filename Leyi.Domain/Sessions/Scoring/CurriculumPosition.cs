namespace Leyi.Domain.Sessions.Scoring;

/// <summary>Where a candidate's knowledge unit sits relative to the learner's current lesson (ASE §16, CurriculumRelevance).</summary>
public enum CurriculumPosition
{
    CurrentLesson,
    NextLesson,
    PreviousLesson,
    OtherLesson,
}
