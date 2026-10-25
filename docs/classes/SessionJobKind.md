# SessionJobKind

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [SessionJob.Models.cs](../../editor/Session/Jobs/SessionJob.Models.cs).
- **Declaration:** `public enum SessionJobKind`

The executable boundary requested from an immutable project snapshot.

## Enumeration values

| Value | Numeric identity | Contract |
| --- | ---: | --- |
| [Build](#build) | 0 | Compile the exact snapshot without activating gameplay. |
| [Run](#run) | 1 | Compile and execute a fresh headless or rendered process. |

## Enumeration descriptions

### Build

Compile the exact snapshot without activating gameplay.

### Run

Compile and execute a fresh headless or rendered process.

## Usage and validation

Used by [SessionJobOptions](SessionJobOptions.md), [SessionJob](SessionJob.md) and [SessionRunContext](SessionRunContext.md). Undefined values reject before launching work. Headless execution does not produce native images; Rendered requires the requested native backend. A running/queued status is not successful completion. See [shared jobs](../components/project-jobs.md) for lifecycle, failure, platform and acceptance limits.
