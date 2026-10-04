# TLSNative

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [TLSNative.cs](../../src/Core/Networking/TLSNative.cs). **Component:** [TLS](../components/tls.md).

## Responsibilities

Internal source-generated OpenSSL 3 imports, context/trust/server-identity setup, DNS/IP/SNI verification, bounded BIO-pair session preparation, native record and error handling. Native pointer identities never enter the public API. Available only with Linux system libssl.so.3/libcrypto.so.3; no bundled OpenSSL source/binary.

## Verification

[TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) executes native TLS over fragmented public streams and TCP, trust/name/failure/lifetime/finalization paths and independent SslStream interoperability. Prepared active/idle cycles measure managed allocation; external OpenSSL native allocation totals and other hosts remain unverified.
