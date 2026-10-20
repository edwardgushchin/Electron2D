# ViewportRenderInfoType

Last updated: 2026-10-21

**Declaration:** `public enum ViewportRenderInfoType` · **Source:** [render statistics](../../src/Scene/Main/Viewport.RenderInfo.cs).

Canvas=2 selects 2D canvas submission. Max=3 is nonselectable. The 3D scene/shadow domains are excluded and undefined values reject.

[Completed canvas counters](../components/canvas-counters.md) defines publication, native granularity, indirect capacity budgets, lifetime and verification. Viewport and RenderingServer share this single enum identity under ADR 0051.
