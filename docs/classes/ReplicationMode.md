# ReplicationMode

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.ReplicationMode`. **Source:** [SceneReplicationConfig.cs](../../src/Scene/Resources/SceneReplicationConfig.cs).

## Description

Selects whether a configured property sends periodic, change-only or no ongoing updates.



**Inherits:** `System.Enum`.

[Scene replication](../components/scene-replication.md) defines authority, target/factory identity, copied policies, callback ordering, concrete codecs, prepared budgets and verification boundaries. No public backend handle or universal object serializer is supplied.


## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `Always` | 1 | Sends at each replication interval. |
| `Never` | 0 | Does not send ongoing updates; spawn state remains independently configurable. |
| `OnChange` | 2 | Sends changed encoded values at the delta interval. |

## Enumeration Descriptions

<a id="member-2bfad389c810"></a>
### Always

`Always = 1`

Sends at each replication interval.

<a id="member-56c6afea0ba9"></a>
### Never

`Never = 0`

Does not send ongoing updates; spawn state remains independently configurable.

<a id="member-aa2310e9cc1f"></a>
### OnChange

`OnChange = 2`

Sends changed encoded values at the delta interval.


## Verification and limits

[SceneReplicationTests](../../tests/Electron2D.Tests/SceneReplicationTests.cs) verifies native WS/WSS custom/automatic/late/visibility spawning and state, copied config, independent malformed/old/wrong-authority schema bytes, actual batching/modes and 64 warmed active/idle zero-managed-allocation intervals. Setup/templates/snapshots/error/caller-codec/native costs remain separate. File-based scene formats/load/save and editor authoring, foreign/routed traffic and human/rendered acceptance require their own gates.
