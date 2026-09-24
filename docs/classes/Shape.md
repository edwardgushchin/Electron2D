# Shape

Last updated: 2026-09-24

**Inherits:** [Resource](Resource.md) · **Inherited By:** [CircleShape](CircleShape.md), [RectangleShape](RectangleShape.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs)
- **Declaration:** `public abstract class Shape : Resource`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

The reusable 2D collision-geometry role. The caller owns a Shape resource; a [CollisionShape](CollisionShape.md) borrows it for a direct physics-body parent. `Changed` invalidates the body's fixture before its next fixed step. Resource duplication of concrete shapes owns independent dimensions. The first physics profile supports circle and rectangle geometry; the abstract standalone collision/sweep and debug-draw APIs remain incomplete on [Shape2D coverage](../coverage/classes/Shape2D.md).

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

Circle and rectangle bounds, validation and copying are checked in [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs). `Collide`, motion sweeps and contact-array queries await the next standalone shape-query slice. Canvas debug drawing requires renderer resource identity; custom solver bias requires a verified backend mapping. The first body slice and its platform limits are recorded in [ADR 0054](../decisions/physics.md#adr-0054).
