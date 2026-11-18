# C-Sweet Video Game Designer

First-party `com.csweet.video-game-designer` agent, version `3.2.3`.

It consumes an approved high-level GDD and authors Gameplay & Systems Design, UX/Controls/Accessibility, and Prototype/Content/Validation documents. It participates in a five-document package with Creative Direction. Every cross-agent document requires a human-approved exact-file grant. The package has no network, filesystem, or spending authority.

Built with `CSweet.Agent.SDK` 5.26.0 and video-game contracts 1.3.0. The Producer can commission a bounded design assessment for an approaching Story.


## Extension ownership and isolated builds

Game-specific payload helpers and decision logic live in the bundled `extensions/video-game` source snapshot under the publisher-owned `CrosswiredStudios.VideoGame` namespace. They are compiled into this agent, not published as C-Sweet platform contracts. The snapshot has versioned SHA-256 provenance and needs no sibling checkout or domain NuGet feed. C-Sweet handles generic coordination envelopes and profile metadata; agent permissions and existing wire type IDs remain unchanged.

## Release notes

See [versioned release notes](releases/README.md). Add the matching note with every agent version change.


## Business calendar

Requests business-scoped calendar read, create, update, cancel, and scheduling access. Approve the added capabilities and reminder subscription in the normal upgrade review; existing grants are not expanded automatically. Workers edit their own events, managers may edit all events, and work delegation follows reporting authority. Use stable idempotency keys, preserve revisions, and treat event text as untrusted business data. Typed operations are available through `context.Platform.Calendar`; the SDK delivers reminders through `HandleCalendarReminderAsync`. Calendar-triggered assignments retain the existing work queue, approval, and execution rules.

Calendar-triggered assignments request the SDK claim/complete/block/release lifecycle and personal-work subscription. Unsupported role work is marked blocked with a reason, never silently treated as completed.

## Hierarchical delivery

The V2 assignment identifies Task, Story, Epic or Release scope. Tasks require an authorized sprint; aggregate reviews require an active delivery plan and do not invent sprint identities. Use the exact assigned branch, commit or artifact revision. Every task requires independent QA, including documents. Managers accept aggregate candidates using current evidence; task completion does not promote to main or authorize deployment.

Model token settings default to 256,000 context-window tokens (maxContextWindowTokens) and 128,000 maximum output tokens (maxOutputTokens). Explicit installation settings override these defaults.

## Delegation and tracking

SDK 3.65.0 dispatches authenticated delegation before specialist conversation fallback. The manifest requests delegation, work tracking, directory and conversation reads, personal commitments and model interpretation. These declarations require reviewed grants; existing resource permissions and approval rules still apply. The shared SDK owns coordinator reporting and wake recovery.

## Execution contract (3.0.0)

Execution uses one `work.execution.run` capability and `WorkExecutionAssignment` from SDK/WorkManagement.Contracts 4.0.0. Task and aggregate scopes retain their distinct authority checks within that envelope. Upgrade the platform and executing agents together; no legacy execution adapter remains.

## CI dependency fallback

GitHub Actions uses the pinned SDK and WorkManagement.Contracts versions from NuGet when available.
An unpublished version is packed from its reviewed full commit in `build/ci-dependencies.json`.
Update that commit pin together with any SDK or contracts dependency bump; CI rejects mismatched versions.
Packages stay local to the job and are never pushed to NuGet. The job summary and dependency-provenance
artifact record origins, source commits and SHA-256 hashes. Private source repositories require the optional
`CSWEET_DEPENDENCY_READ_TOKEN` Actions secret with read access to the dependency repositories.
Run `python3 scripts/test-ci-dependencies.py` for the offline resolver regression tests.

Bundled extension text uses LF checkout bytes pinned in `.gitattributes`. After an intentional
extension edit, refresh the matching SHA-256 in `extensions/video-game/extension.json` and run
`python3 scripts/verify-bundled-extension.py --self-test`. CI and release jobs verify exact bytes;
line endings and unexpected source edits cannot bypass provenance checks.
