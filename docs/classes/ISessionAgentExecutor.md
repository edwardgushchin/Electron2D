# ISessionAgentExecutor

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime assembly.
- **Source:** [SessionAgent.cs](../../editor/Session/Agents/SessionAgent.cs).
- **Declaration:** `public interface ISessionAgentExecutor : IAsyncDisposable`

Replaceable conversation/control boundary. Engine semantics remain in the shared operations host; model/provider lifecycle remains outside Electron2D.dll.

## Public members

| Signature | Contract |
| --- | --- |
| `string Name { get; }` | Display executor identity. |
| `SessionAgentStatus Status { get; }` | Thread-safe lifecycle state. |
| `bool IsConnected { get; }` | Whether an authenticated conversation remains usable. |
| `string Model { get; }` | Actual selected model after connection; empty beforehand. |
| `string? ThreadID { get; }` | Conversation identity retained for reconnect. |
| `Task ConnectAsync(string projectRoot, string actor, string capability, CancellationToken cancellation = default)` | Connect using privately issued rights and verify actual protocol/tool support. |
| `Task SubmitAsync(string prompt, CancellationToken cancellation = default)` | Submit a task; acknowledgement does not establish terminal success. |
| `Task InterruptAsync(CancellationToken cancellation = default)` | Request turn interruption; observe a later terminal event. |
| `Task DisconnectAsync()` | Retire owned processes/pipes, retain conversation identity. |
| `IReadOnlyList<SessionAgentEvent> DrainEvents()` | Return at most 128 copied literal events without worker-thread UI callbacks. |
| `ValueTask DisposeAsync()` | Permanently disconnect/retire this adapter. |

## Contract and verification

A frontend polls state/events on its owner thread and alone updates controls. It must cancel/revoke operation rights when an executor disconnects or loses its protocol; an RPC failure can leave already committed authoring. Interrupt is complemented by SessionOperationsHost.CancelClientWork, and Disconnect by RevokeClient. Credentials belong to the chosen executor. Implementations must report missing capabilities, failures and actual completion explicitly. The real Codex adapter is exercised by the native co-development scenario; deterministic failure fixtures are not model evidence.

See [the agent workflow](../components/project-agent.md) for protocol, examples, credentials, cancellation, evidence and platform limits. This tooling API changes no runtime coverage row.
