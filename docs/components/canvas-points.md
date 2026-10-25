# Canvas point programs

Last updated: 2026-10-25

Under [ADR 0028](../decisions/rendering.md#adr-0028), the existing offline graph importer executes native point sizes and coordinates through the retained canvas shader path. No new public declaration or runtime source compiler is introduced.

## Selection

| Candidate | User capability and dependent families | Current backend feasibility |
| --- | --- | --- |
| Point size/coordinates | Four input/output/expression/custom families; point sprites across primitive, mesh, instance and particle consumers | Selected: native point pipelines exist; complete graph lowering, preserve mesh topology and negotiate sized points. |
| Particle attractor force | Input, particle-process and attractor integrations | Needs a real retained force producer, accumulation and compute-frame transport. A constant cannot implement it. |
| Shared texture views | Four texture/view families and three blocked device APIs | Needs native subresource/alternate-format adapters, shared allocation ownership and alias synchronization. Copied descriptions alone do not supply them. |

## Public workflow

```csharp
using var graph = new VisualShader();
using var diameter = new VisualShaderNodeFloatConstant { Constant = 9 };
graph.AddNode(VisualShader.Type.Vertex, diameter, default, 2);
graph.ConnectNodes(VisualShader.Type.Vertex, 2, 0, 0, 4);
using var coordinate = new VisualShaderNodeInput { InputName = "point_coord" };
graph.AddNode(VisualShader.Type.Fragment, coordinate, default, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
graph.GetNode(VisualShader.Type.Fragment, 0).SetInputPortDefaultValue(1, 1f);
ResourceSaver.Save(graph, "res://point.e2dres", SaverFlags.BundleResources);
```

Import with `ShaderImport point.e2dres graph point.compiled.e2dres --visual-shader`, then load the compiled resource into `ShaderMaterial.Shader`. Draw a one-vertex `DrawPrimitive` command or a `Mesh.PrimitiveType.Points` surface. A two-vertex primitive command selects a line; use separate one-vertex commands for separate points. Mesh indices, original vertex IDs, skinning, LOD selection and instance/particle poses keep their existing order. As with authored vertex positions, shader output does not enlarge retained/group-fit bounds; provide appropriate custom bounds or group margins when needed.

| Value | Stage and type | Contract |
| --- | --- | --- |
| `point_size` / `POINT_SIZE` | Vertex, Scalar | Native point diameter in framebuffer pixels. Starts at one before custom setup or graph evaluation. Expression/custom code can read and write it. |
| Output root port 4 | Vertex, Scalar | Writes point diameter after evaluating its inputs, including stored defaults. An unconnected port preserves the current value, including authored side effects before an early return. |
| `point_coord` / `POINT_COORD` | Fragment and Light, Vector2 | Read-only native raster coordinate, zero at the top-left and one at the bottom-right of the point footprint. Pixel centers sample the interior. Independent of mesh UV, fragment UV writes and custom setup. Undefined for other primitive topologies. |

Typed input definitions retain scalar zero outside these stages. An expression using an unavailable stage builtin or writing POINT_COORD fails import without publishing a partial resource. Global/custom helpers use the same builtins and the existing `ELECTRON2D_SHADER_STAGE` guards. Point coordinates consume no custom varying locations, so a four-location matrix varying remains usable. Fragment-to-light state retains the coordinate alongside the native light-stage read.

Each vertex invocation receives its own default size. Native vertex composition initializes one before calling authored code; it does not overwrite authored output afterward. GPU ArrayMesh point surfaces retain native topology in ordinary, CPU/hardware/device-written instance and particle paths. Compatibility keeps its existing point geometry and explicit shader-material refusal.

## Device capability

The shared cold GPU device factory requests Vulkan `largePoints` with the existing SDL Vulkan options structure. If that device cannot be created, it retries the prior core device and preserves the existing Android relaxed-feature fallback. The canvas and local RenderingDevice/physics context retain the negotiated flag with the native device lifetime. Metal has native sized points. Debug mode is explicitly disabled, matching the core constructor policy.

Before native shader creation, cold SPIR-V validation accepts literal-one size writes without optional capabilities. Authored/dynamic sizes, whole output-block writes and uncertain output-pointer flows require sized-point support. This conservative check uses builtin decorations, not debug names. PointCoord is accepted on Vulkan/Metal; unsupported SDL drivers reject explicitly. Positive finite size and the hardware point-size range remain the caller's shader contract; there is no engine-side clamp. Vulkan clamps/rasterizes according to device limits. D3D12 cannot execute this sized-point/coordinate contract through SDL's point path.

Paired raw GLSL uses `gl_PointSize` and `gl_PointCoord`. The HLSL fixture uses `[[vk::builtin("PointSize")]]` and DXC's documented `[[vk::ext_builtin_input(16)]] static const float2` input. The named `vk::builtin("PointCoord")` spelling is rejected by the pinned DXC; the supported inline SPIR-V attribute produces the native builtin without engine rewriting. See [DXC inline SPIR-V](https://github.com/microsoft/DirectXShaderCompiler/wiki/Inline-SPIR%E2%80%90V), [SDL point topology](https://wiki.libsdl.org/SDL3/SDL_GPUPrimitiveType), and [Vulkan point rasterization](https://docs.vulkan.org/spec/latest/chapters/primsrast.html#primsrast-points).

## Verification

[Public authoring](../../tests/VisualShader.Consumer/PointInputs.Author.cs), [paired raw sources](../../tests/VisualShader.Consumer/PointInputs.Raw.cs), [rendering](../../tests/VisualShader.Consumer/PointInputs.Render.cs) and [contract/module checks](../../tests/Electron2D.Tests/CanvasPointTests.cs) exercise eleven graphs plus GLSL/HLSL pairs. Node, expression, global/custom helper, default/stored output, early-return, matrix-varying and fragment-to-light producers execute across eleven geometry cases: primitive, indexed mesh, hardware/CPU/device-written MultiMesh, local particles, trails, world-coordinate particles, weighted skin, native scissor clipping and position-free procedural points. The procedural graph supplies positions from original vertex IDs.

Final CLR and strict full-trim NativeAOT profiles pass Wayland, X11 and HDR/4xMSAA. The node/global/light programs also execute after independent SPIR-V 1.3 and 1.5 compilation; the complete thirteen-program cohort exports 637 native vertex/fragment files per version, 1,911 independently validated files in total. RGB tolerance is 0.009 for native UNORM/HDR conversion and light accumulation. All ten final profiles verify 121 geometry/program cases in nine observed phases, with two points per case and strict 0/0-byte active/idle brackets.

The source-scoped receipts in ignored `bin/canvas-point-inputs/2026-10-25/` record actual profiles, module validation, allocation brackets and regressions. Native readback checks two distinct points per case, exact footprints and coordinate gradients, one-pixel initialization, sizes 5/9/13, live parameters, paired reload and zero/one/two scene lights. Prepared 64-active/64-idle frame brackets measure owner and all-thread managed bytes separately.

Linux software Vulkan verification does not establish physical-GPU, native-heap, foreign-platform, large-scene or owner visual acceptance. Optional-device fallback is checked through the program guard; no device lacking largePoints or native Metal/D3D12 device is available locally. Additional skeletal/LOD combinations retain the shared composition path and independent module checks; the native pixel corpus above names only the geometry it actually exercises. The particle attractor-force producer remains a separate input dependency.

The frozen full runtime suite passes with immutable DLL/PDB hashes. Its runtime and test IL bodies match the final Release builds (59,076 and 30,050 method bodies respectively); metadata differs only by module identity after XML/format edits. These are local functional checks, not publication qualification.

One earlier NativeAOT X11 interval observed 24 owner/all-thread bytes after its pixel phases. The cause was not localized. Three instrumented X11 repeats and the HDR/MSAA/modern-module controls passed strict zero-byte brackets; the fixture now retains per-frame input/process/parameter/render boundaries for a future recurrence. The original failure remains in `failed-active24-native-final-aot-x11.log`. Passing bounded brackets do not establish unconditional allocation freedom across native input/startup timing.
