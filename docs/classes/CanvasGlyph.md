# CanvasGlyph

Last updated: 2026-10-14

- Declaration: `internal readonly record struct CanvasGlyph(CanvasGlyphMode Mode, float Outline = 0, float PixelRange = 4, float Scale = 1)`
- Source: [CanvasGlyph.cs](../../src/Servers/Rendering/CanvasGlyph.cs)
- Component: [canvas rendering](../components/canvas-rendering.md#lcd-and-msdf-texture-regions)
- Visibility: internal.

A copied recording/batch value for encoded glyph texture sampling. Parameters returns Vector4(mode, pixelRange, outline/scale/4, 0); Validate rejects nonfinite outline, nonpositive/nonfinite range or scale and nonfinite derived parameters. Default CanvasGlyph means None and is never decoded. The explicit LCD/MSDF constructor defaults retain finite range/scale. It owns no resource or native identity. Scene/server tests cover guard failures, outline truncation and scale conversion; the native shader tests cover median/outline and component coverage.
