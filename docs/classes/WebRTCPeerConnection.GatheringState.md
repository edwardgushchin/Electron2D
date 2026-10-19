# WebRTCPeerConnection.GatheringState

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.WebRTCPeerConnection.GatheringState`. **Source:** [WebRTCPeerConnection.cs](../../src/Core/Networking/WebRTCPeerConnection.cs). **Component:** [WebRTC](../components/webrtc.md).

## Description

Describes local ICE discovery.

Owner-specific state observations preserve the declared numeric values. Querying state does not dispatch callbacks; Poll advances queued owner observations.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.WebRTCPeerConnection.GatheringState Complete = 2` | Complete state. |
| `public const Electron2D.WebRTCPeerConnection.GatheringState Gathering = 1` | Gathering state. |
| `public const Electron2D.WebRTCPeerConnection.GatheringState New = 0` | New state. |

## Enumeration Descriptions

<a id="member-ed81e4b830f5"></a>
### Complete

`public const Electron2D.WebRTCPeerConnection.GatheringState Complete = 2`

Complete state.

<a id="member-fe9143927939"></a>
### Gathering

`public const Electron2D.WebRTCPeerConnection.GatheringState Gathering = 1`

Gathering state.

<a id="member-b46a9bcb690a"></a>
### New

`public const Electron2D.WebRTCPeerConnection.GatheringState New = 0`

New state.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
