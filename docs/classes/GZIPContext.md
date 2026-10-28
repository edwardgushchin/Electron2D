# GZIPContext

Last updated: 2026-10-28

**Visibility:** internal. **Source:** [StreamPeerGZIP.cs](../../src/Core/Networking/StreamPeerGZIP.cs). **Component:** [HTTP](../components/http.md).

## Responsibilities

SafeHandle owner for prepared unmanaged compression PAL state and native inflate/deflate lifecycle. Reset at concatenated gzip boundaries preserves prepared storage; validation errors never claim completion.

The internal constructor receives the native window-bit value; StreamPeerGZIP
selects 15 for zlib and 31 for gzip. Failed initialization retires an existing
runtime-owned inner record before releasing the engine-owned outer record.
Ordinary retirement shares that cleanup; null inner state never enters native End.

## Verification

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) exercises public API integration, ownership/framing/codec/error boundaries and prepared allocation scopes. Other hosts, routed throughput, native codec internals and owner acceptance remain separate.

[GZIPCleanupTests](../../tests/Electron2D.Tests/GZIPCleanupTests.cs) exercises actual
native initialization failures, preserved error results and normal/repeated
disposal; its optional calibrated native probe detects the leaked inner record.
