# CanvasStroke

Last updated: 2026-09-23

- Declaration: `internal sealed class CanvasStroke`
- Source: [CanvasStroke.cs](../../src/Servers/Rendering/CanvasStroke.cs)
- Component: [Canvas rendering](../components/canvas-rendering.md#stroke-commands)

## Description

Item-owned retained geometry for polylines, independent pairs, dashes, sampled arcs and filled/outlined rectangles/circles/ellipses. It is not a resource or a public API. CanvasItem reuses one slot per recorded stroke; recording resets the slot cursor and disposal clears storage. Access follows the recording/render owner thread. Public usage is shown in [CanvasItem](CanvasItem.md#stroke-example-and-verification).

## Internal API

| Member | Contract |
| --- | --- |
| `void Set(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased, bool connected)` | Copies thin points/colors or bakes joined/independent local triangles into reusable lists. Counts and finite inputs are validated by CanvasItem. |
| `void SetRect(Rect2 rect, Color color, bool filled, float width, bool antialiased)` | Normalizes dimensions, uses the existing closed strip for ordinary outlines, or bakes a fill with compensated local side/corner feathers. |
| `void SetEllipse(Vector2 center, float major, float minor, Color color, bool antialiased)` | Bakes a 64-segment fan and optional local alpha ring. |
| `void Append(List<CanvasVertex> output, Transform transform, Color modulation, bool snap)` | Transforms/modulates retained triangles; expands thin lines to one framebuffer pixel on replay. |
| `static void AppendLine(List<CanvasVertex> output, Vector2 from, Vector2 to, Color color, float width, bool antialiased, Transform transform, bool snap)` | Shared direct DrawLine and positive-width independent-segment tessellation. |
| `static void AppendThinLine(List<CanvasVertex> output, CanvasVertex a, CanvasVertex b, bool snap)` | Expands transformed endpoints and only then snaps final corners. |

## Flow, invariants and limits

Connected positive strokes build shared bisector joins with miter factors clamped to three. A repeated approximate endpoint closes the strip; adjacent nonzero directions handle repeated points. Body, left feather and right feather retain submission order. An extra end-corner vertex reverses the feather diagonal to avoid a transparent seam. Independent pairs use the shared line path. Empty polyline colors use white, missing colors repeat the last entry, and independent colors belong to segments.

Positive antialiased widths compensate the core and add a local 1.25-unit feather. Thin polyline/multiline paths ignore antialiasing and retain copied endpoint colors. Direct DrawLine can still feather negative widths. Rectangle outlines at least as wide as either dimension become expanded fills, even from zero-area input. Filled AA rectangles shrink by 0.3125 local units and add separate side/corner quads; small adjusted cores scale the 1.25-unit border, while negative adjusted sizes are preserved. Filled ellipses use a 64-segment fan with an optional compensated local ring. See the [public width contract](CanvasItem.md#stroke-recording-contract).

Local overflow throws ArgumentException before command commit. Transformed or modulated overflow throws InvalidOperationException during replay; the caller discards the incomplete frame. Failed recording does not reference a partially changed unused pool slot. Capacity growth allocates; warmed recording and replay reuse all lists. Thin rasterization uses triangles and does not yet guarantee exact native hardware line coverage. No texture is owned or retained here; materials are selected by the existing CanvasItem submission path.

## Verification

[CanvasStrokeTests](../../tests/Electron2D.Tests/CanvasStrokeTests.cs) covers geometry, attributes, guards and allocation. [CanvasStrokeRenderingTests](../../tests/Electron2D.Tests/CanvasStrokeRenderingTests.cs) covers native pixels and frame retention on the currently tested Linux backends. Other platforms and owner visual acceptance remain unverified.

## Low-level primitive integration

Raw multiline input can provide one color per endpoint. Negative-width lines retain endpoint attributes; wide/feather triangles interpolate endpoint colors using widened finite arithmetic. Existing segment-color and connected-strip consumers keep their policy. See [the executable primitive contract](../components/canvas-rendering.md#low-level-primitive-commands).
