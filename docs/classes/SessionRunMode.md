# SessionRunMode

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [JobProtocol.cs](../../editor/Session/Jobs/JobProtocol.cs).
- **Declaration:** `public enum SessionRunMode`

Selects the services exercised by a fresh immutable-snapshot scenario.

## Enumeration values

| Value | Numeric identity | Contract |
| --- | ---: | --- |
| [Headless](#headless) | 0 | Advances the ordinary scene lifecycle without native presentation. |
| [Rendered](#rendered) | 1 | Runs the ordinary native host with fixed movie timing and completed-frame capture. |

## Enumeration descriptions

### Headless

Advances the ordinary scene lifecycle without native presentation.

### Rendered

Runs the ordinary native host with fixed movie timing and completed-frame capture.

## Usage and validation

Used by [SessionJobOptions](SessionJobOptions.md), [SessionJob](SessionJob.md) and [SessionRunContext](SessionRunContext.md). Undefined values reject before launching work. Headless execution does not produce native images; Rendered requires the requested native backend. A running/queued status is not successful completion. See [shared jobs](../components/project-jobs.md) for lifecycle, failure, platform and acceptance limits.
