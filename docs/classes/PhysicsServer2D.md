# PhysicsServer2D

Last updated: 2026-09-26

**Inherits:** ElectronObject · **Source:** [PhysicsServer2D.cs](../../src/Servers/Physics/PhysicsServer2D.cs), [PhysicsServer2D.Resources.cs](../../src/Servers/Physics/PhysicsServer2D.Resources.cs)

## Description

The process-wide registry for typed 2D physics RIDs. It registers each SceneTree's existing Box2D world and its scene CollisionObject identities, and can also create explicit spaces, bodies, Areas and the six implemented shape families. A server-created collider can join either kind of space; [World2D](World2D.md) and `SpaceGetDirectState` query that same solver state. RID values never expose Box2D IDs and never resolve to a later object after free. The server singleton cannot be disposed by consumers.

## Example

```csharp
var server = PhysicsServer2D.Instance;
RID space = server.SpaceCreate();
RID body = server.BodyCreate();
RID shape = server.CircleShapeCreate();
using var circle = new CircleShape { Radius = 12 };
server.ShapeSetData(shape, circle);
server.BodySetMode(body, PhysicsServer2D.BodyMode.Static);
server.BodyAddShape(body, shape);
server.BodySetSpace(body, space);
using var ray = PhysicsRayQueryParameters2D.Create(new(0, -40), new(0, 40));
var hit = server.SpaceGetDirectState(space).IntersectRay(ray);
server.FreeRID(body);
server.FreeRID(shape);
server.FreeRID(space);
```

## API summary

| Member | Contract |
| --- | --- |
| `public static PhysicsServer2D Instance { get; }` | Shared process server. |
| `public enum BodyMode` | [Static, Kinematic, Rigid, RigidLinear](PhysicsServer2D.BodyMode.md). |
| `public RID SpaceCreate()` | Caller-owned independent physics space. |
| `public void SpaceStep(RID space, double delta)` | Advance only an explicitly created space; zero delta is inert. |
| `public PhysicsDirectSpaceState2D SpaceGetDirectState(RID space)` | Cached query view of any live server/scene space. |
| `public bool BodyTestMotion(RID body, PhysicsTestMotionParameters2D parameters, PhysicsTestMotionResult2D? result = null)` | Test a scene or server body against its current space without moving it; optionally fill typed output. |
| `public void BodyAddCollisionException(RID body, RID exceptedBody)` / `BodyRemoveCollisionException(RID body, RID exceptedBody)` | Change a one-sided body-owned RID exception affecting both solver contacts and motion tests. |
| `public RID BodyCreate()` / `AreaCreate()` | Detached rigid body or sensor Area with default layer/mask one. |
| `public RID CircleShapeCreate()` / `RectangleShapeCreate()` | Default concrete geometry RIDs. |
| `public RID CapsuleShapeCreate()` / `SegmentShapeCreate()` | Default concrete geometry RIDs. |
| `public RID ConvexPolygonShapeCreate()` / `ConcavePolygonShapeCreate()` | Empty polygon geometry RIDs. |
| `public void ShapeSetData(RID shape, Shape data)` | Copy same-kind caller geometry into a server shape and rebuild users. |
| `public Shape ShapeGetData(RID shape)` | Caller-owned independent geometry copy. |
| `public void BodyAddShape(RID body, RID shape, Transform? transform = null, bool disabled = false)` | Add one indexed body shape slot; null transform is identity. |
| `public void AreaAddShape(RID area, RID shape, Transform? transform = null, bool disabled = false)` | Add one indexed sensor shape slot. |
| `public int BodyGetShapeCount(RID body)` / `AreaGetShapeCount(RID area)` | Count slots, including disabled ones. |
| `public void BodySetShapeDisabled(RID body, int index, bool disabled)` / `AreaSetShapeDisabled(RID area, int index, bool disabled)` | Rebuild one indexed slot's fixtures. |
| `public void BodyRemoveShape(RID body, int index)` / `AreaRemoveShape(RID area, int index)` | Remove one slot and its fixtures. |
| `public void BodySetSpace(RID body, RID space)` / `AreaSetSpace(RID area, RID space)` | Attach to a live space; empty RID detaches. |
| `public RID BodyGetSpace(RID body)` / `AreaGetSpace(RID area)` | Current space RID, or empty while detached. |
| `public void BodySetTransform(RID body, Transform transform)` / `AreaSetTransform(RID area, Transform transform)` | Set finite unit-scale, zero-skew pose. |
| `public Transform BodyGetTransform(RID body)` | Current solver pose, including dynamic movement. |
| `public void BodySetLinearVelocity(RID body, Vector2 velocity)` | Finite scene units per second. |
| `public void BodySetMode(RID body, BodyMode mode)` / `BodyMode BodyGetMode(RID body)` | Change/read the solver motion mode. |
| `public void BodySetCollisionLayer(RID body, uint layer)` / `BodySetCollisionMask(RID body, uint mask)` | Rebuild body fixtures with 32-bit filters. |
| `public void AreaSetCollisionLayer(RID area, uint layer)` | Rebuild Area sensor fixtures with 32-bit queryable layers. |
| `public void FreeRID(RID rid)` | Free a caller-owned space, body, Area or shape. |
| `protected override void ValidateDisposal()` | Reject consumer disposal of the singleton. |

