namespace ClarityBelongs.DatabaseTool;

public sealed record DatabaseInventoryReport(
	DateTime GeneratedUtc,
	DatabaseInventorySource Source,
	DatabaseInventoryValidation Validation,
	DatabaseInventoryCounts Counts);

public sealed record DatabaseInventorySource(
	string FileName,
	long LengthBytes,
	DateTime LastWriteUtc,
	string Sha256);

public sealed record DatabaseInventoryValidation(
	bool Reachable,
	string? Integrity,
	int ForeignKeyViolations,
	bool SchemaCurrent,
	string[] AppliedMigrations,
	string[] PendingMigrations);

public sealed record DatabaseInventoryCounts(
	long Users,
	long Workspaces,
	long Memberships,
	long PasswordResetTokens,
	long Targets,
	long SourceDefinitions,
	long Follows,
	long ObservationRuns,
	long Snapshots,
	long Changes,
	long AlertRules,
	long FollowChanges,
	long Notifications,
	long DigestDeliveryStates,
	long StripeWebhookEvents,
	long FeedbackSubmissions);

public static class DatabaseInventoryReconciler
{
	public static IReadOnlyList<string> Compare(
		DatabaseInventoryCounts baseline,
		DatabaseInventoryCounts candidate)
	{
		var mismatches = new List<string>();

		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.Users),
			baseline.Users,
			candidate.Users);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.Workspaces),
			baseline.Workspaces,
			candidate.Workspaces);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.Memberships),
			baseline.Memberships,
			candidate.Memberships);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.PasswordResetTokens),
			baseline.PasswordResetTokens,
			candidate.PasswordResetTokens);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.Targets),
			baseline.Targets,
			candidate.Targets);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.SourceDefinitions),
			baseline.SourceDefinitions,
			candidate.SourceDefinitions);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.Follows),
			baseline.Follows,
			candidate.Follows);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.ObservationRuns),
			baseline.ObservationRuns,
			candidate.ObservationRuns);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.Snapshots),
			baseline.Snapshots,
			candidate.Snapshots);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.Changes),
			baseline.Changes,
			candidate.Changes);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.AlertRules),
			baseline.AlertRules,
			candidate.AlertRules);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.FollowChanges),
			baseline.FollowChanges,
			candidate.FollowChanges);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.Notifications),
			baseline.Notifications,
			candidate.Notifications);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.DigestDeliveryStates),
			baseline.DigestDeliveryStates,
			candidate.DigestDeliveryStates);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.StripeWebhookEvents),
			baseline.StripeWebhookEvents,
			candidate.StripeWebhookEvents);
		CompareValue(
			mismatches,
			nameof(DatabaseInventoryCounts.FeedbackSubmissions),
			baseline.FeedbackSubmissions,
			candidate.FeedbackSubmissions);

		return mismatches;
	}

	private static void CompareValue(
		ICollection<string> mismatches,
		string name,
		long baseline,
		long candidate)
	{
		if (baseline == candidate)
			return;

		mismatches.Add(
			$"{name}: baseline={baseline}, candidate={candidate}");
	}
}
