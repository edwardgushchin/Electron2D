# PhysicsBody

Last updated: 2026-10-08

**Inherits:** [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject · **Inherited By:** [RigidBody](RigidBody.md), [StaticBody](StaticBody.md), [CharacterBody](CharacterBody.md)

- **Source:** [PhysicsBody.cs](../../src/Scene/2D/PhysicsBody.cs)
- **Declaration:** `public abstract class PhysicsBody : CollisionObject`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Physical skeletal integration

PhysicalBone overrides internal effective filters for inactive followers: prepared fixtures and motion-layer lookup use zero, while public CollisionLayer/CollisionMask retain configured values. Other body classes retain their ordinary filters.

Backend body and fixture ownership is retained by the shared [PhysicsColliderBackend](PhysicsColliderBackend.md), also used by Area and raw server colliders. Scene state, mass policy, view invalidation and pose publication retain their existing responsibilities.

Scene roles express initial motion through [PhysicsBodyConfiguration](PhysicsBodyConfiguration.md)
and the existing PhysicsServer.BodyMode. Effective disable/freeze choices and
physical-bone activity select that mode; the shared backend builds the vendor
definition and converts linear units.

Live pose, velocity, sleeping and target-motion operations also pass through that
adapter. CompleteBackend publishes the solved scene pose before sampling rigid
motion, preserving velocity edits made by synchronous transform notifications.
Resolved gravity/damping are shared with server bodies in PhysicsBodyRuntime;
scene authored settings and custom-integration policy remain unchanged.

## Description

The shared scene-body role. It registers a backend body when entering a SceneTree and unregisters on exit or disposal. Attached [Joint](Joint.md) constraints are released before the backend body is destroyed. Direct [CollisionShape](CollisionShape.md) children supply fixtures; their resource, disabled state, one-way side, local pose and collision-filter changes are applied before the next physics step or motion query. The body owns backend fixtures and never owns a borrowed Shape or PhysicsMaterial resource. Concrete RigidBody and StaticBody types expose their material override properties; edits rebuild these fixtures before stepping. `MoveAndCollide` and `TestMove` run kinematic sweeps over the registered space; explicit and active-joint RID exceptions suppress a body pair in both those sweeps and ordinary solver contacts. `GetGravity()` exposes the last resolved field for a dynamic body.

## API summary

A direct [CollisionPolygon](CollisionPolygon.md) child supplies owned solid or hollow fixtures through the same body lifetime, filtering and material path as a direct CollisionShape child. Its contour and mode can change while attached; the next fixed step rebuilds its fixtures.

The inherited [CollisionObject.GetRID](CollisionObject.md#getrid) remains stable when the backend body and its fixtures are recreated. [PhysicsDirectSpaceState](PhysicsDirectSpaceState.md) returns this RID and a direct child shape-owner index in typed ray and point results.

| Member | Contract |
| --- | --- |
| `protected PhysicsBody()` | Creates a detached body with no fixtures. |
| `public Vector2 GetGravity()` | Returns the last area/world gravity after body scaling; zero for detached or stationary bodies. |
| `public void AddCollisionExceptionWith(PhysicsBody body)` / `RemoveCollisionExceptionWith(PhysicsBody body)` | Add or remove a one-sided exception entry by another scene body's RID. |
| `protected override void ValidateDisposal()` | Preflight scene and dependent-joint world ownership/phases before disposal. |
| `public PhysicsBody?[] GetCollisionExceptions()` | Caller-owned deduplicated explicit/joint array; a server-only or freed RID has a null scene slot. |
| `public KinematicCollision? MoveAndCollide(Vector2 motion, bool testOnly = false, float safeMargin = 0.08f, bool recoveryAsCollision = false)` | Move to safe travel or test without moving; return a caller-owned contact or null. |
| `public bool TestMove(Transform from, Vector2 motion, KinematicCollision? collision = null, float safeMargin = 0.08f, bool recoveryAsCollision = false)` | Query from an arbitrary global pose without moving; optionally fill a caller-owned result. |
| `protected override void OnEnterTree()` / `OnExitTree()` | Attaches or detaches the backend body from this SceneTree's world. |
| `protected override void Dispose(bool disposing)` | Releases any remaining backend body and shape slots before inherited cleanup. |

## State and failures

The first profile accepts unit global scale and zero skew; a transformed active body that violates this fails its step before any shape replacement. Its translated/rotated pose can be changed by game code while attached and reaches the backend on the next fixed step. Explicit RigidBody force/impulse actions also synchronize pending pose and fixture edits before applying, so current mass and center of mass are available before the first frame. Solver pose sync writes one unit-scale global transform, avoiding scale drift during repeated rotation. A failed step leaves the world available for a corrected later step. Attachment, scene callbacks and disposal use the scene owner thread. Re-entering the tree creates a fresh backend body from current typed state.

<a id="getgravity"></a>
### `GetGravity()`

A dynamic [RigidBody](RigidBody.md) reports its most recently resolved [Area](Area.md) and world gravity vector after `GravityScale`. [CharacterBody](CharacterBody.md) reports the same resolved field without automatically applying it to its desired velocity. The value is zero before its first fixed step, when detached, and for [StaticBody](StaticBody.md). Area edits update it on the next nonzero physics step. An attached read requires the scene owner thread; a disposed body throws. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks rigid-body field reduction; [CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) checks world and Area gravity on a kinematic character.

<a id="motion"></a>
### `MoveAndCollide` and `TestMove`

Both methods use finite global scene-unit motion and a finite nonnegative recovery margin. They prepare pending body/shape/filter edits before scanning other bodies in the same physics space; Areas are sensors and do not block. Reciprocal layer/mask bits, the body's own RID and one-way pass-through direction are respected. Initial penetration is moved out before the sweep; `recoveryAsCollision=true` also reports that depenetration. Remaining overlap after recovery attempts stops motion at a zero safe fraction rather than allowing tunneling. Eight sweep refinements bracket the first new impact. Compound fixtures retain their direct owner indices. `TestMove` uses its supplied finite unit-scale global pose, leaves the body unchanged and fills `collision` on a completed hit or miss. A detached `TestMove` returns false; an attached `MoveAndCollide` is required and throws if no registered space exists. `MoveAndCollide(testOnly:true)` returns a collision without changing pose. On a regular call it applies travel, including recovery; a miss returns null. Callers own returned [KinematicCollision](KinematicCollision.md) objects.

[PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) covers scene and server colliders, contact owners/angle, test-only and actual travel, alternate starting pose, masks, disabled fixtures, one-way approach and margin, deep overlap, owner-thread rejection and 64 warmed unchanged `TestMove` calls with zero managed allocation on Linux/.NET 8. Input picking retains its own [coverage](../coverage/classes/PhysicsBody2D.md); the derived CharacterBody's separation-ray floor behavior remains a distinct [coverage gap](../coverage/classes/CharacterBody2D.md). Native allocation, other platforms and owner acceptance remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).

<a id="collisionexceptions"></a>
### Collision exceptions

`AddCollisionExceptionWith` and `RemoveCollisionExceptionWith` take a live PhysicsBody, reject null, and are idempotent. Each method changes only this body's exception list; a pair stops colliding when either body lists the other. `GetCollisionExceptions()` returns a new deduplicated array with explicit entries in insertion order followed by active joint targets. Entries introduced through the server API may contain a server-only or later freed RID; their scene array slot is null until the RID is explicitly removed. An exception survives fixture rebuild and tree detachment while this body remains live. Changing the list while attached prepares fresh fixture contacts before the next physics step or motion query, so an already touching pair responds to the new rule. Areas continue to sense normally. Attached calls and reads require the scene owner thread. [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) covers unilateral lists, duplicates, scene/server RIDs, live contact changes, motion queries, lifecycle and warmed managed allocation.

The [CollisionObject owner registry](CollisionObject.md#createshapeowner) now supplies logical shape slots for both child and manual groups. Query/contact indices identify global slots, while ShapeFindOwner returns the distinct group ID; removal shifts later indices. Motion owner accessors resolve weak configured objects as well as child nodes. See [ADR 0071](../decisions/physics.md#adr-0071).

## Disabled processing and physics

Inherited `DisableMode` applies to every concrete body. Remove detaches native fixtures and invalidates live body views while retaining RID and owner state. MakeStatic temporarily changes the native type, preserving configured mass and role; enabling restores dynamic, kinematic or static behavior. [CollisionDisableModeTests](../../tests/Electron2D.Tests/CollisionDisableModeTests.cs) verifies queries, contacts, restoration and lifecycle. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).

## Dependent joint lifetime

`protected override void ValidateDisposal()` checks the scene phase and every active world referenced by a dependent server joint before beginning disposal. Rejected off-owner or in-step disposal leaves the body alive. This applies even after the body is detached while the other endpoint remains attached. Body departure suspends caller-owned connections before native destruction; final disposal clears their configured role. `GetCollisionExceptions()` includes deduplicated explicit and active collision-disabled joint targets, returning null for a server-only peer. Removing an explicit exception never removes a joint contribution. [PhysicsServerJointTests](../../tests/Electron2D.Tests/PhysicsServerJointTests.cs) covers these cases under [ADR 0087](../decisions/physics-joints.md#adr-0087).

[Indexed PhysicsServer geometry](PhysicsServer.md#shape-slots) now shares the scene/server logical slots, effective local poses and native fixture/query path under [ADR 0088](../decisions/physics-shape-slots.md#adr-0088). Raw replacement/pose/disabled/one-way edits do not rewrite child configuration; group/child edits reclaim corresponding overrides. Shape free/replacement follows shared RID/view ownership and related-world phase guards. Effective poses also feed mass geometry. [PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) verifies real geometry, body/Area lifetime and one-way contacts/motion on Linux/.NET 10.

PhysicsServer's typed state methods accept this body's RID and preserve its scene
role. Attachment restores non-rigid contact velocity/sleep policy and explicitly
requested dynamic sleep even when automatic sleep is disabled. See [typed state](PhysicsServer.md#body-state).

After an internally enabled GPU backend fails, body/area/joint teardown preserves
owner/stepping guards but skips individual raw graph destruction and partial-motion
capture. Managed bindings/views are released; the failed space reclaims raw storage
in bulk. Queries and further simulation remain rejected. See the
[GPU island graph failure contract](../components/gpu-physics.md#gpu-contact-driven-island-graph-2026-10-08).
