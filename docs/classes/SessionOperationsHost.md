# SessionOperationsHost

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime assembly.
- **Source:** [SessionOperationsHost.cs](../../editor/Session/Operations/SessionOperationsHost.cs).
- **Declaration:** `public sealed partial class SessionOperationsHost : IDisposable`

Publishes authenticated local operation ingress over an existing owner. Create it through ProjectSession.StartOperations; socket workers queue data and Pump alone invokes typed clients on the creating thread.

## Public members

| Signature | Contract |
| --- | --- |
| `Guid OwnerID { get; }` | Fresh endpoint generation; never reused by a new owner/host. |
| `Task Completion { get; }` | Socket-worker retirement after Dispose; await before finishing shutdown. |
| `int Pump(int maximum = 16)` | Attempt 1..128 queued requests; never wait for model/game IO. |
| `string GrantClient(string clientName, bool allowFileWrites = false, bool allowDiscard = false)` | Issue a secret named capability with explicit publication/discard rights. |
| `void CancelClientWork(string capability)` | Cancel undispatched requests/jobs and block later mutations while retaining reads/conversation/committed edits. |
| `void ResumeClientWork(string capability)` | Resume a still-valid stopped client for a new task after cancelling late queued work. |
| `void RevokeClient(string capability)` | Expire an issued capability, cancel its work and disconnect its clients. |
| `IReadOnlyList<SessionOperationActivity> DrainActivity()` | Drain the latest bounded owner-confirmed dispatch notices. |
| `void Dispose()` | Close endpoint/discovery and clients, cancel queued work/jobs, retain committed authoring. |

## Contract and verification

All control methods require the idle owner thread. Client names are nonblank, at most 64 characters and contain no controls; at most 128 capability tokens are retained. Tokens must stay private. Default discovery rights permit authoring/jobs, without Save or UseDisk. RevokeClient cannot expire the default token: dispose the host. One host per owner; matching canonical project/documents are required. Pump rejects invalid budgets and a disposed host. Dispose is idempotent and worker retirement is asynchronous. Discovery corruption is reported while ProjectSession retirement still releases its lease. Authentication, rights, queued stop/revoke, disconnect and recovery execute in tests/ProjectOperations.

See [the agent workflow](../components/project-agent.md) for protocol, examples, credentials, cancellation, evidence and platform limits. This tooling API changes no runtime coverage row.
