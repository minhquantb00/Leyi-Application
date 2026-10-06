namespace Leyi.Domain.Policies;

/// <summary>
/// Confidence configuration (Learning Model §9.1): LOW &lt; 3 evidence; MEDIUM ≥ 3; HIGH ≥ 6 evidence on ≥ 3
/// learner-local days. Data only. Values in <see cref="V1"/> are V1 defaults (N1).
/// </summary>
public sealed record ConfidencePolicy
{
    public static ConfidencePolicy V1 { get; } = new()
    {
        Version = "confidence-v1.0",
        MediumMinEvidenceCount = 3,
        HighMinEvidenceCount = 6,
        HighMinEvidenceDays = 3,
    };

    public required string Version { get; init; }

    public required int MediumMinEvidenceCount { get; init; }

    public required int HighMinEvidenceCount { get; init; }

    public required int HighMinEvidenceDays { get; init; }

    public void Validate()
    {
        PolicyValidation.RequireVersion(Version);
        PolicyValidation.Require(MediumMinEvidenceCount >= 1, "MEDIUM needs at least one evidence.");
        PolicyValidation.Require(HighMinEvidenceCount > MediumMinEvidenceCount, "HIGH must need more evidence than MEDIUM.");
        PolicyValidation.Require(HighMinEvidenceDays >= 1 && HighMinEvidenceDays <= HighMinEvidenceCount, "HIGH evidence days must be within 1..HIGH evidence count.");
    }
}
