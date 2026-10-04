# RPCRegistration

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public struct Electron2D.RPCRegistration`. **Source:** [RPCMethod.cs](../../src/Core/Networking/RPCMethod.cs).

## Description

One immutable configured method and its policy, as returned by Node.GetNodeRPCConfig.



**Inherits:** `System.ValueType`.

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.

## Example

Shared token/configuration use is shown on [RPCMethod](RPCMethod.Generic.md); Node.GetNodeRPCConfig returns immutable RPCRegistration snapshots.

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public RPCRegistration(Electron2D.RPCMethod method, Electron2D.RPCOptions options)` | Creates a typed configuration snapshot. |

## Constructor Descriptions

<a id="member-ba5d3930901f"></a>
### .ctor

`public RPCRegistration(Electron2D.RPCMethod method, Electron2D.RPCOptions options)`

Creates a typed configuration snapshot.

method: Shared immutable method token.

options: Invocation policy.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.RPCMethod Method { get;  }` | Gets the shared immutable method token. |
| `public Electron2D.RPCOptions Options { get;  }` | Gets the copied invocation policy. |

## Property Descriptions

<a id="member-979dae075844"></a>
### Method

`public Electron2D.RPCMethod Method { get;  }`

Gets the shared immutable method token.

<a id="member-4ae0a1f5b431"></a>
### Options

`public Electron2D.RPCOptions Options { get;  }`

Gets the copied invocation policy.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public virtual System.Boolean Equals(Electron2D.RPCRegistration other)` | Projects inherited lifecycle behavior for this concrete type. |
| `public override System.Boolean Equals(System.Object obj)` | Projects inherited lifecycle behavior for this concrete type. |
| `public override System.Int32 GetHashCode()` | Projects inherited lifecycle behavior for this concrete type. |
| `public override System.String ToString()` | Projects inherited lifecycle behavior for this concrete type. |

## Method Descriptions

<a id="member-f67575950435"></a>
### Equals

`public virtual System.Boolean Equals(Electron2D.RPCRegistration other)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-ccdaf317ffcc"></a>
### Equals

`public override System.Boolean Equals(System.Object obj)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-1e8db688a032"></a>
### GetHashCode

`public override System.Int32 GetHashCode()`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-6d02245b2c0a"></a>
### ToString

`public override System.String ToString()`

Projects inherited lifecycle behavior for this concrete type.

## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.
