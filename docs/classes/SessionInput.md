# SessionInput

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime.
- **Source:** [SessionJob.Models.cs](../../editor/Session/Jobs/SessionJob.Models.cs).
- **Declaration:** `public sealed record SessionInput(int Step, string? Action = null, Key? Key = null, bool Pressed = true, float Strength = 1, bool Echo = false);`

An immutable input edge to inject before a one-based simulation step.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; equality, copy/with and Deconstruct use ordinary C# record semantics. Construction does not submit work or change the owner. Job submission copies option collections; published snapshot/result collections are read-only copied values.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [Step](#step) | `int`; `no constructor default` | One-based step inside the requested budget. |
| [Action](#action) | `string?`; `null` | Registered action, or null when Key is selected. |
| [Key](#key) | `Key?`; `null` | Typed key, or null when Action is selected. |
| [Pressed](#pressed) | `bool`; `true` | Press/release state. |
| [Strength](#strength) | `float`; `1` | Finite action strength from zero through one. |
| [Echo](#echo) | `bool`; `false` | Whether the key edge is an echo. |

## Property descriptions

### Step

One-based step inside the requested budget.

### Action

Registered action, or null when Key is selected.

### Key

Typed key, or null when Action is selected.

### Pressed

Press/release state.

### Strength

Finite action strength from zero through one.

### Echo

Whether the key edge is an echo.

## Usage, validation and lifetime

See [shared jobs](../components/project-jobs.md), the [compiled consumer](../../tests/ProjectJobs/Program.cs) and [SessionClient.StartJob](SessionClient.md#startjob). Input tokens come from current shared observations; records do not validate themselves. Invalid timing/paths/profile/bounds reject explicitly. Results distinguish completion, cancellation and failure, preserving exact provenance and structured diagnostics. Snapshot staleness uses live session revisions; a copied result cannot relabel an artifact. These cold allocating tooling records do not change the runtime declaration census.
