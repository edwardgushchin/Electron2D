# SessionJobOptions

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime.
- **Source:** [SessionJob.Models.cs](../../editor/Session/Jobs/SessionJob.Models.cs).
- **Declaration:** `public sealed record SessionJobOptions(SessionJobKind Kind, string ProjectFile, string SDKDirectory, SessionRunMode Mode = SessionRunMode.Headless, int Steps = 60, int FPS = 60, int Width = 320, int Height = 180, string Backend = "gpu", ulong Seed = 1, int DeadlineMilliseconds = 300000, IReadOnlyList<int>? CaptureSteps = null, IReadOnlyList<SessionInput>? Inputs = null);`

Bounded options for a build or fresh-process run; source and SDK inputs are frozen by the session.

## Constructor and properties

The positional constructor accepts the declared fields in order. Each field is a public get/init record property; equality, copy/with and Deconstruct use ordinary C# record semantics. Construction does not submit work or change the owner. Job submission copies option collections; published snapshot/result collections are read-only copied values.

| Property | Type/default | Meaning |
| --- | --- | --- |
| [Kind](#kind) | `SessionJobKind`; `no constructor default` | Build only or build and run. |
| [ProjectFile](#projectfile) | `string`; `no constructor default` | Project-relative single .NET executable project. |
| [SDKDirectory](#sdkdirectory) | `string`; `no constructor default` | Qualified managed SDK output with Electron2D.dll, this assembly and native dependencies. |
| [Mode](#mode) | `SessionRunMode`; `SessionRunMode.Headless` | Headless or actual native rendering for Run. |
| [Steps](#steps) | `int`; `60` | Maximum completed simulation steps. |
| [FPS](#fps) | `int`; `60` | Fixed process/physics cadence. |
| [Width](#width) | `int`; `320` | Rendered width. |
| [Height](#height) | `int`; `180` | Rendered height. |
| [Backend](#backend) | `string`; `"gpu"` | Explicit gpu or compatibility backend, without fallback. |
| [Seed](#seed) | `ulong`; `1` | Run-local random generator seed. |
| [DeadlineMilliseconds](#deadlinemilliseconds) | `int`; `300000` | Whole-job deadline, including snapshot/build/cleanup boundaries. |
| [CaptureSteps](#capturesteps) | `IReadOnlyList<int>?`; `null` | Requested completed rendered frames; copied on submission. |
| [Inputs](#inputs) | `IReadOnlyList<SessionInput>?`; `null` | Input edges; copied on submission. |

## Property descriptions

### Kind

Build only or build and run.

### ProjectFile

Project-relative single .NET executable project.

### SDKDirectory

Qualified managed SDK output with Electron2D.dll, this assembly and native dependencies.

### Mode

Headless or actual native rendering for Run.

### Steps

Maximum completed simulation steps.

### FPS

Fixed process/physics cadence.

### Width

Rendered width.

### Height

Rendered height.

### Backend

Explicit gpu or compatibility backend, without fallback.

### Seed

Run-local random generator seed.

### DeadlineMilliseconds

Whole-job deadline, including snapshot/build/cleanup boundaries.

### CaptureSteps

Requested completed rendered frames; copied on submission.

### Inputs

Input edges; copied on submission.

## Usage, validation and lifetime

See [shared jobs](../components/project-jobs.md), the [compiled consumer](../../tests/ProjectJobs/Program.cs) and [SessionClient.StartJob](SessionClient.md#startjob). Input tokens come from current shared observations; records do not validate themselves. Invalid timing/paths/profile/bounds reject explicitly. Results distinguish completion, cancellation and failure, preserving exact provenance and structured diagnostics. Snapshot staleness uses live session revisions; a copied result cannot relabel an artifact. These cold allocating tooling records do not change the runtime declaration census.
