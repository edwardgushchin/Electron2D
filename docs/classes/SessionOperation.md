# SessionOperation

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [ProjectSession.Models.cs](../../editor/Session/ProjectSession.Models.cs).
- **Declaration:** `public sealed record SessionOperation(Guid ID, string Label, string Origin, bool IsApplied);`

A copied retained history entry, usable after owner recovery.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; generated equality, copy/with and Deconstruct use ordinary C# record semantics. Request construction does not validate; the shared owner validates on submission. Snapshots are copied data, never borrowed mutable runtime objects.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [ID](#id) | `Guid`; no constructor default | Stable operation identity accepted by Undo/Redo. |
| [Label](#label) | `string`; no constructor default | Authored group label. |
| [Origin](#origin) | `string`; no constructor default | Original client attribution. |
| [IsApplied](#isapplied) | `bool`; no constructor default | Whether the group's forward change is currently applied. |

## Property descriptions

### ID

Stable operation identity accepted by Undo/Redo.

### Label

Authored group label.

### Origin

Original client attribution.

### IsApplied

Whether the group's forward change is currently applied.

## Usage, validation and lifetime

See the [compiled operation consumer](../../tests/ProjectSession/Program.cs), [session example](ProjectSession.md#example) and [SessionClient](SessionClient.md) methods for creation/inspection/submission. IDs and expected tokens come from the current shared snapshots; undo/redo publishes fresh tokens. Arbitrary record copies do not mutate the owner. Conflicts expose SessionConflictException; invalid requests and unsupported roles fail before publication. The [component](../components/project-session.md) defines bounds, threading, persistence and actual verification limits. This is tooling API outside the runtime census.
