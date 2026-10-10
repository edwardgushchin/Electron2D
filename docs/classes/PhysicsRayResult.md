# PhysicsRayResult

Last updated: 2026-10-10

**Source:** [PhysicsDirectSpaceState.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.cs) · **Declaration:** `public readonly struct PhysicsRayResult`

A typed value for the nearest direct-space ray hit, replacing a dynamic result dictionary. It is returned by [IntersectRay](PhysicsDirectSpaceState.md) or absent as null when no eligible shape is hit.

## Example

Partial snippet inside a query implementation with a live `colliderRID` and its logical `shapeIndex`, plus computed global `position` and `normal`:

```csharp
var hit = new PhysicsRayResult(colliderRID, shapeIndex, position, normal);
Console.WriteLine(hit.ColliderID);
```

## Constructors

| Constructor | Contract |
| --- | --- |
| `public PhysicsRayResult(RID collider, int shapeIndex, Vector2 position, Vector2 normal)` | Validated result construction for a query implementation. |

## Properties

| Member | Meaning |
| --- | --- |
| `public RID ColliderRID { get; }` | Stable server identity, including server-only colliders. |
| `public CollisionObject? Collider { get; }` | Scene object when one owns the hit; null for server-only bodies/Areas. |
| `public ulong ColliderID { get; }` | Sampled assigned instance ID, or zero when unassigned. |
| `public ElectronObject? ColliderObject { get; }` | Live sampled weak association, or null after disposal/collection. |
| `public int ShapeIndex { get; }` | Stable direct shape-owner slot across fixture rebuilds. |
| `public Vector2 Position { get; }` | Global scene-unit hit location. |
| `public Vector2 Normal { get; }` | Outward unit normal, or zero for an inside-origin hit. |

## Constructor descriptions

<a id="constructor"></a>
### PhysicsRayResult constructor

`collider` must be a live scene or server body/Area RID; `shapeIndex` is its logical shape-owner slot. Wrong/stale RIDs reject with ArgumentException, invalid indices or nonfinite vectors with ArgumentOutOfRangeException, and disposed shape resources with ObjectDisposedException. Attached colliders require their space owner thread, an idle solver and a usable world; violations reject with InvalidOperationException. Detached live colliders can supply identity metadata.

`position` is a global point in scene units; `normal` is a finite global direction, including zero for an inside hit. Both values are preserved as supplied. The constructor does not simulate or prove a collision; the query implementation supplies physical geometry. It samples ColliderID and the weak ColliderObject association itself, while Collider remains the separate scene convenience reference. Later rebind, disposal or collection cannot change the sampled ID. The value does not own a collider's lifetime; default(struct) remains an empty value.

The separate [public consumer](../../examples/PhysicsResultConstruction/ResultConstructionChecks.cs) constructs this family on CPU/GPU, validates scene/raw identities, rebinding, disposal/collection, stale/wrong IDs, threads, synchronized callbacks and failed worlds, and checks zero warmed managed allocation. Backend registration and direct-state extension dispatch remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

The value does not own the collider or keep its RID live. A later object/free operation can invalidate server lookup while this copied result remains readable. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks scene and server-only objects, normals, RID/shape identity and inside hits. See [ADR 0063](../decisions/physics.md#adr-0063).


ColliderObject returns the live weakly borrowed instance assigned at sampling time; ColliderID
retains its sampled ID even after disposal or rebinding. Collider remains physical scene-collider
convenience. See [object associations](../components/physics-object-bindings.md).
