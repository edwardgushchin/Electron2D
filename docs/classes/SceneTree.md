# SceneTree

Last updated: 2026-10-07

**Inherits:** [MainLoop](MainLoop.md)

**Inherited By:** —

- **Source:** [SceneTree.cs](../../src/Scene/Main/SceneTree.cs), [SceneTree.TextInput.cs](../../src/Scene/Main/SceneTree.TextInput.cs), [SceneTree.GUIDrag.cs](../../src/Scene/Main/SceneTree.GUIDrag.cs), [SceneTree.GUIHover.cs](../../src/Scene/Main/SceneTree.GUIHover.cs), [SceneTree.SceneChange.cs](../../src/Scene/Main/SceneTree.SceneChange.cs), [SceneTree.Physics.cs](../../src/Scene/Main/SceneTree.Physics.cs), [SceneTree.PhysicsInterpolation.cs](../../src/Scene/Main/SceneTree.PhysicsInterpolation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed partial class SceneTree : MainLoop`

> Owns one active node hierarchy and coordinates its lifecycle, input, frames, groups, timers, tweens, and deferred work.

AnimatedSprite uses the existing internal idle lane and tree pause/process policy. Worker SpriteFrames changes remain pending until owner-thread reconciliation; they do not dispatch scene callbacks from a worker. No new public scheduling API is introduced.

## Prepared skeletal path bindings

An internal PathRevision increments before tree-change and rename callback delivery. Skeleton/Polygon/LookAt weak path bindings observe that revision, so membership or a failed public callback cannot preserve a stale path. Existing scene-owner mutation and notification behavior remains the binding authority.

## Description

Owns one active node hierarchy and coordinates its lifecycle, input, frames, groups, timers, tweens, and deferred work.

`SceneTree` is the concrete [`MainLoop`](MainLoop.md) that owns one active root [`Node`](Node.md) hierarchy. An optional `CurrentScene` selects one direct child; in-memory scene changes keep the root alive, remove the old scene immediately, and enter the new scene at a deferred safe point. It establishes lifecycle and owner-thread boundaries, accepts direct frame calls or scheduling through [`Engine`](Engine.md), propagates typed input and system notifications, manages pause state, reusable Node [`Timer`](Timer.md) scheduling, lightweight tree timers, [`Tween`](Tween.md) sequences, typed group operations, deferred actions, and queued deletion, and finalizes the complete hierarchy.

The tree registers its existing physics world with [PhysicsServer](PhysicsServer.md) when the first body, Area or [Joint](Joint.md) enters, or a CanvasItem asks for [World](World.md). Its fixed physics lane steps that same space; `World.Space` and its direct query view have stable identities until tree teardown. The lane prepares scene bodies and pin/groove constraints and spring impulses before the solver. A query prepares pending shape/pose edits even before the first fixed step.

An enabled [RayCast](RayCast.md) samples its cached result through Node's internal physics callback before the backend step. Scene pause/process eligibility and physics priority govern this callback; a forced update can query immediately from the owner thread, including while automatic sampling is disabled. A ray result is held between samples.

Before entry, a root still set to `NodeAutoTranslateMode.Inherit` samples `ProjectSettings.RootNodeAutoTranslate` and becomes `Always` or `Disabled`. Each automatically translating node receives `NotificationTranslationChanged` during entry; the setting is not re-read for an active tree.

`PhysicsInterpolation` samples its typed project setting at construction. Physics frames snapshot eligible canvas and camera transforms before and after callbacks; rendering uses `Engine.PhysicsInterpolationFraction` without altering logical transforms or input coordinates. The flag can be changed on the owner thread, resetting display history.

The creating thread becomes the owner thread for scene mutation, frame execution, flushing, and disposal.
Electron2D does not create a frame-pump thread. A host can drive the loop through [`Engine.AdvanceFrame(Double)`](Engine.md#m-electron2d-engine-advanceframe-system-double),
call [`MainLoop.Process(Double)`](MainLoop.md#m-electron2d-mainloop-process-system-double) and [`MainLoop.PhysicsProcess(Double)`](MainLoop.md#m-electron2d-mainloop-physicsprocess-system-double) directly, or use this class's wrappers.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var root = new Node { Name = "Root" };
using var tree = new SceneTree(root);
tree.ChangeSceneToNode(new Entity { Name = "Level" });
tree.ProcessFrame(1.0 / 60.0);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public SceneTree(Node root)`](#m-electron2d-scenetree-ctor-electron2d-node) | Creates and immediately activates a scene tree rooted at `root`. |

## Properties

| Member | Description |
| --- | --- |
| [`public bool DebugPathsHint { get; set; }`](#diagnostics-debugpathshint) | Gets or sets whether paths draw their curves and tangent direction markers. |
| [`public Node? EditedSceneRoot { get; set; }`](#diagnostics-editedsceneroot) | Gets or selects the root of the scene whose configuration warnings are being inspected. |
| [`public bool AutoAcceptQuit { get; set; }`](#p-electron2d-scenetree-autoacceptquit) | True by default. |
| [`public Node Root { get; }`](#p-electron2d-scenetree-root) | Gets the root node owned by this tree. |
| [`public Node? CurrentScene { get; set; }`](#p-electron2d-scenetree-currentscene) | Gets or selects an existing direct scene child of the root. |
| [`public int NodeCount { get; }`](#p-electron2d-scenetree-nodecount) | Gets the number of nodes currently inside this tree. |
| [`public bool Paused { get; set; }`](#p-electron2d-scenetree-paused) | Gets or sets whether pause-aware processing and timers are paused. |
| [`public bool PhysicsInterpolation { get; set; }`](#p-electron2d-scenetree-physicsinterpolation) | Enables 2D presentation between fixed physics ticks. |

## Methods

| Member | Description |
| --- | --- |
| [`public void Quit(int exitCode = 0)`](#m-electron2d-scenetree-quit-system-int32) | Atomically requests exit from any thread, without immediate disposal or process termination. |
| [`public void Defer(Action action)`](#m-electron2d-scenetree-defer-system-action) | Thread-safely queues an action for a future deferred flush while the tree remains live. |
| [`public void ChangeSceneToNode(Node node)`](#m-electron2d-scenetree-changescenetonode-electron2d-node) | Replaces the selected scene with a detached node at a deferred safe point. |
| [`public void ChangeSceneToPacked(PackedScene packedScene)`](#m-electron2d-scenetree-changescenetopacked-electron2d-packedscene) | Instantiates and schedules a packed scene. |
| [`public void UnloadCurrentScene()`](#m-electron2d-scenetree-unloadcurrentscene) | Disposes the selected scene immediately. |
| [`public void SetDeferred<T>(Action<T> setter, T value)`](#m-electron2d-scenetree-setdeferred-1-system-action-0-0) | Thread-safely queues a typed setter invocation for a future deferred flush. |
| [`public SceneTreeTimer CreateTimer(double timeSeconds, bool processAlways = true, bool processInPhysics = false, bool ignoreTimeScale = false)`](#m-electron2d-scenetree-createtimer-system-double-system-boolean-system-boolean-system-boolean) | Creates a one-shot timer owned and processed by this tree. |
| [`public Tween CreateTween()`](#m-electron2d-scenetree-createtween) | Creates a valid tween processed by this tree. |
| [`public IReadOnlyList<Tween> GetProcessedTweens()`](#m-electron2d-scenetree-getprocessedtweens) | Returns the tweens currently registered for processing. |
| [`public void ProcessFrame(double delta)`](#m-electron2d-scenetree-processframe-system-double) | Runs one host-driven process frame, process timers, process tweens, and one deferred safe point. |
| [`public void PhysicsFrame(double delta)`](#m-electron2d-scenetree-physicsframe-system-double) | Runs physics callbacks, body/joint simulation and area monitoring before physics timers and tweens. |
| [`public void SetInputAsHandled()`](#m-electron2d-scenetree-setinputashandled) | Marks the input event currently being dispatched as handled. |
| [`public bool IsInputHandled()`](#m-electron2d-scenetree-isinputhandled) | Gets whether the input event currently being dispatched has been handled. |
| [`public IReadOnlyList<Node> GetNodesInGroup(string group)`](#m-electron2d-scenetree-getnodesingroup-system-string) | Returns every current node in a group in depth-first pre-order. |
| [`public Node GetFirstNodeInGroup(string group)`](#m-electron2d-scenetree-getfirstnodeingroup-system-string) | Returns the first current node in a group using depth-first pre-order. |
| [`public int GetNodeCountInGroup(string group)`](#m-electron2d-scenetree-getnodecountingroup-system-string) | Gets the number of current nodes in a group. |
| [`public bool HasGroup(string group)`](#m-electron2d-scenetree-hasgroup-system-string) | Determines whether this tree currently contains a node in a group. |
| [`public void CallGroup(string group, Action<Node> action, GroupCallFlags flags = GroupCallFlags.Default)`](#m-electron2d-scenetree-callgroup-system-string-system-action-electron2d-entity-electron2d-groupcallflags) | Invokes a typed action for each current node in a group. |
| [`public void SetGroup<T>(string group, Action<Node, T> setter, T value, GroupCallFlags flags = GroupCallFlags.Default)`](#m-electron2d-scenetree-setgroup-1-system-string-system-action-electron2d-entity-0-0-electron2d-groupcallflags) | Applies a typed value through a setter for each current node in a group. |
| [`public void NotifyGroup(string group, int notification, GroupCallFlags flags = GroupCallFlags.Default)`](#m-electron2d-scenetree-notifygroup-system-string-system-int32-electron2d-groupcallflags) | Delivers a numeric notification to each current node in a group. |
| [`public void QueueDelete(ElectronObject instance)`](#m-electron2d-scenetree-queuedelete-electron2d-electronobject) | Thread-safely queues an engine object for deterministic disposal at a future deletion phase. |
| [`public void FlushDeferred()`](#m-electron2d-scenetree-flushdeferred) | Executes one captured deferred-action batch followed by one captured deletion batch. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-scenetree-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override bool OnProcess(double delta)`](#m-electron2d-scenetree-onprocess-system-double) | Performs one variable-step frame. |
| [`protected override bool OnPhysicsProcess(double delta)`](#m-electron2d-scenetree-onphysicsprocess-system-double) | Performs one fixed-step physics frame. |
| [`protected override void OnNotification(int what)`](#m-electron2d-scenetree-onnotification-system-int32) | Handles an engine notification delivered to this object. |
| [`protected override void ValidateFinalization()`](#m-electron2d-scenetree-validatefinalization) | Validates derived finalization preconditions before the loop enters its terminal state. |
| [`protected override void ValidateDisposal()`](#m-electron2d-scenetree-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected override void OnFinalize()`](#m-electron2d-scenetree-onfinalize) | Releases resources acquired by a successfully initialized loop. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<SceneTree, Node>? NodeConfigurationWarningChanged`](#diagnostics-nodeconfigurationwarningchanged) | Occurs when a node in EditedSceneRoot's subtree requests a configuration-warning refresh. |
| [`public event Action<SceneTree, Node> NodeAdded`](#e-electron2d-scenetree-nodeadded) | Occurs after a node enters this tree. |
| [`public event Action<SceneTree, Node> NodeRemoved`](#e-electron2d-scenetree-noderemoved) | Occurs after a node exits this tree. |
| [`public event Action<SceneTree, Node> NodeRenamed`](#e-electron2d-scenetree-noderenamed) | Occurs after an active node is renamed. |
| [`public event Action<SceneTree> ProcessFrameStarted`](#e-electron2d-scenetree-processframestarted) | Occurs before the idle transform-delivery phase and eligible node process callbacks. |
| [`public event Action<SceneTree> PhysicsFrameStarted`](#e-electron2d-scenetree-physicsframestarted) | Occurs after pending transform delivery and before eligible node physics-process callbacks. |
| [`public event Action<SceneTree> TreeChanged`](#e-electron2d-scenetree-treechanged) | Occurs after the active hierarchy is structurally changed or an active node is renamed. |
| [`public event Action<SceneTree>? SceneChanged`](#e-electron2d-scenetree-scenechanged) | Occurs after a pending scene enters successfully. |

## Constructor Descriptions

<a id="m-electron2d-scenetree-ctor-electron2d-node"></a>
### `public SceneTree(Node root)`

Creates and immediately activates a scene tree rooted at `root`.

**Parameters**

- `root`: A live, detached, parentless node that is not queued for deletion.

**Exceptions**

- `ArgumentNullException`: `root` is `null`.
- `ArgumentException`: `root` has a parent, belongs to a tree, or is queued for deletion.
- `InvalidOperationException`: Construction is attempted from a scene factory, the root is being captured or instantiated, or an inactive Window is supplied.
- `ObjectDisposedException`: Disposal of `root` has started.
- `AggregateException`: Activation or rollback callbacks fail.

**Remarks:** Construction enters the hierarchy parent-first, dispatches post-enter notifications after each node's
descendants, and then delivers ready child-first. Lifecycle callbacks and event handlers run synchronously.
If activation fails, the tree first stops accepting work, every attached node is exited, ready state consumed
by this attempt is restored, created timers are disposed, created tweens are invalidated, queued work is discarded,
and the supplied hierarchy remains owned by the caller. A reference captured from an activation callback observes
a terminal disposed tree.

## Property Descriptions

<a id="p-electron2d-scenetree-currentscene"></a>
### `public Node? CurrentScene { get; set; }`

The selected direct child of the stable `Root`, or null initially. Assigning an existing direct child changes only the selection; it does not add, remove or dispose nodes. Detachment clears the selection before `NodeRemoved`. Selection and inspection require the owner thread and a live tree; assigning a detached, foreign or non-direct node throws `ArgumentException`.

<a id="diagnostics-debugpathshint"></a>
### `public bool DebugPathsHint { get; set; }`

Gets or sets whether paths draw their curves and tangent direction markers.

Value: False initially.

Contract: Changes invalidate all attached Path nodes, including hidden ones. Drawing uses the existing canvas pipeline, visibility and transforms. Color is sampled from ProjectSettings.DebugPathsColor at tree construction. This optional diagnostic works in all build configurations; it creates no editor.

ObjectDisposedException: The tree is finalized or disposed.

InvalidOperationException: The caller is not the scene owner thread.


<a id="diagnostics-editedsceneroot"></a>
### `public Node? EditedSceneRoot { get; set; }`

Gets or selects the root of the scene whose configuration warnings are being inspected.

Value: Null initially. A nonnull value must be a live node in this tree; this tree's Root is allowed.

Contract: This is a borrowed tooling selection, independent of packed-scene ownership. It enables warning change events only for the selected subtree, without enabling an editor or changing processing. Exiting the tree clears the selection before NodeRemoved. Selection itself emits no warning-change event.

ArgumentException: The selected node belongs to another tree or is detached.

ObjectDisposedException: The tree or selected node is disposed.

InvalidOperationException: The caller is not the scene owner thread.


<a id="p-electron2d-scenetree-autoacceptquit"></a>
### `public bool AutoAcceptQuit { get; set; }`

True by default. The root Window raises CloseRequested, then requests quit if this property remains true and quit is not already requested. A handler may disable it or request its own exit code. Read/write requires the owner thread and a live tree; closed trees throw ObjectDisposedException.

<a id="p-electron2d-scenetree-root"></a>
### `public Node Root { get; }`

Gets the root node owned by this tree.

**Value:** The immutable root reference. Tree finalization exits and recursively disposes this hierarchy.

<a id="p-electron2d-scenetree-nodecount"></a>
### `public int NodeCount { get; }`

Gets the number of nodes currently inside this tree.

**Value:** The current hierarchy size, including [`SceneTree.Root`](SceneTree.md#p-electron2d-scenetree-root), or zero after finalization releases the hierarchy.

**Exceptions**

- `InvalidOperationException`: The property is read from a thread other than the owner thread.
- `ObjectDisposedException`: The tree is disposing on another thread or has finished disposing.

<a id="p-electron2d-scenetree-paused"></a>
### `public bool Paused { get; set; }`

Gets or sets whether pause-aware processing and timers are paused.

**Value:** `false` by default.

**Exceptions**

- `ObjectDisposedException`: The setter is used after finalization, or tree disposal has started or finished.
- `InvalidOperationException`: The setter is called from another thread or a pause callback requests the opposite state.
- `AggregateException`: One or more node notification handlers fail.

**Remarks:** On an actual change, the new state is stored first and the hierarchy is synchronously traversed depth-first.
Each node's child snapshot is taken only after that node's notification returns, so re-entrant hierarchy changes
can affect the remainder of the same traversal. A live attached node is notified at most once even when it is
reparented, and removed or disposed candidates are skipped. Notification failures do not roll the state back and
are collected after traversal completes.

<a id="p-electron2d-scenetree-physicsinterpolation"></a>
### `public bool PhysicsInterpolation { get; set; }`

Samples `ProjectSettings.PhysicsInterpolation` at construction, false by default. When enabled, each physics frame retains the previous and current canvas transforms for nodes whose inherited `PhysicsInterpolationMode` resolves On. The renderer samples that history using `Engine.PhysicsInterpolationFraction`; camera scroll from a current camera also interpolates. Changing the flag resets presentation snapshots without changing logical transforms. An attached caller must use the tree owner thread; notification failures aggregate after later descendants are attempted.

## Method Descriptions

<a id="m-electron2d-scenetree-changescenetonode-electron2d-node"></a>
### `public void ChangeSceneToNode(Node node)`

Accepts ownership of a live detached scene on the owner thread. The selected old scene exits immediately, remains alive until the next deferred safe point, then is disposed before the new scene enters. During that gap `CurrentScene` is null. Repeated requests before the safe point supersede and later dispose earlier pending nodes. A child with a conflicting name or an attached, queued, or already owned node is rejected before replacing the current scene. An exit callback error can be reported after a change has been accepted; the deferred change still runs.

<a id="m-electron2d-scenetree-changescenetopacked-electron2d-packedscene"></a>
### `public void ChangeSceneToPacked(PackedScene packedScene)`

Instantiates the in-memory template first, then applies `ChangeSceneToNode` to its detached root. An empty or failing template preserves the current scene. If the replacement is rejected before ownership transfers, the temporary instance is disposed. Scene files are not loaded here.

<a id="m-electron2d-scenetree-unloadcurrentscene"></a>
### `public void UnloadCurrentScene()`

Immediately disposes the selected scene and leaves other root children intact. A pending replacement remains scheduled. Disposal callback errors propagate after the node's teardown continues.

<a id="m-electron2d-scenetree-quit-system-int32"></a>
### `public void Quit(int exitCode = 0)`

Atomically requests exit from any thread, without immediate disposal or process termination. The latest accepted request supplies the exit code. Engine.Run observes quit before frames and during waits; the current frame completes. Manual Process/PhysicsProcess return true after a request; manual embedding still owns finalization. Requests after work acceptance closes throw ObjectDisposedException.

<a id="m-electron2d-scenetree-defer-system-action"></a>
### `public void Defer(Action action)`

Thread-safely queues an action for a future deferred flush while the tree remains live.

**Parameters**

- `action`: The action to invoke.

**Exceptions**

- `ArgumentNullException`: `action` is `null`.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

**Remarks:** An action queued during a captured flush batch waits for a later flush. Queue acceptance is atomic with the
start of tree finalization: a successful call is either captured by a future flush or intentionally discarded by
later finalization; a call that loses that race throws and does not enqueue.

<a id="m-electron2d-scenetree-setdeferred-1-system-action-0-0"></a>
### `public void SetDeferred<T>(Action<T> setter, T value)`

Thread-safely queues a typed setter invocation for a future deferred flush.

**Type parameters**

- `T`: The value type accepted by the setter.

**Parameters**

- `setter`: The setter to invoke.
- `value`: The value captured for the deferred invocation.

**Exceptions**

- `ArgumentNullException`: `setter` is `null`.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

**Remarks:** Has the same atomic lifetime and captured-batch behavior as [`SceneTree.Defer(Action)`](SceneTree.md#m-electron2d-scenetree-defer-system-action).

<a id="m-electron2d-scenetree-createtimer-system-double-system-boolean-system-boolean-system-boolean"></a>
### `public SceneTreeTimer CreateTimer(double timeSeconds, bool processAlways = true, bool processInPhysics = false, bool ignoreTimeScale = false)`

Creates a one-shot timer owned and processed by this tree.

**Parameters**

- `timeSeconds`: The finite non-negative delay in seconds.
- `processAlways`: Whether the timer advances while [`SceneTree.Paused`](SceneTree.md#p-electron2d-scenetree-paused) is true.
- `processInPhysics`: Whether the timer advances after physics callbacks instead of process callbacks.
- `ignoreTimeScale`: Whether to use the original lane delta when Engine drives the tree.

**Returns:** The live timer. It is automatically disposed after timeout delivery or when this tree is finalized.

**Exceptions**

- `ArgumentOutOfRangeException`: `timeSeconds` is negative, NaN, or infinite.
- `InvalidOperationException`: The method is called from a thread other than the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

**Remarks:** Timers are updated after node callbacks and before deferred work. A timer created during node callbacks can be
included in that frame's timer phase; a timer created by another timer waits for the next matching frame. Time
advances only from supplied frame deltas. When [`Engine`](Engine.md) drives the tree, timers normally use deltas scaled by
[`Engine.TimeScale`](Engine.md#p-electron2d-engine-timescale); `ignoreTimeScale` selects the original lane delta,
including at zero scale. Direct callers supply the same delta for both modes. There is no internal clock. Keeping a
managed reference does not keep an expired timer alive: timeout delivery is followed by deterministic disposal.
A zero duration expires during the next matching frame, not during this method call.

<a id="m-electron2d-scenetree-createtween"></a>
### `public Tween CreateTween()`

Creates a valid tween processed by this tree.

**Returns:** A running empty tween that starts on the next matching frame after tweeners are appended.

**Exceptions**

- `InvalidOperationException`: The method is called from a thread other than the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

**Remarks:** The tween is not bound to a node. It is advanced after node callbacks and lightweight timers and before deferred
work. A tween created during another tween's callback waits for the next matching frame.

<a id="m-electron2d-scenetree-getprocessedtweens"></a>
### `public IReadOnlyList<Tween> GetProcessedTweens()`

Returns the tweens currently registered for processing.

**Returns:** A read-only snapshot in creation order, including paused, stopped, just-finished, and killed tweens awaiting their next matching step.

**Exceptions**

- `InvalidOperationException`: The method is called from a thread other than the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

<a id="m-electron2d-scenetree-processframe-system-double"></a>
### `public void ProcessFrame(double delta)`

Runs one host-driven process frame, process timers, process tweens, and one deferred safe point.

**Parameters**

- `delta`: Elapsed process time in seconds; it must be finite and non-negative.

**Exceptions**

- `ArgumentOutOfRangeException`: `delta` is negative, NaN, or infinite.
- `InvalidOperationException`: The method is called off the owner thread, re-entered, called before initialization or after finalization, or called during node lifecycle or pause delivery.
- `ObjectDisposedException`: Tree disposal has started or finished.
- `AggregateException`: One or more frame events, node callbacks, timers, tweens, or deferred operations fail.

<a id="m-electron2d-scenetree-physicsframe-system-double"></a>
### `public void PhysicsFrame(double delta)`

Runs one host-driven physics-process frame, attached scene-body simulation and area monitoring, physics timers, physics tweens, and one deferred safe point. Current area field overlaps resolve gravity and damping, then stored body force, torque and kinematic targets apply before solver stepping. Body sleep/contact reports and area monitoring snapshots update after body synchronization; sleep and contact callbacks precede area events. Zero elapsed time leaves these paths unchanged.

**Parameters**

- `delta`: Elapsed physics-step time in seconds; it must be finite and non-negative.

**Exceptions**

- `ArgumentOutOfRangeException`: `delta` is negative, NaN, or infinite.
- `InvalidOperationException`: The method is called off the owner thread, re-entered, called before initialization or after finalization, or called during node lifecycle or pause delivery.
- `ObjectDisposedException`: Tree disposal has started or finished.
- `AggregateException`: One or more frame events, node callbacks, timers, tweens, or deferred operations fail.

**Remarks:** Attached rigid and static bodies step in an internal Box2D world after node physics callbacks and before timers, tweens and interpolation end capture. Zero delta leaves the world unchanged. Other physics services remain incomplete under [ADR 0054](../decisions/physics.md#adr-0054).

<a id="m-electron2d-scenetree-setinputashandled"></a>
### `public void SetInputAsHandled()`

Marks the input event currently being dispatched as handled.

**Exceptions**

- `InvalidOperationException`: No input event is currently being dispatched or the caller is not the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

**Remarks:** Handling stops the current stage immediately and skips every later input stage. The flag belongs only to the
active synchronous dispatch and is reset before the next event.

<a id="m-electron2d-scenetree-isinputhandled"></a>
### `public bool IsInputHandled()`

Gets whether the input event currently being dispatched has been handled.

**Returns:** `true` only after [`SceneTree.SetInputAsHandled`](SceneTree.md#m-electron2d-scenetree-setinputashandled) during active dispatch.

**Exceptions**

- `InvalidOperationException`: No input event is currently being dispatched or the caller is not the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

<a id="m-electron2d-scenetree-getnodesingroup-system-string"></a>
### `public IReadOnlyList<Node> GetNodesInGroup(string group)`

Returns every current node in a group in depth-first pre-order.

**Parameters**

- `group`: The nonblank, case-sensitive group name.

**Returns:** A read-only snapshot of matching nodes.

**Exceptions**

- `ArgumentException`: `group` is empty or consists only of whitespace.
- `ArgumentNullException`: `group` is `null`.
- `InvalidOperationException`: The method is called from a thread other than the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

<a id="m-electron2d-scenetree-getfirstnodeingroup-system-string"></a>
### `public Node GetFirstNodeInGroup(string group)`

Returns the first current node in a group using depth-first pre-order.

**Parameters**

- `group`: The nonblank, case-sensitive group name.

**Returns:** The first matching node, or `null` when the group has no current member.

**Exceptions**

- `ArgumentException`: `group` is empty or consists only of whitespace.
- `ArgumentNullException`: `group` is `null`.
- `InvalidOperationException`: The method is called from a thread other than the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

<a id="m-electron2d-scenetree-getnodecountingroup-system-string"></a>
### `public int GetNodeCountInGroup(string group)`

Gets the number of current nodes in a group.

**Parameters**

- `group`: The nonblank, case-sensitive group name.

**Returns:** The current number of matching nodes.

**Exceptions**

- `ArgumentException`: `group` is empty or consists only of whitespace.
- `ArgumentNullException`: `group` is `null`.
- `InvalidOperationException`: The method is called from a thread other than the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

<a id="m-electron2d-scenetree-hasgroup-system-string"></a>
### `public bool HasGroup(string group)`

Determines whether this tree currently contains a node in a group.

**Parameters**

- `group`: The nonblank, case-sensitive group name.

**Returns:** `true` when at least one current node belongs to the group.

**Exceptions**

- `ArgumentException`: `group` is empty or consists only of whitespace.
- `ArgumentNullException`: `group` is `null`.
- `InvalidOperationException`: The method is called from a thread other than the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

<a id="m-electron2d-scenetree-callgroup-system-string-system-action-electron2d-entity-electron2d-groupcallflags"></a>
### `public void CallGroup(string group, Action<Node> action, GroupCallFlags flags = GroupCallFlags.Default)`

Invokes a typed action for each current node in a group.

**Parameters**

- `group`: The nonblank, case-sensitive group name.
- `action`: The action to invoke for every eligible node.
- `flags`: Ordering and scheduling behavior.

**Exceptions**

- `ArgumentException`: `group` is blank or `flags` is invalid.
- `ArgumentNullException`: `group` or `action` is `null`.
- `InvalidOperationException`: An immediate operation is called off the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.
- `AggregateException`: One or more node callbacks fail during execution.

**Remarks:** Immediate operations require the owner thread. Deferred operations may be requested from any thread and execute
during a future flush. The selected nodes are captured when the operation executes; each is revalidated before
invocation. All selected callbacks are attempted before failures are reported. Deferred callback failures are
reported by the future flush or frame, not by this scheduling call.

<a id="m-electron2d-scenetree-setgroup-1-system-string-system-action-electron2d-entity-0-0-electron2d-groupcallflags"></a>
### `public void SetGroup<T>(string group, Action<Node, T> setter, T value, GroupCallFlags flags = GroupCallFlags.Default)`

Applies a typed value through a setter for each current node in a group.

**Type parameters**

- `T`: The value type accepted by the setter.

**Parameters**

- `group`: The nonblank, case-sensitive group name.
- `setter`: The typed setter to invoke for every eligible node.
- `value`: The value captured for this operation.
- `flags`: Ordering and scheduling behavior.

**Exceptions**

- `ArgumentException`: `group` is blank or `flags` is invalid.
- `ArgumentNullException`: `group` or `setter` is `null`.
- `InvalidOperationException`: An immediate operation is called off the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.
- `AggregateException`: One or more setter calls fail during execution.

**Remarks:** With [`GroupCallFlags.Unique`](GroupCallFlags.md#f-electron2d-groupcallflags-unique), the group and setter identify equality; differing values do not create
additional queued operations, and the first accepted value is retained. Immediate execution requires the owner
thread. Deferred execution may be requested from another thread, resolves membership when it starts, revalidates
each candidate, attempts every selected setter, and then aggregates failures through the future flush or frame.

<a id="m-electron2d-scenetree-notifygroup-system-string-system-int32-electron2d-groupcallflags"></a>
### `public void NotifyGroup(string group, int notification, GroupCallFlags flags = GroupCallFlags.Default)`

Delivers a numeric notification to each current node in a group.

**Parameters**

- `group`: The nonblank, case-sensitive group name.
- `notification`: The notification identifier.
- `flags`: Ordering and scheduling behavior.

**Exceptions**

- `ArgumentException`: `group` is blank or `flags` is invalid.
- `ArgumentNullException`: `group` is `null`.
- `InvalidOperationException`: An immediate operation is called off the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.
- `AggregateException`: One or more notification callbacks fail during execution.

**Remarks:** Immediate execution requires the owner thread. Deferred execution may be requested from another thread, resolves
membership when it starts, revalidates each candidate, attempts every selected notification, and then aggregates
failures through the future flush or frame. Equal deferred unique operations are coalesced by group and
notification identifier.

<a id="m-electron2d-scenetree-queuedelete-electron2d-electronobject"></a>
### `public void QueueDelete(ElectronObject instance)`

Thread-safely queues an engine object for deterministic disposal at a future deletion phase.

**Parameters**

- `instance`: The live object to dispose.

**Exceptions**

- `ArgumentNullException`: `instance` is `null`.
- `ArgumentException`: `instance` is this tree.
- `InvalidOperationException`: The object is this tree's root or is a node attached to another tree.
- `ObjectDisposedException`: The tree has been finalized, tree disposal has started or finished, or object disposal has started or finished.

**Remarks:** A node attached to this tree is detached before disposal. Detached objects are allowed. This method does not
provide cancellation; use [`Node.QueueFree`](Node.md#m-electron2d-node-queuefree) and [`Node.CancelFree`](Node.md#m-electron2d-node-cancelfree) for cancellable node
deletion.

<a id="m-electron2d-scenetree-flushdeferred"></a>
### `public void FlushDeferred()`

Executes one captured deferred-action batch followed by one captured deletion batch.

**Exceptions**

- `InvalidOperationException`: The method is called off the owner thread, re-entered, or called during node lifecycle or pause delivery.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.
- `AggregateException`: One or more actions or deletions fail.

**Remarks:** Every captured entry is attempted. Deferred actions queued after action capture wait for a later flush. The
deletion batch is captured after those actions, so deletion requested by a captured action runs in the same
flush. Re-entrant execution and execution during node lifecycle or pause delivery are rejected. Failures are
reported after both phases.

<a id="m-electron2d-scenetree-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

Appends this class's typed ownership, frame, queue, count, and pause descriptors.

<a id="m-electron2d-scenetree-onprocess-system-double"></a>
### `protected override bool OnProcess(double delta)`

Performs one variable-step frame.

**Parameters**

- `delta`: Elapsed frame time in seconds.

**Returns:** `true` to ask the host to stop; otherwise `false`.

**Remarks:** The default implementation does no work and returns `false`.

Runs the process-frame pipeline and returns whether Quit was requested.

<a id="m-electron2d-scenetree-onphysicsprocess-system-double"></a>
### `protected override bool OnPhysicsProcess(double delta)`

Performs one fixed-step physics frame.

**Parameters**

- `delta`: Elapsed fixed-step time in seconds.

**Returns:** `true` to ask the host to stop; otherwise `false`.

**Remarks:** The default implementation does no work and returns `false`.

Runs the physics-frame pipeline and returns whether Quit was requested.

<a id="m-electron2d-scenetree-onnotification-system-int32"></a>
### `protected override void OnNotification(int what)`

Handles an engine notification delivered to this object.

**Parameters**

- `what`: The notification identifier.

**Remarks:** Derived overrides should call the base implementation unless they intentionally suppress inherited handling.

System lifecycle notifications are propagated through a depth-first snapshot of the live hierarchy after
inherited handling. Removed or disposed candidates are skipped and callback failures are reported together.

<a id="m-electron2d-scenetree-validatefinalization"></a>
### `protected override void ValidateFinalization()`

Validates derived finalization preconditions before the loop enters its terminal state.

**Exceptions**

- `InvalidOperationException`: Construction is incomplete or execution is active.

**Remarks:** Overrides must be side-effect-free, throw when finalization is temporarily unsafe, and call the base implementation.

Rejects finalization during construction, a frame, input dispatch, a flush, lifecycle delivery, or pause delivery.

<a id="m-electron2d-scenetree-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Exceptions**

- `InvalidOperationException`: The caller is not the owner thread or execution is active.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

Requires the owner thread and rejects disposal re-entered from a frame, input, flush, lifecycle, or pause callback.

<a id="m-electron2d-scenetree-onfinalize"></a>
### `protected override void OnFinalize()`

Releases resources acquired by a successfully initialized loop.

**Remarks:** The callback runs once on the owner thread after the last frame and before object disposal completes.

Atomically closes the work queues, exits and recursively disposes the root, disposes active timers, invalidates
active tweens, clears event subscribers, and attempts every teardown stage before reporting collected failures.

## Event Descriptions

<a id="e-electron2d-scenetree-scenechanged"></a>
### `public event Action<SceneTree>? SceneChanged`

Receives this tree after a pending scene has entered and completed ready delivery. The handler can read `CurrentScene`. Failed attachment emits no event; event handler failures propagate from the deferred flush without undoing an attached scene.

<a id="diagnostics-nodeconfigurationwarningchanged"></a>
### `public event Action<SceneTree, Node>? NodeConfigurationWarningChanged`

Occurs when a node in EditedSceneRoot's subtree requests a configuration-warning refresh.

Contract: Arguments are this tree and the requesting node. Delivery is synchronous on the owner thread, without automatic warning evaluation or deduplication. A throwing subscriber stops later subscribers. A consumer queries Node.GetConfigurationWarnings; no scene dock or editor UI is created.


<a id="e-electron2d-scenetree-nodeadded"></a>
### `public event Action<SceneTree, Node> NodeAdded`

Occurs after a node enters this tree.

**Remarks:** The first argument is this tree and the second is the entering node. Delivery is synchronous after the node's
own enter event and before descendant entry. A throwing subscriber stops later subscribers of this event
invocation, but the failure is aggregated after remaining lifecycle work.

<a id="e-electron2d-scenetree-noderemoved"></a>
### `public event Action<SceneTree, Node> NodeRemoved`

Occurs after a node exits this tree.

**Remarks:** The node's [`Node.Tree`](Node.md#p-electron2d-node-tree) is already `null` when handlers run. Delivery is child-first;
a throwing subscriber stops later subscribers of this event invocation, but the failure is aggregated after
remaining exit work.

<a id="e-electron2d-scenetree-noderenamed"></a>
### `public event Action<SceneTree, Node> NodeRenamed`

Occurs after an active node is renamed.

**Remarks:** Path-change notifications and the node's own renamed event run first. [`SceneTree.TreeChanged`](SceneTree.md#e-electron2d-scenetree-treechanged) is still
attempted when this event invocation fails, and both failures are aggregated. A throwing subscriber prevents
later subscribers of this event invocation from running.

<a id="e-electron2d-scenetree-processframestarted"></a>
### `public event Action<SceneTree> ProcessFrameStarted`

Occurs before the idle transform-delivery phase and eligible node process callbacks.

**Remarks:** A throwing subscriber stops later subscribers of this invocation; node callbacks, timers, tweens, and the deferred safe point are still attempted.

<a id="e-electron2d-scenetree-physicsframestarted"></a>
### `public event Action<SceneTree> PhysicsFrameStarted`

Occurs after pending transform delivery and before eligible node physics-process callbacks.

**Remarks:** A throwing subscriber stops later subscribers of this invocation; node callbacks, timers, tweens, and the deferred safe point are still attempted.

<a id="e-electron2d-scenetree-treechanged"></a>
### `public event Action<SceneTree> TreeChanged`

Occurs after the active hierarchy is structurally changed or an active node is renamed.

**Remarks:** Delivery is synchronous after the corresponding state change. A throwing subscriber prevents later subscribers of that invocation; the state change is not rolled back.

## Inherited API

Public and protected members inherited from [MainLoop](MainLoop.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Complete protected API

| Member | Current behavior |
| --- | --- |
| `OnProcess(double)` / `OnPhysicsProcess(double)` | Map inherited loop frames to the existing scene frame pipelines and return `false` |
| `OnNotification(int)` | Calls inherited handling and propagates system notifications depth-first to live attached nodes, aggregating callback failures |
| `ValidateFinalization()` | Rejects finalization before successful construction or during frame, flush, lifecycle, or pause callbacks |
| `ValidateDisposal()` | Requires the creating thread and rejects disposal re-entered from a frame, flush, node-lifecycle, or pause callback |
| `GetPropertyDescriptors()` | Appends typed root, queue, frame-count, node-count, and pause descriptors to inherited descriptors |
| `OnFinalize()` | Closes queues atomically, exits and disposes the root, disposes all timers, invalidates all tweens, clears subscribers, calls inherited finalization, and aggregates teardown failures after every stage is attempted |

The class is sealed, so these overrides document lifetime behavior rather than extension points.

## Frame, timer, and deferred flow

A valid inherited or wrapper frame increments its lane counter, delivers transforms around the matching frame event as described below, captures the then-current hierarchy in a reusable buffer, orders candidates by the lane's priority and captured pre-order, and revalidates membership, lifetime, pause eligibility, and public-or-internal enable state before every callback. Engine-internal node processing runs before the same node's independently enabled public callback. Failures are retained while later callbacks/phases are attempted; detachment or disposal during the internal phase skips that node's public phase.

Reusable `Timer` nodes advance inside node processing and can select Engine's original process step in either lane. Matching `SceneTreeTimer` instances are captured afterward and can select the original delta of that lane. A tree timer created by a node callback may therefore advance in that frame; a tree timer created by another tree timer waits for the next matching frame. Expired tree timers are removed, notify synchronously, and are disposed even when a timeout handler fails. Matching Tweens are then captured and processed in creation order; a tween created by a node or tree-timer callback can enter that frame, while one created by another tween waits. Tweens may select the original Engine delta. Deferred actions then run from one captured batch. A nested `Defer` waits for a later flush. The deletion batch is captured after actions, so deletion requested by a captured action runs in the same flush. All phase failures are flattened into one `AggregateException`.

Frame and flush execution cannot be re-entered and cannot begin during node lifecycle or pause delivery. Calling `Dispose`, `FinalizeLoop`, any frame entry point, or `FlushDeferred` from frame, flush, lifecycle, or pause callbacks is rejected before queue consumption or tree lifetime changes; the outer operation continues and reports the failure.

## Input propagation

For a Viewport root, input dispatch first removes the viewport final transform unless PushInput specifies local coordinates. Positional copies are borrowed during callbacks and disposed afterward, including failures; non-positional/local input retains identity. Execution/re-entry guards run before conversion. Node-root trees retain the original event path. See [Viewport.PushInput](Viewport.md#pushinput) and [coordinate tests](../../tests/Electron2D.Tests/CanvasCoordinateTests.cs).

`Input.ParseInputEvent` first asks the active SceneTree to validate its owner thread and execution barrier, commits raw/action state only after that succeeds, and then dispatches through MainLoop's internal boundary. The tree captures pre-order once and visits the snapshot in reverse for Node stages. It runs `Node.OnInput`, then root viewport `Control.OnGUIInput` and `GUIInput` for hit pointer or focused keyboard/controller input, then root viewport `ui_*` focus navigation when unhandled, then `Node.OnShortcutInput` for key, gamepad-button and direct shortcut events, then for key events `Node.OnUnhandledKeyInput`, then `Node.OnUnhandledInput`. Root GUI focus is stored by the tree and exposed through Viewport.GetGUIFocusOwner. Transfer sends the previous Control's exit notification/event, the viewport's GUIFocusChanged event, then the new Control's enter notification/event; explicit release sends only exit. Each Node stage requires its matching enable flag and current `CanProcess()` eligibility. `SetInputAsHandled()` stops immediately. Removed, moved-to-another-tree, disposed, disabled, or newly added candidates are handled by snapshot revalidation. GUI targets are rechecked for visibility, membership and CanProcess eligibility. Touch captures are independent by contact index and mouse captures retain all pressed button bits. User callback failures are aggregated after other eligible callbacks run; committed Input state is not rolled back. Dispatch is owner-thread and non-reentrant; navigation searches the live Control hierarchy.

The same root GUI stage now owns one typed drag state: it accumulates left-held motion past `Viewport.GUIDragThreshold`, asks the captured Control/ancestors for a borrowed [DragPayload](DragPayload.md), then checks the hit target/ancestors in local coordinates. A temporary internal top-layer CanvasLayer owns the optional preview; active motion updates preview/cursor and left release/press attempts delivery. Right press, `ui_cancel`, hidden source and explicit viewport cancellation end without success. `Node.NotificationDragBegin` sees the active payload; `NotificationDragEnd` sees the committed result and cleared payload. Drop/notification failures continue cleanup. Scene activation rollback closes an already-started preview before hierarchy exit. [GUIDragTests](../../tests/Electron2D.Tests/GUIDragTests.cs) covers these transitions; [GUIDragRenderingTests](../../tests/Electron2D.Tests/GUIDragRenderingTests.cs) checks GPU/compatibility pixels and cursor. Nested/cross-window routing remains absent.

Native committed text is a separate owner-thread scene delivery, distinct from key and action state. The Window forwards each complete SDL text commit to the eligible focused Control as one string. A native composition update arrives after DisplayServer commits its IME text/selection, propagates `NotificationOsImeUpdate` across the root hierarchy and then delivers the typed preedit to that Control. Failed notifications do not skip focused delivery; a throwing virtual text hook does not suppress the later event. No key event is fabricated, and no SDL text is associated with a previous key press. Root text dispatch rejects reentry and skips hidden, detached or non-processing focus. [TextDeliveryTests](../../tests/Electron2D.Tests/TextDeliveryTests.cs) and [TextDeliveryNativeTests](../../tests/Electron2D.Tests/TextDeliveryNativeTests.cs) verify managed phase/failure behavior and native Linux Wayland GPU/compatibility routing. Nested viewports, per-key native Unicode and visible IME candidate UI remain separate gaps.

## Lifecycle and failure safety

Construction rejects a root still under packed-scene instantiation and rejects every `SceneTree` activation attempted while a packed-scene node factory is executing; neither case begins enter callbacks or transfers ownership. Otherwise enter is parent-first, post-enter follows descendant entry, ready is child-first, and exit is child-first. Each lifecycle stage attempts all applicable callbacks and events before reporting errors. Constructor activation failure first closes acceptance, terminally marks any escaped tree reference, exits attached nodes, resets ready flags, disposes activation-created timers, invalidates activation-created tweens, and leaves the hierarchy live and caller-owned. Entry/ready snapshots revalidate membership. Removing, reparenting, or disposing the node whose lifecycle is in progress is rejected, as is re-entrant child escape from a disposing parent. Side effects outside owned membership, ready state, timers, tweens, and queues are not reversible.

Pause traversal revalidates lifetime and membership before every notification. An opposite transition requested by a pause callback is rejected, so the outer transition retains one coherent state.

Explicit finalization or normal disposal first closes queue acceptance under the same lock used by cross-thread enqueue. Work accepted before closure is intentionally discarded; work racing after closure receives `ObjectDisposedException`. Exit, recursive node disposal, timer disposal, tween invalidation, queue cleanup, subscriber cleanup, and inherited finalization are all attempted even when earlier callbacks fail. `FinalizeLoop()` leaves the tree object alive but terminal after releasing ownership; `Dispose()` also publishes the final disposed state. Callback failure never permits a finalization retry.

System notifications `2009..2020` are snapshotted in depth-first order, revalidated before delivery, and attempted for every live attached node before failures are aggregated. Native creation of those notifications and focus-driven Input state changes remain outside SceneTree.

## Group semantics

Group names are nonblank and ordinal case-sensitive. Immediate group operations require the owner thread. Deferred group operations may be requested from another thread and resolve current membership when their queued callback begins. Candidate nodes are then revalidated before each invocation. Reverse order is the exact reverse of hierarchy pre-order. `Unique` is valid only with `Deferred`; equality uses operation kind, group, and delegate or notification identifier, ignores setter values, and retains the first accepted value.

## Threading guarantees and non-guarantees

The creating thread owns lifecycle, hierarchy reads and mutation, input dispatch/handled state, pause mutation, immediate group operations, timer/tween creation, mutation and disposal, frames, flushes, and tree disposal. `Defer`, `SetDeferred`, deferred group operations, `QueueDelete`, and `Node.QueueFree` are cross-thread request boundaries. Typed event receipt by `AwaitTweener` may also originate elsewhere, but continuation occurs on the owner thread. Their acceptance is serialized with disposal. User game state, event subscription, timer/tween reads, and node reads are not made thread-safe by the tree.

## Dependencies and interactions

The class depends on [`MainLoop`](MainLoop.md), typed [`InputEvent`](InputEvent.md) values, `Node`, `ProcessMode`, [`Timer`](Timer.md), [`SceneTreeTimer`](SceneTreeTimer.md), [`Tween`](Tween.md), [`GroupCallFlags`](GroupCallFlags.md), reusable scheduler/input/timer/tween lists, concurrent queues, and a single queue-lifetime lock. `Node` supplies input/internal lanes plus the construction/factory barriers that keep [`PackedScene`](PackedScene.md) reconstruction detached. Core's [`Engine`](Engine.md) can drive the tree through the base contract, and [`EventConnection`](EventConnection.md) supports deferred delivery and typed tween waits. Root-window rendering is invoked through the internal RenderingServer after scene processing; SDL stays behind the native backend. The lazily owned physics space shares PhysicsServer identity; audio, asset loader/serializer, networking and editor services retain separate dependencies.

## Verification and remaining limits

`tests/Electron2D.Tests/Program.cs` covers constructor validation, inherited-loop initialization/driving/finalization, Engine attachment/zero-delta scheduling/finalization, three-stage input ordering/handled state/re-entry/failure continuation/allocation, system-notification propagation, escaped-reference terminal state, timer/tween cleanup, and enter/ready rollback; stale lifecycle snapshots; lifecycle and tree-event order; exception-safe teardown and queued deletion; cross-tree deletion transfer; lifecycle execution barriers; pause re-entry/traversal/execution barriers; exiting/pre-delete/cleanup ownership guards; 256 concurrent QueueFree/flush iterations; a 64-iteration concurrent enqueue/disposal stress check; frame counters/events; public/internal process ordering, failure continuation, pause eligibility, and scaled/original deltas; group operations and invalid flags; both timer facilities; complete typed tween sequencing/lifetime/failure cases; generic queued object deletion; captured deferred batches; cancellation; recursive node disposal; and zero steady-state managed allocation across warmed idle, active-Timer, active-Tween, and non-positional/Node-root input paths. Positional viewport projections allocate temporary events.

[PhysicsInterpolationTests](../../tests/Electron2D.Tests/PhysicsInterpolationTests.cs) checks project-setting initialization, runtime toggles, eligible snapshots, first/repeated ticks, reset/pause, camera/Control policy, callback failure, and 128 warmed active ticks with zero managed allocation. [Native pixel checks](../../tests/Electron2D.Tests/PhysicsInterpolationNativeTests.cs) pass on dummy compatibility and Linux Wayland compatibility/GPU for moving items and camera scroll. Other platforms and visual owner acceptance remain unverified.

`SceneTree` itself has no automatic frame pump or elapsed-time source. Core [`Engine`](Engine.md) provides host-driven fixed-step accumulation, scaled/original delta delivery, time scaling, and the fraction consumed by 2D presentation, and Engine.Run supplies the window clock/pump and frame wait. [SceneChangeTests](../../tests/Electron2D.Tests/SceneChangeTests.cs) cover in-memory scene replacement, ownership, deferred entry, callback failures and cleanup. Further import/remapping rules, multithreaded renderer synchronization, complete GUI input routing, wider physics server/area/joint APIs, loaded-scene performance benchmark, and exception logging remain absent. Root viewport GUI dispatch and hover are covered by [ControlInputTests](../../tests/Electron2D.Tests/ControlInputTests.cs) and [ControlHoverTests](../../tests/Electron2D.Tests/ControlHoverTests.cs); clipping, stationary-pointer geometry changes, keyboard navigation, exact renderer order and nested viewports remain. Allocation checks cover warmed empty and small active-Timer/Tween/input hierarchies, not large-scene performance; concurrency checks are local stress tests rather than formal proofs or platform-wide performance evidence. Input hardware gaps use ADR 0038's exact triggers.

## Related decision

- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)

## Diagnostic consumer example

This managed example uses `using Electron2D;` and a caller-defined Node override for custom warning strings. It requires no editor window.

```csharp
using var root = new Node();
var follower = new PathFollow();
root.AddChild(follower);
using var tree = new SceneTree(root);
tree.EditedSceneRoot = root;
tree.NodeConfigurationWarningChanged += (_, node) =>
{
    foreach (var warning in node.GetConfigurationWarnings())
        Console.WriteLine(warning);
};
follower.UpdateConfigurationWarnings(); // Warns about the missing direct Path parent.
tree.DebugPathsHint = true; // Attached Path nodes draw on the next canvas recording.
```

EditedSceneRoot and DebugPathsHint are independent transient tooling state, not PackedScene properties. A selected subtree is not reselected automatically after detachment/reentry. A selection may be the tree root or any attached descendant. Disposal closes diagnostics together with other tree operations and clears event subscribers. These runtime capabilities do not create editor UI, enable script tool mode, or implement accessibility/navigation/collision diagnostics. See [scene paths](../components/scene-paths.md) and [SceneDiagnosticsTests](../../tests/Electron2D.Tests/SceneDiagnosticsTests.cs).

## Canvas transform phases

Canvas transform notifications use dedicated owner-thread queues. Physics delivers pending entries before PhysicsFrameStarted. Idle delivers after ProcessFrameStarted and again after node callbacks. Both lanes deliver after timers, tweens and the captured deferred-action batch, before queued deletion. Each pass follows pending-list order, capturing the next entry before invoking a callback. Reentrant additions behind an existing successor can be reached in that pass; an addition from the current tail waits for another pass. Cancellation advances the saved cursor before unlinking a pending entry, so force, exit and disposal cannot strand later items. Callback failures are aggregated after the other pending entries and later frame stages are attempted. Explicit FlushDeferred remains an action/deletion flush, not a transform flush. ForceUpdateTransform selects one item inside or outside a frame under the same execution barrier.

## Canvas render time

RenderCanvas forwards the captured scaled process step inside the existing scene execution barrier. Tree pause does not suppress the renderer clock; TimeScale zero supplies a zero step. Canvas callbacks retain the same mutation, failure and lifetime guards. See [canvas timing](../components/canvas-rendering.md#animation-intervals-and-rectangles).

## Reused captured action batches

SceneTree.Defer and deferred group operations enqueue under the existing lifetime/work lock. Two action queues swap roles when a nonempty batch is captured; the owner drains the captured queue outside the lock and recycles its capacity. Work enqueued by a callback or another thread after capture waits for another flush. Later capacity growth and user callbacks can allocate. [BoxContainerTests](../../tests/Electron2D.Tests/BoxContainerTests.cs) verifies next-batch/cross-thread semantics and zero bytes over 64 prepared batch cycles, alongside the existing concurrent disposal and lifetime tests. Queued deletions retain their separate ownership mechanism.

Root GUI hover traversal reuses warmed candidate and prior-chain buffers, including nested callback depth. Positional event delivery still creates separately owned temporary localized InputEvent resources under ADR 0038. The button input check measures 180,736 managed bytes for 64 routed pointer cycles, equal to the isolated three-copy baseline; button/group state, shortcut routing and hover-only work are measured separately at zero. See [GUI buttons](../components/gui-buttons.md).

Shared [scene/server joint resources](../components/physics-joints.md) use the existing physics world and native body IDs. Stable joint identities, typed settings, pending connection lifetime and independent contact/motion exception contributions now execute under [ADR 0087](../decisions/physics-joints.md#adr-0087).

[Physics world activity](PhysicsServer.md#activity) is now independent of scene scheduling under [ADR 0089](../decisions/physics-activity.md#adr-0089). SceneTree activates its lazily created world; explicit SpaceCreate defaults inactive and requires SpaceSetActive(true). Global/local false skips simulation, force consumption and solver callbacks without clearing native state or accumulating elapsed time. Queries/configuration/cleanup and scene callbacks/timers continue. [PhysicsActivityTests](../../tests/Electron2D.Tests/PhysicsActivityTests.cs) checks the profile and warmed allocation on Linux/.NET 10.


The process-frame path collects pending native bus effect/gain failures before running its usual callbacks. It attempts ordinary frame dispatch even when those failures exist, then reports the combined error. Each failed effect reports once and remains silent until structural recreation; native exceptions never cross the callback boundary. AudioEffectTests and its failed public Engine.Run host verify this integration.

Embedded GUI integration is implemented in [SceneTree.GUIState.cs](../../src/Scene/Main/SceneTree.GUIState.cs) and [ViewportGUIState](ViewportGUIState.md). The tree owns a viewport-state map and restores a prepared value scope after synchronous callbacks. Focus/hover/capture/input snapshots/tooltips stay independent; connected sections borrow shared drag state. Only direct container forwarding nests scene input. Parent snapshots and handled-owner state survive child dispatch and failure. Detachment attempts GUI and base lifetime cleanup even after callback errors.

## Scene multiplayer integration

Nearest-branch assignment, automatic idle polling and typed RPC/authority configuration execute through [the component](../components/scene-multiplayer.md). RPC method tokens/codecs replace string/reflection invocation; configure derived nodes in their construction/factory for PackedScene reconstruction. Authority/configuration changes remain local and are not automatically replicated. API/transport ownership remains borrowed for caller-supplied interfaces.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean MultiplayerPoll { get; set; }` | Gets or sets automatic process-frame multiplayer polling. |

## Property Descriptions

<a id="member-04597d844662"></a>
### MultiplayerPoll

`public System.Boolean MultiplayerPoll { get; set; }`

Gets or sets automatic process-frame multiplayer polling.

Value: True initially; polling occurs after the process-frame event and before node callbacks, including while paused.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.MultiplayerAPI GetMultiplayer(System.String forPath = "")` | Gets a branch-specific override or the tree's default multiplayer interface. |
| `public System.Void SetMultiplayer(Electron2D.MultiplayerAPI multiplayer, System.String rootPath = "")` | Assigns a borrowed interface to the default or an existing absolute scene branch. |

## Method Descriptions

<a id="member-82c74a431a2e"></a>
### GetMultiplayer

`public Electron2D.MultiplayerAPI GetMultiplayer(System.String forPath = "")`

Gets a branch-specific override or the tree's default multiplayer interface.

forPath: Absolute branch path; empty selects default. The most specific ancestor override wins.

Returns: A borrowed live interface.

<a id="member-af3cf6cc6239"></a>
### SetMultiplayer

`public System.Void SetMultiplayer(Electron2D.MultiplayerAPI multiplayer, System.String rootPath = "")`

Assigns a borrowed interface to the default or an existing absolute scene branch.

multiplayer: Live owner-thread interface; null removes a custom branch or creates a new default.

rootPath: Empty selects default; an absolute existing node path selects a branch.

Remarks: One interface can belong to only one tree/branch. Replacement detaches the old interface; only tree-created defaults are disposed by the tree.

Branch replacement now rebinds existing spawner/synchronizer configurations after committing the new interface, and continues all rebind/cleanup stages after observer failure. Removing an existing custom mapping remains possible after its branch node has left the scene. Native replication tests exercise real idle-frame spawn/state/visibility flow; no rendered/editor acceptance is inferred.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

## File integration API additions

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void ChangeSceneToFile(System.String path)` | Loads a typed file scene and replaces the current scene at its ordinary deferred safe point. |
| `public System.Void ReloadCurrentScene()` | Reloads the selected scene from its source file without reusing old file content. |

## Method Descriptions

<a id="member-b73111695a75"></a>
### ChangeSceneToFile

`public System.Void ChangeSceneToFile(System.String path)`

Loads a typed file scene and replaces the current scene at its ordinary deferred safe point.

Existing cached templates remain borrowed. A newly loaded temporary template releases its graph owner after the new scene acquires its lease.

- `path`: Scene path or UID.

<a id="member-eba9d9f5c7b1"></a>
### ReloadCurrentScene

`public System.Void ReloadCurrentScene()`

Reloads the selected scene from its source file without reusing old file content.

Requires a current scene with a file path. Decode failure preserves the current scene.

Embedded Window routing descends recursively to the final focused or pointer target, releasing intermediate transformed events. Command-menu hover uses the shared canvas-order/clipping hit test beneath the active popup. MenuButtonTests verifies nested Window command/choice input and related hover gates.

FileDialog regression checks exercise teardown with an active embedded-window tooltip. CancelTooltip clears its presenter state while a closing tree relies on recursive root ownership for attached layer disposal, avoiding a new deletion request after work queues close. Detached tooltip layers retain direct cleanup.

## Viewport world integration

[Canvas and physics worlds](../components/worlds.md) documents World.Canvas, Viewport.World/FindWorld, nearest-viewport CanvasItem access, shared rendering, independent physics, membership changes and runtime lifetime. Existing server and native kernels remain the implementation path. [WorldTests](../../tests/Electron2D.Tests/WorldTests.cs) supplies direct behavior and actual target-pixel evidence.

## Navigation map integration

World.NavigationMap now lazily owns an active borrowed map in the same runtime lifetime as canvas/physics. Scene NavigationRegion nodes and server-owned regions use that same map storage; the physics lane commits staged topology. NavigationServer is available through Engine named-service lookup. [The navigation contract](../components/navigation-maps.md) records implemented behavior and remaining dependencies.

Viewport World replacement also rebinds direct Entity-parent NavigationAgent map memberships through the retained navigation service. These nonspatial nodes participate without being CanvasItem types; nested viewports retain their own selection boundary.
