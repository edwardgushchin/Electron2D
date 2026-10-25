# SessionCodeEdit

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [ProjectSession.Models.cs](../../editor/Session/ProjectSession.Models.cs).
- **Declaration:** `public sealed record SessionCodeEdit(Guid DocumentID, ulong ExpectedRevision, string Text) : SessionEdit;`

Replaces the code buffer only at the exact observed document revision.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; generated equality, copy/with and Deconstruct use ordinary C# record semantics. Request construction does not validate; the shared owner validates on submission. Snapshots are copied data, never borrowed mutable runtime objects.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [DocumentID](#documentid) | `Guid`; no constructor default | Stable code-document identity. |
| [ExpectedRevision](#expectedrevision) | `ulong`; no constructor default | Observed revision. |
| [Text](#text) | `string`; no constructor default | Valid bounded UTF-8 source text. |

## Property descriptions

### DocumentID

Stable code-document identity.

### ExpectedRevision

Observed revision.

### Text

Valid bounded UTF-8 source text.

## Usage, validation and lifetime

See the [compiled operation consumer](../../tests/ProjectSession/Program.cs), [session example](ProjectSession.md#example) and [SessionClient](SessionClient.md) methods for creation/inspection/submission. IDs and expected tokens come from the current shared snapshots; undo/redo publishes fresh tokens. Arbitrary record copies do not mutate the owner. Conflicts expose SessionConflictException; invalid requests and unsupported roles fail before publication. The [component](../components/project-session.md) defines bounds, threading, persistence and actual verification limits. This is tooling API outside the runtime census.
