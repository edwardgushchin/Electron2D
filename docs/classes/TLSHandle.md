# TLSHandle

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [TLSNative.cs](../../src/Core/Networking/TLSNative.cs). **Component:** [TLS](../components/tls.md).

## Responsibilities

Internal deterministic SafeHandle owner for one OpenSSL context, session, BIO, certificate or key. Context/session references and BIO ownership transfer follow the native contract. Abandoned handles finalize; StreamPeerTLS also finalizes resource-use retention without touching a borrowed stream.

## Verification

[TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) executes native TLS over fragmented public streams and TCP, trust/name/failure/lifetime/finalization paths and independent SslStream interoperability. Prepared active/idle cycles measure managed allocation; external OpenSSL native allocation totals and other hosts remain unverified.
