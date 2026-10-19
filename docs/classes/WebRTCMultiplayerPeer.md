# WebRTCMultiplayerPeer

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.WebRTCMultiplayerPeer`. **Source:** [WebRTCMultiplayerPeer.cs](../../src/Core/Networking/WebRTCMultiplayerPeer.cs). **Component:** [WebRTC](../components/webrtc.md).

**Inherits:** [MultiplayerPeer](MultiplayerPeer.md).

## Description

Routes multiplayer packets through explicitly negotiated WebRTC connections.

Owns generated negotiated channels while borrowing the supplied connections. Three reserved modes and copied additional channel configuration route real packets with a 1200-byte ceiling. Membership snapshots contain borrowed identities. Setup rollback attempts all channels and aggregates errors; polling retires failed channels and commits membership before events. Removing a membership preserves its caller connection. SceneMultiplayer consumes this peer directly, including automatic SceneTree polling and typed RPC.

## Example

Public C# excerpt. Channel/observation examples require the indicated application transport; MyPeer/applicationChannel stand for a complete application extension. The peer example generates and commits only its local offer; a remote peer and an application signaling path are required for connectivity. Equivalent complete native and scene workflows execute in WebRTCTests.

```csharp
using var transport = new WebRTCMultiplayerPeer();
using var connection = WebRTCPeerConnection.Create();
transport.CreateServer([TransferMode.Reliable]);
transport.AddPeer(connection, 7);
using var scene = new SceneMultiplayer { MultiplayerPeer = transport };
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public WebRTCMultiplayerPeer()` | Creates an unconfigured multiplayer endpoint. |

## Constructor Descriptions

<a id="member-2477b5b66c7b"></a>
### WebRTCMultiplayerPeer()

`public WebRTCMultiplayerPeer()`

Creates an unconfigured multiplayer endpoint.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean RefuseNewConnections { get; set; }` | Gets or sets whether a server admits new remote peers. |
| `public System.Int32 TransferChannel { get; set; }` | Gets or sets the requested outgoing transport channel. |
| `public Electron2D.TransferMode TransferMode { get; set; }` | Gets or sets the requested delivery mode. |

## Property Descriptions

<a id="member-38744cb0a701"></a>
### RefuseNewConnections

`public System.Boolean RefuseNewConnections { get; set; }`

Gets or sets whether a server admits new remote peers.

Value: False initially; existing connections remain usable.

<a id="member-71f8ca88e690"></a>
### TransferChannel

`public System.Int32 TransferChannel { get; set; }`

Gets or sets the requested outgoing transport channel.

Value: Zero initially; nonnegative. A transport may provide only channel zero.

T:System.ArgumentOutOfRangeException: The requested channel is negative.

<a id="member-f9cc4f20f10f"></a>
### TransferMode

`public Electron2D.TransferMode TransferMode { get; set; }`

Gets or sets the requested delivery mode.

Value: Reliable initially; a reliable-only transport can ignore the selected mode on the wire.

