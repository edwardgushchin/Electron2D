# ENetPacketFlags

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.ENetPacketFlags`. **Source:** [ENetPacketFlags.cs](../../src/Core/Networking/ENetPacketFlags.cs).

## Description

Selects ENet packet reliability, ordering and fragmentation behavior.



See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.ENetPacketFlags None = 0` | Uses ordinary sequenced unreliable delivery. |
| `public const Electron2D.ENetPacketFlags Reliable = 1` | Retransmits and orders the packet. |
| `public const Electron2D.ENetPacketFlags UnreliableFragment = 8` | Allows unreliable fragments instead of promoting fragmented data to reliable delivery. |
| `public const Electron2D.ENetPacketFlags Unsequenced = 2` | Permits delivery without sequencing. |

## Enumeration Descriptions

<a id="member-5c7da3278990"></a>
### None

`public const Electron2D.ENetPacketFlags None = 0`

Uses ordinary sequenced unreliable delivery.

<a id="member-7610f7e0da61"></a>
### Reliable

`public const Electron2D.ENetPacketFlags Reliable = 1`

Retransmits and orders the packet.

<a id="member-a7c30024bf85"></a>
### UnreliableFragment

`public const Electron2D.ENetPacketFlags UnreliableFragment = 8`

Allows unreliable fragments instead of promoting fragmented data to reliable delivery.

<a id="member-e008b8c5a994"></a>
### Unsequenced

`public const Electron2D.ENetPacketFlags Unsequenced = 2`

Permits delivery without sequencing.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