## Method descriptions

<a id="spaces"></a>
### Space creation, access and stepping

`SpaceCreate` allocates one real Box2D world with the current sampled 2D project gravity default. `SpaceGetDirectState` returns a live view of any registered explicit or SceneTree space; disposing a view allows the next lookup to create another. `SpaceStep` accepts a finite nonnegative delta only for explicit spaces, so it cannot double-step a scene world. The SceneTree advances its own registered space in its fixed physics lane. Space mutation and querying require the creating/owning thread and reject a world currently stepping. Explicit spaces with server-created bodies advance the real solver, not a parallel query-only representation. Scene Area and RigidBody damping reduction is separate and does not yet apply to server-only bodies.

<a id="colliders"></a>
### Collider creation, shapes and ownership

`BodyCreate` defaults to rigid mode; `AreaCreate` defaults to a stationary nonresponding sensor. Neither has a space until `BodySetSpace` or `AreaSetSpace`. The six shape constructors correspond to the already implemented circle, rectangle, capsule, segment, convex and concave resource families. `ShapeSetData` rejects a disposed or different-kind Shape, duplicates caller data and rebuilds attached users; `ShapeGetData` returns another independent duplicate. Each `BodyAddShape` or `AreaAddShape` appends an indexed slot with a finite unit-scale, zero-skew local transform, default identity, and optional disabled state. Slot-count, disabled-toggle and removal methods make those indices executable after attachment; invalid indices throw before mutation. Compound fixtures from one slot share its public query ShapeIndex. An explicit collider may be moved into the SceneTree's `World2D.Space`; direct queries then see it beside scene nodes. Queries return its RID with a null scene Collider.

<a id="body-state"></a>
### Body state and filters

`BodySetMode` supports all four numeric mode values and rejects undefined input. Static, kinematic and rigid bodies share the Box2D world; RigidLinear locks rotation and clears angular velocity. Switching to Static or Kinematic clears linear and angular velocity while retaining the solved pose. The typed `BodySetTransform`, `BodySetLinearVelocity` and `BodyGetTransform` methods cover the corresponding transform/linear-velocity branches of the dynamic reference state API. A moving body's solved pose and velocity are captured before space detachment, preserving state when reattached. Body layer/mask and Area layer setters accept all 32 bits; direct queries match the layer independently of the collider's mask. Server-only Area mask writes remain [Blocked](../coverage/classes/PhysicsServer2D.md) until Area overlap monitoring or fields consume them. Other body states, forces, parameters, callbacks and Area field parameters retain separate coverage gaps.

`BodyTestMotion` prepares pending scene and server fixtures, then tests the supplied body's own shapes from a typed global pose. Reciprocal body filters, RID and managed-instance exclusions, one-way surfaces, recovery margin and initial overlap are applied. It returns false on a miss and updates an optional [PhysicsTestMotionResult2D](PhysicsTestMotionResult2D.md) with full travel and cleared contact fields. On a hit it reports contact identity, point, normal, depth, velocity, local/collider shape-owner indices and safe/unsafe fractions. It never changes the actual body pose. A detached body or wrong RID rejects; off-owner and in-step calls reject. [PhysicsTestMotionParameters2D](PhysicsTestMotionParameters2D.md) names the input. The separation-ray option remains an exact [coverage gap](../coverage/classes/PhysicsServer2D.md) until that shape family exists.

`BodyAddCollisionException` and `BodyRemoveCollisionException` edit only the owner's RID list. Either body's entry suppresses the pair in fixed-step solver contacts and `BodyTestMotion`, independent of reciprocal collision masks; Area monitoring is unaffected. Duplicates and absent removals do nothing. The owner must be a live scene or server body; an arbitrary excepted RID, including an empty or later freed one, is retained but cannot match a live pair. An attached owner requires its space thread and cannot change exceptions while stepping. A list change marks that owner's fixtures for rebuilding before the next query or step, including when the pair is already touching. Freeing an owner removes its own entries; other bodies can retain its RID as an inert exception until explicitly removed.

<a id="free"></a>
### `FreeRID`

Frees only caller-owned server resources. A shape free removes its slots from live users; a body/Area free detaches its backend object; a space free detaches its server colliders and invalidates retained query views. Collider RIDs remain live and detached after their space is freed, so they can be assigned to another space. SceneTree spaces and scene CollisionObject RIDs must be released by their owning objects; attempting to free them here throws. Stale or wrong-kind RIDs reject without resolving to a later resource. The `RID` value itself remains nonzero after free.

## Verification and limits

[PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks scene/server shared world, explicit stepping, six shape families, filters, modes, cross-space moves and RID lifecycle. [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks direct shape operations; [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks body motion; [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) checks unilateral scene/server lists, live solver and motion filtering, owner/target lifetime and warmed allocation. Remaining server methods, world-boundary/separation-ray/custom shapes, joints and direct body state remain separate slices. Scene Area field/event delivery does not yet represent server-only colliders in typed object-level events. Query paths scan fixtures linearly; native allocator accounting, other platforms and large-world throughput remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).
