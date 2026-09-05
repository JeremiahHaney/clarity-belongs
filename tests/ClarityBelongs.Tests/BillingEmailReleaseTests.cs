using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClarityBelongs.Tests;

public sealed class BillingEmailReleaseTests
{
    [Fact]
    public void EmailConfiguration_RequiresCompleteSecurePublicConfiguration()
    {
        var options = ReadyEmailOptions();

        Assert.True(EmailConfiguration.IsPublicReady(options));

        options.EnableSsl = false;
        Assert.False(EmailConfiguration.IsPublicReady(options));

        options.EnableSsl = true;
        options.Password = string.Empty;
        Assert.False(EmailConfiguration.IsPublicReady(options));
    }

    [Fact]
    public async Task ImmediateEmail_SucceedsOnceAndDoesNotDuplicate()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await SeedPaidNotificationAsync(store.Db);
        var sender = new RecordingEmailSender();
        var options = ReadyEmailOptions();
        var plans = new PlanCatalog();
        var now = DateTime.UtcNow;

        store.Db.ChangeTracker.Clear();
        await NotificationDeliveryCoordinator.DeliverAsync(
            store.Db,
            sender,
            options,
            plans,
            NullLogger.Instance,
            now);

        store.Db.ChangeTracker.Clear();
        await NotificationDeliveryCoordinator.DeliverAsync(
            store.Db,
            sender,
            options,
            plans,
            NullLogger.Instance,
            now.AddMinutes(1));

        var notification = await store.Db.Notifications
            .SingleAsync(x => x.Id == seeded.Notification.Id);

