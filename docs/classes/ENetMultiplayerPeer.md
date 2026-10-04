# ENetMultiplayerPeer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.ENetMultiplayerPeer`. **Source:** [ENetMultiplayerPeer.cs](../../src/Core/Networking/ENetMultiplayerPeer.cs).

## Description

Provides server/client and manually connected mesh multiplayer over native ENet UDP channels.

Channel zero uses separate reliable/unreliable native channels; positive channels use additional negotiated channels. Client/server topology supports higher-layer relay. Mesh hosts transfer their active protocol lifetime to this endpoint: Close destroys them while leaving borrowed objects undisposed. Incoming native packets retain exact metadata until consumed. All calls require the constructing thread.

See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Example

The public excerpt compiles and executes in the documentation consumer. The executable tests supply complete connection/poll/scene flows.

```csharp
using var server = new ENetMultiplayerPeer();
server.SetBindIP("127.0.0.1");
server.CreateServer(0, maxClients: 4, maxChannels: 2);
using var client = new ENetMultiplayerPeer();
client.CreateClient("127.0.0.1", server.Host!.GetLocalPort(), channelCount: 2);
using var api = new SceneMultiplayer { MultiplayerPeer = client };
api.Poll();
// Continue owner-thread polling on both endpoints before sending scene messages.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public ENetMultiplayerPeer()` | Creates a disconnected ENet multiplayer endpoint. |

## Constructor Descriptions

<a id="member-d65aa1401366"></a>
### .ctor

`public ENetMultiplayerPeer()`

Creates a disconnected ENet multiplayer endpoint.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.ENetConnection Host { get;  }` | Gets the current client/server host. |
| `public System.Boolean RefuseNewConnections { get; set; }` | Gets or sets whether a server admits new remote peers. |

## Property Descriptions

<a id="member-d5efb8488ab5"></a>
### Host

`public Electron2D.ENetConnection Host { get;  }`

Gets the current client/server host.

Value: Borrowed active host, or null in idle/mesh mode. Direct Service/Destroy/disposal disrupts the multiplayer endpoint.

<a id="member-6e863283b5e5"></a>
### RefuseNewConnections

`public System.Boolean RefuseNewConnections { get; set; }`

Gets or sets whether a server admits new remote peers.

Value: False initially; existing connections remain usable.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddMeshPeer(System.Int32 peerID, Electron2D.ENetConnection host)` | Adds an already negotiated single-peer host to a mesh. |
| `public override System.Void Close()` | Immediately releases all owned transport state without disconnection events. |
| `public System.Void CreateClient(System.String address, System.Int32 port, System.Int32 channelCount = 0, System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0, System.Int32 localPort = 0)` | Creates a UDP client and begins peer negotiation. |
| `public System.Void CreateMesh(System.Int32 uniqueID)` | Creates a manually connected mesh endpoint. |
| `public System.Void CreateServer(System.Int32 port, System.Int32 maxClients = 32, System.Int32 maxChannels = 0, System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0)` | Creates a UDP server. |
| `public override System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)` | Disconnects one remote peer. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `protected override System.Void Finalize()` | Releases unread native packets when deterministic disposal was omitted. |
| `public override System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public override Electron2D.MultiplayerConnectionStatus GetConnectionStatus()` | Gets the cached local connection status. |
| `public override System.Int32 GetMaxPacketSize()` | Gets the maximum application packet capacity. |
| `public override System.Int32 GetPacketChannel()` | Gets the next packet's transport channel. |
| `public override Electron2D.TransferMode GetPacketMode()` | Gets the next packet's actual delivery mode. |
| `public override System.Int32 GetPacketPeer()` | Gets the source identity of the next queued packet. |
| `public Electron2D.ENetPacketPeer GetPeer(System.Int32 id)` | Returns an associated borrowed native peer. |
| `public override System.Int32 GetUniqueID()` | Gets the local multiplayer identity. |
| `public override System.Boolean IsServer()` | Reports whether this endpoint acts as server. |
| `public override System.Boolean IsServerRelaySupported()` | Reports whether a higher multiplayer layer can implement server relay through this transport. |
| `protected override System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public override System.Void Poll()` | Advances connection and packet progress using the concrete transport's polling policy. |
| `public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `public System.Void SetBindIP(System.String ip)` | Sets the bind IP for future client/server hosts. |
| `public override System.Void SetTargetPeer(System.Int32 id)` | Chooses broadcast, a positive peer, or all peers except a negative ID. |

## Method Descriptions

<a id="member-a822964d87b1"></a>
### AddMeshPeer

`public System.Void AddMeshPeer(System.Int32 peerID, Electron2D.ENetConnection host)`

Adds an already negotiated single-peer host to a mesh.

Membership is committed before the connection event. The host must not be shared with another mesh.

- `peerID`: Positive remote identity distinct from local/existing identities.
- `host`: Host with exactly one connected peer. Close destroys its protocol state.

<a id="member-ad4a827e2946"></a>
### Close

`public override System.Void Close()`

Immediately releases all owned transport state without disconnection events.

<a id="member-6218e787f224"></a>
### CreateClient

`public System.Void CreateClient(System.String address, System.Int32 port, System.Int32 channelCount = 0, System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0, System.Int32 localPort = 0)`

Creates a UDP client and begins peer negotiation.

- `address`: DNS name or IPv4/IPv6 literal.
- `port`: Server port.
- `channelCount`: Zero selects all channels; one through 253 adds two reserved channels.
- `inBandwidth`: Incoming byte rate; zero unlimited.
- `outBandwidth`: Outgoing byte rate; zero unlimited.
- `localPort`: Zero selects automatic binding.

<a id="member-bdced13e0527"></a>
### CreateMesh

`public System.Void CreateMesh(System.Int32 uniqueID)`

Creates a manually connected mesh endpoint.

- `uniqueID`: Positive local identity.

<a id="member-0bffc2fe4c3f"></a>
### CreateServer

`public System.Void CreateServer(System.Int32 port, System.Int32 maxClients = 32, System.Int32 maxChannels = 0, System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0)`

Creates a UDP server.

- `port`: Zero selects an ephemeral port.
- `maxClients`: One through 4095.
- `maxChannels`: Zero selects all channels; one through 253 adds two reserved channels.
- `inBandwidth`: Incoming byte rate; zero unlimited.
- `outBandwidth`: Outgoing byte rate; zero unlimited.

<a id="member-1cb0df12bf87"></a>
### DisconnectPeer

`public override System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)`

Disconnects one remote peer.

- `peer`: Remote identity.
- `force`: True suppresses the local disconnection event and tears down immediately.

<a id="member-4dd507c448e2"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

- `System.AggregateException`: Both notification delivery and derived cleanup fail.
- `System.Exception`: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-d82dfba152f5"></a>
### Finalize

`protected override System.Void Finalize()`

Releases unread native packets when deterministic disposal was omitted.

<a id="member-cd8a95bc2962"></a>
### GetAvailablePacketCount

`public override System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-ef8634fd105f"></a>
### GetConnectionStatus

