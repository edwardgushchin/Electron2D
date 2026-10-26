# SessionAgentEventKind

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime assembly.
- **Source:** [SessionAgent.cs](../../editor/Session/Agents/SessionAgent.cs).
- **Declaration:** `public enum SessionAgentEventKind`

Event categories for bounded conversation and control feedback.

## Public members

| Signature | Contract |
| --- | --- |
| `Status = 0` | Connection, actual model or capability information. |
| `Message = 1` | Incremental literal assistant message. |
| `Activity = 2` | Actual executor/operation stage. |
| `Completed = 3` | Executor confirmed terminal completion; project/job checks remain authoritative. |
| `Interrupted = 4` | Executor confirmed interruption; committed edits remain. |
| `Failed = 5` | Failure or unsupported interaction requiring attention. |

## Contract and verification

Values use the declared zero-based order. The adapter emits them; constructing/casting an enum starts no process. Native real-agent checks observe completion, interruption and reconnect; deterministic checks cover unavailable executors. Undefined enum values do not grant capabilities.

See [the agent workflow](../components/project-agent.md) for protocol, examples, credentials, cancellation, evidence and platform limits. This tooling API changes no runtime coverage row.
