# MultiplayerSpawner

Last updated: 2026-10-05

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.MultiplayerSpawner`. **Source:** [MultiplayerSpawner.cs](../../src/Scene/Multiplayer/MultiplayerSpawner.cs).

## Description

Replicates authored PackedScene children or explicit typed custom factories from its authority.

Registered scenes/factory tokens are borrowed. Spawn construction and membership are cold work. Remote ownership belongs to the configured parent; Spawned/Despawned run only on remote participants.

**Inherits:** [Node](Node.md).

[Scene replication](../components/scene-replication.md) defines authority, target/factory identity, copied policies, callback ordering, concrete codecs, prepared budgets and verification boundaries. No public backend handle or universal object serializer is supplied.

## Example

Public API excerpt; SceneReplicationTests executes native WS/WSS peers and pre-Ready, late-join, visibility and state transitions. Offline examples do not establish a network connection or rendered output.

```csharp
var root = new Node { Name = "Root" };
root.AddChild(new Node { Name = "Actors" });
var spawner = new MultiplayerSpawner
{
    Name = "Spawner", SpawnPath = "../Actors",
    SpawnFunction = new SpawnFactory(1, static () => new Node { Name = "Actor" }),
};
root.AddChild(spawner);
using var tree = new SceneTree(root);
Node instance = spawner.Spawn(); // Local offline authority; the parent now owns instance.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public MultiplayerSpawner()` | Creates an unconfigured spawner. |

## Constructor Descriptions

<a id="member-95423fe74821"></a>
### .ctor

`public MultiplayerSpawner()`

Creates an unconfigured spawner.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.SpawnFactory SpawnFunction { get; set; }` | Gets or sets immutable borrowed custom factory/codec configuration. |
| `public System.UInt32 SpawnLimit { get; set; }` | Gets or sets the maximum tracked node count. |
| `public System.String SpawnPath { get; set; }` | Gets or sets the path to the direct spawn parent. |

## Property Descriptions

<a id="member-458690c517ec"></a>
### SpawnFunction

`public Electron2D.SpawnFactory SpawnFunction { get; set; }`

Gets or sets immutable borrowed custom factory/codec configuration.

Value: Null initially.

<a id="member-42b669293b4a"></a>
### SpawnLimit

`public System.UInt32 SpawnLimit { get; set; }`

Gets or sets the maximum tracked node count.

Value: Zero initially means unlimited; lowering it leaves existing nodes alive.

<a id="member-1f1f35afe7ff"></a>
### SpawnPath

`public System.String SpawnPath { get; set; }`

Gets or sets the path to the direct spawn parent.

Value: Empty initially, disabling spawning.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddSpawnableScene(Electron2D.PackedScene scene)` | Registers a borrowed in-memory PackedScene template for automatic direct-child replication. |
| `public System.Void ClearSpawnableScenes()` | Clears template registration without despawning existing nodes. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public override System.String[] GetConfigurationWarnings()` | Returns this node's current configuration warnings for tooling. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public Electron2D.PackedScene GetSpawnableScene(System.Int32 index)` | Gets an ordered borrowed scene template. |
| `public System.Int32 GetSpawnableSceneCount()` | Gets the registered template count. |
| `protected override System.Void OnEnterTree()` | Called synchronously when this node enters an active scene tree. |
| `protected override System.Void OnExitTree()` | Called synchronously when this node exits an active scene tree. |
| `public Electron2D.Node Spawn()` | Creates a custom node using default typed arguments, then adds it beneath SpawnPath. |
| `public Electron2D.Node Spawn<T>(T data)` | Creates a custom node with a concrete typed argument model. |

## Method Descriptions

<a id="member-a1c279e1f189"></a>
### AddSpawnableScene

`public System.Void AddSpawnableScene(Electron2D.PackedScene scene)`

Registers a borrowed in-memory PackedScene template for automatic direct-child replication.

scene: Template; ordered indices must match across peers.

Remarks: Instantiate this same template and add its root beneath SpawnPath for automatic local spawning. File scene loading remains the resource loader's separate contract.

<a id="member-55857edec659"></a>
### ClearSpawnableScenes

`public System.Void ClearSpawnableScenes()`

Clears template registration without despawning existing nodes.

<a id="member-e1e4c362fee8"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

Returns: A non-null factory that creates a fresh node of the exact same runtime type.

Remarks: The base implementation supports only an exact Electron2D.Node. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

System.NotSupportedException: A derived node has not explicitly supplied an instancing factory.

