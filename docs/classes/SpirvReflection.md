# SpirvReflection

Last updated: 2026-09-22

- Declaration: `internal static unsafe partial class SpirvReflection`
- Source: [SpirvReflection.cs](../../src/Servers/Rendering/SpirvReflection.cs)
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
| `internal static ShaderProgram Read(byte[] code, bool fragment)` | [Read](#read) |

## Member descriptions

### Read

`internal static ShaderProgram Read(byte[] code, bool fragment)`

Requires the bounded structurally checked payload from ShaderCompiler. Parses active resources and accepts at most four uniform buffers (1..16384 bytes each) and sixteen fragment textures. Validates sets, contiguous bindings, names, std140 alignment/strides, fixed array lengths, member bounds/overlap, image types and actual HLSL image/sampler pairing. Copies layout into ShaderProgram. Context creation failure raises InvalidOperationException; parser/reflection errors raise ArgumentException; unsupported layouts raise NotSupportedException. The adopted Code array remains owned/immutable by caller agreement.

## Native layout helpers

Private sequential `ReflectedResource` stores the three uint IDs (`Id`, `BaseTypeId`, `TypeId`) and borrowed name pointer returned by SPIRV-Cross. Private sequential `CombinedSampler` stores combined/image/sampler uint IDs. Their fields match the C ABI and are read only while the context is live. LibraryImport methods use exact native entry points and Cdecl; they are implementation details of Read.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.