T:System.ArgumentOutOfRangeException: The selector is invalid.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddPeer(Electron2D.WebRTCPeerConnection peer, System.Int32 peerID, System.Int32 unreliableLifetime = 1)` | Adds a new, caller-owned connection and creates its negotiated channel cohort. |
| `public override System.Void Close()` | Immediately releases all owned transport state without disconnection events. |
| `public System.Void CreateClient(System.Int32 peerID, System.Collections.Generic.IReadOnlyList<Electron2D.TransferMode> channelsConfig = null)` | Creates a client endpoint which connects to server identity one. |
| `public System.Void CreateMesh(System.Int32 peerID, System.Collections.Generic.IReadOnlyList<Electron2D.TransferMode> channelsConfig = null)` | Creates an explicitly connected mesh endpoint. |
| `public System.Void CreateServer(System.Collections.Generic.IReadOnlyList<Electron2D.TransferMode> channelsConfig = null)` | Creates a server endpoint with identity one. |
| `public override System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)` | Disconnects one remote peer. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Releases resources owned by a derived class. |
| `public override System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public override Electron2D.MultiplayerConnectionStatus GetConnectionStatus()` | Gets the cached local connection status. |
| `public override System.Int32 GetMaxPacketSize()` | Reports the maximum outgoing raw payload supported by this transport. |
| `public override System.Int32 GetPacketChannel()` | Gets the next packet's transport channel. |
| `public override Electron2D.TransferMode GetPacketMode()` | Gets the next packet's actual delivery mode. |
| `public override System.Int32 GetPacketPeer()` | Gets the source identity of the next queued packet. |
| `public Electron2D.WebRTCPeerInfo GetPeer(System.Int32 peerID)` | Returns a copied observation of one registered peer. |
| `public System.Collections.Generic.IReadOnlyDictionary<System.Int32, Electron2D.WebRTCPeerInfo> GetPeers()` | Returns copied membership observations keyed by remote identity. |
| `public override System.Int32 GetUniqueID()` | Gets the local multiplayer identity. |
| `public System.Boolean HasPeer(System.Int32 peerID)` | Reports current membership. |
| `public override System.Boolean IsServer()` | Reports whether this endpoint acts as server. |
| `public override System.Boolean IsServerRelaySupported()` | Reports whether a higher multiplayer layer can implement server relay through this transport. |
| `protected override System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public override System.Void Poll()` | Advances connection and packet progress using the concrete transport's polling policy. |
| `public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `public System.Void RemovePeer(System.Int32 peerID)` | Removes one membership and its generated channels, preserving the caller-owned connection. |
| `public override System.Void SetTargetPeer(System.Int32 id)` | Chooses broadcast, a positive peer, or all peers except a negative ID. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-704da2ea7ea7"></a>
### AddPeer(Electron2D.WebRTCPeerConnection, System.Int32, System.Int32)

`public System.Void AddPeer(Electron2D.WebRTCPeerConnection peer, System.Int32 peerID, System.Int32 unreliableLifetime = 1)`

Adds a new, caller-owned connection and creates its negotiated channel cohort.

peer: Connection in New state; it remains caller-owned.

peerID: Positive remote identity distinct from local and existing identities.

unreliableLifetime: Reserved unreliable-channel lifetime in milliseconds, zero through 65535.

<a id="member-5aaea77c0c41"></a>
### Close()

`public override System.Void Close()`

Immediately releases all owned transport state without disconnection events.

<a id="member-000af75a8715"></a>
### CreateClient(System.Int32, System.Collections.Generic.IReadOnlyList<Electron2D.TransferMode>)

`public System.Void CreateClient(System.Int32 peerID, System.Collections.Generic.IReadOnlyList<Electron2D.TransferMode> channelsConfig = null)`

Creates a client endpoint which connects to server identity one.

peerID: Local identity greater than one.

channelsConfig: Copied additional channel modes.

<a id="member-4e8ea7be8693"></a>
### CreateMesh(System.Int32, System.Collections.Generic.IReadOnlyList<Electron2D.TransferMode>)

`public System.Void CreateMesh(System.Int32 peerID, System.Collections.Generic.IReadOnlyList<Electron2D.TransferMode> channelsConfig = null)`

Creates an explicitly connected mesh endpoint.

peerID: Positive local identity.

channelsConfig: Copied additional channel modes.

<a id="member-d387ae71e865"></a>
### CreateServer(System.Collections.Generic.IReadOnlyList<Electron2D.TransferMode>)

`public System.Void CreateServer(System.Collections.Generic.IReadOnlyList<Electron2D.TransferMode> channelsConfig = null)`

Creates a server endpoint with identity one.

channelsConfig: Copied additional channel modes; null selects only the three reserved channels.

<a id="member-d5cd53706515"></a>
### DisconnectPeer(System.Int32, System.Boolean)

`public override System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)`

Disconnects one remote peer.

peer: Remote identity.

force: True suppresses the local disconnection event and tears down immediately.

<a id="member-04356a42c35a"></a>
### Dispose(System.Boolean)

`protected override System.Void Dispose(System.Boolean disposing)`

Releases resources owned by a derived class.

disposing: true when called from Electron2D.ElectronObject.Dispose.

