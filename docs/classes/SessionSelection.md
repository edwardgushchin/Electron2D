# SessionSelection

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime assembly.
- **Source:** [ProjectSession.Selection.cs](../../editor/Session/ProjectSession.Selection.cs).
- **Declaration:** `public sealed record SessionSelection(ulong Revision, Guid NodeID)`

Shared context observed by the native tree, inspector and external operations. Selecting does not dirty the scene or create a history group. An absent selected object is cleared after scene replacement.

## Public members

| Signature | Contract |
| --- | --- |
| `public sealed record SessionSelection(ulong Revision, Guid NodeID)` | Positional constructor; each field is a public get/init property. Generated equality, copy/with and Deconstruct follow ordinary C# record semantics. |
| `ulong Revision { get; init; }` | Owner-local monotonic selection token; starts at one and is not persisted. |
| `Guid NodeID { get; init; }` | Stable selected scene object; empty clears selection. |

## Contract and verification

Copied bounded data. Construction alone changes no owner state and grants no capability. No runtime object, credential or mutable shared collection is returned. Validation occurs at the receiving operation boundary. The public transport and native consumers exercise these values.

See [the agent workflow](../components/project-agent.md) for protocol, examples, credentials, cancellation, evidence and platform limits. This tooling API changes no runtime coverage row.
