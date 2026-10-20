# Scene recovery connection internals

Last updated: 2026-10-21

- **Source:** [SceneConnectionBinding.cs](../../src/Scene/Resources/SceneConnectionBinding.cs), [Node.SceneConnections.cs](../../src/Scene/Main/Node.SceneConnections.cs).
- **Declarations:** internal sealed UnresolvedSceneConnectionSchema : SceneConnectionSchema; private sealed Node.RecordedConnectionLease : IDisposable.

## Description

UnresolvedSceneConnectionSchema is immutable cold authoring metadata for an unavailable registered adapter. It retains the portable ID, diagnostic event/method labels, persistent delivery flags and unbind count from a validated version-seven archive. Its endpoint role is Node; the surrounding loader permits this schema only with an unavailable endpoint or content omitted beneath an unavailable scene. Known ordinary endpoints still require a registered compatible adapter. PayloadDescriptor and Subscribe throw because this role cannot manufacture a compiled payload descriptor or callback.

SceneConnectionSchema.Restore retains a nonexecuting SceneConnectionRuntime when either endpoint is MissingNode. Registered adapters use their ordinary subscription factory once both actual compiled endpoints are available. Nonexecuting records use a null event token and are retired through both endpoint lists; they never impersonate a live EventConnection.

RecordedConnectionLease weakly references the retained runtime. Dispose removes one explicit authoring record on the endpoint owner thread and is idempotent. After endpoint retirement it neither retains the endpoint/resource graph nor attempts another mutation. A failing synchronous editor notification rolls back newly added records; removal remains committed when its notification throws.

## Lifecycle and invariants

These types are internal implementation details with no public constructors or direct user API. The endpoint lists own active retained records. Payload arrays are snapshots and Resource references are borrowed under ordinary scene ownership. Either endpoint disposal removes the record from both lists. Scene capture validates membership and serializes only selected persistent records. Counts, string labels, roles, payload schemas and references are bounded and validated at the archive boundary.

## Verification and limits

MissingNodeTests covers fresh-process unavailable-adapter retention/restoration, record flags, duplicate records, idempotent lease disposal, endpoint cleanup, authoring failure rollback and archive versions 3–7. SceneConnectionTests exercises ordinary executing subscription capture/rebind/one-shot/deferred/disposal behavior and actual software-rendered output. These are cold authoring operations; native allocation totals, hardware, foreign-platform and editor acceptance are separate.

## Decisions

[ADR 0023](../decisions/packed-scenes.md#adr-0023), [ADR 0013](../decisions/resources.md#adr-0013), [ADR 0002](../decisions/product.md#adr-0002).
