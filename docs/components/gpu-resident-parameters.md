# Resident GPU body parameters

Last updated: 2026-10-09

## Implemented boundary

GPUPhysicsBodyStore now changes body roles and integration policies in place.
The internal `SetMode`/`GetMode` pair uses PhysicsServer.BodyMode;
`SetIntegrationPolicy`/`GetIntegrationPolicy` retain gravity scale, authored signed
linear/angular damping with independent Combine/Replace modes, a dynamic rotation
lock and default-force omission.
These are executable device settings, with comparisons against the public CPU
contract. The [public scene/server adapter](physics-backends.md) now supplies roles, Area fields and forces. Full parameter-family conformance on that path remains open.
No public declarations or coverage states change in this stage.

Changing mode preserves the body handle, pose, authored mass/center/inertia,
constant forces, configured CCD and attached shapes, joints and exceptions.
A real Static or Kinematic transition clears motion. RigidLinear clears angular
motion; restoring Rigid leaves it zero until another action supplies it. A
same-mode write is harmless. Static velocities represent virtual surface motion;
kinematic velocities now configure a separate virtual channel alongside
[device-derived target travel](gpu-resident-kinematic.md). Targets ignore forces/
impulses; only actual travel moves geometry and joint anchors. The public scene/server adapter now separates actual target travel from virtual surface velocity.

The authored rotation lock is effective on dynamic roles; nondynamic modes retain
the setting without suppressing their commanded/virtual angular velocity.
RigidLinear enforces its lock independently of the authored flag. Omission skips
automatic gravity, damping and persistent force/torque integration; it does not
disable movement, impulses or contact/joint constraints. CPU scene Freeze and
disable policies retain additional scene configuration and still require their
normal projection when attaching this backend.

## Ordered edits and state lifetime

All changes share the 176-byte per-body command (including
[transient force](gpu-resident-forces.md) and [kinematic target](gpu-resident-kinematic.md) input). Mode/policy fields have
separate masks from pose, velocity, force and mass edits. Angular clearing is an
ordered side effect: lock then unlock cannot resurrect old spin, while a later
explicit unlocked velocity or impulse can supply new spin. A transient
nondynamic role similarly clears velocity even if the final role returns to
dynamic before submission. Initial sleeping/locked definitions discard the
disallowed velocity before later queued wake/unlock operations can observe it.

Role edits update active CCD participation without discarding the configured mode,
wake the body, advance the solver-history epoch and retire shape contact revisions.
Existing joint frames and handles remain valid. Policy changes wake existing
bodies and advance their impulse-history epoch; structural wake propagation uses
the existing device component graph. A later explicit sleep assignment wins over
an earlier wake. Initial policy edits preserve the other creation flags, including
can-sleep and CCD. Invalid enum/nonfinite input rejects before authoring changes;
stale, foreign, wrong-thread, disposed and failed-store guards are unchanged.

The selected Snapshot (now 64 bytes with [resolved fields](gpu-resident-fields.md)) uses a reserved word for the actual role and
reports effective rotation lock/omission through its existing flags. CPU retains
only authored integration configuration, never a current velocity/pose mirror.
Metadata is now 112 bytes per retained body including authored surface velocity and damping modes; command/device-body/snapshot
sizes are now 176/96/64 bytes. At 65,536 bodies the metadata/command capacity is
18,874,368 bytes (18 MiB), excluding shapes, joints and driver/object overhead.

## Damping cadence

