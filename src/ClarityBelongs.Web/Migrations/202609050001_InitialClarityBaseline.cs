using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBelongs.Web.Migrations;

public partial class InitialClarityBaseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AlertRules",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FollowId = table.Column<long>(type: "INTEGER", nullable: false),
                RuleType = table.Column<string>(type: "TEXT", nullable: false),
                ConfigurationJson = table.Column<string>(type: "TEXT", nullable: true),
                MinimumSeverity = table.Column<string>(type: "TEXT", nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AlertRules", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Changes",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TargetId = table.Column<long>(type: "INTEGER", nullable: false),
                PreviousSnapshotId = table.Column<long>(type: "INTEGER", nullable: true),
                CurrentSnapshotId = table.Column<long>(type: "INTEGER", nullable: false),
                DetectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                Severity = table.Column<string>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", nullable: false),
                Summary = table.Column<string>(type: "TEXT", nullable: false),
                BeforeJson = table.Column<string>(type: "TEXT", nullable: true),
                AfterJson = table.Column<string>(type: "TEXT", nullable: true),
                IsMeaningful = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Changes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FeedbackSubmissions",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                ProductSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Contact = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeedbackSubmissions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Follows",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                WorkspaceId = table.Column<long>(type: "INTEGER", nullable: false),
                TargetId = table.Column<long>(type: "INTEGER", nullable: false),
                SourceDefinitionId = table.Column<long>(type: "INTEGER", nullable: false),
                MonitorType = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                Importance = table.Column<string>(type: "TEXT", nullable: false),
                CheckCadenceMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                LastCheckedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                NextCheckAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastMeaningfulChangeAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Follows", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Memberships",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<long>(type: "INTEGER", nullable: false),
                WorkspaceId = table.Column<long>(type: "INTEGER", nullable: false),
                PlanCode = table.Column<string>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                StripeCustomerId = table.Column<string>(type: "TEXT", nullable: true),
                StripeSubscriptionId = table.Column<string>(type: "TEXT", nullable: true),
                StripePriceId = table.Column<string>(type: "TEXT", nullable: true),
                CurrentPeriodEndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CancelAtPeriodEnd = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Memberships", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                WorkspaceId = table.Column<long>(type: "INTEGER", nullable: false),
                UserId = table.Column<long>(type: "INTEGER", nullable: false),
                FollowId = table.Column<long>(type: "INTEGER", nullable: false),
                ChangeId = table.Column<long>(type: "INTEGER", nullable: false),
                Channel = table.Column<string>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                DedupKey = table.Column<string>(type: "TEXT", nullable: false),
                Subject = table.Column<string>(type: "TEXT", nullable: false),
                BodySummary = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                FailedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                FailureReason = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ObservationRuns",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TargetId = table.Column<long>(type: "INTEGER", nullable: false),
                SourceDefinitionId = table.Column<long>(type: "INTEGER", nullable: false),
                StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                HttpStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                ErrorCode = table.Column<string>(type: "TEXT", nullable: true),
                ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                DurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                SnapshotId = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ObservationRuns", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PasswordResetTokens",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<long>(type: "INTEGER", nullable: false),
                TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UsedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Snapshots",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TargetId = table.Column<long>(type: "INTEGER", nullable: false),
                ObservationRunId = table.Column<long>(type: "INTEGER", nullable: false),
                ObservedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ContentType = table.Column<string>(type: "TEXT", nullable: false),
                Fingerprint = table.Column<string>(type: "TEXT", nullable: false),
                NormalizedDataJson = table.Column<string>(type: "TEXT", nullable: false),
                SummaryText = table.Column<string>(type: "TEXT", nullable: true),
                RetentionClass = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Snapshots", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SourceDefinitions",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TargetId = table.Column<long>(type: "INTEGER", nullable: false),
                AdapterType = table.Column<string>(type: "TEXT", nullable: false),
                ConfigurationJson = table.Column<string>(type: "TEXT", nullable: true),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SourceDefinitions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Targets",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TargetType = table.Column<string>(type: "TEXT", nullable: false),
                CanonicalKey = table.Column<string>(type: "TEXT", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                PrimaryUri = table.Column<string>(type: "TEXT", nullable: false),
                MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Targets", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Email = table.Column<string>(type: "TEXT", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                EmailVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Workspaces",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OwnerUserId = table.Column<long>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Workspaces", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FollowChanges",
            columns: table => new
            {
                FollowId = table.Column<long>(type: "INTEGER", nullable: false),
                ChangeId = table.Column<long>(type: "INTEGER", nullable: false),
                MatchedRuleId = table.Column<long>(type: "INTEGER", nullable: true),
                Relevance = table.Column<string>(type: "TEXT", nullable: false),
                IsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false),
                AcknowledgedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FollowChanges", x => new { x.FollowId, x.ChangeId });
            });

        migrationBuilder.CreateIndex("IX_Changes_TargetId_DetectedAtUtc", "Changes", new[] { "TargetId", "DetectedAtUtc" });
        migrationBuilder.CreateIndex("IX_FeedbackSubmissions_CreatedUtc", "FeedbackSubmissions", "CreatedUtc");
        migrationBuilder.CreateIndex("IX_Follows_WorkspaceId_TargetId_MonitorType", "Follows", new[] { "WorkspaceId", "TargetId", "MonitorType" });
        migrationBuilder.CreateIndex("IX_Memberships_StripeCustomerId", "Memberships", "StripeCustomerId");
        migrationBuilder.CreateIndex("IX_Memberships_StripeSubscriptionId", "Memberships", "StripeSubscriptionId");
        migrationBuilder.CreateIndex("IX_Memberships_UserId", "Memberships", "UserId", unique: true);
        migrationBuilder.CreateIndex("IX_Memberships_WorkspaceId", "Memberships", "WorkspaceId", unique: true);
        migrationBuilder.CreateIndex("IX_Notifications_DedupKey", "Notifications", "DedupKey", unique: true);
        migrationBuilder.CreateIndex("IX_ObservationRuns_TargetId_SourceDefinitionId_StartedAtUtc", "ObservationRuns", new[] { "TargetId", "SourceDefinitionId", "StartedAtUtc" });
        migrationBuilder.CreateIndex("IX_PasswordResetTokens_TokenHash", "PasswordResetTokens", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_Snapshots_TargetId_ObservedAtUtc", "Snapshots", new[] { "TargetId", "ObservedAtUtc" });
        migrationBuilder.CreateIndex("IX_SourceDefinitions_TargetId_AdapterType", "SourceDefinitions", new[] { "TargetId", "AdapterType" });
        migrationBuilder.CreateIndex("IX_Targets_CanonicalKey", "Targets", "CanonicalKey", unique: true);
        migrationBuilder.CreateIndex("IX_Users_Email", "Users", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Workspaces_OwnerUserId", "Workspaces", "OwnerUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AlertRules");
        migrationBuilder.DropTable("Changes");
        migrationBuilder.DropTable("FeedbackSubmissions");
        migrationBuilder.DropTable("FollowChanges");
        migrationBuilder.DropTable("Follows");
        migrationBuilder.DropTable("Memberships");
        migrationBuilder.DropTable("Notifications");
        migrationBuilder.DropTable("ObservationRuns");
        migrationBuilder.DropTable("PasswordResetTokens");
        migrationBuilder.DropTable("Snapshots");
        migrationBuilder.DropTable("SourceDefinitions");
        migrationBuilder.DropTable("Targets");
        migrationBuilder.DropTable("Users");
        migrationBuilder.DropTable("Workspaces");
    }
}
