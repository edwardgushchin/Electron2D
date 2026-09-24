# Shape

Last updated: 2026-09-25

**Inherits:** [Resource](Resource.md) · **Inherited By:** [CircleShape](CircleShape.md), [CapsuleShape](CapsuleShape.md), [RectangleShape](RectangleShape.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs)
- **Declaration:** `public abstract class Shape : Resource`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

The reusable 2D collision-geometry role. The caller owns a Shape resource; a [CollisionShape](CollisionShape.md) borrows it for a direct physics-body or [Area](Area.md) parent. `Changed` invalidates the parent's fixture before its next fixed step. Geometry revisions also let attached owners detect an edit when an earlier user `Changed` subscriber throws. Resource duplication of concrete shapes owns independent dimensions. The current profile supports circle, capsule and rectangle geometry; the abstract standalone collision/sweep and debug-draw APIs remain incomplete on [Shape2D coverage](../coverage/classes/Shape2D.md).

## API summary

| Member | Contract |
| --- | --- |
| `protected Shape()` | Base construction through a concrete derived shape. |
| `public abstract Rect2 GetRect()` | Returns the local bounding rectangle in scene units. |

## Method description

<a id="getrect"></a>
### `GetRect()`

Concrete shapes return bounds centered on their origin, independent of the scene transform or a Box2D world. A disposed resource rejects the call. The returned rectangle is a value copy.

## Limits and verification

Circle and rectangle bounds, validation and copying are checked in [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs); [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks the capsule. `Collide`, motion sweeps and contact-array queries await the next standalone shape-query slice. Canvas debug drawing requires renderer resource identity; custom solver bias requires a verified backend mapping. The body and capsule geometry boundaries are recorded in [ADRs 0054 and 0059](../decisions/physics.md#adr-0059).
