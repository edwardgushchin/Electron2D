# GZIPState

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [StreamPeerGZIP.cs](../../src/Core/Networking/StreamPeerGZIP.cs). **Component:** [HTTP](../components/http.md).

## Responsibilities

Private sequential ABI projection of the .NET 10 compression PAL stream. Input/output pointers are pinned for one call and cleared afterward; unmanaged codec state is retained privately.

## Verification

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) exercises public API integration, ownership/framing/codec/error boundaries and prepared allocation scopes. Other hosts, routed throughput, native codec internals and owner acceptance remain separate.
