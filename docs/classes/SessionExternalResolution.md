# SessionExternalResolution

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [ProjectSession.Models.cs](../../editor/Session/ProjectSession.Models.cs).
- **Declaration:** `public enum SessionExternalResolution`

How an explicit file reconciliation treats an unsaved live buffer.

## Values

| Name | Value | Contract |
| --- | ---: | --- |
| [Reject](#reject) | 0 | Refresh a clean buffer; reject a dirty buffer. |
| [UseDisk](#usedisk) | 1 | Replace live content with the current disk file, explicitly discarding unsaved changes. |
| [KeepLive](#keeplive) | 2 | Keep live content while acknowledging the current disk file as its new persisted base. |

## Enumeration descriptions

### Reject

Refresh a clean buffer; reject a dirty buffer.

### UseDisk

Replace live content with the current disk file, explicitly discarding unsaved changes.

### KeepLive

Keep live content while acknowledging the current disk file as its new persisted base.

## Usage and verification

Used by [SessionClient](SessionClient.md) and [SessionResult](SessionResult.md). See the [session example](ProjectSession.md#example) and [public consumer](../../tests/ProjectSession/Program.cs). Undefined document/reconciliation values reject; no enum starts a game loop, UI or transport. See [the component](../components/project-session.md) for owner/failure and platform boundaries.
