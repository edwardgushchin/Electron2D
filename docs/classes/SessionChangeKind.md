# SessionChangeKind

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [ProjectSession.Models.cs](../../editor/Session/ProjectSession.Models.cs).
- **Declaration:** `public enum SessionChangeKind`

A published session transition.

## Values

| Name | Value | Contract |
| --- | ---: | --- |
| [Applied](#applied) | 0 | A new grouped edit. |
| [Undone](#undone) | 1 | A guarded inverse of an earlier group. |
| [Redone](#redone) | 2 | A guarded replay of an earlier group. |
| [Saved](#saved) | 3 | One document's persistence boundary. |
| [Refreshed](#refreshed) | 4 | An explicitly reconciled external file. |
| [Selected](#selected) | 5 | Owner-local shared selection; no document/history change. |

## Enumeration descriptions

### Applied

A new grouped edit.

### Undone

A guarded inverse of an earlier group.

### Redone

A guarded replay of an earlier group.

### Saved

One document's persistence boundary.

### Refreshed

An explicitly reconciled external file.

### Selected

The shared selected stable object changed or was explicitly observed through a selection operation. Selection context is owner-local and is not persisted.

## Usage and verification

Used by [SessionClient](SessionClient.md) and [SessionResult](SessionResult.md). See the [session example](ProjectSession.md#example) and [public consumer](../../tests/ProjectSession/Program.cs). Undefined document/reconciliation values reject; no enum starts a game loop, UI or transport. See [the component](../components/project-session.md) for owner/failure and platform boundaries.
