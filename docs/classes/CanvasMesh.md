# CanvasMesh

Last updated: 2026-10-25

Ordinary mesh batches require matching primitive topology and native-line snapping policy before merging with adjacent canvas ranges.

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

## Writable instance consumer

Exported/indirect MultiMesh storage selects a retained GPU consumer. CanvasMesh captures source vertices/indices, palette deformation, LOD metadata and finite authored canvas state. CanvasMultiMesh retains manual/authored bounds and resource guards. GPUCanvasBackend owns conversion outputs, native index/LOD buffers and indirect commands; the private kernel interpolates raw device records and chooses each instance's LOD. CanvasVertexProgram snaps ordinary instance positions before the authored stage and procedural homogeneous output after it; built-in instance vertices support snapping and group screen UVs. Group color premultiplication, masks and original vertex IDs remain connected. Relaxed Android devices bind each LOD instance separately when nonzero indirect first-instance support is unavailable. See [ownership and Linux-native verification limits](../components/meshes.md#writable-instance-device-buffers).

## Particle mesh templates

[CanvasMesh.Particles.cs](../../src/Servers/Rendering/CanvasMesh.Particles.cs) prepares ordinary mesh surface templates for retained GPU particle batches. It preserves topology, indexed/procedural vertex identity, materials, UV/custom channels and all four/eight bone influences, validates trail indices and omits empty tessellation. Particle-specific native streams belong to the GPU renderer; cached geometry metadata is described in [CanvasParticles](CanvasParticles.md).

Batch creation resolves the item glyph policy. ApplyGlyph stamps complete parameters onto mesh/native-instance/particle template ranges before batching or GPU expansion. AddBatch compares complete glyph policy so plain and encoded ranges cannot merge incorrectly. Explicit glyph geometry and untextured meshes retain their existing paths. See [retained distance-field mode](../components/canvas-rendering.md#retained-distance-field-mode).

Mesh batches require the same private normal-map flags before merging with an adjacent range. Attribute/mesh drawing itself uses zero rectangle flags, preserving its local-normal sampling convention. See [the connected normal producer](../components/visual-shaders.md#fresnel-and-implicit-canvas-normals).

[Mesh graph inputs](../components/visual-shaders.md#mesh-channels-and-logical-instance-inputs) retain decoded custom channels and exact logical instance IDs across hardware streams, CPU-expanded snapping and sorted GPU particles. The private CanvasVertex record is 172 bytes, with uint2 identities at byte 164/input location 15; glyph fields retain their existing offsets. Native and allocation verification is scoped by that contract.

Batch coalescing compares TextureRegion so a preceding textured rectangle cannot supply its source region to attribute geometry using the same material and image. See [canvas command metadata](../components/canvas-command-metadata.md) for semantics and verification.

## Canvas point integration

See [canvas point programs](../components/canvas-points.md) for native topology, the shared sized-point device capability and cold program validation. Authored sizes are preserved after the one-pixel default initialization; optional capabilities retain explicit rejection and device fallback.
