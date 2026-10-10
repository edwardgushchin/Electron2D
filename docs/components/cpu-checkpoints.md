# CPU solver replay checkpoints

Last updated: 2026-10-10

The internal [CPUPhysicsCheckpoint](../classes/CPUPhysicsCheckpoint.md) retains a
CPU solver restore point under ADR 0054/0094. It complements the
[device-local GPU checkpoint](gpu-checkpoints.md). Neither is a public whole-world
snapshot or a portable network message. The common scene/server adapter and
network acceptance remain open in the [physics audit](physics-contract-audit.md).

## State, identity and ownership

Capture deep-copies the managed solver's bodies and motion/force state, shapes,
chain geometry/materials, contacts/manifolds and accumulated impulses, joint
configuration/warm state, island membership, sleeping solver sets, graph colors,
ID pools, spatial trees, pair/move sets, sensor overlap history and completed
body/contact/sensor/joint event buffers. World solver policy and completed tick
metadata are retained. Capture does not advance the world. Every checkpoint owns
independent storage; recapture reuses capacity.

Restore requires the same live world generation, owner thread and body/shape/chain/
joint identities. Topology mismatch rejects before writing logical state. Within
that identity boundary, the CPU solver's geometry, roles and other owned settings
can rewind together with the simulation. External callback/task configuration must
match. Stage-provider hooks reject because their external state is not included.
This does not rewind Electron2D resources, scene nodes, RID adapters, OneWayPair
state or gameplay callbacks. User-data objects and callback contexts remain borrowed
references; their contents are never cloned.

A preparation pass reserves all nested destination storage before the copy pass
changes logical state. Derivable task/arena/constraint/tree-rebuild scratch and
profiling counters remain live; the next step rebuilds its work from restored
persistent state. Capture/restore require an unlocked world with no active workers.
A failed capture copy invalidates that checkpoint. A destroyed/reused world is rejected.
The caller owns and disposes each checkpoint; disposal releases retained arrays and
borrowed references and remains possible after world destruction. No GPU, renderer,
window, serialization or runtime-generated copy code is involved.

## Executable checks and measurement

`ELECTRON2D_TEST_CPU_CHECKPOINT=1` runs `CPUPhysicsCheckpointTests`; the default test
runner also includes it. A focused run completed with DISPLAY/WAYLAND_DISPLAY unset
and SDL_VIDEODRIVER=invalid. Tests exercise:

- Twelve bodies on one worker and 288 on four workers, a contact stack and motor,
  then 60 replay ticks at 1/60 s with four substeps. Tolerances are .0001 m for
  positions, .0001 for dimensionless sine/cosine rotation components and .001 m/s
  or rad/s for velocities,
  well below the solver's .005 m linear slop; sleep identity is exact.
- Independent simultaneous checkpoints, broad-phase changes after moving a body,
  real sleeping-set restoration and a pending force consumed exactly once.
- Sensor begin/end buffers and overlap history, restored spatial queries, chain
  storage including an unused chain slot, a fast thin-wall collision, geometry/mass/
  body-role restoration, foreign-thread/locked-world/topology/lifetime rejection.

On Linux/.NET runtime 10.0.1 (SDK 10.0.101)/Ryzen 7 5700X, 1,024 active dynamic boxes plus a floor with
four workers used 64 warmup cycles and 128 measured capture+restore pairs. The
final focused run measured mean .4309 ms, p50/p95/p99 .3918/.5255/.8132 ms and
zero owner/all-thread managed allocation after warmup. Initial capture allocated
2,531,776 managed bytes; this is an allocation count, not exact retained size or a
bounded network-history budget. Log: `/tmp/e2d-cpu-checkpoint-verified.log`.
The default-runner repetition measured mean .3259 ms, p50/p95/p99
.3201/.3760/.3913 ms with the same zero allocation and initial-capture count;
log: `/tmp/e2d-cpu-checkpoint-verified-full.log`.
These timings cover checkpoints only, not simulation, network correction or FPS;
the GPU checkpoint's circle workload is different and is not a comparison.

The internal [common-world layer](physics-space-checkpoints.md) additionally captures
CPU scene/server attachment and observer state under fixed configuration. Portable
identity, lifecycle rewind, portable authoritative capture/apply, CPU-server/GPU-client reconciliation and separate-process
network validation are still required. Backend-private replay passing here does
not close those requirements or establish cross-platform deterministic replay.

During default-runner integration, the existing spring allocation check reported
4,784 bytes when a background generation-2 collection completed inside its sample.
A separate package-free SpinWait-only program on the same .NET 10.0.1/X64 runtime
reported nonzero deltas in 8 of 256 background-GC intervals (one 744-byte and seven
8,136-byte samples) without Electron2D. This matches the upstream
[allocation-context accounting issue](https://github.com/dotnet/runtime/pull/134855).
A second controlled run gave 0/32 nonzero empty controls, 32/32 exactly 64-byte
positive controls, then 26/256 nonzero background-GC empty intervals. The checkpoint
and spring fixtures now finish background collection before warmup;
the measured work and zero-byte assertions are unchanged. Runtime frame code and GC
policy are unchanged. The temporary solver probes were removed. Evidence:
`/tmp/e2d-spring-alloc-failure.nettrace`, `/tmp/e2d-gc-counter-repro.log`,
`/tmp/e2d-gc-counter-controls.log`.

The common layer is now available to game code through the public
[PhysicsCheckpoint](../classes/PhysicsCheckpoint.md) factory and world-local tick.
Its storage remains same-world and cannot serve as a wire payload.

[Report-only contact verification](physics-report-only.md) exercises restoration
through the public [world checkpoint wrapper](physics-space-checkpoints.md),
including validated receiver caps, observations and identity. The wrapper restores
frame storage and body runtime eligibility; the native CPU snapshot does not own
report-only observer state. GPU reporting retains its existing resident metadata
and device snapshot rather than adding a CPU body-state mirror.
