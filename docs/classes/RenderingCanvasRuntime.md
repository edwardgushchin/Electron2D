# RenderingCanvasRuntime

Last updated: 2026-10-07

- Visibility: internal
- Source: [RenderingCanvasRuntime.cs](../../src/Servers/Rendering/RenderingCanvasRuntime.cs)
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Description

Retained owned or weak borrowed canvas state, per-viewport attachment overrides, canvas tint and root mirroring.

## Verification

[RenderingCanvasTests](../../tests/Electron2D.Tests/RenderingCanvasTests.cs) exercises actual projections, native replay and cleanup. Scope and limits are recorded in [the canvas contract](../components/canvas-rendering.md#caller-owned-canvases-and-items).

## Viewport world integration

[Canvas and physics worlds](../components/worlds.md) documents World.Canvas, Viewport.World/FindWorld, nearest-viewport CanvasItem access, shared rendering, independent physics, membership changes and runtime lifetime. Existing server and native kernels remain the implementation path. [WorldTests](../../tests/Electron2D.Tests/WorldTests.cs) supplies direct behavior and actual target-pixel evidence.
