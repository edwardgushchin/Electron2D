# FileCompressionMode

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/IO/FileCompressionMode.cs`](../../src/Core/IO/FileCompressionMode.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum FileCompressionMode`

> Identifies the codec used by a compressed [`FileAccess`](FileAccess.md) container.

## Description

Identifies the codec used by a compressed [`FileAccess`](FileAccess.md) container.

The enum identifies the codec recorded in a whole-file compressed `FileAccess` container.

| Value | Number | Current state |
| --- | ---: | --- |
| `FastLz` | 0 | Defined for API/format identity; explicitly rejected because no codec provider is integrated |
| `Deflate` | 1 | Read and write implemented with `DeflateStream` |
| `Zstandard` | 2 | Defined for API/format identity; explicitly rejected because no codec provider is integrated |
| `Gzip` | 3 | Read and write implemented with `GZipStream` |
| `Brotli` | 4 | Read and write implemented with `BrotliStream` |

Unknown numeric values throw `ArgumentOutOfRangeException`; unavailable known codecs throw `NotSupportedException` before a file is created or read.

Brotli write support is a deliberate BCL-backed Electron2D extension; the reference contract promises this mode only for decompression.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = FileCompressionMode.FastLz;
```

## Constants

| Member | Description |
| --- | --- |
| [`FastLz = 0`](#f-electron2d-filecompressionmode-fastlz) | Uses the FastLZ codec. |
| [`Deflate = 1`](#f-electron2d-filecompressionmode-deflate) | Uses the DEFLATE codec. |
| [`Zstandard = 2`](#f-electron2d-filecompressionmode-zstandard) | Uses the Zstandard codec. |
| [`Gzip = 3`](#f-electron2d-filecompressionmode-gzip) | Uses the GZip container and DEFLATE codec. |
| [`Brotli = 4`](#f-electron2d-filecompressionmode-brotli) | Uses the Brotli codec. |

## Constant Descriptions

<a id="f-electron2d-filecompressionmode-fastlz"></a>
### `FastLz = 0`

Uses the FastLZ codec. The current runtime has no FastLZ provider.

<a id="f-electron2d-filecompressionmode-deflate"></a>
### `Deflate = 1`

Uses the DEFLATE codec.

<a id="f-electron2d-filecompressionmode-zstandard"></a>
### `Zstandard = 2`

Uses the Zstandard codec. The current runtime has no Zstandard provider.

<a id="f-electron2d-filecompressionmode-gzip"></a>
### `Gzip = 3`

Uses the GZip container and DEFLATE codec.

<a id="f-electron2d-filecompressionmode-brotli"></a>
### `Brotli = 4`

Uses the Brotli codec.

## Lifecycle, ownership, and threading

The value owns no state. Codec streams are created and disposed inside one complete container decode or encode operation under the owning `FileAccess` lock.

## Dependencies and limitations

Implemented codecs use `System.IO.Compression`. The container is Electron2D-specific and is not a transparent reader for arbitrary raw `.gz`, Brotli, DEFLATE, or another engine's compressed-file envelope.

## Verification

The executable harness round-trips all three available codecs, verifies intermediate flush, cross-instance reads, malformed envelopes, and explicit failures for FastLZ and Zstandard.
