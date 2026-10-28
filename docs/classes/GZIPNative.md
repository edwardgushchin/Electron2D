# GZIPNative

Last updated: 2026-10-28

**Visibility:** internal. **Source:** [StreamPeerGZIP.cs](../../src/Core/Networking/StreamPeerGZIP.cs). **Component:** [HTTP](../components/http.md).

## Responsibilities

Private source-generated C imports for create/process/reset/destroy of opaque gzip/zlib contexts in the existing ENet/file-codec bridge. Only pinned input/output spans and fixed-width counts cross each call. Matching exports are required by native packaging, publish checks and the desktop preflight; no runtime compression PAL layout or new dependency is used.

## Verification

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) exercises public API integration, ownership/framing/codec/error boundaries and prepared allocation scopes. Other hosts, routed throughput, native codec internals and owner acceptance remain separate.
