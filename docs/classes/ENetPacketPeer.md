# ENetPacketPeer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.ENetPacketPeer`. **Source:** [ENetPacketPeer.cs](../../src/Core/Networking/ENetPacketPeer.cs).

## Description

Exposes an ENet host-owned peer, its complete packet queue and protocol statistics.

GetState/IsActive project detached state. Other protocol operations require an active host/peer on the constructing thread. Incoming packets are queued by ENetConnection.Service and read with the inherited caller-span/snapshot contract. Disposal forcefully disconnects this peer without disposing its host.

See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Example

The public excerpt compiles and executes in the documentation consumer. The executable tests supply complete connection/poll/scene flows.

```csharp
using var server = new ENetConnection();
using var client = new ENetConnection();
server.CreateHostBound("127.0.0.1", 0, 1, 2);
client.CreateHost(1, 2);
ENetPacketPeer peer = client.ConnectToHost("127.0.0.1", server.GetLocalPort(), 2);
while (peer.GetState() != ENetPeerState.Connected)
{
    server.Service();
    client.Service();
}
peer.Send(1, "hello"u8, ENetPacketFlags.Reliable);
client.Flush();
```

## Constant summary

| Complete C# signature | Contract |
| --- | --- |
| `public const System.UInt32 PacketLossScale = 65536` | Scales packet-loss statistics. |
| `public const System.UInt32 PacketThrottleScale = 32` | Scales throttle statistics. |

## Constant Descriptions

<a id="member-e4c502df32bd"></a>
### PacketLossScale

`public const System.UInt32 PacketLossScale = 65536`

Scales packet-loss statistics.

<a id="member-42f0ead3d5f1"></a>
### PacketThrottleScale

`public const System.UInt32 PacketThrottleScale = 32`

Scales throttle statistics.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `protected override System.Void Finalize()` | Releases unread native packets when deterministic disposal was omitted. |
| `public override System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public System.Int32 GetChannels()` | Returns the negotiated channel count. |
| `public override System.Int32 GetMaxPacketSize()` | Reports the maximum outgoing raw payload supported by this transport. |
| `public Electron2D.ENetPacketFlags GetPacketFlags()` | Returns the flags of the next queued packet without consuming it. |
| `public System.String GetRemoteAddress()` | Returns the peer's cached remote IP literal. |
| `public System.Int32 GetRemotePort()` | Returns the remote port. |
| `public Electron2D.ENetPeerState GetState()` | Returns the ENet protocol state without polling. |
| `public System.Double GetStatistic(Electron2D.ENetPeerStatistic statistic)` | Returns one current native peer statistic. |
| `public System.Boolean IsActive()` | Reports whether this logical peer remains attached to a host. |
| `protected override System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public System.Void PeerDisconnect(System.UInt32 data = 0)` | Starts a graceful disconnect. |
| `public System.Void PeerDisconnectLater(System.UInt32 data = 0)` | Starts graceful disconnect after queued outgoing data drains. |
| `public System.Void PeerDisconnectNow(System.UInt32 data = 0)` | Immediately disconnects locally and sends a best-effort notification. |
| `public System.Void Ping()` | Queues a protocol ping. |
| `public System.Void PingInterval(System.UInt32 interval)` | Sets the native ping interval. |
| `public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `public System.Void Reset()` | Resets locally without sending a disconnect packet. |
| `public System.Void Send(System.Int32 channel, System.ReadOnlySpan<System.Byte> packet, Electron2D.ENetPacketFlags flags)` | Queues one complete packet on a negotiated channel. |
| `public System.Void SetTimeout(System.UInt32 timeout, System.UInt32 timeoutMin, System.UInt32 timeoutMax)` | Sets timeout backoff and minimum/maximum bounds. |
| `public System.Void ThrottleConfigure(System.UInt32 interval, System.UInt32 acceleration, System.UInt32 deceleration)` | Configures native packet-throttle adaptation. |

## Method Descriptions

<a id="member-e8e67f116827"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

- `System.AggregateException`: Both notification delivery and derived cleanup fail.
- `System.Exception`: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-8807b3133d3f"></a>
### Finalize

`protected override System.Void Finalize()`

Releases unread native packets when deterministic disposal was omitted.

<a id="member-cb536688ca39"></a>
### GetAvailablePacketCount

`public override System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-8fd5c2c868c2"></a>
### GetChannels

`public System.Int32 GetChannels()`

Returns the negotiated channel count.

Returns: One through 255 for an active peer.

