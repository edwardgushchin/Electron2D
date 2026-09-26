# CanvasNinePatch

Last updated: 2026-09-26

**Declaration:** `internal readonly record struct CanvasNinePatch` · **Source:** [CanvasNinePatch.cs](../../src/Servers/Rendering/CanvasNinePatch.cs)

A retained value payload containing border vectors, independent axis modes and center flag. CanvasItem records it beside the borrowed texture, destination and raw source region. Submission resolves current texture/atlas dimensions, constructs piecewise affine axis segments and appends triangles through existing CanvasGeometry texture logic. Constant-source centers remain valid. Region flips, inherited sampling/material/blend/clip and source-start interpolation adjustment preserve native pixel checks.

Private axis/segment values live on the stack; output lists are reused after capacity preparation. Tile-count ceilings reject before output expansion; CPU tessellation remains proportional to repeat count, with a common shader path as the measured-cost upgrade. This helper exposes no public API or resource ownership. [NinePatchTests](../../tests/Electron2D.Tests/NinePatchTests.cs) checks an independent shader-equation oracle; [native tests](../../tests/Electron2D.Tests/NinePatchRenderingTests.cs) validate both backends and warmed resized frames. Limits remain in [ADR 0079](../decisions/rendering.md#adr-0079).
