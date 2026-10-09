# ShapeCast

Last updated: 2026-10-05

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [ShapeCast.cs](../../src/Scene/2D/ShapeCast.cs)
- **Declaration:** `public sealed class ShapeCast : Entity`
- **Component:** [Physics server and direct queries](../components/physics-queries.md)

## Description

A spatial shape query from this node's global pose along `TargetPosition` in local coordinates. An enabled ShapeCast samples in its internal fixed physics lane. `ForceShapecastUpdate()` samples immediately, even while disabled or before the first physics frame. It caches the safe/unsafe motion fractions and contacts at the earliest new impact, or at its current pose for a zero target or initial overlap. Each contact represents a different collider RID; server-only colliders have no scene object. The caller owns `Shape`, which is borrowed for querying. The node shares its SceneTree's [World](World.md) and owns no solver world.

## Example

Partial snippet with an attached `player` body and a live `probe` resource:

```csharp
var cast = new ShapeCast { Shape = probe, TargetPosition = new Vector2(0, 100) };
player.AddChild(cast);
cast.ForceShapecastUpdate();
if (cast.IsColliding()) Console.WriteLine(cast.GetCollisionNormal(0));
```

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public ShapeCast()` | — | Detached enabled cast with no geometry or result. |
| `public Shape? Shape { get; set; }` | null | Caller-owned live query resource; null prevents sampling. |
| `public Vector2 TargetPosition { get; set; }` | (0, 50) | Finite local target; zero checks current overlap. |
| `public float Margin { get; set; }` | 0 | Nonnegative finite expansion in scene units. |
| `public int MaxResults { get; set; }` | 32 | Maximum distinct collider RIDs; nonpositive values return no contacts. |
| `public uint CollisionMask { get; set; }` | 1 | Accepted 32-bit collider layers. |
| `public bool Enabled { get; set; }` | true | Automatic fixed-frame sampling; false clears only collision flag. |
| `public bool ExcludeParent { get; set; }` | true | Add the direct CollisionObject parent's RID on entry. |
| `public bool CollideWithAreas { get; set; }` | false | Include Area sensors. |
| `public bool CollideWithBodies { get; set; }` | true | Include physics bodies. |
| `public PhysicsRestInfo[] CollisionResult { get; }` | empty | Caller-owned copy of cached typed contacts. |
| `public void AddException(CollisionObject node)` / `RemoveException(CollisionObject node)` | — | Change scene collider exclusion by RID. |
| `public void AddExceptionRID(RID rid)` / `RemoveExceptionRID(RID rid)` | — | Change any scene/server RID exclusion. |
| `public void ClearExceptions()` | — | Clear every current exception, including the parent RID. |
| `public bool GetCollisionMaskValue(int layerNumber)` / `SetCollisionMaskValue(int layerNumber, bool value)` | — | Read/write a one-based layer bit from 1 through 32. |
| `public void ForceShapecastUpdate()` | — | Sample now regardless of `Enabled`; requires a live attached world and Shape. |
| `public bool IsColliding()` / `GetCollisionCount()` | false / 0 | Cached collision flag and number of cached contacts. |
| `public ElectronObject? GetCollider(int index)` | — | Live sampled object association, or null when unassigned/disposed/collected. |
| `public RID GetColliderRID(int index)` / `GetColliderShape(int index)` | — | Cached RID and stable direct shape-owner index. |
| `public Vector2 GetCollisionPoint(int index)` / `GetCollisionNormal(int index)` | — | Cached global collider contact point and outward normal. |
| `public float GetClosestCollisionSafeFraction()` / `GetClosestCollisionUnsafeFraction()` | 0 / 0 | Last motion bracket; one/one on a moving miss. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Packs shape and query options, excluding runtime contacts and exceptions. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Restores the exact ShapeCast type. |
| `protected override void OnEnterTree()` / `OnExitTree()` | — | Manage parent RID and automatic processing. |
| `protected override void OnNotification(int what)` | — | Sample during internal physics notification. |
| `protected override void Dispose(bool disposing)` | — | Release the owned parameter object; leave the borrowed Shape caller-owned. |

## Property descriptions

`Shape` accepts a live concrete [Shape](Shape.md) or null. A null or disposed shape makes forced sampling fail without changing the prior snapshot. A successful assignment lazily borrows the resource's physics RID; clearing this property drops the query reference but does not dispose the resource. The Shape owner remains responsible for its lifetime. Geometry edits become visible at the next sample. `TargetPosition` is transformed by the node's current global basis, including rotation; active physics shape queries require unit scale and zero skew under [ADR 0054](../decisions/physics.md#adr-0054). `TargetPosition` rejects nonfinite components without mutation. `Margin` rejects negative or nonfinite values before mutation. `MaxResults` follows the reference's nonpositive behavior: the cast still computes fractions, but stores no contacts. `CollisionMask` tests collider layers even if that collider's own collision mask is zero. `CollideWithAreas` and `CollideWithBodies` are independent.

`Enabled=false` stops automatic sampling and immediately clears only `IsColliding`; the contact count, typed result array and fractions remain until another forced sample. A forced update while disabled can set `IsColliding` again. `ExcludeParent=true` adds a direct collision-parent RID on tree entry; toggling it while attached adds or removes that RID. Leaving the parent removes its automatic exception, so a later attachment can query that body. `ClearExceptions()` clears the current set, including parent, until the flag is toggled or the node enters a tree again. Explicit exceptions are runtime state and are not packed. All other options and the borrowed Shape association survive PackedScene; results do not.

## Method descriptions

`ForceShapecastUpdate()` queries the current shared physics space immediately. It calls the direct motion cast once to find the earliest new collision. When that fraction is below one, it moves the query shape just inside the first impact so backend contact tolerance yields stable points. It then queries rest contacts at that fixed pose, excluding each returned collider RID, up to `MaxResults`. With a zero target it samples only current overlap and leaves fractions at zero; with a moving miss it stores fractions `(1, 1)`. A detached call, missing/disposed Shape, wrong owner thread, world-step call or invalid active transform rejects before replacing the prior cached results. The query reuses its parameter, exclusion and result storage after warmup.

`GetCollisionCount()` and `CollisionResult` expose the held contacts. The array is a copy; each value carries collider RID, shape-owner index, global point and normal, and point velocity. The array getter resolves live physical scene references separately from sampled object identity. A disposed assigned object resolves to null while its cached instance ID and RID remain. Indexed getters throw `ArgumentOutOfRangeException` for a missing index. `GetCollider()` resolves the weak object association captured by the sample, so later rebinding does not retarget it. Attached reads require the scene owner thread. `GetClosestCollisionSafeFraction()` and `GetClosestCollisionUnsafeFraction()` return the last bracket without sampling.

## Verification and limits

[ShapeCastTests](../../tests/Electron2D.Tests/ShapeCastTests.cs) checks defaults, invalid writes, PackedScene, first-frame and automatic samples, pause, fixture failure/recovery, multiple colliders and caps, parent/RID exceptions, Area/body/layer filters, zero motion, rotation, server-only RID lifetime, disabled force updates, owner-thread errors and 64 warmed active frames with zero managed allocation on Linux/.NET 8. Physics debug-gizmo drawing and virtual tile collision-object projections remain [coverage gaps](../coverage/classes/ShapeCast2D.md). Native allocator, other platforms and owner visual acceptance remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).


Assigned object identity is sampled with each query result, including raw server objects.
Rebinding does not retarget earlier results; disposal/collection makes object resolution null
without erasing the sampled ID. See [object associations](../components/physics-object-bindings.md).
