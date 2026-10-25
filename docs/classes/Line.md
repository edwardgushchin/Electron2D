# Line

Last updated: 2026-10-25

- Declaration: `public partial class Line : Entity`
- Source: [Line.cs](../../src/Scene/2D/Line.cs), [geometry](../../src/Scene/2D/Line.Geometry.cs)
- Inherits: [Entity](Entity.md)
- Inherited by: no production type currently
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Contract

Line draws a thick polyline in its local coordinates through the retained canvas renderer. It inherits Node hierarchy and lifecycle, CanvasItem visibility/material/sampling, and Entity transforms. At least two distinct points and a positive Width are required to draw. `Closed` connects the final point to the first when there are at least three distinct points; caps apply only to open lines. Consecutive duplicate points are omitted from the drawing mesh, while `Points` preserves exactly what was assigned.

`Points` is a copied `Vector2[]` snapshot in both directions. `AddPoint`, `ClearPoints`, `GetPointCount`, `GetPointPosition`, `RemovePoint`, and `SetPointPosition` operate on the stored sequence. Invalid indices throw; nonfinite positions are rejected. Properties and methods reject disposed nodes, and mutations on an attached node use the scene owner thread. Point and drawing-property edits request redraw. There is no public SDL dependency.

Width defaults to 10, DefaultColor to white, SharpLimit to 2, RoundPrecision to 8. Negative Width and SharpLimit clamp to zero; RoundPrecision clamps to at least one. JointMode defaults to Sharp, BeginCapMode and EndCapMode to None, TextureMode to None, Closed and Antialiased to false. Undefined enum values are rejected. The three enum contracts are [LineCapMode](LineCapMode.md), [LineJointMode](LineJointMode.md), and [LineTextureMode](LineTextureMode.md).

WidthCurve, Gradient and Texture are borrowed resources. WidthCurve scales the width by normalized distance; Gradient replaces DefaultColor along that distance. Open-line box and round caps contribute to the total distance used for interior curve, gradient and longitudinal UV samples. Round caps map their arc to texture columns before or after the body; zero-radius caps emit no arc geometry. Resource changes request redraw; replacing or disposing Line removes the old subscriptions. TextureMode.None supplies zero UV coordinates, Stretch maps the full line to one texture width, and Tile advances UV by distance divided by base width and texture aspect. Tile needs effective repeat sampling on the canvas item. AtlasTexture resolves to its source and remaps UVs at recording. The renderer's normal backend capability checks still apply to texture filtering, repeat and materials.

Line builds an indexed strip for sharp, bevel and round joints and for none, box and round caps. Intersections use the adjacent finite segments; near reversals and short turns use bounded fallback geometry. SharpLimit bounds miters, falling back to bevels. RoundPrecision fixes the angular step at half a circle divided by its value, retaining the final shorter step. Width/color/UV attributes interpolate across each emitted triangle. Closed lines build the first joint first and include the closing segment in distance. Width curves need not produce a seamless closure; the closing joint samples their beginning. Zero starting width has a finite fallback.

Antialiased shares the retained polygon boundary path: transformed and snapped sides, caps and joints receive a one-framebuffer-pixel transparent fringe. Coincident strip endpoints cancel shared edges without merging their original color/UV attributes; internal diagonals and cap/body seams receive no fringe. Boundary miters are bounded to four framebuffer pixels. PremultAlpha fades RGB and alpha; Mul rejects because it cannot express coverage alpha. Authored vertex programs run after fringe construction. Self-overlap preserves ordinary triangle blending and has no shape-union guarantee. SDL software triangle rasterization retains its documented subpixel precision limit; OpenGL compatibility and GPU profiles verify fractional coverage. See [geometry and verification](../components/line-geometry.md).

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
| `LineTextureMode TextureMode` | None | Zero UVs, tiled or stretched longitudinal UVs. |
| `LineJointMode JointMode` | Sharp | Miter, bevel or round bend. |
| `LineCapMode BeginCapMode` | None | Shape of an open line's first endpoint. |
| `LineCapMode EndCapMode` | None | Shape of an open line's final endpoint. |
| `float SharpLimit` | 2 | Maximum miter length in half-widths, clamped to zero. |
| `int RoundPrecision` | 8 | Subdivisions per half circle, clamped to at least one. |
| `bool Antialiased` | false | One-framebuffer-pixel coverage on sides, caps and joints. |

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

[LineTests](../../tests/Electron2D.Tests/LineTests.cs), [LineGeometryTests](../../tests/Electron2D.Tests/LineGeometryTests.cs) and the [independent pinned builder check](../../tools/line/check_reference.py) verify point/resource lifetime, strip coverage, ordered color/UV layers, caps, joints, distance sampling, closure and degenerate widths. [LineGeometryRenderingTests](../../tests/Electron2D.Tests/LineGeometryRenderingTests.cs) checks actual side/cap/joint/atlas/alpha/scale/snap pixels, source vertex identity, GPU HDR/MSAA and 64 prepared active redraws at zero owner managed bytes. Linux Wayland/X11 software Vulkan and OpenGL compatibility are exercised; physical hardware, native allocation totals, foreign platforms and owner visual acceptance retain separate gates.
