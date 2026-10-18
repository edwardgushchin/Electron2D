# CanvasMesh

Last updated: 2026-10-18

Internal retained mesh geometry in [CanvasMesh.cs](../../src/Servers/Rendering/CanvasMesh.cs), part of [canvas rendering](../components/canvas-rendering.md) and [mesh rendering](../components/meshes.md). It captures immutable or live mesh surfaces, transform/color/UV/custom instance data and primitive tessellation before appending to the existing canvas vertex/batch stream. Material, texture, filtering, repeat, clipping and native profile rules stay with that stream. Native resources belong to the renderer; this geometry owns no device handles.

Mesh batch coalescing is limited to ordinary Draw operations with equal state. It cannot merge across Copy, GroupBegin or GroupEnd, preserving the screen snapshot and attachment boundaries. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots) and [CanvasCompositionTests](../../tests/Electron2D.Tests/CanvasCompositionTests.cs). Existing mesh component/tests own the detailed primitive and surface contracts.

## Executable mesh skin integration

Replay now resolves the drawing item palette once, holds owned storage against callback reentry, validates every skin index, and applies all four/eight UNORM16 influences through item/base conjugation and raw weight sums before command-local transforms. Custom surfaces quantize copied skin channels on revision preparation.

## Low-level primitive integration

PrepareSkin now supplies one shared palette/basis/lease policy to ordinary mesh surfaces and low-level indexed triangles. AppendTriangleData uses existing vertex deformation, finite validation, color/UV capture and lease cleanup. See [the executable primitive contract](../components/canvas-rendering.md#low-level-primitive-commands).

Surface and inherited material batches retain the drawing CanvasItem as InstanceOwner when their shader declares instance fields. Hardware MultiMesh batches use the node's one parameter set. See [per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters).

## LOD preparation and replay

Private alternate views share base channel arrays and retain copied indices. The full framebuffer basis selects the largest local threshold within one pixel using its greatest singular value. Custom callbacks capture validated levels under the revision guard; MultiMesh selects independently from each presentation transform and expands alternate geometry into the ordinary stream. See [authored LOD indices](../components/meshes.md#authored-lod-indices-and-scale-selection).


Compressed surfaces use the same decoded numerical channel contract. The cold custom format callback selects the compression codec with stable-revision recovery; prepared retained replay follows the shared [compressed attribute layout](../components/meshes.md#compressed-attributes).

## Packed surface integration

Packed server surfaces supply authored local visibility rectangles. Attached skeleton palettes transform optional nullable bone rectangles through the existing mesh/skeleton basis; bounds storage stays private, and prepared queries/replay allocate no new arrays. Ordinary resource surfaces retain geometry-derived bounds.

## Authored mesh visibility

Draw bounds select a nonzero ArrayMesh.CustomAABB before palette-derived or per-surface bounds, then apply the draw-local transform. Geometric GetLocalBounds remains independent for Mesh.GetAABB. Hardware instance preparation uses the same override; whole MultiMesh bounds retain their separate precedence. See [manual mesh visibility](../components/meshes.md#manual-mesh-visibility-bounds).

## Procedural mesh integration

Procedural snapshots retain source IDs and non-position channels, transport an affine basis/origin privately and submit real native topology. Expanded instances retain separate strip batches. Custom authored visibility is captured under stable revision/reentry guards; default geometric bounds capture never recursively reads itself. See [the connected contract](../components/meshes.md#procedural-surfaces-without-positions).
