using Leyi.Domain.Curricula;
using Leyi.Domain.Evidence;
using Leyi.Domain.Knowledge;
using Leyi.Domain.LearningProfile;
using Leyi.Domain.Mastery;
using Leyi.Domain.Progress;
using Leyi.Domain.Sessions;
using Leyi.Domain.Subscriptions;
using Leyi.Infrastructure.Idempotency;
using Leyi.Infrastructure.Identity;
using Leyi.Infrastructure.Persistence.Conventions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Leyi.Infrastructure.Persistence;

/// <summary>
/// Single EF Core context for the Leyi SQL Server database (source of truth).
/// Contains ASP.NET Core Identity, infrastructure tables (refresh tokens, idempotency), the MassTransit
/// transactional outbox/inbox tables and the learning domain (curriculum, knowledge, mastery, review, evidence,
/// sessions, progress, subscription). DbSets are exposed for aggregate roots and query entry points only;
/// aggregate children are reached through their roots.
/// </summary>
public sealed class LeyiDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public LeyiDbContext(DbContextOptions<LeyiDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    // Curriculum & knowledge (reference content)
    public DbSet<Curriculum> Curriculums => Set<Curriculum>();

    public DbSet<CurriculumLevel> CurriculumLevels => Set<CurriculumLevel>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<KnowledgeUnit> KnowledgeUnits => Set<KnowledgeUnit>();

    // Learning profile & curriculum progress
    public DbSet<UserLearningGoal> UserLearningGoals => Set<UserLearningGoal>();

    public DbSet<UserLearningPlan> UserLearningPlans => Set<UserLearningPlan>();

    public DbSet<UserReminder> UserReminders => Set<UserReminder>();

    public DbSet<UserLessonProgress> UserLessonProgress => Set<UserLessonProgress>();

    // Mastery, review and learning history
    public DbSet<UserKnowledgeMastery> UserKnowledgeMasteries => Set<UserKnowledgeMastery>();

    public DbSet<ReviewSchedule> ReviewSchedules => Set<ReviewSchedule>();

    public DbSet<LearningEvidence> LearningEvidences => Set<LearningEvidence>();

    // Sessions
    public DbSet<LearningSession> LearningSessions => Set<LearningSession>();

    // Progress projections
    public DbSet<DailyLearningStats> DailyLearningStats => Set<DailyLearningStats>();

    public DbSet<UserSkillSnapshot> UserSkillSnapshots => Set<UserSkillSnapshot>();

    // Subscription
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(LeyiDbContext).Assembly);

        // MassTransit EF Core transactional outbox / consumer inbox (infrastructure persistence, not domain).
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
