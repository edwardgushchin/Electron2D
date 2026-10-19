# WebRTCDataChannel

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.WebRTCDataChannel`. **Source:** [WebRTCDataChannel.cs](../../src/Core/Networking/WebRTCDataChannel.cs). **Component:** [WebRTC](../components/webrtc.md).

**Inherits:** [PacketPeer](PacketPeer.md).

**Inherited By:** [WebRTCDataChannelExtension](WebRTCDataChannelExtension.md).

## Description

Exchanges complete WebRTC messages through the common packet span contract.

PacketPeer owns exact message reads and caller-span retry. Outgoing text payloads are strict UTF-8 and preserve embedded NUL; binary payloads and empty packets preserve their boundaries. Close retires transport while Dispose releases the wrapper. Owner access, negotiated IDs, reliability sentinels and native receive budgets follow the component contract. This abstract base is created by a peer or implemented by an extension.

## Example

Public C# excerpt. Channel/observation examples require the indicated application transport; MyPeer/applicationChannel stand for a complete application extension. The peer example generates and commits only its local offer; a remote peer and an application signaling path are required for connectivity. Equivalent complete native and scene workflows execute in WebRTCTests.

```csharp
Span<byte> storage = stackalloc byte[1200];
if (channel.GetAvailablePacketCount() > 0)
    channel.GetPacket(storage);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected WebRTCDataChannel()` | Creates an owner-thread channel identity. |

## Constructor Descriptions

<a id="member-53c7010afa12"></a>
### WebRTCDataChannel()

`protected WebRTCDataChannel()`

Creates an owner-thread channel identity.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.PacketWriteMode WriteMode { get; set; }` | Gets or sets UTF-8 text versus binary transmission. |

## Property Descriptions

<a id="member-f7a20ddcbb0b"></a>
### WriteMode

`public Electron2D.PacketWriteMode WriteMode { get; set; }`

Gets or sets UTF-8 text versus binary transmission.

Value: Binary initially.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public abstract System.Void Close()` | Closes this channel without disposing its managed identity. |
| `public abstract System.Int32 GetBufferedAmount()` | Returns the number of buffered unsent application bytes. |
| `public abstract System.Int32 GetID()` | Returns the stream identity. |
| `public abstract System.String GetLabel()` | Returns the immutable application label. |
| `public abstract System.Int32 GetMaxPacketLifeTime()` | Returns the partial-reliability lifetime. |
| `public abstract System.Int32 GetMaxRetransmits()` | Returns the partial-reliability retransmission count. |
| `public abstract System.String GetProtocol()` | Returns the immutable application protocol. |
| `public abstract Electron2D.WebRTCDataChannel.ChannelState GetReadyState()` | Reports the current ready state. |
| `public abstract System.Boolean IsNegotiated()` | Reports whether stream identity was negotiated out of band. |
| `public abstract System.Boolean IsOrdered()` | Reports whether delivered messages preserve ordering. |
| `public abstract System.Void Poll()` | Observes native channel errors without consuming queued packets. |
| `public abstract System.Boolean WasStringPacket()` | Reports the type of the last successfully received packet. |

## Method Descriptions

<a id="member-356ddd1b40fb"></a>
### Close()

`public abstract System.Void Close()`

Closes this channel without disposing its managed identity.

<a id="member-7b6e271779da"></a>
### GetBufferedAmount()

`public abstract System.Int32 GetBufferedAmount()`

Returns the number of buffered unsent application bytes.

Returns: A nonnegative byte count.

<a id="member-e5d40b83df14"></a>
### GetID()

`public abstract System.Int32 GetID()`

Returns the stream identity.

Returns: Zero through 65534, or 65535 before assignment.

<a id="member-746403dc4c6a"></a>
### GetLabel()

`public abstract System.String GetLabel()`

Returns the immutable application label.

Returns: A UTF-8 label.

<a id="member-961f6650a35b"></a>
### GetMaxPacketLifeTime()

`public abstract System.Int32 GetMaxPacketLifeTime()`

Returns the partial-reliability lifetime.

Returns: Milliseconds, or 65535 when unset.

<a id="member-9f513ddac47a"></a>
### GetMaxRetransmits()

`public abstract System.Int32 GetMaxRetransmits()`

Returns the partial-reliability retransmission count.

Returns: A count, or 65535 when unset.

<a id="member-cf83c4041255"></a>
### GetProtocol()

`public abstract System.String GetProtocol()`

Returns the immutable application protocol.

Returns: A UTF-8 subprotocol label.

<a id="member-80bd79f2c0d1"></a>
### GetReadyState()

`public abstract Electron2D.WebRTCDataChannel.ChannelState GetReadyState()`

Reports the current ready state.

Returns: The current channel state.

<a id="member-e9693301c602"></a>
### IsNegotiated()

`public abstract System.Boolean IsNegotiated()`

Reports whether stream identity was negotiated out of band.

Returns: True for explicit negotiation.

<a id="member-859dfebd292a"></a>
### IsOrdered()

`public abstract System.Boolean IsOrdered()`

Reports whether delivered messages preserve ordering.

Returns: True for ordered delivery.

<a id="member-f4efa3780d78"></a>
### Poll()

`public abstract System.Void Poll()`

Observes native channel errors without consuming queued packets.

<a id="member-078566734d8f"></a>
### WasStringPacket()

`public abstract System.Boolean WasStringPacket()`

Reports the type of the last successfully received packet.

Returns: True for UTF-8 text, false for binary or before any read.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
