# WebRTCDataChannel.ChannelState

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.WebRTCDataChannel.ChannelState`. **Source:** [WebRTCDataChannel.cs](../../src/Core/Networking/WebRTCDataChannel.cs). **Component:** [WebRTC](../components/webrtc.md).

## Description

Describes the channel's ready state.

Owner-specific state observations preserve the declared numeric values. Querying state does not dispatch callbacks; Poll advances queued owner observations.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.WebRTCDataChannel.ChannelState Closed = 3` | The channel is closed. |
| `public const Electron2D.WebRTCDataChannel.ChannelState Closing = 2` | The channel is closing. |
| `public const Electron2D.WebRTCDataChannel.ChannelState Connecting = 0` | The channel is connecting. |
| `public const Electron2D.WebRTCDataChannel.ChannelState Open = 1` | The channel accepts messages. |

## Enumeration Descriptions

<a id="member-456ac85d1bf7"></a>
### Closed

`public const Electron2D.WebRTCDataChannel.ChannelState Closed = 3`

The channel is closed.

<a id="member-ae1a351bfaf6"></a>
### Closing

`public const Electron2D.WebRTCDataChannel.ChannelState Closing = 2`

The channel is closing.

<a id="member-edadf8db0e32"></a>
### Connecting

`public const Electron2D.WebRTCDataChannel.ChannelState Connecting = 0`

The channel is connecting.

<a id="member-07d77bb151b6"></a>
### Open

`public const Electron2D.WebRTCDataChannel.ChannelState Open = 1`

The channel accepts messages.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
