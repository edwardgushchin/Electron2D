# Shape

Last updated: 2026-09-26

**Inherits:** [Resource](Resource.md) · **Inherited By:** [CircleShape](CircleShape.md), [CapsuleShape](CapsuleShape.md), [SegmentShape](SegmentShape.md), [SeparationRayShape](SeparationRayShape.md), [ConvexPolygonShape](ConvexPolygonShape.md), [ConcavePolygonShape](ConcavePolygonShape.md), [RectangleShape](RectangleShape.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs)
- **Declaration:** `public abstract class Shape : Resource`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

The reusable 2D collision-geometry role. The caller owns a Shape resource; a [CollisionShape](CollisionShape.md) borrows it for a direct physics-body or [Area](Area.md) parent. [PhysicsShapeQueryParameters2D](PhysicsShapeQueryParameters2D.md) can also borrow it for direct shape queries, lazily registering a physics RID that remains stable through edits and is released on disposal. `Changed` invalidates the parent's and borrowed server fixtures before their next fixed step or direct query. Geometry revisions also let attached owners detect an edit when an earlier user `Changed` subscriber throws. Resource duplication of concrete shapes owns independent geometry state. The current profile supports circle, capsule, segment, separation ray, convex polygon, concave segment collection and rectangle geometry; the abstract standalone collision/sweep and debug-draw APIs remain incomplete on [Shape2D coverage](../coverage/classes/Shape2D.md).

## API summary

| Member | Contract |
| --- | --- |
| `protected Shape()` | Base construction through a concrete derived shape. |
| `public abstract Rect2 GetRect()` | Returns the local bounding rectangle in scene units. |

## Method description

<a id="getrect"></a>
### `GetRect()`

Concrete shapes return local-axis bounds (including drawing padding for a separation ray), which may be off-center, independent of the scene transform or a Box2D world. A disposed resource rejects the call. The returned rectangle is a value copy.

## Limits and verification

Circle and rectangle bounds, validation and copying are checked in [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs); [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks the capsule; [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks off-center line bounds and fixtures; [ConvexPolygonShapeTests](../../tests/Electron2D.Tests/ConvexPolygonShapeTests.cs) checks compound solid contours; [ConcavePolygonShapeTests](../../tests/Electron2D.Tests/ConcavePolygonShapeTests.cs) checks hollow paired contours. [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks borrowed RID lifetime, edits and direct query geometry. Standalone `Collide`, motion sweeps and contact-array Shape methods still await their own slice. Canvas debug drawing requires renderer resource identity; custom solver bias requires a verified backend mapping. See [ADR 0063](../decisions/physics.md#adr-0063) for direct query ownership.
