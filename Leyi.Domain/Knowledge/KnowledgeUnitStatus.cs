namespace Leyi.Domain.Knowledge;

/// <summary>Knowledge unit status (ERD §6.8): ACTIVE / ARCHIVED. Learned content is archived, never hard-deleted.</summary>
public enum KnowledgeUnitStatus
{
    Active,
    Archived,
}
