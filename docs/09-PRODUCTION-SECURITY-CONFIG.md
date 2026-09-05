# Production Security Configuration

Clarity Belongs keeps production credentials out of source control. `appsettings.json` contains only disabled integrations, empty credential fields, an empty owner allowlist, and non-secret defaults.

## Production configuration

Supply production values through the host environment, deployment secret store, or another configuration provider outside the repository.

### Database

- `DatabaseStorage__Path` - absolute path to the production SQLite database.
- `DatabaseStorage__BackupDirectory` - protected directory for database backups.

The production database and backups must not live under the public web root or be committed to source control.

### Owner operations

Configure at least one owner email before using `/owner` in production:

- `Admin__Emails__0=owner@example.com`
- additional owners can use `Admin__Emails__1`, `Admin__Emails__2`, and so on.

The allowlist defaults to empty, so ordinary authenticated customers do not receive owner access. Owner access is evaluated against the authenticated account email. Keep `/owner` out of public navigation and search indexing.

### SMTP

Phase 1 launches with public email delivery disabled. Do not enable it until a real provider has been certified.

Future provider activation requires both release gates and valid provider configuration, including:

- `Email__Enabled=true`
- `Email__PublicDeliveryEnabled=true`
- `Email__Host`
- `Email__Port`
- `Email__EnableSsl=true`
- `Email__Username` when authentication is required
- `Email__Password` when authentication is required
- `Email__FromAddress`
- `Email__FromName`

When email is enabled, startup validation requires the SMTP host and sender address. SMTP passwords must be supplied only from deployment configuration. Before enabling public delivery, certify change, failure, recovery, expiration, password-reset, and digest delivery through the real provider.

### Stripe

Phase 1 launches Free-only. Paid plans must stay hidden until Stripe test/live configuration has been certified.

Future paid activation requires:

- `Stripe__Enabled=true`
- `Stripe__PublicPaidPlansEnabled=true`
- `Stripe__SecretKey`
- `Stripe__WebhookSecret`
- `Stripe__PersonalPriceId`
- `Stripe__BusinessPriceId`
- approved `Stripe__PersonalDisplayPrice` and `Stripe__BusinessDisplayPrice`
- production success/cancel/portal URLs as needed

When Stripe is enabled, startup validation requires the secret key, webhook signing secret, and both price IDs. Stripe webhook failures return a generic response; secret material is not rendered to the customer. Do not enable the public paid-plan gate until checkout, portal, signed webhooks, replay handling, cancellation/downgrade behavior, and displayed prices have been verified against the provider.

### Public base URL

Set `PublicBaseUrl` to the canonical HTTPS origin. Password-reset links use this configured origin rather than trusting an arbitrary request Host header.

## Feedback operations

The normal owner workflow is `/owner`, using the configured owner email allowlist.

`/api/feedback/recent` also retains the optional `FeedbackOps:Token` / `FeedbackOps__Token` operational-token path for non-browser tooling. If used, supply the token through deployment secrets/configuration. Do not commit, render, or log it. Configured owners may also access the export through their authenticated session.

## Cookie and transport requirements

Production is expected to run behind HTTPS. Session and antiforgery cookies use `Secure` outside Development and are `HttpOnly`. HSTS and HTTPS redirection remain enabled outside Development.

## Logging

Log exception objects only to server-side logging. Customer responses must stay generic and must not include credentials, connection strings, tokens, stack traces, SQL details, or upstream exception messages.

## Repository check

Do not commit real values for:

- owner/customer credentials
- Stripe secret keys or webhook secrets
- SMTP passwords
- database credentials/connection secrets
- reset tokens
- session cookies
- FeedbackOps/API tokens
