# Node

Last updated: 2026-09-21

## Declaration

- Source: [`Node.cs`](../../src/Scene/Main/Node.cs)
- Namespace: `Electron2D`
- Declaration: `public class Node : ElectronObject`
- Domain: [Scene](../domains/scene.md)
- Component: [Unified 2D node](../components/unified-node.md)

## Responsibility and ownership

`Node` is Electron2D's primary and single public hierarchical and spatial game-object base. Individual game objects, composed subsystems, and complete worlds use the same ordered Node hierarchy. Specialized gameplay objects derive from `Node` and can compose child Nodes and typed resources. It intentionally combines Godot-like `Node` and `Node2D` responsibilities: ordered ownership, lifecycle, paths, groups, pause-aware processing, deletion, local/global 2D transforms, visibility, and Z state. There is no separate `Node2D`, `GameObject`, or public entity hierarchy.

A parent owns its children. An active [`SceneTree`](SceneTree.md) owns its root and therefore the whole hierarchy. A node owns no renderer or native SDL handle. Its current transform surface is `Matrix3x2`; the standalone [`Transform2D`](Transform2D.md) now exists, but migration of this public surface remains a separate source-breaking slice under ADR 0026 and ADR 0029.

Any self-contained root and its owned descendants can be captured by [`PackedScene`](PackedScene.md) as a reusable scene. Instantiation returns an independent detached hierarchy; lifecycle begins only after explicit attachment to a `SceneTree`.

## Constants

| Constant | Value | Meaning |
| --- | ---: | --- |
| `NotificationEnterTree` | `10` | Entering an active tree |
| `NotificationExitTree` | `11` | Leaving an active tree |
| `NotificationReady` | `13` | SceneTree-managed ready delivery |
| `NotificationPaused` | `14` | Tree pause state became paused |
| `NotificationUnpaused` | `15` | Tree pause state became running |
| `NotificationPhysicsProcess` | `16` | Physics-process callback lane |
| `NotificationProcess` | `17` | Process callback lane |
| `NotificationParented` | `18` | Parent reference was assigned |
| `NotificationUnparented` | `19` | Parent reference was cleared |
| `NotificationSceneInstantiated` | `20` | This packed-scene root finished complete detached reconstruction |
| `NotificationPathRenamed` | `23` | This node or an ancestor changed path |
| `NotificationChildOrderChanged` | `24` | Direct child order changed |
| `NotificationPostEnterTree` | `27` | This node's descendants finished entering |
| `NotificationDisabled` | `28` | Effective process mode became `Disabled` |
| `NotificationEnabled` | `29` | Effective process mode stopped being `Disabled` |
| `NotificationVisibilityChanged` | `31` | Local/ancestor visibility propagation occurred |
| `NotificationLocalTransformChanged` | `35` | Local matrix changed and local notifications are enabled |
| `NotificationTransformChanged` | `2000` | Global transform changed and global notifications are enabled |
| `NotificationOsMemoryWarning` | `2009` | Propagated operating-system memory warning |
| `NotificationTranslationChanged` | `2010` | Translated messages may have changed |
| `NotificationWmAbout` | `2011` | Operating-system application-information request |
| `NotificationCrash` | `2012` | Unrecoverable crash is imminent |
| `NotificationOsImeUpdate` | `2013` | Input-method composition update |
| `NotificationApplicationResumed` | `2014` | Application resumed |
| `NotificationApplicationPaused` | `2015` | Application is about to suspend |
| `NotificationApplicationFocusIn` | `2016` | Keyboard focus gained |
| `NotificationApplicationFocusOut` | `2017` | Keyboard focus lost |
| `NotificationTextServerChanged` | `2018` | Active text service changed |
| `NotificationApplicationPipModeEntered` | `2019` | Picture-in-picture mode entered |
| `NotificationApplicationPipModeExited` | `2020` | Picture-in-picture mode exited |
| `MinimumZIndex` | `-4096` | Minimum accepted/effective Z index |
| `MaximumZIndex` | `4095` | Maximum accepted/effective Z index |

## Public state API

