# SceneReplicationConfig

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.SceneReplicationConfig`. **Source:** [SceneReplicationConfig.cs](../../src/Scene/Resources/SceneReplicationConfig.cs).

## Description

Stores an ordered, copied list of immutable typed property descriptors and replication policies.

New properties spawn and replicate Always. Tokens/codecs remain shared immutable application configuration. List/policy changes emit Changed after commit. Preparation/copy allocates; attached consumers reprepare on change.

**Inherits:** [Resource](Resource.md).

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
config.PropertySetReplicationMode(priority, ReplicationMode.OnChange);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public SceneReplicationConfig()` | Creates an empty property configuration. |

## Constructor Descriptions

<a id="member-cbd33fdf4ee2"></a>
### .ctor

`public SceneReplicationConfig()`

Creates an empty property configuration.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddProperty(Electron2D.ReplicationProperty property, System.Int32 index = -1)` | Adds a unique descriptor at an index or appends for any negative index. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public Electron2D.ReplicationProperty[] GetProperties()` | Returns a copied ordered descriptor snapshot. |
| `public System.Boolean HasProperty(Electron2D.ReplicationProperty property)` | Tests descriptor membership by immutable token identity. |
| `protected override System.Void OnResetState()` | Clears non-stored state when Electron2D.Resource.ResetState or Electron2D.Resource.CopyFromResource(Electron2D.Resource) requests it. |
| `public System.Int32 PropertyGetIndex(Electron2D.ReplicationProperty property)` | Finds descriptor order. |
| `public Electron2D.ReplicationMode PropertyGetReplicationMode(Electron2D.ReplicationProperty property)` | Gets ongoing delivery policy. |
| `public System.Boolean PropertyGetSpawn(Electron2D.ReplicationProperty property)` | Reports whether the property is included in initial spawn state. |
| `public System.Boolean PropertyGetSync(Electron2D.ReplicationProperty property)` | Reports whether ongoing mode is Always. |
| `public System.Boolean PropertyGetWatch(Electron2D.ReplicationProperty property)` | Reports whether ongoing mode is OnChange. |
| `public System.Void PropertySetReplicationMode(Electron2D.ReplicationProperty property, Electron2D.ReplicationMode mode)` | Sets Never, Always or OnChange. |
| `public System.Void PropertySetSpawn(Electron2D.ReplicationProperty property, System.Boolean enabled)` | Sets initial-spawn inclusion after validating membership. |
| `public System.Void PropertySetSync(Electron2D.ReplicationProperty property, System.Boolean enabled)` | Enables Always, or disables it if currently Always. |
| `public System.Void PropertySetWatch(Electron2D.ReplicationProperty property, System.Boolean enabled)` | Enables OnChange, or disables it if currently OnChange. |
| `public System.Void RemoveProperty(Electron2D.ReplicationProperty property)` | Removes a descriptor if present; missing descriptors do nothing. |

## Method Descriptions

<a id="member-f115332466ff"></a>
### AddProperty

`public System.Void AddProperty(Electron2D.ReplicationProperty property, System.Int32 index = -1)`

Adds a unique descriptor at an index or appends for any negative index.

property: Shared immutable token.

index: Insertion position; negative appends.

<a id="member-dc8de77731fc"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Copies derived stored state into a duplicate or copy target.

target: A live resource with the exact same runtime type.

deep: Whether typed collection containers should be cloned recursively.

subresourceMode: The nested-resource policy for this copy.

duplicateSubresource: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource. Pass every nested resource through this function when deep is true.

forceDuplicateSubresource: A graph-preserving function that duplicates a nested resource even when the current policy would share it. Use it for typed properties whose contract requires duplication; assign the original reference directly for properties whose contract forbids duplication.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Derived implementations must copy all stored custom state and call the base implementation only when they intentionally want its validation. Assigning the original nested-resource reference directly expresses a never-duplicate property.

System.NotSupportedException: A derived resource has not explicitly implemented custom-state copying.

