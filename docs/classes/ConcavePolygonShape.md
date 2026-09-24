# ConcavePolygonShape

Last updated: 2026-09-25

**Inherits:** [Shape](Shape.md), [Resource](Resource.md)

- **Source:** [ConcavePolygonShape.cs](../../src/Scene/Resources/ConcavePolygonShape.cs)
- **Declaration:** `public sealed class ConcavePolygonShape : Shape`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

A caller-owned hollow contour made from independent line segments. Consecutive pairs of local points form two-sided edges; the pairs need not connect. A closed collection has no filled interior. A direct [CollisionShape](CollisionShape.md) child borrows the resource and applies its local position and rotation to every edge. This is primarily level and sensor geometry; a body with only these zero-area fixtures uses the mass rule in [ADR 0061](../decisions/physics.md#adr-0061).

## Example

```csharp
using var boundary = new ConcavePolygonShape
{
    Segments = [new Vector2(-80, 0), new Vector2(80, 0),
        new Vector2(-80, 0), new Vector2(-80, -60)]
};
var ground = new StaticBody();
ground.AddChild(new CollisionShape { Shape = boundary });
// Keep boundary alive while the ground borrows it.
```

## API summary

| Member | Contract |
| --- | --- |
| `public ConcavePolygonShape()` | Creates an empty contour with no fixture. |
| `public Vector2[] Segments { get; set; }` | Gets or sets a caller-owned array of endpoint pairs. |
| `public override Rect2 GetRect()` | Returns tight local-axis bounds across every endpoint. |
| `protected override Resource CreateDuplicateInstance()` | Creates an independent contour for Resource copying. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies the stored endpoint array without publishing intermediate changes. |

## Property description

<a id="segments"></a>
### `Segments`

Empty by default. Array positions 0/1 form the first line segment, 2/3 the second, and so on. The array length must be even; every coordinate, pair difference and overall bounds must be finite. Reads and writes copy arrays, so caller mutation cannot change stored geometry. An invalid array throws `ArgumentException` before mutation and null throws `ArgumentNullException`. Every accepted assignment increments the geometry revision and emits `Changed`, including an equal assignment. Empty input removes every fixture on the next fixed step.

A pair whose endpoints coincide, or whose length is at or below the backend's current 0.5-scene-unit tolerance, uses a zero-radius point fixture at its midpoint while retaining exact public points. Each other pair creates a two-sided segment fixture. Direct static, dynamic and area owners can borrow the same resource; the contour stays hollow for overlap tests.

## Method and lifecycle descriptions

<a id="getrect"></a>
### `GetRect()`

Returns a value-copy rectangle spanning every endpoint. It may be off-center and has default zero bounds for an empty contour. It needs no SceneTree and throws `ObjectDisposedException` when the resource has been disposed.

Resource duplication copies endpoint storage independently. A CollisionShape borrows either instance and releases its event connection on replacement or disposal. Geometry revisions let a body or area detect a successful edit even if an earlier user `Changed` listener throws. The scene tree owns backend fixture IDs; the caller owns the Shape resource.

## Verification and limits

[ConcavePolygonShapeTests](../../tests/Electron2D.Tests/ConcavePolygonShapeTests.cs) checks paired endpoints, exact bounds, invalid/equal writes, independent copying, PackedScene borrowing, multi-edge terrain contact, dynamic zero-area mass and torque, a body fully inside versus touching a hollow area, point fallback, live edits after callback failure and 64 warmed unchanged body/area frames without managed allocations on Linux/.NET 8. [ADR 0064](../decisions/physics.md#adr-0064) records the hollow resource and [CollisionPolygon2D coverage](../coverage/classes/CollisionPolygon2D.md) names the separate scene-node and one-way contact dependencies. Native allocator, other platforms, owner visual acceptance and inherited standalone shape queries remain unverified or incomplete.
