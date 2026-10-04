# MultiplayerSynchronizer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.MultiplayerSynchronizer`. **Source:** [MultiplayerSynchronizer.cs](../../src/Scene/Multiplayer/MultiplayerSynchronizer.cs).

## Description

Replicates explicitly typed properties from its authority and controls peer visibility.

Root/configuration/factories are authored through the public scene API. Codecs and property tokens remain borrowed. Interval values are monotonic seconds. Lifecycle/configuration prepares bounded state; stable capture/encode/decode/poll reuses it. Attached operations require the scene owner.

**Inherits:** [Node](Node.md).

[Scene replication](../components/scene-replication.md) defines authority, target/factory identity, copied policies, callback ordering, concrete codecs, prepared budgets and verification boundaries. No public backend handle or universal object serializer is supplied.

## Example

Public API excerpt; SceneReplicationTests executes native WS/WSS peers and pre-Ready, late-join, visibility and state transitions. Offline examples do not establish a network connection or rendered output.

```csharp
var priority = new ReplicationProperty<Node, int>(1,
    static node => node.ProcessPriority, static (node, value) => node.ProcessPriority = value,
    static _ => 4,
    static (value, bytes) => { System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, value); return 4; },
    static bytes => bytes.Length == 4 ? System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes) : throw new System.IO.InvalidDataException(),
    maxEncodedBytes: 4);
using var config = new SceneReplicationConfig();
config.AddProperty(priority);
var root = new Node { Name = "Root" };
root.AddChild(new MultiplayerSynchronizer { Name = "Sync", ReplicationConfig = config });
using var tree = new SceneTree(root);
tree.ProcessFrame(0); // Offline authoring; supply a connected peer for network replication.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public MultiplayerSynchronizer()` | Creates a parent-root synchronizer with public visibility and zero intervals. |

## Constructor Descriptions

<a id="member-1215b981e4b4"></a>
### .ctor

`public MultiplayerSynchronizer()`

Creates a parent-root synchronizer with public visibility and zero intervals.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Double DeltaInterval { get; set; }` | Gets or sets the minimum interval between OnChange updates per peer in seconds. |
| `public System.Boolean PublicVisibility { get; set; }` | Gets or sets whether every peer is visible in addition to explicit peer entries. |
| `public Electron2D.SceneReplicationConfig ReplicationConfig { get; set; }` | Gets or sets the borrowed ordered property configuration. |
| `public System.Double ReplicationInterval { get; set; }` | Gets or sets the interval between Always updates in seconds. |
| `public System.String RootPath { get; set; }` | Gets or sets the relative target root path. |
| `public Electron2D.VisibilityUpdateMode VisibilityUpdateMode { get; set; }` | Gets or sets the phase for automatic visibility filter refresh. |

## Property Descriptions

<a id="member-fa4fb911aae8"></a>
### DeltaInterval

`public System.Double DeltaInterval { get; set; }`

Gets or sets the minimum interval between OnChange updates per peer in seconds.

Value: Zero initially; zero checks each network process frame.

<a id="member-c98e9f81487b"></a>
### PublicVisibility

`public System.Boolean PublicVisibility { get; set; }`

Gets or sets whether every peer is visible in addition to explicit peer entries.

Value: True initially. False allows explicit positive peer visibility.

<a id="member-a91119936e58"></a>
### ReplicationConfig

`public Electron2D.SceneReplicationConfig ReplicationConfig { get; set; }`

Gets or sets the borrowed ordered property configuration.

Value: Null initially, providing no property updates.

<a id="member-b10f0d76adc1"></a>
### ReplicationInterval

`public System.Double ReplicationInterval { get; set; }`

Gets or sets the interval between Always updates in seconds.

Value: Zero initially; zero sends each network process frame.

<a id="member-eb841b5ec597"></a>
### RootPath

`public System.String RootPath { get; set; }`

Gets or sets the relative target root path.

Value: Parent path initially; empty disables synchronization.

<a id="member-f9b26d5cf0f1"></a>
### VisibilityUpdateMode

`public Electron2D.VisibilityUpdateMode VisibilityUpdateMode { get; set; }`

Gets or sets the phase for automatic visibility filter refresh.

Value: Idle initially; processing is enabled only when filters and a valid root exist.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddVisibilityFilter(System.Func<System.Int32, System.Boolean> filter)` | Adds a unique conjunctive peer predicate. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public override System.String[] GetConfigurationWarnings()` | Returns this node's current configuration warnings for tooling. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public System.Boolean GetVisibilityFor(System.Int32 peer)` | Reports explicit membership, independently of public visibility and filters. |
| `protected override System.Void OnEnterTree()` | Called synchronously when this node enters an active scene tree. |
| `protected override System.Void OnExitTree()` | Called synchronously when this node exits an active scene tree. |
| `protected override System.Void OnNotification(System.Int32 what)` | Handles an engine notification delivered to this object. |
| `public System.Void RemoveVisibilityFilter(System.Func<System.Int32, System.Boolean> filter)` | Removes a visibility predicate if present. |
| `public override System.Void SetMultiplayerAuthority(System.Int32 id, System.Boolean recursive = true)` | Sets a positive authority identity, optionally recursively for current descendants. |
| `public System.Void SetVisibilityFor(System.Int32 peer, System.Boolean visible)` | Changes explicit peer/public membership and reevaluates visibility. |
| `public System.Void UpdateVisibility(System.Int32 forPeer = 0)` | Reevaluates one or all peers and delivers the visibility event. |

