# RayCast

Last updated: 2026-10-05

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [RayCast.cs](../../src/Scene/2D/RayCast.cs)
- **Declaration:** `public sealed class RayCast : Entity`
- **Component:** [Physics server and direct queries](../components/physics-queries.md)

## Description

A spatial ray from its local origin to `TargetPosition` that caches the nearest eligible collider. An enabled RayCast samples in its internal fixed physics lane and holds that result until the next eligible physics frame; `ForceRaycastUpdate()` samples immediately, even while disabled. Its endpoint follows the full global transform, including rotation and scale. A direct CollisionObject parent is excluded by default. The current scene and server share one [World](World.md) solver/query view. A raw hit retains its collider RID and can report an explicitly assigned object instance.

## Example

```csharp
var ray = new RayCast { TargetPosition = new Vector2(0, 120) };
player.AddChild(ray);
// In a physics callback after the tree has advanced:
if (ray.IsColliding()) Console.WriteLine(ray.GetColliderRID());
```

The snippet assumes `player` is an attached collision body. The ray is also usable under an ordinary Entity.

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public RayCast()` | — | Detached enabled ray with an empty result. |
| `public Vector2 TargetPosition { get; set; }` | (0, 50) | Finite local endpoint; zero samples a 0.01-unit downward fallback. |
| `public uint CollisionMask { get; set; }` | 1 | Eligible 32-bit collider layers. |
| `public bool Enabled { get; set; }` | true | Automatic fixed-frame sampling; false clears only collision flag. |
| `public bool ExcludeParent { get; set; }` | true | Exclude a direct CollisionObject parent by RID. |
| `public bool HitFromInside { get; set; }` | false | Allow a containing filled shape with zero hit normal. |
| `public bool CollideWithAreas { get; set; }` | false | Include Area sensors. |
| `public bool CollideWithBodies { get; set; }` | true | Include physics bodies. |
| `public void AddException(CollisionObject node)` / `RemoveException(CollisionObject node)` | — | Add/remove a scene collider RID. |
| `public void AddExceptionRID(RID rid)` / `RemoveExceptionRID(RID rid)` | — | Add/remove a RID, including server-only colliders. |
| `public void ClearExceptions()` | — | Clear explicit exclusions; re-add an enabled direct-parent exclusion. |
| `public bool GetCollisionMaskValue(int layerNumber)` / `SetCollisionMaskValue(int layerNumber, bool value)` | — | One-based layer bit from 1 through 32. |
| `public void ForceRaycastUpdate()` | — | Sample now regardless of Enabled; requires an attached world. |
| `public bool IsColliding()` | false | Latest automatic or forced collision flag. |
| `public ElectronObject? GetCollider()` | null | Live sampled object association; null on miss, unassigned or disposed/collected target. |
| `public RID GetColliderRID()` | empty | Last hit RID; cleared by a miss, retained by disabling. |
| `public int GetColliderShape()` | 0 | Last direct shape-owner index; zero before hit or after miss. |
| `public Vector2 GetCollisionPoint()` / `GetCollisionNormal()` | zero | Last hit point/normal, retained after a miss or disable. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores options and target, without serializing transient hit/exceptions. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Restores the exact RayCast type in PackedScene. |
| `protected override void OnEnterTree()` / `OnExitTree()` | — | Enable/disable internal fixed processing and parent RID exclusion. |
| `protected override void OnNotification(int what)` | — | Samples on internal physics-process notification. |

## Property descriptions

<a id="targetposition"></a>
### `TargetPosition`

This finite local endpoint is transformed by the node's current global Transform on each sample. Its default is `(0, 50)`. Zero uses a tiny `(0, 0.01)` downward ray rather than an empty cast. A nonfinite assignment throws `ArgumentOutOfRangeException` without mutation. A property change does not refresh the held hit until a physics frame or `ForceRaycastUpdate()`.

<a id="filters"></a>
### `CollisionMask`, `CollideWithAreas`, `CollideWithBodies` and `HitFromInside`

The mask defaults to layer one and tests each collider's layer independently of that collider's own mask. The Area flag defaults false; body flag defaults true. `HitFromInside=true` reports a filled shape containing the origin at that origin with normal zero. `GetCollisionMaskValue` and `SetCollisionMaskValue` use one-based numbers 1–32 and throw `ArgumentOutOfRangeException` before changing bits outside that range. All options survive PackedScene.

<a id="enabled"></a>
### `Enabled`

True schedules the internal physics callback while the node can process; pause skips sampling and retains the previous snapshot. False stops automatic sampling and clears only `IsColliding`. The collider RID, shape index, point and normal remain until another sample. Setting false again after a forced hit also clears the flag. `ForceRaycastUpdate()` still runs while disabled. The value is stored in PackedScene.

<a id="excludeparent"></a>
### `ExcludeParent`

True by default. On tree entry, the RID of a direct CollisionObject parent joins the exception set; changing this flag while attached adds/removes that RID. `ClearExceptions()` restores the parent RID when this flag is true. An ordinary Entity parent does not add an exception. The flag is stored, while individual exceptions are runtime state and are not packed.

## Method descriptions

<a id="exceptions"></a>
### Exception methods

`AddException` and `RemoveException` take a live CollisionObject and use its RID; null throws `ArgumentNullException`, and a disposed node rejects its RID read. RID overloads also work for server-created bodies and Areas. Duplicate additions and absent removals are no-ops. The cached RID array changes only when the exception set changes, so warmed per-frame sampling allocates no managed memory.

<a id="forceraycastupdate"></a>
### `ForceRaycastUpdate()`

Uses the current scene transform, target, masks and exception set immediately, even before the first physics frame or while disabled. A detached call throws `InvalidOperationException`; off-owner access and querying during solver stepping also reject. The query prepares pending scene body geometry before testing. A query error leaves the former snapshot intact so a corrected later call can recover.

<a id="resultmethods"></a>
### Cached result methods

`IsColliding()` reports the last sample or immediate disabled state. On a hit, collider RID, shape index, point and normal update together. On a miss, `IsColliding` becomes false and RID/shape reset to empty/zero, while point and normal retain their last hit values. `GetCollider()` resolves the sampled weak object association and returns null when unassigned, disposed or collected; later body rebinding does not change the cached target. Attached result reads require the scene owner thread. The typed return permits future non-CollisionObject scene owners, such as tile collision nodes, but those virtual-body mappings remain incomplete in [coverage](../coverage/classes/RayCast2D.md).

## Lifecycle and limits

The node owns no backend shape or separate solver world. Internal physics processing follows Node pause and process-priority rules. Disabling or leaving a tree stops automatic queries; leaving a CollisionObject parent also removes its automatic RID exception so reparenting does not suppress the former parent. A detached node retains its last snapshot, but cannot force a query until reattached. [RayCastTests](../../tests/Electron2D.Tests/RayCastTests.cs) checks defaults, bit bounds, PackedScene state, first-frame and forced sampling, pause, exclusion changes and reparenting, Area/body filtering, inside hits, stale result fields, callback failure, server-only RIDs, owner-thread guards and 64 warmed active frames with zero managed allocation on Linux/.NET 8. Physics debug-gizmo drawing and virtual tile collision-object results remain separate dependencies. Native allocator, other platforms and owner visual acceptance remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).


Assigned object identity is sampled with each query result, including raw server objects.
Rebinding does not retarget earlier results; disposal/collection makes object resolution null
without erasing the sampled ID. See [object associations](../components/physics-object-bindings.md).
