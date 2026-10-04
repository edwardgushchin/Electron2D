# RPCOptions

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public struct Electron2D.RPCOptions`. **Source:** [RPCMethod.cs](../../src/Core/Networking/RPCMethod.cs).

## Description

Immutable RPC policy; an unconfigured method is disabled.



**Inherits:** `System.ValueType`.

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.

## Example

Public API excerpt. The server example requires port 8080 available; native tests execute the full two-client WS/WSS scene workflow.

```csharp
var policy = new RPCOptions(RPCMode.AnyPeer, callLocal: true, transferMode: TransferMode.Reliable, channel: 0);
// default(RPCOptions) is Disabled; new RPCOptions() selects Authority/Reliable.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public RPCOptions()` | Creates an authority-only remote-only reliable policy. A zero-initialized default value remains disabled. |
| `public RPCOptions(Electron2D.RPCMode mode = Authority, System.Boolean callLocal = false, Electron2D.TransferMode transferMode = Reliable, System.Int32 channel = 0)` | Creates an explicit invocation policy. |

## Constructor Descriptions

<a id="member-55258b6d7f5c"></a>
### .ctor

`public RPCOptions()`

Creates an authority-only remote-only reliable policy. A zero-initialized default value remains disabled.

<a id="member-fa180d84d105"></a>
### .ctor

`public RPCOptions(Electron2D.RPCMode mode = Authority, System.Boolean callLocal = false, Electron2D.TransferMode transferMode = Reliable, System.Int32 channel = 0)`

Creates an explicit invocation policy.

mode: Admission policy.

callLocal: Also invokes locally when the target includes this peer.

transferMode: Requested transport mode.

channel: Nonnegative outgoing channel.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean CallLocal { get;  }` | Gets whether targets including the local peer invoke locally after sending. |
| `public System.Int32 Channel { get;  }` | Gets requested nonnegative outgoing channel. |
| `public Electron2D.RPCMode Mode { get;  }` | Gets remote invocation admission policy. |
| `public Electron2D.TransferMode TransferMode { get;  }` | Gets requested outgoing delivery mode. |

## Property Descriptions

<a id="member-792ffebc40e3"></a>
### CallLocal

`public System.Boolean CallLocal { get;  }`

Gets whether targets including the local peer invoke locally after sending.

<a id="member-b67c8ebe6dbe"></a>
### Channel

`public System.Int32 Channel { get;  }`

Gets requested nonnegative outgoing channel.

<a id="member-59a9b879dc1b"></a>
### Mode

`public Electron2D.RPCMode Mode { get;  }`

Gets remote invocation admission policy.

<a id="member-d5b26ece5a1b"></a>
### TransferMode

`public Electron2D.TransferMode TransferMode { get;  }`

Gets requested outgoing delivery mode.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public virtual System.Boolean Equals(Electron2D.RPCOptions other)` | Projects inherited lifecycle behavior for this concrete type. |
| `public override System.Boolean Equals(System.Object obj)` | Projects inherited lifecycle behavior for this concrete type. |
| `public override System.Int32 GetHashCode()` | Projects inherited lifecycle behavior for this concrete type. |
| `public override System.String ToString()` | Projects inherited lifecycle behavior for this concrete type. |

## Method Descriptions

<a id="member-d270ec389e0f"></a>
### Equals

`public virtual System.Boolean Equals(Electron2D.RPCOptions other)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-31a664d6e0ad"></a>
### Equals

`public override System.Boolean Equals(System.Object obj)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-39ff8f93694c"></a>
### GetHashCode

`public override System.Int32 GetHashCode()`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-f8db8ca003c6"></a>
### ToString

`public override System.String ToString()`

Projects inherited lifecycle behavior for this concrete type.

## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.
