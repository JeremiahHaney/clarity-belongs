namespace ClarityBelongs.Web.Services;

public sealed record PublicProductFaq(
    string Question,
    string Answer);

public sealed record PublicProductContent(
    string ObserveTitle,
    string ObserveBody,
    string CompareTitle,
    string CompareBody,
    string HistoryTitle,
    string HistoryBody,
    IReadOnlyList<string> UseCases,
    string Limitation,
    IReadOnlyList<PublicProductFaq> Faqs);

public static class PublicProductContentCatalog
{
    private static readonly IReadOnlyDictionary<string, PublicProductContent> Content =
        new Dictionary<string, PublicProductContent>(StringComparer.OrdinalIgnoreCase)
        {
            ["website-uptime"] = new(
                "Check whether the site responds",
                "Clarity requests the public URL and records whether it is reachable along with the returned HTTP result.",
                "Notice outages and recovery",
                "A failed observation can be followed by a later successful one, giving you a simple availability history instead of a single current-state check.",
                "Keep an availability timeline",
                "Saved observations make it possible to review when the site was unavailable and when it began responding again.",
                [
                    "A business homepage that should stay reachable",
                    "A public customer portal or documentation site",
                    "A public health or status URL where availability matters"
                ],
                "Website Uptime is about reachability. It does not tell you whether the visible page content changed or whether every feature behind the page works.",
                [
                    new("What does the monitor consider an outage?", "Clarity records the result of its public HTTP check. A request that cannot complete successfully is retained as an unavailable observation rather than being treated as a content change."),
                    new("Will it show when the site comes back?", "Yes. A later successful observation is stored after an unavailable one, so recovery is visible in the follow history."),
                    new("Should I use this or Website Change Monitor?", "Use Website Uptime when the question is whether the URL responds. Use Website Change when the question is whether the page content changed.")
                ]),
            ["http-status"] = new(
                "Record the HTTP result",
                "Clarity checks the exact public URL you provide and stores the returned HTTP status together with the final URL it observed.",
                "Compare status changes",
                "A move from one status to another, such as 200 to 404 or 503 back to 200, becomes useful evidence in the history.",
                "Keep the exact response history",
                "The stored status makes this monitor useful when the numeric HTTP result matters more than a general up/down label.",
                [
                    "A public health endpoint",
                    "A URL that should continue returning 200",
                    "A page where redirects, 404s, or server errors matter"
                ],
                "This monitor records the public HTTP result. It does not authenticate to private endpoints or test application behavior beyond the request it makes.",
                [
                    new("Does Clarity keep 4xx and 5xx responses?", "Yes. Returned error statuses are observations worth keeping because they can show a broken page, unavailable endpoint, or later recovery."),
                    new("Does the final URL matter?", "Yes. Clarity records the final public URL and status it observes, which helps distinguish an expected response from a redirect or changed destination."),
                    new("Is this the same as uptime monitoring?", "They overlap, but HTTP Status is the better fit when the exact status code is the thing you need to track.")
                ]),
            ["redirect-chain"] = new(
                "Follow the redirect path",
                "Clarity starts with the public URL you provide and follows the supported redirect chain to the final public destination.",
                "Detect a destination change",
                "If the final destination later differs, the new observed URL can be compared with the previous result.",
                "Keep where the URL used to go",
                "History preserves the earlier and later destinations so a redirect change is reviewable instead of disappearing after the switch.",
                [
                    "A shortened or campaign URL",
                    "An old page that permanently redirects elsewhere",
                    "A public link whose destination should remain stable"
                ],
                "Clarity follows a bounded redirect chain of up to three redirects. It is not a general crawler and does not bypass authentication or private destinations.",
                [
                    new("How many redirects are followed?", "The current public monitor follows up to three redirects before recording the final supported destination."),
                    new("Will a different final destination be saved?", "Yes. A later check that resolves somewhere different can be retained as a changed observation."),
                    new("Can it monitor a private redirect?", "No. This watch is designed for safe public URLs and does not use private credentials.")
                ]),
            ["broken-link"] = new(
                "Check one important link",
                "Clarity requests the exact public link you provide and records whether that link returns a working or failing HTTP result.",
                "Notice failure and repair",
                "When the link moves from working to failing, or later becomes reachable again, the change is visible in history.",
                "Keep evidence for that URL",
                "The follow remains focused on the one link you selected, which makes the history easy to interpret.",
                [
                    "A download link customers depend on",
                    "A documentation or support link",
                    "A partner or external resource linked from your site"
                ],
                "Broken Link Monitor checks one specific public URL per follow. It does not crawl an entire site looking for every broken link.",
                [
                    new("Does Clarity crawl my whole website?", "No. You choose the specific public link that matters and Clarity watches that URL."),
                    new("What happens when the link is repaired?", "A later successful check is stored after the failure so the recovery is visible."),
                    new("Can I use it for an external link?", "Yes, as long as the target is a supported public URL that Clarity can safely request.")
                ]),
            ["ssl-expiration"] = new(
                "Read the public certificate",
                "Clarity connects to the public TLS endpoint and records certificate identity and expiration information.",
                "Watch renewal and certificate changes",
                "Later observations can show a new certificate or a changed expiration date after renewal.",
                "Keep certificate evidence",
                "The observed certificate state stays with the follow so you can review what Clarity saw before and after a renewal.",
                [
                    "A production website certificate",
                    "A customer-facing API with HTTPS",
                    "A domain where certificate renewal must not be missed"
                ],
                "Clarity reports the certificate presented by the public endpoint it can reach. It does not validate every possible client, internal certificate, or private network path.",
                [
                    new("What expiration date does Clarity use?", "It uses the expiration date from the public certificate presented by the endpoint during the observation."),
                    new("Will renewal show up?", "Yes. A renewed or replaced certificate changes the observed certificate state and can be retained in history."),
                    new("Can I watch more than one site?", "Yes. Each public HTTPS target can be followed separately, subject to the active-follow limits of your plan.")
                ]),
            ["domain-expiration"] = new(
                "Read published registry data",
                "Clarity queries available RDAP information for the domain and records the expiration evidence returned by the registry source.",
                "Notice a changed expiration date",
                "Renewal can move the published expiration date forward, and later observations preserve that changed registry state.",
                "Keep the registry evidence",
                "The history shows what expiration information was available when Clarity checked rather than only displaying today's value.",
                [
                    "A primary business domain",
                    "A campaign or product domain that must stay registered",
                    "A domain managed by someone else but important to your work"
                ],
                "Registry data varies by top-level domain and registry. Some domains expose less information or may temporarily return incomplete RDAP data.",
                [
                    new("Where does the expiration date come from?", "Clarity uses available public RDAP registry data rather than guessing from DNS or the website itself."),
                    new("Will a renewal be visible?", "If the registry publishes a new expiration date, a later observation can preserve that updated value."),
                    new("Why might a domain have no expiration result?", "RDAP coverage and fields vary by registry, so Clarity can only record the public registration evidence that is actually available.")
                ]),
            ["dns-change"] = new(
                "Resolve the hostname",
                "Clarity looks up the public address set for the hostname and normalizes the returned IP addresses before storing them.",
                "Compare address sets",
                "Because the addresses are normalized and sorted, a reordered answer does not look like a change while a genuinely different set does.",
                "Keep before-and-after DNS evidence",
                "History lets you see which public addresses were observed before the hostname began resolving somewhere else.",
                [
                    "A website moving between hosting providers",
                    "A public service behind changing infrastructure",
                    "A hostname whose destination should stay predictable"
                ],
                "This monitor tracks the public IP address set returned by DNS. It does not inspect every DNS record type; those have dedicated monitors.",
                [
                    new("Does DNS answer order create false changes?", "Clarity normalizes and sorts the address set before comparison so ordering alone is not treated as a change."),
                    new("Can I see the previous addresses?", "Yes. Earlier observations remain available in the follow history when the resolved set changes."),
                    new("Does this include MX, SPF, or nameserver records?", "No. DNS Change is for public address resolution. MX, SPF, DKIM, DMARC, and nameservers have their own watches.")
                ]),
            ["nameserver-change"] = new(
                "Read the authoritative nameserver set",
                "Clarity queries the public NS records for the domain and stores a normalized set of nameserver answers.",
                "Detect a delegation change",
                "A different normalized nameserver set indicates that the domain's published delegation changed.",
                "Keep the old and new nameservers",
                "The history gives you a factual record of the previous and current nameserver sets.",
                [
                    "A domain being migrated to a new DNS provider",
                    "A business domain whose DNS delegation should remain stable",
                    "A third-party domain where an unexpected provider change matters"
                ],
                "This watch reports the public NS records returned for the domain. It does not determine whether the new nameservers are configured correctly.",
                [
                    new("Does nameserver order matter?", "No. Clarity normalizes the set so a different ordering of the same nameservers is not treated as a meaningful change."),
                    new("What kind of change is recorded?", "A change in the public authoritative nameserver set can be retained with the previous and later observations."),
                    new("Does this prove DNS is working?", "No. It shows the delegation Clarity observed. Other DNS records and service behavior need their own checks.")
                ]),
            ["mx-record"] = new(
                "Read mail-exchange records",
                "Clarity queries the domain's public MX records and stores the observed mail-routing targets and priorities.",
                "Compare mail-routing changes",
                "A changed MX record set can show that mail delivery has been moved, reordered, or reconfigured.",
                "Keep prior MX evidence",
                "History preserves the earlier mail-exchange configuration so you can compare it with the current one.",
                [
                    "A company domain using hosted email",
                    "A domain being migrated between mail providers",
                    "A domain where unexpected mail-routing changes would matter"
                ],
                "Clarity records public MX evidence. It does not send test mail or prove that the receiving mail system is delivering messages correctly.",
                [
                    new("Are MX priorities preserved?", "Yes. The public MX evidence includes the routing information Clarity observes, including priority data where returned."),
                    new("Can I see the old mail provider?", "If the MX set changes, the previous observation remains in history for comparison."),
                    new("Does this test email delivery?", "No. It watches the public DNS routing configuration, not end-to-end message delivery.")
                ]),
            ["spf-record"] = new(
                "Read public SPF-related TXT evidence",
                "Clarity queries the domain's public TXT records and stores the normalized values used to observe SPF-related changes.",
                "Notice policy changes",
                "A changed published TXT value can reveal that the domain's sender policy was modified.",
                "Keep the previous published value",
                "History gives you the earlier and later DNS evidence without requiring you to remember what the SPF text used to say.",
                [
                    "A business domain with a stable outbound-mail policy",
                    "A domain adding or removing a mail service",
                    "A migration where SPF changes need to be reviewed"
                ],
                "Clarity records the public DNS evidence. It does not grade SPF quality, simulate mail delivery, or guarantee that a policy is valid for every sender.",
                [
                    new("What does Clarity inspect for SPF?", "It observes public TXT records used for SPF-related DNS evidence and compares the normalized values over time."),
                    new("Does Clarity tell me whether my SPF policy is correct?", "No. This monitor is about detecting a published change, not providing a full SPF compliance audit."),
                    new("Can I review an earlier SPF value?", "Yes. A changed observation keeps the previous and later published DNS evidence in history.")
                ]),
            ["dkim-record"] = new(
                "Read the selected DKIM record",
                "Clarity queries the public TXT record for the selector hostname you provide and stores the observed value.",
                "Notice key publication changes",
                "A changed selector record can show that published DKIM key material or related settings were rotated.",
                "Keep the prior selector evidence",
                "History retains what the public selector returned before and after the change.",
                [
                    "A mail system rotating DKIM keys",
                    "A domain migration between email providers",
                    "A critical selector whose public key should not change unexpectedly"
                ],
                "You need to provide the DKIM selector hostname. Clarity observes the public record but does not validate signed email messages or delivery.",
                [
                    new("Do I need the selector?", "Yes. DKIM keys are published under selector-specific hostnames, so Clarity needs the selector hostname you want to watch."),
                    new("Will a rotated public key show up?", "Yes. A changed TXT result for that selector can be retained as a new observation."),
                    new("Does this verify DKIM email delivery?", "No. It monitors the public DNS record, not message signatures or mail-flow success.")
                ]),
            ["dmarc-record"] = new(
                "Read the public DMARC policy",
                "Clarity queries the TXT record at the domain's _dmarc hostname and stores the observed policy evidence.",
                "Compare policy changes",
                "A later record can show changes to policy, reporting addresses, alignment settings, or other published DMARC values.",
                "Keep the previous policy text",
                "History makes it possible to review exactly what public DMARC evidence Clarity observed before the change.",
                [
                    "A business domain with a published DMARC policy",
                    "A domain moving from monitoring to enforcement",
                    "A mail-security configuration managed by multiple administrators"
                ],
                "Clarity records the public DMARC TXT evidence. It does not determine whether the policy is optimal or analyze DMARC aggregate reports.",
                [
                    new("Can Clarity track the _dmarc record?", "Yes. The monitor watches the public TXT evidence at the DMARC hostname you provide."),
                    new("Does it judge whether my policy is strong enough?", "No. It records and compares the published policy rather than assigning a security grade."),
                    new("Can I see the policy that existed before?", "Yes. Earlier observations remain available when the public record changes.")
                ]),
            ["api-endpoint-uptime"] = new(
                "Check a safe public endpoint",
                "Clarity requests the public API health or status endpoint you choose and records whether it responds.",
                "Notice failure and recovery",
                "Availability transitions are kept so a failed endpoint and its later recovery can be reviewed.",
                "Keep endpoint-specific history",
                "The follow stays tied to the exact public endpoint rather than treating the entire service as a black box.",
                [
                    "A public /health or /status endpoint",
                    "A public API dependency used by your application",
                    "A lightweight endpoint that represents service availability"
                ],
                "The endpoint must be safe to access publicly without private credentials. Clarity does not store API secrets or authenticate to private services for this watch.",
                [
                    new("Can Clarity monitor authenticated APIs?", "Not with this public watch. Use a safe public health or status endpoint that does not require private credentials."),
                    new("What endpoint should I choose?", "Choose a stable endpoint whose response is a useful signal for the service you care about, such as a public health or status URL."),
                    new("Will recovery be recorded?", "Yes. A successful observation after a failed one becomes part of the endpoint's history.")
                ]),
            ["service-outage"] = new(
                "Check the public service signal",
                "Clarity requests the public service or status URL you choose and records its HTTP availability.",
                "Track outage and recovery transitions",
                "A failure and later recovery are stored as separate observations so you can see the service state change over time.",
                "Keep a simple outage history",
                "The timeline gives you factual evidence from the public endpoint instead of relying on memory or repeated manual checks.",
                [
                    "A hosted service's public status endpoint",
                    "A public application URL used as an availability signal",
                    "A third-party dependency with a stable public endpoint"
                ],
                "Clarity only observes the public endpoint you provide. It does not infer outages from private telemetry, customer accounts, or internal service health.",
                [
                    new("How does Clarity decide the service is unavailable?", "It uses the result of the public HTTP observation for the endpoint you selected."),
                    new("Does it use private account data?", "No. This watch is deliberately based on a public endpoint and does not require private service credentials."),
                    new("Can I review when the service recovered?", "Yes. A later successful observation is retained after the failed state.")
                ]),
            ["website-change"] = new(
                "Capture the public page state",
                "Clarity stores the observed response for the public webpage you choose and creates the first baseline.",
                "Compare later page observations",
                "A later whole-page content difference becomes a change that can be reviewed against the earlier observation.",
                "Keep before-and-after evidence",
                "The history is useful for pages where the fact that something changed matters more than repeatedly opening the page yourself.",
                [
                    "Pricing and plan pages",
                    "Terms, privacy policies, and public notices",
                    "Product, schedule, announcement, or availability pages"
                ],
                "The current monitor compares the observed whole-page content. Dynamic pages can change for reasons that are not important to you, so relatively stable public pages are the best fit.",
                [
                    new("Does Clarity tell me exactly what the change means?", "It preserves factual before-and-after evidence. Interpreting the business, legal, or practical meaning of that change remains up to you."),
                    new("What pages are a good fit?", "Public pages that are fairly stable and worth revisiting when they change, such as pricing, policies, notices, schedules, and product information."),
                    new("How is this different from uptime?", "Website Change asks whether the observed content differs. Website Uptime asks whether the URL responds.")
                ])
        };

    public static PublicProductContent Get(string slug) =>
        Content.TryGetValue(slug, out var content)
            ? content
            : throw new KeyNotFoundException($"Public product content is missing for '{slug}'.");

    public static bool HasContent(string slug) => Content.ContainsKey(slug);
}
