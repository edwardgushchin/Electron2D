# PackedScene

Last updated: 2026-09-21

**Inherits:** [Resource](Resource.md)

**Inherited By:** —

- **Source:** [`src/Scene/Resources/PackedScene.cs`](../../src/Scene/Resources/PackedScene.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class PackedScene : Resource`

> Stores a reusable in-memory node hierarchy and creates independent runtime instances from it.

## Description

Stores a reusable in-memory node hierarchy and creates independent runtime instances from it.

`PackedScene` is the reuse boundary for Electron2D's Node-based game objects. It stores an in-memory, typed snapshot of one Node hierarchy and constructs independent detached runtime instances from it. The hierarchy may represent one composed game object, a reusable subsystem, or a complete level; these cases use the same capture and instantiation contract. It is a managed [`Resource`](Resource.md) in `Electron2D.dll`; it is not a text/binary scene file, loader, saver, import artifact, or editor document.

The snapshot owns no source [`Node`](Node.md). It retains source-independent static node factories, immutable node metadata, typed stored-property values, and references to resources used by those values. Shared source resources remain caller-owned. During instantiation, the returned root owns all created child nodes and every duplicated scene resource; disposing that root disposes the complete hierarchy and those duplicates.

Runtime packing is typed and uses storage-enabled [`PropertyDescriptor`](PropertyDescriptor.md) instances. Text and binary scene
files, editor metadata, inheritance authoring, placeholders, and persistent event endpoints belong to later domains.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var source = new Node { Name = "Enemy" };
using var scene = new PackedScene();
scene.Pack(source);
using Node instance = scene.Instantiate();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public PackedScene()`](#m-electron2d-packedscene-ctor) | Initializes an empty packed scene. |

## Methods

| Member | Description |
| --- | --- |
| [`public bool CanInstantiate()`](#m-electron2d-packedscene-caninstantiate) | Gets whether this resource contains a scene that can be instantiated. |
| [`public SceneState GetState()`](#m-electron2d-packedscene-getstate) | Gets the live read-only metadata object for this resource. |
| [`public Node Instantiate(PackedSceneEditState editState = PackedSceneEditState.Disabled)`](#m-electron2d-packedscene-instantiate-electron2d-packedsceneeditstate) | Creates an independent detached node hierarchy from the stored scene. |
| [`public void Pack(Node root)`](#m-electron2d-packedscene-pack-electron2d-node) | Replaces this resource's contents with a typed snapshot of a node hierarchy. |
| [`protected override Resource CreateDuplicateInstance()`](#m-electron2d-packedscene-createduplicateinstance) | Creates a fresh default instance used as the target of duplication. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource, Resource> duplicateSubresource, Func<Resource, Resource> forceDuplicateSubresource)`](#m-electron2d-packedscene-copycustomstateto-electron2d-resource-system-boolean-electron2d-deepduplicatemode-system-func-electron2d-resource-electron2d-resource-system-func-electron2d-resource-electron2d-resource) | Copies derived stored state into a duplicate or copy target. |
| [`protected override void OnResetState()`](#m-electron2d-packedscene-onresetstate) | Clears non-stored state when [`Resource.ResetState`](Resource.md#m-electron2d-resource-resetstate) or [`Resource.CopyFromResource(Resource)`](Resource.md#m-electron2d-resource-copyfromresource-electron2d-resource) requests it. |
| [`protected override void OnResourcePathChanged(string path)`](#m-electron2d-packedscene-onresourcepathchanged-system-string) | Handles any committed change to this resource's visible path. |

## Constructor Descriptions

<a id="m-electron2d-packedscene-ctor"></a>
### `public PackedScene()`

Initializes an empty packed scene.

## Method Descriptions

<a id="m-electron2d-packedscene-caninstantiate"></a>
### `public bool CanInstantiate()`

Gets whether this resource contains a scene that can be instantiated.

**Returns:** `true` after a successful nonempty pack or state copy; otherwise `false`.

**Exceptions**

- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.

<a id="m-electron2d-packedscene-getstate"></a>
### `public SceneState GetState()`

Gets the live read-only metadata object for this resource.

**Returns:** A state object that observes later content and path transitions until either object is disposed.

**Exceptions**

- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.

**Remarks:** If a caller disposes a previously returned state, the next call creates a replacement.

<a id="m-electron2d-packedscene-instantiate-electron2d-packedsceneeditstate"></a>
### `public Node Instantiate(PackedSceneEditState editState = PackedSceneEditState.Disabled)`

Creates an independent detached node hierarchy from the stored scene.

**Parameters**

- `editState`: The editor-state policy. Runtime instantiation accepts only [`PackedSceneEditState.Disabled`](PackedSceneEditState.md#f-electron2d-packedsceneeditstate-disabled).

**Returns:** The live, detached root node. It has not entered a [`SceneTree`](SceneTree.md).

**Exceptions**

- `ArgumentOutOfRangeException`: `editState` is not defined.
- `NotSupportedException`: `editState` requests editor-only behavior.
- `InvalidOperationException`: The scene is empty or stored node schema cannot be reconstructed safely.
- `ObjectDisposedException`: The resource or a referenced stored resource is disposing or disposed.
- `Exception`: A factory, property setter, setup callback, notification, or cleanup operation fails.

**Remarks:** Nodes are constructed parent-first. Stored properties and persistent groups are restored before parenting;
owners and scene-local resources are assigned after the hierarchy is complete. Only the root receives
[`Node.NotificationSceneInstantiated`](Node.md#f-electron2d-node-notificationsceneinstantiated).

<a id="m-electron2d-packedscene-pack-electron2d-node"></a>
### `public void Pack(Node root)`

Replaces this resource's contents with a typed snapshot of a node hierarchy.

**Parameters**

- `root`: The live root to capture. A `null` argument is rejected without changing existing state.

**Exceptions**

- `ArgumentNullException`: `root` is `null`.
- `InvalidOperationException`: Capture is re-entered, the hierarchy changes, or a node factory is unsafe.
- `NotSupportedException`: A stored property uses an unsupported typed representation.
- `ObjectDisposedException`: This resource, a captured node, or a captured resource is disposing or disposed.
- `Exception`: Property discovery, capture, change notification, or cleanup fails.

**Remarks:** The root is always captured. A descendant branch is captured only when its first node is owned by
`root`; rejected branches are pruned. After capture starts, any failure leaves this resource empty.

<a id="m-electron2d-packedscene-createduplicateinstance"></a>
### `protected override Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

**Returns:** A live resource of the exact same runtime type with empty path and scene ID.

**Exceptions**

- `NotSupportedException`: The runtime type derives from [`Resource`](Resource.md) and has not overridden this method.

**Remarks:** The base implementation supports only an exact [`Resource`](Resource.md) instance. Every derived class must
override this method, even when it adds no state, so duplication support is explicit.

<a id="m-electron2d-packedscene-copycustomstateto-electron2d-resource-system-boolean-electron2d-deepduplicatemode-system-func-electron2d-resource-electron2d-resource-system-func-electron2d-resource-electron2d-resource"></a>
### `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource, Resource> duplicateSubresource, Func<Resource, Resource> forceDuplicateSubresource)`

Copies derived stored state into a duplicate or copy target.

**Parameters**

- `target`: A live resource with the exact same runtime type.
- `deep`: Whether typed collection containers should be cloned recursively.
- `subresourceMode`: The nested-resource policy for this copy.
- `duplicateSubresource`: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource.
Pass every nested resource through this function when `deep` is `true`.
- `forceDuplicateSubresource`: A graph-preserving function that duplicates a nested resource even when the current policy would share it.
Use it for typed properties whose contract requires duplication; assign the original reference directly for
properties whose contract forbids duplication.

**Exceptions**

- `NotSupportedException`: A derived resource has not explicitly implemented custom-state copying.

**Remarks:** The base implementation supports only an exact [`Resource`](Resource.md) instance. Derived implementations must
copy all stored custom state and call the base implementation only when they intentionally want its validation.
Assigning the original nested-resource reference directly expresses a never-duplicate property.

<a id="m-electron2d-packedscene-onresetstate"></a>
### `protected override void OnResetState()`

Clears non-stored state when [`Resource.ResetState`](Resource.md#m-electron2d-resource-resetstate) or [`Resource.CopyFromResource(Resource)`](Resource.md#m-electron2d-resource-copyfromresource-electron2d-resource) requests it.

<a id="m-electron2d-packedscene-onresourcepathchanged-system-string"></a>
### `protected override void OnResourcePathChanged(string path)`

Handles any committed change to this resource's visible path.

**Parameters**

- `path`: The newly committed path, or an empty string after displacement.

**Remarks:** The path has already changed when this callback runs.

## Inherited API

Public and protected members inherited from [Resource](Resource.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Capture selection and stored state

The root is always included and does not own itself. Capture then walks children depth-first in sibling order. A descendant branch is included only when its first node has `Owner` equal to the packed root; a rejected branch and all of its descendants are pruned, even if a deeper node names the root as owner.

For each included node, capture stores:

- a source-independent static factory for the exact runtime type and the source instance identity used to reject source reuse;
- the node name, relative path, parent/owner indices, and persistent groups;
- every writable [`PropertyDescriptor`](PropertyDescriptor.md) whose `IsStored` flag is `true`, except `Name`, which has its dedicated field;
- runtime-only empty metadata for nested-scene instances and placeholders, which are not authored by the current implementation.

Supported stored values are strings, `Resource` subtypes, and value types that contain no managed references. This includes [`Color`](Color.md), [`Vector2`](Vector2.md), [`Vector2I`](Vector2I.md), [`Vector4`](Vector4.md), [`Vector4I`](Vector4I.md), [`Rect`](Rect.md), [`RectI`](RectI.md), [`Transform`](Transform.md), and enums such as [`TimerProcessCallback`](TimerProcessCallback.md), whose numeric, ordinary/HDR, negative, integer, affine, or enum components are copied exactly. Reference-shaped values such as arbitrary objects, collections, delegates, and node references are rejected with `NotSupportedException`; no reflection-driven discovery or invocation, dynamic value container, or string-addressed property call is used.

`Pack(null)` fails before capture and preserves the previous state. Once a non-null capture begins, the published state is cleared first. Any later factory/property/schema/capture failure leaves the packed scene empty. Both the empty transition and a successful replacement are visible to a live `SceneState`. `Changed` is emitted after the attempt; a throwing handler does not roll back the already committed result.

## Instantiation flow

1. The current immutable packed data and path classification are captured under the packed-scene lock.
2. Exact-type nodes are created parent-first. Factory execution is guarded so neither a new `SceneTree` nor entry into an existing tree can begin in that execution context before the node is returned. A factory result must be new for this packed state, live, detached, parentless, childless, unowned, unqueued, and free of a scene path. Returning the source, a previously issued node, a wrong type, or state retained through a capturing delegate is rejected.
3. Stored properties are resolved against the new node's current typed descriptor schema and restored before parenting. Persistent groups are then applied.
4. Nodes are parented in captured order, then `Owner` references are assigned.
5. Scene-local resource graphs are duplicated with alias/cycle preservation. Their owning root is assigned before setup callbacks, and each local duplicate is set up once.
6. The root adopts every created resource. An external packed-scene path is copied only to the root's `SceneFilePath`; built-in paths remain empty.
7. The complete hierarchy is revalidated, the root alone receives `Node.NotificationSceneInstantiated`, and topology is revalidated again before return.

The returned root has no parent and belongs to no `SceneTree`. Enter/ready lifecycle starts only when the caller later installs or attaches it. Source event subscribers and [`EventConnection`](EventConnection.md) tokens are not copied; persistent typed event endpoints have no schema yet.

If any factory, setter, parenting callback, resource duplication/setup callback, notification, validation, or cleanup step fails, instantiation attempts to dispose every node result and resource duplicate whose ownership it acquired. Every new node remains under an instantiation barrier until final validation: it cannot be disposed or enter any `SceneTree` while unfinished. Topology validation detects temporary attachment to an unrelated detached hierarchy, and rollback removes and disposes the escaped node. Cleanup attempts every owned item and aggregates rollback failures with the original exception. A factory remains responsible for side effects or allocations it performs before throwing instead of returning a node.

## SceneState lifecycle

`GetState()` exposes one live metadata object. Successful repacks, reset/copy transitions, and path changes update that object. Disposing a returned state does not dispose the packed scene; the next access or update creates a replacement. Disposing the packed scene does not dispose an externally held state, which retains its final immutable data and last path.

The state is a view, not an ownership transfer. A resource value returned through `SceneState.GetNodePropertyValue<TValue>()` is the stored resource reference and remains governed by normal `Resource` ownership.

## Threading and reentrancy

Packed data access, capture replacement, state replacement, and path propagation are serialized by an internal lock. Concurrent instantiations use the immutable data snapshot observed when each call begins. Factory issuance and `SceneState` reads are safe for concurrent calls. A concurrent state disposal is recovered by replacing the cached state.

Callbacks execute synchronously on the caller's thread. Packing an attached hierarchy therefore requires its `SceneTree` owner thread; detached custom node/resource state has no automatic synchronization. Capture marks the complete source hierarchy so node setters, structural mutation, disposal, deletion requests, and inherited mutable object state are rejected until capture ends. Derived stored-property setters must honor `Node.EnsureMutable()`.

No hard real-time or allocation-free guarantee is made for packing or instantiation. Factories, descriptors, resource duplication, validation, and callbacks allocate managed objects and may run arbitrary user code.

## Errors and invariants

- Empty instantiation and incompatible stored schemas throw `InvalidOperationException`.
- Undefined edit-state values throw `ArgumentOutOfRangeException`; defined editor modes throw `NotSupportedException`.
- Null roots throw `ArgumentNullException` without changing data.
- Unsupported stored value shapes and derived nodes without an explicit reusable factory throw `NotSupportedException`.
- Disposed nodes/resources and disposal races throw `ObjectDisposedException`.
- Factory, descriptor, resource, notification, event, and cleanup exceptions propagate; multiple primary/cleanup failures are aggregated.
- Packed data never stores a live source node, instance-bound factory, arbitrary object graph, event delegate, or `SceneTree`.

## Dependencies and interactions

`PackedScene` depends on `Resource`, `Node`, `PropertyDescriptor`, `SceneState`, `PackedSceneEditState`, standard collections, weak identity tracking, and deterministic disposal. It uses `Resource`'s internal graph duplication machinery for local resources. It does not depend on SDL3-CS, rendering, input, audio, physics, a filesystem scene format, an asset loader/saver, scripting, networking, or an editor.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` covers empty state, runtime and invalid edit modes, owned-branch pruning, paths/owners/groups, typed state queries including `Color`, all four vector values, `Rect`, `RectI`, `Transform`, and Timer configuration restoration, source disposal independence, scene-local aliasing and setup, repeated independent instances, packed-scene duplication, live-state replacement and disposal races, concurrent path updates, failed capture clearing, capture mutation rejection, capturing/source-returning/wrong-type/reused factories, detached-parent and active-tree escape rollback, setup failure cleanup, and state survival after packed-scene disposal.

The current contract is runtime-only and in-memory. It has no `.tscn`/binary loader or saver, resource UID/import remapping, inherited-scene authoring, nested packed-scene overrides, editable instances, placeholders, missing-resource recovery, script state, persistent event endpoint schema, or editor mode implementation. `SceneState` exposes honest empty/null answers for those absent metadata categories.

## Decision

- [0023: Typed in-memory packed scenes](../decisions/scene.md#adr-0023)
- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0035: Foreseeable public type-family completeness](../decisions/core-math.md#adr-0035)
- [0029: Typed Transform2D value and affine semantics](../decisions/core-math.md#adr-0029)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0031: Node trees and reusable scenes as the primary game-object model](../decisions/scene.md#adr-0031)
