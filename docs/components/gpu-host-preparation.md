# GPU world host preparation and pose reads

Last updated: 2026-10-10

## Authored integration changes

The common GPU space previously rebuilt and compared every body's integration
policy on every tick. The attachment now synchronizes authored parameters when
they change. PhysicsBodyRuntime tracks raw-body edits in its typed setters;
RigidBody marks scene edits, shared constant-force operations and replay.
Preparation clears markers only after successful synchronization. A fresh GPU
attachment, changed solver role, direct gravity/rotation-lock update or replay
invalidates prepared state. Reentry cannot reuse another attachment's status.
Same-value raw writes retain their authored value, including signed zero, even
when no device update is needed. No public signature changes.

| Input family | Invalidation path |
| --- | --- |
| Gravity scale, signed damping and Combine/Replace | Scene setters or shared raw-runtime typed setters |
| Constant force/torque and force omission | Scene/shared force setters, direct views and replay |
| CCD | Shared scene/server runtime setter |
| Freeze, rotation lock and body role | Scene edits plus backend role/lock invalidation |
| Reentry and checkpoint/portable correction | Fresh attachment or restored runtime/adapter state |

World/Area fields still resolve on GPU. Geometry/material revisions, report
selection, callback registration, pending forces and motion targets retain their
independent preparation. Owner, phase, lifetime and failed-world checks remain;
the marker does not authorize an old body/view binding or skip physical work.

Parameter synchronization shares the existing callback/activity scan and finishes
before the shared pre-step publication. A changed policy can wake a body and
invalidate the world state epoch. Applying it after publication caused each
following force consumer to flush wakes and read that body again. Preparing all
policies first preserves the single shared snapshot even during mass live edits.

## Current transforms

The GPU snapshot already contains the unit rotation basis. GetTransform copies
it and the origin instead of decoding an angle and reconstructing the matrix.
Raw-body getters, direct views, scene publication, character/animatable pose
capture, GPU Area scans and sampled joint frames share this adapter operation.
It uses the existing current snapshot, without another host mirror or readback.
Intermediate-edit epochs and selected-read invalidation remain unchanged.

The CPU adapter retains its published-angle convention. Scalar-angle consumers
continue using GetPose; unchanged scene poses avoid rebuilding their transform.
Both paths expose finite unit-scale, unskewed physical transforms within the
existing floating-point tolerances.

## Verification

[PhysicsHostPreparationTests](../../tests/Electron2D.Tests/PhysicsHostPreparationTests.cs)
uses explicit public CPU/GPU worlds and raw/scene owners. It changes force, gravity,
damping/modes, omission, lock and freeze after prepared frames, checks actual motion
and resolved fields, restores constant forces from a checkpoint, tests reentry and
stale views, preserves signed zero and compares transform views at axis/wrap angles.
Velocity/field tolerance is .002 scene units or rad/s for fixed 1/60 s analytic
steps. Basis lengths squared, dot product and determinant allow .000002, covering
float normalization without accepting physical scale or skew.

A separate test changes CastRay → Disabled → CastShape after preparation: 3000 u/s,
.02 s ticks, radius one, wall thickness .2 at x=20. Continuous modes stop before
the wall; Disabled crosses x=25. Actual public backend identity is checked. This
live-edit case does not accept every CCD shape/trajectory combination or use the
historical CPU-hosted GPU-stage harness as independent-backend evidence.

A 32-body public GPU callback case applies pending forces and changes every body's
gravity scale after warmup. Before the ordering fix, one step made 107 submissions
against 11 for ordinary preparation. After the fix, both make 11; each callback
still runs once and observes the analytically expected force/gravity velocity.
The regression permits at most four extra submissions rather than encoding the
current solver's exact total.

```sh
ELECTRON2D_TEST_PHYSICS_HOST_PREPARATION=1 dotnet run --project tests/Electron2D.Tests -c Release
```

The default suite includes its CPU cases. Shared publication, public CPU/GPU motion
scenes, parameters, selected-world failure/fallback, checkpoints, portable snapshots
and separate-process network correction also exercise this change. Full contract
acceptance remains governed by the [physics audit](physics-contract-audit.md).

## Measurement controls

