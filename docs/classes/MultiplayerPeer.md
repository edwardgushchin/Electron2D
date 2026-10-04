# MultiplayerPeer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.MultiplayerPeer`. **Inherits:** [PacketPeer](PacketPeer.md). **Inherited by:** [OfflineMultiplayerPeer](OfflineMultiplayerPeer.md), [WebSocketMultiplayerPeer](WebSocketMultiplayerPeer.md). **Source:** [MultiplayerPeer.cs](../../src/Core/Networking/MultiplayerPeer.cs).

## Description

Defines typed peer identity, packet metadata, routing and connection events for multiplayer transports.

Calls and disposal require the constructing thread. Implementations inherit the span packet hooks from PacketPeer and override lifecycle, metadata and configurable delivery properties. Transport-specific capabilities determine whether transfer channel/mode settings affect the wire. Scene replication is a separate consumer.

[Multiplayer transports](../components/multiplayer.md) and [ADR 0094](../decisions/networking.md#adr-0094) define lifecycle/routing, prepared capacities, admission, metadata, event failure/reentry and native ownership. PacketPeer snapshot/span reads and last-read failure state remain inherited. Properties expose typed descriptors. Higher SceneTree/Node multiplayer, RPC and replication are separate consumers.

## Example

Public API excerpt; MultiplayerPeer requires a caller-provided configured concrete transport. The WebSocket example requires port 8080 to be available and continued owner-thread polling. MultiplayerTests exercises full native and public scene workflows; compilation alone does not establish a remote server or rendered output.

```csharp
MultiplayerPeer peer = configuredTransport;
peer.Poll();
if (peer.GetAvailablePacketCount() > 0)
{
    int source = peer.GetPacketPeer();
    int channel = peer.GetPacketChannel();
    TransferMode mode = peer.GetPacketMode();
    byte[] payload = peer.GetPacket();
}
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected MultiplayerPeer()` | Creates a multiplayer transport owned by the current thread. |

## Constructor Descriptions

<a id="member-bdc04595f547"></a>
### .ctor

`protected MultiplayerPeer()`

Creates a multiplayer transport owned by the current thread.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean RefuseNewConnections { get; set; }` | Gets or sets whether a server admits new remote peers. |
| `public System.Int32 TransferChannel { get; set; }` | Gets or sets the requested outgoing transport channel. |
| `public Electron2D.TransferMode TransferMode { get; set; }` | Gets or sets the requested delivery mode. |

## Property Descriptions

<a id="member-e257ba01d19b"></a>
### RefuseNewConnections

`public System.Boolean RefuseNewConnections { get; set; }`

Gets or sets whether a server admits new remote peers.

Value: False initially; existing connections remain usable.

<a id="member-84dd9135c23b"></a>
### TransferChannel

`public System.Int32 TransferChannel { get; set; }`

Gets or sets the requested outgoing transport channel.

Value: Zero initially; nonnegative. A transport may provide only channel zero.

System.ArgumentOutOfRangeException: The requested channel is negative.

<a id="member-70fb0067f19a"></a>
### TransferMode

`public Electron2D.TransferMode TransferMode { get; set; }`

Gets or sets the requested delivery mode.

Value: Reliable initially; a reliable-only transport can ignore the selected mode on the wire.

System.ArgumentOutOfRangeException: The selector is invalid.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public abstract System.Void Close()` | Immediately releases all owned transport state without disconnection events. |
| `public abstract System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)` | Disconnects one remote peer. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `protected System.Void EmitPeerConnected(System.Int32 id)` | Delivers a committed connection event on the owner thread. |
| `protected System.Void EmitPeerDisconnected(System.Int32 id)` | Delivers a committed disconnection event on the owner thread. |
| `public System.Int32 GenerateUniqueID()` | Generates a positive random peer identity distinct from broadcast and server. |
| `public abstract Electron2D.MultiplayerConnectionStatus GetConnectionStatus()` | Gets the cached local connection status. |
| `public abstract System.Int32 GetPacketChannel()` | Gets the next packet's transport channel. |
| `public abstract Electron2D.TransferMode GetPacketMode()` | Gets the next packet's actual delivery mode. |
| `public abstract System.Int32 GetPacketPeer()` | Gets the source identity of the next queued packet. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public abstract System.Int32 GetUniqueID()` | Gets the local multiplayer identity. |
| `public abstract System.Boolean IsServer()` | Reports whether this endpoint acts as server. |
| `public virtual System.Boolean IsServerRelaySupported()` | Reports whether a higher multiplayer layer can implement server relay through this transport. |
| `public abstract System.Void Poll()` | Advances connection and packet progress using the concrete transport's polling policy. |
| `public abstract System.Void SetTargetPeer(System.Int32 id)` | Chooses broadcast, a positive peer, or all peers except a negative ID. |

## Method Descriptions

<a id="member-aab1c5ee35cc"></a>
### Close

`public abstract System.Void Close()`

Immediately releases all owned transport state without disconnection events.

<a id="member-1b11b8504f6d"></a>
### DisconnectPeer

`public abstract System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)`

Disconnects one remote peer.

peer: Remote identity.

force: True suppresses the local disconnection event and tears down immediately.

<a id="member-0db1fbb67a4e"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-3df258f762f2"></a>
### EmitPeerConnected

`protected System.Void EmitPeerConnected(System.Int32 id)`

Delivers a committed connection event on the owner thread.

id: Remote peer identity.

System.AggregateException: Subscribers failed after every subscriber was attempted.

<a id="member-3f16bcf1f140"></a>
### EmitPeerDisconnected

`protected System.Void EmitPeerDisconnected(System.Int32 id)`

Delivers a committed disconnection event on the owner thread.

id: Detached peer identity.

System.AggregateException: Subscribers failed after every subscriber was attempted.

<a id="member-5d3635bd95df"></a>
### GenerateUniqueID

`public System.Int32 GenerateUniqueID()`

Generates a positive random peer identity distinct from broadcast and server.

Returns: Two through int.MaxValue; a host must still reject identity collisions.

<a id="member-e8913f1a0176"></a>
### GetConnectionStatus

`public abstract Electron2D.MultiplayerConnectionStatus GetConnectionStatus()`

Gets the cached local connection status.

Returns: The latest polled status.

<a id="member-93b5d66877d8"></a>
### GetPacketChannel

`public abstract System.Int32 GetPacketChannel()`

Gets the next packet's transport channel.

Returns: The receiving channel; zero for single-channel transports.

<a id="member-6e335f8a5711"></a>
### GetPacketMode

`public abstract Electron2D.TransferMode GetPacketMode()`

Gets the next packet's actual delivery mode.

Returns: The wire mode, independent of requested outgoing configuration.

<a id="member-fe708fbdea6f"></a>
### GetPacketPeer

`public abstract System.Int32 GetPacketPeer()`

Gets the source identity of the next queued packet.

Returns: A peer ID; empty-queue behavior is transport-specific.

<a id="member-9a89fc512073"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-f7bb2773752b"></a>
### GetUniqueID

`public abstract System.Int32 GetUniqueID()`

Gets the local multiplayer identity.

Returns: One for servers, greater than one for assigned clients, or zero while disconnected/connecting.

<a id="member-e85a30f958f8"></a>
### IsServer

`public abstract System.Boolean IsServer()`

Reports whether this endpoint acts as server.

Returns: The current server role.

<a id="member-e53bf643e777"></a>
### IsServerRelaySupported

`public virtual System.Boolean IsServerRelaySupported()`

Reports whether a higher multiplayer layer can implement server relay through this transport.

Returns: False by default; this does not automatically relay application bytes.

<a id="member-9711f2e1ea3f"></a>
### Poll

`public abstract System.Void Poll()`

Advances connection and packet progress using the concrete transport's polling policy.

<a id="member-5fc3d49c4e41"></a>
### SetTargetPeer

`public abstract System.Void SetTargetPeer(System.Int32 id)`

Chooses broadcast, a positive peer, or all peers except a negative ID.

id: Zero broadcasts; one is server; int.MinValue cannot denote an exclusion.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<System.Int32> PeerConnected` | Occurs after a remote peer and its identity have been committed. |
| `public event System.Action<System.Int32> PeerDisconnected` | Occurs after a remote peer has been detached; Close and forced local disconnect suppress it. |

