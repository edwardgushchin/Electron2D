# TransferMode

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.TransferMode`. **Source:** [MultiplayerPeer.cs](../../src/Core/Networking/MultiplayerPeer.cs). **Component:** [Multiplayer](../components/multiplayer.md).

## Description

Selects the transport delivery and ordering contract.

## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `Reliable` | 2 | Packets are retransmitted and delivered in order. |
| `Unreliable` | 0 | Packets may be lost or arrive out of order. |
| `UnreliableOrdered` | 1 | Packets may be lost, while delivered packets preserve ordering. |

## Enumeration Descriptions

<a id="member-6f1f579850f4"></a>
### Reliable

`Reliable = 2`

Packets are retransmitted and delivered in order.

<a id="member-318cfda25467"></a>
### Unreliable

`Unreliable = 0`

Packets may be lost or arrive out of order.

<a id="member-ac74314f24c0"></a>
### UnreliableOrdered

`UnreliableOrdered = 1`

Packets may be lost, while delivered packets preserve ordering.

## Verification

MultiplayerTests verifies selectors, transport lifecycle and configured versus actual delivery semantics. This shared enum remains distinct from raw socket/HTTP/WebSocket/backend state under ADR 0051.
