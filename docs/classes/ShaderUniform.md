# ShaderUniform

Last updated: 2026-09-22

- Declaration: `internal sealed record ShaderUniform`
- Source: [ShaderProgram.cs](../../src/Servers/Rendering/ShaderProgram.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

One validated std140 uniform member. Reflection determines its numeric element type, buffer index, offset, stride and optional fixed array length. MaterialState uses this record for typed access and name/type/shape migration. It owns no buffer.

## Member summary

| Declaration | Contract |
| --- | --- |
| `ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride)` | [Construction and values](#construction-and-values) |
| `internal int Count { get; }` | [Element count](#element-count) |
| `internal bool Accepts<T>() where T : unmanaged` | [Type acceptance](#type-acceptance) |
| `internal bool SameType(ShaderUniform other)` | [Migration compatibility](#migration-compatibility) |
| `internal PropertyDescriptor Describe(string propertyName)` | [Descriptor](#descriptor) |

## Member descriptions

### Construction and values

`ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride)`

Arguments become record properties. ArrayLength zero denotes a scalar/vector; a positive length denotes a fixed array. Offsets/strides are bytes. Creation follows reflection validation, not arbitrary user input.

### Element count

`internal int Count { get; }`

Returns max(1, ArrayLength).

### Type acceptance

`internal bool Accepts<T>() where T : unmanaged`

Accepts the reflected C# type, plus Color as an alias for Vector4. Does not imply arbitrary unmanaged types are supported.

### Migration compatibility

`internal bool SameType(ShaderUniform other)`

Compares element Type and ArrayLength only. Matching values can migrate to changed buffer offsets/bindings.

### Descriptor

`internal PropertyDescriptor Describe(string propertyName)`

Builds a typed ShaderMaterial descriptor for a scalar/vector or whole array. Reads/setters use the material API; zero values are revert defaults. Array setters reject null and copy values through the span overload.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.
