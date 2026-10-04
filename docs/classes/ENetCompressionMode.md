# ENetCompressionMode

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.ENetCompressionMode`. **Source:** [ENetCompressionMode.cs](../../src/Core/Networking/ENetCompressionMode.cs).

## Description

Identifies the CompressionMode domain of an ENet transport.



See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.ENetCompressionMode FASTLZ = 2` | Uses the FastLZ block codec. |
| `public const Electron2D.ENetCompressionMode None = 0` | Disables payload compression. |
| `public const Electron2D.ENetCompressionMode RangeCoder = 1` | Uses the prepared ENet range coder. |
| `public const Electron2D.ENetCompressionMode ZLIB = 3` | Uses the zlib block codec. |
| `public const Electron2D.ENetCompressionMode ZSTD = 4` | Uses the Zstandard block codec. |

## Enumeration Descriptions

<a id="member-6563ef8ab645"></a>
### FASTLZ

`public const Electron2D.ENetCompressionMode FASTLZ = 2`

Uses the FastLZ block codec.

<a id="member-f9ecf9e3f8b4"></a>
### None

`public const Electron2D.ENetCompressionMode None = 0`

Disables payload compression.

<a id="member-0f26c8e203c4"></a>
### RangeCoder

`public const Electron2D.ENetCompressionMode RangeCoder = 1`

Uses the prepared ENet range coder.

<a id="member-444f6a829bea"></a>
### ZLIB

`public const Electron2D.ENetCompressionMode ZLIB = 3`

Uses the zlib block codec.

<a id="member-94d96650e92a"></a>
### ZSTD

`public const Electron2D.ENetCompressionMode ZSTD = 4`

Uses the Zstandard block codec.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
