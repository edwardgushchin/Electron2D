# PhysicsShapeResult

Last updated: 2026-10-10

**Declaration:** `public readonly struct PhysicsShapeResult` · **Source:** [PhysicsDirectSpaceState.Shapes.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.Shapes.cs) · **Component:** [Physics queries](../components/physics-queries.md)

## Description

Copied typed result of [IntersectShape](PhysicsDirectSpaceState.md). It identifies one collider shape owner; multiple backend pieces of that owner yield one result. It does not own or keep the collider or RID live.

## Example

Partial snippet with a live `direct` view and `query`:

```csharp
foreach (var hit in direct.IntersectShape(query))
    Console.WriteLine($"{hit.ColliderRID}: {hit.ShapeIndex}");
```

## Constructors

| Constructor | Contract |
| --- | --- |
| `public PhysicsShapeResult(RID collider, int shapeIndex)` | Validated result construction for a query implementation. |

## Properties

| Property | Contract |
| --- | --- |
| `public RID ColliderRID { get; }` | Stable scene/server collider identity at query time. |
| `public CollisionObject? Collider { get; }` | Scene collider, or null for a server-only collider. |
| `public ulong ColliderID { get; }` | Sampled assigned instance ID, or zero when unassigned. |
| `public ElectronObject? ColliderObject { get; }` | Live sampled weak association, or null after disposal/collection. |
| `public int ShapeIndex { get; }` | Direct collider shape-owner index, stable across fixture rebuilds. |

## Constructor descriptions

<a id="constructor"></a>
### PhysicsShapeResult constructor

`collider` must be a live scene or server body/Area RID; `shapeIndex` is its logical shape-owner slot. Wrong/stale RIDs reject with ArgumentException, invalid indices or nonfinite vectors with ArgumentOutOfRangeException, and disposed shape resources with ObjectDisposedException. Attached colliders require their space owner thread, an idle solver and a usable world; violations reject with InvalidOperationException. Detached live colliders can supply identity metadata.

No shape intersection is performed. The value identifies one supplied collider shape. The constructor does not simulate or prove a collision; the query implementation supplies physical geometry. It samples ColliderID and the weak ColliderObject association itself, while Collider remains the separate scene convenience reference. Later rebind, disposal or collection cannot change the sampled ID. The value does not own a collider's lifetime; default(struct) remains an empty value.

The separate [public consumer](../../examples/PhysicsResultConstruction/ResultConstructionChecks.cs) constructs this family on CPU/GPU, validates scene/raw identities, rebinding, disposal/collection, stale/wrong IDs, threads, synchronized callbacks and failed worlds, and checks zero warmed managed allocation. Backend registration and direct-state extension dispatch remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Property descriptions

`ColliderRID` and `ShapeIndex` form the result sort and deduplication key. `Collider` is a borrowed scene reference and may later be disposed. `ColliderID` is sampled from the object association independently of the physical scene reference; raw colliders can report an assigned object through ColliderObject.

## Verification

[PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks scene/server identities, compound deduplication, sorting/filtering and direct query caps. See [ADR 0063](../decisions/physics.md#adr-0063).


ColliderObject returns the live weakly borrowed instance assigned at sampling time; ColliderID
retains its sampled ID even after disposal or rebinding. Collider remains physical scene-collider
convenience. See [object associations](../components/physics-object-bindings.md).