| Member | Current behavior |
| --- | --- |
| `Node()` | Initializes `Name` to the runtime class name and the transform to identity |
| `string Name { get; set; }` | Non-blank ordinal sibling key; rejects `.`, `..`, and `/`; renaming an active node propagates path notification |
| `Node? Parent { get; }` | Direct parent or `null` |
| `string SceneFilePath { get; }` | External packed-resource path on an instantiated scene root; empty for other nodes and built-in scenes |
| `Node? Owner { get; set; }` | Strict ancestor selecting this node for packed-scene storage, or `null`; a root never owns itself |
| `IReadOnlyList<Node> Children { get; }` | Live read-only view of ordered direct children |
| `int ChildCount { get; }` | Direct-child count |
| `SceneTree? Tree { get; }` | Active owner tree or `null` |
| `bool IsInsideTree { get; }` | Whether `Tree` is non-null |
| `bool IsNodeReady { get; }` | Whether SceneTree-managed ready has been consumed since construction or the last `RequestReady()`; the stored flag remains `true` after detachment |
| `bool IsQueuedForDeletion { get; }` | Atomic deletion-request state |
| `Matrix3x2 Transform { get; set; }` | Local 2D affine transform |
| `Matrix3x2 GlobalTransform { get; set; }` | World transform; setting it solves a local transform unless top-level |
| `Vector2 Position/GlobalPosition { get; set; }` | Local/global translation |
| `float Rotation/GlobalRotation { get; set; }` | Local/global rotation in radians |
| `float RotationDegrees/GlobalRotationDegrees { get; set; }` | Degree projections of local/global rotation |
| `Vector2 Scale/GlobalScale { get; set; }` | Canonical local/global scale decomposition |
| `float Skew/GlobalSkew { get; set; }` | Local/global angle between the transformed basis axes relative to an unskewed basis |
| `bool TopLevel { get; set; }` | Ignores parent transform while preserving the current global transform when toggled |
| `bool Visible { get; set; }` | Local logical visibility, default `true` |
| `bool IsVisibleInTree { get; }` | `true` only when active and every ancestor plus this node is visible |
| `int ZIndex { get; set; }` | Local Z value in `-4096..4095` |
| `bool ZAsRelative { get; set; }` | Whether effective Z accumulates ancestors, default `true` |
| `int EffectiveZIndex { get; }` | Relative accumulated or absolute Z, clamped to the supported range |
| `bool NotifyLocalTransformChanges { get; set; }` | Enables notification `35`; typed event delivery remains enabled |
| `bool NotifyTransformChanges { get; set; }` | Enables notification `2000`; typed event delivery remains enabled |
| `NodeProcessMode ProcessMode { get; set; }` | Pause policy, default `Inherit`; undefined enum values are rejected |
| `bool ProcessEnabled { get; set; }` | Explicit opt-in for `OnProcess`, default `false` |
| `bool PhysicsProcessEnabled { get; set; }` | Explicit opt-in for `OnPhysicsProcess`, default `false` |
| `int ProcessPriority { get; set; }` | Ascending process order key, default `0` |
| `int PhysicsProcessPriority { get; set; }` | Independent ascending physics-process order key, default `0` |
| `double ProcessDeltaTime { get; }` | Most recent SceneTree-managed process delta, initially `0`; Engine applies its time scale before Engine-driven delivery, and manual notification does not update it |
| `double PhysicsProcessDeltaTime { get; }` | Most recent SceneTree-managed physics-process delta, initially `0`; Engine applies its time scale before Engine-driven delivery, and manual notification does not update it |

`Name`, `Transform`'s scalar projections `Position`/`RotationDegrees`/`Scale`/`Skew`, `Visible`, `ZIndex`, `ZAsRelative`, `TopLevel`, `ProcessMode`, both process-enable flags, and both priorities are included in the typed property list and marked for packed-scene storage. Inherited `CanTranslateMessages` and `TranslationDomain` are stored as well. `Transform` itself and computed/global state are not descriptors. All inherited identity, notification, property, translation, and disposal API follows [`ElectronObject`](ElectronObject.md).

## Events

