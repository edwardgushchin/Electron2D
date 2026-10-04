# RPCEncoder\<T\>

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public delegate Electron2D.RPCEncoder<T>`. **Source:** [RPCMethod.cs](../../src/Core/Networking/RPCMethod.cs).

## Description

Encodes one concrete RPC argument model into borrowed output storage.



**Inherits:** `System.MulticastDelegate`.

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.

## Example

The explicit typed codec example on [RPCMethod](RPCMethod.Generic.md) shows both delegate signatures and a complete public caller.

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public RPCEncoder(System.Object object, System.IntPtr method)` | Projects inherited lifecycle behavior for this concrete type. |

## Constructor Descriptions

<a id="member-6e7dc55fb114"></a>
### .ctor

`public RPCEncoder(System.Object object, System.IntPtr method)`

Projects inherited lifecycle behavior for this concrete type.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public virtual System.IAsyncResult BeginInvoke(T value, System.Span<System.Byte> destination, System.AsyncCallback callback, System.Object object)` | Projects inherited lifecycle behavior for this concrete type. |
| `public virtual System.Int32 EndInvoke(System.IAsyncResult result)` | Projects inherited lifecycle behavior for this concrete type. |
| `public virtual System.Int32 Invoke(T value, System.Span<System.Byte> destination)` | Projects inherited lifecycle behavior for this concrete type. |

## Method Descriptions

<a id="member-469d18cb50a6"></a>
### BeginInvoke

`public virtual System.IAsyncResult BeginInvoke(T value, System.Span<System.Byte> destination, System.AsyncCallback callback, System.Object object)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-d38c6fa3276d"></a>
### EndInvoke

`public virtual System.Int32 EndInvoke(System.IAsyncResult result)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-a0b2437f6db6"></a>
### Invoke

`public virtual System.Int32 Invoke(T value, System.Span<System.Byte> destination)`

Projects inherited lifecycle behavior for this concrete type.

## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.

## Delegate summary

| Complete C# signature | Contract |
| --- | --- |
| `public delegate int RPCEncoder<T>(T value, Span<byte> destination);` | Synchronous concrete typed codec or borrowed packet callback. |

## Delegate Description

`public delegate int RPCEncoder<T>(T value, Span<byte> destination);`

Arguments and spans are borrowed during the call. Encoders return complete bytes written; decoders return concrete values and validate the complete payload. Packet callbacks receive original peer identity and copy data when retaining it. Exceptions propagate through the owning provider and its documented callback fanout.
