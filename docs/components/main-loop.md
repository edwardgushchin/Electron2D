# Main loop component

Last updated: 2026-10-20

## Scope

This Core component defines the host-facing application lifecycle, variable-step and fixed-step callback lanes, stable system-notification identifiers, typed operating-system permission-result delivery, and internal boundaries for Input transition completion and event forwarding. It does not implement a clock, native event translation, rendering, raw input ownership, or physics simulation. The separate Display domain now owns an SDL window and event pump.

## Owned types

| Type | Role |
| --- | --- |
| [`MainLoop`](../classes/MainLoop.md) | Abstract owner-thread lifecycle and frame contract |

[`SceneTree`](../classes/SceneTree.md) is the concrete Scene-domain implementation.

## Runtime flow

A direct host can call `Initialize()`, drive `Process(delta)` and `PhysicsProcess(delta)` in its chosen cadence, and then call `FinalizeLoop()` or `Dispose()`. Direct calls treat the supplied delta as the effective and original lane delta and original process step. The implemented [`Engine runtime`](engine-runtime.md) is the normal coordinator: `Engine.Start()` initializes a created loop or attaches an already running one, `AdvanceFrame()` schedules fixed callbacks before process while carrying scaled and original lane deltas plus the original process step, and `Stop()` finalizes without disposing. The original timing values are internal callback context used by built-in scene behavior such as [`Timer`](../classes/Timer.md); protected user callbacks continue to receive one effective delta. Hooks are synchronous and non-reentrant. A frame exception returns the loop to running state; initialization and finalization failures are terminal.

The host may deliver system notifications through inherited `Notify(int)` and publishes a permission result through a protected typed endpoint supplied to platform integrations. `SceneTree` forwards system notifications to active nodes.

## Dependencies

The component depends on Core object lifecycle, the process-wide Input transition service, and the .NET Base Class Library. The Engine runtime drives this contract; its ordinary `Run(Window)` path coordinates native ownership through DisplayServer and RenderingServer. `SceneTree` supplies the concrete scene and physics frame hooks.

## Invariants

- The constructing thread owns lifecycle, frames, permission-result publication, and disposal.
- Exactly one successful initialization is paired with at most one finalization callback.
- Frame callbacks execute only while running and never re-enter.
- Effective/original deltas and the selected Input transition lane exist only during the current frame callback and clear in `finally`.
- Finalization reaches a terminal state even when user cleanup throws.
- Disposal does not retry failed initialization or failed finalization.
- The component creates no hidden thread, time source, event pump, or physics work.

## Current implementation status and exclusions

The lifecycle, callbacks, stop results, constants, `Engine` attachment, Input transition integration, error states and `SceneTree` integration execute. `Engine.Run(Window)` owns the ordinary windowed clock, event pump, pacing, exit-code result and cleanup. Those services belong to their respective components; MainLoop itself remains caller-driven. Native permission requests and results retain their separate platform prerequisite. Runtime exception diagnostics do not establish an operating-system crash handler.

## Verification

Executable checks cover success, invalid order, delta boundaries, wrong-thread calls, frame and lifecycle callback failures, re-entry, deterministic disposal pairing, permission delivery, numeric constants, SceneTree input/system propagation and finalization, independent Input transition lanes, and warmed idle-frame allocations. Optional separate Display tests exercise SDL's dummy video driver but do not prove real operating-system behavior.

## Complete frame and lifecycle callback audit

The four protected hooks preserve the accepted explicit-library-host projection
under ADRs 0015/0016. Initialization runs once before callbacks; successful
initialization is paired with at most one finalization. A failed initializer owns
partial acquisition rollback and cannot be retried. Finalization remains terminal
even if cleanup throws. Direct frame methods preserve their supplied seconds and
boolean stop results; they do not apply Engine time scaling themselves.

Engine supplies scaled fixed deltas and bounded variable deltas. Fixed callbacks
precede the current idle callback. A fixed stop skips the remaining catch-up calls
and still delivers that idle callback; either boolean stop reaches the embedding
host without automatically detaching its loop. `Engine.Stop()` finalizes and
detaches without taking the caller's disposal ownership. Callback exceptions
restore the running phase so the host can handle the error and issue a later
frame. A callback failure does not imply that work already completed by the
application can be rolled back.

The public-only `--loop-lifecycle` consumer implements a custom MainLoop owning a
SceneTree, selected World, moving RigidBody and real point queries. CPU, software
GPU and independently registered implementations run with owner/separate numerics.
It checks direct deltas, half-cadence scheduling, zero scale, the four-step stall
cap, both stop results, foreign and reentrant rejection, original exception
identity, initialization rollback, automatic/explicit finalization and terminal
cleanup. One hundred twenty-eight warm and 128 measured Engine frames at 120 Hz
and scale .5 must produce exactly 128 physical ticks, both callback counts and
1.6 units of travel at speed 3, with strict zero owner/all-thread managed bytes.
The .003-unit travel tolerance covers native float accumulation/unit conversion
while rejecting an extra, omitted or unscaled physical step.

The root `ELECTRON2D_TEST_LOOP_LIFECYCLE=1` and existing physics-clock selector
exercise default hooks, SceneTree hooks, input-lane cleanup, captured timing and
existing lifecycle failure controls. Linux CLR and strict NativeAOT evidence lives
under `bin/physics-loop-lifecycle/2026-10-20/`. This callback audit does not close
the permission signal, the MainLoop/Object class-wide inherited audit, native
allocator totals, hardware performance or another platform. It adds no runtime
API or numerical algorithm.

## Decisions

- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0014: Managed Resource lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
