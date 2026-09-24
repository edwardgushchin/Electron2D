# RectangleShape

Last updated: 2026-09-24

**Inherits:** [Shape](Shape.md), [Resource](Resource.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs)
- **Declaration:** `public sealed class RectangleShape : Shape`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

An independently owned rectangular collision resource centered on its local origin. A CollisionShape node beneath a body or area borrows it; a size change invalidates attached fixtures for the next fixed step even when an earlier user `Changed` subscriber throws. Managed duplication owns independent dimensions and clears external Resource identity.

## API summary

| Member | Contract |
| --- | --- |
| `public RectangleShape()` | Creates a 20 × 20 scene-unit rectangle. |
| `public Vector2 Size { get; set; }` | Finite positive width and height; default (20, 20). |
| `public override Rect2 GetRect()` | Returns a centered local rectangle of the current size. |
| `protected override Resource CreateDuplicateInstance()` | Creates a new exact-type resource. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies the stored dimensions without intermediate events. |

## Member descriptions

<a id="size"></a>
### `Size`

Either zero, negative or nonfinite component throws `ArgumentOutOfRangeException` before mutation. A successful assignment commits and emits `Changed`, including an equal value. The Box2D fixture uses half-extents after scene-unit conversion.

<a id="getrect"></a>
### `GetRect()`

Returns `(-Size.X/2, -Size.Y/2, Size.X, Size.Y)` in local scene units, independent of SceneTree membership.

## Verification and limits

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) verifies default/invalid geometry, falling box versus floor, collision filtering and borrowed resource lifetime. Inherited standalone shape queries remain separate [coverage rows](../coverage/classes/Shape2D.md).
