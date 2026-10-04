# MultiplayerPacketHandler

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public delegate Electron2D.MultiplayerPacketHandler`. **Source:** [RPCMethod.cs](../../src/Core/Networking/RPCMethod.cs).

## Description

Receives borrowed custom/authentication bytes on the multiplayer owner thread.



**Inherits:** `System.MulticastDelegate`.

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.

## Example

```csharp
MultiplayerPacketHandler receiver = static (id, data) => Console.WriteLine($"Peer {id}: {data.Length} bytes");
// Assign AuthCallback or subscribe PeerPacket on an owner-thread SceneMultiplayer.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public MultiplayerPacketHandler(System.Object object, System.IntPtr method)` | Projects inherited lifecycle behavior for this concrete type. |

## Constructor Descriptions

<a id="member-876079d3a46e"></a>
### .ctor

`public MultiplayerPacketHandler(System.Object object, System.IntPtr method)`

Projects inherited lifecycle behavior for this concrete type.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public virtual System.IAsyncResult BeginInvoke(System.Int32 id, System.ReadOnlySpan<System.Byte> data, System.AsyncCallback callback, System.Object object)` | Projects inherited lifecycle behavior for this concrete type. |
| `public virtual System.Void EndInvoke(System.IAsyncResult result)` | Projects inherited lifecycle behavior for this concrete type. |
| `public virtual System.Void Invoke(System.Int32 id, System.ReadOnlySpan<System.Byte> data)` | Projects inherited lifecycle behavior for this concrete type. |

## Method Descriptions

<a id="member-bcf40c6464d2"></a>
### BeginInvoke

`public virtual System.IAsyncResult BeginInvoke(System.Int32 id, System.ReadOnlySpan<System.Byte> data, System.AsyncCallback callback, System.Object object)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-5d4695085529"></a>
### EndInvoke

`public virtual System.Void EndInvoke(System.IAsyncResult result)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-8175f21ee9c7"></a>
### Invoke

`public virtual System.Void Invoke(System.Int32 id, System.ReadOnlySpan<System.Byte> data)`

Projects inherited lifecycle behavior for this concrete type.

## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.

## Delegate summary

| Complete C# signature | Contract |
| --- | --- |
| `public delegate void MultiplayerPacketHandler(int id, ReadOnlySpan<byte> data);` | Synchronous concrete typed codec or borrowed packet callback. |

## Delegate Description

`public delegate void MultiplayerPacketHandler(int id, ReadOnlySpan<byte> data);`

Arguments and spans are borrowed during the call. Encoders return complete bytes written; decoders return concrete values and validate the complete payload. Packet callbacks receive original peer identity and copy data when retaining it. Exceptions propagate through the owning provider and its documented callback fanout.
