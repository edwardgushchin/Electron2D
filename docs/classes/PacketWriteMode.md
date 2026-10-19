# PacketWriteMode

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.PacketWriteMode`. **Source:** [PacketWriteMode.cs](../../src/Core/Networking/PacketWriteMode.cs). **Component:** [WebRTC](../components/webrtc.md).

## Description

Selects the common WebSocket and WebRTC message payload contract.

One semantic text/binary contract shared by WebSocketPeer and WebRTCDataChannel under ADR 0051. Text=0 requires valid UTF-8 bytes; Binary=1 accepts arbitrary bytes. Channels preserve the incoming packet type independently of the outgoing mode.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.PacketWriteMode Binary = 1` | Payload bytes are arbitrary binary data. |
| `public const Electron2D.PacketWriteMode Text = 0` | Payload bytes must be valid UTF-8 text. |

## Enumeration Descriptions

<a id="member-0a4942f9a330"></a>
### Binary

`public const Electron2D.PacketWriteMode Binary = 1`

Payload bytes are arbitrary binary data.

<a id="member-6c4741b30fb5"></a>
### Text

`public const Electron2D.PacketWriteMode Text = 0`

Payload bytes must be valid UTF-8 text.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