| Event | Delivery |
| --- | --- |
| `ChildAdded` / `ChildRemoved` | On the parent after structural attachment/removal; arguments are publishing parent then affected child |
| `ChildEnteredTree` / `ChildExitingTree` | On the direct parent when that child enters or begins exiting; arguments are publishing parent then affected child |
| `ChildOrderChanged` | On the parent after add, remove, or reorder |
| `Renamed` | On an active node after its own name changes |
| `TreeEntered` | After this node's enter notification, before descendants enter |
| `TreeExiting` | After descendants exit and this node receives exit notification, while `Tree` is still set |
| `TreeExited` | After `Tree` is cleared |
| `Ready` | After ready notification, once until `RequestReady()` |
| `VisibilityChanged` | On this node and descendants after visibility propagation |
| `LocalTransformChanged` | On every actual local matrix change |
| `TransformChanged` | On every affected global transform; propagation stops at top-level descendants |

Events are synchronous typed C# events. An event carrying only its source passes that source as its sole argument; child events use sender-first two-argument signatures. Exceptions propagate to the initiating operation unless `SceneTree` explicitly aggregates a frame/flush phase. [`EventConnection`](EventConnection.md) adds owned, one-shot, and deferred subscriptions without changing event signatures.

## Hierarchy, path, group, and lifetime methods

| Member | Current behavior |
| --- | --- |
| `AddChild(Node child)` | Appends a validated detached child and attaches its subtree if active |
| `AddSibling(Node sibling)` | Inserts immediately after this node; detached/root callers are rejected |
| `RemoveChild(Node child)` | Detaches a direct child and returns `true`, or returns `false` for a non-child |
| `MoveChild(Node child, int index)` | Reorders a direct child; negative indices count from the end |
| `MoveToFront()` | Moves this node to the last sibling position; detached/root nodes are unchanged |
| `Reparent(Node newParent, bool keepGlobalTransform = true)` | Moves a non-root node, including between trees when both owner-thread contracts are satisfied; by default preserves the global matrix and rejects singular new parents |
| `GetChild(int index)` | Gets a direct child; negative indices count from the end |
| `GetIndex()` | Returns the sibling index or `-1` without a parent |
| `IsAncestorOf(Node node)` | Tests strict ancestry |
| `FindChild(...)` / `FindChild<TNode>(...)` | Finds the first matching descendant in pre-order; `*` and `?` use case-insensitive wildcard matching |
| `FindChildren(...)` / `FindChildren<TNode>(...)` | Returns all case-insensitive wildcard matches in pre-order as a read-only snapshot |
| `FindParent(pattern)` | Finds the nearest case-insensitive wildcard-matching ancestor |
| `GetPath()` | Returns an absolute slash-separated path from the hierarchy root, including for detached hierarchies |
| `GetPathTo(Node node)` | Returns `.`, `..`, and names relative to another node sharing the same hierarchy root |
| `GetNode(string path)` / `GetNode<TNode>(string path)` | Resolves relative/absolute paths, including in detached hierarchies; throws when missing or of the wrong requested type |
| `GetNodeOrNull(string path)` | Nullable path resolution supporting `.`, `..`, and an optional root-name segment, including outside a `SceneTree` |
| `AddToGroup(string group, bool persistent = false)`, `RemoveFromGroup`, `IsInGroup` | Mutate/query non-blank ordinal string group membership; only persistent memberships are packed |
| `GetGroups()` | Returns a sorted read-only snapshot of group names |
| `CanProcess()` | Resolves inherited mode against current tree pause state; detached nodes return `false` |
| `RequestReady()` | Allows ready to be delivered on a later attachment; it does not emit ready immediately |
| `QueueFree()` | Atomically requests deletion at a future tree safe point; detached requests queue on later attachment, detachment after an active request does not cancel deletion, transfer to another tree transfers request consumption, and active root requests are rejected |
| `CancelFree()` | Atomically clears a request and reports whether one existed |

## Spatial and visibility methods

