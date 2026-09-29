namespace ClarityBelongs.Web.Services;

public sealed record LearnFaq(
    string Question,
    string Answer);

public sealed record LearnEditorialContent(
    string SimpleAnswer,
    string DetailTitle,
    string DetailBody,
    IReadOnlyList<LearnFaq> Faqs);

public static class LearnEditorialContentCatalog
{
    private static readonly IReadOnlyDictionary<string, LearnEditorialContent> Content =
        new Dictionary<string, LearnEditorialContent>(StringComparer.OrdinalIgnoreCase)
        {
            ["how-to-monitor-a-website-for-changes"] = new(
                "Choose the public page you care about, let the first successful check become the baseline, and compare later page observations against it.",
                "What this kind of monitoring is good for",
                "Website change monitoring works best for public pages that are usually stable but important when they change, such as pricing, policies, notices, schedules, and product information.",
                [
                    new("How can I tell when a webpage changes?", "Clarity stores the first observed page state, checks the same public URL again later, and records a change when the later whole-page content differs."),
                    new("Can I keep before and after evidence?", "Yes. The follow history preserves the observations used to show that the public page changed."),
                    new("How often should I check a webpage?", "Use a cadence that matches the consequence of missing a change. A page that changes weekly usually does not need the same cadence as a time-sensitive notice page.")
                ]),
            ["get-notified-when-a-webpage-changes"] = new(
                "Create a Website Change follow for the public page and let Clarity watch it instead of reopening the page yourself.",
                "What the notification is based on",
                "The useful signal is a changed stored observation. Clarity first needs a baseline, then a later check that differs from it before there is a page change to surface.",
                [
                    new("Will Clarity notify me on the first check?", "The first successful check establishes the baseline. There is no earlier observation to compare against yet."),
                    new("What kinds of pages can I follow?", "Public pages such as pricing, policies, notices, schedules, product pages, and other pages Clarity can safely request without signing in."),
                    new("Can I stop watching later?", "Yes. Follows can be paused, resumed, reviewed, or archived from My Clarity.")
                ]),
            ["website-uptime-monitoring-for-small-sites"] = new(
                "Use a public URL that should stay reachable and let Clarity record whether it responds over time.",
                "What uptime monitoring tells you",
                "Uptime monitoring answers a narrow question: did the public URL respond when checked? It is useful for small sites because it creates an availability history without requiring a separate monitoring stack.",
                [
                    new("What counts as downtime?", "Clarity records the result of the public HTTP check. A request that cannot complete successfully is retained as an unavailable observation."),
                    new("Will I see recovery?", "Yes. A later successful observation after a failure shows that the public URL began responding again."),
                    new("Does uptime monitoring detect page edits?", "No. Use Website Change Monitor when the content itself matters.")
                ]),
            ["check-http-status-code-over-time"] = new(
                "Watch the exact public URL and retain the HTTP status Clarity receives on each observation.",
                "Why the exact status can matter",
                "A numeric HTTP result can distinguish a healthy page from a redirect, missing page, access problem, or server error even when a browser would hide some of that detail.",
                [
                    new("Can Clarity keep 404 or 500 responses?", "Yes. Returned error statuses are useful observations and can show failure and later recovery."),
                    new("Does Clarity follow redirects?", "The HTTP observation records the final public URL and status it reaches within the supported request behavior."),
                    new("When should I use this instead of uptime?", "Use HTTP Status when the exact response code is the signal you care about rather than only a general available/unavailable state.")
                ]),
            ["monitor-website-redirect-destination"] = new(
                "Start with the public URL that redirects and let Clarity record where the supported redirect chain ultimately lands.",
                "Why redirect destinations change",
                "Redirects often move during site migrations, campaign changes, domain consolidation, or routing updates. Keeping the final destination over time makes those changes visible.",
                [
                    new("Can Clarity follow redirects?", "Yes. Redirect Destination Monitor follows the supported public redirect chain and records the final destination it reaches."),
                    new("How many redirects are followed?", "The current monitor follows up to three redirects."),
                    new("Will a new final destination be recorded?", "Yes. If a later observation ends at a different supported public URL, that changed destination can be retained in history.")
                ]),
            ["monitor-a-broken-link"] = new(
                "Choose one public link that matters and let Clarity keep checking whether that exact URL still works.",
                "Why one-link monitoring is useful",
                "A single download, documentation page, support link, or partner resource can matter enough to watch even when you do not need a full-site crawler.",
                [
                    new("Does Clarity crawl my whole website?", "No. Broken Link Monitor checks the specific public URL you choose."),
                    new("Can it monitor one important external link?", "Yes, as long as the target is a supported public URL that Clarity can safely request."),
                    new("Will a repaired link show as recovered?", "Yes. A later successful observation after a failed one is retained in the link history.")
                ]),
            ["ssl-certificate-expiration-alert"] = new(
                "Follow the public HTTPS endpoint so Clarity can record the certificate it presents and its expiration date.",
                "What happens around renewal",
                "Certificate renewal usually changes the observed certificate identity, expiration date, or both. Keeping those observations makes renewal visible rather than showing only the current certificate.",
                [
                    new("When does Clarity record expiration information?", "It records the expiration evidence from the public certificate presented when the TLS observation succeeds."),
                    new("Does Clarity keep certificate history?", "Yes. Certificate observations and later changes remain tied to the follow."),
                    new("Can I watch more than one domain?", "Yes. Each supported public HTTPS target can be followed separately, subject to plan limits.")
                ]),
            ["domain-expiration-reminder"] = new(
                "Follow the domain so Clarity can query available public RDAP registry data and preserve the expiration evidence it finds.",
                "Why registry data can vary",
                "Domain registration information is not identical across every top-level domain. RDAP availability and the fields returned depend on the registry, so the monitor records what the public source actually provides.",
                [
                    new("Where does the expiration date come from?", "Clarity uses available public RDAP registry data."),
                    new("Do all top-level domains expose the same data?", "No. Registry coverage and returned fields vary, so some domains provide more complete expiration evidence than others."),
                    new("Can Clarity preserve old registry evidence?", "Yes. Later observations can be compared with earlier registry results when the published expiration state changes.")
                ]),
            ["dns-change-monitor"] = new(
                "Follow the hostname and let Clarity compare its normalized public IP address set over time.",
                "Why normalization matters",
                "DNS answers can arrive in a different order without the underlying destinations changing. Clarity normalizes and sorts the public address set so ordering alone does not look like a real change.",
                [
                    new("What DNS data does Clarity track?", "DNS Change Monitor tracks the public IP address set returned for the hostname."),
                    new("Will reordered DNS answers look like a change?", "No. The address set is normalized and sorted before comparison."),
                    new("Can I review the old addresses?", "Yes. Earlier and later address sets remain available when a real change is recorded.")
                ]),
            ["nameserver-change-monitor"] = new(
                "Follow the domain and let Clarity record its public authoritative nameserver set.",
                "What a nameserver change can indicate",
                "A changed NS set often accompanies a DNS-provider migration, delegation update, or administrative change. The monitor records the public delegation without trying to judge whether the new provider is configured correctly.",
                [
                    new("What nameserver data is observed?", "Clarity records the public NS answers returned for the domain."),
                    new("Does record order matter?", "No. The nameserver set is normalized so ordering alone is not treated as a change."),
                    new("Can I review the previous nameservers?", "Yes. The earlier NS set remains in history when the delegation changes.")
                ]),
            ["monitor-mx-record-changes"] = new(
                "Follow the domain's public MX records so mail-routing changes are retained over time.",
                "What MX changes can show",
                "Changing mail providers, routing priorities, or backup mail exchangers can alter the public MX set. Keeping the old and new records makes that transition reviewable.",
                [
                    new("Can Clarity track mail-routing records?", "Yes. MX Record Monitor observes the public MX records for the domain."),
                    new("Are MX priorities preserved?", "The public evidence includes the routing information returned by DNS, including priority data where available."),
                    new("Can I see the old record set?", "Yes. Earlier MX observations remain in history when the public set changes.")
                ]),
            ["monitor-spf-record-changes"] = new(
                "Follow the domain's public TXT evidence so changes to the published SPF-related policy are easier to spot.",
                "What this monitor does not do",
                "SPF Record Monitor is a change detector, not a full mail-security audit. It records the public DNS evidence and leaves policy quality or sender authorization analysis to a separate review.",
                [
                    new("What does Clarity inspect for SPF?", "It observes public TXT records used for SPF-related evidence and compares the normalized values over time."),
                    new("Does Clarity judge whether my SPF policy is correct?", "No. It detects published changes rather than assigning a compliance or security score."),
                    new("Can I review a previous value?", "Yes. Earlier and later DNS evidence remain available when the published value changes.")
                ]),
            ["monitor-dkim-record-changes"] = new(
                "Use the DKIM selector hostname you care about and let Clarity record its public TXT value over time.",
                "Why the selector matters",
                "DKIM keys are published under selector-specific hostnames. Watching the correct selector lets you see when that public key material or related TXT value changes.",
                [
                    new("Do I need the DKIM selector?", "Yes. The selector identifies the public DKIM hostname that should be observed."),
                    new("Can Clarity tell when the public key changes?", "Yes. A changed TXT result for the selector can be retained as a new observation."),
                    new("Does Clarity validate email delivery?", "No. It watches the public DNS record and does not test message signatures or mail-flow success.")
                ]),
            ["monitor-dmarc-record-changes"] = new(
                "Follow the public TXT record at the domain's _dmarc hostname and keep the observed policy over time.",
                "What can change in DMARC",
                "The published DMARC text can change policy, reporting destinations, alignment settings, percentages, and other fields. Clarity preserves the public record so the before-and-after values are reviewable.",
                [
                    new("Can Clarity track the _dmarc record?", "Yes. DMARC Record Monitor observes the public TXT evidence at the DMARC hostname."),
                    new("Does it interpret whether my policy is good enough?", "No. It records the policy change rather than grading the policy."),
                    new("Can I see the prior policy later?", "Yes. Earlier observations remain in history when the public DMARC value changes.")
                ]),
            ["monitor-public-api-endpoint-uptime"] = new(
                "Choose a safe public health or status endpoint and let Clarity record whether it responds.",
                "Choose the endpoint carefully",
                "A good monitoring target is a lightweight public endpoint whose availability actually represents something you care about. Private APIs that require credentials are intentionally outside this watch.",
                [
                    new("Can Clarity monitor authenticated APIs?", "Not with this public watch. It is designed for safe public endpoints that do not require private credentials."),
                    new("What kind of endpoint should I use?", "Use a stable public health, status, or availability endpoint that is meaningful for the service."),
                    new("Will recovery be recorded?", "Yes. A later successful observation after a failure remains part of the endpoint history.")
                ]),
            ["monitor-public-service-outage"] = new(
                "Follow a public URL that represents the service and let Clarity retain failure and recovery observations.",
                "What the outage signal means",
                "The watch is only as meaningful as the public endpoint you select. Clarity records that endpoint's HTTP availability; it does not infer hidden service health from private telemetry.",
                [
                    new("How does Clarity decide a service is unavailable?", "It uses the result of the public HTTP observation for the endpoint you chose."),
                    new("Does it use private account data?", "No. Service Outage Monitor is based on a public endpoint and does not require private account credentials."),
                    new("Can I review recovery later?", "Yes. A later successful observation after an unavailable state is retained in history.")
                ]),
            ["track-pricing-page-changes"] = new(
                "Follow the public pricing page and let Clarity preserve whole-page evidence when its observed content changes.",
                "What counts as useful pricing evidence",
                "The monitor is most useful on relatively stable pricing pages where plan names, published prices, fees, or offer language matter. It records the page evidence rather than extracting a universal structured price feed.",
                [
                    new("Can I monitor a public pricing page?", "Yes. Website Change Monitor can follow a supported public pricing page."),
                    new("Can I see what the page looked like before?", "The stored observations provide before-and-after evidence when Clarity records a change."),
                    new("Should I use a faster cadence for pricing pages?", "Only when noticing quickly has real value. Stable pricing pages often do not need aggressive checking.")
                ]),
            ["monitor-terms-and-privacy-policy-changes"] = new(
                "Follow the public policy page and keep a factual history when the observed page content changes.",
                "Evidence is not legal interpretation",
                "Clarity can preserve the public before-and-after page evidence, but it does not decide whether a policy change is legally important or explain its legal effect.",
                [
                    new("Can Clarity monitor policy pages?", "Yes. Public terms and privacy pages are good candidates for Website Change Monitor when they are reasonably stable."),
                    new("Does Clarity explain the legal meaning of a change?", "No. It records the factual page change; legal interpretation requires separate review."),
                    new("Will the evidence still be available later?", "Yes. Recorded observations remain in the follow history for later comparison.")
                ]),
            ["monitor-public-notice-page"] = new(
                "Follow the public notice, agenda, schedule, or announcement page so you do not have to keep refreshing it manually.",
                "Pick a page that represents the information",
                "A stable public page works better than a highly dynamic homepage. Choose the specific notice or listing page that is most likely to change when the information you care about changes.",
                [
                    new("Can I monitor a government or school page?", "Yes, if it is a supported public page that can be accessed without signing in."),
                    new("Does the page need an RSS feed?", "No. Website Change Monitor observes the public page itself."),
                    new("Can I choose a slower daily cadence?", "Yes. Use a cadence that matches how quickly the notice matters rather than checking more often than necessary.")
                ]),
            ["what-is-website-change-monitoring"] = new(
                "Website change monitoring stores a baseline for a public page, checks it again later, and records when the observed content differs.",
                "How it differs from simply bookmarking a page",
                "A bookmark helps you return to a page. Change monitoring removes the repeated manual visit and gives you a retained history of what Clarity observed when the page changed.",
                [
                    new("How is change monitoring different from uptime monitoring?", "Uptime asks whether the URL responds. Change monitoring asks whether the observed page content differs from the earlier observation."),
                    new("Why keep snapshots instead of only showing the latest state?", "Without earlier evidence, you can see the current page but not what changed from the previous observed state."),
                    new("What kinds of pages are useful to monitor?", "Pricing, policies, notices, schedules, product information, and other relatively stable public pages where changes matter.")
                ]),
            ["website-change-monitor-vs-uptime-monitor"] = new(
                "Use uptime monitoring for availability and website change monitoring for content. Use both when both questions matter.",
                "Two different signals",
                "A website can be perfectly reachable while its content changes, and it can be unavailable without any meaningful content change. Keeping the signals separate makes the history easier to understand.",
                [
                    new("Do I need both monitors?", "Only if both availability and page-content changes matter to you."),
                    new("Can the same website be followed twice?", "Yes. The same public site can have separate follows for different monitoring questions."),
                    new("Which one should run more often?", "Set each cadence from the value of noticing that specific event quickly. Availability may justify a faster cadence than ordinary content changes.")
                ]),
            ["how-often-should-a-website-monitor-check"] = new(
                "Choose the slowest cadence that still notices the change soon enough to be useful.",
                "Faster is not automatically better",
                "Checking more often creates more requests without adding value when the source changes rarely. A useful cadence depends on how quickly you would act after learning about the change.",
                [
                    new("Is checking every minute always better?", "No. A page that changes weekly gains little from minute-by-minute checks."),
                    new("Why is the Free plan cadence limited?", "Cadence limits keep the shared monitoring service bounded while still supporting useful recurring checks."),
                    new("What cadence works for ordinary public pages?", "For many stable pages, hourly or daily checks are sufficient. Time-sensitive pages may justify a faster supported cadence.")
                ])
        };

    public static LearnEditorialContent Get(string slug) =>
        Content.TryGetValue(slug, out var content)
            ? content
            : throw new KeyNotFoundException($"Learn editorial content is missing for '{slug}'.");

    public static bool HasContent(string slug) => Content.ContainsKey(slug);
}
