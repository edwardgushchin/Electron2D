# SessionJobArtifact

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime.
- **Source:** [JobProtocol.cs](../../editor/Session/Jobs/JobProtocol.cs).
- **Declaration:** `public sealed record SessionJobArtifact(string Kind, string Path, int Step, [property: JsonPropertyName("sha256")] string SHA256);`

A completed artifact path, step and exact content digest.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; equality, copy/with and Deconstruct use ordinary C# record semantics. Construction does not submit work or change the owner. Job submission copies option collections; published snapshot/result collections are read-only copied values.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [Kind](#kind) | `string`; `no constructor default` | Image, assembly or log category. |
| [Path](#path) | `string`; `no constructor default` | Absolute owned artifact path. |
| [Step](#step) | `int`; `no constructor default` | Completed frame, or zero for nonframe output. |
| [SHA256](#sha256) | `string`; `no constructor default` | Exact lowercase content digest. |

## Property descriptions

### Kind

Image, assembly or log category.

### Path

Absolute owned artifact path.

### Step

Completed frame, or zero for nonframe output.

### SHA256

Exact lowercase content digest.

## Usage, validation and lifetime

See [shared jobs](../components/project-jobs.md), the [compiled consumer](../../tests/ProjectJobs/Program.cs) and [SessionClient.StartJob](SessionClient.md#startjob). Input tokens come from current shared observations; records do not validate themselves. Invalid timing/paths/profile/bounds reject explicitly. Results distinguish completion, cancellation and failure, preserving exact provenance and structured diagnostics. Snapshot staleness uses live session revisions; a copied result cannot relabel an artifact. These cold allocating tooling records do not change the runtime declaration census.
