# WaterSurface

Last updated: 2026-10-09

**Namespace:** `Electron2D.Examples.WaterPlayground`. **Declaration:** `internal sealed class WaterSurface`. **Source:** [WaterSurface.cs](../../examples/WaterPlayground/WaterSurface.cs). **Component:** [Water playground](../components/water-playground.md).

Reconstructs one continuous transparent view from released particle positions. Retained density and scratch grids use a four-unit spacing and cover the current entry plane through the basin bottom, including negative world Y; bilinear particle splats preserve relative density before separable smoothing. Clipped cell triangles retain free-surface holes and splashes, while full interior rows merge into larger strips. The clipped silhouette has a sharp edge; vertex tint and opacity depend on depth, not on sparse edge density. This avoids the former wide transparent fringe without accumulating overlapping particle opacity. The mesh uses only solved particle density. No per-body waterline, collider fill, harmonic extension or foreground silhouette overlay is added.

`Update` rebuilds retained triangle lists once per physics tick and when the view changes, including during pause. Grid capacity grows only when a taller view needs it; only active rows are processed. `Vertices` and `Colors` are borrowed spans for public RenderingServer.CanvasItemAddTriangleArray. `Sample` reads the reconstructed density for continuity checks. WaterPlaygroundTests checks a continuous basin and native rendered captures on the GPU and compatibility canvas paths.
