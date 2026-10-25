# Canvas fragment surfaces

Last updated: 2026-10-25

[ADR 0028](../decisions/rendering.md#adr-0028) connects existing fragment output ports to automatic and authored scene-light composition. This extends the offline VisualShader import path and retained ShaderMaterial programs.

## Selection

| Candidate | API value and dependent families | Backend feasibility |
| --- | --- | --- |
| Fragment surfaces | Five graph/material/input/expression/custom families; normal maps and displaced lighting/shadows | Selected: original model/canvas snapshots and native light composition already exist. |
| Point size/coordinates | Four output/input/expression/custom families; procedural point sprites | At selection, required native point-size output and a coordinate/capability policy. Subsequently implemented in [canvas points](canvas-points.md). |
| Shared texture views | RDTextureView, RenderingDevice, TextureRD and TextureArrayRD; alternate formats and subresources | Requires native shared-view lifetime and writable alias synchronization, beyond existing initial component maps. |

## Authoring and surface behavior

Canvas fragment output node zero accepts these existing ports:

| Port | Value | Result |
| --- | --- | --- |
| 2 | Vector3 Normal | Direct local normal; later normal-map output takes precedence. |
| 3 | Vector3 Normal Map | Encoded normal map, decoded by `(2R-1, 1-2G, B)`. |
| 4 | float Normal Map Depth | Linear blend from `(0,0,1)` to the decoded map; default one, with no clamp. Depth alone does not enable a normal map. |
| 5 | Vector3 Light Vertex | Framebuffer-pixel XY and height used by point mask/direction and authored light input. Starts at fragment XY with zero height. |
| 6 | Vector2 Shadow Vertex | Independent framebuffer-pixel origin for the retained shadow ray. Starts at fragment XY. |

Expressions and global helpers use the same `NORMAL`, `NORMAL_MAP`, `NORMAL_MAP_DEPTH`, `LIGHT_VERTEX` and `SHADOW_VERTEX` values. Fragment initialization runs before custom setup. Missing direct normals use the command normal texture, effective rectangle swap/flips and reconstructed nonnegative Z when normal use is active; a depth-only graph activates that path only when the actual command has a normal channel. The existing flat fallback remains authoritative. Map output overrides the direct normal after the fragment function returns. Early return still reaches finalization and composition; discard retires that fragment.

After decoding/blending, multiply normal XY first by the independently normalized original model/draw basis columns, then the independently normalized canvas basis columns; normalize the resulting XYZ once. This preserves nonuniform scale/skew order. DrawMesh local transforms participate. MultiMesh poses, skin bones and particle/trail poses affect geometry while the fragment normal retains the original draw basis. Translation never affects normals. Zero basis columns remain finite zero vectors.

The completed surface feeds both automatic lighting and the real authored Light function. Each light receives the same finalized normal and light vertex; local Light-function writes do not leak to following lights. Zero lights retain fragment color, and authored lights retain their established zero-energy behavior. Shadow filtering keeps the existing contour-based adaptation; this change does not claim reference shadow-atlas parity.

## Retained implementation

Two private float4 renderer fields, `NORMAL_MODEL_BASIS` and `NORMAL_CANVAS_BASIS`, use the existing fragment uniform buffers. Their layout is `(X.x,X.y,Y.x,Y.y)` after column normalization. They require canvas-fragment non-array float32 vectors, cannot occupy Globals/Instance or sampled-image roles, and are absent from user material descriptors/defaults. Immutable CanvasMatrices snapshots and batch keys retain their original model identity without per-frame allocation. The fourth NORMAL_MAP_FLAGS component indicates an actual normal channel only for surface programs; existing ordinary graph bytes and interfaces stay unchanged.

A version-one private SPIR-V record identifies a no-argument void surface finalizer and a private structure containing vec3 normal, vec3 map, float depth, bool normal-used, vec3 light vertex and vec2 shadow vertex. The shared parser checks canonical live IDs, exact field/signature roles and one fragment-entry marker call. Unlit native code removes the marker; lit composition invokes the same finalizer before the real light loop. This consumes no extra varying location, storage buffer or native descriptor set and preserves the four-location matrix-varying budget. Reload replaces the retained program atomically.

## Evidence and limits

[Public authoring](../../tests/VisualShader.Consumer/Surface.Author.cs) and [render](../../tests/VisualShader.Consumer/Surface.Render.cs) fixtures create 22 programs and 190 geometry probes: rectangles, meshes, CPU/GPU and device-written MultiMesh, skinned and procedural geometry, local/world-coordinate GPU particles and particle trails. Twelve observed phases cover one/two/zero lights, point lighting and shadows, canvas skew, reload, zero energy and live depth parameters and normal-channel removal/restoration. Tests include map precedence, zero/negative/greater-than-one depth, depth without a map, normal texture flipping, global helpers, custom setup, early return, discard and a full four-location matrix varying. Procedural vertices use the existing local-position contract.

CLR and strict full-trim NativeAOT software Vulkan pass Wayland, X11 and HDR/4xMSAA, including the independently compiled modern source programs, with 64 prepared active and 64 idle frames at zero owner/all-thread managed bytes. Independent validation passes 726 unlit/channel/mask modules in each of three corpora; the modern corpora independently compile normal/matrix/light-vertex programs as SPIR-V 1.3/1.5; the other 19 programs retain their original module version. Nine malformed surface records reject atomically; debug-name-free external modules compose. [Ownership checks](../../tests/Electron2D.Tests/CanvasSurfaceTests.cs) cover reserved material/global/graph names, invalid field shapes/roles, atomic rejection, instance separation and degenerate bases. The 3,385 existing ordinary graph source/interface hashes and 561 existing authored-light native module hashes remain identical.

Pinned comparison evidence uses commit `ed1daf0bf001b61586d9930840f2f1394092c079`: `servers/rendering/renderer_rd/shaders/canvas.glsl` lines 638–714 for normal/map order and separate vertices, and `renderer_canvas_render_rd.cpp` lines 686–690 and 1824–1827 for independent canvas columns and NORMAL_MAP activation. The GLES shader/renderer confirms the same roles. The five owning graph families have no deprecated type marker in the pinned XML snapshot; no public declaration or deprecated alias is added. Source URLs and SHA-256 hashes are retained in the ignored `reference.json` receipt.

Initial failures were fixture errors: a negative rectangle incorrectly moved its origin, a procedural shader applied transforms twice, a support-resource directory was absent, and the rejection helper omitted InvalidDataException. Their logs remain with the source-scoped receipts in ignored `bin/canvas-surface-outputs/2026-10-25/`. Physical GPU, native heap, foreign platforms, large-scene performance, editor and owner visual acceptance retain separate gates. VisualShaderNodeOutput remains Partial for point size; this behavior slice does not advance a class count.
