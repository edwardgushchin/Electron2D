# WorldBoundaryShape

Last updated: 2026-10-09

**Inherits:** [Shape](Shape.md), [Resource](Resource.md)

- **Source:** [WorldBoundaryShape.cs](../../src/Scene/Resources/WorldBoundaryShape.cs)
- **Declaration:** `public sealed class WorldBoundaryShape : Shape`
- **Component:** [Collision shapes](../components/physics-shapes.md#infinite-world-boundaries)

## Description

An infinite solid half-plane `Normal.Dot(localPoint) <= Distance`. The normal points
into free space. This is analytic geometry, with no finite width or wall thickness.
A CollisionShape offsets and rotates the plane for a body or Area. The borrowed
resource contributes no geometric mass or inertia; explicit body parameters remain
effective. Two boundaries do not collide. CPU scene/server use is implemented;
independent GPU geometry executes internally, while public GPU binding remains open.

## Example

```csharp
using var boundary = new WorldBoundaryShape();
var floor = new StaticBody { Position = new(0, 400) };
floor.AddChild(new CollisionShape { Shape = boundary });
// Keep boundary alive for the lifetime of the borrowed collision slot.
```

## API summary

| Member | Contract |
| --- | --- |
| `public WorldBoundaryShape()` | Upward normal and zero distance. |
| `public Vector2 Normal { get; set; }` | Finite nonzero local normal; nonunit magnitude is retained. |
| `public float Distance { get; set; }` | Finite right-hand side of the line equation. |
| `public override Rect2 GetRect()` | Finite editing marker, not collision bounds. |
| `protected override Resource CreateDuplicateInstance()` | Creates an independent boundary resource. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies raw normal/distance independently. |

## Property and method descriptions

### Normal and Distance

Defaults are `(0, -1)` and `0`. Signed distance along the unit normal equals
`Distance / Normal.Length()`: normal `(0, -2)` and distance `10` place the line at
local Y = -5. Invalid or nonrepresentable normalized offsets reject before mutation.
Equal writes are silent; changed values revise borrowed geometry before the next
query or step. Resource getters/setters reject disposal. Copies preserve authored
normal magnitude. Disposal releases the resource-owned RID and retires its fixtures.

### GetRect()

Returns the envelope of a 200-unit tangent segment and a 30-unit outward normal
at the nearest point on the normalized plane. Default: `Rect2(-100, -30, 200, 30)`.
Its finite size is only an editing marker; it never limits physical collision.

### Inherited collision methods

World-boundary pairs use the half-plane normal, including against separation rays.
A zero ray has no contact. Standalone `CollideWithMotion` ignores boundary motion
and tests the other shape at its final pose. Direct-space casts instead preserve
swept-query and initial-overlap rules. See [Shape](Shape.md) and [ADR 0069](../decisions/physics.md#adr-0069).

## Verification

[WorldBoundaryTests](../../tests/Electron2D.Tests/WorldBoundaryTests.cs) checks
nonunit/default/copied geometry, invalid edits, all logical shape identities,
far-away infinite response and queries, Area membership, contact events, sleep,
live edits, removal and translating/rotating CCD. Shared GPU shape/motion matrices
exercise both argument positions. The [component report](../components/physics-shapes.md#infinite-world-boundaries)
records measured allocation, traffic and remaining acceptance limits.

Inherited [Shape.CustomSolverBias](Shape.md#customsolverbias) is stored and copied
alongside geometry. CopyCustomStateTo calls the Shape base and publishes the changed
geometry revision, including in-place copying into a resource with existing borrowers.
PhysicsContactPolicyTests checks duplication, in-place copy and .e2dres round trips.

<a id="getpropertydescriptors"></a>
`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` combines
inherited resource/policy descriptors with this shape's authored geometry for storage.
The built-in resource file registry constructs this concrete shape on load.
