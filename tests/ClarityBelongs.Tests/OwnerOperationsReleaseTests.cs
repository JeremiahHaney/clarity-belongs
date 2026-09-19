using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace ClarityBelongs.Tests;

public sealed class OwnerOperationsReleaseTests
{
    [Fact]
    public void OwnerAccessRequiresAuthenticatedConfiguredEmail()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Emails:0"] = "owner@example.com"
            })
            .Build();
        var owner = Principal("owner@example.com");
        var customer = Principal("customer@example.com");
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.True(OwnerAccess.IsOwner(owner, configuration));
        Assert.False(OwnerAccess.IsOwner(customer, configuration));
        Assert.False(OwnerAccess.IsOwner(anonymous, configuration));
    }

    [Fact]
    public async Task OverviewAndMessagesSeparateContactFromProductFeedback()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await TestData.SeedFollowAsync(store.Db);
        var now = DateTime.UtcNow;

        store.Db.FeedbackSubmissions.AddRange(
            new FeedbackSubmission
            {
                Kind = "contact",
                Message = "Please help with my account.",
                Contact = "person@example.com",
                ProductSlug = "clarity",
                Path = "/contact",
                CreatedUtc = now
            },
            new FeedbackSubmission
            {
                Kind = "bug",
                Message = "A product behavior needs attention.",
                ProductSlug = "clarity",
                Path = "/feedback",
                CreatedUtc = now
            });
        store.Db.ObservationRuns.Add(new ObservationRun
        {
            TargetId = seeded.Target.Id,
            SourceDefinitionId = seeded.Source.Id,
            StartedAtUtc = now,
            CompletedAtUtc = now,
            Status = ObservationStatuses.Failed,
            ErrorCode = "test_failure",
            ErrorMessage = "Safe first line\nInternal second line"
        });
        store.Db.Notifications.Add(new Notification
        {
            WorkspaceId = seeded.Workspace.Id,
            UserId = seeded.User.Id,
            FollowId = seeded.Follow.Id,
            Channel = "InApp",
            Status = NotificationStatuses.Pending,
            DedupKey = "owner-ops-test",
            Subject = "Test alert",
            BodySummary = "Test"
        });
        await store.Db.SaveChangesAsync();

        var service = new OwnerOperationsService(store.Db);
        var overview = await service.GetOverviewAsync();
        var messages = await service.GetMessagesAsync();
        var failures = await service.GetFailuresAsync();

        Assert.Equal(1, overview.RecentContacts);
        Assert.Equal(1, overview.RecentFeedback);
        Assert.Equal(1, overview.FailedRuns24Hours);
        Assert.Equal(1, overview.PendingNotifications);
        Assert.Contains(messages, message => message.Source == "Contact");
        Assert.Contains(messages, message => message.Source == "Feedback");
        Assert.Single(failures);
        Assert.DoesNotContain("Internal second line", failures[0].ErrorSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AcquisitionReportConnectsCampaignToUsefulFollowAndReturn()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await TestData.SeedFollowAsync(store.Db);
        var now = DateTime.UtcNow;

        store.Db.AcquisitionEvents.AddRange(
            new AcquisitionEvent
            {
                VisitorId = "0123456789abcdef0123456789abcdef",
                EventType = AcquisitionEventTypes.Visit,
                Source = "reddit",
                Medium = "community",
                Campaign = "priority-zero",
                OccurredAtUtc = now.AddMinutes(-10)
            },
            new AcquisitionEvent
            {
                VisitorId = "0123456789abcdef0123456789abcdef",
                UserId = seeded.User.Id,
                WorkspaceId = seeded.Workspace.Id,
                EventType = AcquisitionEventTypes.FollowStarted,
                ProductSlug = "website-uptime",
                Source = "reddit",
                Medium = "community",
                Campaign = "priority-zero",
                OccurredAtUtc = now.AddMinutes(-8)
            },
            new AcquisitionEvent
            {
                VisitorId = "0123456789abcdef0123456789abcdef",
                UserId = seeded.User.Id,
                WorkspaceId = seeded.Workspace.Id,
                EventType = AcquisitionEventTypes.FollowCreated,
                ProductSlug = "website-uptime",
                FollowId = seeded.Follow.Id,
                Source = "reddit",
                Medium = "community",
                Campaign = "priority-zero",
                OccurredAtUtc = now.AddMinutes(-7)
            },
            new AcquisitionEvent
            {
                VisitorId = "0123456789abcdef0123456789abcdef",
                UserId = seeded.User.Id,
                WorkspaceId = seeded.Workspace.Id,
                EventType = AcquisitionEventTypes.DashboardOpened,
                Source = "reddit",
                Medium = "community",
                Campaign = "priority-zero",
                OccurredAtUtc = now
            });

        store.Db.ObservationRuns.Add(new ObservationRun
        {
            TargetId = seeded.Target.Id,
            SourceDefinitionId = seeded.Source.Id,
            StartedAtUtc = now.AddMinutes(-5),
            CompletedAtUtc = now.AddMinutes(-5),
            Status = ObservationStatuses.Succeeded
        });
        await store.Db.SaveChangesAsync();

        var rows = await new OwnerOperationsService(store.Db)
            .GetAcquisitionAsync();

        var row = Assert.Single(rows);
        Assert.Equal("reddit", row.Source);
        Assert.Equal(1, row.Visits);
        Assert.Equal(1, row.Starts);
        Assert.Equal(1, row.Created);
        Assert.Equal(1, row.UsefulObservations);
        Assert.Equal(1, row.ReturnUsers);
    }

    [Fact]
    public async Task UserAndFollowSearchReturnOnlyOperationalFieldsNeededForSupport()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await TestData.SeedFollowAsync(store.Db);
        var service = new OwnerOperationsService(store.Db);

        var users = await service.SearchUsersAsync(seeded.User.Email);
        var follows = await service.SearchFollowsAsync(seeded.Follow.Id.ToString());

        Assert.Single(users);
        Assert.Equal(seeded.User.Email, users[0].Email);
        Assert.Single(follows);
        Assert.Equal(seeded.Follow.Id, follows[0].FollowId);
        Assert.Equal(seeded.Target.PrimaryUri, follows[0].Target);
    }

    private static ClaimsPrincipal Principal(string email) =>
        new(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Email, email)
                ],
                "test"));
}
