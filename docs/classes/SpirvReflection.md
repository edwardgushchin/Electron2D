# SpirvReflection

Last updated: 2026-10-25

- Declaration: `internal static unsafe partial class SpirvReflection`
- Sources: [SpirvReflection.cs](../../src/Servers/Rendering/SpirvReflection.cs), [SpirvReflection.Booleans.cs](../../src/Servers/Rendering/SpirvReflection.Booleans.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

Reads active SPIR-V resources with the SPIRV-Cross C library already delivered by the SDL3-CS shadercross native package. It creates one native context per read, copies every retained name/layout into managed values, and destroys the context before returning. No native reflection pointer escapes. This adds no separate native package or public SDL type.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
var program = SpirvReflection.Read(ownedBytecode, fragment: true);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static ShaderProgram Read(byte[] code, bool fragment, IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes = null)` | [Read](#read) |

## Member descriptions

### Read

`internal static ShaderProgram Read(byte[] code, bool fragment, IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes = null)`

Requires the bounded structurally checked payload from ShaderCompiler. Parses active resources and accepts at most four uniform buffers (1..16384 bytes each) and sixteen fragment textures. Validates sets, contiguous bindings, names, std140 alignment/strides (including 16-byte float3 alignment with twelve stored bytes), fixed array lengths, member bounds/overlap, image types and actual HLSL image/sampler pairing. TIME is reserved for a non-array float32 scalar in a fragment uniform buffer. A wrong type, texture named TIME, duplicate member name or unsupported vertex TIME fails explicitly. The same name/layout checks apply to all input origins. TIME layout is separated from ordinary uniforms before constructing ShaderProgram descriptors; buffer sizing still includes it. Maps float3 to Vector3 and signed/unsigned 2/3/4-component vectors to Vector2i/Vector3i/Vector4i, preserving shader signedness for migration. Accepts float2x2 matrices and arrays mapped to Transform. Matrices require exactly one explicit RowMajor/ColMajor decoration, a positive 16-byte-multiple MatrixStride within the buffer limit, aligned offsets, valid array stride and no overlap. Other matrix shapes remain unsupported. Copies layout into ShaderProgram. Context creation failure raises InvalidOperationException; parser/reflection errors raise ArgumentException; unsupported layouts raise NotSupportedException. The adopted Code array remains owned/immutable by caller agreement.

## Native layout helpers

Private sequential `ReflectedResource` stores the three uint IDs (`Id`, `BaseTypeId`, `TypeId`) and borrowed name pointer returned by SPIRV-Cross. Private sequential `CombinedSampler` stores combined/image/sampler uint IDs. Their fields match the C ABI and are read only while the context is live. LibraryImport methods use exact native entry points and Cdecl; they are implementation details of Read.

The optional internal sourceTypes table is used only by import preflight to recover logical booleans before annotation. Every reflected member must have a source entry. Already annotated input is rejected in this mode. Published artifacts and runtime callers omit the table and validate embedded boolean records against unsigned physical width/array layout; unmatched or malformed records fail. The importer validates the final annotated output again through this ordinary path. See [the schema](../components/shader-materials.md#boolean-type-information).

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

Reflection validates reserved SCREEN_PIXEL_SIZE as a non-array fragment float32 vec2 and removes it from user uniforms. SCREEN_TEXTURE must be an ordinary supported sampled 2D binding, and reserved uniform names cannot masquerade as texture resources. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

SPIRV-Cross reflection imports now resolve the already packaged runtime-directory shared library directly before any SDL shadercross/device initialization. CPUParticlesTests creates its custom-data Shader in a fresh GPU host process before Engine.Run, then checks actual native pixels. Windows/macOS/Android names were checked against package artifacts; this Linux execution does not establish their native runtime acceptance. Static browser/Apple shader integrations keep their existing separate gates.

Fragment block type Instance/type.Instance routes its members into InstanceUniforms. It rejects more than sixteen fields, arrays, matrices and renderer-owned uniform names there. Boolean annotation covers both shared and instance dictionaries. See [per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters).

## Stage resources

Vertex resources now share the same typed scalar/vector/matrix/array and Instance reflection as fragments. Uniform sets remain one for vertex and three for fragment; sampled resources use zero and two respectively. Each stage has an independent sixteen-sampler limit. Renderer TIME, SCREEN_PIXEL_SIZE and SDF_COORDS accept the same types in vertex material blocks. Frame ownership and cross-stage name/layout merging are validated by ShaderProgram before publication.

## Sampled descriptor array integration

Read expands active fixed one-dimensional sampled descriptor arrays into per-element ShaderTexture metadata while preserving logical Binding, ArrayLength and Name. Literal lengths and total physical slots are bounded to sixteen; paired HLSL declarations require the same shape. Renderer/global descriptors remain scalar. Array pairing is validated by the managed adapter, avoiding the native combined-sampler helper that cannot represent arrays. Load/query-only images receive private native samplers. Reflection returns original Code and derived NativeCode without replacing source identity. See [the component contract](../components/shader-materials.md#fixed-sampled-descriptor-arrays).

The image record reader checks version, canonical native/public/global names, scalar/array shape, floating type, active ownership, stage/domain, policy bounds, padding and duplicates before publication. Canvas normal/specular sampler names require ordinary floating fragment images. The shared boolean metadata dispatcher routes image records to this reader; unknown metadata remains rejected.

NORMAL_MAP_FLAGS is a reserved float32-vector4 canvas-fragment renderer field. Wrong shapes, noncanvas stages and global/instance/image declarations reject before program publication; the field is removed from public material descriptors. See [the connected normal producer](../components/visual-shaders.md#fresnel-and-implicit-canvas-normals).

Canvas command texture size uses the renderer-owned float2 `TEXTURE_PIXEL_SIZE` contract in both canvas stages; it follows the resolved diffuse image logical metadata, independently of material samplers. See [command texture pixel size](../components/visual-shaders.md#command-texture-pixel-size) for authoring, stage/lifetime rules and verification limits.

Authored light programs retain validated function/state IDs, full validation code and declared native uniform slot extents. Light records retain raw color and separate energy without widening their eight-vector frame layout. The compositor invokes the private function per actual light and keeps the unlit entry separate. See [canvas light graphs](../components/canvas-light-graphs.md) for the exact integration and measured limits.

REGION_RECT and CANVAS_SPECULAR require ordinary canvas-fragment non-array float32 vec4 uniform fields. They reject global/instance/image ownership and are removed from user descriptors before publication. See [canvas command metadata](../components/canvas-command-metadata.md) for semantics and verification.
