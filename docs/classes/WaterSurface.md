# WaterSurface

Last updated: 2026-10-09

**Namespace:** `Electron2D.Examples.WaterPlayground`. **Declaration:** `internal sealed class WaterSurface`. **Source:** [WaterSurface.cs](../../examples/WaterPlayground/WaterSurface.cs). **Component:** [Water playground](../components/water-playground.md).

Reconstructs one continuous transparent view from released particle positions. Retained density and scratch grids use a four-unit spacing; bilinear particle splats preserve relative density before separable smoothing. Clipped cell triangles retain free-surface holes and splashes, while full interior rows merge into larger strips. Vertex colors feather the contour and tint depth without accumulating overlapping particle opacity. The view bridges submerged toy silhouettes so foreground water remains visible over solid objects.

`Update` rebuilds retained triangle lists once per physics tick. `Vertices` and `Colors` are borrowed spans for public RenderingServer.CanvasItemAddTriangleArray. `Sample` and `Submerged` constrain decorative swimming fish to the current wet field; these methods do not alter the liquid solver. WaterPlaygroundTests checks a continuous basin and native rendered captures on the GPU and compatibility canvas paths.
