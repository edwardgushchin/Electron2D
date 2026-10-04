# Electron2D networking decisions

Last updated: 2026-10-04

This bounded log owns native stream and packet transport behavior. The [decision index](index.md) routes other work; current classes and [networking](../domains/networking.md) describe the executable surface.

<a id="adr-0094"></a>
## ADR 0094: Typed native streams and packet transports

**Status:** Accepted

### Context

Games and tools need real byte streams, TCP listeners, UDP endpoint routing and local IPC before HTTP, TLS, WebSocket or multiplayer layers can execute. Existing typed C# and ownership rules remain authoritative under [ADR 0001](product.md#adr-0001), [ADR 0003](core-object-runtime.md#adr-0003), [ADR 0014](resources.md#adr-0014), [ADR 0021](product.md#adr-0021) and [ADR 0051](product.md#adr-0051).

### Decision

- Native sockets use the .NET socket backend internally. Raw socket handles and native option identities remain private. Linux listener setup applies SO_REUSEADDR without SO_REUSEPORT through one private pinned-handle call, allowing restart while accepted connections remain open and rejecting simultaneous listener sharing. Other native hosts use explicit exclusive listener binding pending their own verification gate. Browser raw sockets fail explicitly; an in-memory StreamPeerBuffer remains platform independent.
- Preserve the StreamPeer → StreamPeerSocket → TCP/UDS inheritance and SocketServer → TCP/UDS listener inheritance. PacketPeer owns packet reads; UDP and framed-stream consumers preserve packet boundaries. Runtime types inherit ElectronObject directly where the source uses its reference-counted object role. Managed references own object memory; IDisposable owns socket cleanup. Calls and disposal require the constructing thread, validated before disposal starts.
- Full stream reads/writes may wait until requested progress or failure; partial operations return a byte count without waiting. Use caller spans for repeated work and caller-owned arrays for snapshots. Primitive APIs use exact signed/unsigned C# widths and IEEE half/single/double encodings. BigEndian also governs string length prefixes. ASCII output replaces unrepresentable scalars with spaces. UTF-8 input skips a leading BOM, stops at NUL and replaces each malformed byte. GetString preserves Latin-1 byte decoding with NUL termination. Native connection, DNS and I/O failures use typed exceptions; socket phases use one shared StreamSocketStatus. GetStatus is cached; Poll advances nonblocking connection and detects FIN only after queued bytes drain.
- String reads have an explicit writable MaxStringBytes allocation budget, initially 16 MiB. Datagram and framed-packet buffer configuration rejects allocation above 64 MiB. UDP receive budget rounds up to a power of two and charges 24 metadata bytes per packet; records additionally retain IPv6 scope. Overflow drops whole datagrams. Framed stream packets use a fixed little-endian uint32 length independent of StreamPeer.BigEndian, ignore empty outgoing packets and accept zero-length incoming frames. Buffer resize rejects queued input; headers above the payload budget fail instead of permanently stalling a full buffer.
- PacketPeer.GetPacketError projects last-read success/unavailable/error through PacketReadStatus; LastReadException retains the original failure. Too-small caller storage leaves a packet queued. Variant serialization and its encode-buffer budget are excluded as dynamic wire representation under ADR 0001; raw packet/stream bytes are fully implemented and callers choose their typed message protocol.
- Accepted TCP/UDS connections transfer socket ownership and survive listener Stop. UDPServer owns pending peers, weakly tracks accepted peers and routes packets by endpoint. Accepted UDP peers borrow its shared socket; peer Close removes its endpoint without closing the listener. Server Stop disposes pending peers and detaches accepted peers. Lowering MaxPendingConnections trims newest pending peers; zero rejects new endpoints while accepted endpoints keep receiving. This corrects the pinned reversed limit-trimming loop. StreamPeerBuffer shrink clamps its cursor instead of allowing a negative readable count; Duplicate copies bytes and resets endian/cursor.
- Consumer-defined transports inherit StreamPeer or PacketPeer and override typed span/progress, availability and packet-capacity hooks. Extension-only facade identities map to these managed bases; native pointer/error-out ABI is adapted to spans and exceptions. GetMaxPacketSize reports outgoing raw payload capacity (UDP conservatively 65507; framed streams use their configured payload limit).
- Scope the first delivery to low-level native transports and framing. TLS/DTLS, certificate/trust resources, HTTP/WebSocket parsing, resolver services, ENet/WebRTC, multiplayer scene replication and router discovery retain their own concrete dependencies. No such protocol owner or empty facade is added by this slice.

### Verification boundary

NetworkingTests exercises native Linux x64 IPv4/IPv6 TCP/UDP, Unix-domain sockets and filesystem cleanup, loopback DNS configuration, independent binary/framing bytes, UTF-8 edges, partial transfers, FIN, port conflict/restart, packet budgets/zero datagrams/connected filtering, UDP peer routing and lifetime, multicast join/leave on loopback, ownership rejection and a public Node/SceneTree request/reply workflow. Headless scene scheduling does not establish rendered output. 64 warmed buffer-number, TCP number/poll and UDP caller-span packet/metadata cycles measure managed allocation after preparation; constructor/connection/DNS/snapshot/string/native allocator costs and broader network throughput remain outside that measurement. Other platforms, routed multicast/broadcast delivery, real network conditions and owner acceptance remain unverified.

### Rejected alternatives

- Publish native handles or socket option codes: ties consumers to a platform backend.
- Use Task/async operations for every small polling step: adds heap/state-machine work where immediate span/socket operations suffice. DNS resolution and full I/O remain explicitly blocking cold/caller-selected operations.
- Recreate heterogeneous error/data arrays or dynamic value serialization: conflicts with typed C# contracts.
- Model UDP peers as separately connected listener sockets: loses the shared listener's endpoint routing and stop ownership.
- Silently accept impossible frame headers, negative readable counts or reversed pending-limit trimming: preserves defects instead of useful transport semantics.
