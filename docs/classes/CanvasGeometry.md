# CanvasGeometry

Last updated: 2026-09-22

- Declaration: `internal static class CanvasGeometry`
- Source: [CanvasGeometry.cs](../../src/Servers/Rendering/CanvasGeometry.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

Tessellates retained rectangles, outlines, lines and textured regions into triangles. It owns no persistent state. The caller supplies a reusable output list, the composed local-to-framebuffer transform and inherited modulation. Temporary corners/cuts use stack storage; warmed replay reuses list capacity.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
CanvasGeometry.Append(vertices, command, viewportTransform * command.Transform, inheritedColor);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static void Append(List<CanvasVertex> output, CanvasCommand command, Transform transform, Color modulation)` | [Append](#append) |

## Member descriptions

### Append

`internal static void Append(List<CanvasVertex> output, CanvasCommand command, Transform transform, Color modulation)`

Appends geometry without clearing prior output. Applies color multiplication, flat line caps, centered outlines, optional one-pixel alpha feathering and texture UV transforms. Texture clipping subdivides half-texel borders to preserve interior interpolation. Zero-area primitives and quads below the 0.000001 cross-product threshold contribute nothing. Nonfinite transformed coordinates, modulation or UVs throw InvalidOperationException. Missing/disposed texture pixels fail explicitly. On failure the caller must discard the incomplete frame; this helper is not a transactional list append.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees.
