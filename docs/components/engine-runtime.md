# Engine runtime component

Last updated: 2026-09-24

## Scope

This Core component coordinates one process-wide engine runtime: project-backed timing settings, fixed-step synchronization, time scaling, `MainLoop` attachment/finalization, frame metrics, architecture/version reporting, and a typed named-singleton registry. Engine.Run(Window) owns the ordinary windowed clock, event pump, frame limit, and cleanup on the calling main thread. Manual embedding can still supply elapsed time.

## Owned types

| Type | Role |
| --- | --- |
| [`Engine`](../classes/Engine.md) | Process-wide coordinator, scheduler, metrics owner, and registry |
| [`EngineVersionInfo`](../classes/EngineVersionInfo.md) | Immutable typed assembly-version descriptor |

[`MainLoop`](../classes/MainLoop.md) is owned by the separate [Main loop](main-loop.md) component and attached non-destructively by this component.

## Runtime flow

The host calls `Engine.Start(loop)`. The loop is published before initialization, initialized only if still created, and the successful caller becomes the runtime owner. Startup samples typed locale, fallback and pseudolocalization settings before this initialization; a manually constructed SceneTree has already completed its ready callbacks. Each `AdvanceFrame(elapsed)` samples active project-setting overrides, bounds catch-up, synchronizes fixed-step distribution, publishes interpolation, invokes fixed callbacks before process, updates process-lifetime counters/current-run FPS, and flushes one pending project-settings event. Every callback receives its scaled delta publicly while MainLoop carries the original lane delta and the original process step internally. [`Timer.IgnoreTimeScale`](../classes/Timer.md) subtracts the process step in either lane; the value is captured before the physics catch-up cap adjusts the process callback delta. A stop request is returned to the host. The host calls `Stop()`, which finalizes and detaches the loop without disposing it.

Callback exceptions restore Engine's running state. Initialization/finalization failures return Engine to idle while preserving the loop's terminal lifecycle decision. Re-entry and concurrent lifecycle/frame transitions are rejected atomically.

## Dependencies

The component depends on Core object lifecycle, MainLoop, ProjectSettings, TranslationServer startup configuration, and the permanent Input/InputMap service registrations plus ordinary .NET synchronization, runtime architecture reporting, and assembly metadata. Manual loops are accepted through MainLoop. Engine.Run has a deliberate in-assembly dependency on SceneTree and Window for native scene orchestration; it does not reference SDL types.

## Invariants

- Exactly one `Engine` instance exists and cannot be disposed.
- At most one loop lifecycle is active.
- One owner thread runs startup, frames, and shutdown.
- Fixed callbacks precede process; stop and exception paths always clear the in-physics flag.
- Catch-up work is bounded by a positive configured maximum.
- Timing settings and effective callback deltas are finite; callback deltas are non-negative.
- Original frame deltas and the original process step remain finite/non-negative, are scoped to one callback, and are never reconstructed by dividing through `TimeScale`.
- Process/physics counters are process-lifetime totals; the synchronizer, interpolation, and FPS window reset only after successful loop preparation.
- Registry names are unique ordinal strings; built-in `Engine`, `ProjectSettings`, `Input`, and `InputMap` entries are permanent, and user registry ownership never implies object disposal.
- Warmed empty frame scheduling has no steady-state managed allocation.

## Current implementation status and exclusions

Managed scheduling, lifecycle integration, timing properties, metrics, architecture/version reporting, and registry behavior are implemented and verified. Engine.Run now supplies the monotonic clock, event pumping through Window, MaxFPS waiting and complete scene/window lifetime. Rendering/draw counts, logging flags, generated attribution/license data, script debugging/languages, movie writing, and editor hints remain absent. Their exact reference-API disposition is in the [`Engine` class inventory](../coverage/classes/Engine.md).

## Verification

Executable checks cover success, invalid values/order, wrong threads, lifecycle and frame re-entry, stop requests, long stalls, callback and lifecycle failures, `SceneTree` attachment, locale/fallback and pseudolocalization startup sampling, original process-step Timer behavior at zero scale in both lanes, permanent Input service registration, registry concurrency, metadata, counters/FPS/interpolation, and warmed idle allocation. The separate SDL host checks verify locale and pseudolocalization settings before scene ready, loop ordering, bounded waiting, and cleanup under the dummy driver; they do not prove hard real-time cadence or rendering.

## Decisions

- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0014: Managed Resource lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/core-data-io.md#adr-0019)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)

## Canvas render time

[Canvas animation intervals](canvas-rendering.md#animation-intervals-and-rectangles) use captured scaled process steps and a live typed rollover setting; ordered transform state is replayed alongside retained geometry. The clock is per Engine.Run and also supplies the optional GPU fragment [TIME built-in](shader-materials.md#render-time).
