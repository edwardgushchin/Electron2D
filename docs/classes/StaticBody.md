# StaticBody

Last updated: 2026-09-24

**Inherits:** [PhysicsBody](PhysicsBody.md), [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [StaticBody.cs](../../src/Scene/2D/StaticBody.cs)
- **Declaration:** `public sealed class StaticBody : PhysicsBody`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

A stationary scene collider. Add one or more direct [CollisionShape](CollisionShape.md) children with caller-owned circle or rectangle resources; its fixtures participate in the SceneTree's fixed physics world and constrain RigidBody movement. Manual position and rotation changes reach the backend before the next step. The body itself has no visual geometry; a Sprite child or another CanvasItem may show it.

## Example

```csharp
using var floorGeometry = new RectangleShape { Size = new Vector2(200, 20) };
var floor = new StaticBody { Position = new Vector2(0, 100) };
floor.AddChild(new CollisionShape { Shape = floorGeometry });
```

## API summary

| Member | Contract |
| --- | --- |
| `public StaticBody()` | Creates a detached, stationary body with default layer/mask one. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Restores this exact node type in PackedScene. |

The inherited [CollisionObject](CollisionObject.md) layer/mask properties and bit methods control which dynamic bodies collide with this fixture. The inherited [PhysicsBody](PhysicsBody.md) role registers and releases backend state on scene entry, exit and disposal. No empty public method is exposed for missing material/conveyor behavior.

## Verification and limits

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) verifies rectangle and circle contacts against a StaticBody floor, filter changes, live shape edits, borrowed resource lifetime and scene disposal. `ConstantLinearVelocity`, `ConstantAngularVelocity` and `PhysicsMaterialOverride` remain incomplete on [StaticBody2D coverage](../coverage/classes/StaticBody2D.md). See [ADR 0054](../decisions/physics.md#adr-0054) for the unit and backend boundary.
