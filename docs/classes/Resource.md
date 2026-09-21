# Resource

Last updated: 2026-09-21

## Declaration

- Source: [`Resource.cs`](../../src/Core/IO/Resource.cs)
- Namespace: `Electron2D`
- Declaration: `public class Resource : ElectronObject`
- Domain: [Resources](../domains/resources.md)
- Component: [Resource base](../components/resources.md)

## Responsibility and ownership

`Resource` is the managed base for reusable engine data. It owns resource naming, optional process-wide path identity, scene-instancing configuration, scene serialization identity, synchronous change notification, reset/setup hooks, and typed graph-preserving duplication.

The managed runtime owns object memory. Deterministic logical cleanup remains available through inherited `IDisposable`; no public reference counter is exposed. A future asset manager may track active leases internally to retain and release native-backed asset payloads, but that counter will not control managed object collection. A resource does not own nested resources returned by a derived class and does not dispose them during its own cleanup.

## Public properties and association query

| Member | Current behavior |
| --- | --- |
| `bool ResourceLocalToScene { get; set; }` | Defaults to `false`; requests per-instance duplication when reached through a packed scene; assignment does not emit `Changed` |
| `string ResourceName { get; set; }` | Non-null display name, empty by default; every successful assignment synchronously emits `Changed`, including an equal value |
| `string ResourcePath { get; set; }` | Non-null ordinal path; a nonempty registered path has exactly one live owner process-wide |
| `string ResourceSceneUniqueId { get; set; }` | Empty or an ASCII identifier containing letters, digits, and underscores; assignment does not emit `Changed` |
| `bool IsBuiltIn { get; }` | `true` for an empty path, a path containing `::`, or a path starting with `local://`; otherwise `false` |
| `Node? GetLocalScene()` | Returns the instantiated scene root assigned to a scene-local duplicate before setup, or `null` otherwise |

All four mutable properties appear in the inherited typed property list. A rejected assignment is non-mutating. An invalid scene ID throws instead of silently replacing caller data with a generated value.

## Events

| Event | Current behavior |
| --- | --- |
| `Changed` | Synchronous sender-only event published by `EmitChanged`, every `ResourceName` assignment, and one coalesced completion of `CopyFromResource` |
| `SetupLocalToSceneRequested` | Obsolete compatibility event raised immediately before `OnSetupLocalToScene` for a scene-local duplicate |

A throwing `Changed` handler stops later handlers and propagates after the triggering state mutation is committed. `SetupLocalToScene` attempts both the compatibility event and virtual hook; if both fail, it throws an `AggregateException` containing both errors.

## Public methods

| Member | Current behavior |
| --- | --- |
| `CopyFromResource(Resource source)` | Requires the exact same runtime type, resets target non-stored state, shallow-copies base/custom stored state, preserves the target path and scene ID, and coalesces changes into one event; self-copy is a no-op |
| `Duplicate(bool deep = false)` | Creates a new exact-type instance; shallow mode shares containers/resources, deep mode clones typed containers and duplicates built-in nested resources |
| `DuplicateDeep(DeepDuplicateMode mode = Internal)` | Creates a deep duplicate under an explicit nested-resource policy |
| `EmitChanged()` | Publishes `Changed` synchronously or marks one pending publication inside an internal copy batch |
| `static GenerateSceneUniqueId()` | Uses a cryptographic process-safe generator to return five characters from `a`-`y` and `0`-`8`; collision detection remains a serializer responsibility |
| `ResetState()` | Invokes `OnResetState` without changing stored base properties or publishing `Changed` by itself |
| `SetPathCache(string path)` | Removes this resource's registered path, commits a raw unregistered path value, then invokes `OnPathCacheSet`; duplicate visible paths are allowed |
| `SetupLocalToScene()` | Obsolete manual infrastructure entry point that raises its compatibility event and then invokes `OnSetupLocalToScene`; packed scenes invoke the same sequence automatically |
| `TakeOverPath(string path)` | Atomically transfers a registered path to this resource and clears a displaced live owner |
| `ToString()` | Returns optional name, current path, runtime class, and instance ID for diagnostics |

`CopyFromResource` is deliberately non-transactional for derived state. If reset or custom copying fails, it still publishes one final change notification because state may be partially changed. If that notification also fails, both errors are aggregated.

