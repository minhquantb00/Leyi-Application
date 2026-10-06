namespace Leyi.Domain.Learning;

/// <summary>
/// Hint used for an attempt (ERD §6.21): NONE / LEVEL_1 / LEVEL_2.
/// "Show answer" is the separate AnswerShown flag, not a hint level.
/// </summary>
public enum HintLevel
{
    None,
    Level1,
    Level2,
}
