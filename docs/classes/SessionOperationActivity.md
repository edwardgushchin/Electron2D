# SessionOperationActivity

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside the runtime assembly.
- **Source:** [SessionOperationsHost.cs](../../editor/Session/Operations/SessionOperationsHost.cs).
- **Declaration:** `public sealed record SessionOperationActivity(string Method, string Origin, bool Succeeded, string ErrorCode)`

Owner-confirmed operation progress without source, request arguments or private tokens. The latest 128 notices are retained until drained; document history remains separate.

## Public members

| Signature | Contract |
| --- | --- |
| `public sealed record SessionOperationActivity(string Method, string Origin, bool Succeeded, string ErrorCode)` | Positional constructor; each field is a public get/init property. Generated equality, copy/with and Deconstruct follow ordinary C# record semantics. |
| `string Method { get; init; }` | The dispatched operation name. |
| `string Origin { get; init; }` | Attributed external client or rejected actor name. |
| `bool Succeeded { get; init; }` | True only after dispatch returned a value. |
| `string ErrorCode { get; init; }` | Structured error code, empty on success. |

## Contract and verification

Copied bounded data. Construction alone changes no owner state and grants no capability. No runtime object, credential or mutable shared collection is returned. Validation occurs at the receiving operation boundary. The public transport and native consumers exercise these values.

See [the agent workflow](../components/project-agent.md) for protocol, examples, credentials, cancellation, evidence and platform limits. This tooling API changes no runtime coverage row.
