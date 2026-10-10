# PhysicsPointResult

Last updated: 2026-10-10

**Source:** [PhysicsDirectSpaceState.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.cs) · **Declaration:** `public readonly struct PhysicsPointResult`

A typed value for a filled shape containing a direct-space query point, replacing a dynamic result dictionary. [IntersectPoint](PhysicsDirectSpaceState.md) returns caller-owned arrays of these values.

## Example

Partial snippet inside a query implementation with a live `colliderRID` and its logical `shapeIndex`:

```csharp
var hit = new PhysicsPointResult(colliderRID, shapeIndex);
Console.WriteLine(hit.ColliderID);
```

## Constructors

| Constructor | Contract |
| --- | --- |
| `public PhysicsPointResult(RID collider, int shapeIndex)` | Validated result construction for a query implementation. |

## Properties

| Member | Meaning |
| --- | --- |
| `public RID ColliderRID { get; }` | Stable server identity, including server-only colliders. |
| `public CollisionObject? Collider { get; }` | Scene object, or null for a server-only body/Area. |
| `public ulong ColliderID { get; }` | Sampled assigned instance ID, or zero when unassigned. |
| `public ElectronObject? ColliderObject { get; }` | Live sampled weak association, or null after disposal/collection. |
| `public int ShapeIndex { get; }` | Shape-owner slot; several backend pieces of one owner deduplicate to it. |

## Constructor descriptions

<a id="constructor"></a>
### PhysicsPointResult constructor

`collider` must be a live scene or server body/Area RID; `shapeIndex` is its logical shape-owner slot. Wrong/stale RIDs reject with ArgumentException, invalid indices or nonfinite vectors with ArgumentOutOfRangeException, and disposed shape resources with ObjectDisposedException. Attached colliders require their space owner thread, an idle solver and a usable world; violations reject with InvalidOperationException. Detached live colliders can supply identity metadata.

No point test is performed. The value identifies one supplied collider shape. The constructor does not simulate or prove a collision; the query implementation supplies physical geometry. It samples ColliderID and the weak ColliderObject association itself, while Collider remains the separate scene convenience reference. Later rebind, disposal or collection cannot change the sampled ID. The value does not own a collider's lifetime; default(struct) remains an empty value.

The separate [public consumer](../../examples/PhysicsResultConstruction/ResultConstructionChecks.cs) constructs this family on CPU/GPU, validates scene/raw identities, rebinding, disposal/collection, stale/wrong IDs, threads, synchronized callbacks and failed worlds, and checks zero warmed managed allocation. Backend registration and direct-state extension dispatch remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

Results sort by ColliderRID and ShapeIndex before the maximum count is applied. They do not own the collider or keep a freed RID live. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks scene/server identity, ordering, exclusions and result caps. See [ADR 0063](../decisions/physics.md#adr-0063).


ColliderObject returns the live weakly borrowed instance assigned at sampling time; ColliderID
retains its sampled ID even after disposal or rebinding. Collider remains physical scene-collider
convenience. See [object associations](../components/physics-object-bindings.md).
