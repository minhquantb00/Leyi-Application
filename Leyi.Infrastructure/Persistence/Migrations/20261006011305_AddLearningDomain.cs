using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leyi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Curriculums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Standard = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curriculums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyLearningStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MeaningfulLearningSeconds = table.Column<int>(type: "int", nullable: false),
                    CompletedSessions = table.Column<short>(type: "smallint", nullable: false),
                    CompletedActivities = table.Column<int>(type: "int", nullable: false),
                    NewKnowledgeCount = table.Column<int>(type: "int", nullable: false),
                    StrengthenedKnowledgeCount = table.Column<int>(type: "int", nullable: false),
                    MasteredKnowledgeCount = table.Column<int>(type: "int", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    IncorrectCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyLearningStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyLearningStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    Code = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    ContentVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeUnits", x => x.Id);
                    table.CheckConstraint("CK_KnowledgeUnits_ContentVersion", "[ContentVersion] >= 1");
                });

            migrationBuilder.CreateTable(
                name: "LearningSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    RequestedDurationSeconds = table.Column<short>(type: "smallint", nullable: true),
                    EffectiveBudgetSeconds = table.Column<short>(type: "smallint", nullable: true),
                    ActualDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    RequestedSkill = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    GenerationVersion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    EvaluatedActivityCount = table.Column<short>(type: "smallint", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningSessions", x => x.Id);
                    table.CheckConstraint("CK_LearningSessions_Counters", "[ActualDurationSeconds] >= 0 AND [EvaluatedActivityCount] >= 0");
                    table.CheckConstraint("CK_LearningSessions_Durations", "([RequestedDurationSeconds] IS NULL OR [RequestedDurationSeconds] > 0) AND ([EffectiveBudgetSeconds] IS NULL OR [EffectiveBudgetSeconds] > 0)");
                    table.ForeignKey(
                        name: "FK_LearningSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    YearlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLearningGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrimaryGoal = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    SelfReportedLevel = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TargetHSKLevel = table.Column<byte>(type: "tinyint", nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DailyStudyMinutes = table.Column<short>(type: "smallint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLearningGoals", x => x.Id);
                    table.CheckConstraint("CK_UserLearningGoals_DailyStudyMinutes", "[DailyStudyMinutes] BETWEEN 5 AND 120");
                    table.CheckConstraint("CK_UserLearningGoals_TargetHSKLevel", "[TargetHSKLevel] IS NULL OR [TargetHSKLevel] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_UserLearningGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    ReminderTime = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    Timezone = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    DaysMask = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReminders", x => x.Id);
                    table.CheckConstraint("CK_UserReminders_DaysMask", "[DaysMask] BETWEEN 0 AND 127");
                    table.ForeignKey(
                        name: "FK_UserReminders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSkillSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Dimension = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    KnowledgeCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSkillSnapshots", x => x.Id);
                    table.CheckConstraint("CK_UserSkillSnapshots_Score", "[Score] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_UserSkillSnapshots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumLevels_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GrammarPatterns",
                columns: table => new
                {
                    KnowledgeUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Pattern = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ExplanationVi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExplanationEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HskLevel = table.Column<byte>(type: "tinyint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarPatterns", x => x.KnowledgeUnitId);
                    table.CheckConstraint("CK_GrammarPatterns_HskLevel", "[HskLevel] IS NULL OR [HskLevel] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_GrammarPatterns_KnowledgeUnits_KnowledgeUnitId",
                        column: x => x.KnowledgeUnitId,
                        principalTable: "KnowledgeUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserKnowledgeMasteries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeState = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    OverallMastery = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPracticedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalEvidenceCount = table.Column<int>(type: "int", nullable: false),
                    EvidenceDayCount = table.Column<int>(type: "int", nullable: false),
                    LastEvidenceLocalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserKnowledgeMasteries", x => x.Id);
                    table.CheckConstraint("CK_UserKnowledgeMasteries_Counts", "[TotalEvidenceCount] >= 0 AND [EvidenceDayCount] >= 0 AND [EvidenceDayCount] <= [TotalEvidenceCount]");
                    table.CheckConstraint("CK_UserKnowledgeMasteries_KnowledgeState", "[KnowledgeState] IN ('LEARNING', 'REVIEWING', 'WEAK', 'STRONG', 'MASTERED')");
                    table.CheckConstraint("CK_UserKnowledgeMasteries_OverallMastery", "[OverallMastery] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_UserKnowledgeMasteries_KnowledgeUnits_KnowledgeUnitId",
                        column: x => x.KnowledgeUnitId,
                        principalTable: "KnowledgeUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserKnowledgeMasteries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vocabularies",
                columns: table => new
                {
                    KnowledgeUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Hanzi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Pinyin = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PartOfSpeech = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MeaningVi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MeaningEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HanViet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HskLevel = table.Column<byte>(type: "tinyint", nullable: true),
                    AudioUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vocabularies", x => x.KnowledgeUnitId);
                    table.CheckConstraint("CK_Vocabularies_HskLevel", "[HskLevel] IS NULL OR [HskLevel] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_Vocabularies_KnowledgeUnits_KnowledgeUnitId",
                        column: x => x.KnowledgeUnitId,
                        principalTable: "KnowledgeUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActivityType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    TargetDimension = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    EstimatedDurationSeconds = table.Column<short>(type: "smallint", nullable: false),
                    PriorityScore = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ContentSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentVersion = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionActivities", x => x.Id);
                    table.CheckConstraint("CK_SessionActivities_ContentSnapshot", "ISJSON([ContentSnapshot]) = 1");
                    table.CheckConstraint("CK_SessionActivities_ContentVersion", "[ContentVersion] >= 1");
                    table.CheckConstraint("CK_SessionActivities_EstimatedDuration", "[EstimatedDurationSeconds] > 0");
                    table.CheckConstraint("CK_SessionActivities_PriorityScore", "[PriorityScore] IS NULL OR [PriorityScore] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_SessionActivities_KnowledgeUnits_KnowledgeUnitId",
                        column: x => x.KnowledgeUnitId,
                        principalTable: "KnowledgeUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionActivities_LearningSessions_LearningSessionId",
                        column: x => x.LearningSessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Provider = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ProviderSubscriptionId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    EstimatedMinutes = table.Column<short>(type: "smallint", nullable: true),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.CheckConstraint("CK_Lessons_EstimatedMinutes", "[EstimatedMinutes] IS NULL OR [EstimatedMinutes] > 0");
                    table.ForeignKey(
                        name: "FK_Lessons_CurriculumLevels_CurriculumLevelId",
                        column: x => x.CurriculumLevelId,
                        principalTable: "CurriculumLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserKnowledgeMasteryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewLevel = table.Column<byte>(type: "tinyint", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextReviewAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastResultQuality = table.Column<byte>(type: "tinyint", nullable: true),
                    ConsecutiveSuccess = table.Column<short>(type: "smallint", nullable: false),
                    ConsecutiveFailure = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewSchedules", x => x.Id);
                    table.CheckConstraint("CK_ReviewSchedules_LastResultQuality", "[LastResultQuality] IS NULL OR [LastResultQuality] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_ReviewSchedules_ReviewLevel", "[ReviewLevel] BETWEEN 0 AND 7");
                    table.ForeignKey(
                        name: "FK_ReviewSchedules_UserKnowledgeMasteries_UserKnowledgeMasteryId",
                        column: x => x.UserKnowledgeMasteryId,
                        principalTable: "UserKnowledgeMasteries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewSchedules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserKnowledgeDimensionMasteries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserKnowledgeMasteryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dimension = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EvidenceCount = table.Column<int>(type: "int", nullable: false),
                    LastPracticedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCorrectAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserKnowledgeDimensionMasteries", x => x.Id);
                    table.CheckConstraint("CK_UKDM_EvidenceCount", "[EvidenceCount] >= 0");
                    table.CheckConstraint("CK_UKDM_Score", "[Score] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_UserKnowledgeDimensionMasteries_UserKnowledgeMasteries_UserKnowledgeMasteryId",
                        column: x => x.UserKnowledgeMasteryId,
                        principalTable: "UserKnowledgeMasteries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyCollocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VocabularyKnowledgeUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Pinyin = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MeaningVi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyCollocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyCollocations_Vocabularies_VocabularyKnowledgeUnitId",
                        column: x => x.VocabularyKnowledgeUnitId,
                        principalTable: "Vocabularies",
                        principalColumn: "KnowledgeUnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyExamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VocabularyKnowledgeUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChineseText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Pinyin = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    MeaningVi = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    MeaningEn = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    AudioUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DifficultyLevel = table.Column<byte>(type: "tinyint", nullable: true),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyExamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyExamples_Vocabularies_VocabularyKnowledgeUnitId",
                        column: x => x.VocabularyKnowledgeUnitId,
                        principalTable: "Vocabularies",
                        principalColumn: "KnowledgeUnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    UserAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Correct = table.Column<bool>(type: "bit", nullable: false),
                    HintLevel = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    AnswerShown = table.Column<bool>(type: "bit", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
                    AudioReplayCount = table.Column<short>(type: "smallint", nullable: true),
                    Quality = table.Column<byte>(type: "tinyint", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityAttempts", x => x.Id);
                    table.CheckConstraint("CK_ActivityAttempts_AttemptNumber", "[AttemptNumber] >= 1");
                    table.CheckConstraint("CK_ActivityAttempts_AudioReplayCount", "[AudioReplayCount] IS NULL OR [AudioReplayCount] BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_ActivityAttempts_Quality", "[Quality] IS NULL OR [Quality] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_ActivityAttempts_ResponseTimeMs", "[ResponseTimeMs] IS NULL OR [ResponseTimeMs] >= 0");
                    table.ForeignKey(
                        name: "FK_ActivityAttempts_SessionActivities_SessionActivityId",
                        column: x => x.SessionActivityId,
                        principalTable: "SessionActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionActivityReasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ScoreContribution = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionActivityReasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionActivityReasons_SessionActivities_SessionActivityId",
                        column: x => x.SessionActivityId,
                        principalTable: "SessionActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonKnowledgeUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonKnowledgeUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonKnowledgeUnits_KnowledgeUnits_KnowledgeUnitId",
                        column: x => x.KnowledgeUnitId,
                        principalTable: "KnowledgeUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonKnowledgeUnits_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLearningPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurriculumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentLessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlanType = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    PlacementStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLearningPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLearningPlans_CurriculumLevels_CurrentLevelId",
                        column: x => x.CurrentLevelId,
                        principalTable: "CurriculumLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLearningPlans_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLearningPlans_Lessons_CurrentLessonId",
                        column: x => x.CurrentLessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLearningPlans_UserLearningGoals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "UserLearningGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLearningPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserLessonProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLessonProgress", x => x.Id);
                    table.CheckConstraint("CK_UserLessonProgress_ProgressPercent", "[ProgressPercent] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_UserLessonProgress_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLessonProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Dimension = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SourceType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActivityType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Quality = table.Column<byte>(type: "tinyint", nullable: false),
                    InitialCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Correct = table.Column<bool>(type: "bit", nullable: false),
                    HintLevel = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    AnswerShown = table.Column<bool>(type: "bit", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
                    EvidenceWeight = table.Column<decimal>(type: "decimal(4,3)", precision: 4, scale: 3, nullable: false),
                    SessionDiscountFactor = table.Column<decimal>(type: "decimal(4,3)", precision: 4, scale: 3, nullable: false),
                    PolicyVersion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeZoneId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningEvidences", x => x.Id);
                    table.CheckConstraint("CK_LearningEvidences_EvidenceWeight", "[EvidenceWeight] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_LearningEvidences_Quality", "[Quality] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_LearningEvidences_SessionDiscountFactor", "[SessionDiscountFactor] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_LearningEvidences_ActivityAttempts_ActivityAttemptId",
                        column: x => x.ActivityAttemptId,
                        principalTable: "ActivityAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningEvidences_KnowledgeUnits_KnowledgeUnitId",
                        column: x => x.KnowledgeUnitId,
                        principalTable: "KnowledgeUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningEvidences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ActivityAttempts_Activity_Attempt",
                table: "ActivityAttempts",
                columns: new[] { "SessionActivityId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CurriculumLevels_Curriculum_Code",
                table: "CurriculumLevels",
                columns: new[] { "CurriculumId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CurriculumLevels_Curriculum_SortOrder",
                table: "CurriculumLevels",
                columns: new[] { "CurriculumId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Curriculums_Code",
                table: "Curriculums",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DailyLearningStats_User_Date",
                table: "DailyLearningStats",
                columns: new[] { "UserId", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_KnowledgeUnits_Code",
                table: "KnowledgeUnits",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvidences_KnowledgeUnitId",
                table: "LearningEvidences",
                column: "KnowledgeUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvidences_User_KU_Created",
                table: "LearningEvidences",
                columns: new[] { "UserId", "KnowledgeUnitId", "CreatedAt" },
                descending: new[] { false, false, true })
                .Annotation("SqlServer:Include", new[] { "Dimension", "Quality" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvidences_User_RecentErrors",
                table: "LearningEvidences",
                columns: new[] { "UserId", "CreatedAt" },
                filter: "[Quality] = 0")
                .Annotation("SqlServer:Include", new[] { "KnowledgeUnitId", "Dimension" });

            migrationBuilder.CreateIndex(
                name: "UX_LearningEvidences_Attempt",
                table: "LearningEvidences",
                column: "ActivityAttemptId",
                unique: true,
                filter: "[ActivityAttemptId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessions_User_CreatedAt",
                table: "LearningSessions",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "Status", "EvaluatedActivityCount" });

            migrationBuilder.CreateIndex(
                name: "UX_LearningSessions_User_Open",
                table: "LearningSessions",
                column: "UserId",
                unique: true,
                filter: "[Status] IN ('GENERATED', 'IN_PROGRESS', 'PAUSED')");

            migrationBuilder.CreateIndex(
                name: "IX_LessonKnowledgeUnits_KnowledgeUnitId",
                table: "LessonKnowledgeUnits",
                column: "KnowledgeUnitId");

            migrationBuilder.CreateIndex(
                name: "UX_LessonKnowledgeUnits_Lesson_KU",
                table: "LessonKnowledgeUnits",
                columns: new[] { "LessonId", "KnowledgeUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Lessons_Level_Code",
                table: "Lessons",
                columns: new[] { "CurriculumLevelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Lessons_Level_SortOrder",
                table: "Lessons",
                columns: new[] { "CurriculumLevelId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSchedules_User_NextReviewAt",
                table: "ReviewSchedules",
                columns: new[] { "UserId", "NextReviewAt" })
                .Annotation("SqlServer:Include", new[] { "ReviewLevel", "UserKnowledgeMasteryId" });

            migrationBuilder.CreateIndex(
                name: "UX_ReviewSchedules_Mastery",
                table: "ReviewSchedules",
                column: "UserKnowledgeMasteryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionActivities_KnowledgeUnitId",
                table: "SessionActivities",
                column: "KnowledgeUnitId");

            migrationBuilder.CreateIndex(
                name: "UX_SessionActivities_Session_Order",
                table: "SessionActivities",
                columns: new[] { "LearningSessionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionActivityReasons_SessionActivityId",
                table: "SessionActivityReasons",
                column: "SessionActivityId");

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionPlans_Code",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UKDM_Mastery_Dimension",
                table: "UserKnowledgeDimensionMasteries",
                columns: new[] { "UserKnowledgeMasteryId", "Dimension" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserKnowledgeMasteries_KnowledgeUnitId",
                table: "UserKnowledgeMasteries",
                column: "KnowledgeUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UserKnowledgeMasteries_User_LastErrorAt",
                table: "UserKnowledgeMasteries",
                columns: new[] { "UserId", "LastErrorAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserKnowledgeMasteries_User_State",
                table: "UserKnowledgeMasteries",
                columns: new[] { "UserId", "KnowledgeState" });

            migrationBuilder.CreateIndex(
                name: "UX_UserKnowledgeMasteries_User_KU",
                table: "UserKnowledgeMasteries",
                columns: new[] { "UserId", "KnowledgeUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UserLearningGoals_Active",
                table: "UserLearningGoals",
                column: "UserId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UserLearningPlans_CurrentLessonId",
                table: "UserLearningPlans",
                column: "CurrentLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLearningPlans_CurrentLevelId",
                table: "UserLearningPlans",
                column: "CurrentLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLearningPlans_CurriculumId",
                table: "UserLearningPlans",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLearningPlans_GoalId",
                table: "UserLearningPlans",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLearningPlans_User_Status",
                table: "UserLearningPlans",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLessonProgress_LessonId",
                table: "UserLessonProgress",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "UX_UserLessonProgress_User_Lesson",
                table: "UserLessonProgress",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReminders_UserId",
                table: "UserReminders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_UserSkillSnapshots_User_Date_Dimension",
                table: "UserSkillSnapshots",
                columns: new[] { "UserId", "LocalDate", "Dimension" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionPlanId",
                table: "UserSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyCollocations_VocabularyKnowledgeUnitId",
                table: "VocabularyCollocations",
                column: "VocabularyKnowledgeUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyExamples_VocabularyKnowledgeUnitId",
                table: "VocabularyExamples",
                column: "VocabularyKnowledgeUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyLearningStats");

            migrationBuilder.DropTable(
                name: "GrammarPatterns");

            migrationBuilder.DropTable(
                name: "LearningEvidences");

            migrationBuilder.DropTable(
                name: "LessonKnowledgeUnits");

            migrationBuilder.DropTable(
                name: "ReviewSchedules");

            migrationBuilder.DropTable(
                name: "SessionActivityReasons");

            migrationBuilder.DropTable(
                name: "UserKnowledgeDimensionMasteries");

            migrationBuilder.DropTable(
                name: "UserLearningPlans");

            migrationBuilder.DropTable(
                name: "UserLessonProgress");

            migrationBuilder.DropTable(
                name: "UserReminders");

            migrationBuilder.DropTable(
                name: "UserSkillSnapshots");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "VocabularyCollocations");

            migrationBuilder.DropTable(
                name: "VocabularyExamples");

            migrationBuilder.DropTable(
                name: "ActivityAttempts");

            migrationBuilder.DropTable(
                name: "UserKnowledgeMasteries");

            migrationBuilder.DropTable(
                name: "UserLearningGoals");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "Vocabularies");

            migrationBuilder.DropTable(
                name: "SessionActivities");

            migrationBuilder.DropTable(
                name: "CurriculumLevels");

            migrationBuilder.DropTable(
                name: "KnowledgeUnits");

            migrationBuilder.DropTable(
                name: "LearningSessions");

            migrationBuilder.DropTable(
                name: "Curriculums");
        }
    }
}
