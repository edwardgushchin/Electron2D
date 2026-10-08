# PhysicsColliderBackend

Last updated: 2026-10-08

**Declaration:** `internal sealed partial class PhysicsColliderBackend`

**Source:** [PhysicsColliderBackend.cs](../../src/Servers/Physics/PhysicsColliderBackend.cs),
[live state](../../src/Servers/Physics/PhysicsColliderBackend.State.cs)
**Component:** [Scene physics bodies](../components/physics-bodies.md)

## Responsibility and ownership

One retained component owns the current backend body, world attachment and shape
ID collection for a scene CollisionObject or a server-created collider. PhysicsBody,
Area and PhysicsServerCollider share it. The current implementation uses Box2D;
this consolidates ownership without implementing a second backend or exposing
vendor handles to applications.

The component keeps the stable public collider RID and a weak scene owner. Raw
server colliders have no scene owner. Logical shape indices come from the owning
slot registry; compound backend pieces retain that one index. Caller-owned Shape
resources are borrowed during rebuild and are never disposed here.

## Operations and invariants

| Operation | Contract |
| --- | --- |
| `Attach` | Accept scene-unit pose and PhysicsBodyConfiguration; convert units/build the vendor definition internally, create the body and commit space ownership. Reject a second attachment. |
| `HasMotionMode`, `SetMotionMode` | Compare/change the existing engine body mode through the backend; motion matching is independent of angular locking. |
| `GetPose`, `SetPose`, `SetTargetPose` | Transfer engine-unit position/rotation and derive kinematic target motion; cached backend references live only for the current attachment. |
| `GetSolverMotion`, velocity/sleep accessors | Return raw integrated velocity for scene publication or combined contact velocity for server state, retaining virtual surface semantics. |
| `SavePose`, `RestorePose` | Keep a private exact backend pose for frozen kinematic query restoration; avoid decoded-angle round trips while idle. |
| Velocity, gravity, sleep and rotation setters; force/torque application | Convert units in one adapter while the scene/server caller retains validation and role policy. |
| `Detach` | Destroy the body and its fixtures, clear IDs and space, retain list capacity. If the world has failed, skip backend calls and leave final backend cleanup to world disposal. |
| `RebuildShapes` | Validate all active slot transforms before removing existing fixtures; preserve disabled/disposed-slot indexing and append the current active geometry. |
| `Shapes`, `BodyID`, `Space` | Internal borrowed backend state; shape IDs can change on rebuild, body IDs on reattachment. Public RID identity is independent. |

Scene and server slot overloads use one shared shape-definition/append path. It
installs category/mask bits, sensor/density policy, surface material, one-way
metadata and exception pre-solve eligibility. Shape resources supply their existing
concrete geometry. Definitions are temporary locals; the component does not retain
a large per-collider shape-definition snapshot. Mass updates are deferred until
the owning body applies its complete profile; Areas remain massless sensors.

Callers preserve their owner-thread/phase guards, body-view invalidation, joint
release, Area reset, solved-pose capture and authored dirty/revision state. These
operations occur around attachment/destruction in their existing order. The
component does not introduce callbacks, asynchronous work or a second world copy.
Geometry, query, joint and runtime methods still have other backend-specific
dependencies recorded by the [physics audit](../components/physics-contract-audit.md).

## Verification

`ELECTRON2D_TEST_COLLIDER_BACKEND=1` runs the existing body/Area, shape-family,
polygon/one-way/owner, material/mass, server-slot, monitoring/fields, disable-mode,
body-view, joint, World, platform/character/freeze and physical-bone tests together. They cover real solver/query results,
identity, reentry, malformed-transform recovery, borrowed lifetime and their
existing warmed allocation checks. PhysicsBodyStateTests also verifies velocity
writes made by synchronous solved-pose notifications: motion is sampled after
publishing the pose, so those writes survive in both scene and direct state.
PhysicsParallelTests checks publication for 288 bodies over 256 fixed steps and
serial/parallel transitions. The GPU suite exercises the current stage
host and failed-step/world teardown. Neither suite establishes a standalone GPU
implementation, network replay, foreign-platform acceptance or a throughput gain.
