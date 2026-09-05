using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace ClarityBelongs.Tests;

public sealed class OwnerOperationsTests
{
    [Fact]
    public void ConfiguredOwnerGetsOwnerRoleAndNormalUserDoesNot()
    {
        using var fixture = DbFixture.Create();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Emails:0"] = "owner@example.com"
            })
            .Build();
        var service = new AccountService(
            fixture.Db,
            new PasswordHasher<AppUser>(),
            new DisabledEmailSender(),
            configuration);
        var owner = service.CreatePrincipal(new AppUser
        {
            Id = 1,
            Email = "owner@example.com",
            DisplayName = "Owner"
        });
        var normal = service.CreatePrincipal(new AppUser
        {
            Id = 2,
            Email = "user@example.com",
            DisplayName = "User"
        });
        var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.True(owner.IsInRole("Owner"));
        Assert.False(normal.IsInRole("Owner"));
        Assert.False(unauthenticated.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public void OwnerRouteRequiresOwnerRole()
    {
        var source = File.ReadAllText(RepositoryFile(
            "src",
            "ClarityBelongs.Web",
            "Components",
            "Pages",
            "OwnerOperations.razor"));

        Assert.Contains("[Authorize(Roles = \"Owner\")]", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ContactSubmissionPersistsSeparatelyFromFeedback()
    {
        await using var fixture = DbFixture.Create();
        var contactService = new ContactService(fixture.Db);

        var contactId = await contactService.SubmitAsync(
            null,
            "Account",
            "Please help with my account.",
            "person@example.com",
            "/contact");
        fixture.Db.FeedbackSubmissions.Add(new FeedbackSubmission
        {
            Kind = "idea",
            Message = "Add a clearer status legend.",
            Contact = "person@example.com",
            CreatedUtc = DateTime.UtcNow
        });
        await fixture.Db.SaveChangesAsync();

        var contact = await fixture.Db.ContactSubmissions.SingleAsync();
        var feedback = await fixture.Db.FeedbackSubmissions.SingleAsync();
        var messages = await new OwnerOperationsService(
            fixture.Db,
            new WorkerHeartbeatRegistry())
            .GetMessagesAsync();

        Assert.Equal(contactId, contact.Id);
        Assert.Equal("Account", contact.Category);
        Assert.Equal("idea", feedback.Kind);
        Assert.Contains(messages, item => item.Source == "Contact" && item.Id == contact.Id);
        Assert.Contains(messages, item => item.Source == "Feedback" && item.Id == feedback.Id);
    }

    [Fact]
    public async Task UserSearchReturnsOnlyIntendedEmailMatches()
    {
        await using var fixture = DbFixture.Create();
        await SeedUserAsync(fixture.Db, "alpha@example.com", "Alpha", "Alpha workspace");
        await SeedUserAsync(fixture.Db, "beta@example.com", "Beta", "Beta workspace");
        var service = new OwnerOperationsService(
            fixture.Db,
            new WorkerHeartbeatRegistry());

        var rows = await service.SearchUsersAsync("alpha@");

        var row = Assert.Single(rows);
        Assert.Equal("alpha@example.com", row.Email);
        Assert.Equal("Alpha workspace", row.WorkspaceName);
    }

    [Fact]
    public async Task WorkerHealthIsSensibleBeforeFirstHeartbeat()
    {
        await using var fixture = DbFixture.Create();
        var service = new OwnerOperationsService(
            fixture.Db,
            new WorkerHeartbeatRegistry());

        var overview = await service.GetOverviewAsync();

        Assert.Null(overview.ObservationHeartbeatUtc);
        Assert.Null(overview.ExpirationHeartbeatUtc);
        Assert.Null(overview.NotificationHeartbeatUtc);
        Assert.Equal(0, overview.DueFollows);
        Assert.Null(overview.OldestDueAge);
    }

    [Fact]
    public void OwnerRoutesAreNoIndexAndSensitiveFieldsAreNotRendered()
    {
        var ownerSource = File.ReadAllText(RepositoryFile(
            "src",
            "ClarityBelongs.Web",
            "Components",
            "Pages",
            "OwnerOperations.razor"));
        var exportSource = File.ReadAllText(RepositoryFile(
            "src",
            "ClarityBelongs.Web",
            "Components",
            "Pages",
            "FeedbackOpsExport.razor"));

        Assert.Contains("noindex, nofollow", ownerSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("noindex, nofollow", exportSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", ownerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("TokenHash", ownerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordResetToken", ownerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("StripeCustomerId", ownerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("StripeSubscriptionId", ownerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void SitemapContainsNoPrivateOperationsRoutes()
    {
        var sitemap = File.ReadAllText(RepositoryFile(
            "src",
            "ClarityBelongs.Web",
            "wwwroot",
            "sitemap.xml"));

        Assert.DoesNotContain("/owner", sitemap, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/api/feedback/recent", sitemap, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SeedUserAsync(
        ClarityDbContext db,
        string email,
        string displayName,
        string workspaceName)
    {
        var user = new AppUser
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = "not-rendered",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var workspace = new Workspace
        {
            OwnerUserId = user.Id,
            Name = workspaceName
        };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        db.Memberships.Add(new Membership
        {
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            PlanCode = MembershipPlans.Free,
            Status = MembershipStatuses.Free
        });
        await db.SaveChangesAsync();
    }

    private static string RepositoryFile(params string[] parts)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ClarityBelongs.slnx")))
                return Path.Combine([directory.FullName, .. parts]);

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Clarity Belongs repository root.");
    }

    private sealed class DisabledEmailSender : IClarityEmailSender
    {
        public bool IsEnabled => false;

        public Task SendAsync(
            string recipient,
            string subject,
            string textBody,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DbFixture : IAsyncDisposable, IDisposable
    {
        private readonly SqliteConnection _connection;

        private DbFixture(
            SqliteConnection connection,
            ClarityDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public ClarityDbContext Db { get; }

        public static DbFixture Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<ClarityDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new ClarityDbContext(options);
            db.Database.EnsureCreated();
            return new DbFixture(connection, db);
        }

        public void Dispose()
        {
            Db.Dispose();
            _connection.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
