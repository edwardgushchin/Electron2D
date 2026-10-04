# SpawnFactory\<T\>

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.SpawnFactory<T>`. **Source:** [SpawnFactory.cs](../../src/Core/Networking/SpawnFactory.cs).

## Description

Creates custom nodes using an explicit concrete typed argument model and wire codec.



**Inherits:** [SpawnFactory](SpawnFactory.md).

[Scene replication](../components/scene-replication.md) defines authority, target/factory identity, copied policies, callback ordering, concrete codecs, prepared budgets and verification boundaries. No public backend handle or universal object serializer is supplied.

## Example

Public API excerpt; SceneReplicationTests executes native WS/WSS peers and pre-Ready, late-join, visibility and state transitions. Offline examples do not establish a network connection or rendered output.

```csharp
var factory = new SpawnFactory<int>(1, static value => new Node { Name = "Actor" + value },
    static _ => 4,
    static (value, bytes) => { System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, value); return 4; },
    static bytes => bytes.Length == 4 ? System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes) : throw new System.IO.InvalidDataException());
using Node detached = factory.Instantiate(factory.Encode(3));
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public SpawnFactory(System.UInt32 id, Func<T, Electron2D.Node> factory, Func<T, System.Int32> size, RPCEncoder<T> encode, RPCDecoder<T> decode)` | Projects inherited lifecycle behavior for this concrete type. |

## Constructor Descriptions

<a id="member-daccfb087c01"></a>
### .ctor

`public SpawnFactory(System.UInt32 id, Func<T, Electron2D.Node> factory, Func<T, System.Int32> size, RPCEncoder<T> encode, RPCDecoder<T> decode)`

Projects inherited lifecycle behavior for this concrete type.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Byte[] Encode(T data)` | Copies arguments into their complete wire representation. |

## Method Descriptions

<a id="member-28996092771a"></a>
### Encode

`public System.Byte[] Encode(T data)`

Copies arguments into their complete wire representation.

data: Concrete arguments.

Returns: Copied payload up to 64 MiB, subject to the consuming transport budget.

## Verification and limits

[SceneReplicationTests](../../tests/Electron2D.Tests/SceneReplicationTests.cs) verifies native WS/WSS custom/automatic/late/visibility spawning and state, copied config, independent malformed/old/wrong-authority schema bytes, actual batching/modes and 64 warmed active/idle zero-managed-allocation intervals. Setup/templates/snapshots/error/caller-codec/native costs remain separate. File-based scene formats/load/save and editor authoring, foreign/routed traffic and human/rendered acceptance require their own gates.
