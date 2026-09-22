# SceneState

Last updated: 2026-09-23

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Scene/Resources/SceneState.cs`](../../src/Scene/Resources/SceneState.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class SceneState : ElectronObject`

> Provides read-only typed metadata for the current contents of a [`PackedScene`](PackedScene.md).

## Description

Provides read-only typed metadata for the current contents of a [`PackedScene`](PackedScene.md).

`SceneState` is the read-only typed metadata view returned by [`PackedScene.GetState()`](PackedScene.md). It exposes node, property, group, path, owner, placeholder, nested-instance, and connection metadata without a dynamic value container or string-based mutation.

Callers cannot construct a state directly. The object owns no nodes or resources and cannot instantiate or mutate a scene. It retains the immutable packed-data reference currently published to it; resource-valued properties are borrowed references governed by normal [`Resource`](Resource.md) ownership.

Instances are created by [`PackedScene.GetState`](PackedScene.md#m-electron2d-packedscene-getstate). A live state object tracks every content and path
transition of its source resource, while retaining its final snapshot if that resource is later disposed.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var scene = new PackedScene();
scene.Pack(root);
SceneState state = scene.GetState();
int nodeCount = state.NodeCount;
```

## Methods

| Member | Description |
| --- | --- |
| [`public SceneState GetBaseSceneState()`](#m-electron2d-scenestate-getbasescenestate) | Gets the inherited base scene state. |
| [`public int GetConnectionCount()`](#m-electron2d-scenestate-getconnectioncount) | Gets the number of persistent connections stored in this state. |
| [`public int GetNodeCount()`](#m-electron2d-scenestate-getnodecount) | Gets the number of stored nodes. |
| [`public IReadOnlyList<string> GetNodeGroups(int nodeIndex)`](#m-electron2d-scenestate-getnodegroups-system-int32) | Gets the persistent groups stored for a node. |
| [`public int GetNodeIndex(int nodeIndex)`](#m-electron2d-scenestate-getnodeindex-system-int32) | Gets the stored sibling index used by an instanced subscene override. |
| [`public PackedScene GetNodeInstance(int nodeIndex)`](#m-electron2d-scenestate-getnodeinstance-system-int32) | Gets the nested packed scene associated with a node. |
| [`public string GetNodeInstancePlaceholder(int nodeIndex)`](#m-electron2d-scenestate-getnodeinstanceplaceholder-system-int32) | Gets the resource path represented by a node placeholder. |
| [`public string GetNodeName(int nodeIndex)`](#m-electron2d-scenestate-getnodename-system-int32) | Gets a stored node's name. |
| [`public string GetNodeOwnerPath(int nodeIndex)`](#m-electron2d-scenestate-getnodeownerpath-system-int32) | Gets the path of a stored node's owner. |
| [`public string GetNodePath(int nodeIndex, bool forParent = false)`](#m-electron2d-scenestate-getnodepath-system-int32-system-boolean) | Gets a stored node path or its parent's path. |
| [`public int GetNodePropertyCount(int nodeIndex)`](#m-electron2d-scenestate-getnodepropertycount-system-int32) | Gets the number of stored properties for a node. |
| [`public string GetNodePropertyName(int nodeIndex, int propertyIndex)`](#m-electron2d-scenestate-getnodepropertyname-system-int32-system-int32) | Gets the name of a stored node property. |
| [`public TValue GetNodePropertyValue<TValue>(int nodeIndex, int propertyIndex)`](#m-electron2d-scenestate-getnodepropertyvalue-1-system-int32-system-int32) | Gets a stored node property through a requested compatible type. |
| [`public string GetNodeType(int nodeIndex)`](#m-electron2d-scenestate-getnodetype-system-int32) | Gets a stored node's runtime type name. |
| [`public string GetPath()`](#m-electron2d-scenestate-getpath) | Gets the resource path associated with this state. |
| [`public bool IsNodeInstancePlaceholder(int nodeIndex)`](#m-electron2d-scenestate-isnodeinstanceplaceholder-system-int32) | Gets whether a stored node is an instance placeholder. |

## Method Descriptions

<a id="m-electron2d-scenestate-getbasescenestate"></a>
### `public SceneState GetBaseSceneState()`

Gets the inherited base scene state.

**Returns:** Always `null` until scene inheritance is available.

**Exceptions**

- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getconnectioncount"></a>
### `public int GetConnectionCount()`

Gets the number of persistent connections stored in this state.

**Returns:** Zero because typed persistent connection endpoints are not yet part of the runtime contract.

**Exceptions**

- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodecount"></a>
### `public int GetNodeCount()`

Gets the number of stored nodes.

**Returns:** Zero for an empty state; otherwise the captured node count.

**Exceptions**

- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodegroups-system-int32"></a>
### `public IReadOnlyList<string> GetNodeGroups(int nodeIndex)`

Gets the persistent groups stored for a node.

**Parameters**

- `nodeIndex`: The zero-based node index.

**Returns:** An immutable group-name snapshot in ordinal order.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodeindex-system-int32"></a>
### `public int GetNodeIndex(int nodeIndex)`

Gets the stored sibling index used by an instanced subscene override.

**Parameters**

- `nodeIndex`: The zero-based node index.

**Returns:** `-1` for ordinary locally captured nodes.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodeinstance-system-int32"></a>
### `public PackedScene GetNodeInstance(int nodeIndex)`

Gets the nested packed scene associated with a node.

**Parameters**

- `nodeIndex`: The zero-based node index.

**Returns:** The nested scene, or `null` for a locally captured node.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodeinstanceplaceholder-system-int32"></a>
### `public string GetNodeInstancePlaceholder(int nodeIndex)`

Gets the resource path represented by a node placeholder.

**Parameters**

- `nodeIndex`: The zero-based node index.

**Returns:** An empty string because runtime-authored placeholders are not supported.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodename-system-int32"></a>
### `public string GetNodeName(int nodeIndex)`

Gets a stored node's name.

**Parameters**

- `nodeIndex`: The zero-based node index.

**Returns:** The captured name.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodeownerpath-system-int32"></a>
### `public string GetNodeOwnerPath(int nodeIndex)`

Gets the path of a stored node's owner.

**Parameters**

- `nodeIndex`: The zero-based node index.

**Returns:** `.` for nodes owned by the root, or an empty string when no owner is stored.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodepath-system-int32-system-boolean"></a>
### `public string GetNodePath(int nodeIndex, bool forParent = false)`

Gets a stored node path or its parent's path.

**Parameters**

- `nodeIndex`: The zero-based node index.
- `forParent`: `true` to return the stored parent path.

**Returns:** A relative scene path; the root is represented by `.`.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodepropertycount-system-int32"></a>
### `public int GetNodePropertyCount(int nodeIndex)`

Gets the number of stored properties for a node.

**Parameters**

- `nodeIndex`: The zero-based node index.

**Returns:** The property count.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodepropertyname-system-int32-system-int32"></a>
### `public string GetNodePropertyName(int nodeIndex, int propertyIndex)`

Gets the name of a stored node property.

**Parameters**

- `nodeIndex`: The zero-based node index.
- `propertyIndex`: The zero-based property index.

**Returns:** The property name.

**Exceptions**

- `ArgumentOutOfRangeException`: Either index is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodepropertyvalue-1-system-int32-system-int32"></a>
### `public TValue GetNodePropertyValue<TValue>(int nodeIndex, int propertyIndex)`

Gets a stored node property through a requested compatible type.

**Type parameters**

- `TValue`: The requested result type.

**Parameters**

- `nodeIndex`: The zero-based node index.
- `propertyIndex`: The zero-based property index.

**Returns:** The captured property value.

**Exceptions**

- `ArgumentOutOfRangeException`: Either index is outside the state.
- `InvalidCastException`: The captured value is not compatible with `TValue`.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getnodetype-system-int32"></a>
### `public string GetNodeType(int nodeIndex)`

Gets a stored node's runtime type name.

**Parameters**

- `nodeIndex`: The zero-based node index.

**Returns:** The unqualified runtime type name captured by the scene.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-getpath"></a>
### `public string GetPath()`

Gets the resource path associated with this state.

**Returns:** The current path of its live packed scene, or the last path observed before that resource was disposed.

**Exceptions**

- `ObjectDisposedException`: This state has been disposed.

<a id="m-electron2d-scenestate-isnodeinstanceplaceholder-system-int32"></a>
### `public bool IsNodeInstancePlaceholder(int nodeIndex)`

Gets whether a stored node is an instance placeholder.

**Parameters**

- `nodeIndex`: The zero-based node index.

**Returns:** `false` for every runtime-authored node.

**Exceptions**

- `ArgumentOutOfRangeException`: `nodeIndex` is outside the state.
- `ObjectDisposedException`: This state has been disposed.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Lifecycle and state transitions

A newly constructed `PackedScene` owns one empty live state. The same state object observes packed-data replacement, reset/copy, and resource-path changes. Disposing it makes that object terminal but does not affect the packed scene; the source creates a replacement on its next state access or update.

Disposing the source packed scene does not dispose externally held states. Such a state stops receiving updates and remains a readable final snapshot until the caller disposes it. Disposing a state never disposes stored resource references.

## Invariants and threading

- The public surface is read-only; no method changes packed data or node instances.
- Published packed data is immutable to callers. Group collections are read-only snapshots.
- Every read and source-driven replacement is serialized by the state lock and rechecks disposal.
- Concurrent reads are supported. Concurrent disposal causes reads to fail and source updates to replace the cached state safely.
- Resource values remain mutable resources; reading metadata does not make their derived state thread-safe.

## Dependencies and interactions

`SceneState` depends on `ElectronObject`, the internal immutable packed-scene records, `Type`, and standard read-only collections. It refers to `PackedScene` and stored `Resource` values but owns neither. It has no `SceneTree`, SDL, renderer, loader/saver, editor, or native dependency.

## Verification and limitations

The executable harness verifies empty/live state identity, path propagation, node ordering and paths, owner/group metadata, absent connection/placeholder/inheritance metadata, typed property name/type/value access including Timer configuration, invalid indices/casts, successful repack observation, concurrent cached-state disposal and path updates, and survival after source disposal.

The type intentionally omits connection-detail accessors, editable-instance metadata, inherited base state, placeholder loading, node-reference remapping metadata, and file-format internals because those producers do not exist. `GetNodeType()` is a diagnostic unqualified name, not a reflection-based factory key.

## Decision

- [0023: Typed in-memory packed scenes](../decisions/scene.md#adr-0023)
