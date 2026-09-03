# Accounts, Membership, and Billing

## Purpose

Launch Phase 5 turns Clarity from a single-owner development workspace into a multi-user self-service product.

The boundary is intentionally simple:

`authenticated user -> personal workspace -> membership -> follows`

A signed-in user can only read or change follows and evidence that belong to that user's personal workspace.

## Authentication

Clarity uses ASP.NET Core cookie authentication.

Current account flows:

- sign up
- sign in
- sign out
- forgot password
- one-hour, single-use password reset tokens

Passwords are stored only as ASP.NET Core password hashes. Raw passwords and raw password-reset tokens are not persisted.

The session cookie is HTTP-only, SameSite=Lax, and uses a sliding 30-day lifetime.

## Personal workspace

Each new account gets one personal workspace named `My Clarity`.

The authenticated user's ID is the ownership root. My Clarity, Follow detail, change evidence, settings, and follow-management operations are scoped through that workspace rather than selecting the first workspace in the database.

## Membership plans

The current product limits are deliberately tied to ongoing delivery cost rather than artificial feature fragmentation.

| Plan | Active follows | Fastest cadence | History target | Email | Digest |
|---|---:|---:|---:|---|---|
| Free | 5 | 6 hours | 30 days | No | No |
| Personal | 50 | 15 minutes | 365 days | Yes | Yes |
| Business | 250 | 5 minutes | 730 days | Yes | Yes |

The history values are product targets for the retention layer. Phase 5 enforces active-follow count, check cadence, and email-delivery entitlement.

In-app alerts remain available on Free.

## Membership persistence

`Membership` stores:

- UserId
- WorkspaceId
- PlanCode
- Status
- Stripe customer ID
- Stripe subscription ID
- Stripe price ID
- current period end
- cancel-at-period-end state

Known membership states include Free, Active, Trialing, PastDue, Canceled, and Incomplete.

## Stripe boundary

Stripe configuration is read from the `Stripe` configuration section. No Stripe secrets belong in the repository.

Required production values:

- `Stripe__Enabled=true`
- `Stripe__SecretKey`
- `Stripe__WebhookSecret`
- `Stripe__PersonalPriceId`
- `Stripe__BusinessPriceId`
- production success/cancel/portal URLs as needed

The implementation uses Stripe-hosted Checkout for subscription creation and Stripe's hosted Billing Portal for customer billing management.

Webhook endpoint:

`POST /webhooks/stripe`

Handled events:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.payment_failed`

Webhook signatures are verified with the configured Stripe signing secret and a five-minute timestamp tolerance before membership state is changed.

## Email entitlement

The notification pipeline may create email delivery records for events, but the delivery worker verifies that the owning Membership is an active paid plan before sending.

This keeps the alert engine independent from billing while enforcing delivery cost at the final delivery boundary.

Production SMTP configuration is still required separately.

## Existing development databases

Earlier V1 builds used EF Core `EnsureCreated`, which does not evolve an existing SQLite schema.

`DatabaseSchemaService` performs the Phase 5 additive SQLite upgrade on startup:

- adds authentication fields to Users when missing
- creates Memberships
- creates PasswordResetTokens
- adds supporting indexes
- creates Free membership records for existing workspaces

A new database still initializes normally through EF Core.

## CI verification

The GitHub Actions workflow now verifies more than startup:

1. restore and Release build
2. start a clean SQLite application instance
3. verify `/health`
4. create a real account through `/auth/signup`
5. retain the authentication cookie
6. open the authenticated Account page
7. create an authenticated Free-plan follow through `/api/follows`

This verifies the local account/workspace/membership boundary without requiring external Stripe or SMTP credentials.

## Testing Required before public billing

The code boundary is implemented, but the following require real external configuration before public release:

- Stripe test-mode Checkout with the actual Personal price
- Stripe test-mode Checkout with the actual Business price
- webhook delivery and signature verification from Stripe
- subscription upgrade/cancel/past-due synchronization
- Billing Portal return flow
- production SMTP password-reset delivery
- production paid alert email delivery

Do not mark real-money billing Released until those external tests pass.