`public override Electron2D.MultiplayerConnectionStatus GetConnectionStatus()`

Gets the cached local connection status.

Returns: The latest polled status.

<a id="member-c3320c8f14cd"></a>
### GetMaxPacketSize

`public override System.Int32 GetMaxPacketSize()`

Gets the maximum application packet capacity.

Returns: 16 MiB minus nine bytes reserved by higher multiplayer protocols.

<a id="member-6eb5e8618af6"></a>
### GetPacketChannel

`public override System.Int32 GetPacketChannel()`

Gets the next packet's transport channel.

Returns: The receiving channel; zero for single-channel transports.

<a id="member-921cfdf45430"></a>
### GetPacketMode

`public override Electron2D.TransferMode GetPacketMode()`

Gets the next packet's actual delivery mode.

Returns: The wire mode, independent of requested outgoing configuration.

<a id="member-3e54cf3cf043"></a>
### GetPacketPeer

`public override System.Int32 GetPacketPeer()`

Gets the source identity of the next queued packet.

Returns: A peer ID; empty-queue behavior is transport-specific.

<a id="member-4346ed6ff169"></a>
### GetPeer

`public Electron2D.ENetPacketPeer GetPeer(System.Int32 id)`

Returns an associated borrowed native peer.

Returns: Peer, or null when absent.

- `id`: Remote identity.

<a id="member-4965c62b3d40"></a>
### GetUniqueID

`public override System.Int32 GetUniqueID()`

Gets the local multiplayer identity.

Returns: One for servers, greater than one for assigned clients, or zero while disconnected/connecting.

<a id="member-91fefd559fa0"></a>
### IsServer

`public override System.Boolean IsServer()`

Reports whether this endpoint acts as server.

Returns: The current server role.

<a id="member-6c28a6755e1e"></a>
### IsServerRelaySupported

`public override System.Boolean IsServerRelaySupported()`

Reports whether a higher multiplayer layer can implement server relay through this transport.

Returns: False by default; this does not automatically relay application bytes.

<a id="member-a930c137d9c7"></a>
### NextPacketSize

`protected override System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-7e79d7b6320c"></a>
### Poll

`public override System.Void Poll()`

Advances connection and packet progress using the concrete transport's polling policy.

<a id="member-55efee6a375a"></a>
### PutPacket

`public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

- `data`: Packet bytes, borrowed only for this call.

<a id="member-50bd65715170"></a>
### ReadPacketCore

`protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

- `destination`: An exact-sized destination.

<a id="member-ec327b95cbab"></a>
### SetBindIP

`public System.Void SetBindIP(System.String ip)`

Sets the bind IP for future client/server hosts.

- `ip`: IPv4/IPv6 literal or wildcard.

<a id="member-c8426f04f8ae"></a>
### SetTargetPeer

`public override System.Void SetTargetPeer(System.Int32 id)`

Chooses broadcast, a positive peer, or all peers except a negative ID.

- `id`: Zero broadcasts; one is server; int.MinValue cannot denote an exclusion.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
