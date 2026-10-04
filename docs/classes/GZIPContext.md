# GZIPContext

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [StreamPeerGZIP.cs](../../src/Core/Networking/StreamPeerGZIP.cs). **Component:** [HTTP](../components/http.md).

## Responsibilities

SafeHandle owner for prepared unmanaged compression PAL state and native inflate/deflate lifecycle. Reset at concatenated gzip boundaries preserves prepared storage; validation errors never claim completion.

## Verification

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) exercises public API integration, ownership/framing/codec/error boundaries and prepared allocation scopes. Other hosts, routed throughput, native codec internals and owner acceptance remain separate.
