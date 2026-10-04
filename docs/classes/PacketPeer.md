# PacketPeer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.PacketPeer`.

**Inherits:** [ElectronObject](ElectronObject.md). **Inherited by:** [PacketPeerStream](PacketPeerStream.md), [PacketPeerUDP](PacketPeerUDP.md), [WebSocketPeer](WebSocketPeer.md). **Source:** [PacketPeer.cs](../../src/Core/Networking/PacketPeer.cs).

## Description

Transfers complete byte packets with caller-owned receive buffers.

Packet boundaries are preserved. Calls require the constructing thread. Dynamic value serialization is absent; callers choose their own typed packet encoding. Snapshot reads allocate; span reads reuse caller storage.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
PacketPeer packets = readyPeer;
Span<byte> receive = stackalloc byte[1024];
if (packets.GetAvailablePacketCount() > 0)
{
    int bytes = packets.GetPacket(receive);
}
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected PacketPeer()` | Creates a packet transport owned by the current thread. |

## Constructor Descriptions

<a id="member-e9a6bb40f10a"></a>
### .ctor

`protected PacketPeer()`

Creates a packet transport owned by the current thread.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Exception LastReadException { get;  }` | Gets the original exception from the last failed packet read. |

## Property Descriptions

<a id="member-2207b3d9540a"></a>
### LastReadException

`public System.Exception LastReadException { get;  }`

Gets the original exception from the last failed packet read.

Value: Null after a successful read or before any read.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected System.Void CheckPacketPeer()` | Checks transport lifetime and constructing-thread ownership. |
| `public abstract System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public abstract System.Int32 GetMaxPacketSize()` | Reports the maximum outgoing raw payload supported by this transport. |
| `public System.Byte[] GetPacket()` | Returns one complete packet in a caller-owned array. |
| `public System.Int32 GetPacket(System.Span<System.Byte> destination)` | Copies one complete packet without allocating a receive array. |
| `public Electron2D.PacketReadStatus GetPacketError()` | Returns the last packet-read status without polling. |
| `protected abstract System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public abstract System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected abstract System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-404a10aefb14"></a>
### CheckPacketPeer

`protected System.Void CheckPacketPeer()`

Checks transport lifetime and constructing-thread ownership.

<a id="member-7f2d965b8b32"></a>
### GetAvailablePacketCount

`public abstract System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-36c8cac0d6dd"></a>
### GetMaxPacketSize

`public abstract System.Int32 GetMaxPacketSize()`

Reports the maximum outgoing raw payload supported by this transport.

Returns: A nonnegative byte count; operating-system limits may be narrower.

<a id="member-7773e7f58ffe"></a>
### GetPacket

`public System.Byte[] GetPacket()`

Returns one complete packet in a caller-owned array.

Returns: The packet, including an empty datagram.

System.InvalidOperationException: No packet is available.

<a id="member-c5fa6c304c05"></a>
### GetPacket

`public System.Int32 GetPacket(System.Span<System.Byte> destination)`

Copies one complete packet without allocating a receive array.

destination: Caller-owned storage large enough for the packet.

Returns: The copied length; zero is a valid datagram.

System.ArgumentException: Storage is too small; the packet is not consumed.

<a id="member-4681bafdb307"></a>
### GetPacketError

`public Electron2D.PacketReadStatus GetPacketError()`

Returns the last packet-read status without polling.

Returns: The last read outcome.

<a id="member-0706e6a7e5be"></a>
### NextPacketSize

`protected abstract System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-e3e8d2766390"></a>
### PutPacket

`public abstract System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

data: Packet bytes, borrowed only for this call.

<a id="member-49a9d74ce9b2"></a>
### ReadPacketCore

`protected abstract System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

destination: An exact-sized destination.

<a id="member-66cfd9787b44"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/PacketPeer.md).
