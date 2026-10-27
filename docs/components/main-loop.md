# Main loop component

Last updated: 2026-10-26

## Scope

This Core component defines the host-facing application lifecycle, variable-step and fixed-step callback lanes, stable system-notification identifiers, typed operating-system permission-result delivery, and internal boundaries for Input transition completion and event forwarding. It does not implement a clock, native event translation, rendering, raw input ownership, or physics simulation. The separate Display domain now owns an SDL window and event pump.

## Owned types

| Type | Role |
| --- | --- |
| [`MainLoop`](../classes/MainLoop.md) | Abstract owner-thread lifecycle and frame contract |

[`SceneTree`](../classes/SceneTree.md) is the concrete Scene-domain implementation.

## Runtime flow

A direct host can call `Initialize()`, drive `Process(delta)` and `PhysicsProcess(delta)` in its chosen cadence, and then call `FinalizeLoop()` or `Dispose()`. Direct calls treat the supplied delta as the effective and original lane delta and original process step. The implemented [`Engine runtime`](engine-runtime.md) is the normal coordinator: `Engine.Start()` initializes a created loop or attaches an already running one, `AdvanceFrame()` schedules fixed callbacks before process while carrying scaled and original lane deltas plus the original process step, and `Stop()` finalizes without disposing. The original timing values are internal callback context used by built-in scene behavior such as [`Timer`](../classes/Timer.md); protected user callbacks continue to receive one effective delta. Hooks are synchronous and non-reentrant. A frame exception returns the loop to running state; initialization and finalization failures are terminal.

The host may deliver system notifications through inherited `Notify(int)` and publishes a permission result through a protected typed endpoint. The owned Android host queues native results for the exact active loop. `SceneTree` forwards system notifications to active nodes.

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

The lifecycle, callbacks, stop results, constants, `Engine` attachment, Input transition integration, error states and `SceneTree` integration execute. `Engine.Run(Window)` owns the ordinary windowed clock, event pump, pacing, exit-code result and cleanup. Those services belong to their respective components; MainLoop itself remains caller-driven. Android permission results have a native producer; OS request/query/revocation APIs remain separate platform work. Runtime exception diagnostics do not establish an operating-system crash handler.

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
under `bin/physics-loop-lifecycle/2026-10-20/`. This callback audit alone does not close
the permission signal, the whole-type audit below, native
allocator totals, hardware performance or another platform. It adds no runtime
API or numerical algorithm.

## Native permission results

The owned Android Activity forwards its SDK permission callback to the SDL base,
then copies permission names and grant decisions to the current window's exact
MainLoop. A weak Activity identity and the current Engine loop identity reject
inactive, replaced or destroyed hosts. A missing decision is denial. Core keeps
only names and booleans; it acquires no Android dependency or permission-request
API. Applications may issue requests through their standard Android bootstrap.

The loop drains a captured FIFO count at its next idle or physics entry, after
Input setup and before the frame hook. Handlers run on the creating thread and
can safely edit scene physics even when numerical simulation uses a separate
thread. Results posted inside a handler wait for another entry. A subscriber
exception consumes its current result and skips that frame hook; the original
exception escapes and unread results remain queued. Signal blocking consumes
results without replay. Custom hosts retain the protected synchronous publisher.

Failed initialization, successful finalization and disposal close the queue and
release pending names; late results cannot migrate to a successor loop. A rejected
lifecycle operation leaves the live queue intact. Queue growth and native string
marshalling are cold work; prepared active and empty full-frame paths allocate
zero managed bytes.

`ELECTRON2D_TEST_LOOP_PERMISSIONS=cpu|gpu`, with optional
`ELECTRON2D_TEST_PHYSICS_THREAD=1`, checks copied batches, four concurrent producers,
FIFO/error/reentry/blocking policy, terminal reference release and real advancing
physical frames. Each selected profile warms 128 and measures 128 permission-active
frames with strict zero owner/all-thread managed bytes. A scoped strict NativeAOT
harness links these same tests; it does not qualify the unrelated aggregate test
project for trimming.

`tests/Permissions.Android` uses SDK requests and actual system dialogs for grant,
denial, mixed two-permission order and a throwing game handler. Its native window
uses explicit CPU physics with separate numerical execution and compatibility
rendering. Before requesting, it validates a positive allocation control, 128
warm and 128 measured full frames at strict zero managed bytes, advancing motion
and a rendered pixel. After the result, eight further physical frames preserve
body identity and selected velocity. The .001 velocity and .02 travel tolerances
cover scene/solver float conversion and accumulation, respectively. The controller
requires an explicitly selected emulator and an initially absent test package,
saves dialog XML/screenshots and removes only its test package after each case:

```bash
python3 -B tests/Permissions.Android/run.py /absolute/path/to/test.apk emulator-5556 /absolute/path/to/new-results
```

Evidence is retained under `bin/physics-permissions/2026-10-26/`. Android emulation
does not qualify physical devices, native allocation totals or performance. Other
platform permission policies and OS permission-request methods retain their own
gates; inherited Object gaps remain on their declaring type.

## Whole-type contract audit

The pinned 4.7.2 MainLoop declares twelve constants, four virtual callbacks and
one permission-result signal. None is deprecated. All seventeen members are
implemented under ADRs 0002/0004/0015/0016. The compiled abstract class derives
directly from ElectronObject and retains the separate SceneTree specialization.
Its 27 own public/protected declarations comprise the type, eighteen mapped
members (the signal has both an event and protected publisher), and eight typed
host/lifetime adaptations: construction, four explicit host entry points,
Dispose(bool), ValidateDisposal and ValidateFinalization. The bidirectional
coverage audit accounts for each; inherited declarations remain on Object.

The existing callback and native-result cohorts are qualified together on the
current runtime under `bin/physics-main-loop-contract/2026-10-26/`. Unchanged
public consumer callbacks execute real selected CPU/NVIDIA GPU/registered worlds
with owner/separate numerics in CLR and strict full-trim NativeAOT. The permission
cohort additionally checks that a derived finalization veto preserves queued
results, future acceptance and both frame lanes. Root lifecycle/Input and physics
clock checks retain default hooks, constants, notification propagation and
scheduler cleanup. CPU processes deny Vulkan device discovery and load no GPU
driver; none of these hosts creates a window or renderer. Original Android dialog
evidence remains in the native-result section above.

The class row now records the complete functional contract of this type.
Object's script initialization/attachment and extension reload remain open in the
inherited physics census. The [compiled construction boundary](object-lifecycle.md#compiled-construction-notification-boundary)
does not require an automatic project notification 0. This audit adds no
runtime API and does not close native heap, foreign-platform, numerical/network,
GPU performance or real-window allocation acceptance. In particular, the separate
[32-byte canvas frame observation](physics-allocation-measurement.md) remains open.

## Decisions

- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0014: Managed Resource lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
