# RenderingServer.RenderingInfo

Last updated: 2026-10-21

**Declaration:** `public enum RenderingServer.RenderingInfo` · **Source:** [render statistics](../../src/Servers/Rendering/RenderingServer.RenderInfo.cs).

TotalObjectsInFrame=0, TotalPrimitivesInFrame=1 and TotalDrawCallsInFrame=2 aggregate the last completed renderer frame. Memory and pipeline compilation counters require their own producers and are not exposed as constant-return fields.

[Completed canvas counters](../components/canvas-counters.md) defines publication, native granularity, indirect capacity budgets, lifetime and verification. Use RenderingServer.GetRenderingInfo or the matching Performance monitors.
