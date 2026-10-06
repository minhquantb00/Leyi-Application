using Leyi.Domain.Learning;

namespace Leyi.Domain.Sessions;

/// <summary>An evaluated answer to an assessment activity (evaluation itself is done by the ActivityEvaluator).</summary>
public sealed record AttemptSubmission
{
    public const int MaxAudioReplayCount = 20;

    public string? UserAnswer { get; init; }

    public required bool Correct { get; init; }

    public HintLevel HintLevel { get; init; } = HintLevel.None;

    public bool AnswerShown { get; init; }

    public int? ResponseTimeMs { get; init; }

    public int? AudioReplayCount { get; init; }

    public DateTime? StartedAtUtc { get; init; }
}
