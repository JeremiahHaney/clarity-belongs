# Core Observation Engine + My Clarity Data Model

## Goal

Clarity Belongs should support many monitoring products without creating a separate data model for each monitor.

The shared model must answer:

1. What is the user following?
2. Where does the data come from?
3. What did Clarity observe?
4. What changed?
5. Does the change matter to this user?
6. Was the user notified?
7. What history/evidence can we show later?

## Core flow

```text
User / Workspace
      |
      v
    Follow
      |
      v
    Target ---------> Source Adapter
      |                    |
      |                    v
      |                Observation Run
      |                    |
      |                    v
      |                 Snapshot
      |                    |
      |                    v
      +---------------> Comparison
                           |
                           v
                         Change
                           |
                 +---------+---------+
                 |                   |
                 v                   v
             My Clarity          Alert Rule
                 |                   |
                 |                   v
                 |              Notification
                 |                   |
                 +---------<---------+
```

## Identity and ownership

### User

Represents the signed-in person.

Minimum fields:

- Id
- Email
- DisplayName
- CreatedAtUtc
- LastSeenAtUtc

### Workspace

Allows the same engine to support one person today and shared/business monitoring later.

Minimum fields:

- Id
- Name
- OwnerUserId
- CreatedAtUtc

V1 can create one personal workspace automatically for each user.

### WorkspaceMember

Minimum fields:

- WorkspaceId
- UserId
- Role
- JoinedAtUtc

V1 roles:

- Owner
- Member

## What the user follows

### Follow

The user-facing object in My Clarity.

A Follow means: "Keep track of this for me."

Minimum fields:

- Id
- WorkspaceId
- TargetId
- MonitorType
- Name
- Description
- Status
- Importance
- CheckCadenceMinutes
- LastCheckedAtUtc
- NextCheckAtUtc
- LastMeaningfulChangeAtUtc
- CreatedAtUtc
- UpdatedAtUtc

Status:

- Active
- Paused
- NeedsAttention
- Error
- Archived

Importance:

- Low
- Normal
- High
- Critical

Examples:

- "softwarebelongs.com SSL"
- "Flight LAX -> LIT for Thanksgiving"
- "Competitor pricing page"
- "California grant page"

## What is observed

### Target

Canonical thing being monitored.

Minimum fields:

- Id
- TargetType
- CanonicalKey
- DisplayName
- PrimaryUri
- MetadataJson
- CreatedAtUtc
- UpdatedAtUtc

Examples of TargetType:

- WebPage
- Website
- Domain
- TlsEndpoint
- DnsRecordSet
- Product
- FareSearch
- Feed
- ApiEndpoint
- PublicDataset
- Package

CanonicalKey allows multiple users to follow the same public target without requiring duplicate target definitions.

### SourceDefinition

Defines how Clarity retrieves the target.

Minimum fields:

- Id
- TargetId
- AdapterType
- ConfigurationJson
- IsEnabled
- CreatedAtUtc
- UpdatedAtUtc

Initial AdapterType values:

- Http
- Dns
- Tls
- Feed
- Api
- Manual

Later adapters can add airline, commerce, package registry, public-record, and other specialized sources without changing the core model.

## Observation execution

### ObservationRun

One attempt to observe a target.

Minimum fields:

- Id
- TargetId
- SourceDefinitionId
- StartedAtUtc
- CompletedAtUtc
- Status
- HttpStatusCode
- ErrorCode
- ErrorMessage
- DurationMilliseconds
- SnapshotId

Status:

- Queued
- Running
- Succeeded
- Failed
- Skipped

Runs are operational history. A failed run does not itself imply that the monitored thing changed.

### Snapshot

Normalized evidence from a successful ObservationRun.

Minimum fields:

- Id
- TargetId
- ObservationRunId
- ObservedAtUtc
- ContentType
- Fingerprint
- NormalizedDataJson
- SummaryText
- EvidenceLocation
- RetentionClass

The core engine should compare normalized values rather than raw provider-specific responses whenever possible.

Fingerprint supports cheap no-change detection before a deeper comparison.

RetentionClass:

- CurrentOnly
- StandardHistory
- ImportantEvidence

V1 can keep normalized text/JSON in the database and defer large binary evidence to later storage infrastructure.

## Detecting change

### Change

Represents a meaningful difference between snapshots.

Minimum fields:

- Id
- TargetId
- PreviousSnapshotId
- CurrentSnapshotId
- DetectedAtUtc
- ChangeType
- Severity
- Title
- Summary
- BeforeJson
- AfterJson
- DiffJson
- IsMeaningful

Common ChangeType values:

- Created
- Removed
- ContentChanged
- ValueIncreased
- ValueDecreased
- StatusChanged
- AvailabilityChanged
- ExpirationApproaching
- CertificateChanged
- DnsChanged
- NewItem
- RemovedItem

Severity:

- Info
- Notice
- Important
- Critical

A monitor-specific comparer can create specialized ChangeType values while still producing the same Change entity.

## Deciding what matters

### AlertRule

Controls when a Follow should notify its user.

Minimum fields:

- Id
- FollowId
- RuleType
- ConfigurationJson
- MinimumSeverity
- IsEnabled
- CreatedAtUtc
- UpdatedAtUtc

