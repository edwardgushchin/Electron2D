# Networking

Last updated: 2026-10-04

## Responsibility and public surface

Networking owns ordered binary streams, native TCP/UDP/UDS connections/listeners, UDP endpoint routing and packet framing. The [native streams and packets component](../components/networking.md) exposes StreamPeer/Buffer/Socket/TCP/UDS, SocketServer/TCPServer/UDSServer, PacketPeer/UDP/Stream and UDPServer. Runtime code is under `src/Core/Networking/` and ships in Electron2D.dll.

## Dependency direction and invariants

The domain uses core ElectronObject identity/disposal, typed project settings and .NET native sockets. It does not depend on scene rendering or public backend handles. Scene code owns ordinary transport instances and can poll them on their constructing thread; accepted stream peers own sockets, UDP peers borrow server sockets and packet-stream wrappers borrow underlying streams. [ADR 0094](../decisions/networking.md#adr-0094) defines byte ordering, exact-width types, wire framing, bounded buffers, failure phases, ownership and private Linux listener options. [ADR 0001](../decisions/product.md#adr-0001) excludes dynamic/untyped wire values; applications choose a typed message protocol.

## Current implementation and limits

The native Linux x64 low-level transport profile executes IPv4/IPv6 loopback and Unix-domain IPC. Browser raw sockets fail explicitly. [TLS streams and certificate/key resources](../components/tls.md) execute native Linux OpenSSL client/server exchange and typed resource loading. [HTTP/HTTPS transfers and stream compression](../components/http.md) add native protocol, proxy, scene/worker and incremental codec behavior. [WebSocket messages](../components/websocket.md) execute WS/WSS client/server and bounded complete-message/control flow. [Typed multiplayer transports](../components/multiplayer.md) add offline authority and owned WS/WSS peer cohorts. Protocol owners for DTLS, ENet/WebRTC, scene multiplayer replication and router discovery are absent and remain in [coverage](../coverage/index.md). Other-platform transport behavior, routed traffic and native allocator totals remain separate gates.

## Verification

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies real local sockets and wire/queue/lifecycle boundaries plus a public Node/SceneTree request/reply workflow. Prepared buffer/TCP/UDP repeated caller-span operations have 64-cycle managed allocation checks; the component defines what those checks include and exclude. Headless scene execution does not prove rendering, editor/agent-tool support or owner acceptance.

Consumer-defined transports inherit StreamPeer or PacketPeer and override typed span, progress, availability and raw payload capacity hooks. NetworkingTests integrates a one-byte-at-a-time consumer stream with primitive reads/writes and PacketPeerStream, and verifies custom packet bounds without consuming undersized reads. GetMaxPacketSize reports 65507 bytes conservatively for UDP and the configured outgoing payload limit for framed streams.

[Cryptographic operations](../components/crypto.md) now execute secure random, generated RSA keys/RSA-EC self-signed identities, asymmetric signatures/encryption and prepared MD5/SHA1/SHA256, HMAC and raw AES contexts. Existing resource loaders and TLS consume generated identities. CryptoTests verifies independent wires and actual generated-identity TLS; prepared span intervals report zero managed bytes. Provider-native/foreign/routed/human gates remain separate.

[Typed scene multiplayer](../components/scene-multiplayer.md) executes owner-thread Node/SceneTree branch assignment and process polling, typed RPC/authority/local policy, authentication/deadlines and WS/WSS original-sender relay. Concrete spawning/property-schema synchronization remains the next dependent producer; other-platform/routed/native allocator and human/editor/rendered acceptance stay separate.

[Typed scene spawning and property replication](../components/scene-replication.md) now supplies MultiplayerSpawner/Synchronizer/SceneReplicationConfig and concrete descriptor/factory codecs over WS/WSS, with actual pre-Ready/late/visibility/authority/batch semantics. In-memory PackedScene provenance enables automatic spawning. Disk scene format/loading/editor authoring and foreign/routed/native/human/rendered acceptance remain separate.

[UPNP gateway discovery/control](../components/upnp.md) now supplies synchronous SSDP and per-device description/connection assessment plus typed SOAP query/add/delete commands. Borrowed membership and owner-thread lifetimes are explicit. Independent native multicast and IPv4/IPv6 SOAP fixtures execute; a real IPv4 gateway passed read-only querying. Actual gateway mapping changes, routed IPv6 discovery/connectivity, foreign hosts and owner acceptance remain separate.

[Native DTLS packets](../components/dtls.md) now supplies PacketPeerDTLS and DTLSServer over borrowed connected UDP peers, with shared TLSStatus, existing TLSOptions/security resources, cookie exchange, retransmission timers and complete authenticated packet delivery. Native Linux OpenSSL 3.2+ is required. Independent OpenSSL processes in both roles and prepared allocation/public-scene checks execute; ENet/routed/foreign/native allocator/human gates remain separate.
