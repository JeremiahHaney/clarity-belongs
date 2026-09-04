# Clarity Belongs Mobile Platform

Clarity Belongs ships as one .NET MAUI iOS/Android app over the existing account, follow, history, evidence, alert, and membership backend.

## Mobile role

The app is a native client for the existing Clarity platform. Observation, scheduling, comparison, persistence, billing enforcement, and outbound email remain server-side.

## First mobile surfaces

- Sign in and account state
- My Clarity dashboard
- Active follows
- Follow detail
- Observation history
- Before/after evidence
- Alert inbox
- Push notification deep links
- Create/edit/pause/delete follow
- Plan/usage status

The 64 product entry points are modules within this single app, not separate store apps.

## Native capabilities

- push notifications
- secure token storage
- deep links
- background refresh where platform policies permit it
- offline cache of recent follows/history
- native sharing

## Reuse boundary

Shared observation contracts remain in `src/Belongs.Shared`. Mobile-specific transport/authentication code belongs in the mobile client. Web UI code is not copied into MAUI pages.
