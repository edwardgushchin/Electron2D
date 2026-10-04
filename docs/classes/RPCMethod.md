# RPCMethod

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.RPCMethod`. **Source:** [RPCMethod.cs](../../src/Core/Networking/RPCMethod.cs).

## Description

Identifies a concrete typed RPC method without reflection or string method dispatch.

Wire IDs must be unique per node and agreed by all participants. The application explicitly supplies its typed codec. Tokens are immutable shared configuration; they own no node/transport lifetime.

**Inherits:** `System.Object`.

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.

## Example

Shared token/configuration use is shown on [RPCMethod](RPCMethod.Generic.md); Node.GetNodeRPCConfig returns immutable RPCRegistration snapshots.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.UInt32 ID { get;  }` | Gets the stable application wire identity. |

## Property Descriptions

<a id="member-c50a8c8f9aec"></a>
### ID

`public System.UInt32 ID { get;  }`

Gets the stable application wire identity.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void InvokeEncoded(Electron2D.Node node, System.ReadOnlySpan<System.Byte> arguments)` | Invokes this token from a complete encoded argument payload. |

## Method Descriptions

<a id="member-87ea18e87fc8"></a>
### InvokeEncoded

`public System.Void InvokeEncoded(Electron2D.Node node, System.ReadOnlySpan<System.Byte> arguments)`

Invokes this token from a complete encoded argument payload.

node: Compatible live Node receiver.

arguments: Borrowed codec input.

Remarks: Custom MultiplayerAPI implementations enforce authority/admission and sender scope before calling this hook.

## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.
