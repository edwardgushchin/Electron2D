# Shape

Last updated: 2026-09-25

**Inherits:** [Resource](Resource.md) · **Inherited By:** [CircleShape](CircleShape.md), [CapsuleShape](CapsuleShape.md), [SegmentShape](SegmentShape.md), [ConvexPolygonShape](ConvexPolygonShape.md), [RectangleShape](RectangleShape.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs)
- **Declaration:** `public abstract class Shape : Resource`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

The reusable 2D collision-geometry role. The caller owns a Shape resource; a [CollisionShape](CollisionShape.md) borrows it for a direct physics-body or [Area](Area.md) parent. `Changed` invalidates the parent's fixture set before its next fixed step. Geometry revisions also let attached owners detect an edit when an earlier user `Changed` subscriber throws. Resource duplication of concrete shapes owns independent geometry state. The current profile supports circle, capsule, segment, convex polygon and rectangle geometry; the abstract standalone collision/sweep and debug-draw APIs remain incomplete on [Shape2D coverage](../coverage/classes/Shape2D.md).

## API summary

| Member | Contract |
| --- | --- |
| `protected Shape()` | Base construction through a concrete derived shape. |
| `public abstract Rect2 GetRect()` | Returns the local bounding rectangle in scene units. |

## Method description

<a id="getrect"></a>
### `GetRect()`

Concrete shapes return tight local-axis bounds, which may be off-center, independent of the scene transform or a Box2D world. A disposed resource rejects the call. The returned rectangle is a value copy.

## Limits and verification

Circle and rectangle bounds, validation and copying are checked in [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs); [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks the capsule; [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks off-center line bounds and fixtures; [ConvexPolygonShapeTests](../../tests/Electron2D.Tests/ConvexPolygonShapeTests.cs) checks compound solid contours. `Collide`, motion sweeps and contact-array queries await the next standalone shape-query slice. Canvas debug drawing requires renderer resource identity; custom solver bias requires a verified backend mapping. The body and concrete shape boundaries are recorded in [ADRs 0054, 0059, 0061 and 0062](../decisions/physics.md#adr-0062).
