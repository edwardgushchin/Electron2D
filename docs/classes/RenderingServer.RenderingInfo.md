# RenderingServer.RenderingInfo

Last updated: 2026-10-27

**Declaration:** `public enum RenderingServer.RenderingInfo` · **Source:** [render statistics](../../src/Servers/Rendering/RenderingServer.RenderInfo.cs).

TotalObjectsInFrame=0, TotalPrimitivesInFrame=1 and TotalDrawCallsInFrame=2 aggregate the last completed renderer frame. PipelineCompilationsCanvas=6 counts successful canvas-owned native graphics-pipeline creations cumulatively during the active renderer session. Cache hits and retirement preserve its count; completed closure returns zero and a new session resets it. Compatibility has no engine-owned GPU pipeline objects and reports zero. TextureMemUsed=3, BufferMemUsed=4 and VideoMemUsed=5 read the actual native allocation sources; see [native render memory](../components/render-memory.md). Other pipeline sources remain open.

[Completed canvas counters](../components/canvas-counters.md) defines separate submission/pipeline lifetimes, publication, native granularity, indirect capacity budgets, lifetime and verification. Use RenderingServer.GetRenderingInfo or the matching Performance monitors.
