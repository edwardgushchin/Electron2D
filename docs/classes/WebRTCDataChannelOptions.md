# WebRTCDataChannelOptions

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.WebRTCDataChannelOptions`. **Source:** [WebRTCConfiguration.cs](../../src/Core/Networking/WebRTCConfiguration.cs). **Component:** [WebRTC](../components/webrtc.md).

## Description

Configures a copied data-channel creation request.

Channel creation input. Negotiated=true requires ID 0 through 65534. MaxPacketLifeTime and MaxRetransmits are mutually exclusive integer limits, including explicit zero; unset limits select reliable delivery. Label/protocol values reject NUL, malformed UTF-8 and more than 65535 UTF-8 bytes. Create channels while the peer is New.

## Example

Public C# excerpt. Channel/observation examples require the indicated application transport; MyPeer/applicationChannel stand for a complete application extension. The peer example generates and commits only its local offer; a remote peer and an application signaling path are required for connectivity. Equivalent complete native and scene workflows execute in WebRTCTests.

```csharp
var options = new WebRTCDataChannelOptions { Negotiated = true, ID = 4, Ordered = false, MaxPacketLifeTime = 1 };
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public WebRTCDataChannelOptions()` | Creates reliable ordered in-band channel options. |

## Constructor Descriptions

<a id="member-e822c5688c63"></a>
### WebRTCDataChannelOptions()

`public WebRTCDataChannelOptions()`

Creates reliable ordered in-band channel options.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Nullable<System.Int32> ID { get; set; }` | Gets or initializes an optional stream identity. |
| `public System.Nullable<System.Int32> MaxPacketLifeTime { get; set; }` | Gets or initializes a partial-reliability lifetime in milliseconds. |
| `public System.Nullable<System.Int32> MaxRetransmits { get; set; }` | Gets or initializes a partial-reliability retransmission count. |
| `public System.Boolean Negotiated { get; set; }` | Gets or initializes whether both peers explicitly negotiate the same channel. |
| `public System.Boolean Ordered { get; set; }` | Gets or initializes whether delivery preserves ordering. |
| `public System.String Protocol { get; set; }` | Gets or initializes an application subprotocol label. |

## Property Descriptions

<a id="member-784c6f194c41"></a>
### ID

`public System.Nullable<System.Int32> ID { get; set; }`

Gets or initializes an optional stream identity.

Value: Null for automatic assignment, or zero through 65534.

<a id="member-a3faa29225c6"></a>
### MaxPacketLifeTime

`public System.Nullable<System.Int32> MaxPacketLifeTime { get; set; }`

Gets or initializes a partial-reliability lifetime in milliseconds.

Value: Null for no lifetime limit; mutually exclusive with MaxRetransmits.

<a id="member-6727c0ffd7c4"></a>
### MaxRetransmits

`public System.Nullable<System.Int32> MaxRetransmits { get; set; }`

Gets or initializes a partial-reliability retransmission count.

Value: Null for no count limit; mutually exclusive with MaxPacketLifeTime.

<a id="member-55b9eeff5abc"></a>
### Negotiated

`public System.Boolean Negotiated { get; set; }`

Gets or initializes whether both peers explicitly negotiate the same channel.

Value: False initially; true requires ID.

<a id="member-2089289fb07c"></a>
### Ordered

`public System.Boolean Ordered { get; set; }`

Gets or initializes whether delivery preserves ordering.

Value: True initially, independently of retransmission reliability.

<a id="member-5fdb500707f9"></a>
### Protocol

`public System.String Protocol { get; set; }`

Gets or initializes an application subprotocol label.

Value: Empty initially.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
