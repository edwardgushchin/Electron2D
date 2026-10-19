# WebRTCPeerConnection

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.WebRTCPeerConnection`. **Source:** [WebRTCPeerConnection.cs](../../src/Core/Networking/WebRTCPeerConnection.cs). **Component:** [WebRTC](../components/webrtc.md).

**Inherits:** [ElectronObject](ElectronObject.md).

**Inherited By:** [WebRTCPeerConnectionExtension](WebRTCPeerConnectionExtension.md).

## Description

Negotiates owner-polled WebRTC data channels over a retained native peer or typed extension.

Create selects the retained native backend or the typed default extension factory. Initialize snapshots ICE configuration before native setup and replaces the old cohort only after success. Returned channels are caller-owned. Close/disposal closes their transport without disposing or rebinding those identities. Native protocol workers enqueue observations; Poll alone emits owner-thread events and aggregates selected subscriber errors. Reinitialize/disposal ends old-cohort delivery. See the component for explicit SDP commitment, bounded queues and application signaling.

## Example

Public C# excerpt. Channel/observation examples require the indicated application transport; MyPeer/applicationChannel stand for a complete application extension. The peer example generates and commits only its local offer; a remote peer and an application signaling path are required for connectivity. Equivalent complete native and scene workflows execute in WebRTCTests.

```csharp
using var peer = WebRTCPeerConnection.Create();
using var channel = peer.CreateDataChannel("game");
peer.SessionDescriptionCreated += (type, sdp) => peer.SetLocalDescription(type, sdp);
peer.CreateOffer();
peer.Poll();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected WebRTCPeerConnection()` | Creates the common owner identity for native and managed implementations. |

## Constructor Descriptions

<a id="member-0e399123fe57"></a>
### WebRTCPeerConnection()

`protected WebRTCPeerConnection()`

Creates the common owner identity for native and managed implementations.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public virtual System.Void AddICECandidate(System.String media, System.Int32 index, System.String name)` | Adds one remote ICE candidate. |
| `protected System.Void CheckPeerAccess()` | Checks lifetime, owner thread and configuration callback reentry. |
| `public virtual System.Void Close()` | Closes native transport and pending observations without disposing channel wrappers. |
| `public static Electron2D.WebRTCPeerConnection Create()` | Constructs the selected extension, or the native implementation when none is selected. |
| `public virtual Electron2D.WebRTCDataChannel CreateDataChannel(System.String label, Electron2D.WebRTCDataChannelOptions options = null)` | Creates an in-band or explicitly negotiated caller-owned data channel. |
| `public virtual System.Void CreateOffer()` | Generates an SDP offer without committing it or starting ICE publication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Releases resources owned by a derived class. |
| `protected System.Void EmitDataChannelReceived(Electron2D.WebRTCDataChannel channel)` | Publishes a typed incoming-channel event on the owner thread. |
| `protected System.Void EmitICECandidateCreated(System.String media, System.Int32 index, System.String name)` | Publishes a typed candidate event on the owner thread. |
| `protected System.Void EmitSessionDescriptionCreated(System.String type, System.String sdp)` | Publishes a typed offer/answer event on the owner thread. |
| `public virtual Electron2D.WebRTCPeerConnection.ConnectionState GetConnectionState()` | Reports the current connection state without delivering events. |
| `public virtual Electron2D.WebRTCPeerConnection.GatheringState GetGatheringState()` | Reports the current ICE gathering state. |
| `public virtual Electron2D.WebRTCPeerConnection.SignalingState GetSignalingState()` | Reports the current SDP signaling state. |
| `public virtual System.Void Initialize(Electron2D.WebRTCConfiguration configuration = null)` | Reinitializes the connection with a copied set of ICE endpoints. |
| `public virtual System.Void Poll()` | Delivers queued native observations once on the constructing thread. |
| `public static System.Void SetDefaultExtension(System.Func<Electron2D.WebRTCPeerConnectionExtension> factory)` | Selects a typed constructor for subsequent Create calls. |
| `public virtual System.Void SetLocalDescription(System.String type, System.String sdp)` | Commits the current emitted local offer or answer and starts native negotiation. |
| `public virtual System.Void SetRemoteDescription(System.String type, System.String sdp)` | Applies remote SDP and prepares an answer for a received offer. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-e7dcc8acff92"></a>
### AddICECandidate(System.String, System.Int32, System.String)

`public virtual System.Void AddICECandidate(System.String media, System.Int32 index, System.String name)`

Adds one remote ICE candidate.

media: Application media identity.

index: The zero-based application media index.

name: Candidate text.

<a id="member-334b4c08ce6a"></a>
### CheckPeerAccess()

`protected System.Void CheckPeerAccess()`

Checks lifetime, owner thread and configuration callback reentry.

<a id="member-07538ee210ac"></a>
### Close()

`public virtual System.Void Close()`

Closes native transport and pending observations without disposing channel wrappers.

<a id="member-b6c47dbae98a"></a>
### Create()

`public static Electron2D.WebRTCPeerConnection Create()`

Constructs the selected extension, or the native implementation when none is selected.

Returns: A caller-owned peer without an open connection.

<a id="member-bd8ac0bb517b"></a>
### CreateDataChannel(System.String, Electron2D.WebRTCDataChannelOptions)

`public virtual Electron2D.WebRTCDataChannel CreateDataChannel(System.String label, Electron2D.WebRTCDataChannelOptions options = null)`

