# RenderingServerCanvasItem

Last updated: 2026-10-07

- Visibility: internal
- Source: [CanvasItem.Rendering.cs](../../src/Scene/Main/CanvasItem.Rendering.cs)
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Description

Private detached Entity command recorder owned by the renderer; scene processing and public scene factories are not involved.

## Verification

[RenderingCanvasTests](../../tests/Electron2D.Tests/RenderingCanvasTests.cs) exercises actual projections, native replay and cleanup. Scope and limits are recorded in [the canvas contract](../components/canvas-rendering.md#caller-owned-canvases-and-items).
