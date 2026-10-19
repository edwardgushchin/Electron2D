# WebRTCPeerConnection.SignalingState

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.WebRTCPeerConnection.SignalingState`. **Source:** [WebRTCPeerConnection.cs](../../src/Core/Networking/WebRTCPeerConnection.cs). **Component:** [WebRTC](../components/webrtc.md).

## Description

Describes SDP offer/answer commitment.

Owner-specific state observations preserve the declared numeric values. Querying state does not dispatch callbacks; Poll advances queued owner observations.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.WebRTCPeerConnection.SignalingState Closed = 5` | Closed state. |
| `public const Electron2D.WebRTCPeerConnection.SignalingState HaveLocalOffer = 1` | HaveLocalOffer state. |
| `public const Electron2D.WebRTCPeerConnection.SignalingState HaveLocalPranswer = 3` | HaveLocalPranswer state. |
| `public const Electron2D.WebRTCPeerConnection.SignalingState HaveRemoteOffer = 2` | HaveRemoteOffer state. |
| `public const Electron2D.WebRTCPeerConnection.SignalingState HaveRemotePranswer = 4` | HaveRemotePranswer state. |
| `public const Electron2D.WebRTCPeerConnection.SignalingState Stable = 0` | Stable state. |

## Enumeration Descriptions

<a id="member-183a5d02f894"></a>
### Closed

`public const Electron2D.WebRTCPeerConnection.SignalingState Closed = 5`

Closed state.

<a id="member-a16454c2228c"></a>
### HaveLocalOffer

`public const Electron2D.WebRTCPeerConnection.SignalingState HaveLocalOffer = 1`

HaveLocalOffer state.

<a id="member-52520c7d758d"></a>
### HaveLocalPranswer

`public const Electron2D.WebRTCPeerConnection.SignalingState HaveLocalPranswer = 3`

HaveLocalPranswer state.

<a id="member-0974ab6f3adb"></a>
### HaveRemoteOffer

`public const Electron2D.WebRTCPeerConnection.SignalingState HaveRemoteOffer = 2`

HaveRemoteOffer state.

<a id="member-4d1d70226b84"></a>
### HaveRemotePranswer

`public const Electron2D.WebRTCPeerConnection.SignalingState HaveRemotePranswer = 4`

HaveRemotePranswer state.

<a id="member-ffea0217498a"></a>
### Stable

`public const Electron2D.WebRTCPeerConnection.SignalingState Stable = 0`

Stable state.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