PhysicsMassPerformance keeps the dense world and real window from the
[contact report](gpu-contact-colors.md#public-workload-and-native-window).
ELECTRON2D_MASS_REFRESH_PARAMETERS=1 forces per-tick policy comparison;
ELECTRON2D_MASS_DECODE_TRANSFORMS=1 reconstructs GPU matrices through angles.
These internal test controls allow a same-build comparison, not public modes.
Both default to optimized behavior. GPUPreparationMeanMS reports policies/activity/
motion/joints, report selection, wake publication and its included wait. PublishMS
measures the public-pose/MultiMesh consumer separately.

Body count, four substeps, 16 iterations, fixed physical time and energy/penetration
checks remain unchanged. Reporting/capture stay outside warmed allocation brackets.
Managed allocations and native-driver allocation rates are separate measurements.

## Full public native-window measurement, 2026-10-10

One Release build on Linux Wayland/.NET 10, Ryzen 7 5700X, RTX 3090 Ti/Vulkan.
The three sequential runs use 65,536 awake radius-four circles, three static
walls, four substeps, 16 iterations, 240 warmup ticks and 240 measured ticks.
Each physical tick advances 1/60 s; wall-clock FPS is measured independently.
Both GPU configurations include the corrected pre-step policy ordering. The
control forces repeated policy comparison and angle reconstruction; it is not
a separate old binary or a CPU-hosted GPU stage.

| Full backend | Step p50 / p95 / p99, ms | GPU policies/activity/motion/joints mean, ms | Pose-to-MultiMesh p50, ms | Window FPS |
| --- | --- | --- | --- | --- |
| CPU Box2D.NET | 247.46 / 266.18 / 309.66 | — | 12.56 | 3.73 |
| GPU repeated-policy/angle control | 55.21 / 62.21 / 73.25 | 8.48 | 9.82 | 13.84 |
| GPU prepared policy/direct basis | 51.11 / 53.31 / 57.48 | 4.73 | 6.42 | 15.73 |

The GPU median full step improves by 7.4% against the same-build control; FPS
improves by 13.7%. CPU remains explicitly selectable. All three runs retain
65,536 active bodies and measure **0 owner/all-thread managed bytes** in warmed
physical steps and full rendered frames. Maximum penetration is .26941 for CPU,
.30057 for GPU control and .30055 for GPU candidate (radius four, allowed
penetration .3; the dense-scene acceptance bound is .601). No body escapes;
final energy stays below initial energy. GPU contact coloring uses no fallback.

Both GPU runs make 19 submissions per tick, upload 240 bytes excluding uniforms and
read back 5,243,640 bytes per tick; this change adds no readback or host mirror.
The full moving-body publication supplies public pose reads for all rendered
instances; its existing cost remains visible. The candidate spends mean 31.28 ms
waiting for GPU work, included in the full step, versus 31.73 ms for the control.
Resident simulation/debug preparation is still the largest measured phase at
35.69 ms mean. The prepared-policy change reduces host work rather than GPU
physical work. Renderer traffic and uniform bytes are recorded separately in JSON.

The real 1152×800 window uses the GPU renderer. The driver reports VSync Enabled;
MaxFPS is zero and physics catch-up is capped at one for separate measured ticks.
The captured candidate image was inspected: the dense pile remains spread over
the physical container. These short local samples include scheduling variance;
p95/p99 and the capture are not cross-device or visual owner acceptance. The
65,536-body 60 Hz/FPS target, native-driver allocation rates and full backend
contract acceptance remain open.

Local ignored evidence is retained under
`bin/physics-host-validation/2026-10-10/final/`: `checks.json`, functional logs,
`cpu/CPU-65536-window.json`, `gpu-control/GPU-65536-window.json`,
`gpu-candidate/GPU-65536-window.json` and their actual-window PNGs. The RED/green
callback logs and earlier development comparisons are stored beside that folder.

```sh
ELECTRON2D_TEST_PHYSICS_MASS=window ELECTRON2D_MASS_COUNT=65536 \
ELECTRON2D_MASS_BACKEND=gpu \
ELECTRON2D_MASS_OUTPUT=bin/physics-mass-host-gpu \
dotnet run --project tests/Electron2D.Tests -c Release
```

Use `ELECTRON2D_MASS_BACKEND=cpu` for the same CPU workload. For the GPU control,
add `ELECTRON2D_MASS_REFRESH_PARAMETERS=1` and
`ELECTRON2D_MASS_DECODE_TRANSFORMS=1`, keeping the other parameters unchanged.
