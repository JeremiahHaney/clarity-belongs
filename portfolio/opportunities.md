# Initial Opportunity Backlog

These are the first Clarity-native opportunities pulled from the broader idea sandbox. They are grouped by product family rather than treated as independent businesses.

## Money

- Product price monitor
- Product stock availability monitor
- Subscription price-change monitor
- Airline fare monitor
- Hotel rate monitor
- Rental-car rate monitor
- Event ticket price monitor

## Your Internet

- Website uptime monitor
- SSL certificate expiration monitor
- Domain expiration monitor
- DNS record change monitor
- Nameserver change monitor
- HTTP status monitor
- Redirect chain monitor
- Broken-link monitor
- Sitemap change monitor
- Robots.txt change monitor
- MX record monitor
- SPF record monitor
- DKIM record monitor
- DMARC record monitor
- IP blacklist monitor
- Website malware-reputation monitor
- Page-speed regression monitor
- Core Web Vitals monitor

## Changes

- Webpage change monitor
- Website content keyword monitor
- Competitor pricing monitor
- Competitor homepage change monitor
- Terms-of-service change monitor
- Privacy-policy change monitor
- App-store rating monitor
- App-store release monitor
- Software release monitor
- GitHub repository release monitor
- Package dependency release monitor
- Public status-page monitor

## Opportunities

- Job posting monitor
- Grant opportunity monitor
- Government bid monitor
- New listing / availability monitor

## Public Information

- Public meeting agenda monitor
- Regulatory filing monitor
- Policy/public-record change monitoring

## Your Identity

- Brand mention monitor
- Username impersonation monitor
- Domain typo-squat monitor
- Certificate-transparency domain monitor
- Reputation-change monitor

## Platform / Reliability

These are likely useful both as products and as internal infrastructure:

- API endpoint uptime monitor
- Webhook health monitor
- Cron-job heartbeat monitor
- Scheduled-backup heartbeat monitor
- Email-delivery heartbeat monitor
- Cloud-service outage aggregator

## Prioritization rule

Start with opportunities that:

1. use reliable, inexpensive data sources
2. can be fully self-service
3. have clear recurring value
4. reuse the core observation engine
5. create useful search/discovery entry points
6. require little manual support

The first build cluster is website change, uptime, SSL expiration, domain expiration, DNS change, and software/release monitoring.
