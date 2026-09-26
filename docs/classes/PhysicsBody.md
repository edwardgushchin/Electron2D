# PhysicsBody

Last updated: 2026-09-26

**Inherits:** [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject · **Inherited By:** [RigidBody](RigidBody.md), [StaticBody](StaticBody.md)

- **Source:** [PhysicsBody.cs](../../src/Scene/2D/PhysicsBody.cs)
- **Declaration:** `public abstract class PhysicsBody : CollisionObject`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

The shared scene-body role. It registers a backend body when entering a SceneTree and unregisters on exit or disposal. Direct [CollisionShape](CollisionShape.md) children supply fixtures; their resource, disabled state, one-way side, local pose and collision-filter changes are applied before the next physics step or motion query. The body owns backend fixtures and never owns a borrowed Shape or PhysicsMaterial resource. Concrete RigidBody and StaticBody types expose their material override properties; edits rebuild these fixtures before stepping. `MoveAndCollide` and `TestMove` run kinematic sweeps over the registered space; body-owned RID exceptions suppress a body pair in both those sweeps and ordinary solver contacts. `GetGravity()` exposes the last resolved field for a dynamic body.

## API summary

A direct [CollisionPolygon](CollisionPolygon.md) child supplies owned solid or hollow fixtures through the same body lifetime, filtering and material path as a direct CollisionShape child. Its contour and mode can change while attached; the next fixed step rebuilds its fixtures.

The inherited [CollisionObject.GetRID](CollisionObject.md#getrid) remains stable when the backend body and its fixtures are recreated. [PhysicsDirectSpaceState](PhysicsDirectSpaceState.md) returns this RID and a direct child shape-owner index in typed ray and point results.

| Member | Contract |
| --- | --- |
| `protected PhysicsBody()` | Creates a detached body with no fixtures. |
| `public Vector2 GetGravity()` | Returns the last area/world gravity after body scaling; zero for detached or stationary bodies. |
| `public void AddCollisionExceptionWith(PhysicsBody body)` / `RemoveCollisionExceptionWith(PhysicsBody body)` | Add or remove a one-sided exception entry by another scene body's RID. |
| `public PhysicsBody?[] GetCollisionExceptions()` | Caller-owned insertion-order array; a server-only or freed RID has a null scene slot. |
| `public KinematicCollision2D? MoveAndCollide(Vector2 motion, bool testOnly = false, float safeMargin = 0.08f, bool recoveryAsCollision = false)` | Move to safe travel or test without moving; return a caller-owned contact or null. |
| `public bool TestMove(Transform from, Vector2 motion, KinematicCollision2D? collision = null, float safeMargin = 0.08f, bool recoveryAsCollision = false)` | Query from an arbitrary global pose without moving; optionally fill a caller-owned result. |
| `protected override void OnEnterTree()` / `OnExitTree()` | Attaches or detaches the backend body from this SceneTree's world. |
| `protected override void Dispose(bool disposing)` | Releases any remaining backend body and shape slots before inherited cleanup. |

## State and failures

The first profile accepts unit global scale and zero skew; a transformed active body that violates this fails its step before any shape replacement. Its translated/rotated pose can be changed by game code while attached and reaches the backend on the next fixed step. Explicit RigidBody force/impulse actions also synchronize pending pose and fixture edits before applying, so current mass and center of mass are available before the first frame. Solver pose sync writes one unit-scale global transform, avoiding scale drift during repeated rotation. A failed step leaves the world available for a corrected later step. Attachment, scene callbacks and disposal use the scene owner thread. Re-entering the tree creates a fresh backend body from current typed state.

<a id="getgravity"></a>
### `GetGravity()`

A dynamic [RigidBody](RigidBody.md) reports its most recently resolved [Area](Area.md) and world gravity vector after `GravityScale`. The value is zero before its first fixed step, when detached, and for [StaticBody](StaticBody.md). Area edits update it on the next nonzero physics step. An attached read requires the scene owner thread; a disposed body throws. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks priority, point falloff, body scaling and restoration of world gravity. The inherited query is Partial until the absent `CharacterBody` executes the same field contract.

<a id="motion"></a>
### `MoveAndCollide` and `TestMove`

Both methods use finite global scene-unit motion and a finite nonnegative recovery margin. They prepare pending body/shape/filter edits before scanning other bodies in the same physics space; Areas are sensors and do not block. Reciprocal layer/mask bits, the body's own RID and one-way pass-through direction are respected. Initial penetration is moved out before the sweep; `recoveryAsCollision=true` also reports that depenetration. Remaining overlap after recovery attempts stops motion at a zero safe fraction rather than allowing tunneling. Eight sweep refinements bracket the first new impact. Compound fixtures retain their direct owner indices. `TestMove` uses its supplied finite unit-scale global pose, leaves the body unchanged and fills `collision` on a completed hit or miss. A detached `TestMove` returns false; an attached `MoveAndCollide` is required and throws if no registered space exists. `MoveAndCollide(testOnly:true)` returns a collision without changing pose. On a regular call it applies travel, including recovery; a miss returns null. Callers own returned [KinematicCollision2D](KinematicCollision2D.md) objects.

[PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) covers scene and server colliders, contact owners/angle, test-only and actual travel, alternate starting pose, masks, disabled fixtures, one-way approach and margin, deep overlap, owner-thread rejection and 64 warmed unchanged `TestMove` calls with zero managed allocation on Linux/.NET 8. Input picking and CharacterBody sliding retain their own [coverage](../coverage/classes/PhysicsBody2D.md); native allocation, other platforms and owner acceptance remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).

<a id="collisionexceptions"></a>
### Collision exceptions

`AddCollisionExceptionWith` and `RemoveCollisionExceptionWith` take a live PhysicsBody, reject null, and are idempotent. Each method changes only this body's exception list; a pair stops colliding when either body lists the other. `GetCollisionExceptions()` returns a new array in insertion order. Entries introduced through the server API may contain a server-only or later freed RID; their scene array slot is null until the RID is explicitly removed. An exception survives fixture rebuild and tree detachment while this body remains live. Changing the list while attached prepares fresh fixture contacts before the next physics step or motion query, so an already touching pair responds to the new rule. Areas continue to sense normally. Attached calls and reads require the scene owner thread. [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) covers unilateral lists, duplicates, scene/server RIDs, live contact changes, motion queries, lifecycle and warmed managed allocation.
