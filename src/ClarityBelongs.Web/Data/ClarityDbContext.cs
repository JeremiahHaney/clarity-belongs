using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Data;

public sealed class ClarityDbContext(DbContextOptions<ClarityDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Target> Targets => Set<Target>();
    public DbSet<SourceDefinition> SourceDefinitions => Set<SourceDefinition>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<ObservationRun> ObservationRuns => Set<ObservationRun>();
    public DbSet<Snapshot> Snapshots => Set<Snapshot>();
    public DbSet<Change> Changes => Set<Change>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<FollowChange> FollowChanges => Set<FollowChange>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<FeedbackSubmission> FeedbackSubmissions => Set<FeedbackSubmission>();
    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        ConfigureModel(modelBuilder);

    public static void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<Workspace>()
            .HasIndex(x => x.OwnerUserId);

        modelBuilder.Entity<Membership>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<Membership>()
            .HasIndex(x => x.WorkspaceId)
            .IsUnique();

        modelBuilder.Entity<Membership>()
            .HasIndex(x => x.StripeCustomerId);

        modelBuilder.Entity<Membership>()
            .HasIndex(x => x.StripeSubscriptionId);

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        modelBuilder.Entity<Target>()
            .HasIndex(x => x.CanonicalKey)
            .IsUnique();

        modelBuilder.Entity<Follow>()
            .HasIndex(x => new { x.WorkspaceId, x.TargetId, x.MonitorType });

        modelBuilder.Entity<SourceDefinition>()
            .HasIndex(x => new { x.TargetId, x.AdapterType });

        modelBuilder.Entity<ObservationRun>()
            .HasIndex(x => new { x.TargetId, x.SourceDefinitionId, x.StartedAtUtc });

        modelBuilder.Entity<Snapshot>()
            .HasIndex(x => new { x.TargetId, x.ObservedAtUtc });

        modelBuilder.Entity<Change>()
            .HasIndex(x => new { x.TargetId, x.DetectedAtUtc });

        modelBuilder.Entity<AlertRule>();

        modelBuilder.Entity<FollowChange>()
            .HasKey(x => new { x.FollowId, x.ChangeId });

        modelBuilder.Entity<Notification>()
            .HasIndex(x => x.DedupKey)
            .IsUnique();

        modelBuilder.Entity<FeedbackSubmission>()
            .Property(x => x.Kind)
            .HasMaxLength(32);

        modelBuilder.Entity<FeedbackSubmission>()
            .Property(x => x.Message)
            .HasMaxLength(4000);

        modelBuilder.Entity<FeedbackSubmission>()
            .Property(x => x.ProductSlug)
            .HasMaxLength(100);

        modelBuilder.Entity<FeedbackSubmission>()
            .Property(x => x.Path)
            .HasMaxLength(500);

        modelBuilder.Entity<FeedbackSubmission>()
            .Property(x => x.Contact)
            .HasMaxLength(320);

        modelBuilder.Entity<FeedbackSubmission>()
            .HasIndex(x => x.CreatedUtc);

        modelBuilder.Entity<ContactSubmission>()
            .Property(x => x.Category)
            .HasMaxLength(32);

        modelBuilder.Entity<ContactSubmission>()
            .Property(x => x.Message)
            .HasMaxLength(4000);

        modelBuilder.Entity<ContactSubmission>()
            .Property(x => x.ContactEmail)
            .HasMaxLength(320);

        modelBuilder.Entity<ContactSubmission>()
            .Property(x => x.SourcePath)
            .HasMaxLength(500);

        modelBuilder.Entity<ContactSubmission>()
            .HasIndex(x => x.CreatedUtc);
    }
}