The accepted [field contract](../decisions/physics-fields.md#adr-0056) applies
`max(0, 1 - outerDelta * damping)` once before that tick's forces. The resident
solver previously repeated damping at every substep, changing both attenuation
and the damping of gravity/force contributions. Simulate now passes the outer
duration to the first force stage and zero damping duration to later substeps.
Gravity and persistent forces still integrate across their scheduled durations.
Integration-only Step retains its one-step equation.

The existing body/sleep uniform spare lanes carry this duration without increasing
their payload. A newly awakened zero-velocity sleeper receives its eligible force
interval. CCD impact splits do not repeat damping or earlier force work. Signed
negative damping amplifies velocity before forces; sufficiently large positive
damping clamps the old velocity to zero. Omitted integration skips all those
default effects while preserving configured values for later restoration.

## Verification

`ELECTRON2D_TEST_GPU_RESIDENT_PARAMETERS=1` runs
[GPUPhysicsBodyParameterTests](../../tests/Electron2D.Tests/GPUPhysicsBodyParameterTests.cs)
and existing public CPU PhysicsBodyParameterTests. The full GPU runner includes
the new suite. A public CPU space supplies the field and mode oracle; no native
internal layout/order is compared.

Field cases use mass 2, inertia 4, initial velocity (10,20), angular velocity 4,
gravity (0,30), force (4,6), torque 8 and duration 0.1. They cover signed gravity,
positive/negative/clamped damping and omission. The GPU uses 1, 4 and 8 substeps;
all final velocities agree with CPU within 0.0005 scene-unit/s or rad/s. All
sixteen source/target role pairs agree with CPU motion reset/retention within
0.0001. Configured profiles, handles and reported roles remain exact.

Ordered-action checks compare coalesced operations with reads between operations,
including initial sleep/lock, intermediate static mode, ignored angular writes
while locked and impulses after unlocking. Contact and joint tests verify that
omission still resolves collisions and that a restored dynamic endpoint keeps its
joint/shape/exception identity. Sleep, numeric rollback, foreign/thread guards,
nonfinite device failure and disposal are exercised. A thin-wall CCD case activates
retained CCD after Static→Rigid, keeps collision-tangential velocity 96 ± 0.001 after
the once-per-tick damping factor, and preserves the no-response rule between kinematic and static peers.
Moving kinematic/dynamic pairs now use their own continuous target path.

The historical residency run before transient-force storage has 4,096 bodies, four substeps and sixteen solver
iterations. Each tick switches one body through Static→Rigid, changes its policy
and writes velocity. After 128 warmup and 128 measured ticks, it allocates zero
owner-thread managed bytes and transfers exactly one 128-byte edit plus 96 bytes
of status uploads, with 96 bytes of status readback. No body state is downloaded
during measurement. The first focused run recorded p50 0.7307 ms, p95 1.1354 ms,
p99 2.5922 ms and mean wait 0.5179 ms on Vulkan / NVIDIA GeForce RTX 3090 Ti;
evidence is `/tmp/electron2d-resident-parameters-focused1.log`.

These are regression timings on a shared Linux/.NET 10 desktop, not a full public
CPU/GPU throughput comparison or window FPS. Native allocations, other devices,
public backend selection/publication, portable state/replay and network acceptance
remain unverified or unfinished. Generated shaders/wiki and temporary logs are
not committed.


Final checks on 2026-10-09 passed after preserving the concurrent WaterPlayground
main update: source-native Release runtime/test builds, focused CPU/GPU parameter
runner, full GPU runner including both renderer compute lifetimes, runtime and
touched-test formatting, runtime shader delivery, compiled API coverage and wiki
tests/generation/check. Logs: `/tmp/electron2d-resident-parameters-final-focused.log`,
`/tmp/electron2d-resident-parameters-final-gpu.log` and
`/tmp/electron2d-resident-parameters-coverage.log`. The final assembled surface has
11,355 accounted declarations and zero unmapped; the increase belongs to the
concurrent display work, while this stage adds no public declarations.

Source-generated shader SHA-256:

| Resource | SHA-256 |
| --- | --- |
| PhysicsResidentBodies.comp.spv | `e9931c4062f60ebd681387967a848c5faf32b29ff3ea693db5529244a35ece7d` |
| PhysicsResidentSleep.comp.spv | `3547e4a9aed18e2c9b05ee00afbca440e0dbfe60046e4f57354c8258cf6a195b` |
