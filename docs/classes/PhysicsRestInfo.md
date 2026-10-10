# PhysicsRestInfo

Last updated: 2026-10-10

**Declaration:** `public readonly struct PhysicsRestInfo` · **Source:** [PhysicsDirectSpaceState.Contacts.cs](../../src/Servers/Physics/PhysicsDirectSpaceState.Contacts.cs) · **Component:** [Physics queries](../components/physics-queries.md)

## Description

Copied typed contact selected by [GetRestInfo](PhysicsDirectSpaceState.md). The direct view chooses the deepest eligible manifold contact, breaking equal-depth ties by collider RID and shape-owner index. The result does not own or keep its collider live.

## Example

Partial snippet with a live `direct` view and `query`:

```csharp
PhysicsRestInfo? rest = direct.GetRestInfo(query);
if (rest is { } contact) Console.WriteLine(contact.Normal);
```

## Constructors

| Constructor | Contract |
| --- | --- |
| `public PhysicsRestInfo(RID collider, int shapeIndex, Vector2 point, Vector2 normal, Vector2 linearVelocity)` | Validated result construction for a query implementation. |

## Properties

| Property | Contract |
| --- | --- |
| `public RID ColliderRID { get; }` | Collider identity at query time. |
| `public CollisionObject? Collider { get; }` | Scene collider, or null for a server-only collider. |
| `public ulong ColliderID { get; }` | Sampled assigned instance ID, or zero when unassigned. |
| `public ElectronObject? ColliderObject { get; }` | Live sampled weak association, or null after disposal/collection. |
| `public int ShapeIndex { get; }` | Direct shape-owner index. |
| `public Vector2 Point { get; }` | Contact point on the collider in global scene units. |
| `public Vector2 Normal { get; }` | Global unit direction pointing away from the collider. |
| `public Vector2 LinearVelocity { get; }` | Collider velocity at `Point`, in scene units per second; zero for an Area. |

## Constructor descriptions

<a id="constructor"></a>
### PhysicsRestInfo constructor

`collider` must be a live scene or server body/Area RID; `shapeIndex` is its logical shape-owner slot. Wrong/stale RIDs reject with ArgumentException, invalid indices or nonfinite vectors with ArgumentOutOfRangeException, and disposed shape resources with ObjectDisposedException. Attached colliders require their space owner thread, an idle solver and a usable world; violations reject with InvalidOperationException. Detached live colliders can supply identity metadata.

`point` uses global scene units; `normal` is a finite global direction; `linearVelocity` is the global contact-point velocity in scene units per second. Values are preserved as supplied. Ordinary Area queries report zero velocity. The constructor does not simulate or prove a collision; the query implementation supplies physical geometry. It samples ColliderID and the weak ColliderObject association itself, while Collider remains the separate scene convenience reference. Later rebind, disposal or collection cannot change the sampled ID. The value does not own a collider's lifetime; default(struct) remains an empty value.

The separate [public consumer](../../examples/PhysicsResultConstruction/ResultConstructionChecks.cs) constructs this family on CPU/GPU, validates scene/raw identities, rebinding, disposal/collection, stale/wrong IDs, threads, synchronized callbacks and failed worlds, and checks zero warmed managed allocation. Backend registration and direct-state extension dispatch remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Property descriptions

`ColliderRID` and `ShapeIndex` preserve identity across compound backend fixtures. `Collider` exposes the physical scene object; ColliderObject and ColliderID expose the sampled assigned instance independently. `Point` lies on that collider's contact surface. `Normal` points toward the query shape. `LinearVelocity` includes angular motion at `Point` for a body and is zero for an Area sensor.

## Verification

[PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) covers static, moving, server-only and scene collider contacts, body/Area filtering, sweep contacts, finite normals and warmed managed allocation. Native allocator and other platforms are unverified. See [ADR 0063](../decisions/physics.md#adr-0063).


ColliderObject returns the live weakly borrowed instance assigned at sampling time; ColliderID
retains its sampled ID even after disposal or rebinding. Collider remains physical scene-collider
convenience. See [object associations](../components/physics-object-bindings.md).
