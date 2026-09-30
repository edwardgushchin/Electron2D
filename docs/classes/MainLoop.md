# MainLoop

Last updated: 2026-09-30

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** [SceneTree](SceneTree.md)

- **Source:** [`src/Core/OS/MainLoop.cs`](../../src/Core/OS/MainLoop.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class MainLoop : ElectronObject`

> Defines the host-driven lifecycle and frame callbacks for an Electron2D application.

## Description

Defines the host-driven lifecycle and frame callbacks for an Electron2D application.

`MainLoop` is the owner-thread lifecycle boundary between an application host or [`Engine`](Engine.md) and Electron2D. It pairs one successful initialization with variable-step and fixed-step callbacks and one finalization. Engine-driven callbacks also carry their original pre-time-scale delta as internal frame context for built-in scene behavior; public callbacks continue to receive only the effective delta. Each lane also scopes and clears [`Input`](Input.md) just-transition state, and the internal event-dispatch boundary validates owner thread and idle-running state before Input mutates state. It owns no thread, clock, window, event pump, renderer, raw input state, or physics world.

The creating thread owns lifecycle and frame execution. A custom loop owns whatever resources its protected callbacks acquire and must release successfully initialized state from `OnFinalize()`.

The creating thread is the owner thread. A host initializes the loop once, supplies non-negative finite frame
deltas, and finalizes it once. Returning `true` from a frame callback asks the host to stop the
application. The class does not create a thread, clock, window, renderer, input pump, or physics scheduler.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using MainLoop loop = new SceneTree(new Node { Name = "Root" });
loop.Process(1.0 / 60.0);
```

## Constructors

| Member | Description |
| --- | --- |
| [`protected MainLoop()`](#m-electron2d-mainloop-ctor) | Initializes a new MainLoop instance. |

## Methods

| Member | Description |
| --- | --- |
| [`public void Initialize()`](#m-electron2d-mainloop-initialize) | Initializes this loop and invokes [`MainLoop.OnInitialize`](MainLoop.md#m-electron2d-mainloop-oninitialize) exactly once. |
| [`public bool Process(double delta)`](#m-electron2d-mainloop-process-system-double) | Runs one variable-step frame. |
| [`public bool PhysicsProcess(double delta)`](#m-electron2d-mainloop-physicsprocess-system-double) | Runs one fixed-step physics frame. |
| [`public void FinalizeLoop()`](#m-electron2d-mainloop-finalizeloop) | Finalizes a successfully initialized loop and invokes [`MainLoop.OnFinalize`](MainLoop.md#m-electron2d-mainloop-onfinalize) exactly once. |
| [`protected virtual void OnInitialize()`](#m-electron2d-mainloop-oninitialize) | Performs loop-specific initialization. |
| [`protected virtual bool OnProcess(double delta)`](#m-electron2d-mainloop-onprocess-system-double) | Performs one variable-step frame. |
| [`protected virtual bool OnPhysicsProcess(double delta)`](#m-electron2d-mainloop-onphysicsprocess-system-double) | Performs one fixed-step physics frame. |
| [`protected virtual void OnFinalize()`](#m-electron2d-mainloop-onfinalize) | Releases resources acquired by a successfully initialized loop. |
| [`protected virtual void ValidateFinalization()`](#m-electron2d-mainloop-validatefinalization) | Validates derived finalization preconditions before the loop enters its terminal state. |
| [`protected void NotifyRequestPermissionsResult(string permission, bool granted)`](#m-electron2d-mainloop-notifyrequestpermissionsresult-system-string-system-boolean) | Synchronously publishes an operating-system permission result. |
| [`protected override void ValidateDisposal()`](#m-electron2d-mainloop-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-mainloop-dispose-system-boolean) | Releases resources owned by a derived class. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<MainLoop, string, bool> OnRequestPermissionsResult`](#e-electron2d-mainloop-onrequestpermissionsresult) | Occurs when a previously requested operating-system permission receives a result. |

## Constants

| Member | Description |
| --- | --- |
| [`public const int NotificationOsMemoryWarning = 2009`](#f-electron2d-mainloop-notificationosmemorywarning) | Identifies an operating-system low-memory warning. |
| [`public const int NotificationTranslationChanged = 2010`](#f-electron2d-mainloop-notificationtranslationchanged) | Identifies a notification that translated messages may have changed. |
| [`public const int NotificationWmAbout = 2011`](#f-electron2d-mainloop-notificationwmabout) | Identifies an operating-system request to show application information. |
| [`public const int NotificationCrash = 2012`](#f-electron2d-mainloop-notificationcrash) | Identifies a notification delivered immediately before an unrecoverable crash. |
| [`public const int NotificationOsImeUpdate = 2013`](#f-electron2d-mainloop-notificationosimeupdate) | Identifies an input-method composition update supplied by the operating system. |
| [`public const int NotificationApplicationResumed = 2014`](#f-electron2d-mainloop-notificationapplicationresumed) | Identifies that the application resumed after suspension. |
| [`public const int NotificationApplicationPaused = 2015`](#f-electron2d-mainloop-notificationapplicationpaused) | Identifies that the application is about to be suspended. |
| [`public const int NotificationApplicationFocusIn = 2016`](#f-electron2d-mainloop-notificationapplicationfocusin) | Identifies that the application received keyboard focus. |
| [`public const int NotificationApplicationFocusOut = 2017`](#f-electron2d-mainloop-notificationapplicationfocusout) | Identifies that the application lost keyboard focus. |
| [`public const int NotificationTextServerChanged = 2018`](#f-electron2d-mainloop-notificationtextserverchanged) | Identifies that the active text service changed. |
| [`public const int NotificationApplicationPipModeEntered = 2019`](#f-electron2d-mainloop-notificationapplicationpipmodeentered) | Identifies that the application entered picture-in-picture mode. |
| [`public const int NotificationApplicationPipModeExited = 2020`](#f-electron2d-mainloop-notificationapplicationpipmodeexited) | Identifies that the application exited picture-in-picture mode. |

## Constructor Descriptions

<a id="m-electron2d-mainloop-ctor"></a>
### `protected MainLoop()`

Initializes a new MainLoop instance.

## Method Descriptions

<a id="m-electron2d-mainloop-initialize"></a>
### `public void Initialize()`

Initializes this loop and invokes [`MainLoop.OnInitialize`](MainLoop.md#m-electron2d-mainloop-oninitialize) exactly once.

**Exceptions**

- `InvalidOperationException`: The caller is not the owner thread or the loop is not awaiting initialization.
- `ObjectDisposedException`: Disposal has started or finished.
- `Exception`: [`MainLoop.OnInitialize`](MainLoop.md#m-electron2d-mainloop-oninitialize) throws.

**Remarks:** Successful return moves the loop to its running state. If the callback fails, initialization is terminal and
the callback is not retried; an override is responsible for rolling back resources acquired before it throws.

<a id="m-electron2d-mainloop-process-system-double"></a>
### `public bool Process(double delta)`

Runs one variable-step frame.

**Parameters**

- `delta`: Elapsed frame time in seconds. The value must be finite and non-negative.

**Returns:** `true` when the host should stop the application; otherwise `false`.

**Exceptions**

- `ArgumentOutOfRangeException`: `delta` is negative, NaN, or infinite.
- `InvalidOperationException`: The caller is not the owner thread, the loop is not running, or frame execution is re-entered.
- `ObjectDisposedException`: Disposal has started or finished.
- `Exception`: [`MainLoop.OnProcess(Double)`](MainLoop.md#m-electron2d-mainloop-onprocess-system-double) throws.

<a id="m-electron2d-mainloop-physicsprocess-system-double"></a>
### `public bool PhysicsProcess(double delta)`

Runs one fixed-step physics frame.

**Parameters**

- `delta`: Elapsed fixed-step time in seconds. The value must be finite and non-negative.

**Returns:** `true` when the host should stop the application; otherwise `false`.

**Exceptions**

- `ArgumentOutOfRangeException`: `delta` is negative, NaN, or infinite.
- `InvalidOperationException`: The caller is not the owner thread, the loop is not running, or frame execution is re-entered.
- `ObjectDisposedException`: Disposal has started or finished.
- `Exception`: [`MainLoop.OnPhysicsProcess(Double)`](MainLoop.md#m-electron2d-mainloop-onphysicsprocess-system-double) throws.

**Remarks:** This callback defines scheduling only; it does not perform collision or rigid-body simulation.

<a id="m-electron2d-mainloop-finalizeloop"></a>
### `public void FinalizeLoop()`

Finalizes a successfully initialized loop and invokes [`MainLoop.OnFinalize`](MainLoop.md#m-electron2d-mainloop-onfinalize) exactly once.

**Exceptions**

- `InvalidOperationException`: The caller is not the owner thread, the loop is not running, or finalization is rejected by a derived invariant.
- `ObjectDisposedException`: Disposal has started or finished.
- `Exception`: [`MainLoop.OnFinalize`](MainLoop.md#m-electron2d-mainloop-onfinalize) throws.

**Remarks:** The loop becomes terminal even if the callback throws. Calling [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose) on a running loop performs
this finalization automatically; disposing an uninitialized or failed loop does not invoke the callback.

<a id="m-electron2d-mainloop-oninitialize"></a>
### `protected virtual void OnInitialize()`

Performs loop-specific initialization.

**Remarks:** The callback runs once on the owner thread before any frame callback. An override that throws must release any
resources it acquired because [`MainLoop.OnFinalize`](MainLoop.md#m-electron2d-mainloop-onfinalize) is not called after failed initialization.

<a id="m-electron2d-mainloop-onprocess-system-double"></a>
### `protected virtual bool OnProcess(double delta)`

Performs one variable-step frame.

**Parameters**

- `delta`: Elapsed frame time in seconds.

**Returns:** `true` to ask the host to stop; otherwise `false`.

**Remarks:** The default implementation does no work and returns `false`.

<a id="m-electron2d-mainloop-onphysicsprocess-system-double"></a>
### `protected virtual bool OnPhysicsProcess(double delta)`

Performs one fixed-step physics frame.

**Parameters**

- `delta`: Elapsed fixed-step time in seconds.

**Returns:** `true` to ask the host to stop; otherwise `false`.

**Remarks:** The default implementation does no work and returns `false`.

<a id="m-electron2d-mainloop-onfinalize"></a>
### `protected virtual void OnFinalize()`

Releases resources acquired by a successfully initialized loop.

**Remarks:** The callback runs once on the owner thread after the last frame and before object disposal completes.

<a id="m-electron2d-mainloop-validatefinalization"></a>
### `protected virtual void ValidateFinalization()`

Validates derived finalization preconditions before the loop enters its terminal state.

**Exceptions**

- `InvalidOperationException`: A derived lifecycle invariant currently prevents finalization.

**Remarks:** Overrides must be side-effect-free, throw when finalization is temporarily unsafe, and call the base implementation.

<a id="m-electron2d-mainloop-notifyrequestpermissionsresult-system-string-system-boolean"></a>
### `protected void NotifyRequestPermissionsResult(string permission, bool granted)`

Synchronously publishes an operating-system permission result.

**Parameters**

- `permission`: The non-null platform permission name.
- `granted`: `true` when permission was granted.

**Exceptions**

- `ArgumentNullException`: `permission` is `null`.
- `InvalidOperationException`: The caller is not the owner thread or the loop is not running.
- `ObjectDisposedException`: Disposal has started or finished.
- `Exception`: An event handler throws.

<a id="m-electron2d-mainloop-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Exceptions**

- `InvalidOperationException`: The caller is not the owner thread or a lifecycle callback is executing.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

Requires the owner thread and rejects disposal from initialization, frame, or finalization callbacks.

<a id="m-electron2d-mainloop-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

Finalizes a running loop, clears loop event subscribers, and then releases inherited state.

## Event Descriptions

<a id="e-electron2d-mainloop-onrequestpermissionsresult"></a>
### `public event Action<MainLoop, string, bool> OnRequestPermissionsResult`

Occurs when a previously requested operating-system permission receives a result.

**Remarks:** The first argument is this loop, the second is the platform permission name, and the third indicates whether
it was granted. Delivery is synchronous on the owner thread. A throwing subscriber stops later subscribers and
propagates its exception to the platform integration that published the result.

## Constant Descriptions

<a id="f-electron2d-mainloop-notificationosmemorywarning"></a>
### `public const int NotificationOsMemoryWarning = 2009`

Identifies an operating-system low-memory warning.

<a id="f-electron2d-mainloop-notificationtranslationchanged"></a>
### `public const int NotificationTranslationChanged = 2010`

Identifies a notification that translated messages may have changed.

<a id="f-electron2d-mainloop-notificationwmabout"></a>
### `public const int NotificationWmAbout = 2011`

Identifies an operating-system request to show application information.

<a id="f-electron2d-mainloop-notificationcrash"></a>
### `public const int NotificationCrash = 2012`

Identifies a notification delivered immediately before an unrecoverable crash.

**Remarks:** Time-consuming work and allocation should be avoided while handling this notification.

<a id="f-electron2d-mainloop-notificationosimeupdate"></a>
### `public const int NotificationOsImeUpdate = 2013`

Identifies an input-method composition update supplied by the operating system. During Engine.Run, the root Window invokes this on its SceneTree after DisplayServer commits the preedit text and codepoint selection; SceneTree propagates the notification to its live hierarchy before focused Control typed composition callbacks.

<a id="f-electron2d-mainloop-notificationapplicationresumed"></a>
### `public const int NotificationApplicationResumed = 2014`

Identifies that the application resumed after suspension.

<a id="f-electron2d-mainloop-notificationapplicationpaused"></a>
### `public const int NotificationApplicationPaused = 2015`

Identifies that the application is about to be suspended.

<a id="f-electron2d-mainloop-notificationapplicationfocusin"></a>
### `public const int NotificationApplicationFocusIn = 2016`

Identifies that the application received keyboard focus.

<a id="f-electron2d-mainloop-notificationapplicationfocusout"></a>
### `public const int NotificationApplicationFocusOut = 2017`

Identifies that the application lost keyboard focus.

<a id="f-electron2d-mainloop-notificationtextserverchanged"></a>
### `public const int NotificationTextServerChanged = 2018`

Identifies that the active text service changed.

<a id="f-electron2d-mainloop-notificationapplicationpipmodeentered"></a>
### `public const int NotificationApplicationPipModeEntered = 2019`

Identifies that the application entered picture-in-picture mode.

<a id="f-electron2d-mainloop-notificationapplicationpipmodeexited"></a>
### `public const int NotificationApplicationPipModeExited = 2020`

Identifies that the application exited picture-in-picture mode.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Complete protected API

| Member | Current behavior |
| --- | --- |
| `OnInitialize()` | One owner-thread initialization hook; default is empty |
| `OnProcess(double delta)` | Variable-step hook; default returns `false` |
| `OnPhysicsProcess(double delta)` | Fixed-step hook; default returns `false` |
| `OnFinalize()` | One owner-thread finalization hook paired with successful initialization; default is empty |
| `ValidateFinalization()` | Side-effect-free derived precondition hook called before explicit finalization enters terminal state |
| `NotifyRequestPermissionsResult(string permission, bool granted)` | Publishes the typed permission event while running |
| `ValidateDisposal()` | Enforces owner-thread disposal and rejects disposal from lifecycle/frame callbacks |
| `Dispose(bool disposing)` | Automatically finalizes a running loop, clears permission subscribers, then releases inherited state |

`CompleteFailedLoopConstruction()` is a `private protected` constructor-rollback endpoint used by an in-assembly derived type. It clears loop subscribers and publishes terminal object state without running public disposal callbacks.

## Lifecycle and state transitions

```text
Created --Initialize--> Initializing --success--> Running
                                 \--failure--> InitializationFailed
Running --Process/PhysicsProcess--> Processing --return/throw--> Running
Running --FinalizeLoop/Dispose--> Finalizing --return/throw--> Finalized
Created/InitializationFailed --Dispose--> Finalized
```

Initialization, frames, and finalization are non-reentrant. During one Engine-driven frame, the effective and original deltas are finite and non-negative; the original value exists only as internal callback context and is cleared in `finally`. Direct `Process`/`PhysicsProcess` calls use their supplied delta as both values. The matching Input transition lane is cleared in the same `finally`, including after callback failure. A frame callback failure restores `Running`, so a later frame remains valid. Initialization failure is terminal and is not retried. A derived `OnInitialize()` that throws must undo its partial acquisition because `OnFinalize()` is not called without successful initialization. Finalization failure is also terminal and is not retried by later disposal.

`Dispose()` automatically pairs a running loop with finalization. Disposal before initialization or after failed initialization does not call `OnFinalize()`. Finalization does not itself mark the `ElectronObject` disposed; an explicitly finalized loop still requires `Dispose()` for base-object teardown.

## Invariants and error behavior

- Effective and original frame deltas must be finite and non-negative; invalid deltas fail before callback execution.
- `true` from either frame callback is returned unchanged as a host-stop request. `MainLoop` does not stop itself.
- Any lifecycle call made in the wrong state, including re-entry and calls after finalization, throws `InvalidOperationException` before invoking user code.
- Exceptions from lifecycle callbacks and permission subscribers propagate synchronously. A finalization callback exception does not restore the running state.
- Permission names cannot be `null`; empty platform-defined names are preserved rather than invented or normalized.
- Event subscribers are cleared at finalization, failed construction, or disposal.

## Threading guarantees and non-guarantees

The constructing thread is the owner. Initialization, both frame lanes, permission-result publication, finalization, and disposal require that thread. Wrong-thread rejection occurs before state changes. Event subscription itself and user-owned state are not made thread-safe. There is no internal worker, synchronization context, timer, or automatic frame pump. `Engine.Start()` must therefore run on the same thread and keeps that thread as runtime owner.

## Dependencies and interactions

`MainLoop` depends on `ElectronObject`, the process-wide `Input` transition service, and the .NET Base Class Library. [`Engine`](Engine.md) attaches it through the internal state-validated boundary. `SceneTree` derives from it, maps its frame hooks and original lane/process-step context to scene processing including built-in [`Timer`](Timer.md) behavior, consumes the internal typed input dispatch hook, propagates system notifications through the hierarchy, and tears down its owned scene state from `OnFinalize()`.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies one-shot initialization/finalization, both callback lanes and stop results, delta validation, re-entry rejection, callback failure recovery, initialization/finalization failure states, automatic disposal finalization, disposal before/after failed initialization, owner-thread enforcement, pre-mutation wrong-thread/nested-frame input rejection, typed permission delivery and validation, notification IDs and dispatch, `Node` aliases, `SceneTree` inheritance/propagation/input delivery/explicit finalization, original-delta delivery at zero time scale, independent Input transition clearing, and zero steady-state allocation for warmed `SceneTree` frame paths.

The class itself has no timing source or scheduler. [`Engine`](Engine.md) now provides host-driven fixed-step scheduling and time scaling, but there is still no SDL event pump, native permission request API, automatic clock, frame pacing/waiting, exit-code owner, crash handler, input-focus state update, renderer, or collision-physics integration. MainLoop does not claim real-time or platform behavior by itself.

## Related decision

- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
