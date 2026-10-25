# SessionNode

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [ProjectSession.Models.cs](../../editor/Session/ProjectSession.Models.cs).
- **Declaration:** `public sealed record SessionNode(Guid ID, Guid ParentID, string Name, string TypeName, ulong Revision, bool CanEdit, Transform? Transform);`

An immutable description of one scene object; IDs survive session rename/reparent and recovery.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; generated equality, copy/with and Deconstruct use ordinary C# record semantics. Request construction does not validate; the shared owner validates on submission. Snapshots are copied data, never borrowed mutable runtime objects.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [ID](#id) | `Guid`; no constructor default | Stable object identity. |
| [ParentID](#parentid) | `Guid`; no constructor default | Parent identity, or empty for the root. |
| [Name](#name) | `string`; no constructor default | Current node name. |
| [TypeName](#typename) | `string`; no constructor default | Concrete CLR type identity. |
| [Revision](#revision) | `ulong`; no constructor default | Monotonic object revision. |
| [CanEdit](#canedit) | `bool`; no constructor default | Whether this root-owned object is editable in this slice. |
| [Transform](#transform) | ``; no constructor default | Local transform for an Entity; null for other nodes. |

## Property descriptions

### ID

Stable object identity.

### ParentID

Parent identity, or empty for the root.

### Name

Current node name.

### TypeName

Concrete CLR type identity.

### Revision

Monotonic object revision.

### CanEdit

Whether this root-owned object is editable in this slice.

### Transform

Local transform for an Entity; null for other nodes.

## Usage, validation and lifetime

See the [compiled operation consumer](../../tests/ProjectSession/Program.cs), [session example](ProjectSession.md#example) and [SessionClient](SessionClient.md) methods for creation/inspection/submission. IDs and expected tokens come from the current shared snapshots; undo/redo publishes fresh tokens. Arbitrary record copies do not mutate the owner. Conflicts expose SessionConflictException; invalid requests and unsupported roles fail before publication. The [component](../components/project-session.md) defines bounds, threading, persistence and actual verification limits. This is tooling API outside the runtime census.
