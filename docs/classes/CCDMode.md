# CCDMode

Last updated: 2026-10-13

**Declaration:** `public enum CCDMode`
**Source:** [CCDMode.cs](../../src/Servers/Physics/CCDMode.cs)
**Component:** [Continuous collision](../components/cpu-continuous-collision.md)

## Description

One shared mode contract for RigidBody.ContinuousCD, PhysicsServer body accessors
on explicitly selected CPU and GPU worlds. The mode is stored while detached or not
dynamic. A changed mode wakes an attached dynamic body. It does not change a
neighbour's configured policy. Detection and ordinary impulse response remain
separate phases of the same physics step; numerical paths differ between backends.

| Value | Number | Meaning |
| --- | --- | --- |
| `Disabled` | 0 | Default discrete collision detection. No continuous trajectory check is requested by this body. |
| `CastRay` | 1 | Sweep a leading support point; off-ray features and the body's own rotational arc may be missed. |
| `CastShape` | 2 | Sweep the complete supported shape, including rotational motion. |

## Example

```csharp
var projectile = new RigidBody { ContinuousCD = CCDMode.CastShape };
PhysicsServer.BodySetContinuousCollisionDetectionMode(projectile.GetRID(), CCDMode.CastRay);
```

Undefined values reject before mutation. Configuration, packing, scene/server
projection, geometry, force budgets and warmed allocation are covered by
PhysicsCCDTests. See the component page for supported geometry and verification
limits. The common CCD, boundary and directed-ray group runs with
`ELECTRON2D_TEST_GPU_CCD_CONTRACT=1`; the older stage-hosted diagnostic is separate.

The ordinary full runner executes the complete PhysicsCCDTests on CPU and GPU;
`ELECTRON2D_TEST_CONTINUOUS_PHYSICS=cpu|gpu` selects the same cases. Scene fixtures
assert the actual backend. After 64 warmup steps, 128 reset-active steps require
0/0 owner/all-thread managed bytes. Existing numerical and error bounds remain;
native allocations, foreign platforms and final performance acceptance stay open.


[Independent numerical CCD](../components/physics-registered-adapter.md#independent-numerical-ccd)
now verifies actual registered/CPU/GPU ray/shape motion, relative impact, remaining
time, full impulse and force/callback/tick budgets through common public checks.
128 warmed complete replay cycles require 0/0 owner/all-thread bytes. Registered
explicit-space controls check custom hollow geometry and terminal numerical
budget failure. This bounded fixture does not establish general geometry/rotation,
full extension, network or native/performance acceptance.

The continuous-contract entry now accepts separate numerical execution without
being redirected to the general CPU suite. Both CPU/GPU profiles pass the same
CCD, infinite-boundary and directed-ray cases on software Vulkan, with explicit
0/0 owner/all-thread warm allocation gates. See [current software conformance](../components/cpu-continuous-collision.md#software-vulkan-continuous-conformance-2026-10-14).
Hardware performance, native allocation and foreign-platform acceptance remain open.
