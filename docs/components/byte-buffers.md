# Binary buffers

Last updated: 2026-10-27

[ByteBuffer](../classes/ByteBuffer.md) supplies scalar byte-offset encoding, in-place byte swapping, strict text decoding, typed array conversion in both directions and compression over caller-owned spans. [ADR 0020](../decisions/core-data-io.md#caller-owned-binary-buffers) owns byte order, text and container adaptations. Existing arrays and lists remain the collection API.

Numeric storage is little endian, independent of native alignment. Scalar/swap operations allocate no managed storage. Returned strings, arrays and compressed bytes are independent preparation snapshots. Invalid complete ranges fail before writes; array conversion requires a whole number of elements. Color uses four binary32 RGBA components. Numeric Vector3 remains ordinary data.

Compression shares [FileAccess](file-access.md)'s E2DCMP container, including codec identity, declared length and native checksummed blocks. Always supply a suitable output limit for untrusted input. This container is not a raw external codec stream. Native FastLZ/Zstandard use the existing private codec bundle; no socket or new dependency is created. Other codecs use the framework. Empty compression returns empty bytes, which cannot be decoded as a container.

The public-only [consumer](../../tests/ByteBuffers.Consumer/ByteBufferTests.cs) exercises all 52 operations, unaligned golden byte vectors, signed boundaries and IEEE special values, failed-range atomicity, malformed Unicode/BOM/legacy encodings, whole-element validation, reverse golden bytes, zero-terminated UTF-8 string arrays and copied storage. It crosses the 16-byte native padding and 64 KiB block boundaries for every codec, rejects forged sizes, wrong codecs and corruption, reads/writes real compressed files in both directions and transfers a typed packet through actual UDP loopback and StreamPeerBuffer. It also checks 4096 warmed scalar/swap intervals for zero owner-thread managed bytes. Foreign hosts, native allocation accounting and large-data performance need separate acceptance.

Verification runs on Linux x64 with CLR and strict NativeAOT (`PublishAot=true`, `TrimMode=full`). Both public consumers run from `/tmp` with `LD_LIBRARY_PATH` unset and use the packaged runtime codec directory. The shared FileAccess regression also passes 110 empty/tiny/block-boundary/multi-block cases, edits, corruption, atomic failure and fresh-process reads.
