# PhysicsBody

Last updated: 2026-09-24

**Inherits:** [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject · **Inherited By:** [RigidBody](RigidBody.md), [StaticBody](StaticBody.md)

- **Source:** [PhysicsBody.cs](../../src/Scene/2D/PhysicsBody.cs)
- **Declaration:** `public abstract class PhysicsBody : CollisionObject`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

The shared scene-body role. It registers a backend body when entering a SceneTree and unregisters on exit or disposal. Direct [CollisionShape](CollisionShape.md) children supply fixtures; their resource, disabled state, local pose and collision-filter changes are applied before the next physics step. The body owns backend fixtures and never owns a borrowed Shape resource. Its public surface currently adds no separate methods to inherited CollisionObject and Entity; concrete dynamic/static behavior lives on RigidBody and StaticBody.

## API summary

| Member | Contract |
| --- | --- |
| `protected PhysicsBody()` | Creates a detached body with no fixtures. |
| `protected override void OnEnterTree()` / `OnExitTree()` | Attaches or detaches the backend body from this SceneTree's world. |
| `protected override void Dispose(bool disposing)` | Releases any remaining backend body and shape slots before inherited cleanup. |

## State and failures

The first profile accepts unit global scale and zero skew; a transformed active body that violates this fails its step before any shape replacement. Its translated/rotated pose can be changed by game code while attached and reaches the backend on the next fixed step. A failed step leaves the world available for a corrected later step. Attachment, scene callbacks and disposal use the scene owner thread. Re-entering the tree creates a fresh backend body from current typed state.

The inherited standalone `MoveAndCollide`, `TestMove`, gravity query, collision exceptions and input-pickable policy remain incomplete until typed sweep, area or picking integrations. See [PhysicsBody2D coverage](../coverage/classes/PhysicsBody2D.md), [component](../components/physics-bodies.md) and [ADR 0054](../decisions/physics.md#adr-0054).
