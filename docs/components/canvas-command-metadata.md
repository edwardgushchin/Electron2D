# Canvas command metadata

Last updated: 2026-10-25

[ADR 0028](../decisions/rendering.md#adr-0028) connects retained command regions and specular values to the existing offline graph/material pipeline. No public declaration or native descriptor set is added.

## Selection

| Candidate | User API value and dependent families | Current backend feasibility |
| --- | --- | --- |
| Command region/specular metadata | Five input, expression, global-helper, custom-provider and material families; texture-aware effects across retained geometry | Selected: command source rectangles, channel images and scene-light factors already have real producers. |
| Point size/coordinates | Four input/output/expression/custom families; procedural point sprites | Native composition currently fixes point size to one; portable raster size and coordinate capability policy must be implemented together. |
| Particle attractor force | Three particle input/expression/custom families; force-dependent particle motion | Requires retained attractor producers, force accumulation and compute-frame transport. A constant input would not implement that dependency. |

## Executed values

Canvas fragment `region_rect` is a Vector4 input, also available as read-only `REGION_RECT` to expressions and global/custom helpers. Rectangle and nine-patch commands expose the complete normalized source rectangle before tessellation. For rectangles, negative source and destination extents change the corresponding size signs; transposition changes sampling without reordering these four fields. Nine-patch pieces keep the original signed full source rectangle even when the destination geometry is reflected. Attribute geometry, including meshes, CPU/GPU MultiMesh and particles/trails, exposes `(0,0,logical image width,logical image height)` in pixels. A missing image uses unit dimensions.

High-level atlas drawing resolves its source region against the backing image. An AtlasTexture RID denotes that backing image; low-level drawing with that RID and an empty source therefore uses the complete backing image. StyleBoxTexture atlas nine-patches retain their resolved region.

Canvas fragment `specular_shininess` is a Vector4 input, also available as read-only `SPECULAR_SHININESS`. Before custom setup or fragment expressions run, sample the current specular image at the initial UV and multiply RGB by CanvasTexture.SpecularColor and alpha by SpecularShininess. Missing channels use white and shininess one. HDR follows the existing linear RGB factor policy; finite negative and greater-than-one factors remain unclamped. Later UV writes do not resample this value. Authored Light invocation keeps its existing separately reset specular state; shared helpers observe the correct state in either call.

The specular channel has the existing scalar float-sampled 2D image contract, including floating device textures. Signed/unsigned device images reject before publication; automatic lighting's separate integer channel variants do not change that graph sampler contract. The sampler uses the existing sixteen-image budget and adds no hidden native binding. Out-of-stage input nodes retain their scalar-zero role; expressions require an applicable stage and both fragment inputs reject writes during offline compilation.

## Ownership and transport

`REGION_RECT` and `CANVAS_SPECULAR` are optional non-array float32 vec4 fields in ordinary canvas-fragment renderer buffers. CANVAS_SPECULAR carries the color/shininess multiplier for ordinary GLSL/HLSL shaders; they sample the existing SPECULAR_SHININESS_TEXTURE themselves. Both fields are excluded from material descriptors, texture defaults, Globals and Instance storage. Invalid shapes, stages or image roles reject before replacing a program. Material copies retain user parameters and clear transient renderer bytes.

Only consuming retained batch keys include the captured region, preventing coalescing across different rectangles or mesh fallback values. Nonconsuming programs retain their previous keys. Existing draw/mesh/channel ownership and resource lifetime remain unchanged. Reload publishes an immutable replacement program. The specular cache uses private fragment state and a read-only getter, initialized before custom setup; shared Light state is reused when present.

## Verification and limits

[Public authoring](../../tests/VisualShader.Consumer/CommandMetadata.Author.cs), [render](../../tests/VisualShader.Consumer/CommandMetadata.Render.cs) and [ownership](../../tests/Electron2D.Tests/CanvasCommandMetadataTests.cs) fixtures cover eight graphs and ordinary GLSL/HLSL fragments. The corpus has 190 geometry/channel observations per phase, with all four components, flips, transpose, tiling, nested atlases, both nine-patch paths, CPU/GPU/device-written instancing, particles/trails and floating device channels. Sixteen phases change factors, channel presence, material components, light count and canvas transforms, and reload programs. Prepared active and idle intervals each observe 64 frames and separate owner/all-thread managed bytes. Typed signed/unsigned sampler rejection has its own successful-cleanup modes.

CLR and strict full-trim NativeAOT software Vulkan pass Wayland, X11, HDR/4xMSAA and both modern source corpora, including separate integer-sampler rejection modes. Every positive profile passes all sixteen phases and both zero-owner/all-thread-byte intervals. The 3,385 preceding ordinary graph source/interface hashes, 726 surface modules and 561 authored-light modules remain identical.

Independent spirv-val validation passes 330 unlit/channel/mask modules in each of three corpora. The modern corpora independently compile the region-node, global-helper and Light graphs as SPIR-V 1.3/1.5; the other seven programs retain their original version. Three invalid graph imports preserve the previous output SHA-256: region assignment, specular assignment and vertex-region expression. CPU checks cover symbols, notification roles, copying, renderer ownership, invalid shape/owner roles, atomic reload and source/destination flip cancellation.

Pinned comparison evidence uses commit `ed1daf0bf001b61586d9930840f2f1394092c079`: `modules/visual_shader/visual_shader.cpp` lines 3416–3419 and 3441 establish graph roles; `servers/rendering/shader_types.cpp` lines 304–307 and 341 establish read-only vectors and the float sampler. The RD canvas shader lines 574–578 and 669–681 establish rectangle/attribute regions and initial-UV sampling; the RD command producer establishes normalized signed source extents. Owning input metadata has no deprecated marker. URLs and complete source SHA-256 hashes are retained in ignored `bin/canvas-command-metadata/2026-10-25/reference.json` and `reference-core.json`.

Initial failed probes exposed fixture assumptions: low-level atlas RID region resolution, integer images for a float sampler, and loading raw SPIR-V as a resource archive. Their receipts remain alongside the corrected probes. Input stays Partial for native point roles and particle attractor force. Declaration counts are unchanged. Physical GPU, native heap, foreign platforms, large-scene performance, editor and owner visual acceptance retain separate gates.
