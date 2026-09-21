# FileCompressionMode

Last updated: 2026-09-21

## Source and declaration

- Source: [`src/Core/IO/FileCompressionMode.cs`](../../src/Core/IO/FileCompressionMode.cs)
- Declaration: `public enum FileCompressionMode`
- Assembly and namespace: `Electron2D.dll`, `Electron2D`

## Responsibility and values

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

## Lifecycle, ownership, and threading

The value owns no state. Codec streams are created and disposed inside one complete container decode or encode operation under the owning `FileAccess` lock.

## Dependencies and limitations

Implemented codecs use `System.IO.Compression`. The container is Electron2D-specific and is not a transparent reader for arbitrary raw `.gz`, Brotli, DEFLATE, or another engine's compressed-file envelope.

## Verification

The executable harness round-trips all three available codecs, verifies intermediate flush, cross-instance reads, malformed envelopes, and explicit failures for FastLZ and Zstandard.
