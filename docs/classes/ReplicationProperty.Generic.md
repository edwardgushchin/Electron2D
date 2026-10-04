# ReplicationProperty\<TNode, T\>

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.ReplicationProperty<TNode, T>`. **Source:** [ReplicationProperty.cs](../../src/Core/Networking/ReplicationProperty.cs).

## Description

Pairs a concrete Node property with direct typed accessors and an explicit wire codec.



**Inherits:** [ReplicationProperty](ReplicationProperty.md).

[Scene replication](../components/scene-replication.md) defines authority, target/factory identity, copied policies, callback ordering, concrete codecs, prepared budgets and verification boundaries. No public backend handle or universal object serializer is supplied.

## Example

Public API excerpt; SceneReplicationTests executes native WS/WSS peers and pre-Ready, late-join, visibility and state transitions. Offline examples do not establish a network connection or rendered output.

```csharp
var priority = new ReplicationProperty<Node, int>(1,
    static node => node.ProcessPriority, static (node, value) => node.ProcessPriority = value,
    static _ => 4,
    static (value, bytes) => { System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, value); return 4; },
    static bytes => bytes.Length == 4 ? System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes) : throw new System.IO.InvalidDataException(),
    maxEncodedBytes: 4);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public ReplicationProperty(System.UInt32 id, Func<TNode, T> get, Action<TNode, T> set, Func<T, System.Int32> size, RPCEncoder<T> encode, RPCDecoder<T> decode, System.Int32 maxEncodedBytes = 65535, System.String nodePath = ".")` | Projects inherited lifecycle behavior for this concrete type. |

## Constructor Descriptions

<a id="member-503972d2b48c"></a>
### .ctor

`public ReplicationProperty(System.UInt32 id, Func<TNode, T> get, Action<TNode, T> set, Func<T, System.Int32> size, RPCEncoder<T> encode, RPCDecoder<T> decode, System.Int32 maxEncodedBytes = 65535, System.String nodePath = ".")`

Projects inherited lifecycle behavior for this concrete type.

## Verification and limits

[SceneReplicationTests](../../tests/Electron2D.Tests/SceneReplicationTests.cs) verifies native WS/WSS custom/automatic/late/visibility spawning and state, copied config, independent malformed/old/wrong-authority schema bytes, actual batching/modes and 64 warmed active/idle zero-managed-allocation intervals. Setup/templates/snapshots/error/caller-codec/native costs remain separate. File-based scene formats/load/save and editor authoring, foreign/routed traffic and human/rendered acceptance require their own gates.
