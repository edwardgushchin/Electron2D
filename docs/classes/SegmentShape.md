# SegmentShape

Last updated: 2026-09-26

**Inherits:** [Shape](Shape.md), [Resource](Resource.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs)
- **Declaration:** `public sealed class SegmentShape : Shape`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

A caller-owned two-sided line segment resource for terrain edges, dynamic bodies and area sensors. Its endpoints use the local scene-unit coordinate system and do not have to surround the origin. A direct [CollisionShape](CollisionShape.md) child borrows it and rotates/translates its backend fixture. Resource changes rebuild attached fixtures before their next fixed step; a duplicate owns independent endpoint state.

## Example

```csharp
using var edge = new SegmentShape { A = new Vector2(-80, 10), B = new Vector2(80, -10) };
var ground = new StaticBody();
ground.AddChild(new CollisionShape { Shape = edge });
// Keep edge alive while the ground borrows it.
```

## API summary

| Member | Contract |
| --- | --- |
| `public SegmentShape()` | Creates endpoints (0, 0) and (0, 10). |
| `public Vector2 A { get; set; }` | First finite local endpoint. |
| `public Vector2 B { get; set; }` | Second finite local endpoint. |
| `public override Rect2 GetRect()` | Returns exact tight local-axis bounds, including zero width or height. |
| `protected override Resource CreateDuplicateInstance()` | Creates an independent segment for Resource copying. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies the two stored endpoints. |

## Property descriptions

<a id="a"></a>
<a id="b"></a>
### `A` and `B`

`A` defaults to zero and `B` to (0, 10). Each successful unequal assignment publishes `Changed` and increments the geometry revision. An equal assignment does neither. Both endpoints and their difference must be finite so local bounds remain representable; invalid input throws `ArgumentOutOfRangeException` before changing the previous value. Equal endpoints are accepted and represent a point. The backend uses a zero-radius point at the midpoint whenever the segment length is at or below its linear slop, currently 0.5 scene units; public endpoints remain exact.

## Method and lifecycle descriptions

<a id="getrect"></a>
### `GetRect()`

Returns a value-copy rectangle from each component's minimum endpoint value and the absolute component differences. It can be off-center and have a zero-size axis. It needs no SceneTree and throws `ObjectDisposedException` on a disposed resource.

The concrete Resource copy hooks preserve both endpoints. A CollisionShape borrows either instance and releases its event connection on replacement or disposal. Geometry revisions let an attached body detect a successful edit even when an earlier user `Changed` subscriber throws. The scene tree owns backend fixtures; the resource stays caller-owned.

## Physics behavior and verification

A static segment constrains circles approaching from either side and a rotated child forms a vertical wall or slope. Areas use the same geometry for nonresponding overlap detection. A dynamic body made only of segments retains its requested mass; the backend center and thin-rod inertia are weighted by each segment's length and offset. Bodies without fixtures retain mass with zero inertia. Extreme mass/inertia that would make the backend inverse nonfinite rejects before mutation.

[SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks defaults, invalid/equal endpoints, exact bounds, independent copying, PackedScene borrowing, live edits after callback failure, two-sided/rotated/dynamic contacts, area sensors, zero-length fallback, mass/inertia and 64 warmed unchanged body/area frames without managed allocations on Linux/.NET 8. The short-segment adaptation is defined in [ADR 0061](../decisions/physics.md#adr-0061); native allocator, other platforms, owner visual acceptance and inherited standalone shape queries remain separately unverified or incomplete.

Direct [shape queries](PhysicsDirectSpaceState.md) use the two-sided segment or its zero-length point fallback; [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) verifies both.
