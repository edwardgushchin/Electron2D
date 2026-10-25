# Electron2D scene decisions

Last updated: 2026-10-18

This bounded document owns the current architectural decisions for scene. Node is the neutral scene-tree base and Entity is the spatial canvas base under ADR 0008; current class pages describe the implemented API. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0006](#adr-0006), [0008](#adr-0008), [0011](#adr-0011), [0023](#adr-0023), [0031](#adr-0031), [0036](#adr-0036), [0037](#adr-0037).

<a id="adr-0006"></a>
## ADR 0006: Own hierarchy, deferred work, and queued deletion in SceneTree

Last updated: 2026-10-20

- Status: Accepted; frame scheduling refined by [0008](scene.md#adr-0008), lifecycle and queue safety refined by [0011](scene.md#adr-0011)
- Scope: `Node` hierarchy and `SceneTree` scheduling

### Context

Godot's object surface includes deferred calls and queued deletion, but both require a safe execution boundary and hierarchy ownership. Putting them on every `ElectronObject` would hide scheduling state and allow objects outside a scene to pretend that a game-loop queue exists.

### Decision

- `Node` owns ordered child relationships and lifecycle callbacks; `SceneTree` owns one active root hierarchy.
- `SceneTree.Root` stays stable for the tree lifetime. `CurrentScene` selects one optional direct child of that root; assigning it does not reparent nodes. `ChangeSceneToNode` accepts a detached scene, removes the selected old scene immediately, and installs the new scene at the next deferred safe point. `ChangeSceneToPacked` instantiates before replacement. The tree owns accepted pending scenes, disposes superseded and old scenes before entry, and releases pending scenes during finalization. `SceneChanged` follows successful entry. `UnloadCurrentScene` disposes only the selected child. File-based change and reload await a scene loader.
- SceneTree-managed entry dispatches enter notification, then the typed OnEnterTree hook, TreeEntered, tree NodeAdded and parent ChildEnteredTree, before descendants. After all entry, ready visits children first, dispatches PostEnterTree on every attachment, then consumes a pending ready flag and delivers ready notification/hook/event. Mark the parent ready pass before descending so children added during entry wait for that pass, while children added during ready can finish immediately. Capture attachment versions in ready snapshots; a removed/reentered sibling cannot revive an older entry. RequestReady rearms only its receiver for a later attachment, including requests made during ready. Its read/mutation metadata permits the owner/current processing group without physical authoring or wake. Detached/disposed IsNodeReady retains its stored observation. Manual ready notification still invokes its hook without consuming that state; manual enter/exit notifications do not invoke tree-transition hooks.
- Node process/physics delta properties read the attached tree's current or most recent duration for that lane, including disabled/paused nodes and frame-start observers. SceneTree commits the supplied scaled duration after frame validation and execution preflight, before frame-start work. Detachment returns zero; reattachment adopts the receiving tree. Manual notifications do not advance time. Owner-thread/lifetime guards remain explicit. This follows the pinned Node tree-time getters; callback enablement controls delivery rather than observation.
- Ready notification enables each overridden compiled C# process/physics/input/shortcut/unhandled-key/unhandled callback before OnReady. Detached defaults remain false; OnReady or later writes can disable lanes. Reentry does not repeat a consumed ready notification, while RequestReady re-arms it. Manual ready notification applies the same callback preparation without consuming managed ready state. Detect the six actual virtual delegate targets, cache only a bit mask under weak CLR Type keys, and keep dispatch as ordinary typed virtual calls. No name lookup, assembly scan, reflection invocation, script alias resolver or new public capability flag is introduced. Bound delegate metadata, automatic callback delivery and extended physical PackedScene/resource consumers now execute in strict NativeAOT CPU/software-GPU/registered profiles. Typed tile arrays and compiled catalog preparation retain their exercised metadata path. File-backed source registration still requires matching assembly/PDB and full project metadata; NativeAOT graph consumers use unbound Script assets when those prerequisites are absent. Full domain/foreign/platform NativeAOT acceptance remains separate. The former explicit-only callback activation is withdrawn as an unaccepted implementation deviation.
- The six public callback switches, two internal switches and two callback priorities are local processing metadata. Getters return stored values to coordinated callers; they are not atomic snapshots and grant no foreign scene/backend mutation. Setters require the scene owner or current processing group and reject capture even for unchanged values, independently of physical authoring. This applies to neutral, canvas and physical subclasses. All six public switches and both priorities have stored typed descriptors; internal switches remain runtime-only. Reconstruction restores configuration before ready applies automatic override preparation.
- Skeleton/Bone/PhysicalBone logical metadata follows active-group observation under ADR 0092. PhysicalBone notifications retain immediate group-local body/joint poses and owner publication of changed native activation after joining. Joint endpoint paths are local authored inputs under ADR 0084; other numerical settings retain their existing guards. Selected failure status is checked on the owner before lending bound worlds to a worker stage.
- Attached hierarchy mutation, flushing, and disposal run on the thread that created the tree; invalid disposal is rejected before object state begins changing.
- `SceneTree.Defer(Action)` and `SetDeferred<T>` accept typed work without string method lookup.
- Node's call/set/notify thread-safe and deferred-thread-group family uses typed target delegates and numeric notifications. Each inherited processing group owns a retained FIFO drained before and after its callbacks; lane masks additionally enable message-only processing. The scene owner or current target group can execute immediate calls; other callers enqueue. Deferred group calls always enqueue and require tree membership. Detached thread-safe calls follow ordinary detached-node access and caller coordination. The explicit groups, order and masks execute under the bounded implementation profile below; broader worker access across inherited scene/backend members retains Partial coverage.
- Group messages sample weak immutable object identity rather than retaining the receiver. A live receiver follows its current tree owner at delivery; transfer to another owner requeues there. Disposed/collected receivers are skipped. Typed values/delegates remain retained only until delivery or discard. Messages appended during a group drain execute in FIFO order until quiescence; each failure is collected while later messages and frame phases continue. Ordinary Defer also drains accepted work until the queue is observed empty; group and ordinary queues retain their distinct delivery phases. Group queues close and discard atomically with tree finalization, sharing the existing typed slot pool and lifetime gate.
- Core's `EventConnection` can receive `SceneTree.Defer` as its scheduler for cancellable deferred event delivery.
- A flush drains deferred calls and typed setters in FIFO order until the queue is observed empty under the work gate, including work enqueued by the current callbacks. Two retained action buffers exchange enqueue/drain roles between batches; no recursive public flush or callback-stack recursion is needed. Callback errors are collected while later and nested work continues. Only after deferred work reaches quiescence does one captured deletion batch run. Work created by deletion callbacks retains its later-safe-point rule. Continuous self-enqueue can keep a flush running indefinitely; frame-start events provide an explicit later-frame boundary. The former one-action-batch-per-flush rule is withdrawn because it delayed applicable deferred calls and property writes beyond the current idle cycle.
- Typed setter/event work uses retained invocation slots under the same work gate. Each slot has independent strongly typed values and one immutable Action in the ordinary FIFO queue. Prepare/growth happens before the warm claim; a slot is recycled only after execution or explicit discard and releases captured references. Finalization/activation rollback discards queued typed slots without invoking user handlers. The exact bound Defer delegate registers an internal provider under ADR 0010; custom schedulers and existing public signatures keep their prior contracts. Subscription reservations are released at token completion/disposal; retained capacity follows the peak simultaneous subscriptions/pending invocations rather than total historical token churn.
- Node deletion is a separate atomic request processed after deferred actions. It detaches and disposes the full subtree; it can be cancelled before processing. Passive `IsQueuedForDeletion` observation belongs to ElectronObject under ADR 0003: it covers both this cancellable Node request and every accepted SceneTree.QueueDelete request. Moving the getter to its owning base preserves inherited Node use; it adds no scheduling capability to detached resources. A claimed Node request remains observable until cleanup finishes, while cancellation is already closed.
- The normal `Engine.Run` entry point owns time and the safe point. An embedding application may instead call `ProcessFrame`, `PhysicsFrame`, or `FlushDeferred`; the frame methods run their callback lane and then flush. `SceneTree` owns no frame-pump thread or clock. Its first worker stage uses one temporary joined helper to prepare actual contention on the shared work gate before game callbacks run. The helper runs no game code and holds no execution context; it also works when a one-worker pool owns the tree. This cold synchronization preparation is separate from retained worker dispatch and warmed frame budgets.

### Consequences

- Ordering and thread affinity are explicit and testable.
- Deferred execution does not require `Callable`, reflection, `Variant`, or method names.
- Node group identifiers are literal, ordinal and case-sensitive. Add requires a nonempty string; whitespace, Unicode and underscore-prefixed names remain valid. Empty lookup/removal and tree selection have no members; null strings reject. The first addition chooses persistence; repeated addition never promotes/demotes it. Remove/readd selects another policy. Preserve the established typed bool RemoveFromGroup result as the membership observation at removal; GetGroups returns an independent read-only ordinal snapshot.
- Group membership is local metadata: the scene owner or the node's current processing group may read and mutate it, including membership on physical nodes. Detached access follows caller coordination. Capture rejects mutation. Keep physical authoring/lifetime and hierarchy on their owning thread; validate physical authoring in the nonvirtual EnsureMutable path before user validation hooks. A derived validation override cannot bypass that guard. Group metadata edits do not alter numerical state or wake bodies.
- Queueing is thread-safe. Outside the currently executing processing-group context, game-state mutation remains on the scene owner thread.
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

Last updated: 2026-10-21

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
| `AudioStreamPlayer : Node` | `AudioStreamPlayer : Node` | Stream playback independent of scene position, including music and UI sounds. |
| `AudioStreamPlayer2D : Node2D` | `AudioStreamEmitter : Entity` | Positioned scene source with distance attenuation, stereo panning and viewport listener/Area routing under [ADR 0047](audio.md#adr-0047). |
| `CollisionShape2D : Node2D` | `CollisionShape : Entity` | Borrowed collision-shape placement as a direct physics-body child; first executable profile under ADR 0054. |
| `CollisionPolygon2D : Node2D` | `CollisionPolygon : Entity` | Owned solid or hollow polygon placement as a direct physics-body or Area child under ADR 0066. |
| `RayCast2D : Node2D` | `RayCast : Entity` | Spatial ray node with cached fixed-physics query state over the shared World under ADR 0063. |
| `ShapeCast2D : Node2D` | `ShapeCast : Entity` | Spatial shape sweep with cached contact results over the shared World under ADR 0063. |
| `CollisionObject2D : Node2D` | `abstract CollisionObject : Entity` | Collision filtering and shape ownership above physics-body specializations. |
| `PhysicsBody2D : CollisionObject2D` | `abstract PhysicsBody : CollisionObject` | Shared fixed-step body and shape lifecycle. |
| `RigidBody2D : PhysicsBody2D` | `RigidBody : PhysicsBody` | Dynamic motion and contact response through the selected physics backend (ADR 0054). |
| `StaticBody2D : PhysicsBody2D` | `StaticBody : PhysicsBody` | Stationary collision geometry through the selected physics backend (ADR 0054). |
| `CharacterBody2D : PhysicsBody2D` | `CharacterBody : PhysicsBody` | Caller-driven grounded/floating slide motion over the shared world under ADR 0067. |
| `Control : CanvasItem` | `Control : CanvasItem` | UI rectangle, layout, anchors/offsets, focus and GUI behavior, including its own position/rotation/scale/pivot model. It is a sibling of Entity. |
| `BaseButton : Control` | `BaseButton : Control` | Shared button behavior; future UI implementation. |
| `Button : BaseButton` | `Button : BaseButton` | Concrete button behavior; future UI implementation. |
| `CanvasLayer : Node` | `CanvasLayer : Node` | Independent canvas placement, visibility and drawing groups; preserves the neutral scene-tree base. |
| `Timer : Node` | `Timer : Node` | Countdown behavior with tree lifecycle and processing. |
| `Viewport : Node` | `Viewport : Node` | Rendering/input context and viewport ownership. |
| `Window : Viewport` | `Window : Viewport` | Window behavior under the viewport contract. |

Owner is borrowed authoring metadata, independent of scene membership and physical-world ownership. Assignment requires a current ancestor. Removing or reparenting a node outside an active tree retains the previous owner, even when it is no longer an ancestor; its unique-name registration remains reachable through that owner. Active-tree removal clears unreachable owners after structural callbacks and before the final exited signals. Reparent preserves its own reachable outside owner and ordinary descendants sharing that owner; it does not restore unrelated descendant owners or traverse internal branches for restoration. Local owners within the moved subtree remain valid. Preserve the established typed bool RemoveChild observation and typed validation exceptions.

Use Core ObjectIdentity under ADR 0003 for owner references and reverse owned-node membership: neither direction retains another node. Owner disposal/collection makes borrowed metadata unavailable, and deterministic disposal releases the records without disposing separately parented owned nodes. Unique-name lookup traverses live owned identities, including detached owned nodes, rather than assuming ownership is the current structural subtree. Registration prepares weak/list capacity before changing ownership; changed graph/owner authoring is cold. Public Parent/Tree/IsInsideTree and live Owner/ancestry observations are coordinated reads, not atomic multi-field snapshots or permission to mutate a foreign scene.

Node authoring metadata executes in the runtime assembly under ADR 0090. EditorDescription is stored typed text and emits EditorDescriptionChanged only after a different value is committed, in every build. DisplayFolded is transient presentation state and never changes visibility, processing or physical participation. SetEditableInstance requires a strict descendant and sets the target's flag, observable through every current strict ancestor; disabling also unfolds the receiver. The flag follows target topology. Null editability queries return false; invalid nonnull relationships reject with typed exceptions.

The existing SceneTree.EditedSceneRoot selection is the explicit authoring context for IsPartOfEditedScene and EditorStateChanged, including selection of the tree root. Membership is the selected root and its descendants; detached nodes have no context. This replaces the external engine's global editor-hint/editor-viewport-parent convention with the established per-tree typed selection. Relevant process-mode, owner, unique-name, persistent-group, source-path, editable-instance and authored-connection changes notify the affected selected node. Ordinary runtime-only groups and nonpersistent CLR subscriptions do not. Signal blocking applies, callbacks run after commitment outside internal locks, and their errors retain the owning operation's documented failure policy. This supplies programmatic authoring observations without creating editor UI or enabling tool scripts.

The complete chain is `ElectronObject → Node → CanvasItem → Entity → Sprite`; the UI branch is `CanvasItem → Control`. Resources stay outside the scene hierarchy. The former `SceneNode` name is retired. No `Node2D`, `Node3D`, `TransformNode`, duplicate compatibility base, or second public game-object hierarchy is introduced.

Node insertion resolves sibling-name collisions across ordinary and internal
children. Existing typed constructors expose their class name immediately; that
nonblank initial name is preserved when unique, as are explicit authored names.
The default insertion policy uses a process counter for `@Class@number` names on
collision. `forceReadableName=true` instead advances an existing decimal suffix
or starts at 2, preserving leading width and arbitrary-length carry. The live,
feature-aware `editor/naming/node_name_num_separator` setting selects no separator,
space, underscore or dash (0 through 3, default 0). Keep the existing typed
InternalMode overload and supply the bool overload without changing old call
positions. AddSibling inherits its source's internal partition. Name assignment
uses the readable policy; Reparent and ReplaceBy use default insertion naming,
including transferred children. Validate structural/lifetime/owner restrictions
before naming or topology mutation; an ancestor cannot replace its descendant.
Public Name assignment replaces each `.`, `:`, `@`, `/`, `"` and `%` with `_`
before sibling resolution. Null, empty and whitespace-only inputs reject; the
typed nonblank construction default is retained. Every accepted assignment,
including the same input, propagates path notifications through detached or active
subtrees. Only active nodes raise Renamed and the tree events, in that order after
path delivery. Names and path invalidation commit before callbacks; remaining
descendants and active events are attempted after failures and errors are combined.
Retained depth-specific path-notification snapshots
preserve reentry and child-list mutation semantics while releasing borrowed
references after every traversal. Packed-scene reconstruction restores validated
stored names directly, preserving generated `@Class@number` identities before
attachment instead of running public authoring normalization again. Stored names
allow internal `@` but reject other reserved path characters. This does not invent
an empty-name or script-identity state.

`Node.GetOrphanNodeIDs()` and `PrintOrphanNodes()` retain the pinned debug-build
boundary: Debug tracks live nodes outside every active SceneTree, including
detached descendants and internal children; Release returns an empty array and
prints nothing. This is the specified release behavior, not an unavailable
runtime capability fallback. The registry must not keep nodes or detached
hierarchies alive. Return caller-owned `ulong[]` identifiers matching InstanceID,
ordered by ID. Print sampled root-relative names and compiled C# class names
through the ordinary diagnostic stream, capturing rows before logger callbacks.
Disposing/collected nodes are absent, independently of physical space attachment.
These are cold nearby observations rather than atomic or zero-allocation scene
snapshots; they do not inspect a numerical backend or introduce script binding.

`Entity.GetRelativeTransformToParent` preserves the source's ordered local-transform product through a direct spatial-parent chain, including across `TopLevel`. A null, disposed or unconnected ancestor fails with a typed C# exception instead of the native diagnostic plus identity fallback, because identity is also the valid result for a self query. Attached queries permit the scene owner or an active processing-group thread. This error adaptation does not change valid hierarchy results.

Logical Entity transform getters and pure ToLocal/ToGlobal/GetAngleTo/GetRelativeTransformToParent queries permit reads from the scene owner or any active processing group, including ancestors and other groups. CanvasItem.GetGlobalTransform and concrete Entity/Control.GetTransform preserve that read policy; Control combined-pivot and derived rectangle reads follow the same logical chain. Callers coordinate concurrent writes; separate reads are not an atomic scene snapshot. Ordinary external/pool work has no group permission. Global cache calculations and virtual transform getters run outside the existing canvas-item gate; one publisher commits the matrix and clears the volatile invalidation flag under that gate. This retains notification coalescing and avoids concurrent writes to a shared dirty cache without new per-node allocation. Mutation, notification forcing, hierarchy, renderer/backend queries and numerical state keep their separate guards; broad worker mutation remains Partial.

Transform-notification policy reads permit the scene owner or any active group; writes and ForceUpdateTransform permit the owner/current group, with capture/lifetime/derived validation. Forced delivery atomically removes only that item's pending entry before calling numeric and typed notifications immediately on the caller, outside the queue gate. Reentry may enqueue and force another delivery; observer failure leaves the consumed entry removed. Concurrent forced callbacks retain balanced lifecycle accounting. Camera viewport updates and generated tile physics/navigation transforms publish on the owner after a joined worker stage, before later main-group queries or numerical stepping, without replaying user notifications. This applies to both ordinary and pooled scene owners.

Configuration-warning queries and refresh requests permit the owner/current processing group under the pinned Node thread guard. Refresh is synchronous on the caller, preserves edited-root filtering, and does not evaluate strings or grant numerical/native permissions. Built-in providers preserve that common guard before reading authored diagnostic state, including invalid-configuration branches. Capture, lifetime and derived validation precede refresh delivery; user callbacks remain outside internal canvas gates.

General Node.PropagateNotification runs synchronously in the owner/current processing group and uses the same parent-first protected traversal as interpolation reset. Skip inaccessible explicit-group branches with a collected error and continue later siblings. Dispatching a number supplies no additional renderer, GUI or numerical permissions to its handler; preserve individual guards and report their failures through the traversal. Pure custom delivery remains available during capture while callback mutations keep their ordinary capture validation. CollisionShape/CollisionPolygon Parent/Unparent handling checks the owner before any canvas/geometry ownership changes. Remaining reserved-consumer publication gaps stay explicit in coverage. RayCast/ShapeCast cached results are logical observations for owner/active groups, and options/exceptions are owner/current-group metadata. Numerical sampling preserves the pinned owner-only direct-space restriction; a failed reserved notification does not queue a delayed query.

CanvasItem.GetCanvasLayerNode is a coordinated logical read for the owner or any active processing group. CollisionObject canvas-identity and enabled/disabled participation effects, plus TileMapLayer generated-body canvas identities, use retained ordered owner publication after joined stages release physical borrows. Preserve immediate user callbacks and effective ProcessMode; retain exact generated RIDs rather than retargeting replacement bodies. All scene publications retain the tree attachment epoch, and canvas identities additionally retain the canvas attachment epoch, so owner callback reentry or TopLevel rebinding cannot apply stale requests. Continue later publications after original errors. Native canvas/viewport queries, hierarchy edits and other reserved consumers retain their own guards and open coverage.

Physics interpolation mode changes and explicit resets are presentation metadata accessible to the owner/current processing group, including on physical subclasses, independently of numerical authoring. Guard capture, lifetime and derived validation before equal-value or disabled/detached exits. Resolve inherited policy immediately across the subtree. Deliver reset notifications immediately, parent first, to accessible branches; report an inaccessible explicit-group branch without calling its descendants and continue later siblings. Preserve committed policy and aggregate callback failures. Policy traversal uses retained child lists with balanced atomic notification-depth guards instead of allocating descendant snapshots.

Source histories reset immediately in the current group; shared MultiMesh history uses its resource gate. Reset a MultiMeshInstance assigned source independently of drawing-command preparation, and exclude that already reset source from the cached-command pass. First presentation must not be a prerequisite for resetting instance history. Publish borrowed renderer policies, foreign-camera scheduling and viewport history on the owner after the stage joins, including after callback failures. A retained ordered value queue captures each worker camera scroll position/rotation and viewport reset. Do not collapse scroll/reset/scroll into a final-pose update: doing so discards the explicit previous endpoint. After the ordered operations, publish any final camera pose changed by a later ancestor edit without advancing unchanged tracking again. Apply camera tracking and reset operations before later main-group queries and end-of-tick capture, outside the queue gate; clear queued references after every publication attempt. These operations never replay user notifications or authorize renderer/numerical calls on a worker.

Own-group Entity transform setters and their mutating helpers commit scene poses immediately after capture/lifetime/derived validation and physical borrow preflight. Local notifications remain synchronous on the calling group; the global FIFO is protected by the scene work gate, with no callbacks under that gate. A revision-qualified global cache prevents an older computation from publishing after invalidation; parallel invalidation visits affected descendant chains even when an ancestor is already dirty. Rendering revision publication is atomic. Physical body/Area/child-shape authoring metadata is consumed by the original world owner before its next query or numerical step, preserving AnimatableBody target presentation and derived local-transform behavior. AnimatableBody.SyncToPhysics uses the same owner/current-group presentation authoring boundary, including pending-target changes and equal-write validation (ADR 0060).

Before each parallel scene stage the owner borrows every existing bound physics space, with rollback if acquisition fails and release after all runners join, including failures. Numerical steps, queries, native authoring and lifetime changes are excluded while those scene transforms are being authored. Only own-group transform metadata may cross this boundary. A pooled thread hosting the tree does not retain owner-only permission while it participates in a worker stage; its current-group permission still applies. Hierarchy, configuration, arbitrary physical writes and renderer calls retain their separate owning boundaries. This is scene pose publication, not permission to execute a solver or direct backend query on a game worker.

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

The runtime implements `Sprite : Entity : CanvasItem : Node : ElectronObject`, `Path : Entity`, `PathFollow : Entity`, `Camera : Entity`, `Control : CanvasItem`, `CanvasLayer : Node`, `Timer : Node` and `Window`/`SubViewport : Viewport : Node`. Control has executable layout, root viewport input, focus, themes, containers and text. BaseButton, Button and their implemented checkbox/texture descendants preserve the separate Control branch; nested viewport routing and remaining widgets retain exact coverage dependencies. Inherited 2D physics interpolation now presents canvas and camera poses from fixed-tick history on both renderers without changing logical transforms. Independent single-layer viewport rendering/texture output now execute; Camera editor overlays, multiview and per-viewport GUI context retain exact gaps. Class pages and compiled coverage describe the actual implemented surface and its remaining gaps.

The hierarchy migration updates consumers, XML/class/component/domain documents, inventory and bidirectional coverage together. SceneHierarchyTests and native mixed-tree pixel checks preserve executable lifecycle, rendering, input, packing and failure cleanup through neutral and canvas bases. Control layout remains in its separate CanvasItem branch; missing GUI features do not move into Entity.

Coverage retains the pinned Godot identities `Node` and `Node2D`, maps them to the actual production types, and records remaining behavioral gaps. Do not mark members implemented merely because this decision has been accepted or a renamed declaration compiles. The existing runtime checks establish only the behavior they exercise; native and platform acceptance still follows ADR 0021.

Path/PathFollow preserve runtime sampling and policy timing through the existing resource and tree APIs. Worker resource changes use SceneTree.Defer; callback errors retain committed state and sibling updates are attempted under ADR 0011. A newer reentrant follower update is not overwritten by an older position assignment. These mappings add no navigation system, editor debounce timer or separate scheduling surface. Current editor gaps and implemented diagnostics remain documented on the [scene paths component](../components/scene-paths.md).

### Rejected alternatives

- Merge tree, canvas and spatial behavior into Entity: conflicts with the accepted responsibility split and gives nonvisual nodes drawing/transform APIs.
- Merge Node2D behavior into CanvasItem and derive Sprite directly from it: conflicts with the accepted separate spatial and UI branches.
- Rename the types while reducing their API or treating current omissions as exclusions: conflicts with the required correspondence contract.

<a id="adr-0011"></a>
## ADR 0011: Exception-safe SceneTree lifecycle, typed groups, and frame timers

Last updated: 2026-10-18

- Status: Accepted; timer scheduling extended by [0036](scene.md#adr-0036), the original tween absence superseded by [0037](scene.md#adr-0037), and input execution barriers extended by [0038](input.md#adr-0038)
- Scope: `SceneTree`, `SceneTreeTimer`, `GroupCallFlags`, and their `Node` lifecycle integration
- Refines: [0006](scene.md#adr-0006)

### Context

The first `SceneTree` version owned a hierarchy and offered frames, deferred actions, and queued node deletion, but constructor callback failures could leave partial membership, teardown failures could strand a live root behind a disposed tree, and cross-thread enqueue could race the one-time disposal clear. The stable reference surface also contains group operations, frame/tree signals, counts, generic queued deletion, and lightweight timers that do not require SDL, rendering, assets, networking, or an editor.

Electron2D must keep typed C# calls, deterministic ownership, its managed runtime in a single public engine assembly, and explicit host-driven frame boundaries. It must not introduce string method/property dispatch or placeholder APIs for missing domains. ADR 0012 vendors the selected managed dependencies into that assembly without changing the `SceneTree` contract.

### Decision

- Queue acceptance and disposal closure share one lock. Work accepted before closure may be intentionally dropped by disposal; work reaching a closed tree is rejected and cannot become stranded.
- Constructor activation completes each lifecycle phase as far as possible. Any failure closes acceptance, terminally marks the failed tree, exits attached nodes, restores ready flags, disposes activation-created timers, clears queued work, and returns hierarchy ownership to the caller.
- Node enter, ready, exit, structural removal, reparent, node disposal, and tree disposal attempt all cleanup stages and aggregate callback failures after state reaches a coherent endpoint. Reparent preflights both destination insertion and child-order access before removing the old parent; removal callback errors cannot suppress possible insertion, owner restoration or derived placement restoration. Retired destinations are not resurrected. Child-order rendering publication, numeric notification and signal continue independently after errors. Departure visits siblings in reverse order, children before parents: OnExitTree, TreeExiting, exit notification, engine membership/physical release, tree NodeRemoved and parent ChildExitingTree, then association removal. After the complete subtree has detached, structural removal clears its root parent and publishes its callbacks; active removal then clears unreachable authoring owners before a second reverse child-first TreeExited pass. Detached authoring removal retains borrowed owners and registrations under ADR 0008. Pending-exit flags qualify that pass and prevent intermediate insertion, hierarchy mutation, disposal or activation from escaping the transition. Only nodes that actually exited receive it. Body/Area/joint retirement belongs to non-public membership cleanup after TreeExiting, independently of throwing/omitted user exit-hook base calls; their inherited typed OnExitTree hook remains available. Constructor rollback restores consumed ready flags; arbitrary user side effects remain outside rollback.
- Parent cleanup attempts every owned child. A child that rejects validation or vetoes pre-delete remains alive, but its structural edge is removed before the parent becomes terminal; its subtree and resources remain its own. Detach failures are collected without skipping siblings. Cleanup callbacks cannot move children out of an actively disposing parent; only internal teardown may release that relationship.
- Lifecycle snapshots revalidate membership; enter/ready/exit re-entry and child escape from an exiting or disposing parent's lifecycle, pre-delete, or cleanup callbacks are rejected.
- Frame, flush, and typed input execution are non-reentrant and cannot begin during entry/exit delivery. Tree disposal from frame, flush, input, or lifecycle callbacks is rejected before the disposal transition.
- Cancellable node deletion continues through disposal after detach failures or detachment, while a stale request in an old tree cannot consume deletion now owned by a new tree.
- Pause traversal visits each still-attached node at most once; opposite re-entrant and teardown-time pause transitions are rejected. Notify only changed callback eligibility: Pausable pauses on tree pause, WhenPaused unpauses, and Always/Disabled remain silent. Retain traversal/visited capacity after warmup while taking each child snapshot after its parent's callback. Paused trees skip numerical steps of their bound worlds; Always/WhenPaused callbacks still execute. Explicit caller-owned PhysicsServer.SpaceStep remains independent, and a separate unpaused tree retains its own clock. This follows the reference pause/physics effect through Electron2D's host-owned tree/space boundary rather than a global service toggle.
- ProcessMode uses Inherit/Pausable/WhenPaused/Always/Disabled identities 0..4. Detached changes are silent; entering roots replace Inherit with Pausable and attached roots reject Inherit before mutation. Attached changes preflight affected collision objects, commit once, and deliver Paused/Unpaused before Disabled/Enabled parent-first through inheriting descendants, stopping at explicit modes. Cached snapshots track delivered eligibility/enabled revisions, so a nested explicit child mode cannot consume pending physical Enabled delivery; newer transitions or retired attachments suppress stale outer delivery without dropping an independent enabled transition. Attempt all remaining phases after callback failures and aggregate them. Reads/CanProcess allow the owner/current processing group; writes retain scene-owner and physical-borrow guards. Inheritance resolution uses internal policy state, so a worker need not read its parent's public metadata.
- Reusable configuration diagnostics live on Node and SceneTree: a typed virtual warning query, explicit refresh request, selected live EditedSceneRoot and a synchronous event limited to that selected subtree. Selection clears on exit; errors and thread affinity follow the existing tree contract. Under ADRs 0012/0027 the single runtime assembly makes these capabilities available to a consuming editor in every build configuration. Selecting a subtree opts into diagnostic delivery; it does not create an editor or enable tool scripts. ProgressBar uses the same selected subtree for its executable indeterminate authoring preview under ADR 0080; selection does not activate a visual editor or change general scene processing policy.
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
- Historical implementation note: scene switching, application quit, tweening, multiplayer, accessibility, editor signals, and platform notifications were absent when this ADR was adopted. ADR 0006 now provides in-memory scene switching; ADR 0037 added typed tweening; the application quit lifecycle is implemented by `SceneTree.Quit`, `AutoAcceptQuit`, and `Engine.Run`. File-based scene loading now executes through the typed archive producer under ADR 0013; other listed gaps retain their coverage triggers.
- Historical implementation note: ADR 0037 extended activation rollback and finalization to invalidate SceneTree-owned tweens while retaining this ADR's failure-continuing cleanup rule.

### Rejected alternatives

- Keep concurrent queues without a lifetime lock: rejected because a successful enqueue could remain permanently stranded after disposal.
- Swallow lifecycle failures: rejected because user callback failures must remain observable.
- Roll back arbitrary user mutations: rejected because external side effects are not reversible; rollback is limited to owned membership, ready state, and pending queues.
- Add reflection-based group method/property names: rejected by the typed API decision.
- Add placeholder scene, tween, networking, or platform members: rejected because they would advertise behavior without an owning domain.

### Processing-group implementation profile

Node's owner-specific ProcessThreadGroupMode (Inherit0/MainThread1/SubThread2), ProcessThreadGroupOrder and ProcessThreadMessageFlags (Process1/Physics2/All3, with zero valid) have stored descriptors. Explicit group owners partition inherited descendants; nested owners establish boundaries. At each frame groups capture ascending order, with subthread stages before main stages at equal order. A stage submits independent groups to retained WorkerThreadPool and joins every runner before the next stage or physical step. Callback priority/preorder and current enablement/pause/lifetime checks remain per group. The captured group identities/order stay fixed for the outer frame; retired entries skip execution. Each eligible group captures its enabled member schedule when that group runs, so changes can affect a later group in the same outer frame.

Group queues use the existing weak typed invocation infrastructure. Active callback groups drain in either lane; masks additionally permit an otherwise inactive group to service messages in the selected lane. The default group is always permitted. Same-group calls may be immediate, other callers queue to the actual destination; rejected owner/render/physics operations keep their ordinary guards. Processing-switch/priority writes grant only the current group context; their stored-value getters permit coordinated reads independently of that context. All eight switches and both priorities follow this metadata rule, including input-stage configuration on physical nodes. Frame delta observation remains owner/current-group guarded. The same access policy permits ProcessThreadGroupOrder and ProcessThreadMessages writes, including on inherited physical descendants; getters retain coordinated stored-value access. Order changes apply to the next captured frame; mask changes determine eligibility when the next stage is considered and never interrupt an active drain. Nested pool callbacks helped synchronously on the same physical worker retain its active scene group; ordinary jobs after the group ends receive no such permission. Input delivery itself stays on the scene owner. These permissions grant no arbitrary worker hierarchy, renderer, geometry, joint or numerical mutation. ProcessThreadGroup role changes/rebinding, hierarchy mutation, disposal, rendering and backend state remain on the scene/backend owner. A later main group can collect pure worker results and mutate actual physics after the worker barrier. Full general in-group Node/CanvasItem/Entity access and physical worker mutation remain separately Partial until implemented and verified; this profile is real group execution, not a completed full inheritance claim.

<a id="adr-0023"></a>
ADR 0023 is maintained in [Packed scenes and runtime hierarchy duplication](packed-scenes.md#adr-0023). This route preserves existing links.

<a id="adr-0031"></a>
## ADR 0031: Make Node trees and reusable scenes the primary game-object model

Last updated: 2026-10-17

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

The implemented `PackedScene` contract captures a typed owned Node hierarchy and creates independent detached instances. Typed scene-file persistence and file-based change/reload execute under ADR 0013; registered persistent typed connections execute under ADR 0010. Programmatic Node authoring metadata and flattened editable-owner selection execute under ADRs 0008/0023. Linked runtime templates and complete selected overrides execute under ADR 0023. Root inheritance, non-Disabled edit modes, live scripting attachment and full editor UI remain prerequisites. Ordinary parenting and packing remain the composition model.

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

Last updated: 2026-10-02

- Status: Accepted
- Scope: Reusable countdown nodes, internal Node processing, and scaled/original frame timing
- Builds on: [0008](scene.md#adr-0008), [0011](scene.md#adr-0011), [0016](core-object-runtime.md#adr-0016), and [0014](resources.md#adr-0014)

### Context

Electron2D already provides a lightweight `SceneTreeTimer` for one-shot deferred work, but reusable scenes also need a configurable countdown that participates in Node hierarchy, pause, packing, and lifecycle semantics. Its ignore-time-scale option cannot be implemented correctly if Engine supplies only the scaled delta: a zero time scale erases the elapsed duration. Reusing public `ProcessEnabled` would also make engine behavior depend on whether a game enables or disables its own process callback.

The pinned 4.7.2 stable [`Timer` class](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Timer.xml), [`timer.h`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/timer.h), [`timer.cpp`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/timer.cpp), and [`main.cpp`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/main/main.cpp) were audited with the complete Godot `Node` and `Object` inheritance chain (mapped to Node and ElectronObject). Runtime expiry tests strict negativity, despite prose describing the countdown as reaching its end. Both Timer callback lanes subtract `Engine::get_process_step()` when scaling is ignored; the Engine publishes that process step before its physics catch-up cap adjusts the delivered idle delta. The earlier Electron2D exact-zero expiry and fixed physics-step adaptation are withdrawn to match this runtime behavior.

The engine is typed C#, uses direct frame traversal for hot paths, and forbids inert compatibility stubs. The solution therefore has to integrate with existing Node/SceneTree/MainLoop/Engine flow, preserve allocation-free warmed frames, and expose no separate task or clock scheduler.

### Decision

- Add `Timer : Node` and the shared `ProcessPhase` enum to the Scene tree component.
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

Last updated: 2026-10-18

- Status: Accepted
- Scope: Frame-driven interpolation sequences, typed tween tasks, and SceneTree scheduling
- Builds on: [0002](product.md#adr-0002), [0006](scene.md#adr-0006), [0008](scene.md#adr-0008), [0010](core-object-runtime.md#adr-0010), [0014](resources.md#adr-0014), and [0016](core-object-runtime.md#adr-0016)

### Context

Games need deterministic property animation, interpolated callbacks, delays, event waits, nested timelines, and sequencing before a renderer or editor exists. The current stable [`Tween`](https://docs.godotengine.org/en/stable/classes/class_tween.html) and tweener APIs, their complete inheritance chains, and the 4.7.2 stable [`tween.h`](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/animation/tween.h), [`tween.cpp`](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/animation/tween.cpp), and [`easing_equations.h`](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/animation/easing_equations.h) were audited. Their dynamic values, callables, signal handles, string property paths, and reference-counted lifetime conflict with Electron2D's accepted typed C# and managed-lifetime contracts.

SceneTree already owns owner-thread frame ordering, scaled/original deltas, pause-aware Node policy, reusable scheduler buffers, deferred work, and typed event connections. A separate animation clock, task scheduler, reflection layer, or dynamic container would duplicate those facilities and weaken deterministic ordering.

### Decision

- Add `Tween : ElectronObject`, its four nested policy/curve enums, abstract `Tweener`, and concrete property, method, callback, interval, subtween, and event-wait tweeners under `src/Scene/Animation/` in `Electron2D.dll`.
- Tweens are created only through `SceneTree.CreateTween()` or `Node.CreateTween()`. SceneTree owns valid top-level registration; Node creation also binds pause policy and lifetime. Node creation selects its attached tree first. For a detached node, the application scene tree is the existing `Engine.MainLoop` when it is a `SceneTree`, including startup/shutdown visibility under ADR 0016. This is the typed explicit-host projection of the pinned `Node::create_tween` application-tree fallback; independently constructed library trees do not elect an implicit process singleton. With no eligible tree the call throws before registration. A detached binding waits for entry into the selected tree; attachment to another tree neither transfers nor advances it. Explicit BindNode rejects a node already owned by another tree. Owner-thread and terminal-tree guards apply before work is published.
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
