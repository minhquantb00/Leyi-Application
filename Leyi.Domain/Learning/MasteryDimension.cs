namespace Leyi.Domain.Learning;

/// <summary>
/// Independent mastery dimensions (Learning Model §5): RECOGNITION / RECALL / LISTENING / USAGE.
/// Which dimensions apply depends on the knowledge type (V1: vocabulary = all four, grammar = USAGE only);
/// the mapping is part of <see cref="Policies.MasteryPolicy"/>.
/// </summary>
public enum MasteryDimension
{
    Recognition,
    Recall,
    Listening,
    Usage,
}
