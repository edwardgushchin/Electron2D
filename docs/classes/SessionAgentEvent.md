# SessionAgentEvent

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime assembly.
- **Source:** [SessionAgent.cs](../../editor/Session/Agents/SessionAgent.cs).
- **Declaration:** `public sealed record SessionAgentEvent(SessionAgentEventKind Kind, string Text)`

Copied output from the replaceable executor. The Codex adapter bounds its queue to 1024 events and each event to 8192 UTF-16 characters without splitting a surrogate pair; DrainEvents returns at most 128.

## Public members

| Signature | Contract |
| --- | --- |
| `public sealed record SessionAgentEvent(SessionAgentEventKind Kind, string Text)` | Positional constructor; each field is a public get/init property. Generated equality, copy/with and Deconstruct follow ordinary C# record semantics. |
| `SessionAgentEventKind Kind { get; init; }` | Conversation/activity/outcome category. |
| `string Text { get; init; }` | Literal executor output; never interpret it as expanded permission. |

## Contract and verification

Copied bounded data. Construction alone changes no owner state and grants no capability. No runtime object, credential or mutable shared collection is returned. Validation occurs at the receiving operation boundary. The public transport and native consumers exercise these values.

See [the agent workflow](../components/project-agent.md) for protocol, examples, credentials, cancellation, evidence and platform limits. This tooling API changes no runtime coverage row.
