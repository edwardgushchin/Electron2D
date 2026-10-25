# SessionSnapshotInfo

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime.
- **Source:** [SessionJob.Models.cs](../../editor/Session/Jobs/SessionJob.Models.cs).
- **Declaration:** `public sealed record SessionSnapshotInfo(Guid ID, string SHA256, ulong SceneRevision, ulong CodeRevision, IReadOnlyList<SessionSnapshotFile> Files);`

Immutable provenance for one materialized project snapshot.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; equality, copy/with and Deconstruct use ordinary C# record semantics. Construction does not submit work or change the owner. Job submission copies option collections; published snapshot/result collections are read-only copied values.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [ID](#id) | `Guid`; `no constructor default` | Snapshot identity. |
| [SHA256](#sha256) | `string`; `no constructor default` | Digest of the complete ordered file inventory and document tokens. |
| [SceneRevision](#scenerevision) | `ulong`; `no constructor default` | Captured scene token. |
| [CodeRevision](#coderevision) | `ulong`; `no constructor default` | Captured source token. |
| [Files](#files) | `IReadOnlyList<SessionSnapshotFile>`; `no constructor default` | Read-only input inventory, including the copied engine/tooling/native SDK. |

## Property descriptions

### ID

Snapshot identity.

### SHA256

Digest of the complete ordered file inventory and document tokens.

### SceneRevision

Captured scene token.

### CodeRevision

Captured source token.

### Files

Read-only input inventory, including the copied engine/tooling/native SDK.

## Usage, validation and lifetime

See [shared jobs](../components/project-jobs.md), the [compiled consumer](../../tests/ProjectJobs/Program.cs) and [SessionClient.StartJob](SessionClient.md#startjob). Input tokens come from current shared observations; records do not validate themselves. Invalid timing/paths/profile/bounds reject explicitly. Results distinguish completion, cancellation and failure, preserving exact provenance and structured diagnostics. Snapshot staleness uses live session revisions; a copied result cannot relabel an artifact. These cold allocating tooling records do not change the runtime declaration census.
