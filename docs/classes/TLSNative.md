# TLSNative

Last updated: 2026-10-29

**Visibility:** internal. **Source:** [TLSNative.cs](../../src/Core/Networking/TLSNative.cs). **Component:** [TLS](../components/tls.md).

## Responsibilities

Internal source-generated OpenSSL imports, context/trust/server-identity setup, DNS/IP/SNI verification, bounded BIO-pair preparation, native record and error handling. Native pointers never enter public API. Desktop targets use the shared [OpenSSL 4.1.0 source profile](../components/tls.md#unified-openssl-source-profile). Linux checks the BCL provider before loading the engine pair, rejecting another version; stock runtime process configuration remains a deployment prerequisite. [TLSNative.SystemTrust.cs](../../src/Core/Networking/TLSNative.SystemTrust.cs) uses .NET OS-chain validation for Windows/macOS system-trust clients before OpenSSL name/purpose verification. Custom CA and unsafe options retain their existing policy.

The native certificate callback restores DNS SAN precedence and CN fallback only without DNS SANs; IP SAN checks retain native semantics. Exceptions remain contained at the callback boundary and temporary decoded names are released.

Every OpenSSL import explicitly uses Cdecl. C `long`/`unsigned long` parameters use `CLong`/`CULong`; results use scalar integer registers and explicit Windows narrowing, preserving LLP64/LP64/ILP32 without the Unix x86 struct-return convention. The native `uint64_t` option mask remains a 64-bit value. Windows full-suite execution is connected but pending.

Android loads private OpenSSL libraries from the SDK application bundle and uses the same bounded OS-chain callback for system trust. Its native host exercises custom trusted records, hostname rejection and rejection of an unknown root by system trust. Earlier x64/ARM64 execution passed before the OpenSSL 4.1.0 upgrade; current target execution, 32-bit and CI acceptance remain required.

## Verification

iOS/tvOS statically link the private OpenSSL closure and use the same bounded .NET OS-chain callback before native policy/name validation. Apple app tests exercise trusted records, hostname rejection and unknown OS-root rejection; actual SDK/simulator results remain required. Import constants preserve ordinary desktop/Android names and select `__Internal` only in Apple target compilation.

[TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) executes native TLS over fragmented public streams and TCP, trust/name/failure/lifetime/finalization paths and independent SslStream interoperability. Prepared active/idle cycles measure managed allocation; external OpenSSL native allocation totals and other hosts remain unverified.

[TLSSystemTrustTests](../../tests/Electron2D.Tests/TLSSystemTrustTests.cs) directly exercises the native verification context: an OS anchor succeeds, while an unknown root or missing certificate fails. The callback is bounded to 64 supplied certificates and 1 MiB per DER certificate, disables certificate downloads/revocation fetching, catches exceptions before returning to native code, and never modifies OS trust stores. Linux regressions passed; macOS executable integration remains pending.

DTLS additionally probes datagram-BIO capability before setup, prepares fixed 64 KiB datagram pairs/MTU, cookie callbacks and native timer progress. Existing stream sessions retain their original BIO preparation. [DTLS](../components/dtls.md) documents packet ownership and independent process verification.
