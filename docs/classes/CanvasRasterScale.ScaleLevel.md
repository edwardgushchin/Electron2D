# CanvasRasterScale.ScaleLevel

Last updated: 2026-10-20

- **Namespace:** `Electron2D`
- **Visibility:** Internal
- **Source:** [CanvasRasterScale.cs](../../src/Scene/Resources/CanvasRasterScale.cs)

Internal lifetime generation for one active quantized raster factor. Counts viewport/item owners and holds weak SVG/font registrations. This identity prevents stale retirement from releasing a newer generation.

[Scale ownership](../components/scalable-svg.md#item-scale-oversampling) describes consumers and verification limits.
