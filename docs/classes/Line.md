# Line

Last updated: 2026-09-23

- Declaration: `public class Line : Entity`
- Source: [Line.cs](../../src/Scene/2D/Line.cs)
- Inherits: [Entity](Entity.md)
- Inherited by: no production type currently
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Contract

Line draws a thick polyline in its local coordinates through the retained canvas renderer. It inherits Node hierarchy and lifecycle, CanvasItem visibility/material/sampling, and Entity transforms. At least two distinct points and a positive Width are required to draw. `Closed` connects the final point to the first when there are at least three distinct points; caps apply only to open lines. Consecutive duplicate points are omitted from the drawing mesh, while `Points` preserves exactly what was assigned.

`Points` is a copied `Vector2[]` snapshot in both directions. `AddPoint`, `ClearPoints`, `GetPointCount`, `GetPointPosition`, `RemovePoint`, and `SetPointPosition` operate on the stored sequence. Invalid indices throw; nonfinite positions are rejected. Properties and methods reject disposed nodes, and mutations on an attached node use the scene owner thread. Point and drawing-property edits request redraw. There is no public SDL dependency.

Width defaults to 10, DefaultColor to white, SharpLimit to 2, RoundPrecision to 8. Negative Width and SharpLimit clamp to zero; RoundPrecision clamps to at least one. JointMode defaults to Sharp, BeginCapMode and EndCapMode to None, TextureMode to None, Closed and Antialiased to false. Undefined enum values are rejected. The three enum contracts are [LineCapMode](Line.LineCapMode.md), [LineJointMode](Line.LineJointMode.md), and [LineTextureMode](Line.LineTextureMode.md).

WidthCurve, Gradient and Texture are borrowed resources. WidthCurve scales the width by normalized distance; Gradient replaces DefaultColor along that distance. Resource changes request redraw; replacing or disposing Line removes the old subscriptions. TextureMode.None samples its first column, Stretch maps the full line to one texture width, and Tile advances UV by distance divided by base width and texture aspect. Tile needs effective repeat sampling on the canvas item. AtlasTexture resolves to its source and remaps UVs at recording. The renderer's normal backend capability checks still apply to texture filtering, repeat and materials.

Line records triangle geometry for sharp, bevel and round joints and for none, box and round caps. SharpLimit bounds miters. Antialiased records a one-local-unit transparent fringe along segment sides. It does not yet feather round caps or joints, and exact bend/cap/gradient/width-curve/texture behavior still needs the complete reference audit; the coverage rows remain Partial where that matters.

Stored scalar, enum, resource and point properties participate in typed PackedScene capture. Captured point arrays and SceneState reads are copied, so mutations cannot change a stored scene or another instance. Scene-local resource duplication follows the ordinary PackedScene policy.

## Properties

| Property | Default | Effect |
| --- | --- | --- |
| `Vector2[] Points` | empty | Copied local point sequence; finite values only. |
| `bool Closed` | false | Connects the last point to the first when there are at least three distinct points. |
| `float Width` | 10 | Base width in local units, clamped to zero. |
| `Curve? WidthCurve` | null | Borrowed normalized width multiplier. |
| `Color DefaultColor` | white | Uniform color when Gradient is null. |
| `Gradient? Gradient` | null | Borrowed color transition along the line. |
| `Texture? Texture` | null | Borrowed sampled texture. |
| `LineTextureMode TextureMode` | None | First column, tiled or stretched longitudinal UVs. |
| `LineJointMode JointMode` | Sharp | Miter, bevel or round bend. |
| `LineCapMode BeginCapMode` | None | Shape of an open line's first endpoint. |
| `LineCapMode EndCapMode` | None | Shape of an open line's final endpoint. |
| `float SharpLimit` | 2 | Maximum miter length in half-widths, clamped to zero. |
| `int RoundPrecision` | 8 | Subdivisions per half circle, clamped to at least one. |
| `bool Antialiased` | false | Adds transparent fringe along segment sides. |

## Methods

| Method | Effect |
| --- | --- |
| `AddPoint(Vector2 position, int index = -1)` | Insert at a valid index; otherwise append. |
| `ClearPoints()` | Remove all stored points. |
| `GetPointCount()` | Count stored points, including duplicates. |
| `GetPointPosition(int index)` | Return a stored point; invalid indices throw. |
| `RemovePoint(int index)` | Remove a stored point; invalid indices throw. |
| `SetPointPosition(int index, Vector2 position)` | Replace a stored point with a finite position. |

## Verification

[LineTests](../../tests/Electron2D.Tests/LineTests.cs) covers defaults, point edits, geometry, caps, closed loops, transparent fringe and PackedScene state. [LineRenderingTests](../../tests/Electron2D.Tests/LineRenderingTests.cs) checks native cap, bend, texture and redraw pixels. Native readback passed on Linux Wayland compatibility and GPU backends (including imported HLSL and GLSL), plus dummy/software compatibility. Other platforms and owner visual acceptance remain unverified.
