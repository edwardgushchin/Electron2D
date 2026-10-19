# WebRTCPeerConnectionExtension

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.WebRTCPeerConnectionExtension`. **Source:** [WebRTCPeerConnectionExtension.cs](../../src/Core/Networking/WebRTCPeerConnectionExtension.cs). **Component:** [WebRTC](../components/webrtc.md).

**Inherits:** [WebRTCPeerConnection](WebRTCPeerConnection.md).

## Description

Defines the typed virtual peer contract for an application-supplied transport.

Derive a concrete provider and implement every abstract operation. Each override checks CheckPeerAccess before touching state. Publish typed descriptions, ICE candidates and incoming channels through inherited emission helpers; subscribers receive the actual caller-owned channel. Register a factory with WebRTCPeerConnection.SetDefaultExtension and restore null to select native construction. No native pointer or reflected class-name ABI is public. Providers that retain backend resources override Dispose(bool) to release them and call base.Dispose; Close implements their explicit transport retirement.

## Example

Public C# excerpt. Channel/observation examples require the indicated application transport; MyPeer/applicationChannel stand for a complete application extension. The peer example generates and commits only its local offer; a remote peer and an application signaling path are required for connectivity. Equivalent complete native and scene workflows execute in WebRTCTests.

```csharp
WebRTCPeerConnection.SetDefaultExtension(static () => new MyPeer());
using var peer = WebRTCPeerConnection.Create();
WebRTCPeerConnection.SetDefaultExtension(null);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected WebRTCPeerConnectionExtension()` | Creates an extension identity on the current owner thread. |

## Constructor Descriptions

<a id="member-f8412267e4b6"></a>
### WebRTCPeerConnectionExtension()

`protected WebRTCPeerConnectionExtension()`

Creates an extension identity on the current owner thread.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public abstract System.Void AddICECandidate(System.String media, System.Int32 index, System.String name)` | Adds one remote ICE candidate. |
| `public abstract System.Void Close()` | Closes native transport and pending observations without disposing channel wrappers. |
| `public abstract Electron2D.WebRTCDataChannel CreateDataChannel(System.String label, Electron2D.WebRTCDataChannelOptions options = null)` | Creates an in-band or explicitly negotiated caller-owned data channel. |
| `public abstract System.Void CreateOffer()` | Generates an SDP offer without committing it or starting ICE publication. |
| `public abstract Electron2D.WebRTCPeerConnection.ConnectionState GetConnectionState()` | Reports the current connection state without delivering events. |
| `public abstract Electron2D.WebRTCPeerConnection.GatheringState GetGatheringState()` | Reports the current ICE gathering state. |
| `public abstract Electron2D.WebRTCPeerConnection.SignalingState GetSignalingState()` | Reports the current SDP signaling state. |
| `public abstract System.Void Initialize(Electron2D.WebRTCConfiguration configuration = null)` | Reinitializes the connection with a copied set of ICE endpoints. |
| `public abstract System.Void Poll()` | Delivers queued native observations once on the constructing thread. |
| `public abstract System.Void SetLocalDescription(System.String type, System.String sdp)` | Commits the current emitted local offer or answer and starts native negotiation. |
| `public abstract System.Void SetRemoteDescription(System.String type, System.String sdp)` | Applies remote SDP and prepares an answer for a received offer. |

## Method Descriptions

<a id="member-e3c4e6045582"></a>
### AddICECandidate(System.String, System.Int32, System.String)

`public abstract System.Void AddICECandidate(System.String media, System.Int32 index, System.String name)`

Adds one remote ICE candidate.

media: Application media identity.

index: The zero-based application media index.

name: Candidate text.

<a id="member-0562fcc5393b"></a>
### Close()

`public abstract System.Void Close()`

Closes native transport and pending observations without disposing channel wrappers.

<a id="member-a8cc9e57895d"></a>
### CreateDataChannel(System.String, Electron2D.WebRTCDataChannelOptions)

`public abstract Electron2D.WebRTCDataChannel CreateDataChannel(System.String label, Electron2D.WebRTCDataChannelOptions options = null)`

Creates an in-band or explicitly negotiated caller-owned data channel.

label: A UTF-8 channel label of at most 65535 bytes.

options: Null uses reliable ordered in-band delivery.

Returns: A channel belonging to this native cohort.

<a id="member-ac13d38347dc"></a>
### CreateOffer()

`public abstract System.Void CreateOffer()`

Generates an SDP offer without committing it or starting ICE publication.

<a id="member-e2f148b7dc7d"></a>
### GetConnectionState()

`public abstract Electron2D.WebRTCPeerConnection.ConnectionState GetConnectionState()`

Reports the current connection state without delivering events.

Returns: The native or initial New state.

<a id="member-ae062905d777"></a>
### GetGatheringState()

`public abstract Electron2D.WebRTCPeerConnection.GatheringState GetGatheringState()`

Reports the current ICE gathering state.

Returns: The native or initial New state.

<a id="member-0ea72a5c9456"></a>
### GetSignalingState()

`public abstract Electron2D.WebRTCPeerConnection.SignalingState GetSignalingState()`

Reports the current SDP signaling state.

Returns: The native or initial Stable state.

<a id="member-ed56dd8806a6"></a>
### Initialize(Electron2D.WebRTCConfiguration)

`public abstract System.Void Initialize(Electron2D.WebRTCConfiguration configuration = null)`

Reinitializes the connection with a copied set of ICE endpoints.

configuration: Null uses host candidates without STUN/TURN servers.

Remarks: Invalid input/setup preserves the old cohort. Successful replacement closes its channels without disposing their caller-owned wrappers.

<a id="member-2fa363504a5f"></a>
### Poll()

`public abstract System.Void Poll()`

Delivers queued native observations once on the constructing thread.

Remarks: Events commit before callbacks. Reinitialization/disposal ends delivery for the old cohort. Callback errors aggregate after other selected subscribers.

<a id="member-f0fa3b993412"></a>
### SetLocalDescription(System.String, System.String)

`public abstract System.Void SetLocalDescription(System.String type, System.String sdp)`

Commits the current emitted local offer or answer and starts native negotiation.

type: The emitted proposal type.

sdp: The current emitted proposal text; stale or foreign transport identity rejects.

<a id="member-5d33997c3f61"></a>
### SetRemoteDescription(System.String, System.String)

`public abstract System.Void SetRemoteDescription(System.String type, System.String sdp)`

Applies remote SDP and prepares an answer for a received offer.

type: Remote description type.

sdp: Remote SDP text.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