<a id="member-ba9aa01f78e9"></a>
### GetMaxPacketSize

`public override System.Int32 GetMaxPacketSize()`

Reports the maximum outgoing raw payload supported by this transport.

Returns: A nonnegative byte count; operating-system limits may be narrower.

<a id="member-257ca7bbf6b1"></a>
### GetPacketFlags

`public Electron2D.ENetPacketFlags GetPacketFlags()`

Returns the flags of the next queued packet without consuming it.

Returns: None when the receive queue is empty; receiver flags need not include every sender flag.

<a id="member-f4b954f575cb"></a>
### GetRemoteAddress

`public System.String GetRemoteAddress()`

Returns the peer's cached remote IP literal.

Returns: The resolved IPv4/IPv6 address.

<a id="member-c163a041aa41"></a>
### GetRemotePort

`public System.Int32 GetRemotePort()`

Returns the remote port.

Returns: The active peer's port.

<a id="member-7d6e9b6b9d5f"></a>
### GetState

`public Electron2D.ENetPeerState GetState()`

Returns the ENet protocol state without polling.

Returns: Disconnected after detachment.

<a id="member-ebc4adf31e6e"></a>
### GetStatistic

`public System.Double GetStatistic(Electron2D.ENetPeerStatistic statistic)`

Returns one current native peer statistic.

Returns: The unsigned counter projected as an exact double.

- `statistic`: The peer-statistic domain.

<a id="member-e4889f9b7645"></a>
### IsActive

`public System.Boolean IsActive()`

Reports whether this logical peer remains attached to a host.

Returns: True includes connecting/disconnecting states.

<a id="member-23466190f8f8"></a>
### NextPacketSize

`protected override System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-8536d742794d"></a>
### PeerDisconnect

`public System.Void PeerDisconnect(System.UInt32 data = 0)`

Starts a graceful disconnect.

- `data`: Peer-visible disconnection data.

<a id="member-7776b64def60"></a>
### PeerDisconnectLater

`public System.Void PeerDisconnectLater(System.UInt32 data = 0)`

Starts graceful disconnect after queued outgoing data drains.

- `data`: Peer-visible disconnection data.

<a id="member-69774c915be3"></a>
### PeerDisconnectNow

`public System.Void PeerDisconnectNow(System.UInt32 data = 0)`

Immediately disconnects locally and sends a best-effort notification.

No local disconnected event is generated.

- `data`: Peer-visible data.

<a id="member-b154f8f865d4"></a>
### Ping

`public System.Void Ping()`

Queues a protocol ping.

<a id="member-0ab5d78082bc"></a>
### PingInterval

`public System.Void PingInterval(System.UInt32 interval)`

Sets the native ping interval.

- `interval`: Milliseconds; zero restores the native default.

<a id="member-77b23dd2ae79"></a>
### PutPacket

`public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

- `data`: Packet bytes, borrowed only for this call.

<a id="member-180a2e60c5ac"></a>
### ReadPacketCore

`protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

- `destination`: An exact-sized destination.

<a id="member-dd183c0780cc"></a>
### Reset

`public System.Void Reset()`

Resets locally without sending a disconnect packet.

<a id="member-25c4e75edfc2"></a>
### Send

`public System.Void Send(System.Int32 channel, System.ReadOnlySpan<System.Byte> packet, Electron2D.ENetPacketFlags flags)`

Queues one complete packet on a negotiated channel.

- `channel`: Existing zero-based native channel.
- `packet`: Borrowed bytes copied into native queued storage.
- `flags`: Reliability/ordering/fragmentation flags.

- `System.ArgumentException`: Channel, flags or payload capacity is invalid.

<a id="member-61e40276cd97"></a>
### SetTimeout

`public System.Void SetTimeout(System.UInt32 timeout, System.UInt32 timeoutMin, System.UInt32 timeoutMax)`

Sets timeout backoff and minimum/maximum bounds.

The multiplier is independent of the millisecond bounds.

- `timeout`: Backoff multiplier; zero selects the native default.
- `timeoutMin`: Minimum milliseconds, zero selects 5000.
- `timeoutMax`: Maximum milliseconds, zero selects 30000.

<a id="member-e2530a8850dd"></a>
### ThrottleConfigure

`public System.Void ThrottleConfigure(System.UInt32 interval, System.UInt32 acceleration, System.UInt32 deceleration)`

Configures native packet-throttle adaptation.

- `interval`: Milliseconds; zero selects native default.
- `acceleration`: Acceleration scale.
- `deceleration`: Deceleration scale.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
