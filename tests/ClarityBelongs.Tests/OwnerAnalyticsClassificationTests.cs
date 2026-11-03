using System.Security.Claims;
using ClarityBelongs.Web.Services;
using Microsoft.Extensions.Configuration;

namespace ClarityBelongs.Tests;

public sealed class OwnerAnalyticsClassificationTests
{
    [Fact]
    public void Owner_identity_requires_matching_authenticated_account()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Emails:0"] = "owner@example.test"
            })
            .Build();

        var owner = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, "OWNER@example.test")
            ],
            "cookie"));

        var visitor = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, "visitor@example.test")
            ],
            "cookie"));

        var anonymous = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, "owner@example.test")
            ]));

        Assert.True(OwnerAccess.IsOwner(owner, configuration));
        Assert.False(OwnerAccess.IsOwner(visitor, configuration));
        Assert.False(OwnerAccess.IsOwner(anonymous, configuration));
    }
}