        Assert.Single(sender.Messages);
        Assert.Equal(NotificationStatuses.Sent, notification.Status);
        Assert.Equal(1, notification.AttemptCount);
        Assert.NotNull(notification.SentAtUtc);
    }

    [Fact]
    public async Task ImmediateEmail_RetriesWithBackoffThenDeadLetters()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await SeedPaidNotificationAsync(store.Db);
        var sender = new RecordingEmailSender(fail: true);
        var options = ReadyEmailOptions();
        options.MaxDeliveryAttempts = 3;
        options.RetryBaseSeconds = 10;
        options.RetryMaxSeconds = 60;
        var plans = new PlanCatalog();
        var now = DateTime.UtcNow;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            store.Db.ChangeTracker.Clear();
            await NotificationDeliveryCoordinator.DeliverAsync(
                store.Db,
                sender,
                options,
                plans,
                NullLogger.Instance,
                now.AddMinutes(attempt));
        }

        store.Db.ChangeTracker.Clear();
        var notification = await store.Db.Notifications
            .SingleAsync(x => x.Id == seeded.Notification.Id);

        Assert.Equal(3, notification.AttemptCount);
        Assert.Equal(NotificationStatuses.DeadLetter, notification.Status);
        Assert.NotNull(notification.DeadLetterAtUtc);
        Assert.Null(notification.NextAttemptAtUtc);
        Assert.Equal(3, sender.Attempts);
        Assert.DoesNotContain("\n", notification.FailureReason ?? string.Empty);
    }

    [Fact]
    public async Task DigestState_IsDurableAndPreventsSecondSameDayDigest()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        await SeedPaidNotificationAsync(store.Db);
        var sender = new RecordingEmailSender();
        var options = ReadyEmailOptions();
        options.DeliveryMode = "DailyDigest";
        options.DigestHourUtc = 0;
        var plans = new PlanCatalog();
        var now = DateTime.UtcNow;

        store.Db.ChangeTracker.Clear();
        await NotificationDeliveryCoordinator.DeliverAsync(
            store.Db,
            sender,
            options,
            plans,
            NullLogger.Instance,
            now);

        store.Db.ChangeTracker.Clear();
        await NotificationDeliveryCoordinator.DeliverAsync(
            store.Db,
            sender,
            options,
            plans,
            NullLogger.Instance,
            now.AddHours(1));

        var state = await store.Db.DigestDeliveryStates.SingleAsync();

        Assert.Single(sender.Messages);
        Assert.Equal(DigestDeliveryStatuses.Completed, state.Status);
        Assert.NotNull(state.CompletedAtUtc);
    }

    [Fact]
    public async Task PasswordReset_GeneratesHashedTokenAndSanitizedEmail()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var sender = new RecordingEmailSender();
        var user = new AppUser
        {
            Email = "reset@clarity.test",
            DisplayName = "Reset User",
            PasswordHash = "placeholder"
        };
        var hasher = new PasswordHasher<AppUser>();
        user.PasswordHash = hasher.HashPassword(user, "OriginalPassword123!");
        store.Db.Users.Add(user);
        await store.Db.SaveChangesAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicBaseUrl"] = "https://claritybelongs.test"
            })
            .Build();
        var accounts = new AccountService(
            store.Db,
            hasher,
            sender,
            configuration);

        await accounts.RequestPasswordResetAsync(
            user.Email,
            "https://claritybelongs.test");

        var token = await store.Db.PasswordResetTokens.SingleAsync();
        var message = Assert.Single(sender.Messages);

        Assert.Equal(64, token.TokenHash.Length);
        Assert.Contains("Reset your Clarity Belongs password", message.Subject);
        Assert.Contains("https://claritybelongs.test/reset-password?token=", message.Body);
        Assert.DoesNotContain(token.TokenHash, message.Body);
    }

    [Fact]
    public async Task PasswordReset_WhenEmailDisabled_DoesNotCreateDeadToken()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var sender = new RecordingEmailSender(enabled: false);
        var user = new AppUser
        {
            Email = "disabled@clarity.test",
            DisplayName = "Disabled User",
            PasswordHash = "placeholder"
        };
        var hasher = new PasswordHasher<AppUser>();
        user.PasswordHash = hasher.HashPassword(user, "OriginalPassword123!");
        store.Db.Users.Add(user);
        await store.Db.SaveChangesAsync();
        var accounts = new AccountService(
            store.Db,
            hasher,
            sender,
            new ConfigurationBuilder().Build());

        await accounts.RequestPasswordResetAsync(
            user.Email,
            "https://claritybelongs.test");

        Assert.Empty(store.Db.PasswordResetTokens);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task StripeWebhook_RejectsInvalidSignature()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var service = CreateStripeService(store.Db);
        var payload = BuildSubscriptionEvent(
            "evt_bad",
            DateTimeOffset.UtcNow,
            "active",
            "price_personal");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.HandleWebhookAsync(payload, "t=1,v1=bad"));
    }

    [Fact]
    public async Task StripeWebhook_ReplayIsIdempotent()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var membership = await SeedMembershipAsync(store.Db);
        var service = CreateStripeService(store.Db);
        var created = DateTimeOffset.UtcNow;
        var payload = BuildSubscriptionEvent(
            "evt_replay",
            created,
            "active",
            "price_personal",
            membership.UserId);
        var signature = SignStripe(payload, "whsec_test", created);

        await service.HandleWebhookAsync(payload, signature);
        await service.HandleWebhookAsync(payload, signature);

        Assert.Single(store.Db.StripeWebhookEvents);
        Assert.Equal(MembershipPlans.Personal, membership.PlanCode);
        Assert.Equal(MembershipStatuses.Active, membership.Status);
    }

    [Fact]
    public async Task StripeWebhook_OlderEventCannotRestoreCanceledSubscription()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var membership = await SeedMembershipAsync(store.Db);
        var service = CreateStripeService(store.Db);
        var newer = DateTimeOffset.UtcNow;
        var older = newer.AddMinutes(-2);
        var canceledPayload = BuildSubscriptionEvent(
            "evt_cancel",
            newer,
            "canceled",
            "price_personal",
            membership.UserId);
        var activePayload = BuildSubscriptionEvent(
            "evt_old_active",
            older,
            "active",
            "price_personal",
            membership.UserId);

        await service.HandleWebhookAsync(
            canceledPayload,
            SignStripe(canceledPayload, "whsec_test", newer));
        await service.HandleWebhookAsync(
            activePayload,
            SignStripe(activePayload, "whsec_test", older));

        Assert.Equal(MembershipPlans.Free, membership.PlanCode);
        Assert.Equal(MembershipStatuses.Canceled, membership.Status);
        Assert.NotNull(membership.LastStripeEventCreatedUtc);
        Assert.Equal(
            newer.ToUnixTimeSeconds(),
            new DateTimeOffset(
                membership.LastStripeEventCreatedUtc!.Value,
                TimeSpan.Zero)
                .ToUnixTimeSeconds());
    }

    [Fact]
    public async Task InactivePaidMembership_UsesFreeEntitlements()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var membership = await SeedMembershipAsync(store.Db);
        membership.PlanCode = MembershipPlans.Personal;
        membership.Status = MembershipStatuses.PastDue;
        await store.Db.SaveChangesAsync();
        var service = new MembershipService(
            store.Db,
            new PlanCatalog());

        var summary = await service.GetAsync(
            membership.UserId,
            membership.WorkspaceId);

        Assert.Equal(MembershipPlans.Free, summary.Plan.Code);
        Assert.Equal(5, summary.Plan.MaxActiveFollows);
        Assert.Equal(360, summary.Plan.MinimumCadenceMinutes);
        Assert.Equal(30, summary.Plan.HistoryDays);
        Assert.False(summary.Plan.EmailAlerts);
        Assert.False(summary.Plan.DailyDigest);
    }

    [Fact]
    public void PricingGate_IsFreeOnlyUntilProviderAndApprovedPricesAreReady()
    {
        var options = ReadyStripeOptions();
        options.PublicPaidPlansEnabled = false;

        Assert.False(StripeConfiguration.IsPublicReady(options));
        Assert.Equal(
            string.Empty,
            StripeConfiguration.GetDisplayPrice(
                options,
                MembershipPlans.Personal));

        options.PublicPaidPlansEnabled = true;

        Assert.True(StripeConfiguration.IsPublicReady(options));
        Assert.Equal(
            "$9/month",
            StripeConfiguration.GetDisplayPrice(
                options,
                MembershipPlans.Personal));
    }

    [Fact]
    public void EmailSanitization_RemovesHeaderInjectionAndControlCharacters()
    {
        var subject = SmtpClarityEmailSender.SanitizeHeader(
            "Alert\r\nBcc: attacker@example.com",
            160);
        var body = SmtpClarityEmailSender.SanitizeBody(
            "Normal\0 body\nline two");

        Assert.DoesNotContain("\r", subject);
        Assert.DoesNotContain("\n", subject);
        Assert.Equal("Normal body\nline two", body);
    }

    private static EmailOptions ReadyEmailOptions() => new()
    {
        Enabled = true,
        PublicDeliveryEnabled = true,
        Host = "smtp.clarity.test",
        Port = 587,
        EnableSsl = true,
        Username = "mailer",
        Password = "secret",
        FromAddress = "alerts@clarity.test",
        FromName = "Clarity Belongs",
        DeliveryMode = "Immediate",
        DigestHourUtc = 0,
        MaxDeliveryAttempts = 5,
        RetryBaseSeconds = 10,
        RetryMaxSeconds = 60
    };

    private static StripeOptions ReadyStripeOptions() => new()
    {
        Enabled = true,
        PublicPaidPlansEnabled = true,
        SecretKey = "sk_test",
        WebhookSecret = "whsec_test",
        PersonalPriceId = "price_personal",
        BusinessPriceId = "price_business",
        PersonalDisplayPrice = "$9/month",
        BusinessDisplayPrice = "$29/month",
        SuccessUrl = "https://clarity.test/account?checkout=success",
        CancelUrl = "https://clarity.test/pricing?checkout=canceled",
        PortalReturnUrl = "https://clarity.test/account"
    };

    private static StripeBillingService CreateStripeService(
        ClarityBelongs.Web.Data.ClarityDbContext db)
    {
        var http = new HttpClient(
            new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK))));

        return new StripeBillingService(
            http,
            db,
            new PlanCatalog(),
            Options.Create(ReadyStripeOptions()),
            NullLogger<StripeBillingService>.Instance);
    }

    private static async Task<(Notification Notification, Membership Membership)> SeedPaidNotificationAsync(
        ClarityBelongs.Web.Data.ClarityDbContext db)
    {
        var membership = await SeedMembershipAsync(db);
        membership.PlanCode = MembershipPlans.Personal;
        membership.Status = MembershipStatuses.Active;
        var notification = new Notification
        {
            WorkspaceId = membership.WorkspaceId,
            UserId = membership.UserId,
            Channel = "Email",
            Status = NotificationStatuses.Pending,
            DedupKey = $"email:{Guid.NewGuid():N}",
            Subject = "Clarity: test alert",
            BodySummary = "A monitored value changed."
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return (notification, membership);
    }

    private static async Task<Membership> SeedMembershipAsync(
        ClarityBelongs.Web.Data.ClarityDbContext db)
    {
        var user = new AppUser
        {
            Email = $"billing-{Guid.NewGuid():N}@clarity.test",
            DisplayName = "Billing User"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var workspace = new Workspace
        {
            OwnerUserId = user.Id,
            Name = "Billing Workspace"
        };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();
        var membership = new Membership
        {
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            PlanCode = MembershipPlans.Free,
            Status = MembershipStatuses.Free,
            StripeCustomerId = $"cus_{user.Id}",
            StripeSubscriptionId = $"sub_{user.Id}"
        };
        db.Memberships.Add(membership);
        await db.SaveChangesAsync();
        return membership;
    }

    private static string BuildSubscriptionEvent(
        string eventId,
        DateTimeOffset created,
        string status,
        string priceId,
        long userId = 1)
    {
        var payload = new
        {
            id = eventId,
            type = "customer.subscription.updated",
            created = created.ToUnixTimeSeconds(),
            data = new
            {
                @object = new
                {
                    id = $"sub_{userId}",
                    customer = $"cus_{userId}",
                    status,
                    cancel_at_period_end = status == "canceled",
                    current_period_end = created.AddDays(30).ToUnixTimeSeconds(),
                    metadata = new Dictionary<string, string>
                    {
                        ["user_id"] = userId.ToString(),
                        ["workspace_id"] = "1"
                    },
                    items = new
                    {
                        data = new[]
                        {
                            new
                            {
                                price = new
                                {
                                    id = priceId
                                }
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string SignStripe(
        string payload,
        string secret,
        DateTimeOffset timestamp)
    {
        var unix = timestamp.ToUnixTimeSeconds();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes($"{unix}.{payload}")))
            .ToLowerInvariant();
        return $"t={unix},v1={signature}";
    }

    private sealed class RecordingEmailSender(
        bool fail = false,
        bool enabled = true) : IClarityEmailSender
    {
        public bool IsEnabled { get; } = enabled;
        public int Attempts { get; private set; }
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(
            string recipient,
            string subject,
            string textBody,
            CancellationToken cancellationToken = default)
        {
            Attempts++;

            if (fail)
                throw new InvalidOperationException("provider\nsecret failure detail");

            Messages.Add(new EmailMessage(
                recipient,
                subject,
                textBody));
            return Task.CompletedTask;
        }
    }

    private sealed record EmailMessage(
        string Recipient,
        string Subject,
        string Body);
}
