# CollisionPolygonBuildMode

Last updated: 2026-09-25

**Declared in:** [CollisionPolygon](CollisionPolygon.md) · **Source:** [CollisionPolygon.cs](../../src/Scene/2D/CollisionPolygon.cs)

Selects the geometry created from a CollisionPolygon's copied local vertex contour.

| Value | Integer | Physics behavior |
| --- | ---: | --- |
| `Solids` | 0 | Decompose a valid filled contour into solid convex fixtures. |
| `Segments` | 1 | Close the contour into paired, hollow line fixtures. |

`CollisionPolygon.BuildMode` defaults to `Solids`; undefined values throw before changing state. [CollisionPolygonTests](../../tests/Electron2D.Tests/CollisionPolygonTests.cs) verifies both modes, live switching and PackedScene state. [ADR 0066](../decisions/physics.md#adr-0066) records the typed C# enum naming adaptation.
