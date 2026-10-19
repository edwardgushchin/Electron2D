# WebRTCPeerInfo

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.WebRTCPeerInfo`. **Source:** [WebRTCMultiplayerPeer.cs](../../src/Core/Networking/WebRTCMultiplayerPeer.cs). **Component:** [WebRTC](../components/webrtc.md).

## Description

Contains a copied membership observation with borrowed connection and channel identities.

Immutable membership observation returned by WebRTCMultiplayerPeer. Connection and each channel remain borrowed resource identities. The Channels collection copies membership and prevents callers from editing it; later removal retires those owned channels while the caller connection survives. Connected records the state at snapshot time.

## Example

Public C# excerpt. Channel/observation examples require the indicated application transport; MyPeer/applicationChannel stand for a complete application extension. The peer example generates and commits only its local offer; a remote peer and an application signaling path are required for connectivity. Equivalent complete native and scene workflows execute in WebRTCTests.

```csharp
WebRTCPeerInfo? observation = transport.GetPeer(7);
if (observation is not null)
    Console.WriteLine(observation.Channels.Count);
```

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Collections.Generic.IReadOnlyList<Electron2D.WebRTCDataChannel> Channels { get;  }` | Gets copied channel membership containing borrowed channel objects. |
| `public System.Boolean Connected { get;  }` | Gets whether every transport channel was open at snapshot time. |
| `public Electron2D.WebRTCPeerConnection Connection { get;  }` | Gets the caller-owned negotiated connection. |

## Property Descriptions

<a id="member-d620d84b9574"></a>
### Channels

`public System.Collections.Generic.IReadOnlyList<Electron2D.WebRTCDataChannel> Channels { get;  }`

Gets copied channel membership containing borrowed channel objects.

Value: Three reserved channels followed by configured additional channels; membership removal retires them.

<a id="member-1608f5815cd8"></a>
### Connected

`public System.Boolean Connected { get;  }`

Gets whether every transport channel was open at snapshot time.

Value: A copied membership flag.

<a id="member-48682efe0474"></a>
### Connection

`public Electron2D.WebRTCPeerConnection Connection { get;  }`

Gets the caller-owned negotiated connection.

Value: The same connection supplied to AddPeer.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
