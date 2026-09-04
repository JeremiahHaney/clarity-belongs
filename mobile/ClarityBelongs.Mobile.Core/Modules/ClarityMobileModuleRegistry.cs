namespace ClarityBelongs.Mobile.Core.Modules;

public enum ClarityMobileArea
{
	Dashboard,
	Follows,
	Alerts,
	History,
	Evidence,
	Discover,
	Account
}

public sealed record ClarityMobileModule(
	string Route,
	string Title,
	ClarityMobileArea Area,
	bool RequiresAuthentication,
	bool SupportsOfflineCache,
	bool SupportsPushDeepLink);

public static class ClarityMobileModuleRegistry
{
	public static IReadOnlyList<ClarityMobileModule> Modules { get; } =
	[
		new("/home", "My Clarity", ClarityMobileArea.Dashboard, true, true, true),
		new("/follows", "Follows", ClarityMobileArea.Follows, true, true, true),
		new("/alerts", "Alerts", ClarityMobileArea.Alerts, true, true, true),
		new("/history", "History", ClarityMobileArea.History, true, true, true),
		new("/evidence", "Evidence", ClarityMobileArea.Evidence, true, true, true),
		new("/discover", "Add Follow", ClarityMobileArea.Discover, true, true, false),
		new("/account", "Account", ClarityMobileArea.Account, true, false, false)
	];
}
