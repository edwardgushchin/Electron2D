# ShaderCompiler

Last updated: 2026-10-23

- Declaration: `internal static unsafe class ShaderCompiler`
- Source: [ShaderCompiler.cs](../../src/Servers/Rendering/ShaderCompiler.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

The common runtime SPIR-V validation and SDL_shadercross GPU creation boundary. It does not compile HLSL/GLSL source. Bytecode loading copies an owned payload, checks structure/capabilities, invokes SpirvReflection for resources and shadercross for stage interfaces. A process-wide gate serializes shadercross Init/Quit around each cold native operation. This is loading/pipeline creation work, not per-frame compilation.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
var program = ShaderCompiler.ValidateFragmentInterface(bytecode);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static SDL3.SDL.GPUShaderFormat GetFormats()` | [Backend formats](#backend-formats) |
| `internal static nint CreateShader(nint device, byte[] code, bool fragment)` | [GPU shader creation](#gpu-shader-creation) |
| `internal static nint CreateComputePipeline(nint device, byte[] code)` | Reflect offline SPIR-V, reject native resource-count limits before creation, and create an internal SDL compute pipeline; retain/free cold reflection storage. |
| `internal static ShaderProgram ValidateFragmentInterface(ReadOnlySpan<byte> bytecode)` | [Fragment validation](#fragment-validation) |
| `internal static ShaderProgram ValidateInterface(ReadOnlySpan<byte> bytecode, bool fragment, IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes = null)` | [Shared validation](#shared-validation) |

## Member descriptions

### Backend formats

`internal static SDL3.SDL.GPUShaderFormat GetFormats()`

Queries shadercross-supported SPIR-V translations within its serialized native lifetime. The native enum stays internal.

### GPU shader creation

Compute creation reflects and validates the complete storage/sampler/uniform
counts and workgroup before native allocation. A DXIL-capable device uses the
existing ShaderCross DXIL compiler and forwards that metadata to SDL. This avoids
the convenience function's preference for FXC/DXBC; failed compilation preserves
its diagnostic and never passes a null bytecode buffer to SDL. Compiled storage
and reflection storage are freed in `finally`. Other device formats retain the
native convenience path. Graphics shader creation is unchanged. Offline DXIL
compilation and Linux Vulkan checks do not establish Windows execution.

`internal static nint CreateShader(nint device, byte[] code, bool fragment)`

Reflects stage resources and asks shadercross to create main for the chosen vertex/fragment stage. Frees native metadata in finally. The returned native shader is adopted by RenderHandle; zero is handled by that owner. Translation/native creation errors are not silently replaced with a default shader.

### Fragment validation

`internal static ShaderProgram ValidateFragmentInterface(ReadOnlySpan<byte> bytecode)`

Runs the shared interface validation for the public canvas fragment resource.

### Shared validation

`internal static ShaderProgram ValidateInterface(ReadOnlySpan<byte> bytecode, bool fragment, IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes = null)`

Rejects lengths below 20 bytes, above 16 MiB or not divisible by four before copying. Checks little-endian header, SPIR-V versions 1.0 through 1.6, bounded IDs, nonzero/in-bounds instruction word counts, a single selected-stage main entry point, baseline Shader capability and logical GLSL450 memory model when declared. Reflection then checks active resources, std140 values, contiguous bindings and fixed canvas varyings. Fragment inputs are optional float4 color/location 0 and float2 UV/location 1, with one float4 output/location 0. The internal vertex profile requires float2 position/location 0, float4 color/location 1, float2 UV/location 2, a reserved Frame block and optional material/Instance blocks, plus color/UV outputs. Malformed data raises ArgumentException; unsupported interfaces raise NotSupportedException. This is not full instruction-semantic validation; import runs spirv-val separately.

The optional internal sourceTypes table is used only by import preflight to recover logical booleans before annotation. Every reflected member must have a source entry. Already annotated input is rejected in this mode. Published artifacts and runtime callers omit the table and validate embedded boolean records against unsigned physical width/array layout; unmatched or malformed records fail. The importer validates the final annotated output again through this ordinary path. See [the schema](../components/shader-materials.md#boolean-type-information).

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

SPIRV-Cross reflection imports now resolve the already packaged runtime-directory shared library directly before any SDL shadercross/device initialization. CPUParticlesTests creates its custom-data Shader in a fresh GPU host process before Engine.Run, then checks actual native pixels. Windows/macOS/Android names were checked against package artifacts; this Linux execution does not establish their native runtime acceptance. Static browser/Apple shader integrations keep their existing separate gates.

SamplerCount reflects only the composed stage sampler count through the existing serialized ShaderCross lifetime. Glyph composition uses it on cold cache creation, never on prepared frames. Native metadata is freed in finally.

## Paired canvas validation

ValidateCanvasProgram checks both module lengths before copying either, validates each canvas stage, compares vertex outputs to fragment inputs and rejects authored hardware instance locations four through six. ShaderProgram.WithVertex enforces shared logical parameter types and roles, then prepares all four derived vertex variants before publication. ValidateBytecodeLength retains the sixteen-MiB, header and word-alignment boundary. Vertex Frame validation applies during source/external import as well as runtime pair loading; renderer builtins in Frame are rejected.

## Sampled descriptor array integration

ValidateInterface reflects the original logical interface and then verifies native metadata against the derived NativeCode, including the physical sixteen-sampler stage bound. Paired validation and vertex adaptation use derived modules. Structural validation admits only the required sampled-array dynamic/nonuniform and image-query capabilities in addition to the established canvas capabilities; unsupported domains still reject. See [the component contract](../components/shader-materials.md#fixed-sampled-descriptor-arrays).

## Default metadata validation

Reflection validates the versioned typed default OpString payload before constructing/publishing a program. Names, scalar/array length, logical type and finite/canonical components must match active user material/Instance fields. Renderer/global defaults reject. Both canvas stages, TextureBlit and particle compute use this same boundary. Source extraction remains offline in ShaderImport. See [authored uniform defaults](../components/shader-materials.md#authored-uniform-defaults).
