# ShaderProgram

Last updated: 2026-10-25

- Declaration: `internal sealed class ShaderProgram`
- Source: [ShaderProgram.cs](../../src/Servers/Rendering/ShaderProgram.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

[Image-array resources](../components/texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Copied byte, normalized/float, block and unsigned-integer images execute; foreign/native-allocation acceptance remains separate.

## Description

A published immutable program version: owned SPIR-V bytes, padded uniform buffer sizes, named member layouts (including numeric signedness, logical boolean width, matrix stride and row/column order), sampled resources and descriptors. The constructor does not validate or copy its collections; ShaderCompiler/SpirvReflection construct them and no writer may mutate them after publication. Shader and its duplicates may share a version safely. Material state and GPU pipeline caches identify that version by reference.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
ShaderProgram program = ShaderCompiler.ValidateFragmentInterface(bytecode);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `ShaderProgram(byte[] code, int[] bufferSizes, Dictionary<string, ShaderUniform> uniforms, ShaderTexture[]? textures = null, ShaderUniform? timeUniform = null, ShaderUniform? screenPixelSizeUniform = null)` | [Construction](#construction) |
| `internal static readonly ShaderProgram Default` | [Default program](#default-program) |
| `internal readonly byte[] Code` | [Code](#code) |
| `internal readonly int[] BufferSizes` | [Buffer sizes](#buffer-sizes) |
| `internal readonly Dictionary<string, ShaderUniform> Uniforms` | [Uniforms](#uniforms) |
| `internal readonly ShaderUniform? TimeUniform` | [Time uniform](#time-uniform) |
| `internal readonly ShaderTexture[] Textures` | [Textures](#textures) |
| `internal readonly IReadOnlyList<PropertyDescriptor> Descriptors` | [Descriptors](#descriptors) |
| `internal readonly IReadOnlyList<PropertyDescriptor> MaterialDescriptors` | [Material descriptors](#material-descriptors) |
| `internal int FindTexture(string name, int index = 0)` | [Texture lookup](#texture-lookup) |

## Member descriptions

### Construction

`ShaderProgram(byte[] code, int[] bufferSizes, Dictionary<string, ShaderUniform> uniforms, ShaderTexture[]? textures = null, ShaderUniform? timeUniform = null, ShaderUniform? screenPixelSizeUniform = null)`

Adopts the owned payload/layout, uses an empty texture list when omitted, and constructs read-only descriptor lists. Native resources are absent.

### Default program

`internal static readonly ShaderProgram Default`

Built-in fragment code with the reserved TEXTURE at binding zero and no material uniforms.

### Code

`internal readonly byte[] Code`

Owned bytecode; public Shader.GetSPIRV clones it.

### Buffer sizes

`internal readonly int[] BufferSizes`

Padded byte count for each contiguous uniform binding.

### Uniforms

`internal readonly Dictionary<string, ShaderUniform> Uniforms`

Ordinal, case-sensitive material name-to-layout lookup, excluding reserved TIME.

### Time uniform

`internal readonly ShaderUniform? TimeUniform`

Optional validated float32 TIME location, separate from material parameters and descriptors. The reflected buffer allocation includes this field. Null means no clock upload is needed.

### Textures

`internal readonly ShaderTexture[] Textures`

Validated binding-ordered texture descriptors, including optional reserved TEXTURE.

### Descriptors

`internal readonly IReadOnlyList<PropertyDescriptor> Descriptors`

Unprefixed typed inspection descriptors, omitting reserved TEXTURE and TIME. Float2x2 uses Transform with identity revert values; float3 uses Vector3 (material access also accepts Color, with alpha one on read); float4 uses Vector4 (material aliases Color and Rect2); signed/unsigned integer vectors use Vector2i/Vector3i/Vector4i. Array descriptors retain the canonical element mapping.

### Material descriptors

`internal readonly IReadOnlyList<PropertyDescriptor> MaterialDescriptors`

The same reflected parameters with shader_parameter/ prefixes for material property discovery.

### Texture lookup

`internal int FindTexture(string name, int index = 0)`

Returns the named texture index, excluding reserved TEXTURE. Null/blank or unknown names raise ArgumentException through the shared typed API.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

The immutable reflected program includes UsesScreenTexture and optional ScreenPixelSizeUniform. Descriptors/FindTexture exclude both renderer-owned texture roles; resource uniforms retain their previous behavior. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

InstanceUniforms, HasInstanceUniforms, InstanceDescriptors and CanvasInstanceDescriptors publish separate per-item layout and typed metadata. BufferSizes includes every shared/instance block under the four-slot fragment limit. The constructor also accepts SDFCoordsUniform and instanceUniforms. See [per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters).

## Paired program metadata

WithVertex validates the exact Frame layout and merges stage uniforms, builtins, instance fields and texture names. Vertex and Fragment retain the original immutable modules. FragmentBufferCount and FragmentTextureCount delimit the fragment prefix; VertexTextureIndices maps vertex sampler slots to combined resource indices. UniformCopies maps canonical shared fields to each target physical layout. ValidateVertexFrame reserves binding zero exclusively for client size. PrepareVertexVariants creates four cold CanvasVertexProgram variants, selected by VertexCode(instanced, lighting). No published collection or derived payload may mutate.

## Sampled descriptor array integration

Code retains the immutable original payload; NativeCode retains the derived scalar-descriptor module. FindTexture(name, index = 0) validates logical element range and resolves its physical slot. Descriptors and MaterialDescriptors expose only Element zero, with typed array values. WithVertex merges matching Name/Element slots, rejects same-name length/role/scalar conflicts and maps each vertex element independently. Composition consumes NativeCode, while copied exports and archives consume Code. See [the component contract](../components/shader-materials.md#fixed-sampled-descriptor-arrays).

## Particle integration

ParticleStorageBuffers records the validated native writable descriptor count, one for state or two with the optional bounded subemission queue; retained simulation binds exactly that count. See [GPU particles](../components/gpu-particles.md).

## Authored uniform defaults

Each validated ShaderUniform carries its own compiled defaults before descriptors are constructed. Paired stages require equal explicit defaults for shared names; a default authored in either stage supplies the canonical value when the other is absent. Physical copies translate each matrix storage order. Renderer Frame defaults reject. Descriptors retain the immutable version and copy array revert values; explicit-storage predicates omit unset numeric values from material archives. See [authored uniform defaults](../components/shader-materials.md#authored-uniform-defaults).

Sampled-image graph imports retain validated ShaderImagePolicy records per ShaderTexture: encoded color, render-only fallback, filter/repeat and screen ownership. Paired stages reject differing policies for one image name. NativeName retains the module binding identity while metadata preserves public material aliases and explicit logical global names. Renderer-owned normal/specular/screen aliases are excluded from material descriptors. See [sampled graph images](../components/visual-shaders.md#sampled-images-and-canvas-channels).

The private NormalMapFlagsUniform records the validated canvas-fragment renderer field and survives paired-stage composition. It is excluded from material, instance and global descriptors. See [the connected normal producer](../components/visual-shaders.md#fresnel-and-implicit-canvas-normals).

Canvas command texture size uses the renderer-owned float2 `TEXTURE_PIXEL_SIZE` contract in both canvas stages; it follows the resolved diffuse image logical metadata, independently of material samplers. See [command texture pixel size](../components/visual-shaders.md#command-texture-pixel-size) for authoring, stage/lifetime rules and verification limits.
