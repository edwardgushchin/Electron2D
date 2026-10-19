# WebRTCConfiguration

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.WebRTCConfiguration`. **Source:** [WebRTCConfiguration.cs](../../src/Core/Networking/WebRTCConfiguration.cs). **Component:** [WebRTC](../components/webrtc.md).

## Description

Configures a copied set of ICE discovery/relay servers for a peer.

Initialization input. ICEServers is enumerated and copied before native peer creation. Invalid values, enumerator errors and reentrant peer edits preserve the old native cohort. Null configuration selects host candidates without STUN/TURN. No network listener or signaling service is created by this value object.

## Example

Public C# excerpt. Channel/observation examples require the indicated application transport; MyPeer/applicationChannel stand for a complete application extension. The peer example generates and commits only its local offer; a remote peer and an application signaling path are required for connectivity. Equivalent complete native and scene workflows execute in WebRTCTests.

```csharp
var configuration = new WebRTCConfiguration
{
    ICEServers = [new WebRTCICEServer { URLs = ["stun:127.0.0.1:3478"] }]
};
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public WebRTCConfiguration()` | Creates an empty host-candidate configuration. |

## Constructor Descriptions

<a id="member-4d1ebb9b23bf"></a>
### WebRTCConfiguration()

`public WebRTCConfiguration()`

Creates an empty host-candidate configuration.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Collections.Generic.IReadOnlyList<Electron2D.WebRTCICEServer> ICEServers { get; set; }` | Gets or initializes STUN/TURN endpoints; an empty list uses host candidates. |

## Property Descriptions

<a id="member-aeca35e7de31"></a>
### ICEServers

`public System.Collections.Generic.IReadOnlyList<Electron2D.WebRTCICEServer> ICEServers { get; set; }`

Gets or initializes STUN/TURN endpoints; an empty list uses host candidates.

Value: Caller-owned entries copied during Initialize.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
