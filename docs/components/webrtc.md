[UTF8 input ownership](../coverage/performance-producers.md#utf8-input-marshalling-domain-2026-10-29) now covers descriptions/candidates/channel strings and copied ICE URL/user/password buffers. Preparation reserves pointer capacity before acquiring native strings and creates the managed peer owner before native creation. Refusal at any of the three credential buffers retires earlier arguments and preserves the existing native peer; real transport/container/OS and composed-memory qualification remain separate.

# WebRTC data channels and multiplayer

Last updated: 2026-11-05

## Scope and dependencies

[WebRTCPeerConnection](../classes/WebRTCPeerConnection.md) negotiates native ICE, DTLS and SCTP. [WebRTCDataChannel](../classes/WebRTCDataChannel.md) exchanges complete packets through PacketPeer's span reads. Both extension classes implement the same typed contract. [WebRTCMultiplayerPeer](../classes/WebRTCMultiplayerPeer.md) supplies the existing SceneMultiplayer/SceneTree RPC and replication consumer.

Source-only C# builds can use an older locally cached platform package for unrelated features; native WebRTC execution requires the matching fresh platform payload or an explicit source/prebuilt bridge.

The engine-owned C ABI bridge statically links unchanged libdatachannel 0.24.6, libjuice, usrsctp and plog. The vendored manifest pins every source file; platform packages carry the corresponding notices and source-access statement. Media and the dependency's independent WebSocket backend are disabled. The shared [OpenSSL 4.1.0 source profile](tls.md#unified-openssl-source-profile) supplies cryptographic transport. All desktop producer recipes select that same pair; configuration and runtime initialization reject a stale provider. Foreign execution remains a separate platform gate under [ADR 0021](../decisions/product.md#adr-0021).

## Runtime flow and ownership

Create a peer with WebRTCPeerConnection.Create, optionally Initialize it with copied WebRTCConfiguration/ICE server lists, then create channels before negotiation. An empty configuration uses local host candidates. ICE URL/username/credential lists are copied before native setup. Invalid setup leaves the old cohort intact; input callbacks cannot mutate their preparing owner.

CreateOffer queues a proposal without changing signaling state. On SessionDescriptionCreated, commit the proposal with SetLocalDescription and send it through the application's signaling service. Apply the remote description with SetRemoteDescription and forward ICECandidateCreated observations to AddICECandidate. Received offers queue an answer through the same explicit commitment path. Local descriptions must match the current generated proposal after SDP canonicalization; changed transport identity and stale proposals reject before commitment. Signaling service selection and authentication belong to the application.

Native protocol workers hold weak native state and enqueue candidate/channel observations. Poll delivers engine events on the constructing thread. Subscriber failures aggregate while later selected subscribers still run. Poll cannot reenter itself. Reinitialize, Close and Dispose end the old event cohort. Returned data channels are caller-owned: peer retirement closes their transport without disposing or rebinding their wrapper identities. The default extension factory returns the actual extension object. Implementors use CheckPeerAccess/CheckPacketPeer and typed emission helpers; no reflected class-name activation or native pointer ownership escapes.

Channels select binary versus strict outgoing UTF-8 text through shared PacketWriteMode, including NUL inside text payloads, empty messages and exact packet boundaries. Receive payloads remain raw bytes with the remote text/binary marker. Too-small receive spans leave the packet queued. Labels/protocols and configuration strings reject NUL and malformed or oversized UTF-8 at the native string boundary. Negotiated IDs are 0 through 65534; an unset ID and absent reliability limits report 65535. MaxPacketLifeTime and MaxRetransmits are mutually exclusive and preserve explicit zero. The native message ceiling is 65536 bytes; the remote negotiated limit governs outgoing capacity. Incoming storage is bounded to 1024 packets and 4 MiB per channel; overflow becomes a sticky observable error and closes transport. Peer observations cap at 4096 and fail the cohort on overflow.

## Multiplayer integration

CreateServer uses identity one; CreateClient requires a local identity greater than one and registers server one; CreateMesh accepts explicit positive identities. AddPeer borrows a new connection and owns its generated channel cohort. IDs 1/2/3 provide reliable ordered, lifetime-limited ordered and lifetime-limited unordered delivery. Reserved unreliable lifetime defaults to one millisecond. Configured extra modes use IDs 4 onward and lifetime one for unreliable delivery. TransferChannel zero selects a reserved channel by TransferMode; positive channels select their configured mode. Packets have a conservative 1200-byte payload ceiling. Positive targets address one peer, zero broadcasts, and negative targets exclude that identity.

GetPeer/GetPeers copy membership and expose borrowed connection/channel identities through WebRTCPeerInfo. Removing a peer releases generated channels and preserves the caller connection. Normal DisconnectPeer closes that connection and Poll retires membership; force removes membership immediately without a disconnected event. Close clears local membership without disposing borrowed connections. Setup rollback attempts every created channel and preserves setup plus cleanup exceptions. Poll retires failed channel cohorts and publishes membership state before callbacks; callbacks may remove themselves. The selected next packet retains consistent source, channel and delivery metadata until a successful read, including after a small-buffer failure.

## Bridge-owned native storage (2026-11-05)

The [native source profile](../../tools/webrtc-native/README.md) routes peer/channel
control blocks and handles, event objects, queue backing storage, weak-channel
capacity and persistent proposal/event/metadata/diagnostic strings through the
common pure-C ledger. Weak control blocks and thread-local capacity remain counted
until their actual retirement. No managed callback or global allocator replacement
is involved. libdatachannel remains the unchanged pinned 0.24.6 release, with the
same OpenSSL 4.1.0 pair.

Failed event/channel extraction preserves ownership for retry. Failed receive
insertion preserves the accepted byte count and becomes a sticky polling error.
Closing moves the existing weak list without allocating, retires event-owned
channels outside the peer gate and attempts every channel plus the peer before
reporting the first native error. Logical closed state and managed membership
commit even on that error. Diagnostic allocation failure uses a static fallback.

Linux x64 checks reject four real native-peer and 22 container allocation positions,
then require complete temporary drain and healthy retry. Separate witnesses cover
queue insertion, diagnostic refusal, exact handle/event payload bytes, weak lifetime,
allocation-free close and thread-local retirement. The bridge/core fixture passes
ASan/UBSan with leak detection; dependency archives retain their ordinary Release
instrumentation profile.

Self-contained CLR and full-trim NativeAOT 10.0.14 run the complete native channel,
multiplayer, configuration, extension and SceneTree RPC profile, including both
32-frame zero-managed-owner-byte intervals. Both observe 768 bytes for a new peer
and another 8937 bytes for a channel with long metadata. The injected post-close
error releases dropped managed channels on both hosts. Matching preceding native
and managed controls fail the accounting and close-retirement witnesses respectively.
Process maps show one ledger module and one selected SSL/crypto pair. Receipts are
in `bin/webrtc-memory/2026-11-05/`.

The consumer modules are RTC/core-only test fixtures, not the complete native
product. A rebuilt unified bundle/package and foreign execution remain open.
Compatible packet payloads, send/SDP conversion temporaries, backend objects and
captures, SCTP/ICE, CRT and provider heaps retain their own allocation domains.
This does not establish total native memory or coherent managed/native monitoring.

## Verification

ELECTRON2D_TEST_WEBRTC=1 exercises native peer negotiation, in-band and negotiated channels, UTF-8/NUL/empty packets, labels/protocols, zero reliability limits, exact span retry, owner/lifecycle/configuration failures, typed extension construction/events, rollback aggregation, callback mutation, bounded receive overflow, server/client reserved/custom routing, and SceneTree automatic polling with bidirectional typed RPC and raw packets. Thirty-two prepared native channel cycles and thirty-two prepared multiplayer cycles measure zero managed allocation on the owner thread. Native workers/allocators, setup, snapshots, string/configuration operations and throughput are outside those measurements.

ELECTRON2D_TEST_WEBRTC=browser starts a loopback HTTP signaling fixture and prints its URL. Opening that URL with an independent browser establishes a real browser/native channel and checks text containing NUL/Unicode, binary and empty messages in both directions. The native host's final success line is authoritative; after it releases the peer, the browser can report transport closure/failure.

Routed STUN/TURN service behavior, a browser-hosted engine adapter, mobile/static-Apple packaging, foreign desktop execution, native allocator totals and owner acceptance remain unverified or require their named adapter/platform prerequisite. A C# build alone does not establish any of those gates. See [ADR 0094](../decisions/networking.md#adr-0094).
