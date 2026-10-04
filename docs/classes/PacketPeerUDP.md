# PacketPeerUDP

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.PacketPeerUDP`.

**Inherits:** [PacketPeer](PacketPeer.md). **Inherited by:** —. **Source:** [PacketPeerUDP.cs](../../src/Core/Networking/PacketPeerUDP.cs).

## Description

Transfers complete UDP datagrams with bounded queued receive storage.

Standalone peers poll while reading/counting packets. Server-created peers share the listener and receive only their endpoint's packets. Closing such a peer detaches it without closing the listener. Bind/connection/destination resolution and first endpoint queries are cold operations; caller-span packet cycles reuse storage.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
using var receiver = new PacketPeerUDP();
receiver.Bind(0, "127.0.0.1");
using var sender = new PacketPeerUDP();
sender.SetDestAddress("127.0.0.1", receiver.GetLocalPort());
sender.PutPacket(new byte[] { 1, 2, 3 });
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public PacketPeerUDP()` | Creates an unopened UDP peer with a 65536-byte receive queue. |

## Constructor Descriptions

<a id="member-812bb20eb805"></a>
### .ctor

`public PacketPeerUDP()`

Creates an unopened UDP peer with a 65536-byte receive queue.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void Bind(System.Int32 port, System.String bindAddress = "*", System.Int32 receiveBufferSize = 65536)` | Binds a native local UDP socket and configures its receive queue. |
| `public System.Void Close()` | Closes or detaches UDP state, clears packets and restores default queue capacity. |
| `public System.Void ConnectToHost(System.String host, System.Int32 port)` | Connects UDP to one remote IP endpoint, filtering other senders. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public override System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public System.Int32 GetLocalPort()` | Returns the current local port or zero while closed. |
| `public override System.Int32 GetMaxPacketSize()` | Reports the maximum outgoing raw payload supported by this transport. |
| `public System.String GetPacketIP()` | Returns the sender IP of the last consumed packet. |
| `public System.Int32 GetPacketPort()` | Returns the sender port of the last consumed packet. |
| `public System.Boolean IsBound()` | Reports whether a local UDP socket is open. |
| `public System.Boolean IsSocketConnected()` | Reports whether UDP filters to one connected endpoint. |
| `public System.Void JoinMulticastGroup(System.String multicastAddress, System.String interfaceName)` | Joins a multicast group on a named native interface. |
| `public System.Void LeaveMulticastGroup(System.String multicastAddress, System.String interfaceName)` | Leaves a previously joined multicast group. |
| `protected override System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `public System.Void SetBroadcastEnabled(System.Boolean enabled)` | Enables or disables native UDP broadcasting. |
| `public System.Void SetDestAddress(System.String host, System.Int32 port)` | Chooses a UDP send destination without connecting the socket. |
| `public System.Void Wait()` | Polls, then waits for one native readable notification if this peer has no queued packet. |

## Method Descriptions

<a id="member-d1f7e599e67a"></a>
### Bind

`public System.Void Bind(System.Int32 port, System.String bindAddress = "*", System.Int32 receiveBufferSize = 65536)`

Binds a native local UDP socket and configures its receive queue.

port: Zero through 65535; zero requests an ephemeral port.

bindAddress: IP literal or wildcard.

receiveBufferSize: Zero through 64 MiB, rounded up to a power of two; each queued packet charges 24 metadata bytes.

<a id="member-452fd78e25fb"></a>
### Close

`public System.Void Close()`

Closes or detaches UDP state, clears packets and restores default queue capacity.

<a id="member-19390a25f30a"></a>
### ConnectToHost

`public System.Void ConnectToHost(System.String host, System.Int32 port)`

Connects UDP to one remote IP endpoint, filtering other senders.

host: Remote IP literal.

port: Remote port, zero through 65535.

<a id="member-93acd030b9ee"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-26df92dafaec"></a>
### GetAvailablePacketCount

`public override System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-af9bf0ac758f"></a>
### GetLocalPort

`public System.Int32 GetLocalPort()`

Returns the current local port or zero while closed.

Returns: The port.

<a id="member-df6ad198332b"></a>
### GetMaxPacketSize

`public override System.Int32 GetMaxPacketSize()`

Reports the maximum outgoing raw payload supported by this transport.

Returns: A nonnegative byte count; operating-system limits may be narrower.

<a id="member-9bcb4a540368"></a>
### GetPacketIP

`public System.String GetPacketIP()`

Returns the sender IP of the last consumed packet.

Returns: A normalized IP literal; empty before receipt.

<a id="member-f15a6f0715f6"></a>
### GetPacketPort

`public System.Int32 GetPacketPort()`

Returns the sender port of the last consumed packet.

Returns: The port, zero initially.

<a id="member-1a0ee7584e40"></a>
### IsBound

`public System.Boolean IsBound()`

Reports whether a local UDP socket is open.

Returns: True after binding or implicit send/connection opening.

<a id="member-8ea1a31b7a9f"></a>
### IsSocketConnected

`public System.Boolean IsSocketConnected()`

Reports whether UDP filters to one connected endpoint.

Returns: True for a native connection or server-created peer.

<a id="member-6ff0e41cc022"></a>
### JoinMulticastGroup

`public System.Void JoinMulticastGroup(System.String multicastAddress, System.String interfaceName)`

Joins a multicast group on a named native interface.

multicastAddress: Multicast IP literal.

interfaceName: Native interface name or identifier; empty chooses the default interface.

<a id="member-af7db84c7dd0"></a>
### LeaveMulticastGroup

`public System.Void LeaveMulticastGroup(System.String multicastAddress, System.String interfaceName)`

Leaves a previously joined multicast group.

multicastAddress: Multicast IP literal.

interfaceName: Native interface name or identifier; empty chooses the default interface.

<a id="member-99bb0f832d9c"></a>
### NextPacketSize

`protected override System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-ac00b109a609"></a>
### PutPacket

`public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

data: Packet bytes, borrowed only for this call.

<a id="member-a5ac05ac4c56"></a>
### ReadPacketCore

`protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

destination: An exact-sized destination.

<a id="member-dd35f382568b"></a>
### SetBroadcastEnabled

`public System.Void SetBroadcastEnabled(System.Boolean enabled)`

Enables or disables native UDP broadcasting.

enabled: Whether broadcast sends are allowed.

<a id="member-8c0e7a641bce"></a>
### SetDestAddress

`public System.Void SetDestAddress(System.String host, System.Int32 port)`

Chooses a UDP send destination without connecting the socket.

host: IP literal or resolvable name.

port: Remote UDP port, zero through 65535.

<a id="member-c11b6edcab16"></a>
### Wait

`public System.Void Wait()`

Polls, then waits for one native readable notification if this peer has no queued packet.

System.InvalidOperationException: The UDP socket is closed.

Queue overflow or traffic for another shared-server endpoint can leave this peer empty after Wait returns.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/PacketPeerUDP.md).
