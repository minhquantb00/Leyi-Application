using Leyi.Domain.Common;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Progress;

/// <summary>
/// Daily skill-profile snapshot per dimension (ERD §6.23.1, C9). Upserted asynchronously after a session completes;
/// used for skill trends. Score is the average of measured dimensions over non-NEW knowledge (Learning Model §45).
/// </summary>
public sealed class UserSkillSnapshot : Entity
{
    private UserSkillSnapshot()
    {
    }

    private UserSkillSnapshot(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; private set; }

    public DateOnly LocalDate { get; private set; }

    public MasteryDimension Dimension { get; private set; }

    public decimal Score { get; private set; }

    public int KnowledgeCount { get; private set; }

    public static UserSkillSnapshot Create(Guid userId, DateOnly localDate, MasteryDimension dimension, decimal score, int knowledgeCount)
    {
        var snapshot = new UserSkillSnapshot(NewId())
        {
            UserId = Guard.NotEmpty(userId),
            LocalDate = localDate,
            Dimension = Guard.Defined(dimension),
        };

        snapshot.Update(score, knowledgeCount);
        return snapshot;
    }

    public void Update(decimal score, int knowledgeCount)
    {
        Score = decimal.Round(Guard.InRange(score, 0m, 100m), 2, MidpointRounding.AwayFromZero);
        KnowledgeCount = Guard.NotNegative(knowledgeCount);
    }
}
