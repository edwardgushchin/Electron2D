# GZIPContext

Last updated: 2026-10-28

**Visibility:** internal. **Source:** [StreamPeerGZIP.cs](../../src/Core/Networking/StreamPeerGZIP.cs). **Component:** [HTTP](../components/http.md).

## Responsibilities

SafeHandle owner for an opaque native bridge context and all internal zlib storage. The shared owned-native allocator counts their requested payload at actual allocation/free boundaries. No runtime-specific PAL or z_stream layout crosses the ABI. Reset at concatenated gzip boundaries reuses prepared state; validation errors never claim completion.

StreamPeerGZIP selects window bits 15 for zlib and 31 for gzip. Native creation cleans partial initialization before reporting the original error. Explicit release and finalization retire all owned storage once. Borrowed spans are pinned only for Process and native pointers are cleared before returning.

## Verification

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) exercises public wire/framing/error/backpressure behavior and prepared zero-byte calls. [GZIPCleanupTests](../../tests/Electron2D.Tests/GZIPCleanupTests.cs) checks codec accounting, invalid windows, repeated disposal, public clear and finalization. The [production native oracle](../../tools/enet-native/check_memory.c) injects failure at every native allocation position and cross-checks stock zlib wire bytes. Complete composed memory and foreign-host qualification remain open.
