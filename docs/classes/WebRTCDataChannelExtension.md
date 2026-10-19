# WebRTCDataChannelExtension

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.WebRTCDataChannelExtension`. **Source:** [WebRTCDataChannelExtension.cs](../../src/Core/Networking/WebRTCDataChannelExtension.cs). **Component:** [WebRTC](../components/webrtc.md).

**Inherits:** [WebRTCDataChannel](WebRTCDataChannel.md).

## Description

Defines a typed channel implementation using the common packet span hooks.

Implement PacketPeer availability/capacity/send and exact span receive hooks together with channel metadata/lifecycle. CheckPacketPeer validates owner access. NextPacketSize observes without consuming; ReadPacketCore consumes exactly one packet into an already validated exact-sized span. Small-buffer retries and packet-read status are inherited. The concrete provider owns its backend and must close it during disposal.

## Example

Public C# excerpt. Channel/observation examples require the indicated application transport; MyPeer/applicationChannel stand for a complete application extension. The peer example generates and commits only its local offer; a remote peer and an application signaling path are required for connectivity. Equivalent complete native and scene workflows execute in WebRTCTests.

```csharp
WebRTCDataChannel channel = applicationChannel;
channel.PutPacket("game"u8);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected WebRTCDataChannelExtension()` | Creates an extension identity on the current owner thread. |

## Constructor Descriptions

<a id="member-f487574fa22c"></a>
### WebRTCDataChannelExtension()

`protected WebRTCDataChannelExtension()`

Creates an extension identity on the current owner thread.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.PacketWriteMode WriteMode { get; set; }` | Gets or sets UTF-8 text versus binary transmission. |

## Property Descriptions

<a id="member-c807514d37aa"></a>
### WriteMode

`public Electron2D.PacketWriteMode WriteMode { get; set; }`

Gets or sets UTF-8 text versus binary transmission.

Value: Binary initially.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public abstract System.Void Close()` | Closes this channel without disposing its managed identity. |
| `public abstract System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public abstract System.Int32 GetBufferedAmount()` | Returns the number of buffered unsent application bytes. |
| `public abstract System.Int32 GetID()` | Returns the stream identity. |
| `public abstract System.String GetLabel()` | Returns the immutable application label. |
| `public abstract System.Int32 GetMaxPacketLifeTime()` | Returns the partial-reliability lifetime. |
| `public abstract System.Int32 GetMaxPacketSize()` | Reports the maximum outgoing raw payload supported by this transport. |
| `public abstract System.Int32 GetMaxRetransmits()` | Returns the partial-reliability retransmission count. |
| `public abstract System.String GetProtocol()` | Returns the immutable application protocol. |
| `public abstract Electron2D.WebRTCDataChannel.ChannelState GetReadyState()` | Reports the current ready state. |
| `public abstract System.Boolean IsNegotiated()` | Reports whether stream identity was negotiated out of band. |
| `public abstract System.Boolean IsOrdered()` | Reports whether delivered messages preserve ordering. |
| `protected abstract System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public abstract System.Void Poll()` | Observes native channel errors without consuming queued packets. |
| `public abstract System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected abstract System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `public abstract System.Boolean WasStringPacket()` | Reports the type of the last successfully received packet. |

## Method Descriptions

<a id="member-ac4c5c7d3aac"></a>
### Close()

`public abstract System.Void Close()`

Closes this channel without disposing its managed identity.

<a id="member-b25d095c82c8"></a>
### GetAvailablePacketCount()

`public abstract System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-242c192cc8df"></a>
### GetBufferedAmount()

`public abstract System.Int32 GetBufferedAmount()`

Returns the number of buffered unsent application bytes.

Returns: A nonnegative byte count.

<a id="member-c61ea6faab28"></a>
### GetID()

`public abstract System.Int32 GetID()`

Returns the stream identity.

Returns: Zero through 65534, or 65535 before assignment.

<a id="member-97541aaa55ff"></a>
### GetLabel()

`public abstract System.String GetLabel()`

Returns the immutable application label.

Returns: A UTF-8 label.

<a id="member-9c9228e122e8"></a>
### GetMaxPacketLifeTime()

`public abstract System.Int32 GetMaxPacketLifeTime()`

Returns the partial-reliability lifetime.

Returns: Milliseconds, or 65535 when unset.

<a id="member-9808762057cd"></a>
### GetMaxPacketSize()

`public abstract System.Int32 GetMaxPacketSize()`

Reports the maximum outgoing raw payload supported by this transport.

Returns: A nonnegative byte count; operating-system limits may be narrower.

<a id="member-f6620ef471b9"></a>
### GetMaxRetransmits()

`public abstract System.Int32 GetMaxRetransmits()`

Returns the partial-reliability retransmission count.

Returns: A count, or 65535 when unset.

<a id="member-5d11d0a4de64"></a>
### GetProtocol()

`public abstract System.String GetProtocol()`

Returns the immutable application protocol.

Returns: A UTF-8 subprotocol label.

<a id="member-a20dded1c20e"></a>
### GetReadyState()

`public abstract Electron2D.WebRTCDataChannel.ChannelState GetReadyState()`

Reports the current ready state.

Returns: The current channel state.

<a id="member-18a780b6b9d7"></a>
### IsNegotiated()

`public abstract System.Boolean IsNegotiated()`

Reports whether stream identity was negotiated out of band.

Returns: True for explicit negotiation.

<a id="member-696847d91c64"></a>
### IsOrdered()

`public abstract System.Boolean IsOrdered()`

Reports whether delivered messages preserve ordering.

Returns: True for ordered delivery.

<a id="member-9aad6fb241c6"></a>
### NextPacketSize()

`protected abstract System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-4526df2f91e9"></a>
### Poll()

`public abstract System.Void Poll()`

Observes native channel errors without consuming queued packets.

<a id="member-749e40119873"></a>
### PutPacket(System.ReadOnlySpan<System.Byte>)

`public abstract System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

data: Packet bytes, borrowed only for this call.

<a id="member-c950d3db717b"></a>
### ReadPacketCore(System.Span<System.Byte>)

`protected abstract System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

destination: An exact-sized destination.

<a id="member-acda76853e57"></a>
### WasStringPacket()

`public abstract System.Boolean WasStringPacket()`

Reports the type of the last successfully received packet.

Returns: True for UTF-8 text, false for binary or before any read.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
