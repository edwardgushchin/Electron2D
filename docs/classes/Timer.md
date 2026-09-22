# Timer

Last updated: 2026-09-23

**Inherits:** [Node](Node.md)

**Inherited By:** —

- **Source:** [`src/Scene/Main/Timer.cs`](../../src/Scene/Main/Timer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Timer : Node`

> Provides a reusable scene-node countdown timer.

## Description

Provides a reusable scene-node countdown timer.

`Timer` is a reusable Node-based countdown. It advances in one selected `SceneTree` frame lane, emits a typed timeout event when its remaining time reaches zero, and either stops or reloads. The parent Node or active tree owns it through ordinary hierarchy lifetime; the timer owns no thread, clock, task, or native handle.

Use [`SceneTreeTimer`](SceneTreeTimer.md) instead for a lightweight tree-owned one-shot delay that is not part of the Node hierarchy.

The timer advances at most once in its selected frame lane, emits [`Timer.Timeout`](Timer.md#e-electron2d-timer-timeout) when its remaining time
reaches zero, and either stops or reloads according to [`Timer.OneShot`](Timer.md#p-electron2d-timer-oneshot). It has no clock or background thread.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var timer = new Timer { WaitTime = 1.0, OneShot = true };
timer.Timeout += _ => Console.WriteLine("Finished");
timer.Start();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Timer()`](#m-electron2d-timer-ctor) | Initializes a stopped timer with a one-second wait in the process-frame lane. |

## Properties

| Member | Description |
| --- | --- |
| [`public TimerProcessCallback ProcessCallback { get; set; }`](#p-electron2d-timer-processcallback) | Gets or sets the frame lane that advances this timer. |
| [`public double WaitTime { get; set; }`](#p-electron2d-timer-waittime) | Gets or sets the countdown duration in seconds. |
| [`public bool OneShot { get; set; }`](#p-electron2d-timer-oneshot) | Gets or sets whether the timer stops after its next timeout. |
| [`public bool Autostart { get; set; }`](#p-electron2d-timer-autostart) | Gets or sets whether ready delivery starts the timer automatically. |
| [`public bool Paused { get; set; }`](#p-electron2d-timer-paused) | Gets or sets whether this timer's own countdown is paused. |
| [`public bool IgnoreTimeScale { get; set; }`](#p-electron2d-timer-ignoretimescale) | Gets or sets whether the countdown ignores [`Engine.TimeScale`](Engine.md#p-electron2d-engine-timescale). |
| [`public double TimeLeft { get; }`](#p-electron2d-timer-timeleft) | Gets the remaining countdown time in seconds. |

## Methods

| Member | Description |
| --- | --- |
| [`public bool IsStopped()`](#m-electron2d-timer-isstopped) | Gets whether the timer is stopped or has not started. |
| [`public void Start()`](#m-electron2d-timer-start) | Starts the timer using [`Timer.WaitTime`](Timer.md#p-electron2d-timer-waittime), or resets an already running countdown. |
| [`public void Start(double timeSeconds)`](#m-electron2d-timer-start-system-double) | Sets a new wait duration and starts or resets the timer. |
| [`public void Stop()`](#m-electron2d-timer-stop) | Stops the timer without emitting [`Timer.Timeout`](Timer.md#e-electron2d-timer-timeout). |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-timer-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#m-electron2d-timer-createsceneinstancefactory) | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| [`protected override void OnNotification(int what)`](#m-electron2d-timer-onnotification-system-int32) | Handles an engine notification delivered to this object. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-timer-dispose-system-boolean) | Releases resources owned by a derived class. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<Timer> Timeout`](#e-electron2d-timer-timeout) | Occurs when the countdown reaches zero. |

## Constructor Descriptions

<a id="m-electron2d-timer-ctor"></a>
### `public Timer()`

Initializes a stopped timer with a one-second wait in the process-frame lane.

## Property Descriptions

<a id="p-electron2d-timer-processcallback"></a>
### `public TimerProcessCallback ProcessCallback { get; set; }`

Gets or sets the frame lane that advances this timer.

**Value:** [`TimerProcessCallback.Idle`](TimerProcessCallback.md#f-electron2d-timerprocesscallback-idle) by default.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is undefined.
- `InvalidOperationException`: An attached timer is mutated off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** Changing the lane while running moves internal processing without resetting [`Timer.TimeLeft`](Timer.md#p-electron2d-timer-timeleft).

<a id="p-electron2d-timer-waittime"></a>
### `public double WaitTime { get; set; }`

Gets or sets the countdown duration in seconds.

**Value:** A finite value greater than zero; the default is one second.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is zero, negative, NaN, or infinite.
- `InvalidOperationException`: An attached timer is mutated off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** Changing the value does not alter the current countdown until the timer restarts or repeats.

<a id="p-electron2d-timer-oneshot"></a>
### `public bool OneShot { get; set; }`

Gets or sets whether the timer stops after its next timeout.

**Value:** `false` by default, causing automatic restart after each timeout.

**Exceptions**

- `InvalidOperationException`: An attached timer is mutated off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

<a id="p-electron2d-timer-autostart"></a>
### `public bool Autostart { get; set; }`

Gets or sets whether ready delivery starts the timer automatically.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: An attached timer is mutated off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** A successful automatic start resets this property to `false`. Setting it after ready delivery
does not start immediately; call [`Timer.Start`](Timer.md#m-electron2d-timer-start) or request another ready cycle before reattachment.

<a id="p-electron2d-timer-paused"></a>
### `public bool Paused { get; set; }`

Gets or sets whether this timer's own countdown is paused.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: An attached timer is mutated off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** Pausing preserves the remaining time. Starting a paused timer resets its countdown but does not resume it.
Scene-tree pause policy remains independently controlled by [`Node.ProcessMode`](Node.md#p-electron2d-node-processmode).

<a id="p-electron2d-timer-ignoretimescale"></a>
### `public bool IgnoreTimeScale { get; set; }`

Gets or sets whether the countdown ignores [`Engine.TimeScale`](Engine.md#p-electron2d-engine-timescale).

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: An attached timer is mutated off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** Engine-driven frames use their original finite elapsed delta when enabled. Direct [`SceneTree`](SceneTree.md) frame
calls have no separate scale and therefore use their supplied delta in either mode.

<a id="p-electron2d-timer-timeleft"></a>
### `public double TimeLeft { get; }`

Gets the remaining countdown time in seconds.

**Value:** The non-negative remaining time, or zero while stopped or after an overshooting repeat frame.

**Exceptions**

- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** The value is read-only. Use [`Timer.Start(Double)`](Timer.md#m-electron2d-timer-start-system-double) to change the duration and restart.

## Method Descriptions

<a id="m-electron2d-timer-isstopped"></a>
### `public bool IsStopped()`

Gets whether the timer is stopped or has not started.

**Returns:** `true` when no positive remaining time is observable; otherwise `false`.

**Exceptions**

- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

<a id="m-electron2d-timer-start"></a>
### `public void Start()`

Starts the timer using [`Timer.WaitTime`](Timer.md#p-electron2d-timer-waittime), or resets an already running countdown.

**Exceptions**

- `InvalidOperationException`: The timer is detached or called off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** Calling this method does not clear [`Timer.Paused`](Timer.md#p-electron2d-timer-paused).

<a id="m-electron2d-timer-start-system-double"></a>
### `public void Start(double timeSeconds)`

Sets a new wait duration and starts or resets the timer.

**Parameters**

- `timeSeconds`: The finite positive countdown duration in seconds.

**Exceptions**

- `ArgumentOutOfRangeException`: `timeSeconds` is zero, negative, NaN, or infinite.
- `InvalidOperationException`: The timer is detached or called off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** This typed overload replaces sentinel duration values. Calling it does not clear [`Timer.Paused`](Timer.md#p-electron2d-timer-paused).

<a id="m-electron2d-timer-stop"></a>
### `public void Stop()`

Stops the timer without emitting [`Timer.Timeout`](Timer.md#e-electron2d-timer-timeout).

**Exceptions**

- `InvalidOperationException`: An attached timer is mutated off its tree's owner thread.
- `ObjectDisposedException`: The timer is disposing on another thread or has finished disposing.

**Remarks:** The method is valid while detached and also clears [`Timer.Autostart`](Timer.md#p-electron2d-timer-autostart).

<a id="m-electron2d-timer-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

Appends countdown configuration, runtime pause, remaining-time, and frame-lane descriptors. Runtime pause and
remaining time are not stored by packed scenes.

<a id="m-electron2d-timer-createsceneinstancefactory"></a>
### `protected override Func<Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

**Returns:** A non-null factory that creates a fresh node of the exact same runtime type.

**Exceptions**

- `NotSupportedException`: A derived node has not explicitly supplied an instancing factory.

**Remarks:** The base implementation supports only an exact [`Node`](Node.md). Derived node types that can be packed must
return a static, non-capturing factory that remains valid after the source node is disposed and creates a live,
detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore
the instance state.

Returns a static factory for exact [`Timer`](Timer.md) instances.

<a id="m-electron2d-timer-onnotification-system-int32"></a>
### `protected override void OnNotification(int what)`

Handles an engine notification delivered to this object.

**Parameters**

- `what`: The notification identifier.

**Remarks:** Derived overrides should call the base implementation unless they intentionally suppress inherited handling.

Runs autostart after inherited ready handling and consumes internal process notifications.

<a id="m-electron2d-timer-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

Clears timeout subscribers before releasing inherited node state.

## Event Descriptions

<a id="e-electron2d-timer-timeout"></a>
### `public event Action<Timer> Timeout`

Occurs when the countdown reaches zero.

**Remarks:** Delivery is synchronous on the scene-tree owner thread. One-shot timers stop before delivery; repeating timers
reload first. At most one timeout is emitted per matching frame. Handler exceptions propagate through the frame
after the remaining callback phases are attempted.

## Inherited API

Public and protected members inherited from [Node](Node.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Complete protected API

| Member | Current behavior |
| --- | --- |
| `GetPropertyDescriptors()` | Appends all seven countdown descriptors with the storage policy above |
| `CreateSceneInstanceFactory()` | Makes exact `Timer` instances packable; a derived runtime type must provide its own static exact-type factory |
| `OnNotification(int what)` | Calls inherited handling, starts on ready when requested, and consumes internal process notifications |
| `Dispose(bool disposing)` | Clears timeout subscribers and active timer state before inherited Node teardown |

## Lifecycle and ordering

The timer begins stopped. `Start` requires membership in an active `SceneTree`, resets `TimeLeft`, and enables only the selected internal frame lane unless locally paused. `Stop` disables both internal lanes. Detaching does not destroy configured or countdown state; ordinary Node reattachment/lifetime rules apply.

Ready delivery starts an autostart timer after inherited ready handling and clears `Autostart`. Setting `Autostart` after ready has no immediate effect. `RequestReady()` followed by a later attachment can provide another ready cycle.

On a matching eligible frame, the timer subtracts either the scaled Node delta or Engine's original delta. A direct `SceneTree.Process`/`PhysicsProcess` call has no separate time-scale source, so both values equal the supplied delta. At zero or below, a one-shot timer stops before `Timeout`; a repeating timer adds the current `WaitTime` before `Timeout`. At most one event is emitted per frame, even when one delta spans several periods. Overshoot is retained internally, so later frames catch up one event at a time and public `TimeLeft` remains clamped to zero while the internal residual is non-positive.

Internal timer processing precedes the same node's public `OnProcess`/`OnPhysicsProcess`. A timeout exception is retained while the public callback and later scheduled nodes are attempted. If the timeout handler disables that public lane, disposes, or detaches the timer, its public callback is skipped; enabling a previously disabled public lane does not inject a callback into the already captured turn. Frame-level failures are reported by `SceneTree` as an aggregate.

## Invariants and error behavior

- `WaitTime` and explicit start duration must be finite and greater than zero. Rejection preserves existing configuration and countdown state.
- `ProcessCallback` accepts only defined enum values. Rejection is non-mutating.
- `Start` while detached throws `InvalidOperationException`; `Stop` remains valid while detached.
- Starting while paused resets the countdown without resuming it.
- Tree pause eligibility still follows inherited `Node.ProcessMode`; `Paused` is an additional local gate.
- The timer emits no event from `Stop`, disposal, detachment, or a nonmatching frame.
- Very short waits remain frame-quantized; no wall-clock or sub-frame delivery is promised.

## Threading guarantees and non-guarantees

Attached mutation, start/stop, ready handling, countdown advance, timeout delivery, and disposal use the owning `SceneTree` thread. Detached configuration has the same unsynchronized caller-owned semantics as `Node`. Event subscription and reads do not become thread-safe. No worker, task, synchronization context, sleep, or native timer exists.

## Dependencies and interactions

`Timer` depends on `Node` internal frame lanes, `SceneTree` scheduling and pause eligibility, `MainLoop`'s current original delta, and `Engine` dual scaled/original delivery. `PackedScene` consumes its storage-enabled typed descriptors. It does not depend on SDL3-CS, rendering, input, audio, collision physics, scripting, file scene serialization, or editor code.

## Verification and known limitations

Executable checks cover identities/defaults, descriptor storage, detached behavior, invalid and non-mutating configuration, process and physics lanes, live lane migration, exact-zero and overshooting countdowns, repeating and one-shot event state, pause combinations, autostart, stop, owner-thread rejection, throwing callbacks, callback detachment, packed-scene restoration, Engine time scale zero in both lanes, and zero warmed managed allocation while stopped or running.

Verification uses deterministic supplied deltas on Linux. It does not establish host cadence, wall-clock accuracy, mobile/native scheduling, editor behavior, or loaded-scene performance.

## Relevant decisions

- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0008: Scene inheritance](../decisions/scene.md#adr-0008)
- [0014: Realtime allocation](../decisions/resources.md#adr-0014)
- [0016: Engine scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
