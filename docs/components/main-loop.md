# Main loop component

Last updated: 2026-09-21

## Scope

This Core component defines the host-facing application lifecycle, variable-step and fixed-step callback lanes, stable system-notification identifiers, typed operating-system permission-result delivery, and internal boundaries for Input transition completion and event forwarding. It does not implement a host, clock, native event translation, rendering, raw input ownership, or physics simulation.

## Owned types

| Type | Role |
| --- | --- |
| [`MainLoop`](../classes/MainLoop.md) | Abstract owner-thread lifecycle and frame contract |

[`SceneTree`](../classes/SceneTree.md) is the concrete Scene-domain implementation.

## Runtime flow

A direct host can call `Initialize()`, drive `Process(delta)` and `PhysicsProcess(delta)` in its chosen cadence, and then call `FinalizeLoop()` or `Dispose()`. Direct calls treat the supplied delta as both effective and original elapsed time. The implemented [`Engine runtime`](engine-runtime.md) is the normal coordinator: `Engine.Start()` initializes a created loop or attaches an already running one, `AdvanceFrame()` schedules fixed callbacks before process while carrying scaled and original deltas, and `Stop()` finalizes without disposing. The original value is internal callback context used by built-in scene behavior such as [`Timer`](../classes/Timer.md); protected user callbacks continue to receive one effective delta. Hooks are synchronous and non-reentrant. A frame exception returns the loop to running state; initialization and finalization failures are terminal.

The host may deliver system notifications through inherited `Notify(int)` and publishes a permission result through a protected typed endpoint supplied to platform integrations. `SceneTree` forwards system notifications to active nodes.

## Dependencies

The component depends on Core object lifecycle, the process-wide Input transition service, and the .NET Base Class Library. The Engine runtime depends on this component, future SDL application hosting depends on both, and `SceneTree` depends on it from the Scene domain.

## Invariants

- The constructing thread owns lifecycle, frames, permission-result publication, and disposal.
- Exactly one successful initialization is paired with at most one finalization callback.
- Frame callbacks execute only while running and never re-enter.
- Effective/original deltas and the selected Input transition lane exist only during the current frame callback and clear in `finally`.
- Finalization reaches a terminal state even when user cleanup throws.
- Disposal does not retry failed initialization or failed finalization.
- The component creates no hidden thread, time source, event pump, or physics work.

## Current implementation status and exclusions

The managed lifecycle, event, callbacks, stop result, constants, `Engine` attachment, Input transition integration, error states, and `SceneTree` integration are implemented and verified. Native system-event generation, permission requests, real clock ownership, frame pacing/waiting, window/application ownership, exit codes, crash integration, and focus synchronization are blocked on the SDL host under ADR 0038. They are absent rather than stubbed.

## Verification

Executable checks cover success, invalid order, delta boundaries, wrong-thread calls, frame and lifecycle callback failures, re-entry, deterministic disposal pairing, permission delivery, numeric constants, SceneTree input/system propagation and finalization, independent Input transition lanes, and warmed idle-frame allocations. They do not exercise an SDL host or real operating-system events.

## Decisions

- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0014: Managed Resource lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
