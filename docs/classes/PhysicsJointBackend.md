# PhysicsJointBackend

Last updated: 2026-10-10

[Shared public family checks](../components/physics-joint-policies.md#public-cpu-gpu-conformance)
now run through explicitly selected CPU and independent GPU worlds, including
motion filtering, extreme finite spring caps and begun-GPU-failure lifetime.

**Declaration:** `internal sealed partial class PhysicsJointBackend`

**Source:** [PhysicsJointBackend.cs](../../src/Servers/Physics/PhysicsJointBackend.cs),
[spring evaluation](../../src/Servers/Physics/PhysicsJointBackend.Spring.cs)
**Component:** [Physics joints](../components/physics-joints.md)

## Ownership and operations

One retained adapter belongs to each PhysicsJointRuntime. It owns the current
CPU joint/body handles or a resident GPU joint handle, private compiled local
frames, attached space reference and CPU transient spring impulses. It borrows both endpoint bodies; an empty second
endpoint uses the existing space-owned static world anchor. Public identity and
settings remain with the runtime, independently of attachment generations.

| Operation | Contract |
| --- | --- |
| `SampleLocalFrame` | Use the attached solver pose or detached typed pose, reject unrepresentable local anchors, return a scene-unit Transform with the sampled basis. |
| `Attach` | Compile engine frames/settings into revolute, wheel or filter definitions and acquire one active joint handle. |
| `IsAttached` | Report whether the current constraint exists without exposing its vendor identity to scene classes. |
| `Detach` | Destroy the joint with endpoint wakeup, or skip backend destruction after world failure; clear all handles, frame/impulse state and the space reference. |
| `ApplySolverPolicy` | Resolve inherited bias, convert linear/angular limits to backend units, wake and reacquire transferred records, update policy and clear old impulses. |
| Pin/collision/groove setters | Update live solver settings without resampling the stored anchors; groove distances convert from scene units here. |
| `PrepareSolverStep`, `ValidateSolverStep`, `ApplySolverStep` | Evaluate the existing Hooke/axial-drag impulse, preflight cumulative world motion, then apply equal opposite anchor impulses. |

CPU prepares every spring before validating and applying the batch, preserving
recoverable numeric rejection. The historical stage host retains that CPU phase.
Independent [resident GPU joints](../components/gpu-resident-joints.md) evaluate and
apply springs on device through this adapter; begun execution errors retain the
defined failed-world state.

Frame publication preserves the sampled rotation basis instead of decoding and
rebuilding angles at every reattachment. Engine frames remain in scene units;
backend frames are compiled for the current attachment. The adapter does not
retain a second body world, add callbacks, or own scene objects.

## Verification

Scene pin/groove/spring and raw server suites verify real response, mutable
settings, numeric validation, endpoint lifetime, exception accounting and warmed
zero managed allocations. PhysicsServerJointTests.VerifyFrameReattachment runs
the same public assertions on explicitly selected CPU and GPU worlds: rotated off-center
anchors, motor/limits, world replacement and rejected reconfiguration. It steps
each world 120 times at 1/120 s; anchor error must remain below one scene unit and
the motor angle within the configured limit plus 0.05 rad solver tolerance.
The old stage overload remains a diagnostic check; this is no new throughput measurement.

The [public joint policies](../components/physics-joint-policies.md) now include bias, pin linear softness and common force/correction caps. Native joint copy/clear and GPU stage packets preserve the policy; spring preflight caps the combined impulse before validating all resulting velocities.

## Independent GPU attachment

The adapter also owns resident pin/groove/spring handles. It samples local frames
in scene units, carries the same motor/bias/force/softness settings and retains
common RID and reattachment lifetime. Springs execute in the device solver.
Single-body pins use the GPU world-anchor representation without a hidden CPU body.
See [backend selection](../components/physics-backends.md).
