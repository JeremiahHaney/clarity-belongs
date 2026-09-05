namespace ClarityBelongs.Web.Domain;

public static class MembershipPlans
{
    public const string Free = "Free";
    public const string Personal = "Personal";
    public const string Business = "Business";
}

public static class MembershipStatuses
{
    public const string Free = "Free";
    public const string Active = "Active";
    public const string Trialing = "Trialing";
    public const string PastDue = "PastDue";
    public const string Canceled = "Canceled";
    public const string Incomplete = "Incomplete";
}

public sealed class Membership
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long WorkspaceId { get; set; }
    public string PlanCode { get; set; } = MembershipPlans.Free;
    public string Status { get; set; } = MembershipStatuses.Free;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? LastStripeEventCreatedUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PasswordResetToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed record PlanDefinition(
    string Code,
    string Name,
    string Description,
    int MaxActiveFollows,
    int MinimumCadenceMinutes,
    int HistoryDays,
    bool EmailAlerts,
    bool DailyDigest);

public sealed class PlanCatalog
{
    private static readonly IReadOnlyList<PlanDefinition> Plans =
    [
        new(
            MembershipPlans.Free,
            "Free",
            "A small personal Clarity workspace with slower checks.",
            5,
            360,
            30,
            false,
            false),
        new(
            MembershipPlans.Personal,
            "Personal",
            "More follows, faster checks, history, and email delivery.",
            50,
            15,
            365,
            true,
            true),
        new(
            MembershipPlans.Business,
            "Business",
            "Higher limits for a larger set of things worth watching.",
            250,
            5,
            730,
            true,
            true)
    ];

    public IReadOnlyList<PlanDefinition> GetAll() => Plans;

    public PlanDefinition Get(string? code) => Plans
        .FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))
        ?? Plans[0];
}
