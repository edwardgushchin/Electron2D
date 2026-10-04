# OfflineMultiplayerPeer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.OfflineMultiplayerPeer`. **Inherits:** [MultiplayerPeer](MultiplayerPeer.md). **Source:** [MultiplayerPeer.cs](../../src/Core/Networking/MultiplayerPeer.cs).

## Description

Models a permanently connected local server without remote endpoints.

The local ID is one. Writes discard bytes, reads return an empty packet, available count and maximum size are zero. Poll, Close, routing and disconnection validate ownership but preserve the offline server identity. This is the local authority transport; automatic SceneTree multiplayer assignment remains a separate integration.

[Multiplayer transports](../components/multiplayer.md) and [ADR 0094](../decisions/networking.md#adr-0094) define lifecycle/routing, prepared capacities, admission, metadata, event failure/reentry and native ownership. PacketPeer snapshot/span reads and last-read failure state remain inherited. Properties expose typed descriptors. Higher SceneTree/Node multiplayer, RPC and replication are separate consumers.

## Example

Public API excerpt; MultiplayerPeer requires a caller-provided configured concrete transport. The WebSocket example requires port 8080 to be available and continued owner-thread polling. MultiplayerTests exercises full native and public scene workflows; compilation alone does not establish a remote server or rendered output.

```csharp
using var peer = new OfflineMultiplayerPeer();
int localID = peer.GetUniqueID(); // 1
peer.Close(); // Local authority remains Connected.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public OfflineMultiplayerPeer()` | Creates an offline local authority endpoint. |

## Constructor Descriptions

<a id="member-2eba179073cc"></a>
### .ctor

`public OfflineMultiplayerPeer()`

Creates an offline local authority endpoint.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public override System.Void Close()` | Immediately releases all owned transport state without disconnection events. |
| `public override System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)` | Disconnects one remote peer. |
| `public override System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public override Electron2D.MultiplayerConnectionStatus GetConnectionStatus()` | Gets the cached local connection status. |
| `public override System.Int32 GetMaxPacketSize()` | Reports the maximum outgoing raw payload supported by this transport. |
| `public override System.Int32 GetPacketChannel()` | Gets the next packet's transport channel. |
| `public override Electron2D.TransferMode GetPacketMode()` | Gets the next packet's actual delivery mode. |
| `public override System.Int32 GetPacketPeer()` | Gets the source identity of the next queued packet. |
| `public override System.Int32 GetUniqueID()` | Gets the local multiplayer identity. |
| `public override System.Boolean IsServer()` | Reports whether this endpoint acts as server. |
| `protected override System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public override System.Void Poll()` | Advances connection and packet progress using the concrete transport's polling policy. |
| `public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `public override System.Void SetTargetPeer(System.Int32 id)` | Chooses broadcast, a positive peer, or all peers except a negative ID. |

## Method Descriptions

<a id="member-f8241771333a"></a>
### Close

`public override System.Void Close()`

Immediately releases all owned transport state without disconnection events.

<a id="member-a9d465f076e3"></a>
### DisconnectPeer

`public override System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)`

Disconnects one remote peer.

peer: Remote identity.

force: True suppresses the local disconnection event and tears down immediately.

<a id="member-242b7234c702"></a>
### GetAvailablePacketCount

`public override System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-74aa1512b69c"></a>
### GetConnectionStatus

`public override Electron2D.MultiplayerConnectionStatus GetConnectionStatus()`

Gets the cached local connection status.

Returns: The latest polled status.

<a id="member-9464e7353a1b"></a>
### GetMaxPacketSize

`public override System.Int32 GetMaxPacketSize()`

Reports the maximum outgoing raw payload supported by this transport.

Returns: A nonnegative byte count; operating-system limits may be narrower.

<a id="member-f979686dbb6e"></a>
### GetPacketChannel

`public override System.Int32 GetPacketChannel()`

Gets the next packet's transport channel.

Returns: The receiving channel; zero for single-channel transports.

<a id="member-5b5b9046643b"></a>
### GetPacketMode

`public override Electron2D.TransferMode GetPacketMode()`

Gets the next packet's actual delivery mode.

Returns: The wire mode, independent of requested outgoing configuration.

<a id="member-798cd9c07985"></a>
### GetPacketPeer

`public override System.Int32 GetPacketPeer()`

Gets the source identity of the next queued packet.

Returns: A peer ID; empty-queue behavior is transport-specific.

<a id="member-9ad65037f5a9"></a>
### GetUniqueID

`public override System.Int32 GetUniqueID()`

Gets the local multiplayer identity.

Returns: One for servers, greater than one for assigned clients, or zero while disconnected/connecting.

<a id="member-07acc7a1e083"></a>
### IsServer

`public override System.Boolean IsServer()`

Reports whether this endpoint acts as server.

Returns: The current server role.

<a id="member-f44f6275b145"></a>
### NextPacketSize

`protected override System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-785392583727"></a>
### Poll

`public override System.Void Poll()`

Advances connection and packet progress using the concrete transport's polling policy.

<a id="member-d2d951fd2823"></a>
### PutPacket

`public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

data: Packet bytes, borrowed only for this call.

<a id="member-9bf1f527edc8"></a>
### ReadPacketCore

`protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

destination: An exact-sized destination.

<a id="member-08b96aa7be44"></a>
### SetTargetPeer

`public override System.Void SetTargetPeer(System.Int32 id)`

Chooses broadcast, a positive peer, or all peers except a negative ID.

id: Zero broadcasts; one is server; int.MinValue cannot denote an exclusion.

## Lifecycle, errors and verification

Transport operations/disposal require the constructing thread. See the component for actual offline authority behavior and WS/WSS state/ID assignment, configured versus actual transfer mode/channel, next-packet metadata, routing and whole handshake deadline. Queued data remains bounded; buffer/queue configuration for the WS/WSS cohort requires Disconnected. Events see committed state; callback errors aggregate and reentry/Close/recreation follow the generation/lifetime rules. Owning Close releases resources; returned peer references remain borrowed. Snapshots/setup/lifecycle/diagnostic allocation is separate from prepared message processing.

[MultiplayerTests](../../tests/Electron2D.Tests/MultiplayerTests.cs) verifies native Linux WS/WSS two-client/public scene workflows, independent ID/application wire, custom managed hooks/offline role, packet/metadata/routing/pressure/admission/deadline/callback/ownership edges and 64 warmed custom event/packet plus native active/idle zero-managed-allocation intervals. Browser/foreign hosts, external native allocation, routed throughput and human acceptance remain separate. Higher scene multiplayer/replication and other transport protocols retain exact coverage dependencies.
