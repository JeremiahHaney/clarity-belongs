# Production Security Configuration

Clarity Belongs keeps production credentials out of source control. `appsettings.json` contains only disabled integrations, empty credential fields, and non-secret defaults.

## Production configuration

Supply production values through the host environment, deployment secret store, or another configuration provider outside the repository.

### Database

- `DatabaseStorage__Path` - absolute path to the production SQLite database.
- `DatabaseStorage__BackupDirectory` - protected directory for database backups.

The production database and backups must not live under the public web root or be committed to source control.

### SMTP

- `Email__Enabled=true`
- `Email__Host`
- `Email__Port`
- `Email__EnableSsl=true`
- `Email__Username` when authentication is required
- `Email__Password` when authentication is required
- `Email__FromAddress`
- `Email__FromName`

When email is enabled, startup validation requires the SMTP host and sender address. SMTP passwords must be supplied only from deployment configuration.

### Stripe

- `Stripe__Enabled=true`
- `Stripe__SecretKey`
- `Stripe__WebhookSecret`
- `Stripe__PersonalPriceId`
- `Stripe__BusinessPriceId`
- production success/cancel/portal URLs as needed

When Stripe is enabled, startup validation requires the secret key, webhook signing secret, and both price IDs. Stripe webhook failures return a generic response; secret material is not rendered to the customer.

### Public base URL

Set `PublicBaseUrl` to the canonical HTTPS origin. Password-reset links use this configured origin rather than trusting an arbitrary request Host header.

## FeedbackOps

The current Clarity application does not require or read a FeedbackOps token. If a future integration adds one, it must be supplied through deployment secrets/configuration and must not be committed, rendered, or logged.

## Cookie and transport requirements

Production is expected to run behind HTTPS. Session and antiforgery cookies use `Secure` outside Development and are `HttpOnly`. HSTS and HTTPS redirection remain enabled outside Development.

## Logging

Log exception objects only to server-side logging. Customer responses must stay generic and must not include credentials, connection strings, tokens, stack traces, SQL details, or upstream exception messages.

## Repository check

Do not commit real values for:

- Stripe secret keys or webhook secrets
- SMTP passwords
- database credentials/connection secrets
- reset tokens
- session cookies
- future FeedbackOps/API tokens
