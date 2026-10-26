# SessionOperationsClient

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime assembly.
- **Source:** [SessionOperationsClient.cs](../../editor/Session/Operations/SessionOperationsClient.cs).
- **Declaration:** `public sealed class SessionOperationsClient : IAsyncDisposable`

Attaches to an explicitly existing owner; connection failure never creates a writable fallback.

## Public members

| Signature | Contract |
| --- | --- |
| `static Task<SessionOperationsClient> ConnectAsync(string projectRoot, string clientName, string? capability = null, CancellationToken cancellation = default)` | Read private discovery, connect to its generation and authenticate. Null capability uses default scoped rights. |
| `Task<JsonElement> InvokeAsync(string method, JsonElement arguments, CancellationToken cancellation = default)` | Call one advertised operation with its typed JSON schema; receive {value,error}. |
| `ValueTask DisposeAsync()` | Disconnect this client; pending work and its jobs are cancelled by the owner, committed edits remain. |

## Contract and verification

Calls serialize per connection. Authentication, unsafe/missing metadata and transport failures throw; semantic errors return bounded structured codes, including CONFLICT with IDs/tokens. Caller cancellation closes the framed connection to prevent response desynchronization. Inspect current history before retrying a mutation whose reply was lost. Methods do not call native widgets or acquire an owner lease. The existing owner must Pump. Linux Unix sockets are the current profile; no remote/TCP endpoint is exposed.

See [the agent workflow](../components/project-agent.md) for protocol, examples, credentials, cancellation, evidence and platform limits. This tooling API changes no runtime coverage row.
