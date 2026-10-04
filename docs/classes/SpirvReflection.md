# SpirvReflection

Last updated: 2026-10-04

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
