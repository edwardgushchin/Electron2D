# GZIPNative

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [StreamPeerGZIP.cs](../../src/Core/Networking/StreamPeerGZIP.cs). **Component:** [HTTP](../components/http.md).

## Responsibilities

Private source-generated imports into the compression shim already delivered with the .NET runtime. The pinned .NET 10 ABI and self-contained native gate are separate from public stream semantics; no new external package or vendor source is added.

## Verification

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) exercises public API integration, ownership/framing/codec/error boundaries and prepared allocation scopes. Other hosts, routed throughput, native codec internals and owner acceptance remain separate.
