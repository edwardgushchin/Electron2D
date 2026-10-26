# SessionAgentStatus

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime assembly.
- **Source:** [SessionAgent.cs](../../editor/Session/Agents/SessionAgent.cs).
- **Declaration:** `public enum SessionAgentStatus`

Observable executor lifecycle; connection readiness is not model/task acceptance.

## Public members

| Signature | Contract |
| --- | --- |
| `Disconnected = 0` | No active connection. |
| `Connecting = 1` | Protocol, login and operation capabilities are being checked. |
| `Ready = 2` | Connected and able to accept a task. |
| `Running = 3` | Task active; no terminal success yet. |
| `Stopping = 4` | Interruption requested; acknowledgement pending. |
| `Failed = 5` | Connection or task failed. Inspect IsConnected and explicit diagnostics. |
| `Disposed = 6` | Adapter permanently retired. |

## Contract and verification

Values use the declared zero-based order. The adapter emits them; constructing/casting an enum starts no process. Native real-agent checks observe completion, interruption and reconnect; deterministic checks cover unavailable executors. Undefined enum values do not grant capabilities.

See [the agent workflow](../components/project-agent.md) for protocol, examples, credentials, cancellation, evidence and platform limits. This tooling API changes no runtime coverage row.
