# ENetHostStatistic

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.ENetHostStatistic`. **Source:** [ENetHostStatistic.cs](../../src/Core/Networking/ENetHostStatistic.cs).

## Description

Identifies the HostStatistic domain of an ENet transport.



See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.ENetHostStatistic TotalReceivedData = 2` | Counts received bytes. |
| `public const Electron2D.ENetHostStatistic TotalReceivedPackets = 3` | Counts received datagrams. |
| `public const Electron2D.ENetHostStatistic TotalSentData = 0` | Counts transmitted bytes. |
| `public const Electron2D.ENetHostStatistic TotalSentPackets = 1` | Counts transmitted datagrams. |

## Enumeration Descriptions

<a id="member-cde947431353"></a>
### TotalReceivedData

`public const Electron2D.ENetHostStatistic TotalReceivedData = 2`

Counts received bytes.

<a id="member-2fb01d972636"></a>
### TotalReceivedPackets

`public const Electron2D.ENetHostStatistic TotalReceivedPackets = 3`

Counts received datagrams.

<a id="member-1ec0911bf732"></a>
### TotalSentData

`public const Electron2D.ENetHostStatistic TotalSentData = 0`

Counts transmitted bytes.

<a id="member-8893a9220b22"></a>
### TotalSentPackets

`public const Electron2D.ENetHostStatistic TotalSentPackets = 1`

Counts transmitted datagrams.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
