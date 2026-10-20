# ViewportRenderInfo

Last updated: 2026-10-21

**Declaration:** `public enum ViewportRenderInfo` · **Source:** [render statistics](../../src/Scene/Main/Viewport.RenderInfo.cs).

ObjectsInFrame=0 counts submitted logical drawing records; PrimitivesInFrame=1 counts topology/instance budgets; DrawCallsInFrame=2 counts native canvas geometry calls. Max=3 is a nonselectable extent.

[Completed canvas counters](../components/canvas-counters.md) defines publication, native granularity, indirect capacity budgets, lifetime and verification. Viewport and RenderingServer share this single enum identity under ADR 0051.
