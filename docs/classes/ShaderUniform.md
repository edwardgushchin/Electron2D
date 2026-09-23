# ShaderUniform

Last updated: 2026-09-23

- Declaration: `internal sealed record ShaderUniform`
- Source: [ShaderProgram.cs](../../src/Servers/Rendering/ShaderProgram.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

One validated std140 uniform member. Reflection determines its numeric element type, buffer index, offset, stride and optional fixed array length. MaterialState uses this record for typed access and name/type/shape migration. It owns no buffer.

## Member summary

| Declaration | Contract |
| --- | --- |
| `ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride, bool Unsigned, int MatrixStride, bool RowMajor, int BooleanWidth)` | [Construction and values](#construction-and-values) |
| `internal int Count { get; }` | [Element count](#element-count) |
| `internal bool Accepts<T>() where T : unmanaged` | [Type acceptance](#type-acceptance) |
| `internal bool SameType(ShaderUniform other)` | [Migration compatibility](#migration-compatibility) |
| `internal void Write<T>(Span<byte> target, in T value) where T : unmanaged` | [Component access](#component-access) |
| `internal T Read<T>(ReadOnlySpan<byte> source) where T : unmanaged` | [Component access](#component-access) |
| `internal PropertyDescriptor Describe(string propertyName)` | [Descriptor](#descriptor) |

## Member descriptions

### Construction and values

`ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride, bool Unsigned, int MatrixStride, bool RowMajor, int BooleanWidth)`

Arguments become record properties. ArrayLength zero denotes a scalar/vector/matrix; a positive length denotes a fixed array. Offsets/strides are bytes. Unsigned distinguishes signed and unsigned shader integers even when both use Vector2I/Vector3I/Vector4I. Float3 has Type Vector3 and ElementSize 12; its alignment and array stride remain 16 bytes. Float2x2 has Type Transform; MatrixStride is the byte step between its two stored vectors and RowMajor chooses rows instead of columns. ElementSize spans the first component through the second vector (MatrixStride + 8); matrices require 16-byte alignment. Non-matrix MatrixStride is zero. BooleanWidth is zero for numeric members and 1..4 for validated boolean storage: width one has Type bool, widths two through four Type int. Physical components remain uint32. Creation follows reflection validation, not arbitrary user input.

### Element count

`internal int Count { get; }`

Returns max(1, ArrayLength).

### Type acceptance

`internal bool Accepts<T>() where T : unmanaged`

Accepts the reflected C# type, plus Color and Rect as aliases for Vector4. Float3 uses Vector3 and also accepts Color. Logical booleans accept bool or int masks according to width; arrays use the same element mapping. Integer vectors carry signed/unsigned component bits without conversion. Float2x2 accepts Transform, storing only X/Y. Does not imply arbitrary unmanaged types are supported.

### Migration compatibility

`internal bool SameType(ShaderUniform other)`

Compares element Type, Unsigned, BooleanWidth and ArrayLength. Matching values can migrate to changed buffer offsets/bindings. Matrix stride and storage order are intentionally omitted: migration decodes the old basis and encodes it in the new layout.

### Component access

`internal void Write<T>(Span<byte> target, in T value) where T : unmanaged`

Boolean writes normalize each physical component to zero/one from bool or the corresponding low mask bit. Copies represented components from a validated value into its member slice. For float2x2, scatters the four Transform basis components according to MatrixStride/RowMajor, omitting Origin and preserving padding. Float3 writes RGB only; following scalars and array padding are preserved.

`internal T Read<T>(ReadOnlySpan<byte> source) where T : unmanaged`

Boolean reads return bool or the low component mask. Copies stored components into a zero-initialized typed value, reconstructing alpha one when float3 is read as Color. Float2x2 gathers its four basis components and leaves Origin zero. Callers validate T and provide exactly the reflected component slice. Both helpers use stack-backed spans with no managed allocation.

### Descriptor

`internal PropertyDescriptor Describe(string propertyName)`

Builds a typed ShaderMaterial descriptor for a scalar/vector/matrix or whole array. Reads/setters use the material API; identity is the float2x2 revert default; other values use zero components, with alpha one for float3 Color, including each array element. Array setters reject null and copy values through the span overload.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.
