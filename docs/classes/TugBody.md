# TugBody

Last updated: 2026-10-06

**Namespace:** `Electron2D.Examples.PhysicsSandbox`. **Declaration:** `internal sealed class TugBody : RigidBody`. **Inherits:** [RigidBody](RigidBody.md). **Source:** [PhysicsScene.Stories.cs](../../examples/PhysicsSandbox/PhysicsScene.Stories.cs). **Component:** [Physics sandbox](../components/physics-sandbox.md).

The orbital story's player ship. It enables `CustomIntegrator`, retains native motion/contact response and updates velocities from the post-solver `IntegrateForces(PhysicsDirectBodyState)` hook. W/S or up/down applies forward/reverse acceleration along the current heading; A/D or left/right applies turn acceleration. Linear and angular damping use the actual supplied step duration.

## Internal consumer surface

| Member | Contract |
| --- | --- |
| `TugBody()` | Creates a two-unit-mass custom-integrated ship with explicit inertia |
| `float Thrust` | Story-controlled acceleration, initially 230 |
| `bool Firing` | Draws the current thrust plume |
| `IntegrateForces(PhysicsDirectBodyState state)` | Protected live post-solver velocity update |

The tow cable is an ordinary DampedSpringJoint with scene/server RID identity. Custom integration intentionally omits ordinary accumulated force integration; mouse grabbing uses an impulse for this body. The ship owns no native handle or resource independently of its normal RigidBody attachment. The parent PhysicsScene owns its shape, dock, cargo and cable. This internal gameplay specialization adds no runtime API, serializer schema or scripting model.
