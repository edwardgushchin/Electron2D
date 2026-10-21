# ShaderUniform

Last updated: 2026-10-21

- Declaration: `internal sealed partial record ShaderUniform`
- Source: [ShaderProgram.cs](../../src/Servers/Rendering/ShaderProgram.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

One validated std140 uniform member. Reflection determines its numeric element type, buffer index, offset, stride and optional fixed array length. MaterialState uses this record for typed access and name/type/shape migration. It owns no buffer.

## Member summary

| Declaration | Contract |
| --- | --- |
| `ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride, bool Unsigned, int MatrixStride, bool RowMajor, int BooleanWidth, int MatrixWidth)` | [Construction and values](#construction-and-values) |
| `internal int Count { get; }` | [Element count](#element-count) |
| `internal bool Accepts<T>() where T : unmanaged` | [Type acceptance](#type-acceptance) |
| `internal bool SameType(ShaderUniform other)` | [Migration compatibility](#migration-compatibility) |
| `internal void Write<T>(Span<byte> target, in T value) where T : unmanaged` | [Component access](#component-access) |
| `internal T Read<T>(ReadOnlySpan<byte> source) where T : unmanaged` | [Component access](#component-access) |
| `internal PropertyDescriptor Describe(string propertyName)` | [Descriptor](#descriptor) |

## Member descriptions

### Construction and values

`ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride, bool Unsigned, int MatrixStride, bool RowMajor, int BooleanWidth, int MatrixWidth)`

Arguments become record properties. ArrayLength zero denotes a scalar/vector/matrix; a positive length denotes a fixed array. Offsets/strides are bytes. Unsigned distinguishes signed and unsigned shader integers even when both use Vector2i/Vector3i/Vector4i. Float3 has Type Vector3 and ElementSize 12; its alignment and array stride remain 16 bytes. Float2x2 has Type Transform; MatrixStride is the byte step between its two stored vectors and RowMajor chooses rows instead of columns. ElementSize spans the first component through the second vector (MatrixStride + 8); matrices require 16-byte alignment. Non-matrix MatrixStride is zero. BooleanWidth is zero for numeric members and 1..4 for validated boolean storage: width one has Type bool, widths two through four Type int. Physical components remain uint32. Creation follows reflection validation, not arbitrary user input.

### Element count

`internal int Count { get; }`

Returns max(1, ArrayLength).

### Type acceptance

`internal bool Accepts<T>() where T : unmanaged`

Accepts the reflected C# type, plus Color and Rect2 as aliases for Vector4. Float3 uses Vector3 and also accepts Color. Logical booleans accept bool or int masks according to width; arrays use the same element mapping. Integer vectors carry signed/unsigned component bits without conversion. Float2x2 accepts Transform, storing only X/Y. Does not imply arbitrary unmanaged types are supported.

### Migration compatibility

`internal bool SameType(ShaderUniform other)`

Compares element Type, Unsigned, BooleanWidth, MatrixWidth and ArrayLength. Matching values can migrate to changed buffer offsets/bindings. Matrix stride and storage order are intentionally omitted: migration decodes the old basis and encodes it in the new layout.

### Component access

`internal void Write<T>(Span<byte> target, in T value) where T : unmanaged`

Boolean writes normalize each physical component to zero/one from bool or the corresponding low mask bit. Copies represented components from a validated value into its member slice. For float2x2, scatters the four Transform basis components according to MatrixStride/RowMajor, omitting Origin and preserving padding. Float3 writes RGB only; following scalars and array padding are preserved.

`internal T Read<T>(ReadOnlySpan<byte> source) where T : unmanaged`

Boolean reads return bool or the low component mask. Copies stored components into a zero-initialized typed value, reconstructing alpha one when float3 is read as Color. Float2x2 gathers its four basis components and leaves Origin zero. Callers validate T and provide exactly the reflected component slice. Both helpers use stack-backed spans with no managed allocation.

### Descriptor

`internal PropertyDescriptor Describe(string propertyName)`

Builds a typed ShaderMaterial descriptor for a scalar/vector/matrix or whole array. Reads/setters use the material API; revert values use compiled defaults for each element, with implicit identity/zero when absent and alpha one for float3 Color. Array setters reject null and copy values through the span overload.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

## Compiled default storage

[ShaderUniform.Defaults.cs](../../src/Servers/Rendering/ShaderUniform.Defaults.cs) retains immutable canonical uint32 words, logical type/element width and array shape after reflection validation. `InitializeDefault` clears physical padding and repacks every matrix/array element through its reflected layout. `ReadDefault<T>` returns typed scalar/vector/Transform values without allocation; descriptor arrays and wide matrices return independent copies. `HasSameDefault` compares components, accepting opposite signed floating zero across stages. `SameType` ignores defaults so explicit values still migrate. See [authored uniform defaults](../components/shader-materials.md#authored-uniform-defaults).
