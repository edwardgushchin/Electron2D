# SessionScene

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [ProjectSession.Models.cs](../../editor/Session/ProjectSession.Models.cs).
- **Declaration:** `public sealed record SessionScene(Guid ID, string Path, ulong Revision, bool IsDirty, bool HasExternalChange, IReadOnlyList<SessionNode> Nodes);`

A copied scene observation with no mutable runtime node references.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; generated equality, copy/with and Deconstruct use ordinary C# record semantics. Request construction does not validate; the shared owner validates on submission. Snapshots are copied data, never borrowed mutable runtime objects.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [ID](#id) | `Guid`; no constructor default | Stable document identity. |
| [Path](#path) | `string`; no constructor default | Project-relative path. |
| [Revision](#revision) | `ulong`; no constructor default | Monotonic content revision. |
| [IsDirty](#isdirty) | `bool`; no constructor default | Whether the live document differs from its persisted base. |
| [HasExternalChange](#hasexternalchange) | `bool`; no constructor default | Whether the disk fingerprint differs from the persisted base. |
| [Nodes](#nodes) | `IReadOnlyList<SessionNode>`; no constructor default | Read-only copied node descriptions. |

## Property descriptions

### ID

Stable document identity.

### Path

Project-relative path.

### Revision

Monotonic content revision.

### IsDirty

Whether the live document differs from its persisted base.

### HasExternalChange

Whether the disk fingerprint differs from the persisted base.

### Nodes

Read-only copied node descriptions.

## Usage, validation and lifetime

See the [compiled operation consumer](../../tests/ProjectSession/Program.cs), [session example](ProjectSession.md#example) and [SessionClient](SessionClient.md) methods for creation/inspection/submission. IDs and expected tokens come from the current shared snapshots; undo/redo publishes fresh tokens. Arbitrary record copies do not mutate the owner. Conflicts expose SessionConflictException; invalid requests and unsupported roles fail before publication. The [component](../components/project-session.md) defines bounds, threading, persistence and actual verification limits. This is tooling API outside the runtime census.
