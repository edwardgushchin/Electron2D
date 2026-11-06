# System certificate export

Last updated: 2026-11-06

OS.GetSystemCACertificates returns a fresh PEM snapshot of operating-system CA encodings. It opens no window, creates no graphics device and changes no trust store. [ADR 0113](../decisions/host-platform.md#system-ca-snapshots) owns source selection, failure policy and the distinction between anchor bytes and native trust decisions.

## Using the snapshot

```csharp
using var anchors = new X509Certificate();
anchors.LoadFromString(OS.GetSystemCACertificates());
using var client = TLSOptions.Client(anchors);
```

An empty export means that a supported source has no anchors; loading empty text into X509Certificate rejects it. The returned string is caller-owned public material. X509Certificate copies it; TLSOptions borrows that resource and an active TLS session retains it. Keep resources alive until the session disconnects. HTTPS, WSS and DTLS consumers accept the same explicitly selected TLSOptions trust chain.

The ordinary TLSOptions.Client() path continues to validate through live OS chain policy. An exported custom snapshot fixes anchor bytes at query time and does not carry every native usage restriction, later disallow decision or revocation policy. Exporting certificates does not select that snapshot automatically or make a generated/self-signed certificate trusted.

## Sources and lifetime

Windows enumerates the effective current-user ROOT collection, including inherited machine roots, with read-only/existing-only flags. It skips arrived disallow timestamps and empty disallow markers. The zero-byte form occurs in real Windows stores and is never interpreted as unwritten timestamp data. Future timestamps remain eligible. Unexpected native metadata sizes or property/open/enumeration errors abort with IOException. Every enumerated context and store closes on success and failure; no native pointer escapes.

Desktop Linux/FreeBSD return the first existing standard bundle using the order in the ADR, without a bundled fallback or virtual project path. Missing bundles return empty; reading a selected bundle can fail. macOS copies native anchors and DER, releasing each copied CFData and the anchor collection. Mobile/TV/browser export adapters remain missing and reject explicitly. Each call takes its own native/file snapshot, so concurrent calls share no mutable export storage.

## Verification boundaries

[SystemCAChecks](../../tests/Electron2D.Tests/SystemCAChecks.cs) is shared by the root profile and the [public consumer](../../tests/SystemCA.Consumer/README.md). Its independent Windows SDK inspector enumerates real native roots and exports DER for a hash-set comparison, independent of managed PEM encoding. Checks also load/roundtrip the whole bundle through X509Certificate, run concurrent exports and select ordinary validating TLSOptions.

The optional live-host profile exercises actual native TLS with the exported snapshot and default system trust, receives HTTP bytes, and rejects an unrelated private anchor and a mismatching hostname. It depends on reachable public DNS/TCP/TLS and the host's current trust configuration. Its receipt states the endpoint and outcome; an offline parse is not a successful network handshake.

On Windows build 26300, source-root, a fresh Core package and strict full-trim NativeAOT each passed 19 public checks with 96 independently observed eligible roots and four TLS controls. The SDK excluded 11 disallow entries, including two empty markers. Exported/default trust both received HTTP bytes from example.com; an unrelated anchor rejected, and a private independent SslStream server established the hostname-mismatch classification without public-server SNI routing. All seven isolated native-source modes passed on CLR and strict NativeAOT with complete context/store drain. These are bounded local Windows receipts.

[SystemCA.NativeChecks](../../tests/SystemCA.NativeChecks/README.md) compiles the exact producer source in an isolated test assembly, with only a permanent-owner shell and a private native resolver. The production assembly already owns its resolver; no production test hook is added. The fixture supplies real private CryptoAPI memory stores and validates readonly opening, past/future/empty disallow metadata, empty sources, source changes and failed native reads. These controlled source checks are separate from testing the actual engine DLL/package/AOT against real stores.

Unix/macOS source, mobile/TV/browser adapters, foreign native/package execution, complete native allocation accounting and the full ADR 0021 matrix remain unqualified. A declaration's implementation count does not close those gates.
