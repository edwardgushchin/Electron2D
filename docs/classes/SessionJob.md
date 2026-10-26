# SessionJob

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [SessionJob.cs](../../editor/Session/Jobs/SessionJob.cs).
- **Declaration:** `public sealed class SessionJob : IAsyncDisposable`

Owns one immutable build/run launched by SessionClient.StartJob. Construction is internal. Submission freezes unsaved document values, then materialization/build/execution proceeds outside the authoring thread. [The component](../components/project-jobs.md) specifies exact provenance, dependency/filesystem bounds, process ownership, cancellation and Linux-only capability. Trusted C#/MSBuild executes with ordinary host permissions.

## Example

```csharp
var scene = client.InspectScene();
var code = client.InspectCode();
await using var job = client.StartJob(
    new SessionJobOptions(SessionJobKind.Run, project.ProjectFile, sdkDirectory, Steps: 2),
    scene.Revision, code.Revision);
var result = await job.Completion;
```

Partial snippet: client/project/sdkDirectory come from an open owner and qualified SDK. Awaiting must not resume subsequent session operations on a different thread; the UI polls Completion on its owner instead. Job polling/cleanup alone is thread-safe.

## Public members

| Signature | Contract |
| --- | --- |
| `public Guid ID { get; }` | Unique job identity. |
| `public Guid SnapshotID { get; }` | Immutable input identity allocated at submission. |
| `public string OutputPath { get; }` | Owned generated directory. |
| `public SessionJobStatus Status { get; }` | Thread-safe lifecycle state. |
| `public string Stage { get; }` | Thread-safe readable current stage. |
| `public SessionSnapshotInfo? Snapshot { get; }` | Frozen provenance after materialization. |
| `public Task<SessionJobResult> Completion { get; }` | Terminal validated outcome. |
| `public bool IsStale { get; }` | Compare captured document tokens to the live owner. |
| `public void Cancel()` | Request owned process retirement. |
| `public ValueTask DisposeAsync()` | Cancel and await retirement. |
| `public void RemoveResult()` | Delete this terminal job's generated data and unregister its shared handle. |

## Property descriptions

### ID

Fresh GUID. Result directories and private process reports carry this identity.

### SnapshotID

Fresh GUID distinct from job/document IDs. Snapshot SHA-256 is available only after materialization; assigning an ID is not successful preparation.

### OutputPath

Absolute .electron2d/jobs/GUID directory inside the source project. It contains input/build payloads, manifests, request, bounded logs and structured results. It is owner-only on Unix and remains until explicit removal.

### Status

Queued, Running, Completed, Cancelled or Failed. Only Completed establishes validated requested work. Terminal state follows process/log/result retirement, not merely child exit.

### Stage

Readable progress such as Freezing inputs, Building snapshot, Running snapshot and terminal status. It is informational, not a stable machine protocol.

### Snapshot

Null before successful materialization. Otherwise read-only copied file/hash inventory and document tokens. Later owner edits cannot mutate it.

### Completion

Task of SessionJobResult. Ordinary snapshot/build/run/timeout/cancel failures become explicit result states/diagnostics; inspect ExitCode and Status. ReportPath names result JSON; RESULT_WRITE_FAILED states that durable output could not be confirmed. Logs remain separate artifacts.

### IsStale

Requires the live, nonfaulted session owner thread. Drains pending local source, then compares scene/code revisions. Edits, undo/redo, saves and reconciliation can advance tokens even if visible content later matches. Wrong-thread/closed/faulted access rejects rather than guessing. Persisted provenance remains available after owner closure.

## Method descriptions

### Cancel

Thread-safe idempotent request. No effect after disposal. Cooperative snapshot boundaries and owned Linux process-group termination retire work; this does not block for completion.

### DisposeAsync

Requests cancellation and awaits terminal process/log cleanup, then releases cancellation resources. Repeated calls are safe. It does not remove evidence; keep the job/result if needed. The session's Dispose requests cancellation but cannot synchronously await a blocked callback, so the job caller still awaits this method.

### RemoveResult

Successful removal also unregisters this live-owner handle from SessionClient.InspectJobs. If its generated directory is already absent, the handle is still unregistered. A failed ownership/path check leaves the shared handle available for diagnosis.

Requires Completion.IsCompleted, otherwise InvalidOperationException. Checks inside-root non-symlink path and exact job.owner marker before recursive deletion. Missing output is a no-op; changed ownership/unsafe paths reject. Only generated input/build/results are removed; source project documents are preserved. References to removed artifact paths cease to be usable. DisposeAsync remains the separate resource-retirement operation.

## Verification and limitations

[Public job contracts](../../tests/ProjectJobs/JobContracts.cs) exercise build/run failure, invalid input/profile, frozen input mutation, cancel, deadline, owner death, descendant cleanup, unrelated-process preservation and recovered unsaved state. [Native UI](../../tests/ProjectWorkspace/Program.cs) exercises actual captures and stale display/clearing. Deliberately detached hostile processes, other-platform job owners, physical GPU/performance/native heap and actual agent/owner acceptance are not established.
