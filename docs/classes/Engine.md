# Engine

Last updated: 2026-09-21

## Declaration

- Source: [`Engine.cs`](../../src/Core/Config/Engine.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class Engine : ElectronObject`
- Domain: [Core](../domains/core.md)
- Component: [Engine runtime](../components/engine-runtime.md)
- Version type: [`EngineVersionInfo`](EngineVersionInfo.md)

## Responsibility and ownership

`Engine` is the non-disposable process-wide runtime coordinator. It reads/writes persisted timing configuration through [`ProjectSettings`](ProjectSettings.md), attaches one [`MainLoop`](MainLoop.md), converts host-supplied unscaled elapsed time into fixed and variable callbacks, publishes runtime metrics, and maintains a thread-safe registry of named non-owned `ElectronObject` instances. The registry is initialized with permanent `Engine` and `ProjectSettings` entries.

The host still owns the elapsed-time source, native event pump, waiting/frame pacing, and final disposal of the loop. `Engine.Stop()` finalizes and detaches the loop but deliberately does not dispose it. The registry retains references but never acquires disposal ownership.

## Complete public API

### Singleton and configuration

| Member | Current behavior |
| --- | --- |
| `static Engine Instance { get; }` | Returns the same process-lifetime instance; `Dispose()` is rejected |
| `int PhysicsTicksPerSecond { get; set; }` | Positive fixed-step frequency; default `60`; a runtime change re-baselines fixed timing on the next frame |
| `int MaxPhysicsStepsPerFrame { get; set; }` | Positive per-process-frame catch-up cap; default `8` |
| `double PhysicsJitterFix { get; set; }` | Finite fixed-boundary smoothing tolerance; default `0.5`; negative input clamps to zero |
| `double TimeScale { get; set; }` | Finite non-negative callback-delta multiplier; default `1`; zero freezes callback deltas without stopping callback cadence |

Configuration reads and writes are atomic and may occur from any thread. The first three properties use active project-setting feature overrides; `TimeScale` remains transient runtime state. `AdvanceFrame()` samples all four values once. Changing tick frequency discards an old-frequency fractional interval instead of mixing two step sizes.

### Runtime and metrics

| Member | Current behavior |
| --- | --- |
| `MainLoop? MainLoop { get; }` | Current loop during startup, running frames, and shutdown; otherwise `null` |
| `void Start(MainLoop mainLoop)` | Atomically reserves the runtime, publishes the loop, initializes it if still created, resets the synchronizer/FPS window, and establishes the caller as owner thread |
| `bool AdvanceFrame(double elapsedSeconds)` | Uses finite non-negative unscaled host time; runs zero or more fixed callbacks before exactly one process callback, updates metrics, flushes one pending project-settings event, and combines stop requests |
| `void Stop()` | Finalizes then detaches the loop even when finalization throws; does not dispose it |
| `ulong ProcessFrames { get; }` | Completed process callbacks since the process-wide Engine was created; a throwing callback is not counted |
| `ulong PhysicsFrames { get; }` | Fixed callbacks started since the process-wide Engine was created; a throwing callback is counted |
| `double FramesPerSecond { get; }` | Completed process callbacks per accumulated unscaled host second; zero until the first window completes |
| `double PhysicsInterpolationFraction { get; }` | Bounded remaining fixed-interval fraction after the latest scheduling decision |
| `bool IsInPhysicsFrame { get; }` | True only while the current owner thread is inside `MainLoop.PhysicsProcess()` |
| `string ArchitectureName { get; }` | Current process architecture using stable lowercase names for the common x86 and ARM targets |
| `EngineVersionInfo VersionInfo { get; }` | Immutable numeric and informational version read from `Electron2D.dll` |

The inherited typed property list includes timing configuration, metrics, architecture, and version descriptors. Identity, notification, translation, and other inherited behavior follows [`ElectronObject`](ElectronObject.md), except deterministic disposal: the singleton's `ValidateDisposal()` always throws `InvalidOperationException`.

### Named singleton registry

| Member | Current behavior |
| --- | --- |
| `void RegisterSingleton(string name, ElectronObject instance)` | Registers one live instance under a unique nonblank ordinal name without taking ownership; `Engine` and `ProjectSettings` are already occupied |
| `void UnregisterSingleton(string name)` | Removes an existing user name without disposing the object; built-in entries cannot be removed |
| `ElectronObject GetSingleton(string name)` | Returns the registered identity or throws `KeyNotFoundException` |
| `T GetSingleton<T>(string name)` | Adds a checked typed cast and throws `InvalidCastException` on mismatch |
| `bool HasSingleton(string name)` | Tests a validated name |
| `IReadOnlyList<string> GetSingletonList()` | Returns an immutable registration-order snapshot |

Registry operations are serialized by one lock. A registered object can later be disposed because registration is non-owning; the registering component must unregister during teardown. `HasSingleton("Engine")` and `HasSingleton("ProjectSettings")` are always true, and the registration-order list begins with those entries.

## Scheduling and ordering

`AdvanceFrame()` takes one configuration snapshot. A twelve-frame synchronizer uses the current jitter tolerance to keep fixed-step distribution stable while maintaining a fractional accumulator. A long stall is reduced to at most the configured fixed-step budget plus its remainder, so whole excess intervals are discarded rather than creating an unbounded catch-up spiral. The interpolation fraction is published before callbacks.

Each fixed callback increments `PhysicsFrames`, sets `IsInPhysicsFrame`, and clears it in `finally`. A fixed callback returning `true` skips remaining fixed callbacks, but the process callback still runs. The process counter and FPS measurement update only after the process callback returns, then one pending `ProjectSettings.SettingsChanged` invocation is flushed. A callback or settings-event exception propagates and restores the runtime to the running state without implicit finalization. Timing already consumed for the failed attempt is not replayed; a throwing settings event occurs after the process callback has been counted.

`TimeScale` multiplies the fixed delta and the synchronized process delta, not callback frequency. If an extreme finite scale would overflow either effective delta, the frame is rejected with `InvalidOperationException` before user callbacks.

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
- The process-wide `Engine` and `ProjectSettings` registry entries cannot be removed or disposed through Engine.

## Threading guarantees and non-guarantees

The thread that successfully calls `Start()` owns runtime lifecycle and frames until `Stop()` completes. Wrong-thread frame and stop calls fail before changing runtime state. Timing configuration, metrics, `MainLoop` reads, and registry operations are cross-thread safe. User callbacks, event subscription, registered object state, and the attached loop are not made thread-safe by `Engine`.

No background thread, clock, sleep, synchronization context, or native pump is created. Real cadence depends on the host that measures elapsed time and calls `AdvanceFrame()`.

## Official reference coverage inventory

The stable reference API, current implementation header, timing synchronizer, main iteration, and complete `Object` inheritance chain were checked on 2026-09-21.

| Reference area | Electron2D disposition |
| --- | --- |
| Global Engine singleton | Implemented as `Engine.Instance`; process lifetime is enforced by rejecting disposal |
| `physics_ticks_per_second`, `max_physics_steps_per_frame`, `physics_jitter_fix`, `time_scale` | Implemented with typed properties, validation, fixed-step synchronization, catch-up cap, scaling, tests, and typed property descriptors; negative time scale is deliberately rejected because every Electron2D frame callback requires a non-negative delta |
| Main loop lookup | Implemented as nullable `MainLoop`; typed `Start`/`AdvanceFrame`/`Stop` are the explicit host integration API |
| Physics/process frame counters, FPS, interpolation fraction, in-physics query | Implemented as typed properties with documented failure/counting semantics |
| Architecture and version information | Implemented from .NET runtime and loaded assembly metadata; the reference dictionary is adapted to [`EngineVersionInfo`](EngineVersionInfo.md) |
| Register/unregister/has/get/list singleton | Implemented with permanent Engine/ProjectSettings entries, `ElectronObject`, generic typed lookup, explicit errors, registration-order snapshots, and non-owning user lifetime |
| `max_fps` | Deferred until the SDL host owns a monotonic clock and waiting/presentation policy; no inert setting is exposed |
| `print_to_stdout`, `print_error_messages` | Deferred until a logging component provides actual output routes |
| Frames drawn | Deferred until a renderer can report completed draws; returning a fabricated process-frame count is rejected |
| Author, copyright, donor, license map, and license text | Deferred until the distributable has an accepted generated attribution/license manifest |
| Script backtraces and script-language registration/query | Deferred until the scripting domain exists |
| Movie-writer path | Deferred until renderer capture/movie writing exists |
| Editor and embedded-editor queries | Deferred until an editor runtime exists |
| Inherited dynamic call/property/meta/script/signal surface | Governed by the typed adaptations and exclusions in [`ElectronObject`](ElectronObject.md), ADR 0001, and ADR 0002 |

No dependency-blocked item is represented by a stored-but-unused flag, constant-return compatibility method, or empty hook.

## Dependencies and interactions

The class depends on `ElectronObject`, `MainLoop`, `EngineVersionInfo`, `ProjectSettings`, the .NET Base Class Library, and reflection over its own assembly metadata. `SceneTree` can be attached because it is already initialized after successful construction. There is no SDL3-CS, renderer, input, audio, logger, scripting, movie writer, editor, collision-physics, or asset dependency.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies singleton lifetime, project-setting defaults/feature overrides/event flushing, invalid configuration, typed property discovery, architecture/version data, registry validation/type/ownership/order/concurrency, loop publication during initialization/finalization, automatic initialization, existing `SceneTree` attachment, owner-thread enforcement, re-entry rejection, fixed-before-process order, scaling, jitter/interpolation boundaries, catch-up cap, stop combination, process and physics callback failures, failed initialization/finalization cleanup, counters, FPS, and zero steady-state allocation across a warmed empty frame path.

The checks use deterministic supplied deltas, not a real SDL clock, display, renderer, operating-system event pump, or loaded game benchmark. They establish managed scheduling behavior, not hard real-time guarantees or visual acceptance.
