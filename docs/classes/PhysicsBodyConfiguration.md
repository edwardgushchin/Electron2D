# PhysicsBodyConfiguration

Last updated: 2026-10-08

**Declaration:** `internal readonly record struct PhysicsBodyConfiguration`

**Source:** [PhysicsBodyConfiguration.cs](../../src/Servers/Physics/PhysicsBodyConfiguration.cs)
**Component:** [Scene physics bodies](../components/physics-bodies.md)

The immutable creation input carries engine values from scene bodies and raw
server colliders to [PhysicsColliderBackend](PhysicsColliderBackend.md). Position
and rotation are supplied separately at attachment. No vendor definition, body ID
or solver storage crosses this input boundary. The value is temporary; each owner
retains its existing authored properties and lifetime.

| Field | Meaning and constructor default |
| --- | --- |
| `Mode` | Required existing PhysicsServer.BodyMode: Static, Kinematic, Rigid or RigidLinear. |
| `LinearVelocity` | Global scene units/s; zero. |
| `AngularVelocity` | Radians/s; zero. |
| `GravityScale` | Unitless gravity multiplier; one. |
| `CanSleep`, `Sleeping` | Automatic sleep permission true, initial explicit sleeping false. |
| `LockRotation` | Explicit angular lock false; RigidLinear additionally locks rotation. |

Scene roles select their existing mode: static surfaces, kinematic platforms and
characters, dynamic rigid bodies, selected freeze mode or inactive physical bones.
Disabled MakeStatic participation replaces the effective mode and initial velocity
without discarding the owner's authored state. Rigid bodies supply custom
integration/gravity, velocity, sleep and rotation-lock settings. Sleep is restored
after fixture/mass initialization when required by the existing body lifecycle.

The current adapter alone builds the Box2D definition, converts linear units and
maps motion modes. `HasMotionMode` compares static/kinematic/dynamic response
independently of rotation locking; existing callers preserve lock restoration
when freeze/participation changes. Live simulation, query and joint state still
require further backend extraction under the [physics audit](../components/physics-contract-audit.md).

Verification uses the expanded `ELECTRON2D_TEST_COLLIDER_BACKEND=1` runner:
body/server state, initial mass/locks/sleep, disable/reentry, platform and CharacterBody
motion, both freeze modes and physical-bone transitions/serialization. The physical
bone storage test also loads the saved scene in a separate process. Existing warmed
allocation and numerical-response checks retain their original bounds.
