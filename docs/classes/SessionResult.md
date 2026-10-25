# SessionResult

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [ProjectSession.Models.cs](../../editor/Session/ProjectSession.Models.cs).
- **Declaration:** `public sealed record SessionResult(Guid OperationID, SessionChangeKind Kind, string Origin, ulong SceneRevision, ulong CodeRevision, bool FileWritten, IReadOnlyList<SessionDiagnostic> Diagnostics);`

The committed transition and resulting content revisions.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; generated equality, copy/with and Deconstruct use ordinary C# record semantics. Request construction does not validate; the shared owner validates on submission. Snapshots are copied data, never borrowed mutable runtime objects.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [OperationID](#operationid) | `Guid`; no constructor default | Stable history identity, or empty for a no-op/persistence transition. |
| [Kind](#kind) | `SessionChange`; no constructor default | Transition kind. |
| [Origin](#origin) | `string`; no constructor default | Client attribution. |
| [SceneRevision](#scenerevision) | `ulong`; no constructor default | Current scene revision. |
| [CodeRevision](#coderevision) | `ulong`; no constructor default | Current code revision. |
| [FileWritten](#filewritten) | `bool`; no constructor default | Whether this persistence call published the requested file. |
| [Diagnostics](#diagnostics) | `IReadOnlyList<SessionDiagnostic>`; no constructor default | Post-commit failures, if any. |

## Property descriptions

### OperationID

Stable history identity, or empty for a no-op/persistence transition.

### Kind

Transition kind.

### Origin

Client attribution.

### SceneRevision

Current scene revision.

### CodeRevision

Current code revision.

### FileWritten

Whether this persistence call published the requested file.

### Diagnostics

Post-commit failures, if any.

## Usage, validation and lifetime

See the [compiled operation consumer](../../tests/ProjectSession/Program.cs), [session example](ProjectSession.md#example) and [SessionClient](SessionClient.md) methods for creation/inspection/submission. IDs and expected tokens come from the current shared snapshots; undo/redo publishes fresh tokens. Arbitrary record copies do not mutate the owner. Conflicts expose SessionConflictException; invalid requests and unsupported roles fail before publication. The [component](../components/project-session.md) defines bounds, threading, persistence and actual verification limits. This is tooling API outside the runtime census.
