# Networking

Last updated: 2026-10-19

The [WebRTC component](../components/webrtc.md) implements native peer/channel negotiation, typed extension providers and WebRTCMultiplayerPeer consumed by SceneMultiplayer/SceneTree. It shares PacketWriteMode with WebSocketPeer. Configuration and peer observations use typed records; signaling remains application-owned. Linux native and independent browser packet checks passed; routed and foreign adapter gates remain explicit.

## Responsibility and public surface

The [physics example check](../components/physics-network-example.md#deterministic-validation-phases-and-control-handoff) coordinates actual client phase execution and current-epoch input under slow polling. It explicitly prepares both old controllers’ queued future commands before a checked control handoff, then requires new-epoch replacement and actual application at the same tick. Ordinary sessions bypass that preparation. Its application receiver replaces retired future input on control/incarnation handoff while preserving same-epoch conflict rejection. This changes no public engine API.

Desktop DTLS oracles now use actual UDP binding readiness and exact byte-stream packet exchange with shared TLS process diagnostics. Native DTLS and ENet cold stage markers narrow target failures without changing protocol assertions, deadlines or warmed allocation intervals. Focused Linux checks passed; the remaining Windows/macOS full-suite failures retain their target gates.

Networking owns ordered binary streams, native TCP/UDP/UDS connections/listeners, UDP endpoint routing and packet framing. The [native streams and packets component](../components/networking.md) exposes StreamPeer/Buffer/Socket/TCP/UDS, SocketServer/TCPServer/UDSServer, PacketPeer/UDP/Stream and UDPServer. Runtime code is under `src/Core/Networking/` and ships in Electron2D.dll.

## Dependency direction and invariants

The domain uses core ElectronObject identity/disposal, typed project settings and .NET native sockets. It does not depend on scene rendering or public backend handles. Scene code owns ordinary transport instances and can poll them on their constructing thread; accepted stream peers own sockets, UDP peers borrow server sockets and packet-stream wrappers borrow underlying streams. [ADR 0094](../decisions/networking.md#adr-0094) defines byte ordering, exact-width types, wire framing, bounded buffers, failure phases, ownership and private Linux listener options. [ADR 0001](../decisions/product.md#adr-0001) excludes dynamic/untyped wire values; applications choose a typed message protocol.

## Current implementation and limits

The native Linux x64 low-level transport profile executes IPv4/IPv6 loopback and Unix-domain IPC. Browser raw sockets fail explicitly. [TLS streams and certificate/key resources](../components/tls.md) execute native Linux OpenSSL client/server exchange and typed resource loading. [HTTP/HTTPS transfers and stream compression](../components/http.md) add native protocol, proxy, scene/worker and incremental codec behavior. [WebSocket messages](../components/websocket.md) execute WS/WSS client/server and bounded complete-message/control flow. [Typed multiplayer transports](../components/multiplayer.md) add offline authority and owned WS/WSS peer cohorts. [Native DTLS](../components/dtls.md), [ENet channels and multiplayer](../components/enet.md), [typed scene multiplayer](../components/scene-multiplayer.md), [scene replication](../components/scene-replication.md) and [UPNP](../components/upnp.md) execute their documented workflows. WebRTC retains its ICE/SDP/SCTP backend prerequisite in [coverage](../coverage/index.md). Other-platform transport behavior, routed traffic and native allocator totals remain separate gates.

The [authoritative physics objective](../components/physics-contract-audit.md#authoritative-networking-audit) requires fixed-tick input, portable physics snapshots, client prediction/correction/replay and remote interpolation over this existing typed networking surface. Property replication is not a complete physics restore point. The bounded example exercises these mechanisms through public runtime APIs and separate native Linux processes, including a CPU server denied GPU startup and a software-GPU client. Complete inherited/extension coverage, foreign platforms, target hardware throughput, complete native memory and real-window/FPS acceptance remain separate under ADRs 0054/0094.

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

Synchronizer root/configuration replacement preflights typed targets and budgets before retiring live state. Invalid authority IDs preserve registration; empty roots stop automatic visibility work. Provider callbacks retain their normal registration-before-preparation order. See the [replacement contract](../components/scene-replication.md#configuration-and-preparation).

[UPNP gateway discovery/control](../components/upnp.md) now supplies synchronous SSDP and per-device description/connection assessment plus typed SOAP query/add/delete commands. Borrowed membership and owner-thread lifetimes are explicit. Independent native multicast and IPv4/IPv6 SOAP fixtures execute; a real IPv4 gateway passed read-only querying. Actual gateway mapping changes, routed IPv6 discovery/connectivity, foreign hosts and owner acceptance remain separate.

[Native DTLS packets](../components/dtls.md) now supplies PacketPeerDTLS and DTLSServer over borrowed connected UDP peers, with shared TLSStatus, existing TLSOptions/security resources, cookie exchange, retransmission timers and complete authenticated packet delivery. Native Linux OpenSSL 3.2+ is required. Independent OpenSSL processes in both roles and prepared allocation/public-scene checks execute; ENet/routed/foreign/native allocator/human gates remain separate.

## Authoritative physics consumer

[Managed scene/network storage](../components/physics-network-memory.md) now has
deduplicated object-size measurements for the actual three-process example and
additional local scene checkpoints, with held disposal and final collection gates.
Native ENet/driver heaps, traffic peaks, AOT sizes and hardware performance remain open.

The [PhysicsNetwork example](../components/physics-network-example.md) runs the existing
SceneMultiplayer/ENet byte API with PhysicsSnapshot and complete game manifests.
It exercises CPU authority, GPU prediction/replay, later joining, control transfer,
ID reuse and confirmed events in separate processes under application-level loss,
delay, duplication and reordering. Game-state recipes and policy remain in the
consumer; the engine does not infer a generic game protocol from property replication.


[PhysicsNetwork process checks](../components/physics-network-example.md#stronger-process-acceptance-2026-10-12) now verify both CPU processes lack usable GPU physics and compare full angular/linear authoritative state after deliberate angular divergence. Warm steps/replay retain zero managed allocation; transport impairment remains application scheduling over real ENet loopback.

The [rendered PhysicsNetwork client](../components/physics-network-example.md#rendered-client-window)
now draws remote interpolation and local correction smoothing in a real window
while CPU authority/late client stay headless. Public snapshot and pixel checks
verify visible separation without physical-state mutation; renderer choice and
window FPS/allocation reporting remain separate from physics/replay metrics.

## Shared address resolution

The [IP service](../classes/IP.md) and [address-resolution component](../components/address-resolution.md) now retain the hostname/family cache and bounded asynchronous queue. Existing transport hostname helpers and HTTP asynchronous resolution use this common backend; cold DNS and connection setup remain outside warmed polling budgets. Engine named lookup includes the permanent IP object and rejects disposal/unregistration.

## JSON-RPC document dispatch

[JSON-RPC documents and typed dispatch](../components/jsonrpc.md) adds caller-owned JSONRPC over existing transport text/bytes. Typed callbacks and compiled schemas handle positional/named parameters, response correlation, notifications and ordered batches. Wire validation and protocol errors retain dedicated document ownership; constructing a dispatcher opens no listener. The public scene TCP test exercises actual framed request/notification/response flow. JSON message work allocates; application framing, authentication, scene ownership and remote acceptance remain separate.

[Ordinary slow-client physics verification](../components/physics-network-example.md#ordinary-slow-client-verification-2026-10-16)
now runs the public example without authority phase/input barriers. Nine ordinary
managed/NativeAOT/instrumented cohorts and two paced regressions pass 900 ticks
with CPU authority, CPU/GPU prediction, late admission, actual state reordering,
sleep/wake correction and strict zero warmed physical bytes. Bounded queue age is
tracked per recipient and verified across slot reuse. This verifies the Linux
software-GPU recipe; general inherited/extension, native/driver memory, foreign
platform and target-device/window/performance acceptance remain open.

[Current ordinary network memory](../components/physics-network-memory.md#current-slow-client-queue-and-history-budget-2026-10-16)
now has 60 CoreCLR heap observations across CPU/GPU and both numerical policies,
with exact owner-union checks, queue-array inventory and actual lifetime payload
high water. Twelve selected children release all chosen scene/network/history
owners; held disposal has zero requested GPU buffers. A separate NativeAOT paced
cohort verifies the report/counter path without establishing native object sizes.
These bounded results preserve open full native/driver/global ownership, unseen
peaks, arbitrary workloads, foreign platforms and target-device/window gates.

[Native session allocation paths](../components/physics-network-memory.md#native-allocation-stack-cohort-2026-10-16)
now add twelve instrumented child traces with 60 process observations. All four
ordinary CPU/GPU and owner/separate cohorts retain their original physics,
network and zero warm managed-allocation checks. Traced process and transport-
module peaks remain separate from wire payload, managed graphs and RSS, and
include the additional checkpoint choice, separated by the marked follow-up
below. Exclusive physical-step budgets, complete ownership and target-device/
window acceptance remain open.

[Event-delimited native intervals](../components/physics-network-memory.md#event-delimited-native-phases-2026-10-16)
now distinguish construction, initial and post-warm session work, added local
checkpoints and release/reporting, while preserving full ordinary network checks.
Calibration excludes only the seven explicit marker requests and retains earlier
live storage. These instrumented event-order scopes remain separate from
physical-step-only allocations, uninstrumented timing and target/window acceptance.
The [boundary call paths](../components/physics-network-memory.md#live-native-call-path-inventory-2026-10-16)
now distinguish software pipeline/loader retirement from later runtime JIT
requests in those same traces; they do not establish exclusive network owners.
The [NativeAOT native collector](../components/physics-network-memory.md#nativeaot-native-lifetime-cohort-2026-10-16)
now records twelve complete native session lifetimes under the same original
checks, with unprofiled controls and explicit unmeasured object graphs. It leaves
exact ownership, phase and target/window gates open.
