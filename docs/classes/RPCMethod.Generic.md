# RPCMethod\<TNode, T\>

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RPCMethod<TNode, T>`. **Source:** [RPCMethod.cs](../../src/Core/Networking/RPCMethod.cs).

## Description

Pairs a strongly typed Node receiver and argument model with an explicit wire codec.



**Inherits:** [RPCMethod](RPCMethod.md).

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.

## Example

Public API excerpt. The server example requires port 8080 available; native tests execute the full two-client WS/WSS scene workflow.

```csharp
var method = new RPCMethod<Node, int>(1,
    static (node, value) => Console.WriteLine(value),
    static _ => 4,
    static (value, bytes) => { System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, value); return 4; },
    static bytes => bytes.Length == 4 ? System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes) : throw new System.IO.InvalidDataException());
using var tree = new SceneTree(new Node { Name = "Root" });
tree.Root.RPCConfig(method, new RPCOptions(RPCMode.AnyPeer, callLocal: true));
tree.Root.RPC(method, 42); // Offline direct local call with sender one.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public RPCMethod(System.UInt32 id, Action<TNode, T> method, Func<T, System.Int32> size, RPCEncoder<T> encode, RPCDecoder<T> decode)` | Projects inherited lifecycle behavior for this concrete type. |

## Constructor Descriptions

<a id="member-1910eb1fda41"></a>
### .ctor

`public RPCMethod(System.UInt32 id, Action<TNode, T> method, Func<T, System.Int32> size, RPCEncoder<T> encode, RPCDecoder<T> decode)`

Projects inherited lifecycle behavior for this concrete type.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public T Decode(System.ReadOnlySpan<System.Byte> source)` | Decodes complete borrowed arguments for a custom provider. |
| `public System.Void Encode(T value, System.Span<System.Byte> destination)` | Writes a complete codec payload into exactly sized borrowed storage. |
| `public System.Int32 GetEncodedSize(T value)` | Measures complete codec output. |
| `public System.Void Invoke(TNode node, T arguments)` | Invokes the direct typed callback without encoding. |

## Method Descriptions

<a id="member-8e9cad962305"></a>
### Decode

`public T Decode(System.ReadOnlySpan<System.Byte> source)`

Decodes complete borrowed arguments for a custom provider.

source: Complete codec payload.

Returns: The concrete decoded value.

<a id="member-a90d85c2eadd"></a>
### Encode

`public System.Void Encode(T value, System.Span<System.Byte> destination)`

Writes a complete codec payload into exactly sized borrowed storage.

value: Concrete arguments.

destination: Exactly GetEncodedSize bytes.

<a id="member-ebb6b49d45de"></a>
### GetEncodedSize

`public System.Int32 GetEncodedSize(T value)`

Measures complete codec output.

value: Concrete arguments.

Returns: A nonnegative encoded size.

<a id="member-22adf9cf7e7e"></a>
### Invoke

`public System.Void Invoke(TNode node, T arguments)`

Invokes the direct typed callback without encoding.

node: Compatible live receiver.

arguments: Concrete argument model.

Remarks: The provider owns remote admission and sender scope.

## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.
