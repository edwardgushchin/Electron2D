# SceneTree

Last updated: 2026-09-22

**Inherits:** [MainLoop](MainLoop.md)

**Inherited By:** —

- **Source:** [`src/Scene/Main/SceneTree.cs`](../../src/Scene/Main/SceneTree.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class SceneTree : MainLoop`

> Owns one active node hierarchy and coordinates its lifecycle, input, frames, groups, timers, tweens, and deferred work.

## Description

Owns one active node hierarchy and coordinates its lifecycle, input, frames, groups, timers, tweens, and deferred work.

`SceneTree` is the concrete [`MainLoop`](MainLoop.md) that owns one active root [`Node`](Node.md) hierarchy. It establishes lifecycle and owner-thread boundaries, accepts direct frame calls or scheduling through [`Engine`](Engine.md), propagates typed input and system notifications, manages pause state, reusable Node [`Timer`](Timer.md) scheduling, lightweight tree timers, [`Tween`](Tween.md) sequences, typed group operations, deferred actions, and queued deletion, and finalizes the complete hierarchy.

The creating thread becomes the owner thread for scene mutation, frame execution, flushing, and disposal.
Electron2D does not create a frame-pump thread. A host can drive the loop through [`Engine.AdvanceFrame(Double)`](Engine.md#m-electron2d-engine-advanceframe-system-double),
call [`MainLoop.Process(Double)`](MainLoop.md#m-electron2d-mainloop-process-system-double) and [`MainLoop.PhysicsProcess(Double)`](MainLoop.md#m-electron2d-mainloop-physicsprocess-system-double) directly, or use this class's wrappers.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var root = new Node { Name = "Root" };
using var tree = new SceneTree(root);
tree.ProcessFrame(1.0 / 60.0);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public SceneTree(Node root)`](#m-electron2d-scenetree-ctor-electron2d-node) | Creates and immediately activates a scene tree rooted at `root`. |

## Properties

| Member | Description |
| --- | --- |
| [`public bool AutoAcceptQuit { get; set; }`](#p-electron2d-scenetree-autoacceptquit) | True by default. |
| [`public Node Root { get; }`](#p-electron2d-scenetree-root) | Gets the root node owned by this tree. |
| [`public int NodeCount { get; }`](#p-electron2d-scenetree-nodecount) | Gets the number of nodes currently inside this tree. |
| [`public bool Paused { get; set; }`](#p-electron2d-scenetree-paused) | Gets or sets whether pause-aware processing and timers are paused. |

## Methods

| Member | Description |
| --- | --- |
| [`public void Quit(int exitCode = 0)`](#m-electron2d-scenetree-quit-system-int32) | Atomically requests exit from any thread, without immediate disposal or process termination. |
| [`public void Defer(Action action)`](#m-electron2d-scenetree-defer-system-action) | Thread-safely queues an action for a future deferred flush while the tree remains live. |
| [`public void SetDeferred<T>(Action<T> setter, T value)`](#m-electron2d-scenetree-setdeferred-1-system-action-0-0) | Thread-safely queues a typed setter invocation for a future deferred flush. |
| [`public SceneTreeTimer CreateTimer(double timeSeconds, bool processAlways = true, bool processInPhysics = false)`](#m-electron2d-scenetree-createtimer-system-double-system-boolean-system-boolean) | Creates a one-shot timer owned and processed by this tree. |
| [`public Tween CreateTween()`](#m-electron2d-scenetree-createtween) | Creates a valid tween processed by this tree. |
| [`public IReadOnlyList<Tween> GetProcessedTweens()`](#m-electron2d-scenetree-getprocessedtweens) | Returns the valid tweens currently registered for processing. |
| [`public void ProcessFrame(double delta)`](#m-electron2d-scenetree-processframe-system-double) | Runs one host-driven process frame, process timers, process tweens, and one deferred safe point. |
| [`public void PhysicsFrame(double delta)`](#m-electron2d-scenetree-physicsframe-system-double) | Runs one host-driven physics-process frame, physics timers, physics tweens, and one deferred safe point. |
| [`public void SetInputAsHandled()`](#m-electron2d-scenetree-setinputashandled) | Marks the input event currently being dispatched as handled. |
| [`public bool IsInputHandled()`](#m-electron2d-scenetree-isinputhandled) | Gets whether the input event currently being dispatched has been handled. |
| [`public IReadOnlyList<Node> GetNodesInGroup(string group)`](#m-electron2d-scenetree-getnodesingroup-system-string) | Returns every current node in a group in depth-first pre-order. |
| [`public Node GetFirstNodeInGroup(string group)`](#m-electron2d-scenetree-getfirstnodeingroup-system-string) | Returns the first current node in a group using depth-first pre-order. |
| [`public int GetNodeCountInGroup(string group)`](#m-electron2d-scenetree-getnodecountingroup-system-string) | Gets the number of current nodes in a group. |
| [`public bool HasGroup(string group)`](#m-electron2d-scenetree-hasgroup-system-string) | Determines whether this tree currently contains a node in a group. |
| [`public void CallGroup(string group, Action<Node> action, GroupCallFlags flags = GroupCallFlags.Default)`](#m-electron2d-scenetree-callgroup-system-string-system-action-electron2d-node-electron2d-groupcallflags) | Invokes a typed action for each current node in a group. |
| [`public void SetGroup<T>(string group, Action<Node, T> setter, T value, GroupCallFlags flags = GroupCallFlags.Default)`](#m-electron2d-scenetree-setgroup-1-system-string-system-action-electron2d-node-0-0-electron2d-groupcallflags) | Applies a typed value through a setter for each current node in a group. |
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
| [`public event Action<SceneTree, Node> NodeAdded`](#e-electron2d-scenetree-nodeadded) | Occurs after a node enters this tree. |
| [`public event Action<SceneTree, Node> NodeRemoved`](#e-electron2d-scenetree-noderemoved) | Occurs after a node exits this tree. |
| [`public event Action<SceneTree, Node> NodeRenamed`](#e-electron2d-scenetree-noderenamed) | Occurs after an active node is renamed. |
| [`public event Action<SceneTree> ProcessFrameStarted`](#e-electron2d-scenetree-processframestarted) | Occurs immediately before eligible node process callbacks are captured and invoked. |
| [`public event Action<SceneTree> PhysicsFrameStarted`](#e-electron2d-scenetree-physicsframestarted) | Occurs immediately before eligible node physics-process callbacks are captured and invoked. |
| [`public event Action<SceneTree> TreeChanged`](#e-electron2d-scenetree-treechanged) | Occurs after the active hierarchy is structurally changed or an active node is renamed. |

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

## Method Descriptions

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

<a id="m-electron2d-scenetree-createtimer-system-double-system-boolean-system-boolean"></a>
### `public SceneTreeTimer CreateTimer(double timeSeconds, bool processAlways = true, bool processInPhysics = false)`

Creates a one-shot timer owned and processed by this tree.

**Parameters**

- `timeSeconds`: The finite non-negative delay in seconds.
- `processAlways`: Whether the timer advances while [`SceneTree.Paused`](SceneTree.md#p-electron2d-scenetree-paused) is true.
- `processInPhysics`: Whether the timer advances after physics callbacks instead of process callbacks.

**Returns:** The live timer. It is automatically disposed after timeout delivery or when this tree is finalized.

**Exceptions**

- `ArgumentOutOfRangeException`: `timeSeconds` is negative, NaN, or infinite.
- `InvalidOperationException`: The method is called from a thread other than the owner thread.
- `ObjectDisposedException`: The tree has been finalized, or disposal has started or finished.

**Remarks:** Timers are updated after node callbacks and before deferred work. A timer created during node callbacks can be
included in that frame's timer phase; a timer created by another timer waits for the next matching frame. Time
advances only from supplied frame deltas. When [`Engine`](Engine.md) drives the tree, those deltas include its
[`Engine.TimeScale`](Engine.md#p-electron2d-engine-timescale); direct callers control scaling themselves. There is no internal clock. Keeping a
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

Returns the valid tweens currently registered for processing.

**Returns:** A read-only snapshot in creation order, including paused and stopped tweens.

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

Runs one host-driven physics-process frame, physics timers, physics tweens, and one deferred safe point.

**Parameters**

- `delta`: Elapsed physics-step time in seconds; it must be finite and non-negative.

**Exceptions**

- `ArgumentOutOfRangeException`: `delta` is negative, NaN, or infinite.
- `InvalidOperationException`: The method is called off the owner thread, re-entered, called before initialization or after finalization, or called during node lifecycle or pause delivery.
- `ObjectDisposedException`: Tree disposal has started or finished.
- `AggregateException`: One or more frame events, node callbacks, timers, tweens, or deferred operations fail.

**Remarks:** This callback lane does not perform collision or rigid-body simulation.

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

<a id="m-electron2d-scenetree-callgroup-system-string-system-action-electron2d-node-electron2d-groupcallflags"></a>
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

<a id="m-electron2d-scenetree-setgroup-1-system-string-system-action-electron2d-node-0-0-electron2d-groupcallflags"></a>
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

Occurs immediately before eligible node process callbacks are captured and invoked.

**Remarks:** A throwing subscriber stops later subscribers of this invocation; node callbacks, timers, tweens, and the deferred safe point are still attempted.

<a id="e-electron2d-scenetree-physicsframestarted"></a>
### `public event Action<SceneTree> PhysicsFrameStarted`

Occurs immediately before eligible node physics-process callbacks are captured and invoked.

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

A valid inherited or wrapper frame increments its lane counter, raises the matching frame event, captures the then-current hierarchy in a reusable buffer, orders candidates by the lane's priority and captured pre-order, and revalidates membership, lifetime, pause eligibility, and public-or-internal enable state before every callback. Engine-internal node processing runs before the same node's independently enabled public callback. Failures are retained while later callbacks/phases are attempted; detachment or disposal during the internal phase skips that node's public phase.

Reusable `Timer` nodes advance inside node processing and can select Engine's original delta. Matching `SceneTreeTimer` instances are captured afterward. A tree timer created by a node callback may therefore advance in that frame; a tree timer created by another tree timer waits for the next matching frame. Expired tree timers are removed, notify synchronously, and are disposed even when a timeout handler fails. Matching Tweens are then captured and processed in creation order; a tween created by a node or tree-timer callback can enter that frame, while one created by another tween waits. Tweens may select the original Engine delta. Deferred actions then run from one captured batch. A nested `Defer` waits for a later flush. The deletion batch is captured after actions, so deletion requested by a captured action runs in the same flush. All phase failures are flattened into one `AggregateException`.

Frame and flush execution cannot be re-entered and cannot begin during node lifecycle or pause delivery. Calling `Dispose`, `FinalizeLoop`, any frame entry point, or `FlushDeferred` from frame, flush, lifecycle, or pause callbacks is rejected before queue consumption or tree lifetime changes; the outer operation continues and reports the failure.

## Input propagation

`Input.ParseInputEvent` first asks the active SceneTree to validate its owner thread and execution barrier, commits raw/action state only after that succeeds, and then dispatches through MainLoop's internal boundary. The tree captures pre-order once and visits the snapshot in reverse. It runs `Node.OnInput`, then for key events `Node.OnUnhandledKeyInput`, then `Node.OnUnhandledInput`. Each stage requires its matching enable flag and current `CanProcess()` eligibility. `SetInputAsHandled()` stops immediately. Removed, moved-to-another-tree, disposed, disabled, or newly added candidates are handled by snapshot revalidation. User callback failures are aggregated after other eligible callbacks run; committed Input state is not rolled back. Dispatch is owner-thread and non-reentrant and reuses buffers after warmup.

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

The class depends on [`MainLoop`](MainLoop.md), typed [`InputEvent`](InputEvent.md) values, `Node`, `NodeProcessMode`, [`Timer`](Timer.md), [`SceneTreeTimer`](SceneTreeTimer.md), [`Tween`](Tween.md), [`GroupCallFlags`](GroupCallFlags.md), reusable scheduler/input/timer/tween lists, concurrent queues, and a single queue-lifetime lock. `Node` supplies input/internal lanes plus the construction/factory barriers that keep [`PackedScene`](PackedScene.md) reconstruction detached. Core's [`Engine`](Engine.md) can drive the tree through the base contract, and [`EventConnection`](EventConnection.md) supports deferred delivery and typed tween waits. There is no SDL3-CS, native input backend, renderer, audio, collision-physics, asset loader/serializer, networking, or editor dependency.

## Verification and remaining limits

`tests/Electron2D.Tests/Program.cs` covers constructor validation, inherited-loop initialization/driving/finalization, Engine attachment/zero-delta scheduling/finalization, three-stage input ordering/handled state/re-entry/failure continuation/allocation, system-notification propagation, escaped-reference terminal state, timer/tween cleanup, and enter/ready rollback; stale lifecycle snapshots; lifecycle and tree-event order; exception-safe teardown and queued deletion; cross-tree deletion transfer; lifecycle execution barriers; pause re-entry/traversal/execution barriers; exiting/pre-delete/cleanup ownership guards; 256 concurrent QueueFree/flush iterations; a 64-iteration concurrent enqueue/disposal stress check; frame counters/events; public/internal process ordering, failure continuation, pause eligibility, and scaled/original deltas; group operations and invalid flags; both timer facilities; complete typed tween sequencing/lifetime/failure cases; generic queued object deletion; captured deferred batches; cancellation; recursive node disposal; and zero steady-state managed allocation across warmed idle, active-Timer, active-Tween, and input paths.

`SceneTree` itself has no automatic frame pump or elapsed-time source. Core [`Engine`](Engine.md) provides host-driven fixed-step accumulation, scaled/original delta delivery, time scaling, and interpolation state, and Engine.Run supplies the window clock/pump and frame wait. There is still no current-scene switching, renderer synchronization, GUI input routing, physics simulation, loaded-scene performance benchmark, or exception logger. Allocation checks cover warmed empty and small active-Timer/Tween/input hierarchies, not large-scene performance; concurrency checks are local stress tests rather than formal proofs or platform-wide performance evidence. Input hardware gaps use ADR 0038's exact triggers.

## Related decision

- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
