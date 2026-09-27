# Resource

Last updated: 2026-09-27

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** [Theme](Theme.md), [StyleBox](StyleBox.md), [Gradient](Gradient.md), [Curve](Curve.md), [Curve2D](Curve2D.md), [Image](Image.md), [InputEvent](InputEvent.md), [PackedScene](PackedScene.md), [SpriteFrames](SpriteFrames.md)

- **Source:** [`src/Core/IO/Resource.cs`](../../src/Core/IO/Resource.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Resource : ElectronObject`

> Provides reusable data, change notification, path identity, and typed duplication for engine assets.

## Description

Provides reusable data, change notification, path identity, and typed duplication for engine assets.

`Resource` is the managed base for reusable engine data. It owns resource naming, optional process-wide path identity, scene-instancing configuration, scene serialization identity, synchronous change notification, reset/setup hooks, and typed graph-preserving duplication.

The managed runtime owns object memory. Deterministic logical cleanup remains available through inherited `IDisposable`; no public reference counter is exposed. A future asset manager may track active leases internally to retain and release native-backed asset payloads, but that counter will not control managed object collection. A resource does not own nested resources returned by a derived class and does not dispose them during its own cleanup.

Resource lifetime uses the managed runtime together with [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose); no public reference
counter is exposed. Base state is safe for concurrent reads and serialized writes, but derived resource state and
callbacks have no implicit synchronization or thread affinity.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var resource = new Resource { ResourceName = "PlayerData" };
resource.Changed += _ => Console.WriteLine("Changed");
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Resource()`](#m-electron2d-resource-ctor) | Initializes a new Resource instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public bool ResourceLocalToScene { get; set; }`](#p-electron2d-resource-resourcelocaltoscene) | Gets or sets whether a scene-instancing component should make this resource unique to each scene instance. |
| [`public string ResourceName { get; set; }`](#p-electron2d-resource-resourcename) | Gets or sets the optional display name of this resource. |
| [`public string ResourcePath { get; set; }`](#p-electron2d-resource-resourcepath) | Gets or sets the unique cache path associated with this resource. |
| [`public string ResourceSceneUniqueID { get; set; }`](#p-electron2d-resource-resourcesceneuniqueid) | Gets or sets the identifier used when this resource is embedded in a serialized scene. |
| [`public bool IsBuiltIn { get; }`](#p-electron2d-resource-isbuiltin) | Gets whether this resource is embedded rather than represented by a standalone external path. |

## Methods

| Member | Description |
| --- | --- |
| [`public Node GetLocalScene()`](#m-electron2d-resource-getlocalscene) | Gets the root node whose scene instance owns this scene-local resource. |
| [`public void CopyFromResource(Resource source)`](#m-electron2d-resource-copyfromresource-electron2d-resource) | Copies stored data from another resource of the exact same runtime type while preserving this resource's path and scene ID. |
| [`public Resource Duplicate(bool deep = false)`](#m-electron2d-resource-duplicate-system-boolean) | Creates a shallow or internally deep duplicate of this resource. |
| [`public Resource DuplicateDeep(DeepDuplicateMode subresourceMode = DeepDuplicateMode.Internal)`](#m-electron2d-resource-duplicatedeep-electron2d-deepduplicatemode) | Creates a deep duplicate with explicit nested-resource policy. |
| [`public void EmitChanged()`](#m-electron2d-resource-emitchanged) | Synchronously reports that this resource's meaningful content changed. |
| [`public static string GenerateSceneUniqueID()`](#m-electron2d-resource-generatesceneuniqueid) | Generates a compact scene-relative resource identifier. |
| [`public void ResetState()`](#m-electron2d-resource-resetstate) | Clears non-stored state through [`Resource.OnResetState`](Resource.md#m-electron2d-resource-onresetstate). |
| [`public void SetPathCache(string path)`](#m-electron2d-resource-setpathcache-system-string) | Sets the path value without registering it in the process-wide resource cache. |
| [`public void SetupLocalToScene()`](#m-electron2d-resource-setuplocaltoscene) | Invokes scene-local setup callbacks for a resource duplicated by a scene-instancing component. |
| [`public void TakeOverPath(string path)`](#m-electron2d-resource-takeoverpath-system-string) | Transfers ownership of a process-wide resource path to this resource. |
| [`protected virtual Resource CreateDuplicateInstance()`](#m-electron2d-resource-createduplicateinstance) | Creates a fresh default instance used as the target of duplication. |
| [`protected virtual void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource, Resource> duplicateSubresource, Func<Resource, Resource> forceDuplicateSubresource)`](#m-electron2d-resource-copycustomstateto-electron2d-resource-system-boolean-electron2d-deepduplicatemode-system-func-electron2d-resource-electron2d-resource-system-func-electron2d-resource-electron2d-resource) | Copies derived stored state into a duplicate or copy target. |
| [`protected virtual void OnResetState()`](#m-electron2d-resource-onresetstate) | Clears non-stored state when [`Resource.ResetState`](Resource.md#m-electron2d-resource-resetstate) or [`Resource.CopyFromResource(Resource)`](Resource.md#m-electron2d-resource-copyfromresource-electron2d-resource) requests it. |
| [`protected virtual void OnPathCacheSet(string path)`](#m-electron2d-resource-onpathcacheset-system-string) | Handles a raw path-cache assignment after the new path has been committed. |
| [`protected virtual void OnResourcePathChanged(string path)`](#m-electron2d-resource-onresourcepathchanged-system-string) | Handles any committed change to this resource's visible path. |
| [`protected virtual void OnSetupLocalToScene()`](#m-electron2d-resource-onsetuplocaltoscene) | Customizes a newly duplicated scene-local resource. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-resource-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-resource-dispose-system-boolean) | Releases resources owned by a derived class. |
| [`public override string ToString()`](#m-electron2d-resource-tostring) | Returns a diagnostic string containing the optional resource name, path, runtime class, and instance identifier. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<Resource> Changed`](#e-electron2d-resource-changed) | Occurs when this resource reports a meaningful content change. |
| [`public event Action<Resource> SetupLocalToSceneRequested`](#e-electron2d-resource-setuplocaltoscenerequested) | Occurs immediately before [`Resource.OnSetupLocalToScene`](Resource.md#m-electron2d-resource-onsetuplocaltoscene) is invoked. |

## Constructor Descriptions

<a id="m-electron2d-resource-ctor"></a>
### `public Resource()`

Initializes a new Resource instance.

## Property Descriptions

<a id="p-electron2d-resource-resourcelocaltoscene"></a>
### `public bool ResourceLocalToScene { get; set; }`

Gets or sets whether a scene-instancing component should make this resource unique to each scene instance.

**Value:** `false` by default; `true` requests per-instance duplication.

**Exceptions**

- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.

**Remarks:** Changing this value does not retroactively affect existing instances. [`PackedScene.Instantiate(PackedSceneEditState)`](PackedScene.md#m-electron2d-packedscene-instantiate-electron2d-packedsceneeditstate)
duplicates a marked resource once per instance while preserving aliases in its duplicated resource graph.

<a id="p-electron2d-resource-resourcename"></a>
### `public string ResourceName { get; set; }`

Gets or sets the optional display name of this resource.

**Value:** An arbitrary non-null string; the default is empty.

**Exceptions**

- `ArgumentNullException`: The assigned value is `null`.
- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value has been assigned.

**Remarks:** Every successful assignment synchronously raises [`Resource.Changed`](Resource.md#e-electron2d-resource-changed), even when the value is unchanged.

<a id="p-electron2d-resource-resourcepath"></a>
### `public string ResourcePath { get; set; }`

Gets or sets the unique cache path associated with this resource.

**Value:** An opaque, case-sensitive path, or an empty string when the resource has no registered path.

**Exceptions**

- `ArgumentNullException`: The assigned value is `null`.
- `InvalidOperationException`: Another live resource owns the assigned nonempty path.
- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.

**Remarks:** Nonempty paths are process-wide and unique among live resources. Assigning an occupied path throws without
changing either resource. Use [`Resource.TakeOverPath(String)`](Resource.md#m-electron2d-resource-takeoverpath-system-string) to transfer ownership deliberately.

<a id="p-electron2d-resource-resourcesceneuniqueid"></a>
### `public string ResourceSceneUniqueID { get; set; }`

Gets or sets the identifier used when this resource is embedded in a serialized scene.

**Value:** An empty string, or an identifier containing only ASCII letters, digits, and underscores.

**Exceptions**

- `ArgumentException`: The assigned value contains a character outside ASCII letters, digits, and underscores.
- `ArgumentNullException`: The assigned value is `null`.
- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.

**Remarks:** Assignments do not raise [`Resource.Changed`](Resource.md#e-electron2d-resource-changed). Scene saving and collision resolution are not implemented yet.

<a id="p-electron2d-resource-isbuiltin"></a>
### `public bool IsBuiltIn { get; }`

Gets whether this resource is embedded rather than represented by a standalone external path.

**Value:** `true` when the path is empty, contains an embedded-resource separator, or starts with the
local-resource prefix; otherwise `false`.

**Exceptions**

- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.

## Method Descriptions

<a id="m-electron2d-resource-getlocalscene"></a>
### `public Node GetLocalScene()`

Gets the root node whose scene instance owns this scene-local resource.

Replacing a packed-scene root with `Node.ReplaceBy` transfers the association to the replacement, so disposing the old root does not invalidate the resource.

**Returns:** The owning scene root after scene instantiation, or `null` for other resources.

**Exceptions**

- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.

**Remarks:** The association is assigned before [`Resource.OnSetupLocalToScene`](Resource.md#m-electron2d-resource-onsetuplocaltoscene) runs and remains until disposal.

<a id="m-electron2d-resource-copyfromresource-electron2d-resource"></a>
### `public void CopyFromResource(Resource source)`

Copies stored data from another resource of the exact same runtime type while preserving this resource's path and scene ID.

**Parameters**

- `source`: The live resource whose stored data is copied.

**Exceptions**

- `ArgumentException`: `source` has a different runtime type.
- `ArgumentNullException`: `source` is `null`.
- `ObjectDisposedException`: Either resource is disposing or disposed.
- `Exception`: A reset, copy, setter, or final change handler fails.

**Remarks:** Copying is shallow: nested resources and collection instances remain shared unless a derived override explicitly
defines other behavior. [`Resource.ResetState`](Resource.md#m-electron2d-resource-resetstate) runs first. Change notifications raised while copying are
coalesced into one final [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) event. Batches targeting the same resource are serialized, but
derived state still requires caller coordination. The operation is not transactional if a derived callback fails.

<a id="m-electron2d-resource-duplicate-system-boolean"></a>
### `public Resource Duplicate(bool deep = false)`

Creates a shallow or internally deep duplicate of this resource.

**Parameters**

- `deep`: `false` to share collection containers and nested resources; `true` to let
derived resources clone containers and duplicate built-in nested resources.

**Returns:** A new live resource of the exact same runtime type with an empty path and scene ID.

**Exceptions**

- `InvalidOperationException`: A derived duplication factory returns an invalid instance.
- `NotSupportedException`: A derived resource does not explicitly implement the duplication hooks.
- `ObjectDisposedException`: This resource is disposing or disposed.
- `Exception`: Construction, copying, or cleanup of a failed duplicate throws.

<a id="m-electron2d-resource-duplicatedeep-electron2d-deepduplicatemode"></a>
### `public Resource DuplicateDeep(DeepDuplicateMode subresourceMode = DeepDuplicateMode.Internal)`

Creates a deep duplicate with explicit nested-resource policy.

**Parameters**

- `subresourceMode`: Controls which nested resources are duplicated.

**Returns:** A new live resource of the exact same runtime type with an empty path and scene ID.

**Exceptions**

- `ArgumentOutOfRangeException`: `subresourceMode` is not defined.
- `InvalidOperationException`: A derived duplication factory returns an invalid instance.
- `NotSupportedException`: A derived resource does not explicitly implement the duplication hooks.
- `ObjectDisposedException`: This resource is disposing or disposed.
- `Exception`: Construction, copying, or cleanup of a failed duplicate throws.

**Remarks:** Repeated and cyclic resource references preserve graph identity. Derived resources remain responsible for
cloning their typed collection containers and passing nested resources to the appropriate supplied duplication delegate.

<a id="m-electron2d-resource-emitchanged"></a>
### `public void EmitChanged()`

Synchronously reports that this resource's meaningful content changed.

**Exceptions**

- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws.

**Remarks:** Calls made inside a copy batch are coalesced into one event when the outermost batch ends.

<a id="m-electron2d-resource-generatesceneuniqueid"></a>
### `public static string GenerateSceneUniqueID()`

Generates a compact scene-relative resource identifier.

**Returns:** A five-character string composed of lowercase letters `a` through `y` and digits `0` through `8`.

**Remarks:** The result is probabilistically unique; a future scene saver must still detect and resolve collisions.

<a id="m-electron2d-resource-resetstate"></a>
### `public void ResetState()`

Clears non-stored state through [`Resource.OnResetState`](Resource.md#m-electron2d-resource-onresetstate).

**Exceptions**

- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.
- `Exception`: [`Resource.OnResetState`](Resource.md#m-electron2d-resource-onresetstate) throws.

**Remarks:** The base implementation does not change stored properties and does not raise [`Resource.Changed`](Resource.md#e-electron2d-resource-changed).

<a id="m-electron2d-resource-setpathcache-system-string"></a>
### `public void SetPathCache(string path)`

Sets the path value without registering it in the process-wide resource cache.

**Parameters**

- `path`: The non-null opaque path, or an empty string.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.
- `Exception`: [`Resource.OnPathCacheSet(String)`](Resource.md#m-electron2d-resource-onpathcacheset-system-string) throws after the path has been committed.

**Remarks:** This loader-oriented operation may produce the same visible path on multiple resources. It first removes this
resource's previously registered path, then invokes [`Resource.OnPathCacheSet(String)`](Resource.md#m-electron2d-resource-onpathcacheset-system-string) after committing the value.

<a id="m-electron2d-resource-setuplocaltoscene"></a>
### `public void SetupLocalToScene()`

Invokes scene-local setup callbacks for a resource duplicated by a scene-instancing component.

**Exceptions**

- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.
- `AggregateException`: Both the compatibility event and virtual callback fail.
- `Exception`: The compatibility event or virtual callback fails.

**Remarks:** Packed-scene instantiation invokes this automatically for each duplicated scene-local resource.

<a id="m-electron2d-resource-takeoverpath-system-string"></a>
### `public void TakeOverPath(string path)`

Transfers ownership of a process-wide resource path to this resource.

**Parameters**

- `path`: The non-null opaque path. An empty path simply clears this resource's current path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.

**Remarks:** A displaced live resource atomically receives an empty path.

<a id="m-electron2d-resource-createduplicateinstance"></a>
### `protected virtual Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

**Returns:** A live resource of the exact same runtime type with empty path and scene ID.

**Exceptions**

- `NotSupportedException`: The runtime type derives from [`Resource`](Resource.md) and has not overridden this method.

**Remarks:** The base implementation supports only an exact [`Resource`](Resource.md) instance. Every derived class must
override this method, even when it adds no state, so duplication support is explicit.

<a id="m-electron2d-resource-copycustomstateto-electron2d-resource-system-boolean-electron2d-deepduplicatemode-system-func-electron2d-resource-electron2d-resource-system-func-electron2d-resource-electron2d-resource"></a>
### `protected virtual void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource, Resource> duplicateSubresource, Func<Resource, Resource> forceDuplicateSubresource)`

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

<a id="m-electron2d-resource-onresetstate"></a>
### `protected virtual void OnResetState()`

Clears non-stored state when [`Resource.ResetState`](Resource.md#m-electron2d-resource-resetstate) or [`Resource.CopyFromResource(Resource)`](Resource.md#m-electron2d-resource-copyfromresource-electron2d-resource) requests it.

<a id="m-electron2d-resource-onpathcacheset-system-string"></a>
### `protected virtual void OnPathCacheSet(string path)`

Handles a raw path-cache assignment after the new path has been committed.

**Parameters**

- `path`: The newly committed path.

<a id="m-electron2d-resource-onresourcepathchanged-system-string"></a>
### `protected virtual void OnResourcePathChanged(string path)`

Handles any committed change to this resource's visible path.

**Parameters**

- `path`: The newly committed path, or an empty string after displacement.

**Remarks:** The path has already changed when this callback runs.

<a id="m-electron2d-resource-onsetuplocaltoscene"></a>
### `protected virtual void OnSetupLocalToScene()`

Customizes a newly duplicated scene-local resource.

**Remarks:** The owning scene is available through [`Resource.GetLocalScene`](Resource.md#m-electron2d-resource-getlocalscene) while this callback runs.

<a id="m-electron2d-resource-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

Appends resource identity and scene-instancing configuration descriptors.

<a id="m-electron2d-resource-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="m-electron2d-resource-tostring"></a>
### `public override string ToString()`

Returns a diagnostic string containing the optional resource name, path, runtime class, and instance identifier.

**Returns:** A stable diagnostic representation of this resource's current base state.

## Event Descriptions

<a id="e-electron2d-resource-changed"></a>
### `public event Action<Resource> Changed`

Occurs when this resource reports a meaningful content change.

**Remarks:** Delivery is synchronous on the calling thread. Custom resource setters should call [`Resource.EmitChanged`](Resource.md#m-electron2d-resource-emitchanged)
after committing a meaningful change. A throwing handler stops later handlers and propagates to the caller.

<a id="e-electron2d-resource-setuplocaltoscenerequested"></a>
### `public event Action<Resource> SetupLocalToSceneRequested`

Occurs immediately before [`Resource.OnSetupLocalToScene`](Resource.md#m-electron2d-resource-onsetuplocaltoscene) is invoked.

**Remarks:** Packed-scene instantiation raises this after assigning the local scene; overrides are preferred.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Duplication invariants

- Root duplication always creates a new root, regardless of its path.
- A duplicate never inherits `ResourcePath` or `ResourceSceneUniqueID`.
- `ResourceName` and `ResourceLocalToScene` are copied.
- Shallow duplication shares collection containers and nested resources.
- Deep duplication delegates typed-container copying to the derived class and applies `DeepDuplicateMode` to nested resources.
- A typed field may force graph-preserving duplication under any mode or retain its original reference under every mode.
- Repeated references to a duplicated resource point to one duplicate.
- A cycle back to an already duplicated resource points to its duplicate even when that original resource has an external path.
- A failed duplication exposes no live partial result and disposes all created resources in reverse creation order.

## Scene-local lifecycle and ownership

[`PackedScene`](PackedScene.md) creates one graph-preserving duplication session per scene instance. A directly stored resource is duplicated when `ResourceLocalToScene` is true. Within a duplicated local graph, nested resources are duplicated when local-to-scene or built-in; external non-local resources remain shared. Repeated references and cycles resolve to the same duplicate.

Every local duplicate receives the new root through `GetLocalScene()` before setup begins. The obsolete compatibility event runs before `OnSetupLocalToScene()` and each local duplicate is set up once. After successful setup, the root adopts every resource created by the session, including non-local built-in duplicates reached inside the graph. `Node.ReplaceBy` transfers those created resources and local-scene associations to the replacement root. Root disposal disposes owned resources after child-node cleanup and clears each resource's local-scene reference through resource disposal. Failed instantiation disposes the partial duplicate graph instead. Shared source/external resources are never owned or disposed by the instance.

## Path lifecycle and invariants

The process-wide path cache uses ordinal comparison and weak references; it does not keep resources alive. Normal assignment is serialized under a shared lock, so concurrent claims yield one owner. Occupied assignment is transactional. `TakeOverPath` is the explicit destructive transfer. `SetPathCache` is a loader-oriented bypass and therefore its visible value is not ownership proof. Disposal unregisters only when the cache still points to that exact resource.

## Threading and errors

Base fields are safe for concurrent reads and serialized writes. Copy batches targeting the same resource are serialized and each operation retains its own final coalesced change event; change publications from other threads are not absorbed into the active caller's batch. Path ownership changes are process-wide atomic operations. ID generation and `GetLocalScene()` reads are safe for concurrent calls. Events and virtual callbacks run synchronously on the caller's thread without implicit affinity. Derived state has no automatic synchronization; callers and derived implementations must coordinate it.

Public state operations reject disposal in progress on another thread and completed disposal. Null strings, invalid identifiers, occupied paths, mismatched copy types, invalid duplicate modes, unsupported derived duplication, invalid factories, and callback failures produce the documented typed exceptions.

## Official API and inheritance coverage

The reference inheritance chain is `Resource -> RefCounted -> Object`. Electron2D maps `Object` responsibilities to [`ElectronObject`](ElectronObject.md). Public `RefCounted.get_reference_count()`, `init_ref()`, `reference()`, and `unreference()` members are excluded because they cannot replace managed garbage collection and would create a second caller-visible lifetime protocol. This does not exclude future internal asset-manager leases used only to decide when a shared native payload may be released.

| Reference surface | Electron2D status |
| --- | --- |
| Four `resource_*` properties | Implemented as the four typed properties above |
| `changed` | Implemented as `Changed` |
| `setup_local_to_scene_requested` | Implemented as an obsolete typed compatibility event |
| `_reset_state`, `_set_path_cache`, `_setup_local_to_scene` | Implemented as protected typed hooks |
| `copy_from`, `duplicate`, `duplicate_deep`, `emit_changed`, `generate_scene_unique_id`, `is_built_in`, `reset_state`, `set_path_cache`, `setup_local_to_scene`, `take_over_path` | Implemented with the typed adaptations documented above; explicit hook delegates replace reflective always/never-duplicate property flags |
| `_get_rid`, `get_rid` | Deferred until a renderer/resource-handle domain defines RID ownership |
| `get_local_scene` and automatic local-to-scene duplication/setup | Implemented for in-memory `PackedScene`; root association precedes setup and persists until resource disposal |
| `get_id_for_path`, `set_id_for_path` | Deferred with editor/import serialization because their mapping is tooling-only |
| Synchronous loader cache modes for image textures | Implemented through typed [`ResourceLoader`](ResourceLoader.md) on this weak path registry |
| General loader/saver formats and serialized stored-property discovery | Deferred until concrete file-format and dependency integrations exist |

No placeholder members are exposed for deferred domains.

## Dependencies and interactions

`Resource` depends on `ElectronObject`, `PropertyDescriptor`, `DeepDuplicateMode`, Scene's `Node` type for local-scene association, cryptographic random generation, weak references, and standard collections. Scene's packed-scene component reciprocally consumes Resource duplication; ADR 0023 records this narrow in-assembly cycle. [`Image`](Image.md) derives from `Resource` and supplies its own managed pixel-state synchronization and duplication. `Resource` has no dependency on `PackedScene`, `SceneTree`, SDL, renderer, native handle, physics, file serializer, editor, or scripting.

Pure managed `Resource` instances are reclaimed by the runtime. `Dispose` performs deterministic logical teardown; future derived resources that own native handles must release them deterministically through safe-handle wrappers. No allocation-free or hard real-time guarantee is claimed for arbitrary resource construction, copying, or user callbacks.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies defaults, property descriptors, event rules, validation, concurrent ID generation, path conflict/transfer/raw-cache/disposal behavior, concurrent path claims, built-in classification, setup ordering and failure aggregation, every duplication mode, forced and forbidden nested duplication, shallow and typed-container semantics, aliases, cycles, external resources, serialized concurrent copy/reset/coalescing behavior, unsupported and invalid factories, partial-graph rollback, packed-scene local duplication/aliasing/root association/setup/ownership, and access after disposal.

The first synchronous image-texture file loader uses the existing weak path cache; it does not own loaded resources or introduce native-payload leases. There is no general asset loader/saver, import pipeline, scene/resource file format, renderer RID, editor path-ID table, or automatic reflection-based discovery. In-memory packed scenes implement automatic scene-local behavior, but derived resources still implement typed copying explicitly.

Engine consumers can detect content changes through an internal monotonic revision advanced by EmitChanged before public observers, including inside notification-coalescing batches. This preserves Changed ordering and permits retained controls to recover when an earlier observer throws. It is not a public version or serialization identity. Custom resource authors still report meaningful mutations through EmitChanged.