<a id="member-8c4755f287d7"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

Remarks: Cancels queued deletion, detaches this node, recursively disposes every owned child, clears groups and event subscribers, and then calls the base implementation. Every teardown stage is attempted before failures are reported together.

<a id="member-a51e07e1d726"></a>
### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Returns this node's current configuration warnings for tooling.

Returns: An empty array by default. Overrides return ordered warning messages and should include base warnings.

Remarks: This query does not cache results, emit events or require an edited scene. Attached queries run on the scene owner thread. Consumers may call it after NodeConfigurationWarningChanged to refresh their display.

System.InvalidOperationException: An attached query runs off the scene owner thread.

System.ObjectDisposedException: This node is disposed.

<a id="member-8c829c7e9807"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Appends this class's typed hierarchy, ownership, processing, and automatic translation descriptors to the inherited descriptors.

<a id="member-689cd0edbe78"></a>
### GetSpawnableScene

`public Electron2D.PackedScene GetSpawnableScene(System.Int32 index)`

Gets an ordered borrowed scene template.

index: Registered position.

Returns: The original resource.

<a id="member-986faef2da93"></a>
### GetSpawnableSceneCount

`public System.Int32 GetSpawnableSceneCount()`

Gets the registered template count.

Returns: Zero through 255.

<a id="member-a1bada4ba9de"></a>
### OnEnterTree

`protected override System.Void OnEnterTree()`

Called synchronously when this node enters an active scene tree.

Remarks: Electron2D.Node.Tree is already assigned. The callback runs parent-first, before Electron2D.Node.TreeEntered, before descendants enter, and on the tree owner thread during SceneTree-managed lifecycle.

<a id="member-cb06701175b9"></a>
### OnExitTree

`protected override System.Void OnExitTree()`

Called synchronously when this node exits an active scene tree.

Remarks: Descendants have already exited and Electron2D.Node.Tree remains assigned. The callback precedes Electron2D.Node.TreeExiting and runs on the tree owner thread during SceneTree-managed lifecycle.

<a id="member-4f13a602adec"></a>
### Spawn

`public Electron2D.Node Spawn()`

Creates a custom node using default typed arguments, then adds it beneath SpawnPath.

Returns: The locally parent-owned node.

<a id="member-e55025fd839e"></a>
### Spawn

`public Electron2D.Node Spawn<T>(T data)`

Creates a custom node with a concrete typed argument model.

T: Factory argument type.

data: Arguments copied through the configured codec.

Returns: The locally parent-owned node.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<Electron2D.Node> Despawned` | Occurs when a remote instance is removed by its authority. |
| `public event System.Action<Electron2D.Node> Spawned` | Occurs after a remote node has entered and readied. |

## Event Descriptions

<a id="member-2a78ac79b35a"></a>
### Despawned

`public event System.Action<Electron2D.Node> Despawned`

Occurs when a remote instance is removed by its authority.

<a id="member-158afdcfe571"></a>
### Spawned

`public event System.Action<Electron2D.Node> Spawned`

Occurs after a remote node has entered and readied.

## Verification and limits

[SceneReplicationTests](../../tests/Electron2D.Tests/SceneReplicationTests.cs) verifies native WS/WSS custom/automatic/late/visibility spawning and state, copied config, independent malformed/old/wrong-authority schema bytes, actual batching/modes and 64 warmed active/idle zero-managed-allocation intervals. Setup/templates/snapshots/error/caller-codec/native costs remain separate. File-based scene formats/load/save and editor authoring, foreign/routed traffic and human/rendered acceptance require their own gates.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

## File integration API additions

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddSpawnableScene(System.String path)` | Loads and registers a file-backed typed PackedScene for automatic replication. |
| `public System.String GetSpawnableScenePath(System.Int32 index)` | Returns the portable source path of an authored scene. |

## Method Descriptions

<a id="member-b724821cb71a"></a>
### AddSpawnableScene

`public System.Void AddSpawnableScene(System.String path)`

Loads and registers a file-backed typed PackedScene for automatic replication.

Cached templates remain borrowed. This spawner owns newly loaded templates until clear/disposal; live instances retain their file graph leases.

- `path`: Resource scene path or UID.

<a id="member-e3aa8bd01660"></a>
### GetSpawnableScenePath

`public System.String GetSpawnableScenePath(System.Int32 index)`

Returns the portable source path of an authored scene.

Returns: Original registration path or UID; ResourcePath for an in-memory template.

- `index`: Ordered scene index.
