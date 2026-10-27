# ByteBuffer

Last updated: 2026-10-27

`public static class Electron2D.ByteBuffer`

Part of [binary buffers](../components/byte-buffers.md), under [ADR 0020](../decisions/core-data-io.md#caller-owned-binary-buffers). Sources: [scalars](../../src/Core/IO/ByteBuffer.cs), [arrays](../../src/Core/IO/ByteBuffer.Arrays.cs), [text](../../src/Core/IO/ByteBuffer.Text.cs), [compression](../../src/Core/IO/ByteBuffer.Compression.cs).

Caller-owned spans retain their ownership. All numeric bytes are little endian; offsets may be unaligned. Scalar/swap operations allocate no managed memory. String/array/compression results are independent preparation snapshots. Compression shares FileAccess’s versioned container; it is not a raw external codec stream.

## Example

```csharp
Span<byte> packet = stackalloc byte[12];
ByteBuffer.EncodeU32(packet, 0, 42);
ByteBuffer.EncodeDouble(packet, 4, 1.5);
var value = ByteBuffer.DecodeDouble(packet, 4);
```

## API reference

| Declaration | Contract |
| --- | --- |
| [`public static System.Void BSwap16(System.Span<System.Byte> data, System.Int32 offset = 0, System.Int32 count = -1)`](#member-ed299fe8b9c1) | Reverses bytes within complete 16-bit segments in place. |
| [`public static System.Void BSwap32(System.Span<System.Byte> data, System.Int32 offset = 0, System.Int32 count = -1)`](#member-d435a52b5aea) | Reverses bytes within complete 32-bit segments in place. |
| [`public static System.Void BSwap64(System.Span<System.Byte> data, System.Int32 offset = 0, System.Int32 count = -1)`](#member-87d09f0c78b6) | Reverses bytes within complete 64-bit segments in place. |
| [`public static System.Byte[] Compress(System.ReadOnlySpan<System.Byte> data, Electron2D.FileCompressionMode compressionMode = FastLz)`](#member-90e53a5f6697) | Compresses bytes into the versioned FileAccess compressed container. |
| [`public static System.Double DecodeDouble(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-c8e85150bce2) | Reads a 64-bit IEEE floating-point value at a byte offset. |
| [`public static System.Single DecodeFloat(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-24968655ab6e) | Reads a 32-bit IEEE floating-point value at a byte offset. |
| [`public static System.Single DecodeHalf(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-4b92635c0bc6) | Reads a 16-bit IEEE floating-point value at a byte offset. |
| [`public static System.Int16 DecodeS16(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-9bce138909b3) | Reads a 16-bit integer at a byte offset. |
| [`public static System.Int32 DecodeS32(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-a88668d156a9) | Reads a 32-bit integer at a byte offset. |
| [`public static System.Int64 DecodeS64(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-8d9edb661d13) | Reads a 64-bit integer at a byte offset. |
| [`public static System.SByte DecodeS8(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-95bf0ea5f31b) | Reads a 8-bit integer at a byte offset. |
| [`public static System.UInt16 DecodeU16(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-276b060e5fcc) | Reads a 16-bit integer at a byte offset. |
| [`public static System.UInt32 DecodeU32(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-035a53b688da) | Reads a 32-bit integer at a byte offset. |
| [`public static System.UInt64 DecodeU64(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-2dabfaa1a704) | Reads a 64-bit integer at a byte offset. |
| [`public static System.Byte DecodeU8(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`](#member-288e7c2e6a3d) | Reads a 8-bit integer at a byte offset. |
| [`public static System.Byte[] Decompress(System.ReadOnlySpan<System.Byte> data, System.Int32 bufferSize, Electron2D.FileCompressionMode compressionMode = FastLz)`](#member-6b0e6490c8b2) | Decompresses a compressed snapshot subject to a caller-supplied output capacity. |
| [`public static System.Byte[] DecompressDynamic(System.ReadOnlySpan<System.Byte> data, System.Int32 maxOutputSize, Electron2D.FileCompressionMode compressionMode = FastLz)`](#member-05d3ec74696a) | Decompresses DEFLATE, GZip or Brotli without requiring the exact decoded size. |
| [`public static System.Void EncodeDouble(System.Span<System.Byte> data, System.Int32 byteOffset, System.Double value)`](#member-ae67dafdc71e) | Writes a 64-bit IEEE floating-point value at a byte offset. |
| [`public static System.Void EncodeFloat(System.Span<System.Byte> data, System.Int32 byteOffset, System.Single value)`](#member-3e519384e637) | Writes a 32-bit IEEE floating-point value at a byte offset. |
| [`public static System.Void EncodeHalf(System.Span<System.Byte> data, System.Int32 byteOffset, System.Single value)`](#member-12fd9fd6558f) | Writes a 16-bit IEEE floating-point value at a byte offset. |
| [`public static System.Void EncodeS16(System.Span<System.Byte> data, System.Int32 byteOffset, System.Int16 value)`](#member-9191a2656b72) | Writes a 16-bit integer at a byte offset. |
| [`public static System.Void EncodeS32(System.Span<System.Byte> data, System.Int32 byteOffset, System.Int32 value)`](#member-6cc7b3ab398f) | Writes a 32-bit integer at a byte offset. |
| [`public static System.Void EncodeS64(System.Span<System.Byte> data, System.Int32 byteOffset, System.Int64 value)`](#member-0315e1409825) | Writes a 64-bit integer at a byte offset. |
| [`public static System.Void EncodeS8(System.Span<System.Byte> data, System.Int32 byteOffset, System.SByte value)`](#member-e36cb4c60dee) | Writes a 8-bit integer at a byte offset. |
| [`public static System.Void EncodeU16(System.Span<System.Byte> data, System.Int32 byteOffset, System.UInt16 value)`](#member-f93d5d0e90c9) | Writes a 16-bit integer at a byte offset. |
| [`public static System.Void EncodeU32(System.Span<System.Byte> data, System.Int32 byteOffset, System.UInt32 value)`](#member-18e631efafbb) | Writes a 32-bit integer at a byte offset. |
| [`public static System.Void EncodeU64(System.Span<System.Byte> data, System.Int32 byteOffset, System.UInt64 value)`](#member-3826dcde27e6) | Writes a 64-bit integer at a byte offset. |
| [`public static System.Void EncodeU8(System.Span<System.Byte> data, System.Int32 byteOffset, System.Byte value)`](#member-165597bbec4d) | Writes a 8-bit integer at a byte offset. |
| [`public static System.String GetStringFromASCII(System.ReadOnlySpan<System.Byte> data)`](#member-441609298762) | Maps Latin-1 bytes to characters, stopping at the first zero byte. |
| [`public static System.String GetStringFromMultibyteChar(System.ReadOnlySpan<System.Byte> data, System.String encoding = "")`](#member-a6f1e0fd6d23) | Decodes a named .NET multibyte encoding with strict error handling. |
| [`public static System.String GetStringFromUTF16(System.ReadOnlySpan<System.Byte> data)`](#member-4c8e9a676e78) | Decodes strict UTF-16, honoring a leading byte-order mark and otherwise assuming little endian. |
| [`public static System.String GetStringFromUTF32(System.ReadOnlySpan<System.Byte> data)`](#member-4a96a2eda4be) | Decodes strict UTF-32, honoring a leading byte-order mark and otherwise assuming little endian. |
| [`public static System.String GetStringFromUTF8(System.ReadOnlySpan<System.Byte> data)`](#member-9fe52f272eea) | Decodes strict UTF-8, removing an optional leading byte-order mark. |
| [`public static System.String GetStringFromWChar(System.ReadOnlySpan<System.Byte> data)`](#member-8cb923fd0479) | Decodes native wide characters: UTF-16 on Windows, UTF-32 on other hosts. |
| [`public static System.String HexEncode(System.ReadOnlySpan<System.Byte> data)`](#member-fc31064d0267) | Encodes every source byte as two lowercase hexadecimal characters. |
| [`public static System.Byte[] ToByteArray(System.ReadOnlySpan<Electron2D.Color> data)`](#member-6a93893c78da) | Encodes Color values into independent little-endian bytes. |
| [`public static System.Byte[] ToByteArray(System.ReadOnlySpan<Electron2D.Vector2> data)`](#member-a70077d4d794) | Encodes Vector2 values into independent little-endian bytes. |
| [`public static System.Byte[] ToByteArray(System.ReadOnlySpan<Electron2D.Vector3> data)`](#member-4d0bd84ddc4b) | Encodes Vector3 values into independent little-endian bytes. |
| [`public static System.Byte[] ToByteArray(System.ReadOnlySpan<Electron2D.Vector4> data)`](#member-7556ca9d6d78) | Encodes Vector4 values into independent little-endian bytes. |
| [`public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.Double> data)`](#member-ae79ba753e7b) | Encodes double values into independent little-endian bytes. |
| [`public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.Int32> data)`](#member-c7d954e31aca) | Encodes int values into independent little-endian bytes. |
| [`public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.Int64> data)`](#member-003018138740) | Encodes long values into independent little-endian bytes. |
| [`public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.Single> data)`](#member-0bc9ea14fa30) | Encodes float values into independent little-endian bytes. |
| [`public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.String> data)`](#member-b8160545e73f) | Encodes consecutive UTF-8 strings, each followed by a zero byte. |
| [`public static Electron2D.Color[] ToColorArray(System.ReadOnlySpan<System.Byte> data)`](#member-6edcc18142cb) | Decodes complete little-endian Color values into independent storage. |
| [`public static System.Single[] ToFloat32Array(System.ReadOnlySpan<System.Byte> data)`](#member-d2724aa6d294) | Decodes complete little-endian float values into independent storage. |
| [`public static System.Double[] ToFloat64Array(System.ReadOnlySpan<System.Byte> data)`](#member-719dcf59764d) | Decodes complete little-endian double values into independent storage. |
| [`public static System.Int32[] ToInt32Array(System.ReadOnlySpan<System.Byte> data)`](#member-d58e226cb046) | Decodes complete little-endian int values into independent storage. |
| [`public static System.Int64[] ToInt64Array(System.ReadOnlySpan<System.Byte> data)`](#member-28048e03f7c0) | Decodes complete little-endian long values into independent storage. |
| [`public static Electron2D.Vector2[] ToVector2Array(System.ReadOnlySpan<System.Byte> data)`](#member-311e4320d9a9) | Decodes complete little-endian Vector2 values into independent storage. |
| [`public static Electron2D.Vector3[] ToVector3Array(System.ReadOnlySpan<System.Byte> data)`](#member-5e5912879471) | Decodes complete little-endian Vector3 values into independent storage. |
| [`public static Electron2D.Vector4[] ToVector4Array(System.ReadOnlySpan<System.Byte> data)`](#member-acd5e2c93c95) | Decodes complete little-endian Vector4 values into independent storage. |

## Member descriptions

<a id="member-ed299fe8b9c1"></a>
### `public static System.Void BSwap16(System.Span<System.Byte> data, System.Int32 offset = 0, System.Int32 count = -1)`

Reverses bytes within complete 16-bit segments in place.

Parameters:

- `data`: Borrowed mutable bytes.
- `offset`: First segment byte, possibly unaligned.
- `count`: Segment count; any negative value processes all complete remaining segments, preserving trailing bytes.

Errors:

- `T:System.ArgumentOutOfRangeException`: The selected range is invalid; no bytes change.

<a id="member-d435a52b5aea"></a>
### `public static System.Void BSwap32(System.Span<System.Byte> data, System.Int32 offset = 0, System.Int32 count = -1)`

Reverses bytes within complete 32-bit segments in place.

Parameters:

- `data`: Borrowed mutable bytes.
- `offset`: First segment byte, possibly unaligned.
- `count`: Segment count; any negative value processes all complete remaining segments, preserving trailing bytes.

Errors:

- `T:System.ArgumentOutOfRangeException`: The selected range is invalid; no bytes change.

<a id="member-87d09f0c78b6"></a>
### `public static System.Void BSwap64(System.Span<System.Byte> data, System.Int32 offset = 0, System.Int32 count = -1)`

Reverses bytes within complete 64-bit segments in place.

Parameters:

- `data`: Borrowed mutable bytes.
- `offset`: First segment byte, possibly unaligned.
- `count`: Segment count; any negative value processes all complete remaining segments, preserving trailing bytes.

Errors:

- `T:System.ArgumentOutOfRangeException`: The selected range is invalid; no bytes change.

<a id="member-90e53a5f6697"></a>
### `public static System.Byte[] Compress(System.ReadOnlySpan<System.Byte> data, Electron2D.FileCompressionMode compressionMode = FastLz)`

Compresses bytes into the versioned FileAccess compressed container.

All five codecs share the existing file envelope; native codecs retain checksummed 64 KiB blocks. This is asset preparation, with allocations and no external raw-codec format promise.

A caller-owned compressed snapshot, readable by FileAccess.OpenCompressed.

Parameters:

- `data`: Borrowed source bytes; empty input returns an empty array.
- `compressionMode`: Codec identity stored in the container.

Errors:

- `T:System.ArgumentOutOfRangeException`: The codec is invalid.
- `T:System.PlatformNotSupportedException`: The native codec bundle is unavailable.

<a id="member-c8e85150bce2"></a>
### `public static System.Double DecodeDouble(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 64-bit IEEE floating-point value at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-24968655ab6e"></a>
### `public static System.Single DecodeFloat(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 32-bit IEEE floating-point value at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-4b92635c0bc6"></a>
### `public static System.Single DecodeHalf(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 16-bit IEEE floating-point value at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-9bce138909b3"></a>
### `public static System.Int16 DecodeS16(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 16-bit integer at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-a88668d156a9"></a>
### `public static System.Int32 DecodeS32(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 32-bit integer at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-8d9edb661d13"></a>
### `public static System.Int64 DecodeS64(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 64-bit integer at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-95bf0ea5f31b"></a>
### `public static System.SByte DecodeS8(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 8-bit integer at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-276b060e5fcc"></a>
### `public static System.UInt16 DecodeU16(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 16-bit integer at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-035a53b688da"></a>
### `public static System.UInt32 DecodeU32(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 32-bit integer at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-2dabfaa1a704"></a>
### `public static System.UInt64 DecodeU64(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 64-bit integer at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-288e7c2e6a3d"></a>
### `public static System.Byte DecodeU8(System.ReadOnlySpan<System.Byte> data, System.Int32 byteOffset)`

Reads a 8-bit integer at a byte offset.

The decoded value, including floating-point infinities and NaNs.

Parameters:

- `data`: Borrowed source bytes.
- `byteOffset`: First byte of the value.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span.

<a id="member-6b0e6490c8b2"></a>
### `public static System.Byte[] Decompress(System.ReadOnlySpan<System.Byte> data, System.Int32 bufferSize, Electron2D.FileCompressionMode compressionMode = FastLz)`

Decompresses a compressed snapshot subject to a caller-supplied output capacity.

A new array containing the actual decoded bytes.

Parameters:

- `data`: Borrowed versioned compressed container.
- `bufferSize`: Positive maximum decoded byte count.
- `compressionMode`: Required codec identity.

Errors:

- `T:System.ArgumentOutOfRangeException`: The capacity or codec is invalid.
- `T:System.IO.InvalidDataException`: The container is invalid, corrupt, mismatched or exceeds the capacity.
- `T:System.PlatformNotSupportedException`: The native codec bundle is unavailable.

<a id="member-05d3ec74696a"></a>
### `public static System.Byte[] DecompressDynamic(System.ReadOnlySpan<System.Byte> data, System.Int32 maxOutputSize, Electron2D.FileCompressionMode compressionMode = FastLz)`

Decompresses DEFLATE, GZip or Brotli without requiring the exact decoded size.

A new decoded snapshot; the stored length is checked before output allocation.

Parameters:

- `data`: Borrowed versioned compressed container.
- `maxOutputSize`: Positive output limit; -1 permits the managed array limit. Zero and smaller negatives reject.
- `compressionMode`: DEFLATE, GZip or Brotli. Native block codecs require Decompress instead.

Errors:

- `T:System.ArgumentOutOfRangeException`: The limit or codec is unsupported.
- `T:System.IO.InvalidDataException`: The container is invalid, corrupt, mismatched or exceeds the limit.

<a id="member-ae67dafdc71e"></a>
### `public static System.Void EncodeDouble(System.Span<System.Byte> data, System.Int32 byteOffset, System.Double value)`

Writes a 64-bit IEEE floating-point value at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-3e519384e637"></a>
### `public static System.Void EncodeFloat(System.Span<System.Byte> data, System.Int32 byteOffset, System.Single value)`

Writes a 32-bit IEEE floating-point value at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-12fd9fd6558f"></a>
### `public static System.Void EncodeHalf(System.Span<System.Byte> data, System.Int32 byteOffset, System.Single value)`

Writes a 16-bit IEEE floating-point value at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode; rounded to binary16.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-9191a2656b72"></a>
### `public static System.Void EncodeS16(System.Span<System.Byte> data, System.Int32 byteOffset, System.Int16 value)`

Writes a 16-bit integer at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-6cc7b3ab398f"></a>
### `public static System.Void EncodeS32(System.Span<System.Byte> data, System.Int32 byteOffset, System.Int32 value)`

Writes a 32-bit integer at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-0315e1409825"></a>
### `public static System.Void EncodeS64(System.Span<System.Byte> data, System.Int32 byteOffset, System.Int64 value)`

Writes a 64-bit integer at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-e36cb4c60dee"></a>
### `public static System.Void EncodeS8(System.Span<System.Byte> data, System.Int32 byteOffset, System.SByte value)`

Writes a 8-bit integer at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-f93d5d0e90c9"></a>
### `public static System.Void EncodeU16(System.Span<System.Byte> data, System.Int32 byteOffset, System.UInt16 value)`

Writes a 16-bit integer at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-18e631efafbb"></a>
### `public static System.Void EncodeU32(System.Span<System.Byte> data, System.Int32 byteOffset, System.UInt32 value)`

Writes a 32-bit integer at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-3826dcde27e6"></a>
### `public static System.Void EncodeU64(System.Span<System.Byte> data, System.Int32 byteOffset, System.UInt64 value)`

Writes a 64-bit integer at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-165597bbec4d"></a>
### `public static System.Void EncodeU8(System.Span<System.Byte> data, System.Int32 byteOffset, System.Byte value)`

Writes a 8-bit integer at a byte offset.

Parameters:

- `data`: Borrowed mutable destination.
- `byteOffset`: First byte of the value.
- `value`: Value to encode.

Errors:

- `T:System.ArgumentOutOfRangeException`: The complete value lies outside the span; no bytes change.

<a id="member-441609298762"></a>
### `public static System.String GetStringFromASCII(System.ReadOnlySpan<System.Byte> data)`

Maps Latin-1 bytes to characters, stopping at the first zero byte.

A new string.

Parameters:

- `data`: Borrowed text bytes.

<a id="member-a6f1e0fd6d23"></a>
### `public static System.String GetStringFromMultibyteChar(System.ReadOnlySpan<System.Byte> data, System.String encoding = "")`

Decodes a named .NET multibyte encoding with strict error handling.

A new string. The framework code-page provider is used locally without changing the process registry.

Parameters:

- `data`: Borrowed encoded bytes.
- `encoding`: A .NET encoding name; empty uses the Windows culture ANSI code page or UTF-8 on other hosts.

Errors:

- `T:System.ArgumentException`: The encoding is null, unknown or unavailable.
- `T:System.Text.DecoderFallbackException`: The text is malformed or truncated.

<a id="member-4c8e9a676e78"></a>
### `public static System.String GetStringFromUTF16(System.ReadOnlySpan<System.Byte> data)`

Decodes strict UTF-16, honoring a leading byte-order mark and otherwise assuming little endian.

A new string.

Parameters:

- `data`: Borrowed complete text bytes.

Errors:

- `T:System.Text.DecoderFallbackException`: The text is malformed or truncated.

<a id="member-4a96a2eda4be"></a>
### `public static System.String GetStringFromUTF32(System.ReadOnlySpan<System.Byte> data)`

Decodes strict UTF-32, honoring a leading byte-order mark and otherwise assuming little endian.

A new string.

Parameters:

- `data`: Borrowed complete text bytes.

Errors:

- `T:System.Text.DecoderFallbackException`: The text is malformed or truncated.

<a id="member-9fe52f272eea"></a>
### `public static System.String GetStringFromUTF8(System.ReadOnlySpan<System.Byte> data)`

Decodes strict UTF-8, removing an optional leading byte-order mark.

A new string.

Parameters:

- `data`: Borrowed text bytes; embedded zero characters are retained.

Errors:

- `T:System.Text.DecoderFallbackException`: The text is malformed or truncated.

<a id="member-8cb923fd0479"></a>
### `public static System.String GetStringFromWChar(System.ReadOnlySpan<System.Byte> data)`

Decodes native wide characters: UTF-16 on Windows, UTF-32 on other hosts.

A new string.

Parameters:

- `data`: Borrowed complete wide-character bytes.

Errors:

- `T:System.Text.DecoderFallbackException`: The text is malformed or truncated.

<a id="member-fc31064d0267"></a>
### `public static System.String HexEncode(System.ReadOnlySpan<System.Byte> data)`

Encodes every source byte as two lowercase hexadecimal characters.

A new hexadecimal string.

Parameters:

- `data`: Borrowed binary bytes.

<a id="member-6a93893c78da"></a>
### `public static System.Byte[] ToByteArray(System.ReadOnlySpan<Electron2D.Color> data)`

Encodes Color values into independent little-endian bytes.

A caller-owned byte array, with no live alias to the source.

Parameters:

- `data`: Borrowed values; each element occupies 16 bytes in component order.

Errors:

- `T:System.OverflowException`: The byte count exceeds the signed managed length range.

<a id="member-a70077d4d794"></a>
### `public static System.Byte[] ToByteArray(System.ReadOnlySpan<Electron2D.Vector2> data)`

Encodes Vector2 values into independent little-endian bytes.

A caller-owned byte array, with no live alias to the source.

Parameters:

- `data`: Borrowed values; each element occupies 8 bytes in component order.

Errors:

- `T:System.OverflowException`: The byte count exceeds the signed managed length range.

<a id="member-4d0bd84ddc4b"></a>
### `public static System.Byte[] ToByteArray(System.ReadOnlySpan<Electron2D.Vector3> data)`

Encodes Vector3 values into independent little-endian bytes.

A caller-owned byte array, with no live alias to the source.

Parameters:

- `data`: Borrowed values; each element occupies 12 bytes in component order.

Errors:

- `T:System.OverflowException`: The byte count exceeds the signed managed length range.

<a id="member-7556ca9d6d78"></a>
### `public static System.Byte[] ToByteArray(System.ReadOnlySpan<Electron2D.Vector4> data)`

Encodes Vector4 values into independent little-endian bytes.

A caller-owned byte array, with no live alias to the source.

Parameters:

- `data`: Borrowed values; each element occupies 16 bytes in component order.

Errors:

- `T:System.OverflowException`: The byte count exceeds the signed managed length range.

<a id="member-ae79ba753e7b"></a>
### `public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.Double> data)`

Encodes double values into independent little-endian bytes.

A caller-owned byte array, with no live alias to the source.

Parameters:

- `data`: Borrowed values; each element occupies 8 bytes.

Errors:

- `T:System.OverflowException`: The byte count exceeds the signed managed length range.

<a id="member-c7d954e31aca"></a>
### `public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.Int32> data)`

Encodes int values into independent little-endian bytes.

A caller-owned byte array, with no live alias to the source.

Parameters:

- `data`: Borrowed values; each element occupies 4 bytes.

Errors:

- `T:System.OverflowException`: The byte count exceeds the signed managed length range.

<a id="member-003018138740"></a>
### `public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.Int64> data)`

Encodes long values into independent little-endian bytes.

A caller-owned byte array, with no live alias to the source.

Parameters:

- `data`: Borrowed values; each element occupies 8 bytes.

Errors:

- `T:System.OverflowException`: The byte count exceeds the signed managed length range.

<a id="member-0bc9ea14fa30"></a>
### `public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.Single> data)`

Encodes float values into independent little-endian bytes.

A caller-owned byte array, with no live alias to the source.

Parameters:

- `data`: Borrowed values; each element occupies 4 bytes.

Errors:

- `T:System.OverflowException`: The byte count exceeds the signed managed length range.

<a id="member-b8160545e73f"></a>
### `public static System.Byte[] ToByteArray(System.ReadOnlySpan<System.String> data)`

Encodes consecutive UTF-8 strings, each followed by a zero byte.

A caller-owned byte array. No element count or length prefix is inserted.

Parameters:

- `data`: Borrowed strings, including empty strings and embedded zero characters.

Errors:

- `T:System.ArgumentNullException`: An element is null.
- `T:System.Text.EncoderFallbackException`: A string contains malformed UTF-16.
- `T:System.OverflowException`: The byte count exceeds the signed managed length range.

<a id="member-6edcc18142cb"></a>
### `public static Electron2D.Color[] ToColorArray(System.ReadOnlySpan<System.Byte> data)`

Decodes complete little-endian Color values into independent storage.

A caller-owned array, with no live alias to the source.

Parameters:

- `data`: Borrowed source; each element occupies 16 bytes in component order.

Errors:

- `T:System.ArgumentException`: The byte length is not a multiple of 16.

<a id="member-d2724aa6d294"></a>
### `public static System.Single[] ToFloat32Array(System.ReadOnlySpan<System.Byte> data)`

Decodes complete little-endian float values into independent storage.

A caller-owned array, with no live alias to the source.

Parameters:

- `data`: Borrowed source; each element occupies 4 bytes.

Errors:

- `T:System.ArgumentException`: The byte length is not a multiple of 4.

<a id="member-719dcf59764d"></a>
### `public static System.Double[] ToFloat64Array(System.ReadOnlySpan<System.Byte> data)`

Decodes complete little-endian double values into independent storage.

A caller-owned array, with no live alias to the source.

Parameters:

- `data`: Borrowed source; each element occupies 8 bytes.

Errors:

- `T:System.ArgumentException`: The byte length is not a multiple of 8.

<a id="member-d58e226cb046"></a>
### `public static System.Int32[] ToInt32Array(System.ReadOnlySpan<System.Byte> data)`

Decodes complete little-endian int values into independent storage.

A caller-owned array, with no live alias to the source.

Parameters:

- `data`: Borrowed source; each element occupies 4 bytes.

Errors:

- `T:System.ArgumentException`: The byte length is not a multiple of 4.

<a id="member-28048e03f7c0"></a>
### `public static System.Int64[] ToInt64Array(System.ReadOnlySpan<System.Byte> data)`

Decodes complete little-endian long values into independent storage.

A caller-owned array, with no live alias to the source.

Parameters:

- `data`: Borrowed source; each element occupies 8 bytes.

Errors:

- `T:System.ArgumentException`: The byte length is not a multiple of 8.

<a id="member-311e4320d9a9"></a>
### `public static Electron2D.Vector2[] ToVector2Array(System.ReadOnlySpan<System.Byte> data)`

Decodes complete little-endian Vector2 values into independent storage.

A caller-owned array, with no live alias to the source.

Parameters:

- `data`: Borrowed source; each element occupies 8 bytes in component order.

Errors:

- `T:System.ArgumentException`: The byte length is not a multiple of 8.

<a id="member-5e5912879471"></a>
### `public static Electron2D.Vector3[] ToVector3Array(System.ReadOnlySpan<System.Byte> data)`

Decodes complete little-endian Vector3 values into independent storage.

A caller-owned array, with no live alias to the source.

Parameters:

- `data`: Borrowed source; each element occupies 12 bytes in component order.

Errors:

- `T:System.ArgumentException`: The byte length is not a multiple of 12.

<a id="member-acd5e2c93c95"></a>
### `public static Electron2D.Vector4[] ToVector4Array(System.ReadOnlySpan<System.Byte> data)`

Decodes complete little-endian Vector4 values into independent storage.

A caller-owned array, with no live alias to the source.

Parameters:

- `data`: Borrowed source; each element occupies 16 bytes in component order.

Errors:

- `T:System.ArgumentException`: The byte length is not a multiple of 16.

## Verification and limits

See the [public consumer and verification boundary](../components/byte-buffers.md). Pure byte conversion needs no renderer. Foreign hosts, native allocator accounting and large-data performance remain separate.
