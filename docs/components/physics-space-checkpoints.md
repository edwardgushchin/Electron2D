# Local physics-world checkpoints

Last updated: 2026-10-10

The public `PhysicsCheckpoint`, created by `PhysicsServer.SpaceCreateCheckpoint`,
uses the internal `PhysicsSpace.Checkpoint` to connect [CPU solver history](cpu-checkpoints.md)
and [GPU resident history](gpu-checkpoints.md) to attached scene/server state. It is
one reusable rewind point in the same live world, with fixed identities and authored
configuration. No wire format is introduced. This is a prerequisite
for the open networking work in the [contract audit](physics-contract-audit.md).

## Public tick contract

`PhysicsServer.SpaceGetTick(space)` starts at zero and advances once per active,
positive-duration world interval, including an empty world. Local/global suspension
and zero duration leave it unchanged. A solved interval retains its tick when a
result callback throws. Counter exhaustion rejects before running another interval.
The counter is independent of scene frames and network packet sequences. It does
not enforce a fixed duration; a fixed-tick game passes the same duration each time.

The public point exposes `Tick`, `IsDisposed`, `Capture`, `Restore` and `Dispose`.
Capture retains the actual world tick; restore rewinds it together with simulation
state. Other worlds, scene clocks, timers and script state retain their current
values. Both scene-owned and server-owned spaces use the same API. Expired RIDs,
foreign-thread access and callback/in-progress-frame capture reject. A source world
releases its points, after which public operations fail as disposed. The engine
never recovers a failed GPU world by replaying its checkpoint on CPU.

```csharp
using var point = PhysicsServer.SpaceCreateCheckpoint(world.Space);
ulong confirmedTick = point.Tick;
// Apply numbered input and advance the scene/world at the fixed game step.
point.Restore(); // Simulation and this world's tick return to confirmedTick.
// The caller restores game state and replays the still-unconfirmed input.
```

This is an executable local history prerequisite. Portable authoritative state,
network identities, input ownership/acknowledgement, lifecycle rewind and confirmed
events remain open in the contract audit. A checkpoint cannot be sent to another
process or applied to a different backend/world.

## State and boundaries

Capture prepares pending shape/pose/joint authoring, then saves the kernel and common
state without advancing a physics tick. Each point owns its saved data and reuses
high-water capacities on recapture. CPU data uses explicit typed copies; GPU solver
history uses device-local copies. Common capture retains already published host
poses, observer contacts and qualified GPU caches; it does not request a new full
GPU body-state readback. The kernel's command flushing and copy fence are still
included in checkpoint timing.

Common state includes pending and persistent forces, resolved fields, static surface
velocity, scene and server motion/sleep, animatable and character targets, character
slide results, CPU one-way episode history, completed contact impulses/reports,
scene contact/Area pairs, server Area pairs, world tick metadata and statistics.
Existing direct-body views retain their identity and immediately expose restored
contacts. Disposed views are not resurrected. Collision-owner identities remain
borrowed local object references.

Restore validates world/body/shape/joint attachment identity, geometry/material
revisions, filtering/exceptions, object/canvas identities, CCD, body and field
configuration, monitoring callbacks and solver policy before copying live state.
Pending scene-joint reconstruction also rejects. A normal validation failure leaves
the world usable. An exception after the kernel restore starts makes the common
world unusable until disposal; it is never silently recomputed with another backend.
Checkpoints require the owner thread and completed event dispatch. Disposing the
world disposes its points, including retained GPU buffers; explicit disposal is
idempotent.

Physics poses restore parent-first through an internal silent transform path. It
invalidates global transforms and presentation interpolation, and discards queued
future transform notifications for affected descendants. Restoring never invokes
transform, integration, contact, sleep or Area callbacks. Subsequent simulation
emits events from the restored pair history. Replaying a future exit emits that
exit again: prediction/confirmation and gameplay-effect deduplication still require
the network event layer. Generic node/script state, timers, interpolation of remote
snapshots, resource authoring and lifecycle changes are outside this point. Physical
bone runtime follows its rigid-body state; skeleton/gameplay state is not cloned.

## Checks and measurements

`ELECTRON2D_TEST_PUBLIC_CHECKPOINT=cpu` or `gpu` exercises the public factory,
numbered-input replay, empty/inactive/zero intervals, callback failure and tick
visibility, independent worlds, lifetime/thread rejection and warmed zero allocation.
`ELECTRON2D_TEST_SPACE_CHECKPOINT=cpu` or `gpu` runs the underlying common-world suite.
CPU is also included in the default test runner. Checks cover scene and server
contacts/direct views, scene and server Area history, callback-silent restore,
no invented enter on the first restored interval, reproducible exits, pending
forces/targets, sleeping motion, moved parents, character motion, motor joints,
one-way episodes, CCD, a gravity edit pending at capture, configuration/lifetime/thread/callback rejection and zero
warmed managed allocation. Lower-level checkpoint suites separately cover storage
growth, solver histories and GPU copy failure.

Same-backend replay allows .02 scene units of position error, .1 units/s velocity,
.002 radians and .02 rad/s angular velocity, with exact sleeping flags. Immediate
restoration allows only .0001 position/velocity unit conversion rounding; contact
counts, normals and impulses are exact. These bounds reject lost impulses/ticks
without requiring cross-backend or cross-platform bitwise agreement.

The warmed fixture uses 64 dynamic circles plus a floor, 90 settling ticks at 1/60 s,
64 warmup and 64 measured capture+restore pairs. Both owner-thread and all-thread
allocation counters must remain zero. A blocking collection before warmup avoids
the runtime accounting issue documented in [CPU checkpoints](cpu-checkpoints.md).
These measurements include the common layer and backend copy/fence, but exclude
simulation, rendering and network traffic. They do not establish GPU simulation
speedup or a bounded network-history budget. The remaining portable capture/apply,
network identity, lifecycle rewind and separate-process CPU authority/GPU client
acceptance stay open.

On the local Linux/.NET 10.0.1 host, the final focused runs measured:

| Backend | Mean capture + restore | p50 / p95 / p99 | Owner / all-thread managed allocation | Initial managed capture allocation |
| --- | --- | --- | --- | --- |
| CPU | .0684 ms | .0679 / .0710 / .0807 ms | 0 / 0 B | 342,512 B |
| GPU | .1874 ms | .1754 / .2496 / .2669 ms | 0 / 0 B | 296,344 B |

GPU capture/restore transferred no upload/readback payload in the 64 warmed pairs.
Device copies totalled 5,103,104 B (79,736 B/pair); aggregate fence waiting was
4.4281 ms (.0692 ms/pair). The retained device checkpoint capacity was 39,868 B.
Initial managed allocation is not an exact retained-memory measure. This small
fixture's GPU copies are slower than CPU memory copies; no simulation speedup is
claimed. Logs: `/tmp/e2d-space-cpu-cache-final.log`, `/tmp/e2d-space-gpu-cache-final.log`.
The CPU run also asserted graphics services stayed unavailable with both display
environment variables removed and an invalid SDL video driver. The default suite
completed with `Electron2D checks passed`; the resident kernel checkpoint suite
also passed its contact/joint/CCD/history-growth/copy-failure and allocation checks.

A regression case changes the default world gravity immediately before capture.
The CPU solver and its host gravity cache must rewind together, so the first replay
step still applies that pending authored value. Before the fix, six replay ticks
produced Y=6.882285 versus 14.25515 expected; both backends now pass the shared
pending-world-setting check. No hidden simulation step is used to prepare capture.
