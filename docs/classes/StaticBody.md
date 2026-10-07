# StaticBody

Last updated: 2026-10-08

**Inherits:** [PhysicsBody](PhysicsBody.md), [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject · **Inherited By:** [AnimatableBody](AnimatableBody.md)

- **Source:** [StaticBody.cs](../../src/Scene/2D/StaticBody.cs)
- **Declaration:** `public class StaticBody : PhysicsBody`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

A stationary scene collider and base of [AnimatableBody](AnimatableBody.md), which moves kinematically. Add one or more direct [CollisionShape](CollisionShape.md) children with caller-owned circle, capsule, segment, convex polygon, concave segment collection or rectangle resources; its fixtures participate in the SceneTree's fixed physics world and constrain RigidBody movement. A child can admit contact only from its selected one-way side. Manual position and rotation changes reach the backend before the next step. The body itself has no visual geometry; a Sprite child or another CanvasItem may show it.

## Example

A direct [CollisionPolygon](CollisionPolygon.md) child can build a solid concave boundary or closed hollow terrain without a separately retained Shape resource.

```csharp
using var floorGeometry = new RectangleShape { Size = new Vector2(200, 20) };
var floor = new StaticBody { Position = new Vector2(0, 100) };
floor.AddChild(new CollisionShape { Shape = floorGeometry });
```

## API summary

| Member | Contract |
| --- | --- |
| `public StaticBody()` | Creates a detached, stationary body with default layer/mask one. |
| `public Vector2 ConstantLinearVelocity { get; set; }` | Global surface velocity in scene units/s; zero by default; no body translation. |
| `public float ConstantAngularVelocity { get; set; }` | Surface angular velocity in rad/s; zero by default; no body rotation. |
| `public PhysicsMaterial? PhysicsMaterialOverride { get; set; }` | Borrows a surface material for every child fixture; null uses friction one and bounce zero. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores surface velocity and the material override for PackedScene. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Restores this exact node type in PackedScene. |

The inherited [CollisionObject](CollisionObject.md) layer/mask properties and bit methods control which dynamic bodies collide with this fixture. The inherited [PhysicsBody](PhysicsBody.md) role registers and releases backend state on scene entry, exit and disposal. Surface velocity is independent of manually assigned body pose.

The inherited `GetGravity()` returns zero for this stationary body; area fields act on simulated dynamic bodies.

## Property description

<a id="physicsmaterialoverride"></a>
### `PhysicsMaterialOverride`

The optional borrowed [PhysicsMaterial](PhysicsMaterial.md) changes friction and bounce on all child collision shapes. Changes take effect before the next fixed step. Assigning a disposed resource throws without replacing the current override; disposing the borrowed resource resets the override to null. The caller owns the material. PackedScene retains its borrowed identity unless the resource requests a local scene duplicate.

## Verification and limits

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) verifies rectangle and circle contacts against a StaticBody floor, filter changes, live shape edits, borrowed resource lifetime and scene disposal. [PhysicsMaterialTests](../../tests/Electron2D.Tests/PhysicsMaterialTests.cs) verifies material mixing and ownership. [PhysicsSurfaceVelocityTests](../../tests/Electron2D.Tests/PhysicsSurfaceVelocityTests.cs) verifies the two stored velocity properties on CPU and the developing GPU backend. See [ADRs 0054 and 0060](../decisions/physics.md#adr-0060) for the unit and kinematic boundaries.

## Disabled processing and physics

Inherited `DisableMode.Remove` omits an effectively disabled static body from solver and queries. MakeStatic and KeepActive keep its normal static response; enabling never changes its configured role. RID, owner groups and borrowed resources survive removal and reentry. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).

## Stationary surface velocity

<a id="constantlinearvelocity"></a>
### `ConstantLinearVelocity`

`public Vector2 ConstantLinearVelocity { get; set; }` supplies finite global-axis
scene units per second (default zero).

<a id="constantangularvelocity"></a>
### `ConstantAngularVelocity`

`public float ConstantAngularVelocity { get; set; }` supplies finite radians per second (default zero). Positive angular speed
follows the scene rotation convention. These values affect normal, tangential,
rolling and restitution contact response without translating or rotating the body.
Point velocity is linear velocity plus angular velocity crossed with the offset
from the center of mass. Character platform carry and contact snapshots use this
same velocity channel.

For example, `floor.ConstantLinearVelocity = new Vector2(120, 0);` makes a belt
surface whose collision geometry remains fixed. Set `ConstantAngularVelocity = 2`
for a rotating surface velocity field. Both properties are stored in PackedScene;
borrowed shapes/materials and reentry retain their existing ownership rules.
Changes wake touching sleeping bodies. Attached reads/writes require the scene
owner; writes reject while the solver owns the world. Nonfinite assignments fail
before mutation, and disposed bodies reject access.

[AnimatableBody](AnimatableBody.md) inherits these properties: surface velocity
adds to its target-derived contact/query velocity, while the integrated pose still
reaches only the manual target. Scene direct-state velocity edits project back to
the surface properties. Raw server static bodies use their configured linear
velocity and direct-state angular velocity; detach/reattach preserves both.

The CPU scalar/SIMD and GPU contact solvers share endpoint velocity data.
`PhysicsSurfaceVelocityTests` covers belt/normal/angular response, captured point
velocity, character carry, kinematic additivity, numeric/thread/phase/disposal
rejection, raw server lifecycle/mode changes, packing and 64 warmed contact frames
with zero all-thread managed allocation on Linux/.NET 10/Vulkan. Native allocation,
other platforms and owner visual acceptance are unverified. [ADR 0075](../decisions/physics.md#adr-0075)
owns the stationary surface channel.
