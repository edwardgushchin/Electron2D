# TLS streams and security resources

Last updated: 2026-10-04

## Surface and dependency direction

[X509Certificate](../classes/X509Certificate.md) owns ordered copied certificate bytes; [CryptoKey](../classes/CryptoKey.md) owns canonical RSA/EC private or public key bytes. Both inherit Resource and use BCL cryptography for PEM/DER parsing, engine FileAccess paths, transactional failures, synchronous Changed and copied duplication. Certificate loads append valid chains; key loads replace a key. ResourceLoader recognizes `.crt` certificates and `.key` private keys for typed discovery, weak reuse/ignore and decode-before-replace. The [Crypto component](crypto.md) generates RSA keys and RSA/EC self-signed identities and performs signing/encryption through these resources. Generic ResourceSaver, serialized scenes and an editor remain separate capabilities.

[TLSOptions](../classes/TLSOptions.md) is an immutable ElectronObject configured through Client, ClientUnsafe or Server. It borrows resource references and preserves their identities. [StreamPeerTLS](../classes/StreamPeerTLS.md) inherits StreamPeer and borrows any StreamPeer, including native TCP/UDS or consumer-defined streams. [TLSStatus](../classes/TLSStatus.md) separates disconnected, handshaking, connected, generic error and name/IP mismatch phases. [TLSNative](../classes/TLSNative.md) and [TLSHandle](../classes/TLSHandle.md) are private backend implementation.

## Executable flow

A connected transport is supplied to ConnectToStream or AcceptStream. The first call prepares native context/session and bounded record I/O, retains resources and starts a nonblocking handshake. Poll advances handshake, feeds encrypted bytes from caller-thread partial reads, flushes encrypted output and peeks application records without consuming plaintext. GetStatus and GetAvailableBytes are cached/native-buffer queries; poll regularly before readiness checks. GetAvailableBytes reports buffered decrypted bytes, not underlying ciphertext.

Full reads/writes can wait until byte progress/failure. Partial operations return immediate plaintext progress; accepted outgoing plaintext may remain encrypted in the prepared output queue until Poll or further I/O flushes it. TLS record boundaries are internal; application data remains an ordered byte stream, so existing StreamPeer number/string codecs and PacketPeerStream framing apply. Do not mutate/reorder an unconsumed send prefix between retries. A remote close notification preserves preceding application bytes and then disconnects TLS. An abrupt underlying stream EOF is a TLS record error rather than an authenticated graceful close.

Disconnect/Dispose performs best-effort close notification when the transport permits it, releases native context/session/BIO ownership and resource retention, and leaves the borrowed stream alive. Error cleanup similarly preserves that stream and keeps a diagnostic TLSStatus while GetStream becomes null. Explicit disconnect resets status. A new session can reuse a still-open raw transport. Abandoned TLS peers finalize native handles and retention without invoking stream methods; deterministic Dispose remains the intended lifecycle.

## Trust, data and failure boundaries

Client validates the certificate chain against system trust or custom CA certificates, and compares the expected DNS name/IP (with SAN support) or CommonNameOverride. An explicitly empty expected name requests chain-only validation. ClientUnsafe suppresses name checks; a supplied custom chain still requires chain verification, while null disables certificate verification. Server configuration requires a nonempty ordered leaf/intermediate chain and matching private key. System crypto policy and certificate purpose/expiry rules remain authoritative. Initialization, certificate/name and record errors use AuthenticationException; only actual DNS/IP mismatch sets ErrorHostnameMismatch. Invalid roles, disposed/self streams and NUL names reject explicitly.

TLS operations and disposal require the constructing thread; no worker thread invokes the borrowed stream. Keys/certificates are locked against payload replacement, copy/reset and disposal while sessions retain them. Option disposal never disposes resources. Parsing commits only after validation; observer failure propagates once after committed data. Private key buffers are cleared on replacement/disposal/finalization and temporary DER file input is cleared. Exported PEM strings are caller-owned immutable managed values and cannot be zeroized by the resource.

Certificate PEM chains preserve every certificate during export, correcting the pinned file saver which repeated the first certificate while traversing the chain. Malformed certificate chains reject transactionally rather than accepting a partially parsed chain. Private key exports use canonical PKCS#8; public exports use SubjectPublicKeyInfo. Input accepts RSA PKCS#1/PKCS#8/SPKI and EC formats through the BCL. The engine does not expose passwords for encrypted private-key PEM; those inputs fail explicitly.

## Backend and preparation

The current TLS backend directly loads system OpenSSL 3 on 64-bit Linux (`libssl.so.3`, `libcrypto.so.3`). TLS 1.2 is the minimum; TLS 1.3 negotiates when supported by peer/system policy. Native pointers/options never enter public API. Source-generated imports plus SafeHandles manage deterministic releases and ownership transfer. A 64 KiB native BIO pair per direction and 16 KiB input/64 KiB output managed buffers are prepared at session construction/start. Repeated successful engine record/poll operations do not grow buffers or schedule Tasks. Setup, parsing, certificate configuration and handshakes are cold allocating work.

OpenSSL is host-provided and is not redistributed. Linux TLS delivery requires its libraries and appropriate system CA installation. Other targets need backend/native packaging plus executable fidelity; unavailable hosts reject with PlatformNotSupportedException. Resource parsing/serialization has the BCL's platform capabilities independently of native stream TLS. See [ADR 0094](../decisions/networking.md#adr-0094), [third-party integrations](../thirdparty.md) and [the platform matrix](../platform-verification.md).

## Verification

[TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) exercises RSA/EC PEM, private/public role, DER files, ordered chains, copied Resource state, loader discovery/cache/replace/failure, observer failure, resource-use rejection/release and abandoned-session finalization. Actual Linux OpenSSL TLS exchanges run over fragmented public StreamPeer and TCP, including 128 KiB partial transfer, DNS/IP/override, valid/wrong/system trust, expired identity, unsafe with/without required custom trust, matching-key rejection, close notification, preceding plaintext at FIN and a second TLS session over preserved TCP. Independent .NET SslStream peers verify both client and server wire interoperability without relying on Electron2D's TLS implementation.

64 prepared active number/read/write/poll cycles and 64 idle status/availability/poll cycles measure zero managed allocated bytes on the owner thread. Native BIO capacity is fixed; OpenSSL internal native allocation totals, routed traffic/throughput, system CA variation, other platforms and owner acceptance are unverified. No renderer or unified editor/project tool acceptance is inferred from these network tests.

A self-contained linux-x64 test application was published to `/tmp/e2d-tls-native-publish` and its TLS selector executed from `/tmp` with LD_LIBRARY_PATH unset. TLS 1.2/1.3 interoperability and all focused resource/lifetime/allocation checks passed using packaged .NET runtime files and host OpenSSL 3.6.4. This confirms the exercised local deployment profile, not other hosts or an external consumer application.

CryptoTests generates a key/certificate through public Crypto methods, saves/reloads them through the existing resource paths and establishes a native TLS server with an independent validating SslStream client. Crypto imports independent locked key snapshots, allowing signing while a session retains the resource; active-session replacement/disposal remains rejected. See [the crypto contract](crypto.md).

[Packet DTLS](dtls.md) reuses this identity/trust/native lifetime implementation with shared TLSStatus and independent datagram BIO, cookie and timer behavior. TLS 1.2/1.3 streams keep their original BIO profile; DTLS requires OpenSSL 3.2+ and negotiates the current DTLS 1.2 profile.
