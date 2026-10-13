using Leyi.Application.Features.Onboarding.CompleteOnboarding;
using Leyi.Domain.LearningProfile;
using Leyi.Infrastructure.Identity;
using Leyi.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leyi.Infrastructure.LearningProfile;

/// <summary>
/// Persists onboarding in one SQL transaction. The user's row is locked first (<see cref="UserSessionLock"/>, canonical
/// order <c>Users</c> → user-owned rows), so concurrent onboarding requests of one user run one after the other and the
/// later ones see the committed plan (409). Inside the lock: status must be ACTIVE (OB-G7); no ACTIVE plan may exist
/// (completion = active plan); an ACTIVE goal without an ACTIVE plan is an inconsistent state (logged, 500 — never
/// reported as "already completed"). The same rules are read unlocked first by <see cref="CheckEligibilityAsync"/> so
/// that existing state decides the error before content is resolved; the locked re-check here decides the final
/// outcome. The filtered unique indexes on active goal/plan stay the final guard.
/// </summary>
/// <remarks>
/// Connection resiliency follows the Login pattern: goal, plan and reminder are created once per logical request with
/// fixed ids; each attempt saves with <c>acceptAllChangesOnSuccess: false</c>; after a failed commit
/// <c>verifySucceeded</c> looks for this plan (<c>Id</c> and <c>UserId</c>) in SQL Server, so an onboarding that did
/// commit is reported as success instead of being re-run into a false 409. Changes are accepted only at the end.
/// </remarks>
internal sealed partial class OnboardingStore : IOnboardingStore
{
    private static readonly string[] ActiveRowIndexes = ["UX_UserLearningPlans_User_Active", "UX_UserLearningGoals_Active"];

    private readonly LeyiDbContext dbContext;
    private readonly ILogger<OnboardingStore> logger;

    public OnboardingStore(LeyiDbContext dbContext, ILogger<OnboardingStore> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    /// <summary>Unlocked, read-only pre-check (no transaction); <see cref="SaveAsync"/> re-checks under the lock.</summary>
    public async Task<OnboardingEligibility> CheckEligibilityAsync(Guid userId, CancellationToken cancellationToken)
    {
        var eligibility = await EvaluateAsync(userId, cancellationToken);
        if (eligibility == OnboardingEligibility.InconsistentState)
        {
            LogInconsistentState(logger, userId);
        }

        return eligibility;
    }

    public async Task<OnboardingSaveOutcome> SaveAsync(OnboardingRegistration registration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var userId = registration.UserId;
        var planId = registration.Plan.Id;
        var outcome = OnboardingSaveOutcome.Saved;

        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteInTransactionAsync(
                async ct => outcome = await SaveOnceAsync(registration, ct),
                // Runs only after a failed commit; a LINQ query always reads committed state from SQL Server.
                ct => dbContext.UserLearningPlans.AsNoTracking().AnyAsync(plan => plan.Id == planId && plan.UserId == userId, ct),
                cancellationToken);
        }
        catch (DbUpdateException exception) when (IsActiveRowConflict(exception))
        {
            Detach(registration);
            return await ResolveActiveRowConflictAsync(userId, cancellationToken);
        }

        if (outcome != OnboardingSaveOutcome.Saved)
        {
            Detach(registration);
            return outcome;
        }

        dbContext.ChangeTracker.AcceptAllChanges();
        LogOnboardingCompleted(logger, userId, planId);
        return outcome;
    }

    private async Task<OnboardingSaveOutcome> SaveOnceAsync(OnboardingRegistration registration, CancellationToken cancellationToken)
    {
        var userId = registration.UserId;

        if (!await UserSessionLock.AcquireAsync(dbContext, userId, cancellationToken))
        {
            return OnboardingSaveOutcome.AccountDisabled;
        }

        // Authoritative re-check under the lock: the state may have changed since CheckEligibilityAsync.
        switch (await EvaluateAsync(userId, cancellationToken))
        {
            case OnboardingEligibility.Eligible:
                break;
            case OnboardingEligibility.AccountDisabled:
                return OnboardingSaveOutcome.AccountDisabled;
            case OnboardingEligibility.AlreadyCompleted:
                return OnboardingSaveOutcome.AlreadyCompleted;
            default:
                throw InconsistentState(userId);
        }

        if (registration.PreferredTimezone is { } timezone)
        {
            await dbContext.Users
                .Where(user => user.Id == userId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(user => user.PreferredTimezone, timezone)
                        .SetProperty(user => user.UpdatedAt, registration.NowUtc),
                    cancellationToken);
        }

        // Added once; a rolled back attempt leaves them Added (changes are not accepted until the end).
        AddIfDetached(registration.Goal);
        AddIfDetached(registration.Plan);
        if (registration.Reminder is { } reminder)
        {
            AddIfDetached(reminder);
        }

        await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken);
        return OnboardingSaveOutcome.Saved;
    }

    /// <summary>
    /// The unique index on active plan/goal fired: another onboarding committed first unless no active plan exists,
    /// which would be an inconsistent state rather than a completed onboarding.
    /// </summary>
    private async Task<OnboardingSaveOutcome> ResolveActiveRowConflictAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (await HasActivePlanAsync(userId, cancellationToken))
        {
            return OnboardingSaveOutcome.AlreadyCompleted;
        }

        throw InconsistentState(userId);
    }

    /// <summary>
    /// The eligibility rules in precedence order: user missing or not ACTIVE; active plan (completed); active goal
    /// without active plan (inconsistent). Used unlocked by the pre-check and under the user's lock by the save.
    /// </summary>
    private async Task<OnboardingEligibility> EvaluateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var status = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => (UserStatus?)user.Status)
            .SingleOrDefaultAsync(cancellationToken);
        if (status != UserStatus.Active)
        {
            return OnboardingEligibility.AccountDisabled;
        }

        if (await HasActivePlanAsync(userId, cancellationToken))
        {
            return OnboardingEligibility.AlreadyCompleted;
        }

        return await dbContext.UserLearningGoals.AsNoTracking().AnyAsync(goal => goal.UserId == userId && goal.IsActive, cancellationToken)
            ? OnboardingEligibility.InconsistentState
            : OnboardingEligibility.Eligible;
    }

    private Task<bool> HasActivePlanAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserLearningPlans.AsNoTracking().AnyAsync(plan => plan.UserId == userId && plan.Status == PlanStatus.Active, cancellationToken);

    private void AddIfDetached(object entity)
    {
        if (dbContext.Entry(entity).State == EntityState.Detached)
        {
            dbContext.Add(entity);
        }
    }

    private void Detach(OnboardingRegistration registration)
    {
        dbContext.Entry(registration.Goal).State = EntityState.Detached;
        dbContext.Entry(registration.Plan).State = EntityState.Detached;
        if (registration.Reminder is { } reminder)
        {
            dbContext.Entry(reminder).State = EntityState.Detached;
        }
    }

    private InvalidOperationException InconsistentState(Guid userId)
    {
        LogInconsistentState(logger, userId);
        return new InvalidOperationException("Onboarding found an active learning goal without an active learning plan.");
    }

    private static bool IsActiveRowConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException
        && ActiveRowIndexes.Any(index => sqlException.Message.Contains(index, StringComparison.Ordinal));

    [LoggerMessage(Level = LogLevel.Information, Message = "Onboarding completed for {UserId} (learning plan {LearningPlanId})")]
    private static partial void LogOnboardingCompleted(ILogger logger, Guid userId, Guid learningPlanId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Inconsistent learning profile for {UserId}: an active learning goal exists without an active learning plan")]
    private static partial void LogInconsistentState(ILogger logger, Guid userId);
}