## Protected extension API

| Member | Contract |
| --- | --- |
| `CreateDuplicateInstance()` | Must return a fresh, live, exact-runtime-type default instance with empty path and scene ID |
| `CopyCustomStateTo(...)` | Must copy every stored derived field; clones typed containers when `deep` is true; uses the policy delegate for ordinary nested resources, the force delegate for always-duplicate properties, and direct assignment for never-duplicate properties |
| `OnResetState()` | Clears derived non-stored state |
| `OnPathCacheSet(string path)` | Observes a committed raw cache path |
| `OnSetupLocalToScene()` | Customizes a newly duplicated scene-local resource; `GetLocalScene()` is already available |
| `GetPropertyDescriptors()` | Appends the four resource descriptors to inherited descriptors |
| `Dispose(bool disposing)` | Unregisters the path and clears resource subscribers before inherited cleanup |

The base duplication hooks support an exact `Resource`. Every derived type must override both duplication hooks explicitly, even if it adds no fields. This prevents a new derived field from being silently omitted. Invalid factory results and every partially created graph member are disposed on failure; cleanup failures are aggregated with the original error.

## Duplication invariants

- Root duplication always creates a new root, regardless of its path.
- A duplicate never inherits `ResourcePath` or `ResourceSceneUniqueId`.
- `ResourceName` and `ResourceLocalToScene` are copied.
- Shallow duplication shares collection containers and nested resources.
- Deep duplication delegates typed-container copying to the derived class and applies `DeepDuplicateMode` to nested resources.
- A typed field may force graph-preserving duplication under any mode or retain its original reference under every mode.
- Repeated references to a duplicated resource point to one duplicate.
- A cycle back to an already duplicated resource points to its duplicate even when that original resource has an external path.
- A failed duplication exposes no live partial result and disposes all created resources in reverse creation order.

## Scene-local lifecycle and ownership

[`PackedScene`](PackedScene.md) creates one graph-preserving duplication session per scene instance. A directly stored resource is duplicated when `ResourceLocalToScene` is true. Within a duplicated local graph, nested resources are duplicated when local-to-scene or built-in; external non-local resources remain shared. Repeated references and cycles resolve to the same duplicate.

Every local duplicate receives the new root through `GetLocalScene()` before setup begins. The obsolete compatibility event runs before `OnSetupLocalToScene()` and each local duplicate is set up once. After successful setup, the root adopts every resource created by the session, including non-local built-in duplicates reached inside the graph. Root disposal disposes those resources after child-node cleanup and clears each resource's local-scene reference through resource disposal. Failed instantiation disposes the partial duplicate graph instead. Shared source/external resources are never owned or disposed by the instance.

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
| Loader/saver cache modes and serialized stored-property discovery | Deferred until asset loading and serialization exist |

No placeholder members are exposed for deferred domains.

## Dependencies and interactions

`Resource` depends on `ElectronObject`, `PropertyDescriptor`, `DeepDuplicateMode`, Scene's `Node` type for local-scene association, cryptographic random generation, weak references, and standard collections. Scene's packed-scene component reciprocally consumes Resource duplication; ADR 0023 records this narrow in-assembly cycle. `Resource` has no dependency on `PackedScene`, `SceneTree`, SDL, renderer, native handle, physics, file serializer, editor, or scripting.

Pure managed `Resource` instances are reclaimed by the runtime. `Dispose` performs deterministic logical teardown; future derived resources that own native handles must release them deterministically through safe-handle wrappers. No allocation-free or hard real-time guarantee is claimed for arbitrary resource construction, copying, or user callbacks.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies defaults, property descriptors, event rules, validation, concurrent ID generation, path conflict/transfer/raw-cache/disposal behavior, concurrent path claims, built-in classification, setup ordering and failure aggregation, every duplication mode, forced and forbidden nested duplication, shallow and typed-container semantics, aliases, cycles, external resources, serialized concurrent copy/reset/coalescing behavior, unsupported and invalid factories, partial-graph rollback, packed-scene local duplication/aliasing/root association/setup/ownership, and access after disposal.

There is no asset loader/saver, import pipeline, scene/resource file format, renderer RID, editor path-ID table, or automatic reflection-based discovery. In-memory packed scenes implement automatic scene-local behavior, but derived resources still implement typed copying explicitly.
