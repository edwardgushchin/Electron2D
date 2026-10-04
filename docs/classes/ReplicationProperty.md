# ReplicationProperty

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.ReplicationProperty`. **Source:** [ReplicationProperty.cs](../../src/Core/Networking/ReplicationProperty.cs).

## Description

Identifies one explicitly typed, encoded scene property without reflection.

IDs are nonzero and unique within a configuration. Participants agree on codecs/IDs. NodePath locates a node relative to the synchronized root; direct getter/setter delegates identify its concrete property.

**Inherits:** `System.Object`.

[Scene replication](../components/scene-replication.md) defines authority, target/factory identity, copied policies, callback ordering, concrete codecs, prepared budgets and verification boundaries. No public backend handle or universal object serializer is supplied.

## Example

Use a concrete typed token as shown on [ReplicationProperty of TNode/T](ReplicationProperty.Generic.md). Configure the exact token on SceneReplicationConfig; different participants agree on stable IDs/codecs.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.UInt32 ID { get;  }` | Gets the stable per-configuration wire identity. |
| `public System.Int32 MaxEncodedBytes { get;  }` | Gets the finite maximum encoded value size prepared by a synchronizer. |
| `public System.String NodePath { get;  }` | Gets the relative node path, initially supplied by the application. |

## Property Descriptions

<a id="member-09ad52c5064f"></a>
### ID

`public System.UInt32 ID { get;  }`

Gets the stable per-configuration wire identity.

<a id="member-7d944c9805e9"></a>
### MaxEncodedBytes

`public System.Int32 MaxEncodedBytes { get;  }`

Gets the finite maximum encoded value size prepared by a synchronizer.

<a id="member-2643f92f2e4c"></a>
### NodePath

`public System.String NodePath { get;  }`

Gets the relative node path, initially supplied by the application.

## Verification and limits

[SceneReplicationTests](../../tests/Electron2D.Tests/SceneReplicationTests.cs) verifies native WS/WSS custom/automatic/late/visibility spawning and state, copied config, independent malformed/old/wrong-authority schema bytes, actual batching/modes and 64 warmed active/idle zero-managed-allocation intervals. Setup/templates/snapshots/error/caller-codec/native costs remain separate. File-based scene formats/load/save and editor authoring, foreign/routed traffic and human/rendered acceptance require their own gates.
