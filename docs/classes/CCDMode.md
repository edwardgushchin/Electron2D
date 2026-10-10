# CCDMode

Last updated: 2026-10-10

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
