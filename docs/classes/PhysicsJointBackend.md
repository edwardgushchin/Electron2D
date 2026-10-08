# PhysicsJointBackend

Last updated: 2026-10-09

**Declaration:** `internal sealed partial class PhysicsJointBackend`

**Source:** [PhysicsJointBackend.cs](../../src/Servers/Physics/PhysicsJointBackend.cs),
[spring evaluation](../../src/Servers/Physics/PhysicsJointBackend.Spring.cs)
**Component:** [Physics joints](../components/physics-joints.md)

## Ownership and operations

One retained adapter belongs to each PhysicsJointRuntime. It owns the current
Box2D joint/body handles, private compiled local frames, attached space reference
and transient spring impulses. It borrows both endpoint bodies; an empty second
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

The world still prepares every spring before validating every spring and applying
any impulse. This preserves numeric rejection without partially applying earlier
springs. These stages remain CPU operations even in the current GPU stage-host
experiment. Independent [resident GPU joints](../components/gpu-resident-joints.md) execute separately; their public-world adapter remains open.

Frame publication preserves the sampled rotation basis instead of decoding and
rebuilding angles at every reattachment. Engine frames remain in scene units;
backend frames are compiled for the current attachment. The adapter does not
retain a second body world, add callbacks, or own scene objects.

## Verification

Scene pin/groove/spring and raw server suites verify real response, mutable
settings, numeric validation, endpoint lifetime, exception accounting and warmed
zero managed allocations. PhysicsServerJointTests.VerifyFrameReattachment runs
the same public assertions on CPU and the GPU prototype: rotated off-center
anchors, motor/limits, world replacement and rejected reconfiguration. It steps
each world 120 times at 1/120 s; anchor error must remain below one scene unit and
the motor angle within the configured limit plus 0.05 rad solver tolerance.
This is not independent GPU acceptance or a new throughput measurement.

The [public joint policies](../components/physics-joint-policies.md) now include bias, pin linear softness and common force/correction caps. Native joint copy/clear and GPU stage packets preserve the policy; spring preflight caps the combined impulse before validating all resulting velocities.