Initial RuleType values:

- AnyMeaningfulChange
- NumericThreshold
- ValueBelow
- ValueAbove
- StatusEquals
- ContainsText
- MissingText
- ExpirationWithinDays

Examples:

- Notify when fare drops below $350.
- Notify when SSL has fewer than 30 days remaining.
- Notify when page content changes.
- Notify when product becomes available.

### FollowChange

Links a shared Target Change to the follows affected by it.

Minimum fields:

- FollowId
- ChangeId
- MatchedRuleId
- Relevance
- IsAcknowledged
- AcknowledgedAtUtc
- CreatedAtUtc

Relevance:

- Informational
- Relevant
- Alert

This indirection is important because one public target change may matter differently to different users.

## Notifications

### Notification

Records a delivery attempt created from an alert-worthy FollowChange.

Minimum fields:

- Id
- WorkspaceId
- UserId
- FollowChangeId
- Channel
- Status
- Subject
- BodySummary
- CreatedAtUtc
- SentAtUtc
- FailedAtUtc
- FailureReason

V1 Channel:

- Email
- InApp

Later:

- Push
- Sms
- Webhook

Status:

- Pending
- Sent
- Failed
- Suppressed

Notification history must remain separate from Change history. A change can exist even if notification delivery fails or is suppressed.

## My Clarity read model

My Clarity should not expose database tables directly. Build a dashboard/read model from the shared entities.

### Dashboard sections

```text
MY CLARITY
|
+-- Needs Attention
|   +-- failed monitors
|   +-- urgent expirations
|   +-- critical changes
|
+-- Since You Were Here
|   +-- relevant changes since LastSeenAtUtc
|
+-- Following
|   +-- all active follows
|   +-- current state
|   +-- last checked
|   +-- next check
|
+-- History
|   +-- chronological changes
|   +-- before / after
|   +-- evidence
|
+-- Alerts
|   +-- notification history
|   +-- acknowledged/unacknowledged
|
+-- Settings
    +-- cadence
    +-- alert rules
    +-- notification preferences
```

### MyClarityFollowSummary

Suggested read-model fields:

- FollowId
- Name
- MonitorType
- CurrentState
- Status
- Importance
- LastCheckedAtUtc
- NextCheckAtUtc
- LastMeaningfulChangeAtUtc
- UnacknowledgedChangeCount
- LatestChangeTitle
- LatestChangeSeverity

### MyClarityChangeItem

Suggested read-model fields:

- FollowId
- FollowName
- ChangeId
- DetectedAtUtc
- Severity
- Title
- Summary
- BeforeSummary
- AfterSummary
- IsAcknowledged

## Monitor plugin contract

Every monitor should provide three narrow capabilities instead of owning its own persistence model.

```text
IObservationAdapter
  ObserveAsync(target, sourceDefinition)
      -> normalized observation result

IObservationComparer
  Compare(previousSnapshot, currentSnapshot)
      -> zero or more changes

IMonitorDefinition
  defines monitor type
  validates user configuration
  creates target/source definition
  supplies default cadence
  supplies default alert rules
  formats current-state summary
```

Examples:

```text
SSL Monitor
  TlsObservationAdapter
  TlsObservationComparer
  SslMonitorDefinition

Webpage Change Monitor
  HttpObservationAdapter
  TextObservationComparer
  WebpageMonitorDefinition
```

## Scheduler model

The scheduler should operate on active Follows but deduplicate execution by Target + SourceDefinition where possible.

```text
Follow.NextCheckAtUtc reached
        |
        v
resolve Target + SourceDefinition
        |
        v
reuse pending/recent compatible run if possible
        |
        v
ObservationRun
        |
        v
Snapshot -> Compare -> Changes
        |
        v
fan changes back out to matching Follows
```

This is how many users can follow the same public page without Clarity necessarily fetching it once per user.

## V1 boundaries

Build now:

- user + automatic personal workspace
- Follow
- Target
- SourceDefinition
- ObservationRun
- Snapshot
- Change
- AlertRule
- FollowChange
- Notification
- My Clarity dashboard/read models
- scheduler abstraction
- HTTP/TLS/DNS adapters as first reusable adapters
- email + in-app notification records

Do not build yet:

- teams/enterprise permission model beyond Owner/Member
- SMS
- mobile push
- webhook notifications
- AI interpretation as a required dependency
- browser automation for hostile/dynamic sites
- large screenshot/video evidence retention
- provider-specific commerce/airline integrations
- elaborate billing quotas

## Design principles

1. **One Clarity experience.** Individual monitors are entry points into My Clarity, not isolated applications.
2. **Target reuse.** Public targets should be reusable across users where safe and appropriate.
3. **Evidence first.** A notification should always be explainable by stored observation/change history.
4. **No alert without meaning.** Operational failures and actual target changes are distinct concepts.
5. **Thin monitors.** New products should mostly add adapters, comparisons, defaults, and presentation.
6. **Low support.** Configuration should be understandable without professional setup.
7. **Privacy-aware.** Do not share private target data or snapshots across workspaces merely for deduplication.
8. **Cost-aware.** Cadence and retention are explicit so expensive monitoring cannot grow invisibly.