| Member | Current behavior |
| --- | --- |
| `Show()` / `Hide()` | Set local visibility |
| `ApplyScale(Vector2 ratio)` | Component-multiplies local scale |
| `Rotate(float radians)` | Adds local rotation |
| `Translate(Vector2 offset)` | Adds an offset rotated by the local rotation |
| `GlobalTranslate(Vector2 offset)` | Adds a world-space offset |
| `MoveLocalX/Y(float delta, bool scaled = false)` | Moves along a local basis axis; normalizes it unless `scaled` is `true` |
| `GetAngleTo(Vector2 globalPoint)` | Signed normalized angle from local +X/world rotation to a world point; coincident points return `0` |
| `LookAt(Vector2 globalPoint)` | Rotates local +X toward a distinct world point |
| `ToGlobal(Vector2 localPoint)` | Applies the global matrix |
| `ToLocal(Vector2 globalPoint)` | Applies its inverse; rejects a singular global transform |
| `GetRelativeTransformToParent(Node parent)` | Returns this global matrix relative to a strict ancestor, identity for self, and rejects unrelated/singular ancestors |

Every transform input must be finite. `Matrix3x2` follows the `System.Numerics` row-vector composition convention. Decomposition is canonical: equivalent matrices with reflections/negative scale can yield an equivalent but not identical rotation/scale/skew tuple.

`Matrix3x2` describes current executable behavior rather than the final transform-type decision. The standalone `Transform2D` value is implemented, but its planned Node migration has not occurred and must not be inferred from this class's present API.

## Protected API

| Member | Current behavior |
| --- | --- |
| `OnEnterTree()` / `OnExitTree()` / `OnReady()` | Virtual lifecycle callbacks mapped from notifications 10, 11, and 13 |
| `OnProcess(double delta)` | Virtual callback mapped from notification 17 after `ProcessDeltaTime` is stored |
| `OnPhysicsProcess(double delta)` | Virtual callback mapped from notification 16 after `PhysicsProcessDeltaTime` is stored |
| `CreateSceneInstanceFactory()` | Returns a static source-independent factory for a fresh exact-runtime-type default node; derived packable nodes must override it |
| `EnsureMutable()` | Required guard for derived stored-property setters; rejects disposal, capture mutation, and off-owner-thread attached mutation |
| `OnNotification(int what)` | Calls the base implementation and maps lifecycle/process IDs to the callbacks above |
| `GetPropertyDescriptors()` | Appends all documented node tooling descriptors to inherited descriptors |
| `ValidateDisposal()` | Rejects direct disposal of an active tree root and enforces owner-thread disposal while attached |
| `Dispose(bool disposing)` | Cancels deletion, detaches, attempts every child disposal, clears groups/events, calls the base override, and then aggregates failures |

## Lifecycle and state transitions

For SceneTree-managed attachment, enter is parent-first, post-enter follows descendant entry, and ready is child-first. Ready is one-shot unless `RequestReady()` is called before a later attachment. Exit is child-first. Lifecycle phases attempt all applicable node and tree events before aggregating failures; exit always clears membership. Constructor activation rollback additionally restores ready flags newly consumed by that attempt. Manual inherited `Notify(int)` calls the mapped callback on the caller's thread but does not change membership/readiness or raise the corresponding tree event.

Toggling `SceneTree.Paused` sends paused/unpaused notifications. A process-mode change that crosses effective `Disabled` sends disabled/enabled notification to the node and affected inheriting descendants. `ProcessFrame` and `PhysicsFrame` invoke only explicitly enabled nodes that remain live, attached, and eligible when their captured turn arrives. MainLoop system notifications `2009..2020` are propagated by the owning tree through a depth-first snapshot with lifetime and membership revalidation.

Direct disposal and queued deletion both detach an active node and attempt to dispose every member of its complete owned subtree. An instantiated packed-scene root additionally owns every resource duplicate created for that instance and disposes them after child-node cleanup. Cleanup failures are aggregated after structural state, child/resource lifetimes, groups, and subscribers reach their final state. The disposal thread may inspect node state from pre-delete and exit callbacks; other threads observe disposal as started and are rejected.

Packed capture marks the complete source hierarchy before reading factories or stored properties. While marked, hierarchy mutation, ordinary node/base-property mutation, disposal, `QueueFree`, and `CancelFree` are rejected. The mark is always released after success or failure. Packed-scene factory execution also carries an execution-context-local barrier: `SceneTree` construction and `Node.EnterTree` reject lifecycle activation until the factory returns, including attempts to attach into an existing active tree. Instantiation then restores stored properties before parenting, persistent groups before owner assignment, and resources before delivering `NotificationSceneInstantiated` to the root alone. It returns a detached hierarchy; later `SceneTree` enter/ready behavior is unchanged.

