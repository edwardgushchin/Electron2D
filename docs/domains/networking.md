# Networking

Last updated: 2026-10-08

## Responsibility and public surface

Desktop DTLS oracles now use actual UDP binding readiness and exact byte-stream packet exchange with shared TLS process diagnostics. Native DTLS and ENet cold stage markers narrow target failures without changing protocol assertions, deadlines or warmed allocation intervals. Focused Linux checks passed; the remaining Windows/macOS full-suite failures retain their target gates.

Networking owns ordered binary streams, native TCP/UDP/UDS connections/listeners, UDP endpoint routing and packet framing. The [native streams and packets component](../components/networking.md) exposes StreamPeer/Buffer/Socket/TCP/UDS, SocketServer/TCPServer/UDSServer, PacketPeer/UDP/Stream and UDPServer. Runtime code is under `src/Core/Networking/` and ships in Electron2D.dll.

## Dependency direction and invariants

The domain uses core ElectronObject identity/disposal, typed project settings and .NET native sockets. It does not depend on scene rendering or public backend handles. Scene code owns ordinary transport instances and can poll them on their constructing thread; accepted stream peers own sockets, UDP peers borrow server sockets and packet-stream wrappers borrow underlying streams. [ADR 0094](../decisions/networking.md#adr-0094) defines byte ordering, exact-width types, wire framing, bounded buffers, failure phases, ownership and private Linux listener options. [ADR 0001](../decisions/product.md#adr-0001) excludes dynamic/untyped wire values; applications choose a typed message protocol.

## Current implementation and limits

The native Linux x64 low-level transport profile executes IPv4/IPv6 loopback and Unix-domain IPC. Browser raw sockets fail explicitly. [TLS streams and certificate/key resources](../components/tls.md) execute native Linux OpenSSL client/server exchange and typed resource loading. [HTTP/HTTPS transfers and stream compression](../components/http.md) add native protocol, proxy, scene/worker and incremental codec behavior. [WebSocket messages](../components/websocket.md) execute WS/WSS client/server and bounded complete-message/control flow. [Typed multiplayer transports](../components/multiplayer.md) add offline authority and owned WS/WSS peer cohorts. [Native DTLS](../components/dtls.md), [ENet channels and multiplayer](../components/enet.md), [typed scene multiplayer](../components/scene-multiplayer.md), [scene replication](../components/scene-replication.md) and [UPNP](../components/upnp.md) execute their documented workflows. WebRTC retains its ICE/SDP/SCTP backend prerequisite in [coverage](../coverage/index.md). Other-platform transport behavior, routed traffic and native allocator totals remain separate gates.

The [authoritative physics objective](../components/physics-contract-audit.md#authoritative-networking-audit) requires fixed-tick input, portable physics snapshots, client prediction/correction/replay and remote interpolation over this existing typed networking surface. Property replication is not a complete physics restore point. These capabilities and separate-process CPU-server/GPU-client acceptance remain open under ADRs 0054/0094.

## Verification

Apple test apps now reference static ENet/OpenSSL and exercise public host binding, trusted TLS records, hostname mismatch and OS-root rejection. The existing bounded system-trust callback is reused. Four simulator results remain required; these mobile smoke checks do not replace the five complete Windows/macOS suites.

Shared TCP/UDS polling uses non-consuming receive peek to distinguish drained FIN from RST. The retained networking fixture passed locally after the Windows x86 timeout fix. TLS checks force both-role TLS 1.2 through SslStream and both-role TLS 1.3 through independent OpenSSL, including graceful versus abrupt closure and borrowed transport lifetime. Linux focused checks passed; complete Windows/macOS runs are still required.

Desktop runtime resolution selects private macOS/Windows ENet/OpenSSL and bounded .NET OS-chain trust before OpenSSL name/purpose verification. macOS fresh text/audio/ENet consumers passed; complete Windows/macOS full-suite acceptance remains pending. All three Windows full-suite/consumer profiles are connected. Linux regressions passed after loader/ABI/trust changes. Mobile/browser integration remains required, with no foreign acceptance inferred from Linux checks.

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies real local sockets and wire/queue/lifecycle boundaries plus a public Node/SceneTree request/reply workflow. Prepared buffer/TCP/UDP repeated caller-span operations have 64-cycle managed allocation checks; the component defines what those checks include and exclude. Headless scene execution does not prove rendering, editor/agent-tool support or owner acceptance.

Shared datagram creation disables Windows ICMP port-unreachable resets so a departed UDP/ENet endpoint cannot reset healthy peers' listener. IPv4/IPv6 peer-departure checks retain exact packet/sender assertions and a Windows-only reset-enabled negative control; TCP reset behavior remains separate and unchanged. Complete foreign-host acceptance still requires the native CI run.

Consumer-defined transports inherit StreamPeer or PacketPeer and override typed span, progress, availability and raw payload capacity hooks. NetworkingTests integrates a one-byte-at-a-time consumer stream with primitive reads/writes and PacketPeerStream, and verifies custom packet bounds without consuming undersized reads. GetMaxPacketSize reports 65507 bytes conservatively for UDP and the configured outgoing payload limit for framed streams.

[Cryptographic operations](../components/crypto.md) now execute secure random, generated RSA keys/RSA-EC self-signed identities, asymmetric signatures/encryption and prepared MD5/SHA1/SHA256, HMAC and raw AES contexts. Existing resource loaders and TLS consume generated identities. CryptoTests verifies independent wires and actual generated-identity TLS; prepared span intervals report zero managed bytes. Provider-native/foreign/routed/human gates remain separate.

[Typed scene multiplayer](../components/scene-multiplayer.md) executes owner-thread Node/SceneTree branch assignment and process polling, typed RPC/authority/local policy, authentication/deadlines and WS/WSS original-sender relay. Concrete spawning/property-schema synchronization remains the next dependent producer; other-platform/routed/native allocator and human/editor/rendered acceptance stay separate.

[Typed scene spawning and property replication](../components/scene-replication.md) now supplies MultiplayerSpawner/Synchronizer/SceneReplicationConfig and concrete descriptor/factory codecs over WS/WSS, with actual pre-Ready/late/visibility/authority/batch semantics. In-memory PackedScene provenance enables automatic spawning. Disk scene format/loading/editor authoring and foreign/routed/native/human/rendered acceptance remain separate.

[UPNP gateway discovery/control](../components/upnp.md) now supplies synchronous SSDP and per-device description/connection assessment plus typed SOAP query/add/delete commands. Borrowed membership and owner-thread lifetimes are explicit. Independent native multicast and IPv4/IPv6 SOAP fixtures execute; a real IPv4 gateway passed read-only querying. Actual gateway mapping changes, routed IPv6 discovery/connectivity, foreign hosts and owner acceptance remain separate.

[Native DTLS packets](../components/dtls.md) now supplies PacketPeerDTLS and DTLSServer over borrowed connected UDP peers, with shared TLSStatus, existing TLSOptions/security resources, cookie exchange, retransmission timers and complete authenticated packet delivery. Native Linux OpenSSL 3.2+ is required. Independent OpenSSL processes in both roles and prepared allocation/public-scene checks execute; ENet/routed/foreign/native allocator/human gates remain separate.
