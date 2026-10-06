namespace Leyi.Domain.Learning;

/// <summary>
/// Result quality of a closed assessment activity (Learning Model §11, §12.1). Stored as 0–4.
/// Numeric weights (Q4 = 1.00 … Q0 = 0.00) are configuration in <see cref="Policies.EvidencePolicy"/>.
/// </summary>
public enum EvidenceQuality : byte
{
    /// <summary>Incorrect, or the answer was shown.</summary>
    Q0 = 0,

    /// <summary>Correct after an initial mistake, or correct with a level-2 hint.</summary>
    Q1 = 1,

    /// <summary>Correct with a level-1 hint.</summary>
    Q2 = 2,

    /// <summary>Correct but slow or with excessive audio replays.</summary>
    Q3 = 3,

    /// <summary>Correct, unaided.</summary>
    Q4 = 4,
}
