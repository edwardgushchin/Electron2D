# SessionJobStatus

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [SessionJob.Models.cs](../../editor/Session/Jobs/SessionJob.Models.cs).
- **Declaration:** `public enum SessionJobStatus`

The lifecycle state of an owned project job.

## Enumeration values

| Value | Numeric identity | Contract |
| --- | ---: | --- |
| [Queued](#queued) | 0 | Captured authoring inputs await materialization. |
| [Running](#running) | 1 | Snapshot materialization, compilation or execution is in progress. |
| [Completed](#completed) | 2 | The requested work completed and its result was validated. |
| [Cancelled](#cancelled) | 3 | Cancellation retired the owned process group. |
| [Failed](#failed) | 4 | Snapshot, build, execution, deadline, validation or cleanup failed. |

## Enumeration descriptions

### Queued

Captured authoring inputs await materialization.

### Running

Snapshot materialization, compilation or execution is in progress.

### Completed

The requested work completed and its result was validated.

### Cancelled

Cancellation retired the owned process group.

### Failed

Snapshot, build, execution, deadline, validation or cleanup failed.

## Usage and validation

Used by [SessionJobOptions](SessionJobOptions.md), [SessionJob](SessionJob.md) and [SessionRunContext](SessionRunContext.md). Undefined values reject before launching work. Headless execution does not produce native images; Rendered requires the requested native backend. A running/queued status is not successful completion. See [shared jobs](../components/project-jobs.md) for lifecycle, failure, platform and acceptance limits.
