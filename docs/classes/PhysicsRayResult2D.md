# PhysicsRayResult2D

Last updated: 2026-09-26

**Source:** [PhysicsDirectSpaceState.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.cs) · **Declaration:** `public readonly struct PhysicsRayResult2D`

A typed value for the nearest direct-space ray hit, replacing a dynamic result dictionary. It is returned by [IntersectRay](PhysicsDirectSpaceState.md) or absent as null when no eligible shape is hit.

| Member | Meaning |
| --- | --- |
| `public RID ColliderRID { get; }` | Stable server identity, including server-only colliders. |
| `public CollisionObject? Collider { get; }` | Scene object when one owns the hit; null for server-only bodies/Areas. |
| `public ulong ColliderID { get; }` | Scene object's InstanceID, or zero for server-only results. |
| `public int ShapeIndex { get; }` | Stable direct shape-owner slot across fixture rebuilds. |
| `public Vector2 Position { get; }` | Global scene-unit hit location. |
| `public Vector2 Normal { get; }` | Outward unit normal, or zero for an inside-origin hit. |

The value does not own the collider or keep its RID live. A later object/free operation can invalidate server lookup while this copied result remains readable. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks scene and server-only objects, normals, RID/shape identity and inside hits. See [ADR 0063](../decisions/physics.md#adr-0063).
