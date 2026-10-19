# WebRTCPeerConnection.ConnectionState

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.WebRTCPeerConnection.ConnectionState`. **Source:** [WebRTCPeerConnection.cs](../../src/Core/Networking/WebRTCPeerConnection.cs). **Component:** [WebRTC](../components/webrtc.md).

## Description

Describes transport establishment.

Owner-specific state observations preserve the declared numeric values. Querying state does not dispatch callbacks; Poll advances queued owner observations.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.WebRTCPeerConnection.ConnectionState Closed = 5` | Closed state. |
| `public const Electron2D.WebRTCPeerConnection.ConnectionState Connected = 2` | Connected state. |
| `public const Electron2D.WebRTCPeerConnection.ConnectionState Connecting = 1` | Connecting state. |
| `public const Electron2D.WebRTCPeerConnection.ConnectionState Disconnected = 3` | Disconnected state. |
| `public const Electron2D.WebRTCPeerConnection.ConnectionState Failed = 4` | Failed state. |
| `public const Electron2D.WebRTCPeerConnection.ConnectionState New = 0` | New state. |

## Enumeration Descriptions

<a id="member-d238a98775c1"></a>
### Closed

`public const Electron2D.WebRTCPeerConnection.ConnectionState Closed = 5`

Closed state.

<a id="member-5c824517506b"></a>
### Connected

`public const Electron2D.WebRTCPeerConnection.ConnectionState Connected = 2`

Connected state.

<a id="member-b0d906ff108b"></a>
### Connecting

`public const Electron2D.WebRTCPeerConnection.ConnectionState Connecting = 1`

Connecting state.

<a id="member-780c401e574c"></a>
### Disconnected

`public const Electron2D.WebRTCPeerConnection.ConnectionState Disconnected = 3`

Disconnected state.

<a id="member-382919301ec7"></a>
### Failed

`public const Electron2D.WebRTCPeerConnection.ConnectionState Failed = 4`

Failed state.

<a id="member-4623e53fad40"></a>
### New

`public const Electron2D.WebRTCPeerConnection.ConnectionState New = 0`

New state.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
