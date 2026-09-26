# StaticBody

Last updated: 2026-09-26

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
| `public PhysicsMaterial? PhysicsMaterialOverride { get; set; }` | Borrows a surface material for every child fixture; null uses friction one and bounce zero. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores the material override for PackedScene. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Restores this exact node type in PackedScene. |

The inherited [CollisionObject](CollisionObject.md) layer/mask properties and bit methods control which dynamic bodies collide with this fixture. The inherited [PhysicsBody](PhysicsBody.md) role registers and releases backend state on scene entry, exit and disposal. No empty public method is exposed for missing material/conveyor behavior.

The inherited `GetGravity()` returns zero for this stationary body; area fields act on simulated dynamic bodies.

## Property description

<a id="physicsmaterialoverride"></a>
### `PhysicsMaterialOverride`

The optional borrowed [PhysicsMaterial](PhysicsMaterial.md) changes friction and bounce on all child collision shapes. Changes take effect before the next fixed step. Assigning a disposed resource throws without replacing the current override; disposing the borrowed resource resets the override to null. The caller owns the material. PackedScene retains its borrowed identity unless the resource requests a local scene duplicate.

## Verification and limits

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) verifies rectangle and circle contacts against a StaticBody floor, filter changes, live shape edits, borrowed resource lifetime and scene disposal. [PhysicsMaterialTests](../../tests/Electron2D.Tests/PhysicsMaterialTests.cs) verifies material mixing and ownership. `ConstantLinearVelocity` and `ConstantAngularVelocity` remain incomplete on [StaticBody2D coverage](../coverage/classes/StaticBody2D.md). See [ADRs 0054 and 0060](../decisions/physics.md#adr-0060) for the unit and kinematic boundaries.

## Disabled processing and physics

Inherited `DisableMode.Remove` omits an effectively disabled static body from solver and queries. MakeStatic and KeepActive keep its normal static response; enabling never changes its configured role. RID, owner groups and borrowed resources survive removal and reentry. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).

## Stationary surface velocity prerequisite

Constant linear/angular surface velocity remains unimplemented. Its [Blocked coverage rows](../coverage/classes/StaticBody2D.md) require a normal and tangential point-velocity channel in contact constraints while keeping body pose stationary. Native static contacts read a zero dummy solver state; a scalar tangent-speed material offset alone is insufficient. [ADR 0075](../decisions/physics.md#adr-0075) records this exact integration trigger.
