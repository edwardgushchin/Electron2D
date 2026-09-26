# PhysicsPointResult2D

Last updated: 2026-09-26

**Source:** [PhysicsDirectSpaceState.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.cs) · **Declaration:** `public readonly struct PhysicsPointResult2D`

A typed value for a filled shape containing a direct-space query point, replacing a dynamic result dictionary. [IntersectPoint](PhysicsDirectSpaceState.md) returns caller-owned arrays of these values.

| Member | Meaning |
| --- | --- |
| `public RID ColliderRID { get; }` | Stable server identity, including server-only colliders. |
| `public CollisionObject? Collider { get; }` | Scene object, or null for a server-only body/Area. |
| `public ulong ColliderID { get; }` | Scene object's InstanceID, or zero for server-only results. |
| `public int ShapeIndex { get; }` | Shape-owner slot; several backend pieces of one owner deduplicate to it. |

Results sort by ColliderRID and ShapeIndex before the maximum count is applied. They do not own the collider or keep a freed RID live. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks scene/server identity, ordering, exclusions and result caps. See [ADR 0063](../decisions/physics.md#adr-0063).
