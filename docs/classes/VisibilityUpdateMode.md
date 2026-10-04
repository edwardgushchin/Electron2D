# VisibilityUpdateMode

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.VisibilityUpdateMode`. **Source:** [MultiplayerSynchronizer.cs](../../src/Scene/Multiplayer/MultiplayerSynchronizer.cs).

## Description

Selects when a synchronizer reevaluates its peer visibility filters.



**Inherits:** `System.Enum`.

[Scene replication](../components/scene-replication.md) defines authority, target/factory identity, copied policies, callback ordering, concrete codecs, prepared budgets and verification boundaries. No public backend handle or universal object serializer is supplied.


## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `Idle` | 0 | Updates during internal process frames. |
| `None` | 2 | Updates only through explicit calls. |
| `Physics` | 1 | Updates during internal physics frames. |

## Enumeration Descriptions

<a id="member-407888644033"></a>
### Idle

`Idle = 0`

Updates during internal process frames.

<a id="member-e506bda545ce"></a>
### None

`None = 2`

Updates only through explicit calls.

<a id="member-237189f84ecd"></a>
### Physics

`Physics = 1`

Updates during internal physics frames.


## Verification and limits

[SceneReplicationTests](../../tests/Electron2D.Tests/SceneReplicationTests.cs) verifies native WS/WSS custom/automatic/late/visibility spawning and state, copied config, independent malformed/old/wrong-authority schema bytes, actual batching/modes and 64 warmed active/idle zero-managed-allocation intervals. Setup/templates/snapshots/error/caller-codec/native costs remain separate. File-based scene formats/load/save and editor authoring, foreign/routed traffic and human/rendered acceptance require their own gates.
