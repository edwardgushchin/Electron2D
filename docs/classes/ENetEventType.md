# ENetEventType

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.ENetEventType`. **Source:** [ENetEventType.cs](../../src/Core/Networking/ENetEventType.cs).

## Description

Identifies the EventType domain of an ENet transport.



See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.ENetEventType Connect = 1` | A peer completed connection negotiation. |
| `public const Electron2D.ENetEventType Disconnect = 2` | A peer completed or timed out disconnection. |
| `public const Electron2D.ENetEventType Error = -1` | Represents a transport failure; Service reports the underlying exception. |
| `public const Electron2D.ENetEventType None = 0` | No queued event became available. |
| `public const Electron2D.ENetEventType Receive = 3` | A complete packet was queued on the associated peer. |

## Enumeration Descriptions

<a id="member-f64e4d1a9795"></a>
### Connect

`public const Electron2D.ENetEventType Connect = 1`

A peer completed connection negotiation.

<a id="member-5c8c4c9f2db5"></a>
### Disconnect

`public const Electron2D.ENetEventType Disconnect = 2`

A peer completed or timed out disconnection.

<a id="member-c9bb4964ce13"></a>
### Error

`public const Electron2D.ENetEventType Error = -1`

Represents a transport failure; Service reports the underlying exception.

<a id="member-49b9b26a22d4"></a>
### None

`public const Electron2D.ENetEventType None = 0`

No queued event became available.

<a id="member-e970178d8a83"></a>
### Receive

`public const Electron2D.ENetEventType Receive = 3`

A complete packet was queued on the associated peer.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
