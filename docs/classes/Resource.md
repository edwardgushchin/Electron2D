# Resource

[Performance.ObjectResourceCount](Performance.Monitor.md) counts current live registered path owners, excluding uncached resources and unregistered aliases.

Last updated: 2026-10-21

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** [Theme](Theme.md), [StyleBox](StyleBox.md), [Gradient](Gradient.md), [Curve](Curve.md), [Curve2D](Curve2D.md), [Image](Image.md), [InputEvent](InputEvent.md), [PackedScene](PackedScene.md), [SpriteFrames](SpriteFrames.md), [AnimationNodeStateMachinePlayback](AnimationNodeStateMachinePlayback.md), [AnimationNodeStateMachineTransition](AnimationNodeStateMachineTransition.md)

- **Source:** [`src/Core/IO/Resource.cs`](../../src/Core/IO/Resource.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Resource : ElectronObject`

> Provides reusable data, change notification, path identity, and typed duplication for engine assets.

## Forced scene resource graphs

The existing internal SceneDuplicationScope now resolves forced subresource copies through its shared DuplicationSession, preserving aliases and tracking created resources for PackedScene-owned cleanup. Skeleton uses this only for its stack graph so each instance has independent mutable scene binding even under shallow copying.

Automatic pre-delete cancellation retains ResourcePath registration and its namespace reservation. During the notification, ordinary cache lookup hides the closed resource without deleting its entry; competing ordinary assignment cannot steal the path. An explicit TakeOverPath remains an intentional ownership transfer.

## Description

Provides reusable data, change notification, path identity, and typed duplication for engine assets.

`Resource` is the managed base for reusable engine data. It owns resource naming, optional process-wide path identity, scene-instancing configuration, scene serialization identity, synchronous change notification, reset/setup hooks, and typed graph-preserving duplication.

The managed runtime owns object memory. Deterministic logical cleanup remains available through inherited `IDisposable`; no public reference counter is exposed. A future asset manager may track active leases internally to retain and release native-backed asset payloads, but that counter will not control managed object collection. A resource does not own nested resources returned by a derived class and does not dispose them during its own cleanup.

Resource lifetime uses the managed runtime together with [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose); no public reference
counter is exposed. Base state is safe for concurrent reads and serialized writes, but derived resource state and
callbacks have no implicit synchronization or thread affinity.

Stored base state includes inherited `ElectronObject.CanTranslateMessages` and `TranslationDomain` together with resource name and locality. Shallow/deep duplicates, scene-local resource graphs and `CopyFromResource` retain that policy. A successful copy publishes its coalesced `Changed` event after inherited settings and custom stored state are applied. Path/scene identity and runtime subscribers retain their separate ownership rules.

Ordinary nested `Script` references retain the same asset under every deep-copy mode, including built-in paths. Force-copy fields and explicit Script root copies create independent wrappers; they do not replace ordinary references even when visited first. Scene instantiation shares non-local ordinary scripts and duplicates local scripts once per scene. Isolated authoring capture keeps independent source snapshots.

Every resource edge selects sharing or duplication before cached targets are used. A prior forced or root copy cannot change an ordinary shared reference. Only selected duplicated edges remap aliases/cycles; shallow/None references and external references under Internal remain original, including a reference back to the source root. All-mode and built-in Internal cycles remap to acquired roots. Scene scopes likewise keep non-local ordinary resources shared regardless of force order.

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
| [`public string GetIDForPath(string path)`](#m-electron2d-resource-getidforpath-system-string) | Returns this current resource path's cached ID in a referring file. |
| [`public void SetIDForPath(string path, string id)`](#m-electron2d-resource-setidforpath-system-string-system-string) | Stores an opaque ID or clears that exact path pair. |
| [`public Node GetLocalScene()`](#m-electron2d-resource-getlocalscene) | Gets the root node whose scene instance owns this scene-local resource. |
| [`public void CopyFromResource(Resource source)`](#m-electron2d-resource-copyfromresource-electron2d-resource) | Copies stored data from another resource of the exact same runtime type while preserving this resource's path and scene ID.

Shape targets preflight all borrowers and geometry/access hooks before reset, base name/locality, file ownership or change batching. A rejected operation leaves all those values and subscribers unchanged; ordinary allowed copying retains its existing non-transactional callback-failure behavior. |
| [`public Resource Duplicate(bool deep = false)`](#m-electron2d-resource-duplicate-system-boolean) | Creates a shallow or internally deep duplicate of this resource. |
| [`public Resource DuplicateDeep(DeepDuplicateMode subresourceMode = DeepDuplicateMode.Internal)`](#m-electron2d-resource-duplicatedeep-electron2d-deepduplicatemode) | Creates a deep duplicate with explicit nested-resource policy. |
| [`public void EmitChanged()`](#m-electron2d-resource-emitchanged) | Synchronously reports that this resource's meaningful content changed. |
| [`public static string GenerateSceneUniqueID()`](#m-electron2d-resource-generatesceneuniqueid) | Generates a compact scene-relative resource identifier. |
| [`public void ResetState()`](#m-electron2d-resource-resetstate) | Clears non-stored state through [`Resource.OnResetState`](Resource.md#m-electron2d-resource-onresetstate). |
| [`public void SetPathCache(string path)`](#m-electron2d-resource-setpathcache-system-string) | Sets the path value without registering it in the process-wide resource cache. |
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

Path hooks run after metadata/cache commitment; hook failures propagate without rolling back that commitment. Registered equal writes are silent, while promoting an equal raw path must still claim cache ownership. Cache entries are weak and ordinal; disposal or collection releases their ownership. Path changes do not automatically emit content `Changed`.

<a id="p-electron2d-resource-resourcesceneuniqueid"></a>
### `public string ResourceSceneUniqueID { get; set; }`

Gets or sets the identifier used when this resource is embedded in a serialized scene.

**Value:** An empty string, or an identifier containing only ASCII letters, digits, and underscores.

**Exceptions**

- `ArgumentException`: The assigned value contains a character outside ASCII letters, digits, and underscores.
- `ArgumentNullException`: The assigned value is `null`.
- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.

**Remarks:** Assignments do not raise [`Resource.Changed`](Resource.md#e-electron2d-resource-changed). Successful graph saving generates missing IDs and resolves duplicate IDs after atomic persistence; the first encountered authored ID is preserved.

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

**Remarks:** The association is assigned before [`Resource.OnSetupLocalToScene`](Resource.md#m-electron2d-resource-onsetuplocaltoscene) runs. It borrows immutable weak object identity without retaining the hierarchy; root disposal or collection makes the query return null.

<a id="m-electron2d-resource-copyfromresource-electron2d-resource"></a>
### `public void CopyFromResource(Resource source)`

Copies stored data from another resource of the exact same runtime type while preserving this resource's path and scene ID.

**Parameters**

- `source`: The live resource whose stored data is copied.

**Exceptions**

- `ArgumentException`: `source` has a different runtime type.
- `ArgumentNullException`: `source` is `null`.
- `ObjectDisposedException`: Either resource is disposing or disposed.
- `InvalidOperationException`: An engine resource authoring policy rejects copying before state or notifications change.
- `Exception`: A reset, copy, setter, or final change handler fails.

**Remarks:** Copying is shallow: nested resources and collection instances remain shared unless a derived override explicitly
defines other behavior. [`Resource.ResetState`](Resource.md#m-electron2d-resource-resetstate) runs first. Change notifications raised while copying are
coalesced into one final [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) event. Batches targeting the same resource are serialized, but
derived state still requires caller coordination. The operation is not transactional if a derived callback fails. Reset/copy failure still attempts one final event; operation and observer failures are aggregated in that order. Nested copies coalesce at the outer boundary, while a final observer can begin a new balanced batch.

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

**Remarks:** The result is probabilistically unique; graph saving reserves authored IDs and resolves collisions before persistence.

<a id="m-electron2d-resource-resetstate"></a>
### `public void ResetState()`

Clears non-stored state through [`Resource.OnResetState`](Resource.md#m-electron2d-resource-onresetstate).

**Exceptions**

- `ObjectDisposedException`: The resource is disposing on another thread or has finished disposing.
- `InvalidOperationException`: An engine resource authoring policy rejects the reset before its hook runs.
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

Equal raw writes also invoke this hook. The general path-change hook is then attempted even if the raw hook fails; both errors aggregate, and the committed raw metadata stays unregistered.

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
Construct the target during the factory invocation. A returned pre-existing resource is rejected before base/custom
copying or ownership acquisition, including a different-type object or a target already created for another graph source.
Newly constructed invalid targets still belong to rollback cleanup. A partial graph failure releases its acquired targets
while preserving caller-owned source and rejected pre-existing objects.

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

[`PackedScene`](PackedScene.md) creates one graph-preserving duplication session per scene instance. A directly stored resource is duplicated when `ResourceLocalToScene` is true. Within a duplicated local graph, nested resources are duplicated according to their local flag; non-local resources remain shared for both built-in and external paths. Ordinary deep duplication retains its separate built-in/internal policy. Repeated references and cycles resolve to the same duplicate.

Every local duplicate receives the new root through `GetLocalScene()` before setup begins. The current `OnSetupLocalToScene()` hook runs once for each local duplicate. After successful setup, the root adopts every resource created by the session, including explicit force-duplicated resources. `Node.ReplaceBy` transfers those created resources and local-scene associations to the replacement root. Root disposal disposes owned resources after child-node cleanup and clears each resource's local-scene reference through resource disposal. Failed instantiation disposes the partial duplicate graph instead. Shared non-local resources are never owned or disposed by the instance.

## Per-referrer authoring IDs

`GetIDForPath(string path)` reads and `SetIDForPath(string path, string id)`
updates a process-wide cache keyed by the exact ordinal pair `(path, ResourcePath)`.
No path normalization occurs. Empty paths are allowed, missing entries return
empty and an empty ID removes the pair. IDs are opaque non-null strings; they
are independent of `ResourceSceneUniqueID`, its validation and numeric resource
UIDs. Null arguments and disposed resources throw before cache mutation.

Aliases/reloads and explicit path takeover share IDs by path rather than object
identity. Changing a resource path selects another key without moving old IDs.
Disposal, reset and copying do not erase those keys; entries retain strings only
until explicitly cleared or process exit. Cache reads/writes serialize with path
assignment and publish no content event. Format extensions explicitly save and
restore IDs; the cache itself is not serialized. This authoring operation is
available without an editor UI under ADR 0013. See the [executable format and
physics checks](../components/resources.md#per-referrer-path-ids).

<a id="m-electron2d-resource-getidforpath-system-string"></a>
### `public string GetIDForPath(string path)`

Returns the current resource path's cached ID for the exact referring `path`, or
empty when absent. The call samples the current `ResourcePath` atomically with
cache lookup.

<a id="m-electron2d-resource-setidforpath-system-string-system-string"></a>
### `public void SetIDForPath(string path, string id)`

Stores a nonempty `id`, or removes the current path pair for an empty `id`.
Neither path ownership nor `ResourceSceneUniqueID` changes. Custom format code
should publish newly generated labels only after its successful file commit.

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
| Deprecated setup method/event | Excluded under ADR 0004; automatic scene instancing invokes the current protected hook |
| `_reset_state`, `_set_path_cache`, `_setup_local_to_scene` | Implemented as protected typed hooks |
| `copy_from`, `duplicate`, `duplicate_deep`, `emit_changed`, `generate_scene_unique_id`, `is_built_in`, `reset_state`, `set_path_cache`, `take_over_path` | Implemented with the typed adaptations documented above; explicit hook delegates replace reflective always/never-duplicate property flags |
| `_get_rid`, `get_rid` | Typed virtual GetRID executes for base managed resources, physics Shape and rendering Texture; material/shader identities remain incomplete |
| `get_local_scene` and automatic local-to-scene duplication/setup | Implemented for in-memory `PackedScene`; root association precedes setup and persists until resource disposal |
| `get_id_for_path`, `set_id_for_path` | Implemented as exact path-pair authoring metadata; available without editor UI under ADR 0013 |
| Synchronous loader cache modes for image textures | Implemented through typed [`ResourceLoader`](ResourceLoader.md) on this weak path registry |
| General loader/saver formats and serialized stored-property discovery | Deferred until concrete file-format and dependency integrations exist |

No placeholder members are exposed for deferred domains.

## Dependencies and interactions

`Resource` depends on `ElectronObject`, `PropertyDescriptor`, `DeepDuplicateMode`, Scene's `Node` type for local-scene association, cryptographic random generation, weak references, and standard collections. Scene's packed-scene component reciprocally consumes Resource duplication; ADR 0023 records this narrow in-assembly cycle. [`Image`](Image.md) derives from `Resource` and supplies its own managed pixel-state synchronization and duplication. `Resource` has no dependency on `PackedScene`, `SceneTree`, SDL, renderer, native handle, physics, file serializer, editor, or scripting.

Pure managed `Resource` instances are reclaimed by the runtime. `Dispose` performs deterministic logical teardown; future derived resources that own native handles must release them deterministically through safe-handle wrappers. No allocation-free or hard real-time guarantee is claimed for arbitrary resource construction, copying, or user callbacks.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies defaults, property descriptors, event rules, validation, concurrent ID generation, path conflict/transfer/raw-cache/disposal behavior, concurrent path claims, built-in classification, setup ordering and failure aggregation, every duplication mode, forced and forbidden nested duplication, shallow and typed-container semantics, aliases, cycles, external resources, serialized concurrent copy/reset/coalescing behavior, unsupported and invalid factories, partial-graph rollback, packed-scene local duplication/aliasing/root association/setup/ownership, and access after disposal.

The first synchronous image-texture file loader uses the existing weak path cache; it does not own loaded resources or introduce native-payload leases. The typed archive loader/saver persists registered stored schemas and scene graphs. General import pipeline, further resource payload schemas, renderer material/shader RID ownership, automatic reflection-based discovery and broader editor/import workflows retain their owning prerequisites. In-memory packed scenes implement automatic scene-local behavior, but derived resources still implement typed copying explicitly.

Engine consumers can detect content changes through an internal monotonic revision advanced by EmitChanged before public observers, including inside notification-coalescing batches. This preserves Changed ordering and permits retained controls to recover when an earlier observer throws. It is not a public version or serialization identity. Custom resource authors still report meaningful mutations through EmitChanged.

## Backend resource identity

| Signature | Contract |
| --- | --- |
| `public virtual RID GetRID()` | Empty for a base managed resource; concrete backend roles override typed RID dispatch. |

<a id="getrid"></a>
**GetRID:** The base resource has no backend identity and returns empty after its disposal guard. C# overrides replace the source dispatcher/hook pair. [Shape.GetRID](Shape.md#getrid) supplies stable physics shape identity; [Texture.GetRID](Texture.md#getrid) supplies stable borrowed rendering identity. Material/shader registration remains incomplete. This is a Partial family-wide projection, with an executable base/physics/texture contract under [ADR 0088](../decisions/physics-shape-slots.md#adr-0088). [PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) checks empty base dispatch and concrete shape identity.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

AudioBusLayout and AudioServer use the existing private file graph retention to preserve internally decoded effects after the root layout is disposed. External dependencies remain borrowed; retention never serializes native processing state.

## Prepared reference publication

The protected `RemapResourceReferences(Func<Resource, Resource> remap)` hook maps freshly prepared references to final cached/new identities before owner publication under ADR 0013. Resource defaults to stored resource descriptors; PackedScene includes its node model; Shader includes default texture bindings. Opaque application storage overrides this hook and calls base for descriptor state. Primitive descriptors are not rewritten by the default remapper. This is a typed graph-publication extension, not a dynamic property API.

Shape targets use the same authoring preflight for standalone ResetState. CopyFromResource invokes the reset hook once under its already accepted preflight; denied calls enter neither reset hooks nor change batching. Base Resources retain their ordinary reset/copy policy.

World targets also apply a copy-specific preflight to the incoming runtime before reset, base-state, source ownership or Changed effects. This preserves the existing standalone reset policy and permits copies between aliases of one bound runtime.

The final active-body cohort also passes strict current-source NativeAOT publication and six native CPU/GPU/registered ordinary/separate profiles. Seven managed profiles cover the same local scenes plus base/packed/replacement/isolated authoring; the existing complete CPU/GPU physical suites pass against the identical retained runtime. Each of twelve local brackets drives two worlds with active rigid integration at 3 units/s and dt 1/60, position-increment tolerance 0.00005 for float accumulation, live queries and borrowed root reads: 128 warm/128 measured intervals remain 0/0 owner/all-thread managed bytes. Four native full-callback/PackedScene profiles also pass at zero warm bytes. GPU uses lavapipe only; separate local profiles observe two real solver threads for the two worlds. Source/runtime/ELF manifests and exact limits are retained in bin/physics-local-scenes/2026-10-14/validation.json.

Graph persistence stages scene-relative IDs for every internal subresource, preserves the first authored owner, avoids every reserved authored ID and repairs missing/later duplicate IDs. Successful writes commit IDs independently of ReplaceSubresourcePaths; that flag changes source paths. Failed encoding or writes preserve IDs and paths. After persistence all IDs commit before path hooks; later paths are attempted even when an earlier callback fails. Readers reject ambiguous embedded paths, including legacy numeric fallback collisions, before constructing resources. Per-referrer authoring IDs are available separately through GetIDForPath/SetIDForPath; custom formats own their persistence.

The current resource-identity cohort passes eight managed profiles and six strict NativeAOT profiles, including twelve real fresh-process physical scene consumers with changed local creation order. Repeated saves retain corrected IDs, names and physical aliases; failed writes preserve source metadata, and malformed resolved paths reject before factories/cache replacement. Twelve brackets warm 128 and measure 128 active full frames with queries and ID reads at 0/0 owner/all-thread managed bytes. GPU runs use explicit lavapipe; hardware performance and native allocations remain separate gates. Exact manifests/results are retained in bin/physics-resource-identity/2026-10-14/validation.json.

Inherited [ordinary event blocking](../classes/ElectronObject.md#ordinary-event-delivery) suppresses new event emissions at the publisher, preserving subscriptions and already accepted deferred work. Notifications, virtual callbacks, committed physical state and resource revisions continue; Disposed cleanup remains independent.

Meaningful EmitChanged revisions conservatively invalidate an active renderer even when signals are blocked or a copy batch defers publication. Off-owner resource publication remains queued/observed through existing scene ownership. Ordinary Image snapshots are copied data; retained texture producers publish their own resource changes. See [rendering change producers](../components/low-processor-rendering.md).

## Backend identities

GetRID is the typed virtual dispatcher for the current backend role. The managed base, CPU Image and PhysicsMaterial return empty. Concrete shapes, textures/arrays, meshes/instances, occluder contours, materials/shaders and font sources resolve their existing owning registrations. AtlasTexture forwards its backing source, and font projections preserve their source/service identities. Runtime identities are not stored resource properties. Rendering ownership, server-created lifetimes, source retirement and capability requirements remain those of each concrete resource.

The resource identifier alone does not transfer ownership. In particular, unused caller-owned shape registrations are weak; actual Body/Area/query consumers retain their source. See [borrowed identity checks](../components/physics-shape-data.md#borrowed-resource-identity-lifetime).

Factory results require [construction provenance](ElectronObject.md#factory-construction-provenance) in the current invocation. Unrelated concurrent construction cannot transfer caller ownership; nested factories, ordinary Task context flow and engine worker tasks remain supported.
