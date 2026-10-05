# PhysicsShapeQueryParameters

Last updated: 2026-10-05

**Inherits:** ElectronObject · **Source:** [PhysicsShapeQueryParameters.cs](../../src/Servers/Physics/PhysicsShapeQueryParameters.cs) · **Component:** [Physics queries](../components/physics-queries.md)

## Description

Caller-owned mutable input for the four direct shape operations. Assigning `Shape` retains that live resource and lazily registers a borrowed physics RID. Assigning a different `ShapeRID` clears the resource reference; an equal RID retains it. The caller must keep the selected shape live until the query completes. `Exclude` copies on get and set. Query coordinates and margins are scene units; `Motion` is a global displacement, not a velocity. A disposed parameter object rejects access.

## Example

Partial snippet with an attached `player` body and a live `probe` shape:

```csharp
using var query = new PhysicsShapeQueryParameters
{
    Shape = probe,
    Transform = new Transform(0, Vector2.One, 0, new Vector2(0, 70)),
    Motion = new Vector2(0, 40)
};
var fractions = player.GetWorld()!.DirectSpaceState.CastMotion(query);
```

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public PhysicsShapeQueryParameters()` | — | Empty shape selection and default filters. |
| `public Shape? Shape { get; set; }` | null | Retained caller resource; assignment requires a live nonnull shape. |
| `public RID ShapeRID { get; set; }` | empty | Server shape identity; a different RID clears `Shape`. |
| `public Transform Transform { get; set; }` | identity | Finite global pose with unit scale and zero skew. |
| `public Vector2 Motion { get; set; }` | zero | Finite global displacement. |
| `public float Margin { get; set; }` | 0 | Finite nonnegative expansion in scene units. |
| `public uint CollisionMask { get; set; }` | all bits | Eligible collider layers. |
| `public RID[] Exclude { get; set; }` | empty | Copied collider RIDs to skip. |
| `public bool CollideWithBodies { get; set; }` | true | Include body fixtures. |
| `public bool CollideWithAreas { get; set; }` | false | Include Area sensors. |

## Property descriptions

`Shape` creates its borrowed RID on first assignment. The server may attach it to a body or Area; later geometry changes update those fixtures before the next direct query. The server rejects mutation and explicit free of a borrowed shape RID, and the Shape owner releases it on disposal. `ShapeRID` can instead select a live server-created shape; an empty or stale RID fails when queried. `Transform` rejects nonfinite, scaled or skewed poses under [ADR 0054](../decisions/physics.md#adr-0054). `Motion` and `Margin` reject nonfinite components, and `Margin` rejects negative values. These invalid assignments leave the previous value intact. `CollisionMask` tests collider layers independently of the collider's own mask. `Exclude` rejects null; entries identify whole colliders rather than individual fixture pieces. Area and body flags are independent.

## Verification and limits

[PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) covers defaults, invalid rollback, copies, RID/resource switching, live geometry edits after a throwing listener, borrowed fixture lifetime, and all four direct operations. Independent viewport canvas filtering remains a separate world-identity prerequisite. Native allocator, other platforms and owner visual acceptance are unverified. See [ADR 0063](../decisions/physics.md#adr-0063).
