# WebRTCICEServer

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.WebRTCICEServer`. **Source:** [WebRTCConfiguration.cs](../../src/Core/Networking/WebRTCConfiguration.cs). **Component:** [WebRTC](../components/webrtc.md).

## Description

Describes STUN/TURN URLs and optional relay credentials.

One ICE endpoint group supplied through WebRTCConfiguration. URLs use STUN/STUNS/TURN/TURNS schemes; username and credential are copied with the endpoints during initialization. Strings reject NUL, malformed UTF-8 and more than 65535 UTF-8 bytes. The complete configuration permits at most 1024 URLs. Routed service reachability remains an external prerequisite.

## Example

Public C# excerpt. Channel/observation examples require the indicated application transport; MyPeer/applicationChannel stand for a complete application extension. The peer example generates and commits only its local offer; a remote peer and an application signaling path are required for connectivity. Equivalent complete native and scene workflows execute in WebRTCTests.

```csharp
var server = new WebRTCICEServer { URLs = ["turn:127.0.0.1:3478"], Username = "player", Credential = "example" };
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public WebRTCICEServer()` | Creates an empty ICE endpoint group. |

## Constructor Descriptions

<a id="member-952d8098f1ff"></a>
### WebRTCICEServer()

`public WebRTCICEServer()`

Creates an empty ICE endpoint group.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.String Credential { get; set; }` | Gets or initializes an optional relay password. |
| `public System.Collections.Generic.IReadOnlyList<System.String> URLs { get; set; }` | Gets or initializes one or more discovery/relay URLs. |
| `public System.String Username { get; set; }` | Gets or initializes an optional relay username. |

## Property Descriptions

<a id="member-f3ae788fb42c"></a>
### Credential

`public System.String Credential { get; set; }`

Gets or initializes an optional relay password.

Value: Null when no password is needed; never included in diagnostics.

<a id="member-37127d5947fd"></a>
### URLs

`public System.Collections.Generic.IReadOnlyList<System.String> URLs { get; set; }`

Gets or initializes one or more discovery/relay URLs.

Value: STUN, STUNS, TURN or TURNS URLs without embedded null characters.

<a id="member-9ea0f8cfb43e"></a>
### Username

`public System.String Username { get; set; }`

Gets or initializes an optional relay username.

Value: Null when no username is needed.

## Lifecycle, errors and verification

The [component contract](../components/webrtc.md) defines complete ownership, threading, signaling, budgets and multiplayer interaction. PacketPeer descendants inherit GetPacket/GetPacketError/LastReadException and caller-span behavior; ElectronObject descendants require owner-thread disposal. Invalid arguments reject before commitment. Native operation failures surface as IOException; user callback/setup cleanup failures aggregate. Snapshot/configuration/string operations allocate.

WebRTCTests executes native peer/channel and extension contracts, bounded receive overflow, reserved/custom multiplayer channels and SceneTree RPC. The independent browser fixture verifies wire packets. Prepared owner-thread intervals measure managed allocations only; routed STUN/TURN, native allocator totals, browser-hosted engine adapter, foreign platforms and human acceptance retain separate gates. See [ADR 0094](../decisions/networking.md#adr-0094).
