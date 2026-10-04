# ENetPeerState

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.ENetPeerState`. **Source:** [ENetPeerState.cs](../../src/Core/Networking/ENetPeerState.cs).

## Description

Identifies the PeerState domain of an ENet transport.



See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.ENetPeerState AcknowledgingConnect = 2` | An incoming connection acknowledgement is pending. |
| `public const Electron2D.ENetPeerState AcknowledgingDisconnect = 8` | An incoming disconnect awaits acknowledgement. |
| `public const Electron2D.ENetPeerState Connected = 5` | Application packets can be exchanged. |
| `public const Electron2D.ENetPeerState Connecting = 1` | An outgoing connection is negotiating. |
| `public const Electron2D.ENetPeerState ConnectionPending = 3` | A connection awaits local negotiation progress. |
| `public const Electron2D.ENetPeerState ConnectionSucceeded = 4` | Successful negotiation awaits local notification. |
| `public const Electron2D.ENetPeerState DisconnectLater = 6` | Outgoing packets drain before disconnect. |
| `public const Electron2D.ENetPeerState Disconnected = 0` | Native peer slot is detached. |
| `public const Electron2D.ENetPeerState Disconnecting = 7` | An outgoing disconnect awaits acknowledgement. |
| `public const Electron2D.ENetPeerState Zombie = 9` | Disconnection awaits local notification. |

## Enumeration Descriptions

<a id="member-0178e3d8bcc3"></a>
### AcknowledgingConnect

`public const Electron2D.ENetPeerState AcknowledgingConnect = 2`

An incoming connection acknowledgement is pending.

<a id="member-c8601eff3ae2"></a>
### AcknowledgingDisconnect

`public const Electron2D.ENetPeerState AcknowledgingDisconnect = 8`

An incoming disconnect awaits acknowledgement.

<a id="member-f296583aaedc"></a>
### Connected

`public const Electron2D.ENetPeerState Connected = 5`

Application packets can be exchanged.

<a id="member-d2b8a4c07b80"></a>
### Connecting

`public const Electron2D.ENetPeerState Connecting = 1`

An outgoing connection is negotiating.

<a id="member-486c80b127f1"></a>
### ConnectionPending

`public const Electron2D.ENetPeerState ConnectionPending = 3`

A connection awaits local negotiation progress.

<a id="member-26eb8fe4dfdf"></a>
### ConnectionSucceeded

`public const Electron2D.ENetPeerState ConnectionSucceeded = 4`

Successful negotiation awaits local notification.

<a id="member-e56a6f01080f"></a>
### DisconnectLater

`public const Electron2D.ENetPeerState DisconnectLater = 6`

Outgoing packets drain before disconnect.

<a id="member-97cf0af144b4"></a>
### Disconnected

`public const Electron2D.ENetPeerState Disconnected = 0`

Native peer slot is detached.

<a id="member-8c74385f2dda"></a>
### Disconnecting

`public const Electron2D.ENetPeerState Disconnecting = 7`

An outgoing disconnect awaits acknowledgement.

<a id="member-386747d07ab3"></a>
### Zombie

`public const Electron2D.ENetPeerState Zombie = 9`

Disconnection awaits local notification.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
