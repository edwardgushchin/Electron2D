# Authored canvas light graphs

Last updated: 2026-10-25

[ADR 0028](../decisions/rendering.md#adr-0028) extends the existing offline visual-shader importer and GPU scene-light compositor with `VisualShader.Type.Light` and `VisualShader.VaryingMode.FragmentToLight`. No runtime source compiler or new public backend handle is introduced.

## Selection and scope

| Candidate | User capability and dependent families | Current backend feasibility |
| --- | --- | --- |
| Authored canvas lights | Five graph/input/output/varying families; custom per-light materials, fragment carry, existing images and parameter scopes | Selected: the retained compositor already supplies real point/directional lights, masks, normal/specular channels and shadows. |
| Inherited scene-root authoring | PackedScene, SceneState and Node; root templates, inherited overrides and rebinding | Root authorship first needs the editor's inherited-state/instance-mode metadata lifecycle. Runtime instantiation does not itself designate a new inherited root. |
| Particle attractor input | Particle graph input plus emitter/force integrations | Requires an actual attractor producer, retained force accumulation and compute-frame transport. Constant input would not establish the capability. |

The selected slice completes authored per-light execution, including source/compiled resources, typed copying, import rejection, retained program reload, material/global/Instance values, images and compiled custom providers. [Fragment surface outputs](canvas-surfaces.md) now supply normal/map/depth and independent light/shadow positions. [Point builtins](canvas-points.md) and [fragment region/specular command metadata](canvas-command-metadata.md) now execute; particle attractor forces retain their producer dependency.

## Public workflow

Author the canvas fragment normally, then connect light output node zero. Its ports are RGB (`Vector3`), alpha (`Scalar`) and RGB shadow modulation (`Vector3`) at indices 0, 1 and 2. The light contribution begins at transparent black; shadow modulation begins at opaque white. An authored light function replaces that light's automatic contribution, so it must explicitly write the required RGB/alpha. Unconnected light output ports preserve the current function value.

```csharp
using var graph = new VisualShader();
using var rgb = new VisualShaderNodeVec3Constant { Constant = new(.25f, .5f, .75f) };
graph.AddNode(VisualShader.Type.Light, rgb, default, 2);
graph.ConnectNodes(VisualShader.Type.Light, 2, 0, 0, 0);
graph.GetNode(VisualShader.Type.Light, 0).SetInputPortDefaultValue(1, 1f);
ResourceSaver.Save(graph, "res://light.e2dres", SaverFlags.BundleResources);
```

Import with the existing tool (`ShaderImport light.e2dres graph light.compiled.e2dres --visual-shader`), load the compiled resource and assign it to `ShaderMaterial.Shader`. Source edits retain the last imported program until successful reimport. Mode changes retain inactive light definitions. The compatibility backend keeps its explicit shader-material refusal.

A Light stage becomes active through a reachable output assignment, side-effect root, or nonempty custom `GetFuncCode(CanvasItem, Light)`. The importer calls this callback for reachable providers even when their node resides in another stage. Providers must return empty source for stages intended to stay inactive; nonempty Light setup creates an authored light function even if it writes no LIGHT output. Previously compiled programs remain immutable.

## Values and invocation

| Light input | Numeric role and source |
| --- | --- |
| `light` | Current RGBA light output; initially zero on each call. |
| `light_color` | Raw authored RGBA multiplied by the real point-light mask. Energy remains separate. |
| `light_energy` | Actual finite authored light energy, including zero. |
| `light_direction` | Normalized direction from the current surface toward the light. |
| `light_is_directional` | Boolean directional/point distinction. |
| `light_position` | Point endpoint plus height; zero for directional lights. |
| `light_vertex` | Current framebuffer surface XY with zero initial height. |
| `normal` | Existing transformed, sampled CanvasTexture normal. |
| `specular_shininess` | Existing sampled specular color/shininess, with the normal HDR color policy. |
| `shadow` | Current SHADOW_MODULATE RGBA; initially one, RGB output modulates the actual shadow color. |
| `color`, `uv` | Values after fragment execution, restored for every light invocation. |
| `fragcoord`, `screen_uv`, `texture_pixel_size`, `time`, `texture` | Actual renderer coordinates, logical command texture metadata, time and diffuse image. |

Light-only names have scalar-zero input-node roles outside Light. Known point producers still reject in their applicable stage. Expressions and typed input nodes use the same aliases. Private INSTANCE_CUSTOM also restarts from the completed fragment. A light's local changes do not leak to the next light. The shared loop retains actual attenuation, masks, blend modes, shadows/filtering and zero-light behavior. ShadowColor retains its existing raw color policy; its HDR interpretation was not changed.

## Varyings, shared resources and source scope

`VertexToFragmentLight` retains four native custom locations, smooth float/matrix and flat integer/Boolean transport. A numeric matrix consumes all four. `FragmentToLight` retains all eight roles privately in the fragment module with no native locations or interpolation. Initialize unwritten values to typed zero or numeric matrix identity. Getters require the declared producer or a later canvas stage; named setters require exactly the producer. Setter roots execute after the reserved output in ascending node ID with each root's dependencies first. A name alone creates no dependency.

Fragment and Light share the material/global/Instance slots, images, parameter defaults and helper declarations. Light-only buffers retain their declared slot extent in the native unlit variant even when inactive reflection drops their accesses. Shared globals compile with `ELECTRON2D_SHADER_STAGE == 1`; fragment/light function bodies use 1/2. Use `!= 0` for helpers shared by fragment and Light, including helpers reading light state. Vertex globals remain a separate module. Images retain existing ownership, fallback and sampler limits.

The portable program stores a private version-one `Electron2D:canvas_light` record identifying a no-argument void function and a private ten-field numeric state by live SPIR-V IDs. Publication validates version, canonical IDs, function signature, exact scalar/vector/Boolean fields and one fragment-entry marker call. The native unlit entry removes only that call/record. Light composition calls the retained function with actual light state, reads LIGHT and RGB shadow modulation, and preserves the ordinary material epilogue. Debug names are not runtime identities. Native resource validation uses the full program, preserving resources referenced only by Light.

## Verification and limits

The public [CPU](../../tests/VisualShader.Consumer/LightGraphs.CPU.cs), [authoring](../../tests/VisualShader.Consumer/LightGraphs.Author.cs), [probe](../../tests/VisualShader.Consumer/LightGraphs.Probes.cs) and [render](../../tests/VisualShader.Consumer/LightGraphs.Render.cs) fixtures exercise 17 programs. They cover constants, every light input role, all eight private varying types and their unwritten zero/identity defaults, a full vertex matrix, shared material/global/Instance values, sampled image/normal/specular channels, shadow modulation, restored fragment carry, early return, global helpers and registered custom procedures. Ten observed phases per profile include zero/one/two lights, live light edits, point shadows, zero energy and resource reload.

Linux software Vulkan CLR and strict full-trim NativeAOT profiles pass Wayland, X11 and HDR/4xMSAA. Builtins/context/matrix programs also execute as independently compiled SPIR-V 1.3 and 1.5. Independent `spirv-val` validates all 561 variants in each of the three corpora (17 programs times one unlit plus 32 channel/mask combinations); 14 programs in each modern corpus retain their original module version. Nine malformed private records reject atomically, and external modules with removed function/state debug names still compose. Seven current invalid graph imports (missing/mistyped declarations, wrong read/write stages, cycle, point producers and out-of-stage light expressions); the former unsupported-surface fixture now succeeds. These imports preserve the previous artifact hash. The existing aggregate graph CPU and native material-lighting regressions pass; nonconsuming generated-source regression remains separately checked.

Each exercised rendering profile measures 64 warmed active plus 64 idle frames at zero owner-thread and whole-render-interval managed bytes. This is measured managed allocation and functional software-driver output. Physical GPU, native heap, foreign-platform, large-scene performance, full inherited API/editor behavior and owner visual acceptance remain unverified. Proof receipts stay in ignored `bin/canvas-light-graphs/2026-10-25/`; generated wiki output is unpublished.