Remarks: Overrides release managed resources when disposing is true and then call the base implementation.

<a id="member-d8c59601dcb5"></a>
### GetAvailablePacketCount()

`public override System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-09c7f90a2b35"></a>
### GetConnectionStatus()

`public override Electron2D.MultiplayerConnectionStatus GetConnectionStatus()`

Gets the cached local connection status.

Returns: The latest polled status.

<a id="member-bf5ea0e3dae2"></a>
### GetMaxPacketSize()

`public override System.Int32 GetMaxPacketSize()`

Reports the maximum outgoing raw payload supported by this transport.

Returns: A nonnegative byte count; operating-system limits may be narrower.

<a id="member-48f8e8de880c"></a>
### GetPacketChannel()

`public override System.Int32 GetPacketChannel()`

Gets the next packet's transport channel.

Returns: The receiving channel; zero for single-channel transports.

<a id="member-c7af820abe31"></a>
### GetPacketMode()

`public override Electron2D.TransferMode GetPacketMode()`

Gets the next packet's actual delivery mode.

Returns: The wire mode, independent of requested outgoing configuration.

<a id="member-3638b34a6f29"></a>
### GetPacketPeer()

`public override System.Int32 GetPacketPeer()`

Gets the source identity of the next queued packet.

Returns: A peer ID; empty-queue behavior is transport-specific.

<a id="member-38fca1ba732f"></a>
### GetPeer(System.Int32)

`public Electron2D.WebRTCPeerInfo GetPeer(System.Int32 peerID)`

Returns a copied observation of one registered peer.

peerID: Remote identity.

Returns: A snapshot with borrowed resources, or null if absent.

<a id="member-053bd07eca32"></a>
### GetPeers()

`public System.Collections.Generic.IReadOnlyDictionary<System.Int32, Electron2D.WebRTCPeerInfo> GetPeers()`

Returns copied membership observations keyed by remote identity.

Returns: A caller-owned dictionary snapshot containing borrowed resource identities.

<a id="member-a11699a21600"></a>
### GetUniqueID()

`public override System.Int32 GetUniqueID()`

Gets the local multiplayer identity.

Returns: One for servers, greater than one for assigned clients, or zero while disconnected/connecting.

<a id="member-97c803fd2cf5"></a>
### HasPeer(System.Int32)

`public System.Boolean HasPeer(System.Int32 peerID)`

Reports current membership.

peerID: Remote identity.

Returns: True while registered, including pending negotiation.

<a id="member-6c3fe16d39bb"></a>
### IsServer()

`public override System.Boolean IsServer()`

Reports whether this endpoint acts as server.

Returns: The current server role.

<a id="member-e2b0cf6595c9"></a>
### IsServerRelaySupported()

`public override System.Boolean IsServerRelaySupported()`

Reports whether a higher multiplayer layer can implement server relay through this transport.

Returns: False by default; this does not automatically relay application bytes.

<a id="member-f2c1b7bd093f"></a>
### NextPacketSize()

`protected override System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-479a40040a83"></a>
### Poll()

`public override System.Void Poll()`

Advances connection and packet progress using the concrete transport's polling policy.

<a id="member-e68944fa2d5d"></a>
### PutPacket(System.ReadOnlySpan<System.Byte>)

`public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

data: Packet bytes, borrowed only for this call.

<a id="member-3fb0bc5197e0"></a>
### ReadPacketCore(System.Span<System.Byte>)

`protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

destination: An exact-sized destination.

<a id="member-fb3a698ef094"></a>
### RemovePeer(System.Int32)

`public System.Void RemovePeer(System.Int32 peerID)`

Removes one membership and its generated channels, preserving the caller-owned connection.

peerID: Registered remote identity.

<a id="member-92d6eae15ff7"></a>
### SetTargetPeer(System.Int32)

`public override System.Void SetTargetPeer(System.Int32 id)`

Chooses broadcast, a positive peer, or all peers except a negative ID.

id: Zero broadcasts; one is server; int.MinValue cannot denote an exclusion.

<a id="member-72aa858ff319"></a>
### ValidateDisposal()

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
