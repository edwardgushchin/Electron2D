# CanvasGeometry

Last updated: 2026-10-25

- Declaration: `internal static class CanvasGeometry`
- Source: [CanvasGeometry.cs](../../src/Servers/Rendering/CanvasGeometry.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

Prepares retained lines and textured regions and dispatches polygons/strokes, including rectangles, to their storage. Thin strokes return raw transformed endpoints with native topology; other geometry remains triangles. CanvasItem consumes ordered interval/transform state before this helper. It owns no persistent state. The caller supplies a reusable output list, the composed local-to-framebuffer transform and inherited modulation. Temporary corners/cuts use stack storage; warmed replay reuses list capacity.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
CanvasGeometry.Append(vertices, command, viewportTransform * drawingTransform, inheritedColor);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static (int Count, Mesh.PrimitiveType Primitive) Append(List<CanvasVertex> output, CanvasCommand command, Transform transform, Color modulation, bool snapVertices = false, CanvasItem? skinOwner = null, bool linear = false, BlendMode blend = BlendMode.Mix)` | [Append](#append) |

## Member descriptions

### Append

`internal static (int Count, Mesh.PrimitiveType Primitive) Append(List<CanvasVertex> output, CanvasCommand command, Transform transform, Color modulation, bool snapVertices = false, CanvasItem? skinOwner = null, bool linear = false, BlendMode blend = BlendMode.Mix)`

Appends geometry without clearing prior output. Returns the native prefix count/topology; trailing vertices remain triangles for optional line feathers. Zero count means there is no native prefix. Applies color multiplication, local line feathering through CanvasStroke and texture UV transforms. Retained strokes provide rectangle fills/outlines and their local feathers. Texture clipping subdivides half-texel borders to preserve interior interpolation. Texture quads below the 0.000001 cross-product threshold contribute nothing; degenerate retained stroke triangles are left to the backend. CanvasStroke shares straight-line geometry with independent segments; pooled curves replay their cached local triangles. Nonfinite transformed coordinates, modulation or UVs throw InvalidOperationException. Missing/disposed texture pixels fail explicitly. A command without geometry throws InvalidOperationException; interval/transform state must be consumed by CanvasItem. On failure the caller must discard the incomplete frame; this helper is not a transactional list append.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees.

## Pixel snapping

`internal static Vector2 Snap(Vector2 point)` computes floor(point + 0.5), including negative half values. Append can snap final primitive corners after transformation. Texture UV clipping cuts are then interpolated on the two snapped triangles, with cells split at their diagonal; rounding those artificial cuts would distort UVs. This also handles rounded quadrilaterals that cease to be parallelograms. Normalized UVs receive the 0.00001 precision offset before source-border clamping. Collapsed texture triangles are skipped. Positive-width line/outline/antialias triangles snap after their normal tessellation. Thin native endpoints remain unsnapped in this shared list; GPU upload and native SDL untextured uniform-color lines snap endpoints; attribute-bearing compatibility ranges expand first and snap final corners. No heap scratch storage is introduced.

CanvasPixelSnapTests checks the exact corners, diagonal/interior interpolation, summed area, clipped UV bounds, negative ties, lines/outlines/antialiasing, transpose/reflection and zero warmed allocations. Native checks belong to the canvas component.

Offscreen canvases now use independently owned completed/write target pairs. Native caches span every selected target in one frame. Viewport textures use their native image and dimensions directly; GetImage readback remains an explicit cold operation. See [offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets).

Polygon replay forwards the selected blend policy to its optional boundary producer. PremultAlpha fringe vertices fade RGBA together; source-alpha modes fade alpha while preserving RGB/UV. Multiply blending ignores coverage alpha and explicitly rejects antialiased polygon submission. Ordinary polygon/primitive/stroke replay is unchanged. See [boundary geometry](CanvasPolygon.md#polygon-boundary-coverage).

Append returns the complete normalized source region for textured rectangle commands, with source/destination size signs and transpose preserved independently of expanded vertices. Attribute/stroke geometry returns no rectangle metadata. See [canvas command metadata](../components/canvas-command-metadata.md) for semantics and verification.
