# CanvasCommand

Last updated: 2026-09-23

- Declaration: `internal readonly record struct CanvasCommand`
- Source: [CanvasGeometry.cs](../../src/Servers/Rendering/CanvasGeometry.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

A retained local drawing operation recorded during CanvasItem NotificationDraw/Draw/OnDraw. CanvasItem owns its list, clears it before redraw, and releases references on disposal. Material selection is captured later by CanvasItem.AppendCanvas; commands borrow their Texture and never dispose it. Pixel updates are consumed on replay without recording new commands.

## Member summary

| Declaration | Contract |
| --- | --- |
| `CanvasCommand(bool Line, Vector2 A, Vector2 B, Color Color, float Width, bool Antialiased, Transform Transform, Texture? Texture = null, Rect2 Source = default, bool Transpose = false, bool ClipUV = false, bool Tile = false, CanvasPolygon? Polygon = null, CanvasStroke? Stroke = null, bool SetTransform = false, CanvasAnimationSlice? AnimationSlice = null)` | [Construction and values](#construction-and-values) |

## Member descriptions

### Construction and values

`CanvasCommand(bool Line, Vector2 A, Vector2 B, Color Color, float Width, bool Antialiased, Transform Transform, Texture? Texture = null, Rect2 Source = default, bool Transpose = false, bool ClipUV = false, bool Tile = false, CanvasPolygon? Polygon = null, CanvasStroke? Stroke = null, bool SetTransform = false, CanvasAnimationSlice? AnimationSlice = null)`

For lines A/B are endpoints; for textures they are destination position/size. Rectangles live in the Stroke buffer. Width is local units when positive and one framebuffer pixel when negative; zero-width ordinary outlines/lines contribute nothing. Transform carries an ordered state value when SetTransform is true; geometry commands store identity. CanvasItem evaluates AnimationSlice before skipping other commands, then applies visible SetTransform commands. Geometry receives the resulting composed transform. Each replay starts unrestricted at identity. A texture command stores normalized Source coordinates, destination sign flips, transpose, clipping and tiling flags. Polygon and Stroke optionally reference item-owned retained geometry, taking precedence over line fields. Each committed command has a distinct reusable pool slot; redraw resets its cursor. Constructor arguments become record properties. Recording validates geometry and color; replay additionally validates transformations and referenced pixels.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees.
