# ShaderProgram

Last updated: 2026-09-23

- Declaration: `internal sealed class ShaderProgram`
- Source: [ShaderProgram.cs](../../src/Servers/Rendering/ShaderProgram.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

A published immutable program version: owned SPIR-V bytes, padded uniform buffer sizes, named member layouts, sampled resources and descriptors. The constructor does not validate or copy its collections; ShaderCompiler/SpirvReflection construct them and no writer may mutate them after publication. Shader and its duplicates may share a version safely. Material state and GPU pipeline caches identify that version by reference.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
ShaderProgram program = ShaderCompiler.ValidateFragmentInterface(bytecode);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `ShaderProgram(byte[] code, int[] bufferSizes, Dictionary<string, ShaderUniform> uniforms, ShaderTexture[]? textures = null, ShaderUniform? timeUniform = null)` | [Construction](#construction) |
| `internal static readonly ShaderProgram Default` | [Default program](#default-program) |
| `internal readonly byte[] Code` | [Code](#code) |
| `internal readonly int[] BufferSizes` | [Buffer sizes](#buffer-sizes) |
| `internal readonly Dictionary<string, ShaderUniform> Uniforms` | [Uniforms](#uniforms) |
| `internal readonly ShaderUniform? TimeUniform` | [Time uniform](#time-uniform) |
| `internal readonly ShaderTexture[] Textures` | [Textures](#textures) |
| `internal readonly IReadOnlyList<PropertyDescriptor> Descriptors` | [Descriptors](#descriptors) |
| `internal readonly IReadOnlyList<PropertyDescriptor> MaterialDescriptors` | [Material descriptors](#material-descriptors) |
| `internal int FindTexture(string name)` | [Texture lookup](#texture-lookup) |

## Member descriptions

### Construction

`ShaderProgram(byte[] code, int[] bufferSizes, Dictionary<string, ShaderUniform> uniforms, ShaderTexture[]? textures = null, ShaderUniform? timeUniform = null)`

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

Unprefixed typed inspection descriptors, omitting reserved TEXTURE and TIME.

### Material descriptors

`internal readonly IReadOnlyList<PropertyDescriptor> MaterialDescriptors`

The same reflected parameters with shader_parameter/ prefixes for material property discovery.

### Texture lookup

`internal int FindTexture(string name)`

Returns the named texture index, excluding reserved TEXTURE. Null/blank or unknown names raise ArgumentException through the shared typed API.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.
