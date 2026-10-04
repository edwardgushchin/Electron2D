# HTTPTransfer

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [HTTPTransfer.cs](../../src/Core/Networking/HTTPTransfer.cs). **Component:** [HTTP](../components/http.md).

## Responsibilities

One operation-owned HTTP client, copied request/configuration, progressive decoder/output and atomic destination staging. Constructing-thread transport state never crosses owners; worker flags/counters and completion use explicit publication. Cancellation and output commit serialize through the lifecycle gate.

## Verification

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) exercises public API integration, ownership/framing/codec/error boundaries and prepared allocation scopes. Other hosts, routed throughput, native codec internals and owner acceptance remain separate.
