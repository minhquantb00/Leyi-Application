namespace Leyi.Domain.Sessions;

/// <summary>How an open session resumes after a gap (Adaptive Session Engine §46).</summary>
public enum ResumeMode
{
    /// <summary>Gap within the continue threshold (V1: 30 minutes): keep the remaining plan.</summary>
    Continue,

    /// <summary>Gap between the continue and expiry thresholds (V1: 30 minutes – 24 hours): pending activities are re-planned.</summary>
    RegenerateRemaining,
}
