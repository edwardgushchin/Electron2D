# CanvasMesh

Last updated: 2026-10-04

Internal retained mesh geometry in [CanvasMesh.cs](../../src/Servers/Rendering/CanvasMesh.cs), part of [canvas rendering](../components/canvas-rendering.md) and [mesh rendering](../components/meshes.md). It captures immutable or live mesh surfaces, transform/color/UV/custom instance data and primitive tessellation before appending to the existing canvas vertex/batch stream. Material, texture, filtering, repeat, clipping and native profile rules stay with that stream. Native resources belong to the renderer; this geometry owns no device handles.

Mesh batch coalescing is limited to ordinary Draw operations with equal state. It cannot merge across Copy, GroupBegin or GroupEnd, preserving the screen snapshot and attachment boundaries. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots) and [CanvasCompositionTests](../../tests/Electron2D.Tests/CanvasCompositionTests.cs). Existing mesh component/tests own the detailed primitive and surface contracts.
