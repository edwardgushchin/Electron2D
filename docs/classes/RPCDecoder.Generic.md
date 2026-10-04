# RPCDecoder\<T\>

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public delegate Electron2D.RPCDecoder<T>`. **Source:** [RPCMethod.cs](../../src/Core/Networking/RPCMethod.cs).

## Description

Decodes and validates a complete borrowed RPC argument payload.



**Inherits:** `System.MulticastDelegate`.

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.

## Example

The explicit typed codec example on [RPCMethod](RPCMethod.Generic.md) shows both delegate signatures and a complete public caller.

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public RPCDecoder(System.Object object, System.IntPtr method)` | Projects inherited lifecycle behavior for this concrete type. |

## Constructor Descriptions

<a id="member-b566bb0e1351"></a>
### .ctor

`public RPCDecoder(System.Object object, System.IntPtr method)`

Projects inherited lifecycle behavior for this concrete type.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public virtual System.IAsyncResult BeginInvoke(System.ReadOnlySpan<System.Byte> source, System.AsyncCallback callback, System.Object object)` | Projects inherited lifecycle behavior for this concrete type. |
| `public virtual T EndInvoke(System.IAsyncResult result)` | Projects inherited lifecycle behavior for this concrete type. |
| `public virtual T Invoke(System.ReadOnlySpan<System.Byte> source)` | Projects inherited lifecycle behavior for this concrete type. |

## Method Descriptions

<a id="member-7ca858b45ac8"></a>
### BeginInvoke

`public virtual System.IAsyncResult BeginInvoke(System.ReadOnlySpan<System.Byte> source, System.AsyncCallback callback, System.Object object)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-8103434b535a"></a>
### EndInvoke

`public virtual T EndInvoke(System.IAsyncResult result)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-dea966c50179"></a>
### Invoke

`public virtual T Invoke(System.ReadOnlySpan<System.Byte> source)`

Projects inherited lifecycle behavior for this concrete type.

## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.

## Delegate summary

| Complete C# signature | Contract |
| --- | --- |
| `public delegate T RPCDecoder<T>(ReadOnlySpan<byte> source);` | Synchronous concrete typed codec or borrowed packet callback. |

## Delegate Description

`public delegate T RPCDecoder<T>(ReadOnlySpan<byte> source);`

Arguments and spans are borrowed during the call. Encoders return complete bytes written; decoders return concrete values and validate the complete payload. Packet callbacks receive original peer identity and copy data when retaining it. Exceptions propagate through the owning provider and its documented callback fanout.
