# Resident GPU kinematic targets and surface velocity

Last updated: 2026-10-10

## Implemented boundary

GPUPhysicsBodyStore now separates prescribed kinematic target motion from virtual
surface motion, following [ADR 0060](../decisions/physics.md#adr-0060),
[ADR 0070](../decisions/physics.md#adr-0070) and
[ADR 0075](../decisions/physics.md#adr-0075). Target velocity derivation, trajectory
sweeps, pose integration and contact/joint response execute on the device.
The [public GPU world adapter](physics-backends.md) now connects target/surface channels, scene poses and queries. Full CharacterBody carry conformance and networking/replay remain open.

BodyDefinition velocity and SetVelocity use physical velocity for dynamic roles,
and configured virtual surface velocity for static/kinematic roles. The latter
never moves geometry. SetKinematicTarget accepts a finite world position and angle
only for a live kinematic body; it replaces the previous pending destination.
SetPose is an explicit teleport and cancels the pending destination. Real role
changes also cancel it; nondynamic entry clears velocity, while dynamic restoration
uses the retained authored surface velocity under the existing raw-body role rule.
These are internal semantic changes, not a new public API or deprecated aliases.

## Step and constraint flow

At outer-tick entry, the body kernel derives center-of-mass travel from the current
device pose, the latest local mass center and pending target. Angular motion uses
the shortest signed arc. The full outer duration sets velocity independently of
the number of substeps. Zero-time Step/Simulate, selected reads and constraint-only
solves do not consume a target. A later surface-velocity edit preserves it.

The final pose interval publishes the exact target position and cosine/sine pair,
then retires the target. That tick's observed velocity still includes target
travel. At the next tick, only consumed target velocity clears; configured virtual
velocity survives. Zero motion skips quaternion normalization and center
reconstruction, avoiding idle drift even with a nonzero custom center.

Contacts use actual plus virtual point velocity for normal, tangent and restitution
response. Pins, grooves and axial springs use actual motion alone: a conveyor does
not move the anchor. Surface edits wake touching sleepers through the existing
connected graph. Selected Snapshot.Velocity publishes the combined observable
channel in the now 64-byte record with resolved fields. Scene queries and contact-event snapshots
still require their normal public backend adapter.

Moving kinematics participate in full-shape continuous sweeps against dynamic
peers even when the peer's authored CCD mode is Disabled. This does not change
that peer's policy or give static/kinematic pairs a physical response. Bounds and
TOI geometry samples actual motion only, so virtual surface speed cannot enlarge
a swept path. After an impact, residual contact closing uses actual plus virtual
point velocity, matching the solved constraint. Relative motion, angular arcs, one-way episodes and body/joint filtering
reuse the resident continuous pipeline. Its existing finite iteration budget and
failed-store behavior remain unchanged. Step is still an integration-only control;
Simulate supplies contact, joint, sleep and continuous response.

## Storage and cost

| Storage | Current payload and purpose |
| --- | --- |
| Device body | 96 bytes, previously 80; a vec4 stores configured linear/angular surface velocity used by contacts. |
| Device target | 16 bytes per retained slot; authoritative pending destination, copied on growth without a CPU pose mirror. Existing body flags mark pending/consuming state. |
| CPU slot | Now 112 bytes including damping modes; retains authored surface configuration for mode changes, never current target travel or solved pose/velocity. |
| Coalesced body command | 176 bytes, previously 144; includes the surface body field and latest target. Consumed staging entries are cleared. |
| Snapshot | Now 64 bytes; pose, combined velocity, resolved fields, role and sleep/policy flags. |
| Growth | Copies 136 bytes per prior slot for body, center, transient force and target, counted in DeviceCopyBytes. |

At 65,536 slots, device body/center/force/target/field payload is 9.5 MiB; retained CPU
metadata/command capacity is 18,874,368 bytes (18 MiB). Device command/request/result
scratch and upload/download transfer payloads are each 16 MiB, plus their status
headers. These are payload capacities, excluding driver/object overhead and other
physics buffers. Body target setup/consumption uses existing dispatches and fences;
fast-path CCD uses its already documented status/fraction waits. No full body read
is needed to calculate target velocity.

## Verification

`ELECTRON2D_TEST_GPU_KINEMATIC=1` runs
[GPUPhysicsKinematicTests](../../tests/Electron2D.Tests/GPUPhysicsKinematicTests.cs),
existing CPU PhysicsSurfaceVelocityTests and PhysicsServerStateTests. The complete
GPU runner also includes the new suite. Earlier resident tests that used an
internal velocity to move a kinematic body now issue actual destinations instead.

The CPU oracle uses only public PhysicsServer operations. At duration 0.1 s it
compares stationary virtual velocity, replaced targets, custom center (3,-2),
combined linear/angular velocity and consumed-target clearing. GPU Step and
Simulate with 1/4/8 substeps are exercised. GPU destination position is exact;
analytic center-travel velocity allows 0.0001 scene-unit/s and shortest-arc angular
velocity 0.00001 rad/s. CPU comparisons allow 0.02 scene units at the origin,
0.1 scene-unit/s for center velocity and 0.05 rad/s angular velocity. The CPU uses
an approximate trigonometric rotation/angle estimator; its rotation error is
amplified by center offset divided by step duration. For this 0.1-rad test the
observed velocity discrepancy is about 0.046 scene-unit/s. The initial 0.02
velocity tolerance therefore failed; source inspection justified the bounded
CPU tolerance, and the separate tighter analytic check prevents concealing a GPU
integration error. No backend-internal arrays or traversal ordering are compared.

Ordering checks exercise queued versus separately flushed target/surface/teleport/
role/mass edits, device growth, destroyed/reused generations and zero-time work.
Sixteen target angles retain identical pose bits over subsequent idle ticks.
Contact checks exercise normal and angular surface response, waking and stationary
geometry; pin and spring checks separate virtual motion from actual anchor travel.
A fast kinematic circle moves 80 units in 0.02 s and pushes a sleeping, default-CCD
circle ahead of it. A nearly half-turn rod catches a default-CCD circle missed by
both endpoint poses. Directed collision exceptions remain effective. Additional opposite/equal normal
surface speeds cancel or double the moving platform's contact velocity. The
cancellation case initially exhausted the 128-interval CCD budget: residual closing
used geometry speed even after the combined contact velocity had been satisfied.
Using the same combined endpoint velocity as the solver fixes that residual check
without changing geometric samples, first-impact detection or the interval budget.
The mixed case checks zero/8,000 scene-unit/s contact response with 0.02 tolerance
and exact completion of the platform target. Existing
one-way/CCD regressions retain moving and rotating geometry through target commands.

Invalid/nonfinite/kind/foreign/thread/stale/disposed edits are checked. A finite
target whose derived velocity overflows produces an explicit failed store without
CPU replay. Invalid authoring edits preserve an earlier valid target. Initial test
compilation caught an ambiguous target-typed helper argument; naming Vector2
resolved it before the executable checks.

The allocation workload contains 4,096 kinematic bodies without shapes; all 4,096
targets change per tick. It uses four substeps, a sixteen-iteration setting and
128 warmup/128 measured ticks. The first successful focused Linux/.NET 10.0.1,
Vulkan/NVIDIA GeForce RTX 3090 Ti run reported p50/p95/p99
1.3598/10.9781/12.7366 ms and mean fence wait 3.0068 ms. Owner-thread managed
allocation is zero. Each tick uploads 720,992 bytes (4,096 authored commands plus
96 bytes of status reset) and downloads 96 bytes of status. A full population read
outside the timed interval confirms every final destination. This is target-update
cost, not a controlled whole-CPU/GPU comparison or real-window FPS. Native
allocation, foreign platforms, public integration, networking and owner acceptance
remain unverified.


After the mixed-surface CCD repair, the focused run passed at p50/p95/p99
0.8871/1.4519/2.4389 ms, mean wait 0.5815 ms. A repeated complete GPU suite passed,
including both renderer/device lifetimes and all existing resident body, mass,
transient force, sleep, CCD, contact, joint, filter and one-way checks. Its target
workload reported 0.9477/1.6133/2.5158 ms, mean wait 0.5927 ms. Both retain zero
owner-thread managed allocation and the same exact transfer counts. Timing
variation is reported, not interpreted as a controlled speedup.

The 65,536-body integration-only control confirms 18,350,080 B of authored body/
command capacity, zero warmed managed bytes and unchanged 8/8 B status traffic.
Release runtime/test builds use `-p:Electron2DBuildNativeFromSource=true`.
Runtime/touched-test formatting, compiled API coverage, shader generation and wiki
checks are recorded with `/tmp/electron2d-kinematic-*.log` artifacts. Public GPU
world/network completion is not inferred from these internal tests.

| Resource | SHA-256 |
| --- | --- |
| PhysicsResidentBodies.comp.spv | `f1db6b1a447782f41a43f0627e95b392e60569d95a59dbc82dfe6760e6eb96c9` |
| PhysicsResidentCCD.comp.spv | `c5f87ff0c96be01568babea6f85649e53897c4c585b66e90a6f1410a4ac199d5` |
| PhysicsResidentSolve.comp.spv | `d6f1157c1d6b0f9c9f29451b64d73053f6afd36fdae891de75cdccc2f5c3089b` |
| PhysicsResidentUpdate.comp.spv | `a21a68efcf9957d57db39debc491bd73b14749cfad1d724ad8b8eb992e0d8f72` |

Current [integration batching](gpu-contact-solver.md#integration-batching-measurements)
reduces the shape-free four-substep status budget to 72 upload / 80 readback bytes
per tick with pending edits. Historical measurements above retain their original
submission policy and payloads. Current tests keep exact traffic and zero-allocation assertions.
