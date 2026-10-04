# ENetPeerStatistic

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.ENetPeerStatistic`. **Source:** [ENetPeerStatistic.cs](../../src/Core/Networking/ENetPeerStatistic.cs).

## Description

Identifies the PeerStatistic domain of an ENet transport.



See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.ENetPeerStatistic LastRoundTripTime = 5` | Last reliable-packet round-trip time in milliseconds. |
| `public const Electron2D.ENetPeerStatistic LastRoundTripTimeVariance = 6` | Last recorded round-trip-time variance. |
| `public const Electron2D.ENetPeerStatistic PacketLoss = 0` | Mean reliable-packet loss scaled by PacketLossScale. |
| `public const Electron2D.ENetPeerStatistic PacketLossEpoch = 2` | Native monotonic timestamp of the last loss update. |
| `public const Electron2D.ENetPeerStatistic PacketLossVariance = 1` | Variance of reliable-packet loss. |
| `public const Electron2D.ENetPeerStatistic PacketThrottle = 7` | Current scaled unreliable-packet throttle. |
| `public const Electron2D.ENetPeerStatistic PacketThrottleAcceleration = 11` | Configured throttle acceleration. |
| `public const Electron2D.ENetPeerStatistic PacketThrottleCounter = 9` | Native throttle sequence counter. |
| `public const Electron2D.ENetPeerStatistic PacketThrottleDeceleration = 12` | Configured throttle deceleration. |
| `public const Electron2D.ENetPeerStatistic PacketThrottleEpoch = 10` | Native monotonic timestamp of the last throttle update. |
| `public const Electron2D.ENetPeerStatistic PacketThrottleInterval = 13` | Configured throttle measurement interval in milliseconds. |
| `public const Electron2D.ENetPeerStatistic PacketThrottleLimit = 8` | Current scaled throttle limit. |
| `public const Electron2D.ENetPeerStatistic RoundTripTime = 3` | Mean reliable-packet round-trip time in milliseconds. |
| `public const Electron2D.ENetPeerStatistic RoundTripTimeVariance = 4` | Round-trip-time variance in milliseconds. |

## Enumeration Descriptions

<a id="member-3d6e3e038750"></a>
### LastRoundTripTime

`public const Electron2D.ENetPeerStatistic LastRoundTripTime = 5`

Last reliable-packet round-trip time in milliseconds.

<a id="member-6a8b9d3b555a"></a>
### LastRoundTripTimeVariance

`public const Electron2D.ENetPeerStatistic LastRoundTripTimeVariance = 6`

Last recorded round-trip-time variance.

<a id="member-24746f0e42af"></a>
### PacketLoss

`public const Electron2D.ENetPeerStatistic PacketLoss = 0`

Mean reliable-packet loss scaled by PacketLossScale.

<a id="member-6a64185e441e"></a>
### PacketLossEpoch

`public const Electron2D.ENetPeerStatistic PacketLossEpoch = 2`

Native monotonic timestamp of the last loss update.

<a id="member-2056bada48a4"></a>
### PacketLossVariance

`public const Electron2D.ENetPeerStatistic PacketLossVariance = 1`

Variance of reliable-packet loss.

<a id="member-9b02935891f6"></a>
### PacketThrottle

`public const Electron2D.ENetPeerStatistic PacketThrottle = 7`

Current scaled unreliable-packet throttle.

<a id="member-f81cd47552c0"></a>
### PacketThrottleAcceleration

`public const Electron2D.ENetPeerStatistic PacketThrottleAcceleration = 11`

Configured throttle acceleration.

<a id="member-b8c12ca032aa"></a>
### PacketThrottleCounter

`public const Electron2D.ENetPeerStatistic PacketThrottleCounter = 9`

Native throttle sequence counter.

<a id="member-a51d6034a83f"></a>
### PacketThrottleDeceleration

`public const Electron2D.ENetPeerStatistic PacketThrottleDeceleration = 12`

Configured throttle deceleration.

<a id="member-649077d06759"></a>
### PacketThrottleEpoch

`public const Electron2D.ENetPeerStatistic PacketThrottleEpoch = 10`

Native monotonic timestamp of the last throttle update.

<a id="member-b89c96414c66"></a>
### PacketThrottleInterval

`public const Electron2D.ENetPeerStatistic PacketThrottleInterval = 13`

Configured throttle measurement interval in milliseconds.

<a id="member-f8613db25260"></a>
### PacketThrottleLimit

`public const Electron2D.ENetPeerStatistic PacketThrottleLimit = 8`

Current scaled throttle limit.

<a id="member-60b41164a036"></a>
### RoundTripTime

`public const Electron2D.ENetPeerStatistic RoundTripTime = 3`

Mean reliable-packet round-trip time in milliseconds.

<a id="member-92b03936c3bf"></a>
### RoundTripTimeVariance

`public const Electron2D.ENetPeerStatistic RoundTripTimeVariance = 4`

Round-trip-time variance in milliseconds.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
