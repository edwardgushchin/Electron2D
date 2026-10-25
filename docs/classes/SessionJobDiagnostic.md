# SessionJobDiagnostic

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime.
- **Source:** [JobProtocol.cs](../../editor/Session/Jobs/JobProtocol.cs).
- **Declaration:** `public sealed record SessionJobDiagnostic(string Code, string Message, string? ExceptionType = null);`

A structured failure category, readable detail and optional exception type.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; equality, copy/with and Deconstruct use ordinary C# record semantics. Construction does not submit work or change the owner. Job submission copies option collections; published snapshot/result collections are read-only copied values.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [Code](#code) | `string`; `no constructor default` | Stable failure category. |
| [Message](#message) | `string`; `no constructor default` | Bounded readable detail. |
| [ExceptionType](#exceptiontype) | `string?`; `null` | Optional exception identity. |

## Property descriptions

### Code

Stable failure category.

### Message

Bounded readable detail.

### ExceptionType

Optional exception identity.

## Usage, validation and lifetime

See [shared jobs](../components/project-jobs.md), the [compiled consumer](../../tests/ProjectJobs/Program.cs) and [SessionClient.StartJob](SessionClient.md#startjob). Input tokens come from current shared observations; records do not validate themselves. Invalid timing/paths/profile/bounds reject explicitly. Results distinguish completion, cancellation and failure, preserving exact provenance and structured diagnostics. Snapshot staleness uses live session revisions; a copied result cannot relabel an artifact. These cold allocating tooling records do not change the runtime declaration census.
