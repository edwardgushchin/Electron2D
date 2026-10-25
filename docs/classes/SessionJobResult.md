# SessionJobResult

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime.
- **Source:** [SessionJob.Models.cs](../../editor/Session/Jobs/SessionJob.Models.cs).
- **Declaration:** `public sealed record SessionJobResult(Guid ID, SessionSnapshotInfo? Snapshot, SessionJobStatus Status, int ExitCode, string ReportPath, IReadOnlyList<SessionJobArtifact> Artifacts, IReadOnlyList<SessionJobDiagnostic> Diagnostics, IReadOnlyList<SessionObservation> Observations, IReadOnlyList<SessionAssertion> Assertions);`

The terminal, attributed outcome of an owned snapshot job.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; equality, copy/with and Deconstruct use ordinary C# record semantics. Construction does not submit work or change the owner. Job submission copies option collections; published snapshot/result collections are read-only copied values.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [ID](#id) | `Guid`; `no constructor default` | Job identity. |
| [Snapshot](#snapshot) | `SessionSnapshotInfo?`; `no constructor default` | Materialized immutable input, or null when snapshot preparation failed. |
| [Status](#status) | `SessionJobStatus`; `no constructor default` | Terminal completion, cancellation or failure. |
| [ExitCode](#exitcode) | `int`; `no constructor default` | Zero only for validated completion. |
| [ReportPath](#reportpath) | `string`; `no constructor default` | Durable result JSON, separate from program output logs. |
| [Artifacts](#artifacts) | `IReadOnlyList<SessionJobArtifact>`; `no constructor default` | Validated artifact identities. |
| [Diagnostics](#diagnostics) | `IReadOnlyList<SessionJobDiagnostic>`; `no constructor default` | Failure and cleanup details. |
| [Observations](#observations) | `IReadOnlyList<SessionObservation>`; `no constructor default` | Copied typed scenario observations. |
| [Assertions](#assertions) | `IReadOnlyList<SessionAssertion>`; `no constructor default` | Copied assertions, including retained failures. |

## Property descriptions

### ID

Job identity.

### Snapshot

Materialized immutable input, or null when snapshot preparation failed.

### Status

Terminal completion, cancellation or failure.

### ExitCode

Zero only for validated completion.

### ReportPath

Durable result JSON, separate from program output logs.

### Artifacts

Validated artifact identities.

### Diagnostics

Failure and cleanup details.

### Observations

Copied typed scenario observations.

### Assertions

Copied assertions, including retained failures.

## Usage, validation and lifetime

See [shared jobs](../components/project-jobs.md), the [compiled consumer](../../tests/ProjectJobs/Program.cs) and [SessionClient.StartJob](SessionClient.md#startjob). Input tokens come from current shared observations; records do not validate themselves. Invalid timing/paths/profile/bounds reject explicitly. Results distinguish completion, cancellation and failure, preserving exact provenance and structured diagnostics. Snapshot staleness uses live session revisions; a copied result cannot relabel an artifact. These cold allocating tooling records do not change the runtime declaration census.
