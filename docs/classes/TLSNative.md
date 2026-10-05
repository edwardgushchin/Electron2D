# TLSNative

Last updated: 2026-10-05

**Visibility:** internal. **Source:** [TLSNative.cs](../../src/Core/Networking/TLSNative.cs). **Component:** [TLS](../components/tls.md).

## Responsibilities

Internal source-generated OpenSSL 3 imports, context/trust/server-identity setup, DNS/IP/SNI verification, bounded BIO-pair preparation, native record and error handling. Native pointers never enter public API. Linux uses system `libssl.so.3`/`libcrypto.so.3`; macOS resolves private OpenSSL 3.6.4 dylibs from its audited native package. [TLSNative.SystemTrust.cs](../../src/Core/Networking/TLSNative.SystemTrust.cs) uses .NET/Keychain chain validation for macOS system-trust clients before OpenSSL name/purpose verification. Custom CA and unsafe options retain their existing policy.

Every OpenSSL import explicitly uses Cdecl. C `long`/`unsigned long` parameters and results use `CLong`/`CULong`, preserving LLP64/LP64 widths; OpenSSL 3's `uint64_t` option mask remains a 64-bit value. This ABI correction does not enable the still-gated Windows backend.

## Verification

[TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) executes native TLS over fragmented public streams and TCP, trust/name/failure/lifetime/finalization paths and independent SslStream interoperability. Prepared active/idle cycles measure managed allocation; external OpenSSL native allocation totals and other hosts remain unverified.

[TLSSystemTrustTests](../../tests/Electron2D.Tests/TLSSystemTrustTests.cs) directly exercises the native verification context: an OS anchor succeeds, while an unknown root or missing certificate fails. The callback is bounded to 64 supplied certificates and 1 MiB per DER certificate, disables certificate downloads/revocation fetching, catches exceptions before returning to native code, and never modifies OS trust stores. Linux regressions passed; macOS executable integration remains pending.

DTLS additionally probes OpenSSL 3.2 datagram-BIO capability before setup, prepares fixed 64 KiB datagram pairs/MTU, cookie callbacks and native timer progress. Existing stream sessions retain their original BIO preparation. [DTLS](../components/dtls.md) documents packet ownership and independent process verification.
