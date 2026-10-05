# Native DTLS packets and server cookies

Last updated: 2026-10-04

## Surface and dependency direction

[PacketPeerDTLS](../classes/PacketPeerDTLS.md) extends [PacketPeer](../classes/PacketPeer.md), borrowing one live connected [PacketPeerUDP](../classes/PacketPeerUDP.md). [DTLSServer](../classes/DTLSServer.md) owns prepared server identity and cookie-secret state, converting connected UDP peers, including accepted [UDPServer](../classes/UDPServer.md) endpoints, into caller-owned DTLS peers. Neither wrapper owns/closes the UDP socket. [TLSOptions](../classes/TLSOptions.md), CryptoKey and X509Certificate retain their existing identity/trust/lifetime contracts. Shared [TLSStatus](../classes/TLSStatus.md) represents the same disconnected/handshaking/connected/error/name-mismatch domain under ADR 0051.

The private [TLSNative](../classes/TLSNative.md) backend reuses certificate/key import, custom/system trust, hostname/IP/override and native SafeHandles. Native 64-bit Linux DTLS uses system OpenSSL 3.2 or later for fixed datagram BIO pairs; macOS uses packaged private OpenSSL 3.6.4 and .NET/Keychain system-chain validation as described in [TLS](tls.md). DTLS 1.2 is the current negotiated profile. Missing backend/capability rejects explicitly. macOS source/package production passed, but its new executable DTLS profile remains pending. Stream TLS retains its original BIO and TLS 1.2/1.3 profile. No public native handle is exposed. Automatic revocation fetching and a separate certificate-pinning API remain absent.

## Construction, identity and ownership

Constructors prepare fixed encrypted/decoded buffers and a bounded packet queue. Setup is cold work. DTLSServer.Setup accepts only server options and rejects repeat successful setup; failure releases partial native/resource state and permits a later valid attempt. It imports a matching private key/leaf/intermediates and generates a fresh 32-byte cookie secret. Setup consumes the options; key/certificate payloads remain retained until helper cleanup. TakeConnection validates a connected same-thread UDP endpoint and constructs a DTLS peer; accepted sessions independently retain the identity and survive helper disposal. The helper retains no client collection or transport.

Client ConnectToPeer requires a live connected UDP transport and client options; null selects system trust. Expected-name override, DNS/IP verification and ClientUnsafe follow the existing TLS contract: unsafe disables expected-name validation, while a supplied custom chain still validates. Null unsafe trust disables chain/name checks. Setup/handshake failures report typed exceptions, release session/security retention and preserve UDP ownership; only actual DNS/IP certificate mismatch produces ErrorHostnameMismatch. Programming/configuration misuse validates before replacing state. Calls/disposal use the constructing thread. Poll/send/disconnect/disposal reentry guards preserve live native state during transport extension callbacks.

A UDP connection generation is captured on attachment. Closing, listener detachment or reopening that UDP object invalidates the DTLS attachment before further network use, even when the endpoint is reused. Other raw access to the borrowed UDP transport remains the caller's exclusive-use responsibility. Server/helper/peer finalization releases abandoned native state and resource-use retention without calling UDP; deterministic disposal remains the normal path.

## Cookies, handshake and timers

The server uses OpenSSL cookie exchange before its full handshake flight. A 32-byte HMAC-SHA256 cookie binds the secret to normalized remote address, IPv6 scope and port. Each session stores its prepared expected cookie in private native memory; unmanaged generation/constant-time verification callbacks cannot call user code or retain a managed GC handle. Cleanup zeroes cookie/secret/key-import buffers. Peer binding is authoritative; unrelated endpoints cannot inherit another peer's cookie. Datagram BIO read/write buffers are 64 KiB per direction; configured MTU is 1200 and native datagram boundaries remain intact.

A cookie challenge continues on the same logical handshaking peer; the backend does not require applications to discard every first attempt. Poll feeds complete UDP datagrams, advances SSL handshake/application records and flushes complete encrypted datagrams. OpenSSL DTLS retransmission timers advance on explicit Poll, including when no input arrives. Dropping the first ClientHello therefore retries without a second client construction. Applications choose their own overall connection deadline and call DisconnectFromPeer; native handshake retry/error policy remains OpenSSL's. No hidden worker, generic polling task or second socket is introduced.

## Packet delivery and teardown

Connected GetAvailablePacketCount reports already decoded complete packets without polling; callers explicitly Poll. Span reads preserve whole packets and leave an undersized destination queued; snapshot reads allocate through PacketPeer. The advertised outgoing capacity remains 488 bytes. Empty sends succeed without emitting a DTLS record. Larger sends reject before native mutation. A successful PutPacket accepts one complete record and flushes it; input/output failure terminates the session with typed errors rather than silently accepting an unsent packet.

Poll processes at most 32 record-progress iterations, staging up to 16 KiB plaintext per record. The prepared 64 KiB queue charges the existing 24-byte packet metadata budget and drops complete new plaintext records on overflow. Native BIO pressure retains a whole pending encrypted input datagram instead of splitting/truncating it. Native replay/record-MAC checks reject duplicate or corrupt ciphertext without exposing partial plaintext; later valid records can continue. DTLS supplies datagram authentication/encryption, not application reliability or ordered delivery.

Close notification/disconnect releases session/BIO/cookie/queued packets and security-resource retention while preserving the UDP owner. Idle disconnect is valid. Remote close transitions to Disconnected; transport or authentication failure uses Error unless native verification identifies a hostname/IP mismatch. Received packets clear on teardown. UDPServer.Stop invalidates accepted DTLS attachments through its existing shared-socket detachment.

## Verification and limits

[DTLSTests](../../tests/Electron2D.Tests/DTLSTests.cs) executes native public client/server packets, UDPServer admission, cookie challenges, first-ClientHello loss/retransmission, exact 488-byte data, undersized read preservation, empty/oversized send, anti-replay/bad-MAC recovery, name/custom/system/unsafe trust policies, setup rollback, wrong-thread/disposal reentry, helper/peer/UDP lifetime and a public Node/SceneTree command. Separate OpenSSL s_server and s_client processes verify both roles and actual DTLS 1.2 application interoperability; test-generated identity files are temporary and removed after bounded process cleanup. Sixty-four warmed active packet/span/poll cycles and idle cycles report zero managed bytes. Preparation, certificate/cookie work, snapshots, user callbacks and OpenSSL native allocations are outside those intervals.

These are headless native network checks, independent of display protocol. Other native platforms/browser hosts and packaging, routed-loss/throughput/native allocator totals, ENet integration and human/rendered/editor/agent acceptance remain separate gates. See [platform verification](../platform-verification.md) and [ADR 0094](../decisions/networking.md#adr-0094).

Backend references: [datagram BIO pair](https://docs.openssl.org/3.6/man3/BIO_s_dgram_pair/), [cookie exchange](https://docs.openssl.org/3.6/man3/DTLSv1_listen/) and [DTLS retransmission timers](https://docs.openssl.org/3.6/man3/DTLSv1_handle_timeout/).

The [ENet host consumer](enet.md) internally configures a larger transport MTU and accepts protocol datagrams through prepared record storage; this does not widen the standalone public 488-byte packet contract.
