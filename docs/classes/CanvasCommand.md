# CanvasCommand

Last updated: 2026-09-22

- Declaration: `internal readonly record struct CanvasCommand`
- Source: [CanvasGeometry.cs](../../src/Servers/Rendering/CanvasGeometry.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

A retained local drawing operation recorded only during Node.OnDraw. Node owns its list, clears it before redraw, and releases references on disposal. Material selection is captured later by Node.AppendCanvas; commands borrow their Texture and never dispose it. Pixel updates are consumed on replay without recording new commands.

## Member summary

| Declaration | Contract |
| --- | --- |
| `CanvasCommand(bool Line, Vector2 A, Vector2 B, Color Color, bool Filled, float Width, bool Antialiased, Transform Transform, Texture? Texture = null, Rect Source = default, bool Transpose = false, bool ClipUV = false, bool Tile = false)` | [Construction and values](#construction-and-values) |

## Member descriptions

### Construction and values

`CanvasCommand(bool Line, Vector2 A, Vector2 B, Color Color, bool Filled, float Width, bool Antialiased, Transform Transform, Texture? Texture = null, Rect Source = default, bool Transpose = false, bool ClipUV = false, bool Tile = false)`

For lines A/B are endpoints; for rectangles they are position/size. Width is local units when positive and one framebuffer pixel when negative; zero-width outlines/lines contribute nothing. Transform is the extra local drawing transform. A texture command stores normalized Source coordinates, destination sign flips, transpose, clipping and tiling flags. Constructor arguments become record properties. Recording validates geometry and color; replay additionally validates transformations and referenced pixels.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees.
