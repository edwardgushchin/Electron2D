# PhysicsRayResult

Last updated: 2026-10-05

**Source:** [PhysicsDirectSpaceState.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.cs) · **Declaration:** `public readonly struct PhysicsRayResult`

A typed value for the nearest direct-space ray hit, replacing a dynamic result dictionary. It is returned by [IntersectRay](PhysicsDirectSpaceState.md) or absent as null when no eligible shape is hit.

| Member | Meaning |
| --- | --- |
| `public RID ColliderRID { get; }` | Stable server identity, including server-only colliders. |
| `public CollisionObject? Collider { get; }` | Scene object when one owns the hit; null for server-only bodies/Areas. |
| `public ulong ColliderID { get; }` | Sampled assigned instance ID, or zero when unassigned. |
| `public int ShapeIndex { get; }` | Stable direct shape-owner slot across fixture rebuilds. |
| `public Vector2 Position { get; }` | Global scene-unit hit location. |
| `public Vector2 Normal { get; }` | Outward unit normal, or zero for an inside-origin hit. |

The value does not own the collider or keep its RID live. A later object/free operation can invalidate server lookup while this copied result remains readable. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks scene and server-only objects, normals, RID/shape identity and inside hits. See [ADR 0063](../decisions/physics.md#adr-0063).


ColliderObject returns the live weakly borrowed instance assigned at sampling time; ColliderID
retains its sampled ID even after disposal or rebinding. Collider remains physical scene-collider
convenience. See [object associations](../components/physics-object-bindings.md).
