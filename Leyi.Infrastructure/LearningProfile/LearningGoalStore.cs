using Leyi.Application.Features.Onboarding.UpdateLearningGoal;
using Leyi.Domain.LearningProfile;
using Leyi.Infrastructure.Identity;
using Leyi.Infrastructure.Persistence;
using Leyi.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leyi.Infrastructure.LearningProfile;

/// <summary>
/// UpdateLearningGoal persistence (OB-G9) in one SQL transaction under <see cref="UserSessionLock"/> (canonical order
/// <c>Users</c> → <c>UserLearningGoals</c> → <c>UserLearningPlans</c>), so concurrent updates of one user run one after
/// the other and the last committed one wins. Order of checks: account ACTIVE; neither plan nor goal → not onboarded;
/// the active plan and goal must exist together with <c>Plan.GoalId</c> = goal id and equal target dates (otherwise an
/// inconsistent state: logged, never repaired — checked before the no-op). Same desired values → no-op. Otherwise the
/// old goal is deactivated by a conditional update <em>before</em> the new version is inserted
/// (<c>UX_UserLearningGoals_Active</c>), then the plan is rebased onto it.
/// </summary>
/// <remarks>
/// Connection resiliency follows the Login/Onboarding pattern: the new goal id is fixed once per logical request; each
/// attempt starts from database state and saves with <c>acceptAllChangesOnSuccess: false</c>; after a failed commit
/// <c>verifySucceeded</c> looks for that goal (<c>Id</c> and <c>UserId</c>) in SQL Server. Its existence proves this
/// request committed (deactivation, insert and rebase are one transaction) even if a later update has since replaced
/// it, so it is not required to still be active. A re-run after a real rollback re-reads the current active goal and
/// may become a no-op.
/// </remarks>
internal sealed partial class LearningGoalStore : ILearningGoalStore
{
    private readonly LeyiDbContext dbContext;
    private readonly ILogger<LearningGoalStore> logger;

    public LearningGoalStore(LeyiDbContext dbContext, ILogger<LearningGoalStore> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<LearningGoalUpdateOutcome> UpdateAsync(LearningGoalChange change, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);

        LearningGoalUpdateOutcome? outcome = null;
        var versionCreated = false;

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteInTransactionAsync(
            async ct => (outcome, versionCreated) = await UpdateOnceAsync(change, ct),
            // Runs only after a failed commit; a LINQ query always reads committed state from SQL Server.
            ct => dbContext.UserLearningGoals.AsNoTracking().AnyAsync(goal => goal.Id == change.NewGoalId && goal.UserId == change.UserId, ct),
            cancellationToken);

        dbContext.ChangeTracker.AcceptAllChanges();

        if (versionCreated)
        {
            LogGoalChanged(logger, change.UserId, change.NewGoalId);
        }

        return outcome ?? throw new InvalidOperationException("The learning goal update completed without an outcome.");
    }

    private async Task<(LearningGoalUpdateOutcome Outcome, bool VersionCreated)> UpdateOnceAsync(LearningGoalChange change, CancellationToken cancellationToken)
    {
        var userId = change.UserId;
        ResetTrackedLearningProfile();

        if (!await UserSessionLock.AcquireAsync(dbContext, userId, cancellationToken))
        {
            return (LearningGoalUpdateOutcome.Rejected(LearningGoalUpdateStatus.AccountDisabled), false);
        }

        var status = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Status)
            .SingleAsync(cancellationToken);
        if (status != UserStatus.Active)
        {
            return (LearningGoalUpdateOutcome.Rejected(LearningGoalUpdateStatus.AccountDisabled), false);
        }

        var activeGoal = await dbContext.UserLearningGoals.AsNoTracking()
            .SingleOrDefaultAsync(goal => goal.UserId == userId && goal.IsActive, cancellationToken);
        var activePlan = await dbContext.UserLearningPlans
            .SingleOrDefaultAsync(plan => plan.UserId == userId && plan.Status == PlanStatus.Active, cancellationToken);

        if (activeGoal is null && activePlan is null)
        {
            return (LearningGoalUpdateOutcome.Rejected(LearningGoalUpdateStatus.LearningPlanNotFound), false);
        }

        EnsureConsistent(userId, activeGoal, activePlan);

        if (activeGoal!.HasGoal(change.PrimaryGoal, change.TargetHskLevel, change.TargetDate, change.DailyStudyMinutes))
        {
            return (LearningGoalUpdateOutcome.Applied(UpdateLearningGoalResult.From(activeGoal)), false);
        }

        // Deactivate first: the filtered unique index allows one active goal, and EF does not order an update before an
        // insert of the same table on its own.
        var deactivated = await dbContext.UserLearningGoals
            .Where(goal => goal.Id == activeGoal.Id && goal.UserId == userId && goal.IsActive)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(goal => goal.IsActive, false)
                    .SetProperty(goal => EF.Property<DateTime>(goal, ConfigurationExtensions.UpdatedAt), change.NowUtc),
                cancellationToken);
        if (deactivated != 1)
        {
            throw InconsistentState(userId, "the active learning goal could not be deactivated");
        }

        var newGoal = UserLearningGoal.Create(
            change.NewGoalId,
            userId,
            change.PrimaryGoal,
            activeGoal.SelfReportedLevel,
            change.DailyStudyMinutes,
            change.TargetHskLevel,
            change.TargetDate);
        dbContext.UserLearningGoals.Add(newGoal);
        activePlan!.RebaseToGoal(newGoal.Id, newGoal.TargetDate);

        // Changes stay pending until the transaction is known to be committed (see remarks).
        await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken);
        return (LearningGoalUpdateOutcome.Applied(UpdateLearningGoalResult.From(newGoal)), true);
    }

    /// <summary>An active plan and goal must exist together, linked, with the same target date (OB-G9).</summary>
    private void EnsureConsistent(Guid userId, UserLearningGoal? activeGoal, UserLearningPlan? activePlan)
    {
        if (activePlan is null)
        {
            throw InconsistentState(userId, "an active learning goal exists without an active learning plan");
        }

        if (activeGoal is null)
        {
            throw InconsistentState(userId, "an active learning plan exists without an active learning goal");
        }

        if (activePlan.GoalId != activeGoal.Id)
        {
            throw InconsistentState(userId, "the active learning plan is not linked to the active learning goal");
        }

        if (activePlan.TargetDate != activeGoal.TargetDate)
        {
            throw InconsistentState(userId, "the active learning plan target date differs from the active learning goal");
        }
    }

    /// <summary>Each attempt starts from database state (a rolled back attempt may have left a rebased plan or an added goal).</summary>
    private void ResetTrackedLearningProfile()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries()
                     .Where(entry => entry.Entity is UserLearningGoal or UserLearningPlan)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private InvalidOperationException InconsistentState(Guid userId, string reason)
    {
        LogInconsistentState(logger, userId, reason);
        return new InvalidOperationException($"Inconsistent learning profile: {reason}.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Learning goal changed for {UserId} (new goal {LearningGoalId})")]
    private static partial void LogGoalChanged(ILogger logger, Guid userId, Guid learningGoalId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Inconsistent learning profile for {UserId}: {Reason}")]
    private static partial void LogInconsistentState(ILogger logger, Guid userId, string reason);
}