Every node created by `PackedScene.Instantiate()` is also marked unfinished until final topology validation completes. During that interval direct disposal and entry into any `SceneTree`, as either a root or an attached child, are rejected. A failed instantiation removes the mark before rollback so ordinary recursive disposal can reclaim the partial hierarchy.

## Invariants, errors, and threading

- Child names are ordinal-unique; invalid names, cycles, multiple parents, direct insertion of an already tree-attached child, disposed children, invalid indices, and unrelated path roots are rejected. `Reparent` is the supported cross-tree move operation when both owner-thread contracts are satisfied.
- An active `SceneTree` root can be destroyed only through `SceneTree.Dispose()`; direct disposal and `QueueFree()` are rejected without changing its lifetime.
- Structural steps and synchronous events are not transactional; an event/callback exception can occur after a documented state change. Implemented cleanup continues to a coherent endpoint rather than rolling the mutation back.
- Removing, reparenting, or disposing a node during its active enter/ready/exit delivery is rejected; recursive exit is ignored, and stale child/ready snapshots revalidate the originally expected tree membership.
- An exiting or disposing parent rejects removal/reparent operations from descendant lifecycle, pre-delete, and cleanup callbacks, so children cannot escape tree or node disposal ownership.
- Attached mutable state requires the `SceneTree` owner thread. Detached mutable state has no built-in synchronization.
- `Owner` is null or a strict ancestor. Removing/reparenting a subtree clears owner references that no longer point to an ancestor.
- Derived nodes are not packable by default. Their factory must be static, source-independent, and return a fresh live exact-type node; constructor-dependent state belongs in stored typed properties.
- A packed-scene node cannot enter an active tree or be disposed until its instantiation barrier is removed; final topology validation and rollback prevent callback-created hierarchy escape from surviving the operation.
- `QueueFree` and `CancelFree` are atomic request operations usable from other threads; actual deletion runs on the owner thread. Detachment does not cancel deletion, while transfer to another tree transfers consumption of the request.
- Access after disposal throws where the member checks lifetime. Simple relationship/status properties (`Parent`, `Children`, `Tree`, `IsInsideTree`, `IsNodeReady`, `IsQueuedForDeletion`) expose their final stored state directly.

## Dependencies and interactions

`Node` depends on `ElectronObject`, `MainLoop` notification identifiers, `PropertyDescriptor`, `NodeProcessMode`, `SceneTree`, the Resource base for owned scene duplicates, `System.Numerics`, LINQ, `FileSystemName`, and atomic operations. It does not depend on SDL3-CS, a renderer, input, audio, collision physics, scene file serialization, or a scripting runtime.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies lifecycle order, activation/ready rollback, stale snapshot rejection, lifecycle re-entry guards, failure-continuing exit and recursive disposal, disposing-parent mutation rejection, hierarchy validation, reparenting, owner cleanup, paths/search/persistent groups, packed capture and instantiation guards/factories/escape rollback/resource ownership, node/tree event order, child order and sender-first child event arguments, transform behavior, visibility and Z state, spatial helpers, pause modes/priorities/deltas, inherited disable/enable notifications, MainLoop system aliases and tree propagation, owner-thread rejection, direct disposal, detached/cross-tree queued deletion, and queued recursive disposal.

There is no renderer-backed canvas behavior, native system-event creation, focus-to-input state synchronization, ordinary input propagation, collision/rigid-body physics, scene file loader/saver, inherited/nested scene authoring, editable-instance metadata, persistent event endpoint schema, RPC/multiplayer, internal processing lane, process auto-enable by override detection, unique-name shorthand, or separate `Node2D`. Visibility and Z are currently logical state only. `Transform2D` exists independently, while migration from the current `Matrix3x2` members remains explicit future work.

## Relevant decisions

- [0008: Unified Node combines Node and Node2D](../decisions/scene.md#adr-0008)
- [0026: Separate Transform2D foundational type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform2D value and affine semantics](../decisions/core-math.md#adr-0029)
