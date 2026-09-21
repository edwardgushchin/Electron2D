# ADR 0023: Typed in-memory packed scenes

Last updated: 2026-09-21

## Status

Accepted.

- Refines: [0013: Managed typed Resource contract](0013-managed-resource-contract.md)
- Extends: [0005: Notifications and typed editor properties](0005-notifications-and-typed-properties.md)
- Preserves: [0010: Typed event connections](0010-typed-event-connections.md)

## Context

Electron2D needs reusable 2D scene templates before it has an asset loader/saver or editor. The accepted architecture excludes dynamic values, reflection-driven string calls, public manual reference counting, hidden scene activation, and fictional APIs for absent domains. Existing `Node`, `PropertyDescriptor`, `Resource`, and deterministic disposal contracts already provide most required runtime mechanisms, but they did not yet define scene ownership, stored-property selection, derived node construction, local resource setup, or failure rollback.

ADR 0013 deliberately deferred automatic local-to-scene behavior and avoided a Resources-to-Scene dependency until a real consumer existed. This component is that consumer. The history in ADR 0013 remains intact; this decision narrows its deferred boundary instead of rewriting the earlier decision as if scene instancing had always existed.

## Decision

Add one runtime-only packed-scene component to the Scene domain and the existing `Electron2D.dll`:

- `PackedScene : Resource` stores one in-memory typed hierarchy and creates detached instances.
- `SceneState : ElectronObject` exposes a live read-only typed metadata view.
- `PackedSceneEditState` retains stable runtime/editor mode identities; only `Disabled` executes.

### Capture and storage

`Node.Owner` is the storage-selection boundary. The root is always stored; traversal is parent-first depth-first, and only branches whose first descendant is owned by that root are included. The root does not own itself. Persistent group flags are captured; runtime-only groups are not.

Stored node state comes only from writable `PropertyDescriptor<TOwner, TValue>` instances explicitly marked `stored: true`. Strings, resources, and reference-free value types are accepted. Arbitrary objects, collections, delegates, node references, dynamic values, and reflection-discovered members are rejected. Names and hierarchy metadata have dedicated fields.

Derived node types opt in through `CreateSceneInstanceFactory()`. The factory must be static, outlive the source, and return a fresh default node of the exact source runtime type. Factory execution carries a context-local barrier that rejects both new-`SceneTree` construction and entry into an existing active tree before a node is returned. Capture stores the source identity; issuance rejects the source and any node already returned for that packed state.

`Pack(null)` preserves current state. For a non-null call, clearing occurs when capture begins; a later error intentionally leaves an empty packed scene. This follows the selected compatibility behavior and is documented rather than made transactionally different. Change notification occurs after the attempt and cannot roll back committed state.

### Reconstruction and lifetime

Instantiation creates nodes parent-first, restores typed properties and persistent groups before parenting, assigns `Owner` after hierarchy construction, then handles scene-local resources. The result is detached and does not enter or become ready in a `SceneTree`.

The returned root owns the created hierarchy and all resource duplicates created for that instance. External non-local resources remain shared. Resource duplication preserves aliases and cycles; `GetLocalScene()` is assigned before each local setup callback. Setup occurs once per local duplicate before notification `20`. Only the root receives that notification after complete hierarchy/resource restoration.

Reconstruction validates topology before and after the final notification. An internal construction barrier prevents unfinished nodes from being disposed or entering any active tree, either as the root or as a child. Linear topology validation catches attachment to an unrelated detached hierarchy. Failure attempts to remove and dispose every returned node and resource duplicate acquired by the operation, then aggregates cleanup errors without disposing shared source resources. Factory-side allocations that are never returned cannot become engine-owned cleanup targets.

`SceneState` follows the current packed resource across replacement and path changes. A disposed state is replaced. An externally held state remains readable as the final snapshot after the source packed scene is disposed.

### Dependency refinement

`PackedScene` lives in Scene and depends on Resources. To implement the established local-resource callback contract, `Resource.GetLocalScene()` now exposes the owning `Node`, so the Resource base has a narrow reciprocal dependency on the Scene node abstraction. This is an intentional in-assembly cycle, not a package or assembly cycle: both domains still ship in `Electron2D.dll`, and Resources does not depend on `SceneTree`, packed-scene internals, rendering, or editor code.

The dependency is restricted to local-scene association and is cleared on resource disposal. Broad asset code should not add more Scene dependencies without a later ADR.

### Events and absent domains

Ordinary C# event subscribers and `EventConnection` tokens are runtime objects, not stored scene data. Persistent event connections remain deferred under ADR 0010 until a typed stable endpoint identity and handler-binding schema exists. No delegate inspection or reflection fallback is introduced.

There is no scene file loader/saver, import/UID remapping, editor, inheritance authoring, placeholders, editable instances, missing-resource recovery, or script serialization. The unsupported `PackedSceneEditState` values fail explicitly.

## Consequences

- Runtime code can construct reusable 2D scene templates without waiting for an editor or asset format.
- Stored state is explicit and compile-time typed; new fields are not serialized accidentally.
- Derived nodes need a small static factory override and stored descriptors for constructor-independent reconstruction.
- Source nodes can be disposed after packing. Stored shared resources retain their ordinary resource lifetime and may still be observed by later instances.
- Per-instance local resource graphs have deterministic root ownership and setup order.
- Scene capture and instantiation are allocation-heavy orchestration paths, not frame-loop primitives.
- The Resources↔Scene type dependency is real, documented, narrow, and contained inside one managed assembly.
- Future file serialization must consume this typed model or supersede it explicitly; it must not silently introduce dynamic values, reflection calls, or persistent delegate capture.

## Rejected alternatives

- Reflection over node properties: rejected because it cannot express storage, ownership, validation, or reference remapping safely.
- A dynamic value container or string `Get`/`Set`/`Call`: rejected by ADR 0001.
- Require parameterless constructors through runtime activation: rejected because it hides factory failures and relies on reflection.
- Capture live source nodes or instance-bound factory delegates: rejected because the snapshot must outlive source disposal.
- Copy all C# event subscribers: rejected because delegates do not provide stable serializable endpoint identity or ownership.
- Activate a new `SceneTree` inside `Instantiate()`: rejected because scene lifecycle and host ownership must remain explicit.
- Add loader/saver/editor placeholders now: rejected because no executable producer or consumer exists.
- Split packed scenes or resources into another managed assembly: rejected because the current product ships one Electron2D-owned DLL.

## Verification

The executable harness covers the current in-memory contract: empty and unsupported modes, capture selection/order, owner/path/group metadata, typed stored values, source disposal independence, repeated instances, local-resource aliasing/setup/ownership, live-state and path races, duplication, capture failure semantics, capture mutation rejection, factory closure/type/identity rejection, setup cleanup, detached-parent and active-tree escape rollback, and final snapshot survival. Repository verification also checks formatting, Release compilation, generated XML, documentation inventory, internal links, and the absence of prohibited production naming.

It does not establish disk format compatibility, editor behavior, performance on very large loaded scenes, platform asset packaging, persistent connections, script state, visual output, or owner acceptance.
