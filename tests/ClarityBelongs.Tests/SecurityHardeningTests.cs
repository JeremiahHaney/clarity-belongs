using Belongs.Shared.Observation;
using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace ClarityBelongs.Tests;

public sealed class SecurityHardeningTests
{
    [Fact]
    public void LoginProtectorTemporarilyBlocksAfterSixFailures()
    {
        var protector = new LoginAttemptProtector();

        for (var i = 0; i < 6; i++)
            protector.RecordFailure("127.0.0.1:user@example.com");

        Assert.False(protector.CanAttempt("127.0.0.1:user@example.com", out var retry));
        Assert.True(retry > TimeSpan.Zero);
    }

    [Fact]
    public void SecurityThrottleRejectsRequestsPastWindowLimit()
    {
        var throttle = new SecurityThrottle();

        Assert.True(throttle.TryAcquire("feedback", "client", 2, TimeSpan.FromMinutes(10), out _));
        Assert.True(throttle.TryAcquire("feedback", "client", 2, TimeSpan.FromMinutes(10), out _));
        Assert.False(throttle.TryAcquire("feedback", "client", 2, TimeSpan.FromMinutes(10), out var retry));
        Assert.True(retry > TimeSpan.Zero);
    }

    [Fact]
    public void SameOriginValidatorRejectsCrossOriginRequests()
    {
        var allowed = new DefaultHttpContext();
        allowed.Request.Scheme = "https";
        allowed.Request.Host = new HostString("clarity.example");
        allowed.Request.Headers.Origin = "https://clarity.example";
        Assert.True(SameOriginRequestValidator.IsAllowed(allowed.Request));

        var blocked = new DefaultHttpContext();
        blocked.Request.Scheme = "https";
        blocked.Request.Host = new HostString("clarity.example");
        blocked.Request.Headers.Origin = "https://evil.example";
        Assert.False(SameOriginRequestValidator.IsAllowed(blocked.Request));
    }

    [Theory]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://10.0.0.1")]
    [InlineData("http://172.16.0.1")]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://169.254.1.1")]
    [InlineData("http://100.64.0.1")]
    [InlineData("http://224.0.0.1")]
    [InlineData("http://[::1]")]
    [InlineData("http://[fe80::1]")]
    [InlineData("http://[fc00::1]")]
    public async Task PublicEndpointGuardRejectsNonPublicTargets(string value)
    {
        var guard = new PublicEndpointGuard();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => guard.ValidateAsync(new Uri(value)));
    }

    [Fact]
    public async Task RedirectToPrivateTargetIsBlockedBeforeSecondRequest()
    {
        var handler = new RedirectHandler();
        using var client = new HttpClient(handler);
        var engine = new HttpObservationEngine(
            client,
            new PublicEndpointGuard());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.ObserveAsync(
                new Uri("http://93.184.216.34"),
                false,
                "test"));

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task PasswordResetInvalidatesOlderAndUsedTokens()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        await fixture.Accounts.CreateAsync(
            "reset@example.com",
            "Reset User",
            "OriginalPassword123");

        await fixture.Accounts.RequestPasswordResetAsync(
            "reset@example.com",
            "https://clarity.example");
        var first = fixture.Sender.LastToken;

        await fixture.Accounts.RequestPasswordResetAsync(
            "reset@example.com",
            "https://clarity.example");
        var second = fixture.Sender.LastToken;

        Assert.NotEqual(first, second);
        Assert.False(await fixture.Accounts.ResetPasswordAsync(first, "NewPassword12345"));
        Assert.True(await fixture.Accounts.ResetPasswordAsync(second, "NewPassword12345"));
        Assert.False(await fixture.Accounts.ResetPasswordAsync(second, "AnotherPassword12345"));
    }

    [Fact]
    public async Task WorkspaceIdsCannotCrossTenantBoundary()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var userA = await fixture.Accounts.CreateAsync("a@example.com", "A", "PasswordForA123");
        var userB = await fixture.Accounts.CreateAsync("b@example.com", "B", "PasswordForB123");
        var workspaceA = await fixture.Db.Workspaces.SingleAsync(x => x.OwnerUserId == userA.Id);
        var workspaceB = await fixture.Db.Workspaces.SingleAsync(x => x.OwnerUserId == userB.Id);
        var memberships = new MembershipService(fixture.Db, new PlanCatalog());
        var follows = new FollowManagementService(fixture.Db, memberships);
        var followId = await follows.CreateAsync(
            userA.Id,
            workspaceA.Id,
            new CreateFollowInput(
                "Example",
                "https://example.com",
                "Website",
                "WebsiteChange",
                AdapterTypes.Http,
                "{\"mode\":\"content\"}",
                "Normal",
                360,
                "AnyMeaningfulChange"));

        Assert.Null(await follows.GetAsync(workspaceB.Id, followId));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => follows.CreateAsync(
                userB.Id,
                workspaceA.Id,
                new CreateFollowInput(
                    "Blocked",
                    "https://example.com",
                    "Website",
                    "WebsiteChange",
                    AdapterTypes.Http,
                    "{\"mode\":\"content\"}",
                    "Normal",
                    360,
                    "AnyMeaningfulChange")));
    }

    private sealed class RedirectHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("http://127.0.0.1/private");
            return Task.FromResult(response);
        }
    }

    private sealed class FakeEmailSender : IClarityEmailSender
    {
        public bool IsEnabled => true;
        public string LastToken { get; private set; } = string.Empty;

        public Task SendAsync(
            string recipient,
            string subject,
            string textBody,
            CancellationToken cancellationToken = default)
        {
            const string marker = "token=";
            var start = textBody.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            var end = textBody.IndexOfAny(['\r', '\n'], start);
            var encoded = end < 0
                ? textBody[start..]
                : textBody[start..end];
            LastToken = Uri.UnescapeDataString(encoded);
            return Task.CompletedTask;
        }
    }

    private sealed class AccountFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private AccountFixture(
            SqliteConnection connection,
            ClarityDbContext db,
            AccountService accounts,
            FakeEmailSender sender)
        {
            _connection = connection;
            Db = db;
            Accounts = accounts;
            Sender = sender;
        }

        public ClarityDbContext Db { get; }
        public AccountService Accounts { get; }
        public FakeEmailSender Sender { get; }

        public static async Task<AccountFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ClarityDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new ClarityDbContext(options);
            await db.Database.EnsureCreatedAsync();
            var sender = new FakeEmailSender();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PublicBaseUrl"] = "https://clarity.example"
                })
                .Build();
            var accounts = new AccountService(
                db,
                new PasswordHasher<AppUser>(),
                sender,
                config);
            return new AccountFixture(connection, db, accounts, sender);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
