# PhysicsShapeResult2D

Last updated: 2026-09-26

**Declaration:** `public readonly struct PhysicsShapeResult2D` · **Source:** [PhysicsDirectSpaceState.Shapes.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.Shapes.cs) · **Component:** [Physics queries](../components/physics-queries.md)

## Description

Copied typed result of [IntersectShape](PhysicsDirectSpaceState.md). It identifies one collider shape owner; multiple backend pieces of that owner yield one result. It does not own or keep the collider or RID live.

## Example

Partial snippet with a live `direct` view and `query`:

```csharp
foreach (var hit in direct.IntersectShape(query))
    Console.WriteLine($"{hit.ColliderRID}: {hit.ShapeIndex}");
```

## API summary

| Property | Contract |
| --- | --- |
| `public RID ColliderRID { get; }` | Stable scene/server collider identity at query time. |
| `public CollisionObject? Collider { get; }` | Scene collider, or null for a server-only collider. |
| `public ulong ColliderID { get; }` | Scene instance ID, or zero for a server-only collider. |
| `public int ShapeIndex { get; }` | Direct collider shape-owner index, stable across fixture rebuilds. |

## Property descriptions

`ColliderRID` and `ShapeIndex` form the result sort and deduplication key. `Collider` is a borrowed scene reference and may later be disposed. `ColliderID` is obtained from that reference; server-only hits retain their RID and index while exposing null and zero for the scene fields.

## Verification

[PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks scene/server identities, compound deduplication, sorting/filtering and direct query caps. See [ADR 0063](../decisions/physics.md#adr-0063).
