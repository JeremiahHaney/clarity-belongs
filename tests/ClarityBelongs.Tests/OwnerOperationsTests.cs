using System.Security.Claims;
using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Tests;

public sealed class OwnerOperationsTests
{
    [Fact]
    public void OwnerRouteRequiresOwnerRole()
    {
        var attribute = typeof(ClarityBelongs.Web.Components.Pages.OwnerOperations)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
        var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity());
        var normalUser = Principal("user@example.com");
        var owner = Principal("owner@example.com", "Owner");

        Assert.Equal("Owner", attribute.Roles);
        Assert.False(unauthenticated.Identity?.IsAuthenticated ?? false);
        Assert.False(normalUser.IsInRole(attribute.Roles));
        Assert.True(owner.IsInRole(attribute.Roles));
    }

    [Fact]
    public async Task ContactSubmissionPersistsSeparatelyFromFeedback()
    {
        await using var fixture = await DbFixture.CreateAsync();
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
        await using var fixture = await DbFixture.CreateAsync();
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
        await using var fixture = await DbFixture.CreateAsync();
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
    public void OwnerRouteIsNoIndexAndDoesNotRenderSensitiveAccountFields()
    {
        var source = File.ReadAllText(FixturePath("OwnerOperations.razor"));

        Assert.Contains("noindex, nofollow", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TokenHash", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordResetToken", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StripeCustomerId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StripeSubscriptionId", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SitemapContainsPublicSupportButNoPrivateOperationsRoutes()
    {
        var sitemap = File.ReadAllText(FixturePath("sitemap.xml"));

        Assert.Contains("https://claritybelongs.com/contact", sitemap, StringComparison.Ordinal);
        Assert.Contains("https://claritybelongs.com/feedback", sitemap, StringComparison.Ordinal);
        Assert.DoesNotContain("/owner", sitemap, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/api/feedback/recent", sitemap, StringComparison.OrdinalIgnoreCase);
    }

    private static ClaimsPrincipal Principal(
        string email,
        string? role = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Email, email)
        };

        if (!string.IsNullOrWhiteSpace(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                "Test"));
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

    private static string FixturePath(string fileName) =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            fileName);

    private sealed class DbFixture : IAsyncDisposable
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

        public static async Task<DbFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ClarityDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new ClarityDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return new DbFixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
