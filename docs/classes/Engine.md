# Engine

Last updated: 2026-09-22

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Core/Config/Engine.cs`](../../src/Core/Config/Engine.cs), [`Engine.Run.cs`](../../src/Core/Config/Engine.Run.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed partial class Engine : ElectronObject`

> Coordinates process-wide frame scheduling, runtime metrics, and named engine singletons.

## Description

Coordinates process-wide frame scheduling, runtime metrics, and named engine singletons.

`Engine` is the non-disposable process-wide runtime coordinator. It reads/writes persisted timing configuration through [`ProjectSettings`](ProjectSettings.md), attaches one [`MainLoop`](MainLoop.md), converts host-supplied unscaled elapsed time into fixed and variable callbacks, publishes runtime metrics, and maintains a thread-safe registry of named non-owned `ElectronObject` instances. The registry is initialized with permanent `Engine`, `ProjectSettings`, [`Input`](Input.md), and [`InputMap`](InputMap.md) entries.

Engine.Run(Window) owns the elapsed-time source, native event pump, waiting/frame pacing and final disposal for ordinary windowed scenes. Manual embedding retains caller ownership. `Engine.Stop()` finalizes and detaches the loop but deliberately does not dispose it. The registry retains references but never acquires disposal ownership.

[`Engine.Instance`](Engine.md#p-electron2d-engine-instance) is created once for the process and cannot be disposed. For manual embedding, a host attaches one [`Engine.MainLoop`](Engine.md#p-electron2d-engine-mainloop), supplies finite elapsed time to
[`Engine.AdvanceFrame(Double)`](Engine.md#m-electron2d-engine-advanceframe-system-double), and finally calls [`Engine.Stop`](Engine.md#m-electron2d-engine-stop).

Runtime lifecycle and frame execution have owner-thread affinity. Configuration properties, metric reads, and
named-singleton operations are safe from other threads. Fixed-step and time-scale properties use the process-wide
[`ProjectSettings`](ProjectSettings.md) registry, including active feature overrides. A frame uses one configuration snapshot.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var window = new Window { Title = "Game", Size = new Vector2I(960, 540) };
window.AddChild(scene); // a caller-created Node hierarchy
Engine.Instance.MaxFPS = 60;
int exitCode = Engine.Instance.Run(window);
```

## Properties

| Member | Description |
| --- | --- |
| [`public int MaxFPS { get; set; }`](#p-electron2d-engine-maxfps) | Maximum cadence for Run. |
| [`public static Engine Instance { get; }`](#p-electron2d-engine-instance) | Gets the process-wide engine instance. |
| [`public int PhysicsTicksPerSecond { get; set; }`](#p-electron2d-engine-physicstickspersecond) | Gets or sets the fixed-step callback frequency. |
| [`public int MaxPhysicsStepsPerFrame { get; set; }`](#p-electron2d-engine-maxphysicsstepsperframe) | Gets or sets the maximum number of fixed-step callbacks run during one process frame. |
| [`public double PhysicsJitterFix { get; set; }`](#p-electron2d-engine-physicsjitterfix) | Gets or sets the tolerance used to smooth fixed-step boundaries against variable frame timing. |
| [`public double TimeScale { get; set; }`](#p-electron2d-engine-timescale) | Gets or sets the rate at which game time advances relative to unscaled host time. |
| [`public ulong ProcessFrames { get; }`](#p-electron2d-engine-processframes) | Gets the number of process callbacks completed since the process-wide engine was created. |
| [`public ulong PhysicsFrames { get; }`](#p-electron2d-engine-physicsframes) | Gets the number of fixed-step callbacks started since the process-wide engine was created. |
| [`public double FramesPerSecond { get; }`](#p-electron2d-engine-framespersecond) | Gets the most recently measured process-frame rate. |
| [`public double PhysicsInterpolationFraction { get; }`](#p-electron2d-engine-physicsinterpolationfraction) | Gets the fraction of the current fixed interval remaining after the latest scheduling decision. |
| [`public bool IsInPhysicsFrame { get; }`](#p-electron2d-engine-isinphysicsframe) | Gets whether the current thread is executing a fixed-step callback. |
| [`public MainLoop MainLoop { get; }`](#p-electron2d-engine-mainloop) | Gets the currently attached application loop. |
| [`public string ArchitectureName { get; }`](#p-electron2d-engine-architecturename) | Gets the architecture targeted by the current Electron2D process. |
| [`public EngineVersionInfo VersionInfo { get; }`](#p-electron2d-engine-versioninfo) | Gets immutable version information for the loaded Electron2D assembly. |

## Methods

| Member | Description |
| --- | --- |
| [`public int Run(Window window)`](#m-electron2d-engine-run-electron2d-window) | Consumes a validated detached root Window after reserving the idle engine. |
| [`public void Start(MainLoop mainLoop)`](#m-electron2d-engine-start-electron2d-mainloop) | Attaches and, when necessary, initializes one application loop. |
| [`public bool AdvanceFrame(double elapsedSeconds)`](#m-electron2d-engine-advanceframe-system-double) | Advances fixed-step callbacks followed by one process callback. |
| [`public void Stop()`](#m-electron2d-engine-stop) | Finalizes and detaches the current application loop. |
| [`public void RegisterSingleton(string name, ElectronObject instance)`](#m-electron2d-engine-registersingleton-system-string-electron2d-electronobject) | Registers a named, non-owned engine singleton. |
| [`public void UnregisterSingleton(string name)`](#m-electron2d-engine-unregistersingleton-system-string) | Unregisters a named engine singleton without disposing it. |
| [`public ElectronObject GetSingleton(string name)`](#m-electron2d-engine-getsingleton-system-string) | Gets a named engine singleton. |
| [`public T GetSingleton<T>(string name)`](#m-electron2d-engine-getsingleton-1-system-string) | Gets a named engine singleton and validates its type. |
| [`public bool HasSingleton(string name)`](#m-electron2d-engine-hassingleton-system-string) | Reports whether a named engine singleton is registered. |
| [`public IReadOnlyList<string> GetSingletonList()`](#m-electron2d-engine-getsingletonlist) | Gets the current engine-singleton names in registration order. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-engine-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override void ValidateDisposal()`](#m-electron2d-engine-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Property Descriptions

<a id="p-electron2d-engine-maxfps"></a>
### `public int MaxFPS { get; set; }`

Maximum cadence for Run. Zero (default) is unlimited; negative values throw ArgumentOutOfRangeException. Atomic reads/writes are allowed from any thread; this runtime value is not persisted in ProjectSettings. Waiting measures unscaled monotonic time and pumps events in intervals of at most 10 ms. Manual AdvanceFrame does not wait.

<a id="p-electron2d-engine-instance"></a>
### `public static Engine Instance { get; }`

Gets the process-wide engine instance.

**Value:** The same non-disposable instance for the lifetime of the process.

<a id="p-electron2d-engine-physicstickspersecond"></a>
### `public int PhysicsTicksPerSecond { get; set; }`

Gets or sets the fixed-step callback frequency.

**Value:** The number of physics callback opportunities per unscaled second. The default is `60`.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is less than or equal to zero.

**Remarks:** Higher values improve fixed-step precision while increasing processor cost. The fixed callback delta is
`TimeScale / PhysicsTicksPerSecond`. The value is sampled once at the start of each frame. Changing it
writes [`ProjectSettings.PhysicsTicksPerSecond`](ProjectSettings.md#p-electron2d-projectsettings-physicstickspersecond), re-baselines fixed-step history on the next frame,
and discards any fractional interval from the old frequency.

<a id="p-electron2d-engine-maxphysicsstepsperframe"></a>
### `public int MaxPhysicsStepsPerFrame { get; set; }`

Gets or sets the maximum number of fixed-step callbacks run during one process frame.

**Value:** A positive callback limit. The default is `8`.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is less than or equal to zero.

**Remarks:** Limiting catch-up avoids an unbounded spiral after a long host stall. Excess whole fixed steps are discarded;
the remaining fractional time is preserved for interpolation. Assignment writes
[`ProjectSettings.MaxPhysicsStepsPerFrame`](ProjectSettings.md#p-electron2d-projectsettings-maxphysicsstepsperframe).

<a id="p-electron2d-engine-physicsjitterfix"></a>
### `public double PhysicsJitterFix { get; set; }`

Gets or sets the tolerance used to smooth fixed-step boundaries against variable frame timing.

**Value:** A finite non-negative multiple of one fixed step. The default is `0.5`.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is NaN or infinite.

**Remarks:** A value of zero disables tolerance-based clock adjustment. Values above `2` are accepted but can make
timing noticeably less responsive. Custom interpolation commonly uses zero. Assignment writes
[`ProjectSettings.PhysicsJitterFix`](ProjectSettings.md#p-electron2d-projectsettings-physicsjitterfix); negative input is clamped to zero before storage.

<a id="p-electron2d-engine-timescale"></a>
### `public double TimeScale { get; set; }`

Gets or sets the rate at which game time advances relative to unscaled host time.

**Value:** A finite non-negative multiplier. The default is `1`; zero freezes callback deltas.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is negative, NaN, or infinite.

**Remarks:** This multiplier changes the deltas supplied to process and fixed-step callbacks. It does not change how often
those callbacks are scheduled. Extremely large values reduce temporal precision and should be avoided.

<a id="p-electron2d-engine-processframes"></a>
### `public ulong ProcessFrames { get; }`

Gets the number of process callbacks completed since the process-wide engine was created.

**Value:** A monotonically increasing process-lifetime count. A callback that throws is not counted.

<a id="p-electron2d-engine-physicsframes"></a>
### `public ulong PhysicsFrames { get; }`

Gets the number of fixed-step callbacks started since the process-wide engine was created.

**Value:** A monotonically increasing process-lifetime count, including a callback that throws.

<a id="p-electron2d-engine-framespersecond"></a>
### `public double FramesPerSecond { get; }`

Gets the most recently measured process-frame rate.

**Value:** Completed process frames per unscaled host second, updated after each accumulated second. The value is zero
until the first measurement window completes and is reset by [`Engine.Start(MainLoop)`](Engine.md#m-electron2d-engine-start-electron2d-mainloop).

<a id="p-electron2d-engine-physicsinterpolationfraction"></a>
### `public double PhysicsInterpolationFraction { get; }`

Gets the fraction of the current fixed interval remaining after the latest scheduling decision.

**Value:** A value from `0` through `1`, where zero is exactly on a fixed-step boundary.

**Remarks:** The value is intended for visual interpolation between the previous and current fixed states.

<a id="p-electron2d-engine-isinphysicsframe"></a>
### `public bool IsInPhysicsFrame { get; }`

Gets whether the current thread is executing a fixed-step callback.

**Value:** `true` only during a call to [`MainLoop.PhysicsProcess(Double)`](MainLoop.md#m-electron2d-mainloop-physicsprocess-system-double).

<a id="p-electron2d-engine-mainloop"></a>
### `public MainLoop MainLoop { get; }`

Gets the currently attached application loop.

**Value:** The loop visible during startup, frames, and shutdown; otherwise `null`.

<a id="p-electron2d-engine-architecturename"></a>
### `public string ArchitectureName { get; }`

Gets the architecture targeted by the current Electron2D process.

**Value:** A stable lowercase architecture name such as `x86_64`, `x86_32`, `arm64`, or `arm32`.

<a id="p-electron2d-engine-versioninfo"></a>
### `public EngineVersionInfo VersionInfo { get; }`

Gets immutable version information for the loaded Electron2D assembly.

**Value:** The process-wide version descriptor.

## Method Descriptions

<a id="m-electron2d-engine-run-electron2d-window"></a>
### `public int Run(Window window)`

Consumes a validated detached root Window after reserving the idle engine. Opens the native window and renderer, creates and publishes SceneTree before ready, pumps events before frames and renders after scene processing. Cleanup disposes the scene, renderer and native window in that order. Returns SceneTree.Quit's code, zero for default close. Rejected null/disposed/attached roots and a busy engine retain caller ownership. Once reserved, failed native startup and callback failures still dispose transferred scene state. Cleanup failures are aggregated. The engine stays reserved until cleanup completes. Runs on the native main thread; no console handlers are installed. Rendering failures propagate through the same cleanup path as scene failures. Native services opened directly through DisplayServer must finish before teardown; pending asynchronous dialogs can reject disposal and leave DisplayServer.Instance alive for completion/release. Start, AdvanceFrame, Stop and manual tree finalization/disposal cannot interfere with the active Run. Reuse requires a new Window.

<a id="m-electron2d-engine-start-electron2d-mainloop"></a>
### `public void Start(MainLoop mainLoop)`

Attaches and, when necessary, initializes one application loop.

**Parameters**

- `mainLoop`: The live loop to own until [`Engine.Stop`](Engine.md#m-electron2d-engine-stop) completes.

**Exceptions**

- `ArgumentNullException`: `mainLoop` is `null`.
- `InvalidOperationException`: A runtime is already starting, running, iterating, or stopping; the loop is in an incompatible state; or the caller does not own the loop.
- `ObjectDisposedException`: The loop is disposing or disposed.
- `Exception`: Loop initialization throws. The engine returns to its idle state.

**Remarks:** The calling thread becomes the runtime owner. An uninitialized loop is initialized; an already running loop,
including a newly constructed [`SceneTree`](SceneTree.md), is attached without a second initialization. The loop
is not disposed by the engine. During its initialization, [`Engine.MainLoop`](Engine.md#p-electron2d-engine-mainloop) already returns
`mainLoop`.

<a id="m-electron2d-engine-advanceframe-system-double"></a>
### `public bool AdvanceFrame(double elapsedSeconds)`

Advances fixed-step callbacks followed by one process callback.

**Parameters**

- `elapsedSeconds`: Finite non-negative unscaled host time elapsed since the previous call.

**Returns:** `true` if either callback lane asks the host to stop; otherwise `false`.

**Exceptions**

- `ArgumentOutOfRangeException`: `elapsedSeconds` is negative, NaN, or infinite.
- `InvalidOperationException`: The runtime is not running, the caller is not its owner thread, frame execution is re-entered, or the current time scale would produce a non-finite callback delta.
- `Exception`: A loop callback or project-settings event handler throws.

**Remarks:** Fixed steps run before the process callback. If a fixed callback requests a stop, remaining fixed callbacks are
skipped but the process callback still runs. A callback exception propagates, restores the engine to its running
state, and does not implicitly finalize the loop. This method performs no waiting, rendering, input pumping,
audio work, or collision simulation. After a successful process callback and metric update, the method flushes
one pending [`ProjectSettings.SettingsChanged`](ProjectSettings.md#e-electron2d-projectsettings-settingschanged) event before returning.

<a id="m-electron2d-engine-stop"></a>
### `public void Stop()`

Finalizes and detaches the current application loop.

**Exceptions**

- `InvalidOperationException`: The runtime is not running, the caller is not its owner thread, or the call occurs during a frame or lifecycle transition.
- `Exception`: Loop finalization throws. Detachment still completes.

**Remarks:** The loop becomes unavailable through [`Engine.MainLoop`](Engine.md#p-electron2d-engine-mainloop) after finalization returns or throws. The engine
returns to its idle state and may start a different loop. The detached loop is not disposed.

<a id="m-electron2d-engine-registersingleton-system-string-electron2d-electronobject"></a>
### `public void RegisterSingleton(string name, ElectronObject instance)`

Registers a named, non-owned engine singleton.

**Parameters**

- `name`: The nonblank case-sensitive name.
- `instance`: The live object to expose.

**Exceptions**

- `ArgumentNullException`: `name` or `instance` is `null`.
- `ArgumentException`: `name` is empty or consists only of whitespace.
- `ObjectDisposedException`: `instance` is disposing or disposed.
- `InvalidOperationException`: The name is already registered.

**Remarks:** Registration retains a managed reference but does not transfer disposal ownership. Disposing an object does not
remove its registration; the registering component must unregister it during teardown. The name `Engine``ProjectSettings`, `Input`, and `InputMap` are already occupied by built-in process singletons.

<a id="m-electron2d-engine-unregistersingleton-system-string"></a>
### `public void UnregisterSingleton(string name)`

Unregisters a named engine singleton without disposing it.

**Parameters**

- `name`: The nonblank case-sensitive registered name.

**Exceptions**

- `ArgumentNullException`: `name` is `null`.
- `ArgumentException`: `name` is empty or consists only of whitespace.
- `Collections.Generic.KeyNotFoundException`: No singleton has the supplied name.
- `InvalidOperationException`: `name` identifies a built-in singleton.

<a id="m-electron2d-engine-getsingleton-system-string"></a>
### `public ElectronObject GetSingleton(string name)`

Gets a named engine singleton.

**Parameters**

- `name`: The nonblank case-sensitive registered name.

**Returns:** The registered object. Ownership remains with the registering component.

**Exceptions**

- `ArgumentNullException`: `name` is `null`.
- `ArgumentException`: `name` is empty or consists only of whitespace.
- `Collections.Generic.KeyNotFoundException`: No singleton has the supplied name.

<a id="m-electron2d-engine-getsingleton-1-system-string"></a>
### `public T GetSingleton<T>(string name)`

Gets a named engine singleton and validates its type.

**Type parameters**

- `T`: The required [`ElectronObject`](ElectronObject.md) type.

**Parameters**

- `name`: The nonblank case-sensitive registered name.

**Returns:** The registered object cast to `T`.

**Exceptions**

- `ArgumentNullException`: `name` is `null`.
- `ArgumentException`: `name` is empty or consists only of whitespace.
- `Collections.Generic.KeyNotFoundException`: No singleton has the supplied name.
- `InvalidCastException`: The registered object is not assignable to `T`.

<a id="m-electron2d-engine-hassingleton-system-string"></a>
### `public bool HasSingleton(string name)`

Reports whether a named engine singleton is registered.

**Parameters**

- `name`: The nonblank case-sensitive name.

**Returns:** `true` when the name is registered; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `name` is `null`.
- `ArgumentException`: `name` is empty or consists only of whitespace.

<a id="m-electron2d-engine-getsingletonlist"></a>
### `public IReadOnlyList<string> GetSingletonList()`

Gets the current engine-singleton names in registration order.

**Returns:** An immutable snapshot using ordinal, case-sensitive names.

<a id="m-electron2d-engine-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

<a id="m-electron2d-engine-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Exceptions**

- `InvalidOperationException`: Always thrown because the singleton has process lifetime.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

The process-wide engine instance cannot be disposed.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Scheduling and ordering

`AdvanceFrame()` takes one configuration snapshot. A twelve-frame synchronizer uses the current jitter tolerance to keep fixed-step distribution stable while maintaining a fractional accumulator. A long stall is reduced to at most the configured fixed-step budget plus its remainder, so whole excess intervals are discarded rather than creating an unbounded catch-up spiral. The interpolation fraction is published before callbacks.

Each fixed callback increments `PhysicsFrames`, sets `IsInPhysicsFrame`, and clears it in `finally`. A fixed callback returning `true` skips remaining fixed callbacks, but the process callback still runs. The process counter and FPS measurement update only after the process callback returns, then one pending `ProjectSettings.SettingsChanged` invocation is flushed. A callback or settings-event exception propagates and restores the runtime to the running state without implicit finalization. Timing already consumed for the failed attempt is not replayed; a throwing settings event occurs after the process callback has been counted.

`TimeScale` multiplies the fixed delta and the synchronized process delta, not callback frequency. Engine supplies both the effective scaled value and its original synchronized value to `MainLoop`; public callbacks receive the scaled value, while built-in scene behavior such as [`Timer.IgnoreTimeScale`](Timer.md) may select the original value. The two values are scoped to the current callback and cleared afterward. If an extreme finite scale would overflow either effective delta, the frame is rejected with `InvalidOperationException` before user callbacks.

## Lifecycle and failure states

```text
Idle --Start--> Starting --success--> Running --AdvanceFrame--> Iterating --return/throw--> Running
                         \--failure-----------------------------------------------> Idle
Running --Stop--> Stopping --return/throw----------------------------------------> Idle
```

Lifecycle/frame entry is atomic and non-reentrant. `MainLoop` is visible during initialization and finalization. Failed initialization clears the engine reference; the loop's own initialization-failed state remains terminal. Failed finalization also clears the reference; the loop remains terminal. Another loop may then be started.

## Invariants and error behavior

- Elapsed time, jitter tolerance, and time scale reject NaN/infinity; elapsed time and time scale reject negatives.
- Tick frequency and maximum fixed steps must be positive.
- Fixed callbacks always precede the process callback.
- Interpolation remains in `[0, 1]` and `IsInPhysicsFrame` is cleared after return, stop request, or exception.
- The active loop is never implicitly disposed.
- `Start`, `AdvanceFrame`, and `Stop` cannot overlap or re-enter.
- Names are nonblank, ordinal, and unique; missing removal/lookup is an error.
- The process-wide `Engine`, `ProjectSettings`, `Input`, and `InputMap` registry entries cannot be removed through Engine.

## Threading guarantees and non-guarantees

The thread that successfully calls `Start()` owns runtime lifecycle and frames until `Stop()` completes. Wrong-thread frame and stop calls fail before changing runtime state. Timing configuration, metrics, `MainLoop` reads, and registry operations are cross-thread safe. User callbacks, event subscription, registered object state, and the attached loop are not made thread-safe by `Engine`.

No background thread, clock, sleep, synchronization context, or native pump is created. Real cadence depends on the host that measures elapsed time and calls `AdvanceFrame()`.

## Dependencies and interactions

The class depends on `ElectronObject`, `MainLoop`, `EngineVersionInfo`, `ProjectSettings`, `Input`, `InputMap`, the .NET Base Class Library, and reflection over its own assembly metadata. `SceneTree` can be attached because it is already initialized after successful construction. There is no SDL3-CS, renderer, native input backend, audio, logger, scripting, movie writer, editor, collision-physics, or asset dependency.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies singleton lifetime, all four permanent service registrations, project-setting defaults/feature overrides/event flushing, invalid configuration, typed property discovery, architecture/version data, registry validation/type/ownership/order/concurrency, loop publication during initialization/finalization, automatic initialization, existing `SceneTree` attachment, owner-thread enforcement, re-entry rejection, fixed-before-process order, scaling, original-delta Timer delivery at zero scale in both lanes, input transition-lane completion, jitter/interpolation boundaries, catch-up cap, stop combination, process and physics callback failures, failed initialization/finalization cleanup, counters, FPS, and zero steady-state allocation across a warmed empty frame path.

The original scheduling checks use deterministic supplied deltas. WindowRuntimeTests additionally exercises Run with native SDL dummy and Wayland windows, event queues, monotonic waiting, quit/close, failure cleanup and reopening. Wayland events are injected and repeated initialization emits a GTK locale warning. No renderer or loaded game benchmark is covered. They establish managed scheduling behavior, not hard real-time guarantees or visual acceptance.

## Related scene decision

- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
