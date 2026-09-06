using ClarityBelongs.DatabaseTool;

namespace ClarityBelongs.Tests;

public sealed class DatabaseInventoryReconcilerTests
{
	[Fact]
	public void Compare_ReturnsNoMismatches_WhenCountsMatch()
	{
		var baseline = CreateCounts();
		var candidate = CreateCounts();

		var mismatches = DatabaseInventoryReconciler.Compare(
			baseline,
			candidate);

		Assert.Empty(mismatches);
	}

	[Fact]
	public void Compare_ReportsEachChangedDurableTable()
	{
		var baseline = CreateCounts();
		var candidate = baseline with
		{
			Users = baseline.Users + 1,
			Snapshots = baseline.Snapshots - 1,
			StripeWebhookEvents = baseline.StripeWebhookEvents + 2
		};

		var mismatches = DatabaseInventoryReconciler.Compare(
			baseline,
			candidate);

		Assert.Equal(
			3,
			mismatches.Count);
		Assert.Contains(
			mismatches,
			item => item.StartsWith(
				"Users:",
				StringComparison.Ordinal));
		Assert.Contains(
			mismatches,
			item => item.StartsWith(
				"Snapshots:",
				StringComparison.Ordinal));
		Assert.Contains(
			mismatches,
			item => item.StartsWith(
				"StripeWebhookEvents:",
				StringComparison.Ordinal));
	}

	private static DatabaseInventoryCounts CreateCounts() =>
		new(
			Users: 4,
			Workspaces: 4,
			Memberships: 4,
			PasswordResetTokens: 2,
			Targets: 15,
			SourceDefinitions: 15,
			Follows: 12,
			ObservationRuns: 44,
			Snapshots: 40,
			Changes: 6,
			AlertRules: 12,
			FollowChanges: 6,
			Notifications: 8,
			DigestDeliveryStates: 3,
			StripeWebhookEvents: 5,
			FeedbackSubmissions: 7);
}
