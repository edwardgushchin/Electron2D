# WebRTC data channels and multiplayer

Last updated: 2026-10-19

## Scope and dependencies

[WebRTCPeerConnection](../classes/WebRTCPeerConnection.md) negotiates native ICE, DTLS and SCTP. [WebRTCDataChannel](../classes/WebRTCDataChannel.md) exchanges complete packets through PacketPeer's span reads. Both extension classes implement the same typed contract. [WebRTCMultiplayerPeer](../classes/WebRTCMultiplayerPeer.md) supplies the existing SceneMultiplayer/SceneTree RPC and replication consumer.

Source-only C# builds can use an older locally cached platform package for unrelated features; native WebRTC execution requires the matching fresh platform payload or an explicit source/prebuilt bridge.

The engine-owned C ABI bridge statically links unchanged libdatachannel 0.24.6, libjuice, usrsctp and plog. The vendored manifest pins every source file; platform packages carry the corresponding notices and source-access statement. Media and the dependency's independent WebSocket backend are disabled. OpenSSL 3 supplies the cryptographic transport. Linux uses its existing system OpenSSL boundary; macOS and Windows producer recipes select the pinned private OpenSSL pair. Foreign execution remains a separate platform gate under [ADR 0021](../decisions/product.md#adr-0021).

## Runtime flow and ownership

Create a peer with WebRTCPeerConnection.Create, optionally Initialize it with copied WebRTCConfiguration/ICE server lists, then create channels before negotiation. An empty configuration uses local host candidates. ICE URL/username/credential lists are copied before native setup. Invalid setup leaves the old cohort intact; input callbacks cannot mutate their preparing owner.

CreateOffer queues a proposal without changing signaling state. On SessionDescriptionCreated, commit the proposal with SetLocalDescription and send it through the application's signaling service. Apply the remote description with SetRemoteDescription and forward ICECandidateCreated observations to AddICECandidate. Received offers queue an answer through the same explicit commitment path. Local descriptions must match the current generated proposal after SDP canonicalization; changed transport identity and stale proposals reject before commitment. Signaling service selection and authentication belong to the application.

Native protocol workers hold weak native state and enqueue candidate/channel observations. Poll delivers engine events on the constructing thread. Subscriber failures aggregate while later selected subscribers still run. Poll cannot reenter itself. Reinitialize, Close and Dispose end the old event cohort. Returned data channels are caller-owned: peer retirement closes their transport without disposing or rebinding their wrapper identities. The default extension factory returns the actual extension object. Implementors use CheckPeerAccess/CheckPacketPeer and typed emission helpers; no reflected class-name activation or native pointer ownership escapes.

Channels select binary versus strict outgoing UTF-8 text through shared PacketWriteMode, including NUL inside text payloads, empty messages and exact packet boundaries. Receive payloads remain raw bytes with the remote text/binary marker. Too-small receive spans leave the packet queued. Labels/protocols and configuration strings reject NUL and malformed or oversized UTF-8 at the native string boundary. Negotiated IDs are 0 through 65534; an unset ID and absent reliability limits report 65535. MaxPacketLifeTime and MaxRetransmits are mutually exclusive and preserve explicit zero. The native message ceiling is 65536 bytes; the remote negotiated limit governs outgoing capacity. Incoming storage is bounded to 1024 packets and 4 MiB per channel; overflow becomes a sticky observable error and closes transport. Peer observations cap at 4096 and fail the cohort on overflow.

## Multiplayer integration

CreateServer uses identity one; CreateClient requires a local identity greater than one and registers server one; CreateMesh accepts explicit positive identities. AddPeer borrows a new connection and owns its generated channel cohort. IDs 1/2/3 provide reliable ordered, lifetime-limited ordered and lifetime-limited unordered delivery. Reserved unreliable lifetime defaults to one millisecond. Configured extra modes use IDs 4 onward and lifetime one for unreliable delivery. TransferChannel zero selects a reserved channel by TransferMode; positive channels select their configured mode. Packets have a conservative 1200-byte payload ceiling. Positive targets address one peer, zero broadcasts, and negative targets exclude that identity.

GetPeer/GetPeers copy membership and expose borrowed connection/channel identities through WebRTCPeerInfo. Removing a peer releases generated channels and preserves the caller connection. Normal DisconnectPeer closes that connection and Poll retires membership; force removes membership immediately without a disconnected event. Close clears local membership without disposing borrowed connections. Setup rollback attempts every created channel and preserves setup plus cleanup exceptions. Poll retires failed channel cohorts and publishes membership state before callbacks; callbacks may remove themselves. The selected next packet retains consistent source, channel and delivery metadata until a successful read, including after a small-buffer failure.

## Verification

ELECTRON2D_TEST_WEBRTC=1 exercises native peer negotiation, in-band and negotiated channels, UTF-8/NUL/empty packets, labels/protocols, zero reliability limits, exact span retry, owner/lifecycle/configuration failures, typed extension construction/events, rollback aggregation, callback mutation, bounded receive overflow, server/client reserved/custom routing, and SceneTree automatic polling with bidirectional typed RPC and raw packets. Thirty-two prepared native channel cycles and thirty-two prepared multiplayer cycles measure zero managed allocation on the owner thread. Native workers/allocators, setup, snapshots, string/configuration operations and throughput are outside those measurements.

ELECTRON2D_TEST_WEBRTC=browser starts a loopback HTTP signaling fixture and prints its URL. Opening that URL with an independent browser establishes a real browser/native channel and checks text containing NUL/Unicode, binary and empty messages in both directions. The native host's final success line is authoritative; after it releases the peer, the browser can report transport closure/failure.

Routed STUN/TURN service behavior, a browser-hosted engine adapter, mobile/static-Apple packaging, foreign desktop execution, native allocator totals and owner acceptance remain unverified or require their named adapter/platform prerequisite. A C# build alone does not establish any of those gates. See [ADR 0094](../decisions/networking.md#adr-0094).
