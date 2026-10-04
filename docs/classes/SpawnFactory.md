# SpawnFactory

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.SpawnFactory`. **Source:** [SpawnFactory.cs](../../src/Core/Networking/SpawnFactory.cs).

## Description

Creates a detached node from a copied explicit spawn argument protocol.

Immutable factory/codec configuration is borrowed by spawners. IDs/codecs must match on participants. A factory must return a fresh live unparented node; ownership transfers to the spawn parent after insertion.

**Inherits:** `System.Object`.

[Scene replication](../components/scene-replication.md) defines authority, target/factory identity, copied policies, callback ordering, concrete codecs, prepared budgets and verification boundaries. No public backend handle or universal object serializer is supplied.

## Example

Public API excerpt; SceneReplicationTests executes native WS/WSS peers and pre-Ready, late-join, visibility and state transitions. Offline examples do not establish a network connection or rendered output.

```csharp
var factory = new SpawnFactory(1, static () => new Node { Name = "Actor" });
using Node detached = factory.Instantiate([]);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public SpawnFactory(System.UInt32 id, System.Func<Electron2D.Node> factory)` | Creates a parameterless custom spawn factory. |

## Constructor Descriptions

<a id="member-85fc0266ede9"></a>
### .ctor

`public SpawnFactory(System.UInt32 id, System.Func<Electron2D.Node> factory)`

Creates a parameterless custom spawn factory.

id: Nonzero protocol identity.

factory: Fresh detached node constructor.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.UInt32 ID { get;  }` | Gets the immutable protocol identity. |

## Property Descriptions

<a id="member-8c85bb07b419"></a>
### ID

`public System.UInt32 ID { get;  }`

Gets the immutable protocol identity.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.Node Instantiate(System.ReadOnlySpan<System.Byte> arguments)` | Creates and validates a new node from complete encoded arguments. |

## Method Descriptions

<a id="member-7a2dba43cbb9"></a>
### Instantiate

`public Electron2D.Node Instantiate(System.ReadOnlySpan<System.Byte> arguments)`

Creates and validates a new node from complete encoded arguments.

arguments: Copied/borrowed complete payload.

Returns: A fresh unparented live node.

## Verification and limits

[SceneReplicationTests](../../tests/Electron2D.Tests/SceneReplicationTests.cs) verifies native WS/WSS custom/automatic/late/visibility spawning and state, copied config, independent malformed/old/wrong-authority schema bytes, actual batching/modes and 64 warmed active/idle zero-managed-allocation intervals. Setup/templates/snapshots/error/caller-codec/native costs remain separate. File-based scene formats/load/save and editor authoring, foreign/routed traffic and human/rendered acceptance require their own gates.
