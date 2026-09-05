namespace ClarityBelongs.Web.Services;

public sealed record LearnEntry(
    string Slug,
    string Title,
    string Description,
    string SearchIntent,
    string ProductSlug,
    string ProductName,
    IReadOnlyList<string> Questions,
    IReadOnlyList<string> Steps);

public sealed class LearnContentCatalog
{
    private static readonly IReadOnlyList<LearnEntry> Entries =
    [
        new(
            "how-to-monitor-a-website-for-changes",
            "How to monitor a website for changes",
            "A simple way to stop revisiting the same webpage and keep a useful history when it changes.",
            "monitor website changes",
            "website-change",
            "Website Change Monitor",
            [
                "How can I tell when a webpage changes?",
                "Can I keep before and after evidence?",
                "How often should I check a webpage?"
            ],
            [
                "Choose the public webpage you would otherwise revisit manually.",
                "Create a Website Change follow in Clarity.",
                "Let the first successful observation establish the baseline.",
                "Review recorded changes and their before/after evidence in My Clarity."
            ]),
        new(
            "get-notified-when-a-webpage-changes",
            "Get notified when a webpage changes",
            "Track public pages such as pricing, policies, product pages, notices, and schedules without checking them yourself.",
            "notify me when a webpage changes",
            "website-change",
            "Website Change Monitor",
            [
                "What kinds of pages can Clarity follow?",
                "What happens when the recorded page content differs?",
                "Where can I review an old change later?"
            ],
            [
                "Paste the public page URL.",
                "Choose a check cadence appropriate for how quickly the page matters.",
                "Clarity stores page observations and compares them over time.",
                "Open the change history when the recorded whole-page content differs."
            ]),
        new(
            "website-uptime-monitoring-for-small-sites",
            "Website uptime monitoring for small sites",
            "Know when a public website stops responding and when it comes back without running a separate operations platform.",
            "website uptime monitor",
            "website-uptime",
            "Website Uptime Monitor",
            [
                "Can Clarity tell me when my site is down?",
                "Does response-time variation count as downtime?",
                "Will recovery be recorded too?"
            ],
            [
                "Add your public website URL.",
                "Clarity checks availability state rather than treating response-time variation as a content change.",
                "A failed availability check records an error or down state.",
                "Recovery is recorded when the endpoint responds again."
            ]),
        new(
            "ssl-certificate-expiration-alert",
            "SSL certificate expiration alert",
            "Track a public TLS certificate and keep its expiration state visible before renewal becomes urgent.",
            "SSL expiration alert",
            "ssl-expiration",
            "SSL Expiration Monitor",
            [
                "When does Clarity record expiration reminders?",
                "Does Clarity store certificate history?",
                "Can I watch more than one domain?"
            ],
            [
                "Add the HTTPS site or host.",
                "Clarity records the public certificate identity and expiration date.",
                "Supported expiration thresholds are recorded without creating duplicate reminders for the same threshold.",
                "Certificate changes remain visible in history."
            ]),
        new(
            "domain-expiration-reminder",
            "Domain expiration reminder",
            "Keep a domain's published registry expiration date visible before renewal is due.",
            "domain expiration reminder",
            "domain-expiration",
            "Domain Expiration Monitor",
            [
                "Where does the expiration date come from?",
                "Do all top-level domains expose the same data?",
                "Can Clarity preserve old registry evidence?"
            ],
            [
                "Enter the domain name.",
                "Clarity queries available RDAP registry data.",
                "The returned expiration evidence is stored in history.",
                "Supported upcoming-expiration thresholds are recorded when the registry data is available."
            ]),
        new(
            "dns-change-monitor",
            "DNS change monitor",
            "See when a hostname begins resolving to a different public address set.",
            "DNS change monitor",
            "dns-change",
            "DNS Change Monitor",
            [
                "What DNS data does Clarity track?",
                "Will reordered DNS answers look like a change?",
                "Can I review the old addresses?"
            ],
            [
                "Enter the public hostname.",
                "Clarity normalizes and sorts the public address set.",
                "A changed normalized set becomes a DNS change event.",
                "Review the before/after evidence from the follow history."
            ]),
        new(
            "track-pricing-page-changes",
            "Track pricing page changes",
            "Keep an evidence trail when a public pricing page changes instead of relying on memory or screenshots.",
            "track pricing page changes",
            "website-change",
            "Website Change Monitor",
            [
                "Can I monitor a public pricing page?",
                "Can I see what the page looked like before?",
                "Should I use a faster cadence for pricing pages?"
            ],
            [
                "Open the public pricing page you care about.",
                "Create a Website Change follow.",
                "Let Clarity establish the baseline.",
                "Use history to review later whole-page differences."
            ]),
        new(
            "monitor-terms-and-privacy-policy-changes",
            "Monitor terms and privacy policy changes",
            "Follow a public terms or privacy page and keep a timeline of recorded page changes.",
            "terms change monitor",
            "website-change",
            "Website Change Monitor",
            [
                "Can Clarity monitor policy pages?",
                "Does Clarity explain the legal meaning of a change?",
                "Will the evidence still be available later?"
            ],
            [
                "Add the public terms or privacy URL.",
                "Clarity records the observed page state.",
                "A later changed fingerprint creates a history item.",
                "Use the evidence as a factual before/after record; Clarity does not provide legal interpretation."
            ]),
        new(
            "monitor-public-notice-page",
            "Monitor a public notice page",
            "Follow a public notice, agenda, schedule, or announcement page so you do not have to keep refreshing it.",
            "monitor public notice page",
            "website-change",
            "Website Change Monitor",
            [
                "Can I monitor a government or school page?",
                "Does the page need an RSS feed?",
                "Can I choose a slower daily cadence?"
            ],
            [
                "Use a public URL that can be accessed without signing in.",
                "Choose Website Change Monitor.",
                "Pick a cadence that matches how quickly the information matters.",
                "Review recorded changes from My Clarity instead of repeatedly visiting the page."
            ]),
        new(
            "what-is-website-change-monitoring",
            "What is website change monitoring?",
            "Website change monitoring revisits a public page, stores a baseline, compares later observations, and records differences.",
            "what is website change monitoring",
            "website-change",
            "Website Change Monitor",
            [
                "How is change monitoring different from uptime monitoring?",
                "Why keep snapshots instead of only showing the latest state?",
                "What kinds of pages are useful to monitor?"
            ],
            [
                "Uptime asks whether the site responds.",
                "Change monitoring asks whether the observed whole-page content differs.",
                "History makes later changes reviewable.",
                "Use it for pages whose changes matter more than constant manual checking."
            ]),
        new(
            "website-change-monitor-vs-uptime-monitor",
            "Website change monitor vs. uptime monitor",
            "Choose the right monitor by separating two questions: did the site respond, and did its content change?",
            "website change monitor vs uptime monitor",
            "website-change",
            "Website Change Monitor",
            [
                "Do I need both monitors?",
                "Can the same website be followed twice?",
                "Which one should run more often?"
            ],
            [
                "Use uptime when availability is the concern.",
                "Use website change when the public page content is the concern.",
                "Use both when both questions matter.",
                "Set cadence based on the consequence of missing a change or outage."
            ]),
        new(
            "how-often-should-a-website-monitor-check",
            "How often should a website monitor check?",
            "Pick a monitoring cadence from the value of noticing quickly, not from an arbitrary fastest-possible setting.",
            "how often website monitor check",
            "website-change",
            "Website Change Monitor",
            [
                "Is checking every minute always better?",
                "Why is the Free plan cadence limited?",
                "What cadence works for ordinary public pages?"
            ],
            [
                "Use slower checks for information that changes daily or weekly.",
                "Use faster checks when a short delay has real value.",
                "Avoid unnecessary checks when a slower cadence is enough.",
                "The Free plan currently checks no more often than every six hours."
            ])
    ];

    public IReadOnlyList<LearnEntry> GetAll() => Entries;

    public LearnEntry? GetBySlug(string? slug) => Entries
        .FirstOrDefault(x => string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