Creates an in-band or explicitly negotiated caller-owned data channel.

label: A UTF-8 channel label of at most 65535 bytes.

options: Null uses reliable ordered in-band delivery.

Returns: A channel belonging to this native cohort.

<a id="member-ea35660ee650"></a>
### CreateOffer()

`public virtual System.Void CreateOffer()`

Generates an SDP offer without committing it or starting ICE publication.

<a id="member-a61dae6cefe9"></a>
### Dispose(System.Boolean)

`protected override System.Void Dispose(System.Boolean disposing)`

Releases resources owned by a derived class.

disposing: true when called from Electron2D.ElectronObject.Dispose.

Remarks: Overrides release managed resources when disposing is true and then call the base implementation.

<a id="member-dbf5bb37a596"></a>
### EmitDataChannelReceived(Electron2D.WebRTCDataChannel)

`protected System.Void EmitDataChannelReceived(Electron2D.WebRTCDataChannel channel)`

Publishes a typed incoming-channel event on the owner thread.

channel: Caller-owned channel.

<a id="member-ad3fcb0b1758"></a>
### EmitICECandidateCreated(System.String, System.Int32, System.String)

`protected System.Void EmitICECandidateCreated(System.String media, System.Int32 index, System.String name)`

Publishes a typed candidate event on the owner thread.

media: Media identity.

index: Media index.

name: Candidate text.

<a id="member-311171ded5de"></a>
### EmitSessionDescriptionCreated(System.String, System.String)

`protected System.Void EmitSessionDescriptionCreated(System.String type, System.String sdp)`

Publishes a typed offer/answer event on the owner thread.

type: Proposal type.

sdp: Proposal text.

<a id="member-ec71f8a2a3d3"></a>
### GetConnectionState()

`public virtual Electron2D.WebRTCPeerConnection.ConnectionState GetConnectionState()`

Reports the current connection state without delivering events.

Returns: The native or initial New state.

<a id="member-76e732d5bb3c"></a>
### GetGatheringState()

`public virtual Electron2D.WebRTCPeerConnection.GatheringState GetGatheringState()`

Reports the current ICE gathering state.

Returns: The native or initial New state.

<a id="member-29472e2d9d1e"></a>
### GetSignalingState()

`public virtual Electron2D.WebRTCPeerConnection.SignalingState GetSignalingState()`

Reports the current SDP signaling state.

Returns: The native or initial Stable state.

<a id="member-143354776fdd"></a>
### Initialize(Electron2D.WebRTCConfiguration)

`public virtual System.Void Initialize(Electron2D.WebRTCConfiguration configuration = null)`

Reinitializes the connection with a copied set of ICE endpoints.

configuration: Null uses host candidates without STUN/TURN servers.

Remarks: Invalid input/setup preserves the old cohort. Successful replacement closes its channels without disposing their caller-owned wrappers.

<a id="member-4cdea79a799b"></a>
### Poll()

`public virtual System.Void Poll()`

Delivers queued native observations once on the constructing thread.

Remarks: Events commit before callbacks. Reinitialization/disposal ends delivery for the old cohort. Callback errors aggregate after other selected subscribers.

<a id="member-a42965520f47"></a>
### SetDefaultExtension(System.Func<Electron2D.WebRTCPeerConnectionExtension>)

`public static System.Void SetDefaultExtension(System.Func<Electron2D.WebRTCPeerConnectionExtension> factory)`

Selects a typed constructor for subsequent Create calls.

factory: An extension factory, or null to restore the native implementation.

<a id="member-27cfb2684cd0"></a>
### SetLocalDescription(System.String, System.String)

`public virtual System.Void SetLocalDescription(System.String type, System.String sdp)`

Commits the current emitted local offer or answer and starts native negotiation.

type: The emitted proposal type.

sdp: The current emitted proposal text; stale or foreign transport identity rejects.

<a id="member-654ae0314520"></a>
### SetRemoteDescription(System.String, System.String)

`public virtual System.Void SetRemoteDescription(System.String type, System.String sdp)`

Applies remote SDP and prepares an answer for a received offer.

type: Remote description type.

sdp: Remote SDP text.

<a id="member-00da8a5bd8aa"></a>
### ValidateDisposal()

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<Electron2D.WebRTCDataChannel> DataChannelReceived` | Occurs on Poll when an in-band remote channel is ready to be delivered. |
| `public event System.Action<System.String, System.Int32, System.String> ICECandidateCreated` | Occurs on Poll with a media identity, index and local candidate. |
| `public event System.Action<System.String, System.String> SessionDescriptionCreated` | Occurs on Poll with an offer/answer to commit and send to the remote peer. |

## Event Descriptions

<a id="member-3f638092df38"></a>
### DataChannelReceived

`public event System.Action<Electron2D.WebRTCDataChannel> DataChannelReceived`

Occurs on Poll when an in-band remote channel is ready to be delivered.

<a id="member-2f4153fabceb"></a>
### ICECandidateCreated

`public event System.Action<System.String, System.Int32, System.String> ICECandidateCreated`

Occurs on Poll with a media identity, index and local candidate.

<a id="member-959816883fec"></a>
### SessionDescriptionCreated

`public event System.Action<System.String, System.String> SessionDescriptionCreated`

Occurs on Poll with an offer/answer to commit and send to the remote peer.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
