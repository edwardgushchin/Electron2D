# CanvasRasterScale

Last updated: 2026-10-20

- **Namespace:** `Electron2D`
- **Visibility:** Internal
- **Source:** [CanvasRasterScale.cs](../../src/Scene/Resources/CanvasRasterScale.cs)

Internal shared quantized raster-level owners and weak SVG/font registrations. Publication is lock-protected; last-owner retirement runs outside the registry gate. Stable lookups allocate nothing.

[Scale ownership](../components/scalable-svg.md#item-scale-oversampling) describes consumers and verification limits.
