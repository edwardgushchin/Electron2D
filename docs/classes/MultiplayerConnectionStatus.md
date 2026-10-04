# MultiplayerConnectionStatus

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.MultiplayerConnectionStatus`. **Source:** [MultiplayerPeer.cs](../../src/Core/Networking/MultiplayerPeer.cs). **Component:** [Multiplayer](../components/multiplayer.md).

## Description

Describes the multiplayer transport's local connection lifecycle.

## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `Connected` | 2 | The multiplayer endpoint is ready to exchange application packets. |
| `Connecting` | 1 | Transport establishment or peer identity assignment is pending. |
| `Disconnected` | 0 | No multiplayer connection is active. |

## Enumeration Descriptions

<a id="member-8afd045e775a"></a>
### Connected

`Connected = 2`

The multiplayer endpoint is ready to exchange application packets.

<a id="member-315ff79f2f0b"></a>
### Connecting

`Connecting = 1`

Transport establishment or peer identity assignment is pending.

<a id="member-b10ebff0fc02"></a>
### Disconnected

`Disconnected = 0`

No multiplayer connection is active.

## Verification

MultiplayerTests verifies selectors, transport lifecycle and configured versus actual delivery semantics. This shared enum remains distinct from raw socket/HTTP/WebSocket/backend state under ADR 0051.
