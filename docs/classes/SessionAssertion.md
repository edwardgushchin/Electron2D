# SessionAssertion

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime.
- **Source:** [JobProtocol.cs](../../editor/Session/Jobs/JobProtocol.cs).
- **Declaration:** `public sealed record SessionAssertion(int Step, string Name, bool Passed, string Message);`

One completed-step assertion and its bounded explanation.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; equality, copy/with and Deconstruct use ordinary C# record semantics. Construction does not submit work or change the owner. Job submission copies option collections; published snapshot/result collections are read-only copied values.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [Step](#step) | `int`; `no constructor default` | One-based step or zero during configuration. |
| [Name](#name) | `string`; `no constructor default` | Stable bounded assertion name. |
| [Passed](#passed) | `bool`; `no constructor default` | Whether the condition passed. |
| [Message](#message) | `string`; `no constructor default` | Bounded explanation. |

## Property descriptions

### Step

One-based step or zero during configuration.

### Name

Stable bounded assertion name.

### Passed

Whether the condition passed.

### Message

Bounded explanation.

## Usage, validation and lifetime

See [shared jobs](../components/project-jobs.md), the [compiled consumer](../../tests/ProjectJobs/Program.cs) and [SessionClient.StartJob](SessionClient.md#startjob). Input tokens come from current shared observations; records do not validate themselves. Invalid timing/paths/profile/bounds reject explicitly. Results distinguish completion, cancellation and failure, preserving exact provenance and structured diagnostics. Snapshot staleness uses live session revisions; a copied result cannot relabel an artifact. These cold allocating tooling records do not change the runtime declaration census.
