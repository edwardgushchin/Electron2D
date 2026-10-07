# RenderingCanvasRuntime

Last updated: 2026-10-07

- Visibility: internal
- Source: [RenderingCanvasRuntime.cs](../../src/Servers/Rendering/RenderingCanvasRuntime.cs)
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Description

Retained owned or weak borrowed canvas state, per-viewport attachment overrides, canvas tint and root mirroring.

## Verification

[RenderingCanvasTests](../../tests/Electron2D.Tests/RenderingCanvasTests.cs) exercises actual projections, native replay and cleanup. Scope and limits are recorded in [the canvas contract](../components/canvas-rendering.md#caller-owned-canvases-and-items).
