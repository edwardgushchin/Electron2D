# HTTPProxySettings

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [HTTPRequest.cs](../../src/Core/Networking/HTTPRequest.cs). **Component:** [HTTP](../components/http.md).

## Responsibilities

Immutable four-field proxy snapshot published across scene/worker owners; each owner applies it to its own HTTP client.

## Verification

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) exercises public API integration, ownership/framing/codec/error boundaries and prepared allocation scopes. Other hosts, routed throughput, native codec internals and owner acceptance remain separate.
