# PhysicsBody

Last updated: 2026-09-24

**Inherits:** [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject · **Inherited By:** [RigidBody](RigidBody.md), [StaticBody](StaticBody.md)

- **Source:** [PhysicsBody.cs](../../src/Scene/2D/PhysicsBody.cs)
- **Declaration:** `public abstract class PhysicsBody : CollisionObject`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

The shared scene-body role. It registers a backend body when entering a SceneTree and unregisters on exit or disposal. Direct [CollisionShape](CollisionShape.md) children supply fixtures; their resource, disabled state, one-way side, local pose and collision-filter changes are applied before the next physics step. The body owns backend fixtures and never owns a borrowed Shape or PhysicsMaterial resource. Concrete RigidBody and StaticBody types expose their material override properties; edits rebuild these fixtures before stepping. `GetGravity()` exposes the last resolved field for a dynamic body.

## API summary

| Member | Contract |
| --- | --- |
| `protected PhysicsBody()` | Creates a detached body with no fixtures. |
| `public Vector2 GetGravity()` | Returns the last area/world gravity after body scaling; zero for detached or stationary bodies. |
| `protected override void OnEnterTree()` / `OnExitTree()` | Attaches or detaches the backend body from this SceneTree's world. |
| `protected override void Dispose(bool disposing)` | Releases any remaining backend body and shape slots before inherited cleanup. |

## State and failures

The first profile accepts unit global scale and zero skew; a transformed active body that violates this fails its step before any shape replacement. Its translated/rotated pose can be changed by game code while attached and reaches the backend on the next fixed step. Explicit RigidBody force/impulse actions also synchronize pending pose and fixture edits before applying, so current mass and center of mass are available before the first frame. Solver pose sync writes one unit-scale global transform, avoiding scale drift during repeated rotation. A failed step leaves the world available for a corrected later step. Attachment, scene callbacks and disposal use the scene owner thread. Re-entering the tree creates a fresh backend body from current typed state.

<a id="getgravity"></a>
### `GetGravity()`

A dynamic [RigidBody](RigidBody.md) reports its most recently resolved [Area](Area.md) and world gravity vector after `GravityScale`. The value is zero before its first fixed step, when detached, and for [StaticBody](StaticBody.md). Area edits update it on the next nonzero physics step. An attached read requires the scene owner thread; a disposed body throws. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks priority, point falloff, body scaling and restoration of world gravity. The inherited query is Partial until the absent `CharacterBody` executes the same field contract.

The inherited standalone `MoveAndCollide`, `TestMove`, collision exceptions and input-pickable policy remain incomplete until typed sweep or picking integrations. See [PhysicsBody2D coverage](../coverage/classes/PhysicsBody2D.md), [component](../components/physics-bodies.md) and [ADR 0056](../decisions/physics.md#adr-0056).
