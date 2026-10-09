# Resident GPU transient forces

Last updated: 2026-10-09

## Implemented boundary

GPUPhysicsBodyStore.ApplyForce journals a world-axis force vector and a resolved
center-of-mass torque. They accumulate independently of constant force and impulse
commands. The device retains them until the next eligible outer tick, following
[ADR 0074](../decisions/physics-forces.md#adr-0074). This is an internal integration
primitive; public PhysicsSpace still uses CPU. The common scene/server adapter,
positioned action projection, explicit direct-state integration and portable replay
checkpoint remain open. No public declarations or coverage states change here.

The existing public force API owns argument validation and positioned moment
resolution. It is not connected to this store yet. A future adapter must preserve
its synchronous accumulated-total validation, call-time rotated-center moment,
scene attachment guards and detached identity. This stage does not claim that
queuing a GPU command implements those public obligations by itself.

## Lifetime and integration

The first force pass of an outer Step/Simulate tick records eligibility on GPU.
Awake dynamic bodies consume queued force over that tick's scheduled force
intervals, using integration-time mass/inertia. Kinematic bodies consume it without
a dynamic response. Static bodies and dynamics asleep at tick entry retain it.
An explicit force call wakes a dynamic body, including zero input; a later explicit
sleep overrides that wake. A body awakened by contact during the tick retains its
queued force until the next outer tick. Contact-only SolveConstraints, zero-time
steps and selected reads neither apply nor consume it.

Transient force is combined with constant force after the once-per-outer-tick
damping operation. It spans all ordinary substeps. CCD impact solves do not repeat
force integration, and the final continuous interval shares the same consumption
boundary. Omission consumes eligible transient force without applying it, while
retaining impulses and constant configuration. Rotation lock suppresses torque
response without preserving the consumed torque for a later unlocked tick.

The last pose pass clears only the input selected at tick entry. This also handles
a body which becomes automatically asleep during that tick. Intermediate reads,
mode changes and mass edits preserve pending force; destruction/reuse starts the
new generation empty. Capacity growth copies pending input GPU-to-GPU.

## Storage and traffic

| Storage or operation | Cost and purpose |
| --- | --- |
| Device transient force | 16 bytes per retained body: force xy, torque and outer-tick eligibility. No CPU mirror. |
| Coalesced body command | 144 bytes, formerly 128; appended vec4 carries only unsubmitted force additions. Successful submission clears CPU staging. |
| Device body / selected snapshot | Unchanged 80 / 48 bytes. Solver/contact hot layouts do not grow. |
| CPU body metadata | Unchanged 88 bytes per reserved slot. It contains configuration and identity, not submitted pending-force totals. |
| Ordinary tick | Existing body passes perform eligibility and consumption. No additional dispatch, fence, status traffic or state readback. |
| Growth | Additional 16 bytes per prior body copied device-to-device; counted in DeviceCopyBytes. |

At 65,536 slots the additional device force buffer and command capacity each cost
1 MiB; CPU metadata plus pending commands totals 15,204,352 bytes (14.5 MiB).
The side buffer is bound only to body integration/edits and sleep-wake integration,
not to the iterative contact/joint kernels. This avoids expanding their hot body
record for an input they do not use.

Nonfinite arguments and overflow within a coalesced CPU command reject before
changing its force channels. Combining an addition with already submitted pending
force is device arithmetic. Overflow there invalidates this internal store at the
next publication boundary, as does nonfinite integration; there is no CPU replay.
Public preflight/rollback across already submitted inputs remains an adapter
obligation, not a guarantee established by this asynchronous internal queue.

## Verification

`ELECTRON2D_TEST_GPU_TRANSIENT_FORCES=1` runs
[GPUPhysicsTransientForceTests](../../tests/Electron2D.Tests/GPUPhysicsTransientForceTests.cs)
and the existing public CPU PhysicsServerForceTests. The full GPU runner includes
the new suite. Initial implementation build rejected a test's null world-joint
endpoint; using the established default BodyHandle representation fixed it.
The subsequent build and first executable focused run passed.

The public CPU oracle uses a 0.1-second step, explicit mass/inertia, linear/angular
damping and independent constant/transient inputs. GPU Step and Simulate with
1/4/8 subdivisions match final linear/angular velocity within 0.0005 scene-unit/s
or rad/s, allowing accumulated float arithmetic. The same comparison exercises all
four body roles, static retention, kinematic consumption, rotation lock, explicit
sleep, zero-input wake and omission with preserved impulses. Queued and separately
flushed additions are both exercised; mass changes before integration use the
new inverse values. Reads, zero-time steps and contact-only solving do not consume.

Additional cases verify late contact wake, automatic sleep during consumption,
thin-wall CCD with tangential force, pin response, device growth, destroyed and
reused generations, owner/thread/lifetime guards and both CPU-queue/device overflow
boundaries. CCD preserves the expected tangential speed 2 ± 0.001 scene-unit/s
through its impact intervals and the next tick; pin linear speed stays within
0.001 scene-unit/s of zero. Identity/lifetime and zero-force results are exact.

The force-edit workload contains 4,096 awake bodies without shapes/contacts,
four substeps and a sixteen-iteration setting; one body's force is edited each tick. It has 128 warmup and 128 measured
ticks. First focused Linux/.NET 10/Vulkan run on NVIDIA GeForce RTX 3090 Ti:
p50 0.7033 ms, p95 1.4321 ms, p99 2.4283 ms, mean fence wait 0.5326 ms.
Owner-thread managed allocation is zero. Each tick uploads 240 bytes (one 144-byte
edit plus 96-byte status resets) and downloads 96-byte status, with no body read.

These are internal-step measurements, not an end-to-end CPU/GPU speedup comparison
or real-window FPS. Native allocation, foreign platforms, public GPU selection,
networking, full callback behavior and owner acceptance remain unverified.


The complete GPU suite also passed, including the resident body/mass/sleep/CCD,
contact/joint/policy/filter/one-way tests and compute-device lifetime with both
renderers. Its force-edit run recorded p50/p95/p99 0.8073/1.5227/2.4397 ms and
mean wait 0.5848 ms, again zero owner-thread managed bytes and 240/96 B traffic.
The separate 65,536-body integration-only control reported 15,204,352 B of authored
body/command capacity, zero warmed managed allocation and unchanged 8/8 B status
traffic. These runs are not a controlled comparative performance certificate.

Shader binaries after the source-native Release build:

| Resource | SHA-256 |
| --- | --- |
| PhysicsResidentBodies.comp.spv | `e041f64fd4082fcc16b4441df368afc34439ae066e5c8fa893a930f485468e34` |
| PhysicsResidentSleep.comp.spv | `166413fc93b5995286a704e930b3fa9ee4f686e77e0d91b2b91913d7b7f0b687` |


Release runtime/test builds with `-p:Electron2DBuildNativeFromSource=true`, runtime
and touched-test formatting checks, `tools/coverage/check.sh`,
`python3 -B tools/shaders/check_runtime.py`, wiki generator tests and generation/check
passed. Coverage remains 11,355 declarations, 8,891 mapped, 2,464 reviewed extras,
zero unmapped; wiki remains 666 public types and 684 files. Logs for this stage use
`/tmp/electron2d-transient-*.log`. The unrelated WaterPlayground changes committed
on main during this work are preserved; this slice changes no example code.
