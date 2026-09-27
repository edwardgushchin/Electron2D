# Electron2D scene decisions

Last updated: 2026-09-27

This bounded document owns the current architectural decisions for scene. Node is the neutral scene-tree base and Entity is the spatial canvas base under ADR 0008; current class pages describe the implemented API. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0006](#adr-0006), [0008](#adr-0008), [0011](#adr-0011), [0023](#adr-0023), [0031](#adr-0031), [0036](#adr-0036), [0037](#adr-0037).

<a id="adr-0006"></a>
## ADR 0006: Own hierarchy, deferred work, and queued deletion in SceneTree

Last updated: 2026-09-24

- Status: Accepted; frame scheduling refined by [0008](scene.md#adr-0008), lifecycle and queue safety refined by [0011](scene.md#adr-0011)
- Scope: `Node` hierarchy and `SceneTree` scheduling

### Context

Godot's object surface includes deferred calls and queued deletion, but both require a safe execution boundary and hierarchy ownership. Putting them on every `ElectronObject` would hide scheduling state and allow objects outside a scene to pretend that a game-loop queue exists.

### Decision

- `Node` owns ordered child relationships and lifecycle callbacks; `SceneTree` owns one active root hierarchy.
- `SceneTree.Root` stays stable for the tree lifetime. `CurrentScene` selects one optional direct child of that root; assigning it does not reparent nodes. `ChangeSceneToNode` accepts a detached scene, removes the selected old scene immediately, and installs the new scene at the next deferred safe point. `ChangeSceneToPacked` instantiates before replacement. The tree owns accepted pending scenes, disposes superseded and old scenes before entry, and releases pending scenes during finalization. `SceneChanged` follows successful entry. `UnloadCurrentScene` disposes only the selected child. File-based change and reload await a scene loader.
- SceneTree-managed enter runs parent-first, ready runs child-first once per node lifetime, and exit runs child-first. Manual notification dispatch remains possible but does not mutate lifecycle state.
- Attached hierarchy mutation, flushing, and disposal run on the thread that created the tree; invalid disposal is rejected before object state begins changing.
- `SceneTree.Defer(Action)` and `SetDeferred<T>` accept typed work without string method lookup.
- Core's `EventConnection` can receive `SceneTree.Defer` as its scheduler for cancellable deferred event delivery.
- A flush atomically captures one action batch. Work enqueued during execution waits for the next flush.
- Node deletion is a separate atomic request processed after deferred actions. It detaches and disposes the full subtree; it can be cancelled before processing.
- The normal `Engine.Run` entry point owns time and the safe point. An embedding application may instead call `ProcessFrame`, `PhysicsFrame`, or `FlushDeferred`; the frame methods run their callback lane and then flush. `SceneTree` does not create a hidden thread or clock.

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
## ADR 0008: Preserve scene inheritance with Node and Entity names

Last updated: 2026-09-24

- Status: Accepted by the user on 2026-09-23.
- Scope: Scene inheritance, type naming, and preservation of the corresponding API and responsibilities.

### Decision

**Godot `Node` corresponds to Electron2D `Node`, with the same applicable API and behavior. Godot `Node2D` corresponds to Electron2D `Entity`, with the same applicable API and behavior.** These are type renames, not permission to combine responsibilities, remove members, or design a reduced substitute. The accepted typed C# and 2D adaptations continue to apply under [ADR 0004](product.md#adr-0004).

| Godot type | Electron2D type | Required responsibility |
| --- | --- | --- |
| `Object` | `ElectronObject` | Typed object identity, events, notifications and lifetime under the existing Core ADRs. |
| `Node : Object` | `Node : ElectronObject` | General tree membership, parent/children, ownership, paths/groups, lifecycle, processing and input. No canvas or spatial API is introduced here. |
| `CanvasItem : Node` | `abstract CanvasItem : Node` | Canvas drawing, visibility, modulation, materials, draw order and the common transform-query/notification contract. Being a CanvasItem does not require emitting visible geometry. |
| `Node2D : CanvasItem` | `Entity : CanvasItem` | Concrete spatial transform model: local/global position, rotation, scale, skew and spatial helpers. It can serve as an empty spatial parent. |
| `Sprite2D : Node2D` | `Sprite : Entity` | Texture drawing and sprite-specific state. |
| `Path2D : Node2D` | `Path : Entity` | Borrowed spatial curve and updates to attached direct followers. |
| `PathFollow2D : Node2D` | `PathFollow : Entity` | Distance/ratio sampling, offsets and tangent rotation for descendant nodes. |
| `Camera2D : Node2D` | `Camera : Entity` | Viewport camera selection and spatial tracking, including zoom, limits, drag margins and smoothing. |
| `CollisionShape2D : Node2D` | `CollisionShape : Entity` | Borrowed collision-shape placement as a direct physics-body child; first executable profile under ADR 0054. |
| `CollisionPolygon2D : Node2D` | `CollisionPolygon : Entity` | Owned solid or hollow polygon placement as a direct physics-body or Area child under ADR 0066. |
| `RayCast2D : Node2D` | `RayCast : Entity` | Spatial ray node with cached fixed-physics query state over the shared World2D under ADR 0063. |
| `ShapeCast2D : Node2D` | `ShapeCast : Entity` | Spatial shape sweep with cached contact results over the shared World2D under ADR 0063. |
| `CollisionObject2D : Node2D` | `abstract CollisionObject : Entity` | Collision filtering and shape ownership above physics-body specializations. |
| `PhysicsBody2D : CollisionObject2D` | `abstract PhysicsBody : CollisionObject` | Shared fixed-step body and shape lifecycle. |
| `RigidBody2D : PhysicsBody2D` | `RigidBody : PhysicsBody` | Dynamic Box2D-backed motion and contact response. |
| `StaticBody2D : PhysicsBody2D` | `StaticBody : PhysicsBody` | Stationary Box2D-backed collision geometry. |
| `CharacterBody2D : PhysicsBody2D` | `CharacterBody : PhysicsBody` | Caller-driven grounded/floating slide motion over the shared world under ADR 0067. |
| `Control : CanvasItem` | `Control : CanvasItem` | UI rectangle, layout, anchors/offsets, focus and GUI behavior, including its own position/rotation/scale/pivot model. It is a sibling of Entity. |
| `BaseButton : Control` | `BaseButton : Control` | Shared button behavior; future UI implementation. |
| `Button : BaseButton` | `Button : BaseButton` | Concrete button behavior; future UI implementation. |
| `CanvasLayer : Node` | `CanvasLayer : Node` | Independent canvas placement, visibility and drawing groups; preserves the neutral scene-tree base. |
| `Timer : Node` | `Timer : Node` | Countdown behavior with tree lifecycle and processing. |
| `Viewport : Node` | `Viewport : Node` | Rendering/input context and viewport ownership. |
| `Window : Viewport` | `Window : Viewport` | Window behavior under the viewport contract. |

The complete chain is `ElectronObject → Node → CanvasItem → Entity → Sprite`; the UI branch is `CanvasItem → Control`. Resources stay outside the scene hierarchy. The former `SceneNode` name is retired. No `Node2D`, `Node3D`, `TransformNode`, duplicate compatibility base, or second public game-object hierarchy is introduced.

`Entity.GetRelativeTransformToParent` preserves the source's ordered local-transform product through a direct spatial-parent chain, including across `TopLevel`. A null, disposed or unconnected ancestor fails with a typed C# exception instead of the native diagnostic plus identity fallback, because identity is also the valid result for a self query. Attached queries enforce the scene owner thread. This error adaptation does not change valid hierarchy results.

`CanvasItem.ZIndex` retains the pinned -4096 through 4096 range. An out-of-range assignment throws `ArgumentOutOfRangeException` and preserves the prior value instead of reporting a native diagnostic and returning. Valid assignments, including equal values, request configuration-warning refresh. Attached Z/order reads and `MoveToFront` enforce the scene owner thread; detached nodes have no bound scene owner and use ordinary `Node` sibling-order rules.

The target hierarchy is:

```text
ElectronObject
└── Node
    ├── Timer
    ├── CanvasLayer
    ├── Viewport
    │   └── Window
    └── CanvasItem
        ├── Entity
        │   ├── Sprite
        │   ├── Camera
        │   ├── CollisionShape
        │   ├── CollisionPolygon
        │   ├── RayCast
        │   ├── ShapeCast
        │   ├── CollisionObject
        │   │   └── PhysicsBody
        │   │       ├── RigidBody
        │   │       ├── CharacterBody
        │   │       └── StaticBody
        │   └── other spatial nodes, preserving their reference intermediate bases
        └── Control
            ├── BaseButton
            │   └── Button
            └── other UI nodes, preserving their reference intermediate bases
```

The hierarchy diagram includes future capabilities. Intermediate reference classes remain part of the contract: Button is in the Control branch through BaseButton, rather than a new direct-inheritance adaptation.

All API positions are mapped by role, including parameter/return types, generic constraints, collections, callbacks/events, factories and stored scene metadata. General tree APIs use `Node`; canvas APIs use `CanvasItem`; APIs that specifically require the spatial node use `Entity`. For example, `Parent`, children, `AddChild`, scene roots, timer/tween binding and packed-scene factories use the neutral base. Member names such as GetNode, AddChild, NodeAdded and ProcessMode retain their tree meaning. The Entity name denotes the spatial node and does not introduce an entity-component system.

Outside these renames, the entire in-scope engine API must correspond to Godot under all previously accepted decisions. Preserve members, inheritance, defaults, values, ordering, lifecycle and observable semantics unless a specific accepted ADR authorizes an adaptation or exclusion. This includes typed C# instead of Variant/string dispatch, typed events, managed lifetime, engine-owned math types, uppercase acronym spelling, accepted resource names, the selected shader contract and internal backend boundaries. Missing implementation is a coverage gap, never an implicit architectural exclusion. New deviations require an explicit decision and coverage rationale.

### Implementation and verification boundary

The runtime implements `Sprite : Entity : CanvasItem : Node : ElectronObject`, `Path : Entity`, `PathFollow : Entity`, `Camera : Entity`, `Control : CanvasItem`, `CanvasLayer : Node`, `Timer : Node` and `Window : Viewport : Node`. Control has executable rectangle/anchor/transform, root viewport mouse routing and keyboard focus foundations; full GUI routing, theme and container behavior remain gaps. CollisionShape, BaseButton and Button remain absent. Inherited 2D physics interpolation now presents canvas and camera poses from fixed-tick history on both renderers without changing logical transforms. Camera editor overlays and independent viewport integration remain gaps. Class pages and compiled coverage describe the actual implemented surface and its remaining gaps.

The hierarchy migration updates consumers, XML/class/component/domain documents, inventory and bidirectional coverage together. SceneHierarchyTests and native mixed-tree pixel checks preserve executable lifecycle, rendering, input, packing and failure cleanup through neutral and canvas bases. Control layout remains in its separate CanvasItem branch; missing GUI features do not move into Entity.

Coverage retains the pinned Godot identities `Node` and `Node2D`, maps them to the actual production types, and records remaining behavioral gaps. Do not mark members implemented merely because this decision has been accepted or a renamed declaration compiles. The existing runtime checks establish only the behavior they exercise; native and platform acceptance still follows ADR 0021.

Path/PathFollow preserve runtime sampling and policy timing through the existing resource and tree APIs. Worker resource changes use SceneTree.Defer; callback errors retain committed state and sibling updates are attempted under ADR 0011. A newer reentrant follower update is not overwritten by an older position assignment. These mappings add no navigation system, editor debounce timer or separate scheduling surface. Current editor gaps and implemented diagnostics remain documented on the [scene paths component](../components/scene-paths.md).

### Rejected alternatives

- Merge tree, canvas and spatial behavior into Entity: conflicts with the accepted responsibility split and gives nonvisual nodes drawing/transform APIs.
- Merge Node2D behavior into CanvasItem and derive Sprite directly from it: conflicts with the accepted separate spatial and UI branches.
- Rename the types while reducing their API or treating current omissions as exclusions: conflicts with the required correspondence contract.

<a id="adr-0011"></a>
## ADR 0011: Exception-safe SceneTree lifecycle, typed groups, and frame timers

Last updated: 2026-09-24

- Status: Accepted; timer scheduling extended by [0036](scene.md#adr-0036), the original tween absence superseded by [0037](scene.md#adr-0037), and input execution barriers extended by [0038](input.md#adr-0038)
- Scope: `SceneTree`, `SceneTreeTimer`, `GroupCallFlags`, and their `Node` lifecycle integration
- Refines: [0006](scene.md#adr-0006)

### Context

The first `SceneTree` version owned a hierarchy and offered frames, deferred actions, and queued node deletion, but constructor callback failures could leave partial membership, teardown failures could strand a live root behind a disposed tree, and cross-thread enqueue could race the one-time disposal clear. The stable reference surface also contains group operations, frame/tree signals, counts, generic queued deletion, and lightweight timers that do not require SDL, rendering, assets, networking, or an editor.

Electron2D must keep typed C# calls, deterministic ownership, its managed runtime in a single public engine assembly, and explicit host-driven frame boundaries. It must not introduce string method/property dispatch or placeholder APIs for missing domains. ADR 0012 vendors the selected managed dependencies into that assembly without changing the `SceneTree` contract.

### Decision

- Queue acceptance and disposal closure share one lock. Work accepted before closure may be intentionally dropped by disposal; work reaching a closed tree is rejected and cannot become stranded.
- Constructor activation completes each lifecycle phase as far as possible. Any failure closes acceptance, terminally marks the failed tree, exits attached nodes, restores ready flags, disposes activation-created timers, clears queued work, and returns hierarchy ownership to the caller.
- Node enter, ready, exit, structural removal, node disposal, and tree disposal attempt all cleanup stages and aggregate callback failures after state reaches a coherent endpoint.
- Lifecycle snapshots revalidate membership; enter/ready/exit re-entry and child escape from an exiting or disposing parent's lifecycle, pre-delete, or cleanup callbacks are rejected.
- Frame, flush, and typed input execution are non-reentrant and cannot begin during entry/exit delivery. Tree disposal from frame, flush, input, or lifecycle callbacks is rejected before the disposal transition.
- Cancellable node deletion continues through disposal after detach failures or detachment, while a stale request in an old tree cannot consume deletion now owned by a new tree.
- Pause traversal visits each still-attached node at most once; opposite re-entrant and teardown-time pause transitions are rejected.
- Reusable configuration diagnostics live on Node and SceneTree: a typed virtual warning query, explicit refresh request, selected live EditedSceneRoot and a synchronous event limited to that selected subtree. Selection clears on exit; errors and thread affinity follow the existing tree contract. Under ADRs 0012/0027 the single runtime assembly makes these capabilities available to a consuming editor in every build configuration. Selecting a subtree opts into diagnostic delivery; it does not create an editor or enable tool scripts.
- DebugPathsHint controls executable Path canvas visualization using a typed project color sampled at tree construction. Live toggles invalidate retained geometry rather than retaining stale commands. This pre-release correction follows ADR 0034 and the existing canvas redraw contract. Sampling is bounded to 1,048,576 points with explicit recording failure; no native mesh dependency or private editor API is introduced.
- Node/tree/frame signals are typed C# events. Group calls and setters accept delegates; group ordering, deferral, and uniqueness use `GroupCallFlags`.
- `Unique` requires `Deferred`, uses operation kind/group/delegate-or-notification identity, ignores setter values, and retains the first accepted operation until its wrapper begins.
- `SceneTreeTimer` is a tree-owned, one-shot, auto-disposed timer updated after nodes and before deferred work in one selected frame lane.
- Node/group/frame counters and uncancellable `QueueDelete(ElectronObject)` are implemented because they require no absent domain.

### Consequences

- Lifecycle callback failures remain visible but no longer leave partial tree ownership, an operational escaped failed tree, or prevent later owned resources from being released.
- Cross-thread scheduling has a precise linearization point at the queue lock. The lock is intentionally small and never held while user code runs.
- Typed group operations require explicit delegates and therefore remain compile-time checked.
- Timers use delivered frame delta. ADR 0016 later added Engine time scaling, and ADR 0036 added reusable Node timers with original-delta time-scale bypass; lightweight `SceneTreeTimer` still has no independent bypass.
- Historical implementation note: scene switching, application quit, tweening, multiplayer, accessibility, editor signals, and platform notifications were absent when this ADR was adopted. ADR 0006 now provides in-memory scene switching; ADR 0037 added typed tweening; the application quit lifecycle is implemented by `SceneTree.Quit`, `AutoAcceptQuit`, and `Engine.Run`. File-based scene loading and the other listed domains remain absent.
- Historical implementation note: ADR 0037 extended activation rollback and finalization to invalidate SceneTree-owned tweens while retaining this ADR's failure-continuing cleanup rule.

### Rejected alternatives

- Keep concurrent queues without a lifetime lock: rejected because a successful enqueue could remain permanently stranded after disposal.
- Swallow lifecycle failures: rejected because user callback failures must remain observable.
- Roll back arbitrary user mutations: rejected because external side effects are not reversible; rollback is limited to owned membership, ready state, and pending queues.
- Add reflection-based group method/property names: rejected by the typed API decision.
- Add placeholder scene, tween, networking, or platform members: rejected because they would advertise behavior without an owning domain.

<a id="adr-0023"></a>
## ADR 0023: Typed in-memory packed scenes

Last updated: 2026-09-27

### Status

Accepted.

- Refines: [0013: Managed typed Resource contract](resources.md#adr-0013)
- Extends: [0005: Notifications and typed editor properties](core-object-runtime.md#adr-0005)
- Preserves: [0010: Typed event connections](core-object-runtime.md#adr-0010)

### Context

Electron2D needs reusable 2D scene templates before it has an asset loader/saver or editor. The accepted architecture excludes dynamic values, reflection-driven string calls, public manual reference counting, hidden scene activation, and fictional APIs for absent domains. Existing `Node`, `PropertyDescriptor`, `Resource`, and deterministic disposal contracts already provide most required runtime mechanisms, but they did not yet define scene ownership, stored-property selection, derived node construction, local resource setup, or failure rollback.

ADR 0013 deliberately deferred automatic local-to-scene behavior and avoided a Resources-to-Scene dependency until a real consumer existed. This component is that consumer. ADR 0013 now records the implemented scene association and root ownership; both active decisions describe the current contract.

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

During reconstruction, each captured property must match a writable stored descriptor on the fresh target with the exact captured value type. Control/Window theme overrides have one bounded extension under [ADR 0083](rendering.md#adr-0083): when a fresh target has no descriptor yet, the engine can reconstruct only the reserved `ThemeColorOverride/`, `ThemeConstantOverride/`, `ThemeFontSizeOverride/`, `ThemeIconOverride/` and `ThemeStyleBoxOverride/` families with exact `Color?`, `int?`, `int?`, `Texture` and `StyleBox` value types. The descriptor is newly bound to the target's typed theme API. Unknown prefixes, other node roles, non-stored entries and mismatched types still fail. Captured source-owner delegates are never reused, and this does not add Variant values or general string member dispatch.

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

<a id="adr-0031"></a>
## ADR 0031: Make Node trees and reusable scenes the primary game-object model

Last updated: 2026-09-23

- Status: Accepted
- Scope: Public game-object, world-composition, and scene-reuse model
- Builds on: [0011](scene.md#adr-0011) and [0023](scene.md#adr-0023). The approved inheritance and API mapping are defined in [0008](scene.md#adr-0008).

### Context

Electron2D uses the neutral `Node` as its common tree contract under ADR 0008, an active `SceneTree`, and typed in-memory `PackedScene` capture and instantiation. The neutral, canvas and spatial layers are implemented. Those decisions define the mechanics but do not yet state the product-level model strongly enough. Future gameplay, editor, serialization, rendering, and physics work needs one stable answer to what a game object is, how a running world is structured, and what may be packaged and reused.

The intended model follows the proven Node-based, scene-oriented structure familiar from Godot while retaining Electron2D's typed C# contracts and explicit lifecycle boundaries. A scene must not be mistaken for only a level file, and a later subsystem must not accidentally introduce a second public entity hierarchy alongside `Node`.

### Decision

- Electron2D is a Node-based, scene-oriented 2D engine. `Node` is the primary public game-object base, and an ordered Node hierarchy is the public representation of a game object, a composed subsystem, and the running game world.
- Specialized gameplay objects derive directly or indirectly from `Node` and compose behavior through child Nodes and typed `Resource` values. Non-spatial, canvas and spatial responsibilities stay separated as `Node`, `CanvasItem` and `Entity` under ADR 0008. The 3D hierarchy remains outside product scope.
- `SceneTree` owns the one active root hierarchy and controls its lifecycle, frame callbacks, deferred work, and deletion. A detached hierarchy is inert until the caller explicitly attaches it to an active tree.
- A scene is a reusable packed Node hierarchy, not merely a level. Any self-contained root and its owned descendants may represent a character, projectile, controller hierarchy, reusable environment object, or complete level without changing the storage model.
- `PackedScene` is the reuse boundary. Each instantiation creates a fresh detached Node hierarchy. Scene-local resources are duplicated with graph identity preserved, while non-local resources remain shared according to the existing resource contract.
- Larger game objects and worlds are built by composing independently reusable scene instances into Node hierarchies. Repeated instantiation must not share mutable Node identity or silently activate lifecycle callbacks.
- The editor and first-party games must use the same public `Node`, `SceneTree`, `Resource`, and `PackedScene` contracts as other consumers. They must not depend on privileged alternate game-object semantics.
- An entity-component or data-oriented implementation may later exist behind a subsystem when measurements justify it, but it must remain an internal implementation detail. Replacing or competing with the public Node/scene model requires revising the owning active ADR.

### Current implementation boundary

The implemented `PackedScene` contract is typed, runtime-only, and in-memory. It can capture one owned Node hierarchy and construct independent detached instances now. Scene files, loaders/savers, editor authoring, nested scene-instance metadata, inherited scenes, editable overrides, scripting, and persistent typed event endpoints remain absent. Composition is currently performed by ordinary Node parenting and packing; this decision does not claim those future authoring workflows are implemented.

### Consequences

- Engine domains can use `Node` as the common public ownership and lifecycle anchor instead of inventing parallel game-object bases.
- Reusable gameplay objects and complete levels use the same packing and instantiation semantics.
- Scene reuse remains explicit and testable: construction is detached, activation is caller-controlled, and instances have independent Node identity.
- File serialization and editor tooling must preserve the typed Node/PackedScene model rather than redefining it.
- Internal performance-oriented storage is permitted only when it does not leak a competing public object model.

### Rejected alternatives

- Treat scenes only as complete levels: rejected because characters, projectiles, controllers, and other reusable hierarchies need the same composition boundary.
- Introduce a separate public `GameObject`, entity, or ECS hierarchy: rejected because it would split ownership, lifecycle, paths, processing, and editor semantics across competing models.
- Activate every scene during instantiation: rejected because reuse requires safe detached construction before explicit tree ownership.
- Wait for the editor or disk format before defining scenes as the reuse unit: rejected because the current in-memory implementation already provides the runtime boundary and future tools need a stable target.

<a id="adr-0036"></a>
## ADR 0036: Reusable Node timer and dual-delta frame delivery

Last updated: 2026-09-24

- Status: Accepted
- Scope: Reusable countdown nodes, internal Node processing, and scaled/original frame timing
- Builds on: [0008](scene.md#adr-0008), [0011](scene.md#adr-0011), [0016](core-object-runtime.md#adr-0016), and [0014](resources.md#adr-0014)

### Context

Electron2D already provides a lightweight `SceneTreeTimer` for one-shot deferred work, but reusable scenes also need a configurable countdown that participates in Node hierarchy, pause, packing, and lifecycle semantics. Its ignore-time-scale option cannot be implemented correctly if Engine supplies only the scaled delta: a zero time scale erases the elapsed duration. Reusing public `ProcessEnabled` would also make engine behavior depend on whether a game enables or disables its own process callback.

The pinned 4.7.2 stable [`Timer` class](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Timer.xml), [`timer.h`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/timer.h), [`timer.cpp`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/timer.cpp), and [`main.cpp`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/main/main.cpp) were audited with the complete Godot `Node` and `Object` inheritance chain (mapped to Node and ElectronObject). Runtime expiry tests strict negativity, despite prose describing the countdown as reaching its end. Both Timer callback lanes subtract `Engine::get_process_step()` when scaling is ignored; the Engine publishes that process step before its physics catch-up cap adjusts the delivered idle delta. The earlier Electron2D exact-zero expiry and fixed physics-step adaptation are withdrawn to match this runtime behavior.

The engine is typed C#, uses direct frame traversal for hot paths, and forbids inert compatibility stubs. The solution therefore has to integrate with existing Node/SceneTree/MainLoop/Engine flow, preserve allocation-free warmed frames, and expose no separate task or clock scheduler.

### Decision

- Add `Timer : Node` and `TimerProcessCallback` to the Scene tree component.
- Timer configuration consists of process lane, finite positive wait time, one-shot, autostart, and ignore-time-scale state. Runtime-only state consists of local pause and remaining time.
- `Start()` uses the configured wait; `Start(double)` validates and stores an explicit duration. These overloads replace a negative sentinel default. Start requires active tree membership, resets a running timer, and never clears local pause. `Stop()` is valid while detached, emits nothing, and clears autostart.
- Ready-time autostart starts after inherited ready handling and clears the flag. A timeout occurs only when internal remaining time becomes strictly negative; at exact zero the public `TimeLeft` is zero and `IsStopped()` reports true, but the timer's internal lane remains enabled until a later negative step.
- One-shot expiration stops before synchronous typed event delivery. Repeating expiration adds the current wait before delivery. Overshoot is retained, but at most one timeout is delivered per frame; public remaining time is clamped to zero while residual time is non-positive.
- Node owns private internal process and physics enable flags in addition to public gameplay callback flags. SceneTree schedules a node when either lane is enabled, dispatches the internal notification before the public callback, and attempts both callbacks when the internal callback fails. Public enablement is captured and revalidated: disabling the lane, disposing, or detaching during internal delivery skips the public callback, while newly enabling it does not inject a callback into the current turn.
- Internal notification IDs `25` and `26` remain public stable identifiers, while scheduling controls and original delta access remain internal engine integration.
- MainLoop carries the delivered scaled delta, its original lane delta, and the original process step only inside the current callback. Direct calls use their supplied delta for all three. Engine captures the unscaled synchronized process step before its physics catch-up cap and supplies it in both callback lanes, including when `TimeScale` is zero. Timer selects that process step when configured to ignore scaling; other built-in consumers keep the original lane delta.
- `SceneTree.CreateTimer` accepts the reference four-argument contract. A lightweight timer configured to ignore time scale selects the original delta of its own process or physics lane; direct calls supply the same delta for both modes. It retains one-shot, tree-owned disposal semantics.
- SceneTree continues using its reusable scheduler buffers. Scheduled-node values compare themselves by priority and captured tree order so sorting creates no steady-state managed allocation.
- Exact Timer configuration is stored by `PackedScene`; runtime pause and remaining time are not. Derived Timer types follow the existing explicit exact-type factory rule.

### Consequences

- Reusable scene hierarchies can own configurable timers without a second scheduler, thread, task, or native clock.
- Game code may independently enable public process callbacks on a Timer-derived node without controlling whether its countdown runs.
- Ignore-time-scale remains meaningful at every non-negative Engine time scale, with the same process-step decrement in both Timer lanes.
- Internal callbacks become a deliberate Node/SceneTree integration point for future built-in nodes; they are not a new public override surface.
- A very short wait is still quantized by delivered frames, and large overshoots catch up at no more than one event per frame.
- `SceneTreeTimer` remains the smaller auto-disposed one-shot facility without repeating, Node, or packing behavior.

### Rejected alternatives

- Run the timer through public `ProcessEnabled`: rejected because user callback configuration must not disable built-in state.
- Add a background timer or task: rejected because it breaks owner-thread event delivery, pause ordering, deterministic tests, and host-driven time.
- Store only scaled deltas and divide by `TimeScale`: rejected because zero cannot be reconstructed and changing scale would introduce numeric ambiguity.
- Add a second SceneTree timer list for reusable Timer nodes: rejected because existing priority/pause-aware Node traversal already supplies the required lifecycle.
- Emit multiple timeouts in one frame: rejected because frame-quantized event behavior and bounded callback work are part of the reference contract.
- Preserve the optional negative duration sentinel: rejected in favor of typed overloads with explicit validation.

### Verification

The executable harness covers defaults and stable identities, descriptors and packed storage, invalid rollback, detached start/stop, both frame lanes and live lane migration, strict-negative expiry after exact zero and overshoot behavior, one-shot/repeating state observed by subscribers, local and tree pause, autostart, owner-thread mutation, callback failure continuation, detachment during timeout, zero time scale with a process step distinct from the fixed physics step, and zero warmed allocations.

It does not establish real host cadence, wall-clock precision, platform scheduling, editor warning presentation, or loaded-scene performance.

<a id="adr-0037"></a>
## ADR 0037: Typed SceneTree tween scheduling

Last updated: 2026-09-24

- Status: Accepted
- Scope: Frame-driven interpolation sequences, typed tween tasks, and SceneTree scheduling
- Builds on: [0002](product.md#adr-0002), [0006](scene.md#adr-0006), [0008](scene.md#adr-0008), [0010](core-object-runtime.md#adr-0010), [0014](resources.md#adr-0014), and [0016](core-object-runtime.md#adr-0016)

### Context

Games need deterministic property animation, interpolated callbacks, delays, event waits, nested timelines, and sequencing before a renderer or editor exists. The current stable [`Tween`](https://docs.godotengine.org/en/stable/classes/class_tween.html) and tweener APIs, their complete inheritance chains, and the 4.7.2 stable [`tween.h`](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/animation/tween.h), [`tween.cpp`](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/animation/tween.cpp), and [`easing_equations.h`](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/animation/easing_equations.h) were audited. Their dynamic values, callables, signal handles, string property paths, and reference-counted lifetime conflict with Electron2D's accepted typed C# and managed-lifetime contracts.

SceneTree already owns owner-thread frame ordering, scaled/original deltas, pause-aware Node policy, reusable scheduler buffers, deferred work, and typed event connections. A separate animation clock, task scheduler, reflection layer, or dynamic container would duplicate those facilities and weaken deterministic ordering.

### Decision

- Add `Tween : ElectronObject`, its four nested policy/curve enums, abstract `Tweener`, and concrete property, method, callback, interval, subtween, and event-wait tweeners under `src/Scene/Animation/` in `Electron2D.dll`.
- Tweens are created only through `SceneTree.CreateTween()` or `Node.CreateTween()`. SceneTree owns valid top-level registration; Node creation also binds pause policy and lifetime. A tween may bind to a detached node but never to a node owned by another tree.
- SceneTree captures and advances matching tweens after node callbacks and lightweight timers and before deferred/deletion work. Creation during an earlier phase may enter that frame; creation during tween processing waits for the next captured batch. Captured entries revalidate their lane and top-level ownership before execution. Process and physics lanes use the existing scaled/original delta pair, so time-scale bypass requires no second clock.
- Property animation uses explicit typed getter/setter delegates and generic values. Method and callback tasks use typed delegates. Event waits use `EventConnection` accessors for zero-, one-, or two-argument events. This permanently replaces dynamic values, reflection callables, signal objects, and string property paths in the implemented surface.
- Built-in interpolation covers booleans, scalar numeric values, and current engine-owned math values. Static `InterpolateValue` accepts finite signed duration, extrapolates outside its interval, and returns the exact composed final value when duration is zero. Int64 interpolation uses wide intermediate arithmetic and checked midpoint rounding to keep valid near-limit results exact. Unsupported values, including strings and collections, require a caller-supplied typed interpolator on task builders; the static method has no untyped fallback. Relative mode requires a built-in addition contract; booleans use replacement and affine transforms compose the captured start with the configured relative transform.
- Sequential and parallel steps preserve overshoot. Exact exhaustion defers a following zero-duration step until later positive time. Callback/method/property/subtween delays and interval/method/property durations accept finite signed values; negative values complete or begin on the first positive step, while forwarded time never exceeds the delivered frame delta. A continuing property captures its start at step start for a delay magnitude below `1e-5`, or after the delay otherwise; a delayed relative final stays based on the step-start value. Finite loops count total sequence executions; non-positive counts select an infinite loop, and an infinite sequence that consumes no time is invalidated instead of hanging a frame.
- Pause, stop/play, speed, default and per-task easing, manual stepping, Node binding, nested tween ownership, and synchronous completion events are explicit owner-thread state. A subtween transfers from its source tree, including another tree on the same owner thread; invalid children are skipped, while self/repeated/cyclic or in-step nesting is rejected before transfer. The parent lane/pause controls the child, both speed scales apply, and unused child time advances later parent steps. A live property `From` retains the active built-in displacement when representable; a full-span integer displacement keeps endpoint interpolation to avoid overflow. A final step stops the tween and emits `Finished` while it remains valid and registered. A following manual step reports completion but only the next eligible tree step removes it and clears its tweeners. `Stop` followed by `Play` during `Finished` restarts it before removal. Kill invalidates immediately while its registry entry remains until that tree sweep or explicit disposal. Typed event receipt may only set an atomic flag from another thread; continuation stays on the owner thread. Await timeouts accept finite signed seconds, with negative values disabling expiry; timeout has priority over an event ready in the same frame. An `EventConnection` cannot observe a publisher that independently clears its invocation list, so callers use a timeout for that case.
- A processing failure attempts every parallel sibling, invalidates the whole sequence, cancels waits and nested work, then participates in SceneTree phase aggregation. A failed manual step also unregisters the invalid tween. Tree activation rollback and finalization invalidate every created or active tween while attempting all other cleanup.
- Top-level tween processing reuses SceneTree snapshot storage and must allocate no managed memory after warmup in the covered steady-state path. Tween objects retain normal managed lifetime and deterministic `IDisposable`; no public reference-count protocol is added.

### Consequences

- Gameplay can build deterministic runtime animation before rendering, assets, scripting, or editor timelines exist.
- The animation surface remains compile-time typed and uses existing lifecycle, scheduling, pause, and event infrastructure.
- Live tween state is runtime-only. Packed scenes do not serialize sequences, property delegates, callbacks, event accessors, or elapsed state.
- Events with more than two payload values need a matching future typed `EventConnection` overload before `TweenAwait` can expose them.
- Method/callback target-disposal detection is available when the delegate directly targets an `ElectronObject`; arbitrary closure captures remain ordinary caller-owned C# state.

### Rejected alternatives

- Dynamic values, string paths, and reflection invocation: rejected by the typed C# contract and because failures would move from compile time into frames.
- A background timer, task-per-tween, or second SceneTree scheduler: rejected because it would break owner-thread ordering, pause semantics, deterministic tests, and allocation policy.
- Public construction or subclassing of tween tasks: rejected because task ownership and reset semantics belong to one Tween timeline and no external extension case currently requires another hierarchy.
- Serialization placeholders for live tween state: rejected because no stable delegate/event endpoint schema or editor animation domain exists.

### Verification

The executable harness covers every enum identity and curve endpoint, 96 pinned easing samples at two elapsed fractions, all supported typed static values, signed/extrapolated and exact-zero static duration, near-limit Int64 in static/method/property paths, managed type lifetime, built-in and custom typed interpolation, property start/relative/delay controls including live From and the pinned capture threshold, callback/interval/method/property/subtween zero, exact and signed timing with capped forwarding, per-method/property curve overrides, per-loop completion order, sequential/parallel ordering, exact boundaries, two-step completion and manual return values, stop/restart during `Finished`, retained loop counts until restart, finite and guarded infinite loops, process/physics and pause policies, scaled/original Engine time, Node/cross-tree lifetime, manual-step cleanup, typed await arities, pre-start reset, cross-thread event receipt, signed/zero/live timeout and same-frame priority, source disposal, external invocation-list limit, loop replay and cancellation failure, cross-tree nested ownership and child disposal, lane/nesting snapshot isolation, callback and completion-event failures, activation rollback, finalization, owner-thread enforcement, validation, and zero warmed active-frame allocation.

It does not establish visual motion quality, editor authoring, serialized animation compatibility, real host cadence, all-target native execution, or large-scale performance.
