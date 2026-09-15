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
            "check-http-status-code-over-time",
            "Check an HTTP status code over time",
            "Track the public HTTP status returned by a URL so redirects, client errors, server errors, and recovery are visible in history.",
            "monitor HTTP status code",
            "http-status",
            "HTTP Status Monitor",
            [
                "Can Clarity record 404 or 500 responses?",
                "Is an HTTP error the same as a network failure?",
                "Can I see when the status recovers?"
            ],
            [
                "Add the public URL you want to observe.",
                "Choose HTTP Status Monitor.",
                "Clarity records the returned HTTP status when a response is available.",
                "Review history when the observed status changes or later recovers."
            ]),
        new(
            "monitor-website-redirect-destination",
            "Monitor a website redirect destination",
            "Keep track of where a public URL ultimately redirects so destination changes do not go unnoticed.",
            "monitor redirect destination",
            "redirect-chain",
            "Redirect Destination Monitor",
            [
                "Can Clarity follow redirects?",
                "How many redirects are followed?",
                "Will a new final destination be recorded?"
            ],
            [
                "Add the public URL that redirects.",
                "Choose Redirect Destination Monitor.",
                "Clarity follows the supported redirect chain and records the final destination.",
                "Review history when the recorded destination changes."
            ]),
        new(
            "monitor-a-broken-link",
            "Monitor a link for broken status",
            "Watch a specific public link and keep evidence when its HTTP state moves between working and broken.",
            "broken link monitor",
            "broken-link",
            "Broken Link Monitor",
            [
                "Does Clarity crawl my whole website?",
                "Can it monitor one important link?",
                "Will a repaired link show as recovered?"
            ],
            [
                "Choose the specific public link that matters.",
                "Create a Broken Link follow.",
                "Clarity checks the response returned by that link.",
                "Review history when the link fails or later recovers."
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
            "nameserver-change-monitor",
            "Nameserver change monitor",
            "Track the public authoritative nameserver set for a domain and keep a history when that set changes.",
            "nameserver change monitor",
            "nameserver-change",
            "Nameserver Monitor",
            [
                "What nameserver data is observed?",
                "Does record order matter?",
                "Can I review the previous nameservers?"
            ],
            [
                "Enter the domain you want to follow.",
                "Choose Nameserver Monitor.",
                "Clarity records the normalized public nameserver set.",
                "Review before-and-after evidence when that set changes."
            ]),
        new(
            "monitor-mx-record-changes",
            "Monitor MX record changes",
            "Watch a domain's public mail-exchange records so mail-routing changes are visible over time.",
            "MX record change monitor",
            "mx-record",
            "MX Record Monitor",
            [
                "Can Clarity track mail-routing records?",
                "Are MX priorities preserved?",
                "Can I see the old record set?"
            ],
            [
                "Enter the domain whose mail routing matters.",
                "Choose MX Record Monitor.",
                "Clarity records the public MX record set.",
                "Review history when the observed mail-exchange configuration changes."
            ]),
        new(
            "monitor-spf-record-changes",
            "Monitor SPF record changes",
            "Track the public SPF policy published for a domain and retain evidence when the observed policy changes.",
            "SPF record monitor",
            "spf-record",
            "SPF Record Monitor",
            [
                "What does Clarity inspect for SPF?",
                "Does Clarity judge whether my SPF policy is correct?",
                "Can I review a previous value?"
            ],
            [
                "Enter the domain whose SPF policy you want to follow.",
                "Choose SPF Record Monitor.",
                "Clarity records the public SPF-related TXT evidence it observes.",
                "Review history when the observed value changes."
            ]),
        new(
            "monitor-dkim-record-changes",
            "Monitor DKIM record changes",
            "Watch a public DKIM selector record and keep history when the published key material changes.",
            "DKIM record monitor",
            "dkim-record",
            "DKIM Record Monitor",
            [
                "Do I need the DKIM selector?",
                "Can Clarity tell when the public key changes?",
                "Does Clarity validate email delivery?"
            ],
            [
                "Use the domain and selector for the public DKIM record you want to follow.",
                "Choose DKIM Record Monitor.",
                "Clarity records the public DKIM DNS evidence it observes.",
                "Review history when that published record changes."
            ]),
        new(
            "monitor-dmarc-record-changes",
            "Monitor DMARC record changes",
            "Track a domain's public DMARC policy and retain a before-and-after history when the record changes.",
            "DMARC record monitor",
            "dmarc-record",
            "DMARC Record Monitor",
            [
                "Can Clarity track the _dmarc record?",
                "Does it interpret whether my policy is good enough?",
                "Can I see the prior policy later?"
            ],
            [
                "Enter the domain whose DMARC policy matters.",
                "Choose DMARC Record Monitor.",
                "Clarity records the public DMARC TXT evidence.",
                "Review history when the observed policy changes."
            ]),
        new(
            "monitor-public-api-endpoint-uptime",
            "Monitor public API endpoint uptime",
            "Watch a safe public health or status endpoint and keep a record of availability and recovery.",
            "API endpoint uptime monitor",
            "api-endpoint-uptime",
            "API Endpoint Uptime Monitor",
            [
                "Can Clarity monitor authenticated APIs?",
                "What kind of endpoint should I use?",
                "Will recovery be recorded?"
            ],
            [
                "Choose a safe public health or status endpoint.",
                "Create an API Endpoint Uptime follow.",
                "Clarity checks public HTTP availability without private credentials.",
                "Review failures and later recovery in the follow history."
            ]),
        new(
            "monitor-public-service-outage",
            "Monitor a public service for outages",
            "Follow a public HTTP endpoint that represents a service you care about and keep outage and recovery evidence over time.",
            "service outage monitor",
            "service-outage",
            "Service Outage Monitor",
            [
                "How does Clarity decide a service is unavailable?",
                "Does it use private account data?",
                "Can I review recovery later?"
            ],
            [
                "Choose the public endpoint that represents the service.",
                "Create a Service Outage follow.",
                "Clarity records public HTTP availability observations.",
                "Review history when the endpoint fails and when it recovers."
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
