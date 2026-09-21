# Electron2D scene decisions

Last updated: 2026-09-21

This bounded log owns the complete architectural records for scene. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0006](#adr-0006), [0008](#adr-0008), [0011](#adr-0011), [0023](#adr-0023).

<a id="adr-0006"></a>
## ADR 0006: Own hierarchy, deferred work, and queued deletion in SceneTree

Last updated: 2026-09-20

- Status: Accepted; frame scheduling refined by [0008](scene.md#adr-0008), lifecycle and queue safety refined by [0011](scene.md#adr-0011)
- Scope: `Node` hierarchy and `SceneTree` scheduling

### Context

Godot's object surface includes deferred calls and queued deletion, but both require a safe execution boundary and hierarchy ownership. Putting them on every `ElectronObject` would hide scheduling state and allow objects outside a scene to pretend that a game-loop queue exists.

### Decision

- `Node` owns ordered child relationships and lifecycle callbacks; `SceneTree` owns one active root hierarchy.
- SceneTree-managed enter runs parent-first, ready runs child-first once per node lifetime, and exit runs child-first. Manual notification dispatch remains possible but does not mutate lifecycle state.
- Attached hierarchy mutation, flushing, and disposal run on the thread that created the tree; invalid disposal is rejected before object state begins changing.
- `SceneTree.Defer(Action)` and `SetDeferred<T>` accept typed work without string method lookup.
- Core's `EventConnection` can receive `SceneTree.Defer` as its scheduler for cancellable deferred event delivery.
- A flush atomically captures one action batch. Work enqueued during execution waits for the next flush.
- Node deletion is a separate atomic request processed after deferred actions. It detaches and disposes the full subtree; it can be cancelled before processing.
- The application owns time and the safe point. It may call `ProcessFrame`, `PhysicsFrame`, or `FlushDeferred`; the frame methods run their callback lane and then flush. `SceneTree` does not create a hidden thread or clock.

### Consequences

- Ordering and thread affinity are explicit and testable.
- Deferred execution does not require `Callable`, reflection, `Variant`, or method names.
- Queueing is thread-safe, while game-state mutation remains single-threaded.
- Historical implementation note: queueing was originally not atomic with disposal. ADR 0011 replaced that behavior with a shared queue-lifetime lock.
- Cancelling deletion may leave a stale queue entry, which is ignored during flush.
- Historical implementation note: teardown originally stopped after some callback failures. ADR 0011 requires all owned cleanup stages to be attempted and failures to be aggregated.

### Rejected alternatives

- Put `CallDeferred` and `Free` on every `ElectronObject`: rejected because scheduling belongs to a tree/game-loop boundary.
- Execute each deferred action as soon as it is queued: rejected because it would not defer re-entrant mutation.
- Add a background scene thread: rejected because SDL and game-state ownership require an explicit application thread; the host drives the implemented frame methods.
- Use dynamic method names: rejected by ADR 0001.

<a id="adr-0008"></a>
## ADR 0008: Combine scene and 2D spatial behavior in one Node

Last updated: 2026-09-21

- Status: Accepted, except the rejection of a separate `Transform2D`, which is superseded by [ADR 0026](core-math.md#adr-0026)
- Scope: Scene-domain public object model

### Context

Godot separates non-spatial hierarchy/lifecycle behavior (`Node`) from 2D spatial behavior (`Node2D`, through `CanvasItem`). Electron2D is exclusively a 2D engine, and its intended game-object API needs both sets of behavior on ordinary nodes. Preserving a second spatial base class would add a hierarchy choice that has no 3D counterpart or non-spatial engine requirement here.

### Decision

- Electron2D exposes one public game-object class named `Node`; it does not expose `Node2D`.
- `Node` combines hierarchy, lifecycle, paths, groups, processing, deletion, 2D local/global transforms, visibility, and Z ordering.
- The current Node transform vocabulary uses `System.Numerics.Vector2` and `Matrix3x2` directly. The original decision not to introduce `Transform2D` is preserved here as history but superseded by ADR 0026; ADR 0029 implements the standalone value while leaving Node migration pending.
- Godot-like concepts keep recognizable names where they remain useful, but the API stays typed C#: strings represent paths/groups/names, delegates and virtual methods represent callbacks, and C# events represent signals.
- Renderer-independent canvas state (`Visible`, `ZIndex`, `ZAsRelative`) belongs on `Node` now. Renderer-bound drawing, materials, canvas handles, lights, clipping, input picking, and viewport behavior wait for their actual domains.
- `SceneTree` is the host-driven frame boundary. It delivers explicitly enabled process and physics-process callbacks in priority/tree order and flushes deferred work afterward; it does not create a hidden thread or clock.

### Consequences

- Every game object can be positioned immediately; users never choose between `Node` and `Node2D`.
- Scene and transform lifetime share one parent tree, making global transforms, inherited visibility, Z state, paths, groups, and processing coherent.
- The public API is intentionally similar rather than source-compatible with Godot: there is no Variant, NodePath, StringName, CanvasItem, or automatic method-name dispatch.
- `System.Numerics` fixes the current matrix convention and keeps Electron2D.dll free of another managed math dependency.
- Future renderer and input work extends `Node` or adds purpose-specific derived types; it must not recreate a parallel `Node2D` hierarchy.

### Rejected alternatives

- Keep separate `Node` and `Node2D`: rejected because the user-facing engine is 2D-only and requires spatial behavior on its single node type.
- Put transforms in a detachable component: rejected because it makes the primary 2D object more indirect without a demonstrated non-spatial use case.
- Create Electron2D-specific vector/matrix wrappers in this initial Node slice: rejected at the time because the .NET standard-library types covered the implemented behavior. ADR 0026 later required a complete standalone `Transform2D`, and ADR 0029 delivered it without silently changing Node's existing surface.
- Copy all `CanvasItem` API before a renderer exists: rejected because those members would be non-functional promises rather than a completed runtime contract.

<a id="adr-0011"></a>
## ADR 0011: Exception-safe SceneTree lifecycle, typed groups, and frame timers

Last updated: 2026-09-21

- Status: Accepted
- Scope: `SceneTree`, `SceneTreeTimer`, `GroupCallFlags`, and their `Node` lifecycle integration
- Refines: [0006](scene.md#adr-0006)

### Context

The first `SceneTree` version owned a hierarchy and offered frames, deferred actions, and queued node deletion, but constructor callback failures could leave partial membership, teardown failures could strand a live root behind a disposed tree, and cross-thread enqueue could race the one-time disposal clear. The stable reference surface also contains group operations, frame/tree signals, counts, generic queued deletion, and lightweight timers that do not require SDL, rendering, assets, networking, or an editor.

Electron2D must keep typed C# calls, deterministic ownership, Electron2D-owned code in its single public engine assembly, and explicit host-driven frame boundaries. It must not introduce string method/property dispatch or placeholder APIs for missing domains. ADR 0012 permits approved third-party dependencies to remain separate assemblies; it does not relax the one-assembly rule for Electron2D-owned code or change the `SceneTree` contract.

### Decision

- Queue acceptance and disposal closure share one lock. Work accepted before closure may be intentionally dropped by disposal; work reaching a closed tree is rejected and cannot become stranded.
- Constructor activation completes each lifecycle phase as far as possible. Any failure closes acceptance, terminally marks the failed tree, exits attached nodes, restores ready flags, disposes activation-created timers, clears queued work, and returns hierarchy ownership to the caller.
- Node enter, ready, exit, structural removal, node disposal, and tree disposal attempt all cleanup stages and aggregate callback failures after state reaches a coherent endpoint.
- Lifecycle snapshots revalidate membership; enter/ready/exit re-entry and child escape from an exiting or disposing parent's lifecycle, pre-delete, or cleanup callbacks are rejected.
- Frame and flush execution are non-reentrant and cannot begin during entry/exit delivery. Tree disposal from frame, flush, or lifecycle callbacks is rejected before the disposal transition.
- Cancellable node deletion continues through disposal after detach failures or detachment, while a stale request in an old tree cannot consume deletion now owned by a new tree.
- Pause traversal visits each still-attached node at most once; opposite re-entrant and teardown-time pause transitions are rejected.
- Node/tree/frame signals are typed C# events. Group calls and setters accept delegates; group ordering, deferral, and uniqueness use `GroupCallFlags`.
- `Unique` requires `Deferred`, uses operation kind/group/delegate-or-notification identity, ignores setter values, and retains the first accepted operation until its wrapper begins.
- `SceneTreeTimer` is a tree-owned, one-shot, auto-disposed timer updated after nodes and before deferred work in one selected frame lane.
- Node/group/frame counters and uncancellable `QueueDelete(ElectronObject)` are implemented because they require no absent domain.

### Consequences

- Lifecycle callback failures remain visible but no longer leave partial tree ownership, an operational escaped failed tree, or prevent later owned resources from being released.
- Cross-thread scheduling has a precise linearization point at the queue lock. The lock is intentionally small and never held while user code runs.
- Typed group operations require explicit delegates and therefore remain compile-time checked.
- Timers use delivered frame delta. ADR 0016 later adds Engine time scaling before delivery without adding a wall clock; timers still have no independent real-time or ignore-time-scale bypass.
- Scene switching, application quit, tween, interpolation, multiplayer, accessibility, editor signals, and platform notifications remain absent and explicitly dependency-blocked.

### Rejected alternatives

- Keep concurrent queues without a lifetime lock: rejected because a successful enqueue could remain permanently stranded after disposal.
- Swallow lifecycle failures: rejected because user callback failures must remain observable.
- Roll back arbitrary user mutations: rejected because external side effects are not reversible; rollback is limited to owned membership, ready state, and pending queues.
- Add reflection-based group method/property names: rejected by the typed API decision.
- Add placeholder scene, tween, networking, or platform members: rejected because they would advertise behavior without an owning domain.

<a id="adr-0023"></a>
## ADR 0023: Typed in-memory packed scenes

Last updated: 2026-09-21

### Status

Accepted.

- Refines: [0013: Managed typed Resource contract](resources.md#adr-0013)
- Extends: [0005: Notifications and typed editor properties](core-object-runtime.md#adr-0005)
- Preserves: [0010: Typed event connections](core-object-runtime.md#adr-0010)

### Context

Electron2D needs reusable 2D scene templates before it has an asset loader/saver or editor. The accepted architecture excludes dynamic values, reflection-driven string calls, public manual reference counting, hidden scene activation, and fictional APIs for absent domains. Existing `Node`, `PropertyDescriptor`, `Resource`, and deterministic disposal contracts already provide most required runtime mechanisms, but they did not yet define scene ownership, stored-property selection, derived node construction, local resource setup, or failure rollback.

ADR 0013 deliberately deferred automatic local-to-scene behavior and avoided a Resources-to-Scene dependency until a real consumer existed. This component is that consumer. The history in ADR 0013 remains intact; this decision narrows its deferred boundary instead of rewriting the earlier decision as if scene instancing had always existed.

### Decision

Add one runtime-only packed-scene component to the Scene domain and the existing `Electron2D.dll`:

- `PackedScene : Resource` stores one in-memory typed hierarchy and creates detached instances.
- `SceneState : ElectronObject` exposes a live read-only typed metadata view.
- `PackedSceneEditState` retains stable runtime/editor mode identities; only `Disabled` executes.

#### Capture and storage

`Node.Owner` is the storage-selection boundary. The root is always stored; traversal is parent-first depth-first, and only branches whose first descendant is owned by that root are included. The root does not own itself. Persistent group flags are captured; runtime-only groups are not.

Stored node state comes only from writable `PropertyDescriptor<TOwner, TValue>` instances explicitly marked `stored: true`. Strings, resources, and reference-free value types are accepted. Arbitrary objects, collections, delegates, node references, dynamic values, and reflection-discovered members are rejected. Names and hierarchy metadata have dedicated fields.

Derived node types opt in through `CreateSceneInstanceFactory()`. The factory must be static, outlive the source, and return a fresh default node of the exact source runtime type. Factory execution carries a context-local barrier that rejects both new-`SceneTree` construction and entry into an existing active tree before a node is returned. Capture stores the source identity; issuance rejects the source and any node already returned for that packed state.

`Pack(null)` preserves current state. For a non-null call, clearing occurs when capture begins; a later error intentionally leaves an empty packed scene. This follows the selected compatibility behavior and is documented rather than made transactionally different. Change notification occurs after the attempt and cannot roll back committed state.

#### Reconstruction and lifetime

Instantiation creates nodes parent-first, restores typed properties and persistent groups before parenting, assigns `Owner` after hierarchy construction, then handles scene-local resources. The result is detached and does not enter or become ready in a `SceneTree`.

The returned root owns the created hierarchy and all resource duplicates created for that instance. External non-local resources remain shared. Resource duplication preserves aliases and cycles; `GetLocalScene()` is assigned before each local setup callback. Setup occurs once per local duplicate before notification `20`. Only the root receives that notification after complete hierarchy/resource restoration.

Reconstruction validates topology before and after the final notification. An internal construction barrier prevents unfinished nodes from being disposed or entering any active tree, either as the root or as a child. Linear topology validation catches attachment to an unrelated detached hierarchy. Failure attempts to remove and dispose every returned node and resource duplicate acquired by the operation, then aggregates cleanup errors without disposing shared source resources. Factory-side allocations that are never returned cannot become engine-owned cleanup targets.

`SceneState` follows the current packed resource across replacement and path changes. A disposed state is replaced. An externally held state remains readable as the final snapshot after the source packed scene is disposed.

#### Dependency refinement

`PackedScene` lives in Scene and depends on Resources. To implement the established local-resource callback contract, `Resource.GetLocalScene()` now exposes the owning `Node`, so the Resource base has a narrow reciprocal dependency on the Scene node abstraction. This is an intentional in-assembly cycle, not a package or assembly cycle: both domains still ship in `Electron2D.dll`, and Resources does not depend on `SceneTree`, packed-scene internals, rendering, or editor code.

The dependency is restricted to local-scene association and is cleared on resource disposal. Broad asset code should not add more Scene dependencies without a later ADR.

#### Events and absent domains

Ordinary C# event subscribers and `EventConnection` tokens are runtime objects, not stored scene data. Persistent event connections remain deferred under ADR 0010 until a typed stable endpoint identity and handler-binding schema exists. No delegate inspection or reflection fallback is introduced.

There is no scene file loader/saver, import/UID remapping, editor, inheritance authoring, placeholders, editable instances, missing-resource recovery, or script serialization. The unsupported `PackedSceneEditState` values fail explicitly.

### Consequences

- Runtime code can construct reusable 2D scene templates without waiting for an editor or asset format.
- Stored state is explicit and compile-time typed; new fields are not serialized accidentally.
- Derived nodes need a small static factory override and stored descriptors for constructor-independent reconstruction.
- Source nodes can be disposed after packing. Stored shared resources retain their ordinary resource lifetime and may still be observed by later instances.
- Per-instance local resource graphs have deterministic root ownership and setup order.
- Scene capture and instantiation are allocation-heavy orchestration paths, not frame-loop primitives.
- The Resources↔Scene type dependency is real, documented, narrow, and contained inside one managed assembly.
- Future file serialization must consume this typed model or supersede it explicitly; it must not silently introduce dynamic values, reflection calls, or persistent delegate capture.

### Rejected alternatives

- Reflection over node properties: rejected because it cannot express storage, ownership, validation, or reference remapping safely.
- A dynamic value container or string `Get`/`Set`/`Call`: rejected by ADR 0001.
- Require parameterless constructors through runtime activation: rejected because it hides factory failures and relies on reflection.
- Capture live source nodes or instance-bound factory delegates: rejected because the snapshot must outlive source disposal.
- Copy all C# event subscribers: rejected because delegates do not provide stable serializable endpoint identity or ownership.
- Activate a new `SceneTree` inside `Instantiate()`: rejected because scene lifecycle and host ownership must remain explicit.
- Add loader/saver/editor placeholders now: rejected because no executable producer or consumer exists.
- Split packed scenes or resources into another managed assembly: rejected because the current product ships one Electron2D-owned DLL.

### Verification

The executable harness covers the current in-memory contract: empty and unsupported modes, capture selection/order, owner/path/group metadata, typed stored values, source disposal independence, repeated instances, local-resource aliasing/setup/ownership, live-state and path races, duplication, capture failure semantics, capture mutation rejection, factory closure/type/identity rejection, setup cleanup, detached-parent and active-tree escape rollback, and final snapshot survival. Repository verification also checks formatting, Release compilation, generated XML, documentation inventory, internal links, and the absence of prohibited production naming.

It does not establish disk format compatibility, editor behavior, performance on very large loaded scenes, platform asset packaging, persistent connections, script state, visual output, or owner acceptance.