<a id="member-4f10e87cc93e"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-fbcc3dbd7546"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-d7b6ad7ebc34"></a>
### GetProperties

`public Electron2D.ReplicationProperty[] GetProperties()`

Returns a copied ordered descriptor snapshot.

Returns: Shared immutable tokens in configuration order.

<a id="member-3feed263332f"></a>
### HasProperty

`public System.Boolean HasProperty(Electron2D.ReplicationProperty property)`

Tests descriptor membership by immutable token identity.

property: Descriptor to query.

Returns: True when present.

<a id="member-41c323e14da1"></a>
### OnResetState

`protected override System.Void OnResetState()`

Clears non-stored state when Electron2D.Resource.ResetState or Electron2D.Resource.CopyFromResource(Electron2D.Resource) requests it.

<a id="member-51307c2f5619"></a>
### PropertyGetIndex

`public System.Int32 PropertyGetIndex(Electron2D.ReplicationProperty property)`

Finds descriptor order.

property: Descriptor to query.

Returns: Index or minus one when absent.

<a id="member-7aa724570abb"></a>
### PropertyGetReplicationMode

`public Electron2D.ReplicationMode PropertyGetReplicationMode(Electron2D.ReplicationProperty property)`

Gets ongoing delivery policy.

property: Configured descriptor.

Returns: Always initially.

<a id="member-ec6fbd6332e6"></a>
### PropertyGetSpawn

`public System.Boolean PropertyGetSpawn(Electron2D.ReplicationProperty property)`

Reports whether the property is included in initial spawn state.

property: Configured descriptor.

Returns: True initially.

<a id="member-02bd8700e072"></a>
### PropertyGetSync

`public System.Boolean PropertyGetSync(Electron2D.ReplicationProperty property)`

Reports whether ongoing mode is Always.

property: Configured descriptor.

Returns: True for Always only.

<a id="member-8a2d6ddf57bc"></a>
### PropertyGetWatch

`public System.Boolean PropertyGetWatch(Electron2D.ReplicationProperty property)`

Reports whether ongoing mode is OnChange.

property: Configured descriptor.

Returns: True for OnChange only.

<a id="member-2de2fb9dc267"></a>
### PropertySetReplicationMode

`public System.Void PropertySetReplicationMode(Electron2D.ReplicationProperty property, Electron2D.ReplicationMode mode)`

Sets Never, Always or OnChange.

property: Configured descriptor.

mode: Ongoing delivery policy.

<a id="member-7ce28e3eae59"></a>
### PropertySetSpawn

`public System.Void PropertySetSpawn(Electron2D.ReplicationProperty property, System.Boolean enabled)`

Sets initial-spawn inclusion after validating membership.

property: Configured descriptor.

enabled: Whether to include it.

<a id="member-1fcabe347865"></a>
### PropertySetSync

`public System.Void PropertySetSync(Electron2D.ReplicationProperty property, System.Boolean enabled)`

Enables Always, or disables it if currently Always.

property: Configured descriptor.

enabled: Requested legacy sync selection.

<a id="member-082e8d1d4bd7"></a>
### PropertySetWatch

`public System.Void PropertySetWatch(Electron2D.ReplicationProperty property, System.Boolean enabled)`

Enables OnChange, or disables it if currently OnChange.

property: Configured descriptor.

enabled: Requested legacy watch selection.

<a id="member-6495d3b3a44e"></a>
### RemoveProperty

`public System.Void RemoveProperty(Electron2D.ReplicationProperty property)`

Removes a descriptor if present; missing descriptors do nothing.

property: Descriptor identity.

## Verification and limits

[SceneReplicationTests](../../tests/Electron2D.Tests/SceneReplicationTests.cs) verifies native WS/WSS custom/automatic/late/visibility spawning and state, copied config, independent malformed/old/wrong-authority schema bytes, actual batching/modes and 64 warmed active/idle zero-managed-allocation intervals. Setup/templates/snapshots/error/caller-codec/native costs remain separate. File-based scene formats/load/save and editor authoring, foreign/routed traffic and human/rendered acceptance require their own gates.