## Event Descriptions

<a id="member-00b6d29edafe"></a>
### PeerConnected

`public event System.Action<System.Int32> PeerConnected`

Occurs after a remote peer and its identity have been committed.

Remarks: All subscribers run on the owner thread; failures aggregate after remaining subscribers.

<a id="member-f078ada7170b"></a>
### PeerDisconnected

`public event System.Action<System.Int32> PeerDisconnected`

Occurs after a remote peer has been detached; Close and forced local disconnect suppress it.

## Constant summary

| Complete C# signature | Contract |
| --- | --- |
| `public const System.Int32 TargetPeerBroadcast = 0` | Selects all connected remote peers. |
| `public const System.Int32 TargetPeerServer = 1` | Identifies the server endpoint. |

## Constant Descriptions

<a id="member-8e614bd46d0e"></a>
### TargetPeerBroadcast

`public const System.Int32 TargetPeerBroadcast = 0`

Selects all connected remote peers.

<a id="member-166d193eb19a"></a>
### TargetPeerServer

`public const System.Int32 TargetPeerServer = 1`

Identifies the server endpoint.

## Lifecycle, errors and verification

Transport operations/disposal require the constructing thread. See the component for actual offline authority behavior and WS/WSS state/ID assignment, configured versus actual transfer mode/channel, next-packet metadata, routing and whole handshake deadline. Queued data remains bounded; buffer/queue configuration for the WS/WSS cohort requires Disconnected. Events see committed state; callback errors aggregate and reentry/Close/recreation follow the generation/lifetime rules. Owning Close releases resources; returned peer references remain borrowed. Snapshots/setup/lifecycle/diagnostic allocation is separate from prepared message processing.

[MultiplayerTests](../../tests/Electron2D.Tests/MultiplayerTests.cs) verifies native Linux WS/WSS two-client/public scene workflows, independent ID/application wire, custom managed hooks/offline role, packet/metadata/routing/pressure/admission/deadline/callback/ownership edges and 64 warmed custom event/packet plus native active/idle zero-managed-allocation intervals. Browser/foreign hosts, external native allocation, routed throughput and human acceptance remain separate. Higher scene multiplayer/replication and other transport protocols retain exact coverage dependencies.
