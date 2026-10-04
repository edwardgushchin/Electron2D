# WebSocketMultiplayerPeer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.WebSocketMultiplayerPeer`. **Inherits:** [MultiplayerPeer](MultiplayerPeer.md). **Source:** [WebSocketMultiplayerPeer.cs](../../src/Core/Networking/WebSocketMultiplayerPeer.cs).

## Description

Runs multiplayer server/client endpoints over reliable single-channel WebSocket messages.

Poll completes the HTTP/TLS upgrade and four-byte little-endian client identity assignment before connection events. Server targeting supports broadcast, positive IDs and negative exclusions; clients send to the server. Transfer mode/channel settings are retained but actual packets are Reliable/channel zero. The higher multiplayer layer, rather than this transport, owns peer-to-peer relay and scene replication.

[Multiplayer transports](../components/multiplayer.md) and [ADR 0094](../decisions/networking.md#adr-0094) define lifecycle/routing, prepared capacities, admission, metadata, event failure/reentry and native ownership. PacketPeer snapshot/span reads and last-read failure state remain inherited. Properties expose typed descriptors. Higher SceneTree/Node multiplayer, RPC and replication are separate consumers.

## Example

Public API excerpt; MultiplayerPeer requires a caller-provided configured concrete transport. The WebSocket example requires port 8080 to be available and continued owner-thread polling. MultiplayerTests exercises full native and public scene workflows; compilation alone does not establish a remote server or rendered output.

```csharp
using var server = new WebSocketMultiplayerPeer();
using var client = new WebSocketMultiplayerPeer();
server.CreateServer(8080, "127.0.0.1");
client.CreateClient("ws://127.0.0.1:8080/game");
while (client.GetConnectionStatus() == MultiplayerConnectionStatus.Connecting)
{
    server.Poll();
    client.Poll();
    Thread.Sleep(1);
}
if (client.GetConnectionStatus() == MultiplayerConnectionStatus.Connected)
    client.PutPacket("hello"u8);
// Continue polling and consume server packets; full workflows are in MultiplayerTests.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public WebSocketMultiplayerPeer()` | Creates a disconnected multiplayer endpoint with default WebSocket settings. |

## Constructor Descriptions

<a id="member-284b9600c52e"></a>
### .ctor

`public WebSocketMultiplayerPeer()`

Creates a disconnected multiplayer endpoint with default WebSocket settings.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.String[] HandshakeHeaders { get; set; }` | Gets or sets copied extra upgrade headers for future connections. |
| `public System.Double HandshakeTimeout { get; set; }` | Gets or sets the whole connection/identity handshake deadline in monotonic seconds. |
| `public System.Int32 InboundBufferSize { get; set; }` | Gets or sets per-connection input and aggregate queued-payload capacity. |
| `public System.Int32 MaxQueuedPackets { get; set; }` | Gets or sets per-connection and aggregate queued-message capacity. |
| `public System.Int32 OutboundBufferSize { get; set; }` | Gets or sets the per-connection outgoing payload budget. |
| `public System.String[] SupportedProtocols { get; set; }` | Gets or sets copied subprotocol tokens used for future connections. |

## Property Descriptions

<a id="member-c1475093ab02"></a>
### HandshakeHeaders

`public System.String[] HandshakeHeaders { get; set; }`

Gets or sets copied extra upgrade headers for future connections.

Value: Empty initially; validation follows WebSocketPeer.

<a id="member-1dc37d0be9f7"></a>
### HandshakeTimeout

`public System.Double HandshakeTimeout { get; set; }`

Gets or sets the whole connection/identity handshake deadline in monotonic seconds.

Value: Three initially; finite positive values. Live changes apply to all pending connections.

System.ArgumentOutOfRangeException: The timeout is nonpositive, nonfinite or exceeds the clock range.

<a id="member-cf4835cf83cf"></a>
### InboundBufferSize

`public System.Int32 InboundBufferSize { get; set; }`

Gets or sets per-connection input and aggregate queued-payload capacity.

Value: 65535 initially; at most 64 MiB while disconnected. Connection setup requires at least four bytes for identity.

<a id="member-06a787fc8dfe"></a>
### MaxQueuedPackets

`public System.Int32 MaxQueuedPackets { get; set; }`

Gets or sets per-connection and aggregate queued-message capacity.

Value: 4096 initially; up to 65536 while disconnected. Setup requires at least one identity-message slot.

<a id="member-0e0250c7393d"></a>
### OutboundBufferSize

`public System.Int32 OutboundBufferSize { get; set; }`

Gets or sets the per-connection outgoing payload budget.

Value: 65535 initially; zero selects 64 MiB. Configurable while disconnected; setup needs four identity bytes.

<a id="member-9ad5cb6c7eb0"></a>
### SupportedProtocols

`public System.String[] SupportedProtocols { get; set; }`

Gets or sets copied subprotocol tokens used for future connections.

Value: Empty initially; connected transports retain their prepared configuration.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public override System.Void Close()` | Immediately releases all owned transport state without disconnection events. |
| `public System.Void CreateClient(System.String url, Electron2D.TLSOptions tlsClientOptions = null)` | Starts a WS/WSS multiplayer client. |
| `public System.Void CreateServer(System.Int32 port, System.String bindAddress = "*", Electron2D.TLSOptions tlsServerOptions = null)` | Starts an owned native TCP/TLS multiplayer listener. |
| `public override System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)` | Disconnects one remote peer. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public override System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public override Electron2D.MultiplayerConnectionStatus GetConnectionStatus()` | Gets the cached local connection status. |
| `public override System.Int32 GetMaxPacketSize()` | Gets the outgoing application limit, retaining nine bytes of multiplayer protocol reserve. |
| `public override System.Int32 GetPacketChannel()` | Gets the next packet's transport channel. |
| `public override Electron2D.TransferMode GetPacketMode()` | Gets the next packet's actual delivery mode. |
| `public override System.Int32 GetPacketPeer()` | Gets the source identity of the next queued packet. |
| `public Electron2D.WebSocketPeer GetPeer(System.Int32 peerID)` | Gets an associated transport without transferring ownership. |
| `public System.String GetPeerAddress(System.Int32 id)` | Gets a connected peer's native remote address. |
| `public System.Int32 GetPeerPort(System.Int32 id)` | Gets a connected peer's native remote port. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public override System.Int32 GetUniqueID()` | Gets the local multiplayer identity. |
| `public override System.Boolean IsServer()` | Reports whether this endpoint acts as server. |
| `public override System.Boolean IsServerRelaySupported()` | Reports whether a higher multiplayer layer can implement server relay through this transport. |
| `protected override System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public override System.Void Poll()` | Advances admission, identity assignment, message queues and transport/close lifecycle without waiting. |
| `public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends a copied binary message to the configured target. |
| `protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `public override System.Void SetTargetPeer(System.Int32 id)` | Chooses broadcast, a positive peer, or all peers except a negative ID. |

## Method Descriptions

<a id="member-7a5f31f06aec"></a>
### Close

`public override System.Void Close()`

Immediately releases all owned transport state without disconnection events.

<a id="member-29336692d5ec"></a>
### CreateClient

`public System.Void CreateClient(System.String url, Electron2D.TLSOptions tlsClientOptions = null)`

Starts a WS/WSS multiplayer client.

url: Server URL; credentials are forbidden.

tlsClientOptions: Borrowed client TLS options, or null for normal trust/name validation.

System.InvalidOperationException: Already active or prepared capacities cannot carry identity assignment.

<a id="member-7e44537152f7"></a>
### CreateServer

`public System.Void CreateServer(System.Int32 port, System.String bindAddress = "*", Electron2D.TLSOptions tlsServerOptions = null)`

Starts an owned native TCP/TLS multiplayer listener.

port: Local port; zero requests an ephemeral port, otherwise the caller chooses the addressable port.

bindAddress: IP literal or wildcard.

tlsServerOptions: Borrowed server identity options, or null for WS.

System.ArgumentException: The supplied TLS options describe a client.

<a id="member-fb140d86cfa1"></a>
### DisconnectPeer

`public override System.Void DisconnectPeer(System.Int32 peer, System.Boolean force = false)`

Disconnects one remote peer.

peer: Remote identity.

force: True suppresses the local disconnection event and tears down immediately.

<a id="member-2e924b32f5c4"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-cb0bdcb86662"></a>
### GetAvailablePacketCount

`public override System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-9497688ed382"></a>
### GetConnectionStatus

`public override Electron2D.MultiplayerConnectionStatus GetConnectionStatus()`

Gets the cached local connection status.

Returns: The latest polled status.

<a id="member-5ca368d64d6b"></a>
### GetMaxPacketSize

`public override System.Int32 GetMaxPacketSize()`

Gets the outgoing application limit, retaining nine bytes of multiplayer protocol reserve.

Returns: Nonnegative effective outgoing budget minus nine; zero output configuration uses the prepared 64 MiB ceiling.

<a id="member-0f81d26d1d23"></a>
### GetPacketChannel

`public override System.Int32 GetPacketChannel()`

Gets the next packet's transport channel.

Returns: The receiving channel; zero for single-channel transports.

<a id="member-978ca9da1622"></a>
### GetPacketMode

`public override Electron2D.TransferMode GetPacketMode()`

Gets the next packet's actual delivery mode.

Returns: The wire mode, independent of requested outgoing configuration.

<a id="member-6fddc1863268"></a>
### GetPacketPeer

`public override System.Int32 GetPacketPeer()`

Gets the source identity of the next queued packet.

Returns: A peer ID; empty-queue behavior is transport-specific.

<a id="member-7c9140e4c846"></a>
### GetPeer

`public Electron2D.WebSocketPeer GetPeer(System.Int32 peerID)`

Gets an associated transport without transferring ownership.

peerID: Remote identity; client ID one is available while connecting.

Returns: A borrowed peer, or null when absent. Direct reading/disposal can disrupt the owning multiplayer protocol.

<a id="member-0914f69d9f1b"></a>
### GetPeerAddress

`public System.String GetPeerAddress(System.Int32 id)`

Gets a connected peer's native remote address.

id: Remote peer identity.

Returns: IP literal or empty when absent.

<a id="member-4420211a500b"></a>
### GetPeerPort

`public System.Int32 GetPeerPort(System.Int32 id)`

Gets a connected peer's native remote port.

id: Remote peer identity.

Returns: Port or zero when absent.

<a id="member-25b5dec23a05"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-101147da2a39"></a>
### GetUniqueID

`public override System.Int32 GetUniqueID()`

Gets the local multiplayer identity.

Returns: One for servers, greater than one for assigned clients, or zero while disconnected/connecting.

<a id="member-fc78e159f350"></a>
### IsServer

`public override System.Boolean IsServer()`

Reports whether this endpoint acts as server.

Returns: The current server role.

<a id="member-0061d5b09c0e"></a>
### IsServerRelaySupported

`public override System.Boolean IsServerRelaySupported()`

Reports whether a higher multiplayer layer can implement server relay through this transport.

Returns: False by default; this does not automatically relay application bytes.

<a id="member-693cc3a091e1"></a>
### NextPacketSize

`protected override System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-0620f97f368a"></a>
### Poll

`public override System.Void Poll()`

Advances admission, identity assignment, message queues and transport/close lifecycle without waiting.

Remarks: Transport failure drops only that connection. Events see committed state, all subscribers run, and callback failures aggregate after remaining peers. Recursive Poll rejects; callback Close/recreation invalidates the old iteration. Deadline includes an open socket still waiting for its identity message.

System.AggregateException: Connection-event subscribers failed.

System.InvalidOperationException: Poll reentered itself.

<a id="member-08dba42bfa90"></a>
### PutPacket

`public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends a copied binary message to the configured target.

data: Borrowed application bytes; empty messages are ignored.

System.InvalidOperationException: Disconnected, target queue is full or a target is closing.

System.ArgumentException: Payload exceeds the advertised limit or the positive server target is absent.

System.AggregateException: Broadcast recipients failed after all selected recipients were attempted; successful recipients retain their packets.

<a id="member-029d075a293e"></a>
### ReadPacketCore

`protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

destination: An exact-sized destination.

<a id="member-f1bdd06a27e3"></a>
### SetTargetPeer

`public override System.Void SetTargetPeer(System.Int32 id)`

Chooses broadcast, a positive peer, or all peers except a negative ID.

id: Zero broadcasts; one is server; int.MinValue cannot denote an exclusion.

## Lifecycle, errors and verification

Transport operations/disposal require the constructing thread. See the component for actual offline authority behavior and WS/WSS state/ID assignment, configured versus actual transfer mode/channel, next-packet metadata, routing and whole handshake deadline. Queued data remains bounded; buffer/queue configuration for the WS/WSS cohort requires Disconnected. Events see committed state; callback errors aggregate and reentry/Close/recreation follow the generation/lifetime rules. Owning Close releases resources; returned peer references remain borrowed. Snapshots/setup/lifecycle/diagnostic allocation is separate from prepared message processing.

[MultiplayerTests](../../tests/Electron2D.Tests/MultiplayerTests.cs) verifies native Linux WS/WSS two-client/public scene workflows, independent ID/application wire, custom managed hooks/offline role, packet/metadata/routing/pressure/admission/deadline/callback/ownership edges and 64 warmed custom event/packet plus native active/idle zero-managed-allocation intervals. Browser/foreign hosts, external native allocation, routed throughput and human acceptance remain separate. Higher scene multiplayer/replication and other transport protocols retain exact coverage dependencies.
