# RPCMode

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RPCMode`. **Source:** [RPCMethod.cs](../../src/Core/Networking/RPCMethod.cs).

## Description

Selects which remote peers may invoke a configured RPC.



**Inherits:** `System.Enum`.

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.


## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `AnyPeer` | 1 | Accepts invocations from every admitted peer. |
| `Authority` | 2 | Accepts only the receiving node's multiplayer authority. |
| `Disabled` | 0 | Rejects remote invocations. |

## Enumeration Descriptions

<a id="member-3c0a1c045fd9"></a>
### AnyPeer

`AnyPeer = 1`

Accepts invocations from every admitted peer.

<a id="member-51af42f15d5b"></a>
### Authority

`Authority = 2`

Accepts only the receiving node's multiplayer authority.

<a id="member-4bcb62ea1fee"></a>
### Disabled

`Disabled = 0`

Rejects remote invocations.


## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.
