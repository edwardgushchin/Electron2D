# MainLoop

Last updated: 2026-09-21

## Declaration

- Source: [`MainLoop.cs`](../../src/Core/OS/MainLoop.cs)
- Namespace: `Electron2D`
- Declaration: `public abstract class MainLoop : ElectronObject`
- Domain: [Core](../domains/core.md)
- Component: [Main loop](../components/main-loop.md)
- Concrete implementation: [`SceneTree`](SceneTree.md)

## Responsibility and ownership

`MainLoop` is the owner-thread lifecycle boundary between an application host or [`Engine`](Engine.md) and Electron2D. It pairs one successful initialization with variable-step and fixed-step callbacks and one finalization. Engine-driven callbacks also carry their original pre-time-scale delta as internal frame context for built-in scene behavior; public callbacks continue to receive only the effective delta. Each lane also scopes and clears [`Input`](Input.md) just-transition state, and the internal event-dispatch boundary validates owner thread and idle-running state before Input mutates state. It owns no thread, clock, window, event pump, renderer, raw input state, or physics world.

The creating thread owns lifecycle and frame execution. A custom loop owns whatever resources its protected callbacks acquire and must release successfully initialized state from `OnFinalize()`.

## Constants

| Constant | Value | Meaning |
| --- | ---: | --- |
| `NotificationOsMemoryWarning` | `2009` | The operating system reports memory pressure |
| `NotificationTranslationChanged` | `2010` | Translated messages may have changed |
| `NotificationWmAbout` | `2011` | The operating system requests application information |
| `NotificationCrash` | `2012` | An unrecoverable crash is imminent; handlers must avoid expensive work |
| `NotificationOsImeUpdate` | `2013` | Input-method composition changed |
| `NotificationApplicationResumed` | `2014` | The application resumed |
| `NotificationApplicationPaused` | `2015` | The application is about to suspend |
| `NotificationApplicationFocusIn` | `2016` | Keyboard focus was gained |
| `NotificationApplicationFocusOut` | `2017` | Keyboard focus was lost |
| `NotificationTextServerChanged` | `2018` | The active text service changed |
| `NotificationApplicationPipModeEntered` | `2019` | Picture-in-picture mode was entered |
| `NotificationApplicationPipModeExited` | `2020` | Picture-in-picture mode was exited |

The identifiers are implemented and may be delivered through inherited `Notify(int)`. Automatic creation of these notifications from native events is blocked on the SDL application host. `SceneTree` propagates manually or host-delivered system notifications to its live nodes.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `event Action<MainLoop, string, bool> OnRequestPermissionsResult` | Synchronous owner-thread permission result with sender, platform permission name, and granted state |
| `void Initialize()` | Invokes `OnInitialize()` once and enters the running state; failure is terminal |
| `bool Process(double delta)` | Runs one variable-step callback and returns its host-stop request |
| `bool PhysicsProcess(double delta)` | Runs one fixed-step callback and returns its host-stop request; no simulation is implied |
| `void FinalizeLoop()` | Finalizes one successfully initialized loop; the state becomes terminal even if the callback fails |

All inherited identity, notification, typed-property, translation, and disposal members follow [`ElectronObject`](ElectronObject.md).

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

## Official reference coverage inventory

The stable reference API and complete `Object` inheritance chain were checked on 2026-09-20.

| Reference area | Electron2D disposition |
| --- | --- |
| `_initialize()` / `_finalize()` | Implemented as protected `OnInitialize()` / `OnFinalize()` hooks plus explicit host entry points `Initialize()` / `FinalizeLoop()` |
| `_process(delta) -> bool` | Implemented as `OnProcess(double)` plus `Process(double)`; return value remains the stop request |
| `_physics_process(delta) -> bool` | Implemented as `OnPhysicsProcess(double)` plus `PhysicsProcess(double)`; simulation remains outside this scheduling contract |
| Runtime ownership and callback cadence | `Engine.Start` can initialize or attach a running loop, `AdvanceFrame` schedules its callbacks, and `Stop` finalizes without disposing it |
| `on_request_permissions_result(permission, granted)` | Implemented as the typed `OnRequestPermissionsResult` event and protected typed publisher |
| Twelve system notification constants `2009..2020` | Implemented with stable values; native event translation remains dependency-blocked |
| Inherited `Object` behavior | Supplied by `ElectronObject` under the typed-C# exclusions and adaptations documented there |

No dependency-blocked platform delivery is represented by an inert method. The future SDL host must translate real platform events into these existing typed and numeric endpoints.

## Dependencies and interactions

`MainLoop` depends on `ElectronObject`, the process-wide `Input` transition service, and the .NET Base Class Library. [`Engine`](Engine.md) attaches it through the internal state-validated boundary. `SceneTree` derives from it, maps its frame hooks and original delta context to scene processing including built-in [`Timer`](Timer.md) behavior, consumes the internal typed input dispatch hook, propagates system notifications through the hierarchy, and tears down its owned scene state from `OnFinalize()`.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies one-shot initialization/finalization, both callback lanes and stop results, delta validation, re-entry rejection, callback failure recovery, initialization/finalization failure states, automatic disposal finalization, disposal before/after failed initialization, owner-thread enforcement, pre-mutation wrong-thread/nested-frame input rejection, typed permission delivery and validation, notification IDs and dispatch, `Node` aliases, `SceneTree` inheritance/propagation/input delivery/explicit finalization, original-delta delivery at zero time scale, independent Input transition clearing, and zero steady-state allocation for warmed `SceneTree` frame paths.

The class itself has no timing source or scheduler. [`Engine`](Engine.md) now provides host-driven fixed-step scheduling and time scaling, but there is still no SDL event pump, native permission request API, automatic clock, frame pacing/waiting, exit-code owner, crash handler, input-focus state update, renderer, or collision-physics integration. MainLoop does not claim real-time or platform behavior by itself.

## Related decision

- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