## Method Descriptions

<a id="member-733f4e049cc7"></a>
### AddVisibilityFilter

`public System.Void AddVisibilityFilter(System.Func<System.Int32, System.Boolean> filter)`

Adds a unique conjunctive peer predicate.

filter: Direct peer-ID predicate, called on the owner thread.

<a id="member-f6795661a625"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

Returns: A non-null factory that creates a fresh node of the exact same runtime type.

Remarks: The base implementation supports only an exact Electron2D.Node. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

System.NotSupportedException: A derived node has not explicitly supplied an instancing factory.

<a id="member-ad6d6c017891"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

Remarks: Cancels queued deletion, detaches this node, recursively disposes every owned child, clears groups and event subscribers, and then calls the base implementation. Every teardown stage is attempted before failures are reported together.

<a id="member-ae947b5b21b6"></a>
### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Returns this node's current configuration warnings for tooling.

Returns: An empty array by default. Overrides return ordered warning messages and should include base warnings.

Remarks: This query does not cache results, emit events or require an edited scene. Attached queries run on the scene owner thread. Consumers may call it after NodeConfigurationWarningChanged to refresh their display.

System.InvalidOperationException: An attached query runs off the scene owner thread.

System.ObjectDisposedException: This node is disposed.

<a id="member-cba0c1670148"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Appends this class's typed hierarchy, ownership, processing, and automatic translation descriptors to the inherited descriptors.

<a id="member-65ac8df2a680"></a>
### GetVisibilityFor

`public System.Boolean GetVisibilityFor(System.Int32 peer)`

Reports explicit membership, independently of public visibility and filters.

peer: Zero selects public membership, otherwise a positive peer.

Returns: True if the explicit entry exists.

<a id="member-ea4542e496ef"></a>
### OnEnterTree

`protected override System.Void OnEnterTree()`

Called synchronously when this node enters an active scene tree.

Remarks: Electron2D.Node.Tree is already assigned. The callback runs parent-first, before Electron2D.Node.TreeEntered, before descendants enter, and on the tree owner thread during SceneTree-managed lifecycle.

<a id="member-df114821a192"></a>
### OnExitTree

`protected override System.Void OnExitTree()`

Called synchronously when this node exits an active scene tree.

Remarks: Descendants have already exited and Electron2D.Node.Tree remains assigned. The callback precedes Electron2D.Node.TreeExiting and runs on the tree owner thread during SceneTree-managed lifecycle.

<a id="member-ac2b93ca0b50"></a>
### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`

Handles an engine notification delivered to this object.

what: The notification identifier.

Remarks: Calls the base implementation, then maps enter, exit, ready, process, and physics-process notification IDs to the corresponding typed virtual callbacks. Pause and application-suspend notifications reset eligible physics presentation history. Manual Electron2D.ElectronObject.Notify(System.Int32) calls invoke callbacks but do not mutate tree membership, ready state, or delta values.

<a id="member-8afc45a172b4"></a>
### RemoveVisibilityFilter

`public System.Void RemoveVisibilityFilter(System.Func<System.Int32, System.Boolean> filter)`

Removes a visibility predicate if present.

filter: Previously added predicate.

<a id="member-cd4f69bee67d"></a>
### SetMultiplayerAuthority

`public override System.Void SetMultiplayerAuthority(System.Int32 id, System.Boolean recursive = true)`

Sets a positive authority identity, optionally recursively for current descendants.

id: Positive peer identity.

recursive: True applies to current descendants; later children retain their own default/configuration.

Remarks: This local configuration does not replicate itself; participants must agree separately.

<a id="member-e26e3f7f18c2"></a>
### SetVisibilityFor

`public System.Void SetVisibilityFor(System.Int32 peer, System.Boolean visible)`

Changes explicit peer/public membership and reevaluates visibility.

peer: Zero means public.

visible: New explicit membership.

<a id="member-e9950b82f893"></a>
### UpdateVisibility

`public System.Void UpdateVisibility(System.Int32 forPeer = 0)`

Reevaluates one or all peers and delivers the visibility event.

forPeer: Zero reevaluates all admitted peers.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action DeltaSynchronized` | Occurs after changed property state has been applied on a receiving peer. |
| `public event System.Action Synchronized` | Occurs after an Always state has been applied on a receiving peer. |
| `public event System.Action<System.Int32> VisibilityChanged` | Occurs after explicit or automatic visibility reevaluation. |

## Event Descriptions

<a id="member-4d4b597f87a6"></a>
### DeltaSynchronized

`public event System.Action DeltaSynchronized`

Occurs after changed property state has been applied on a receiving peer.

<a id="member-0954950324e6"></a>
### Synchronized

`public event System.Action Synchronized`

Occurs after an Always state has been applied on a receiving peer.

<a id="member-c166aa4e2869"></a>
### VisibilityChanged

`public event System.Action<System.Int32> VisibilityChanged`

Occurs after explicit or automatic visibility reevaluation.

## Verification and limits

[SceneReplicationTests](../../tests/Electron2D.Tests/SceneReplicationTests.cs) verifies native WS/WSS custom/automatic/late/visibility spawning and state, copied config, independent malformed/old/wrong-authority schema bytes, actual batching/modes and 64 warmed active/idle zero-managed-allocation intervals. Setup/templates/snapshots/error/caller-codec/native costs remain separate. File-based scene formats/load/save and editor authoring, foreign/routed traffic and human/rendered acceptance require their own gates.
