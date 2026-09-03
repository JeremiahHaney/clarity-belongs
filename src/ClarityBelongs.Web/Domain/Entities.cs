namespace ClarityBelongs.Web.Domain;

public static class FollowStatuses
{
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string NeedsAttention = "NeedsAttention";
    public const string Error = "Error";
    public const string Archived = "Archived";
}

public static class ObservationStatuses
{
    public const string Queued = "Queued";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";
}

public static class ChangeSeverities
{
    public const string Info = "Info";
    public const string Notice = "Notice";
    public const string Important = "Important";
    public const string Critical = "Critical";
}

public static class AdapterTypes
{
    public const string Http = "Http";
    public const string Tls = "Tls";
    public const string Dns = "Dns";
    public const string DnsRecord = "DnsRecord";
    public const string Domain = "Domain";
}

public sealed class AppUser
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Workspace
{
    public long Id { get; set; }
    public long OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Target
{
    public long Id { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string CanonicalKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PrimaryUri { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SourceDefinition
{
    public long Id { get; set; }
    public long TargetId { get; set; }
    public string AdapterType { get; set; } = AdapterTypes.Http;
    public string? ConfigurationJson { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Follow
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public long TargetId { get; set; }
    public long SourceDefinitionId { get; set; }
    public string MonitorType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = FollowStatuses.Active;
    public string Importance { get; set; } = "Normal";
    public int CheckCadenceMinutes { get; set; } = 15;
    public DateTime? LastCheckedAtUtc { get; set; }
    public DateTime NextCheckAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastMeaningfulChangeAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ObservationRun
{
    public long Id { get; set; }
    public long TargetId { get; set; }
    public long SourceDefinitionId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = ObservationStatuses.Queued;
    public int? HttpStatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public long? DurationMilliseconds { get; set; }
    public long? SnapshotId { get; set; }
}

public sealed class Snapshot
{
    public long Id { get; set; }
    public long TargetId { get; set; }
    public long ObservationRunId { get; set; }
    public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;
    public string ContentType { get; set; } = "application/json";
    public string Fingerprint { get; set; } = string.Empty;
    public string NormalizedDataJson { get; set; } = "{}";
    public string? SummaryText { get; set; }
    public string RetentionClass { get; set; } = "StandardHistory";
}

public sealed class Change
{
    public long Id { get; set; }
    public long TargetId { get; set; }
    public long? PreviousSnapshotId { get; set; }
    public long CurrentSnapshotId { get; set; }
    public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
    public string ChangeType { get; set; } = "ContentChanged";
    public string Severity { get; set; } = ChangeSeverities.Notice;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public bool IsMeaningful { get; set; } = true;
}

public sealed class AlertRule
{
    public long Id { get; set; }
    public long FollowId { get; set; }
    public string RuleType { get; set; } = "AnyMeaningfulChange";
    public string? ConfigurationJson { get; set; }
    public string MinimumSeverity { get; set; } = ChangeSeverities.Notice;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class FollowChange
{
    public long FollowId { get; set; }
    public long ChangeId { get; set; }
    public long? MatchedRuleId { get; set; }
    public string Relevance { get; set; } = "Relevant";
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Notification
{
    public long Id { get; set; }
    public long WorkspaceId { get; set; }
    public long UserId { get; set; }
    public long FollowId { get; set; }
    public long ChangeId { get; set; }
    public string Channel { get; set; } = "InApp";
    public string Status { get; set; } = "Pending";
    public string DedupKey { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodySummary { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
