# ShaderCompiler

Last updated: 2026-09-22

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
| `internal static ShaderProgram ValidateFragmentInterface(ReadOnlySpan<byte> bytecode)` | [Fragment validation](#fragment-validation) |
| `internal static ShaderProgram ValidateInterface(ReadOnlySpan<byte> bytecode, bool fragment)` | [Shared validation](#shared-validation) |

## Member descriptions

### Backend formats

`internal static SDL3.SDL.GPUShaderFormat GetFormats()`

Queries shadercross-supported SPIR-V translations within its serialized native lifetime. The native enum stays internal.

### GPU shader creation

`internal static nint CreateShader(nint device, byte[] code, bool fragment)`

Reflects stage resources and asks shadercross to create main for the chosen vertex/fragment stage. Frees native metadata in finally. The returned native shader is adopted by RenderHandle; zero is handled by that owner. Translation/native creation errors are not silently replaced with a default shader.

### Fragment validation

`internal static ShaderProgram ValidateFragmentInterface(ReadOnlySpan<byte> bytecode)`

Runs the shared interface validation for the public canvas fragment resource.

### Shared validation

`internal static ShaderProgram ValidateInterface(ReadOnlySpan<byte> bytecode, bool fragment)`

Rejects lengths below 20 bytes, above 16 MiB or not divisible by four before copying. Checks little-endian header, SPIR-V versions 1.0 through 1.6, bounded IDs, nonzero/in-bounds instruction word counts, a single selected-stage main entry point, baseline Shader capability and logical GLSL450 memory model when declared. Reflection then checks active resources, std140 values, contiguous bindings and fixed canvas varyings. Fragment inputs are optional float4 color/location 0 and float2 UV/location 1, with one float4 output/location 0. The internal vertex profile requires float2 position/location 0, float4 color/location 1, float2 UV/location 2, one uniform buffer and color/UV outputs. Malformed data raises ArgumentException; unsupported interfaces raise NotSupportedException. This is not full instruction-semantic validation; import runs spirv-val separately.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.
