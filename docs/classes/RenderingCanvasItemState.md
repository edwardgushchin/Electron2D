# RenderingCanvasItemState

Last updated: 2026-10-07

- Visibility: internal
- Source: [RenderingCanvasRuntime.cs](../../src/Servers/Rendering/RenderingCanvasRuntime.cs)
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Description

Separate native parent/field/command projections, renderer ownership and weak extra child links; scene authoring remains separate.

## Verification

[RenderingCanvasTests](../../tests/Electron2D.Tests/RenderingCanvasTests.cs) exercises actual projections, native replay and cleanup. Scope and limits are recorded in [the canvas contract](../components/canvas-rendering.md#caller-owned-canvases-and-items).
