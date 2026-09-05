namespace ClarityBelongs.Web.Domain;

public static class DigestDeliveryStatuses
{
    public const string Started = "Started";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

// Durable state keeps digest delivery restart-safe instead of relying on worker memory.
public sealed class DigestDeliveryState
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTime DigestDateUtc { get; set; }
    public string Status { get; set; } = DigestDeliveryStatuses.Started;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? LastError { get; set; }
}

// Stripe event IDs and timestamps make webhook processing replay-safe and order-aware.
public sealed class StripeWebhookEvent
{
    public long Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime StripeCreatedUtc { get; set; }
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}