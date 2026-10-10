# ResourceLoader

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Declaration:** `public sealed partial class Electron2D.ResourceLoader`. **Source:** [ResourceLoader.cs](../../src/Core/IO/ResourceLoader.cs).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Loads supported resource files through the engine's typed resource path cache.

Supported resources include registered typed archives/format extensions, image textures, dynamic fonts, WAV/MP3/Ogg audio and certificate/private-key files. Static operations use a permanent retained service. The returned resource belongs to the caller and is cached weakly while it remains live. Synchronous load operations serialize cache decisions. File roots own newly decoded dependency graphs; the weak cache owns no resources.

See [typed resource files](../components/resource-files.md) for the complete storage, type/factory, graph, UID, cache, failure, ownership and thread contract.

## Example

This public excerpt requires its existing file/directory or the named application extension types; ResourceArchiveTests supplies executable complete producers and consumers.

```csharp
using var scene = ResourceLoader.Load<PackedScene>("user://level.e2dscene");
using var root = scene.Instantiate();
using var tree = new SceneTree(root);
tree.ProcessFrame(0);
```

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public static System.Void AddResourceFormatLoader(Electron2D.ResourceFormatLoader formatLoader, System.Boolean atFront = false)` | Registers a borrowed typed file loader without duplicating identity. |
| `public static System.Boolean Exists<TResource>(System.String path)` | Reports whether a supported resource file exists or is already cached. |
| `public static TResource GetCachedRef<TResource>(System.String path)` | Gets a live cached resource of the requested type, or null when absent. |
| `public static System.Type[] GetClassesUsed(System.String path)` | Returns compiled types used by a recognized resource file. |
| `public static System.String[] GetDependencies(System.String path, System.Boolean addTypes = false)` | Returns external dependency tokens from a recognized format. |
| `public static System.String[] GetRecognizedExtensionsForType<TResource>()` | Gets caller-owned lowercase filename extensions supported for a resource type. |
| `public static System.Int64 GetResourceUID(System.String path)` | Reports the UID stored by a recognized file format. |
| `public static System.Boolean HasCached(System.String path)` | Reports whether any live registered resource occupies an exact cache path. |
| `public static TResource Load<TResource>(System.String path, Electron2D.ResourceLoader.CacheMode cacheMode = Reuse)` | Loads a supported typed resource from an operating-system, res:// or user:// path. |
| `public static System.Void RemoveResourceFormatLoader(Electron2D.ResourceFormatLoader formatLoader)` | Removes loader registration without disposing its object. |
| `public static System.Void RenameDependencies(System.String path, System.Collections.Generic.IReadOnlyDictionary<System.String, System.String> renames)` | Rewrites recognized external dependency paths without instantiating resource objects. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-cc670a01db39"></a>
### AddResourceFormatLoader

`public static System.Void AddResourceFormatLoader(Electron2D.ResourceFormatLoader formatLoader, System.Boolean atFront = false)`

Registers a borrowed typed file loader without duplicating identity.

- `formatLoader`: Live format extension.
- `atFront`: Whether it precedes existing extensions.

<a id="member-2250e1b0dad0"></a>
### Exists

`public static System.Boolean Exists<TResource>(System.String path)`

Reports whether a supported resource file exists or is already cached.

The cache is checked first, so a cached resource may outlive removal of its source file.

Returns: True when a live cached instance has this type, or a recognized resource file exists for it.

- `path`: Exact cache path or file path.

- `TResource`: Requested resource type or compatible base type.

- `System.ArgumentException`: The path is null or empty.

<a id="member-884bec15ba40"></a>
### GetCachedRef

`public static TResource GetCachedRef<TResource>(System.String path)`

Gets a live cached resource of the requested type, or null when absent.

Retrieval does not extend ownership; the resource's owner controls disposal.

Returns: A borrowed live instance, or null when the path is absent or holds another type.

- `path`: Ordinal, case-sensitive cache path.

- `TResource`: Resource or one of its concrete derived types.

- `System.ArgumentException`: The path is null or empty.

<a id="member-b317089970c5"></a>
### GetClassesUsed

`public static System.Type[] GetClassesUsed(System.String path)`

Returns compiled types used by a recognized resource file.

Returns: Copied type census; empty when unrecognized.

- `path`: Source file.

<a id="member-a75cda62f9d1"></a>
### GetDependencies

`public static System.String[] GetDependencies(System.String path, System.Boolean addTypes = false)`

Returns external dependency tokens from a recognized format.

Returns: Copied ordered dependencies.

- `path`: Source file.
- `addTypes`: Whether to append stable type IDs.

<a id="member-bb8993f82127"></a>
### GetRecognizedExtensionsForType

`public static System.String[] GetRecognizedExtensionsForType<TResource>()`

Gets caller-owned lowercase filename extensions supported for a resource type.

Returns: The supported extensions without dots, or an empty array for unsupported types.

- `TResource`: Requested resource type or compatible base type.

<a id="member-5bb87d60e949"></a>
### GetResourceUID

`public static System.Int64 GetResourceUID(System.String path)`

Reports the UID stored by a recognized file format.

Returns: UID or InvalidID.

- `path`: Source file.

<a id="member-cbe43c14b72e"></a>
### HasCached

`public static System.Boolean HasCached(System.String path)`

Reports whether any live registered resource occupies an exact cache path.

Returns: True for a live registered resource, including a resource registered outside this loader.

- `path`: Ordinal, case-sensitive cache path.

- `System.ArgumentException`: The path is null or empty.

<a id="member-b8dbcdb9271d"></a>
### Load

`public static TResource Load<TResource>(System.String path, Electron2D.ResourceLoader.CacheMode cacheMode = Reuse)`

Loads a supported typed resource from an operating-system, res:// or user:// path.

Returns: A caller-owned live resource. Reuse and Replace can return the same cached instance.

- `path`: File path; the exact path string is the cache key.
- `cacheMode`: Whether to reuse, ignore or refresh an existing live instance.

- `TResource`: Registered archive/plugin resource, ImageTexture, FontFile, implemented AudioStream, X509Certificate, CryptoKey, or an assignable resource base type.

- `System.ArgumentException`: The path or cache mode is invalid.
- `System.NotSupportedException`: The resource type or file extension is unsupported.
- `System.InvalidOperationException`: Reuse finds a different resource type at the path.
- `System.IO.IOException`: The file cannot be read.
- `System.FormatException`: Encoded audio is malformed or outside its verified channel profile.
- `System.IO.InvalidDataException`: The encoded resource is malformed or exceeds supported limits.
- `System.Security.Cryptography.CryptographicException`: Certificate/key input is malformed or does not match the requested key role.

<a id="member-bfd4bf710af5"></a>
### RemoveResourceFormatLoader

`public static System.Void RemoveResourceFormatLoader(Electron2D.ResourceFormatLoader formatLoader)`

Removes loader registration without disposing its object.

- `formatLoader`: Extension identity.

<a id="member-6531d0f124e7"></a>
### RenameDependencies

`public static System.Void RenameDependencies(System.String path, System.Collections.Generic.IReadOnlyDictionary<System.String, System.String> renames)`

Rewrites recognized external dependency paths without instantiating resource objects.

- `path`: Source archive or plugin file.
- `renames`: Old-to-new path map.

<a id="member-119f8d90a0d8"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Lifecycle, failures and verification

Permanent service objects cannot be disposed or unregistered; format extensions are caller-owned and borrowed only while registered. Factories must return fresh exact live identities; node factories are detached, without caller-owned tree membership. Unknown schemas, incompatible property/resource types, invalid flags/UIDs, missing files and corrupt payloads reject explicitly. Metadata, registration, snapshots, save/load and scene instantiation allocate outside the frame interval. File cache publication follows complete decoding; arbitrary custom copy or observer failure follows Resource commitment rules. Read the component for concrete bounds and remaining payload integrations.

ResourceArchiveTests exercises registered public formats, graph/scene persistence, separate-process lifecycle, UID/dependency rewrite, cache replacement, font/theme/pixel/geometry consumers and rollback/lifetime edges on Linux x64. It does not establish editor, rendered archive scenes, foreign-host/AOT, all-resource payloads, unmeasured native allocations or human acceptance.

## Bitmap/indexed font integration

[Bitmap font authoring](../components/bitmap-fonts.md) connects FontFile indexed image/glyph/kerning/metric records and matching configured FontVariation resources to the existing HarfBuzz and common canvas/control path. Copied pixel UV regions preserve clipping and recorded image snapshots; authored publication retires native data after active readers finish. Text/binary v3 import, typed archive/fresh-process restoration and current Linux GPU/compatibility prepared output are exercised. Source policies and other platform/native-allocator gates remain explicit.

## Threaded loading

[Threaded resource loading](../components/threaded-resource-loading.md) implements typed requests, monotonic progress/status and blocking collection. `ResourceLoader.ThreadLoadStatus` keeps values InvalidResource=0, InProgress=1, Failed=2 and Loaded=3. Error results use typed exceptions.

| Declaration | Behavior |
| --- | --- |
| `LoadThreadedRequest<TResource>(string path, bool useSubThreads = false, CacheMode cacheMode = CacheMode.Reuse, CancellationToken cancellationToken = default, SceneTree? publicationTree = null)` | Retains one matching request; identical duplicates share work and require matching gets. An explicit tree or the active engine scene owns publication. |
| `LoadThreadedGetStatus(string path)` | Reports current request status; absent/consumed paths are InvalidResource. |
| `LoadThreadedGetStatus(string path, out float progress)` | Reports monotonic zero-through-one progress. Owner polls may publish ready data without waiting on the cache gate. |
| `LoadThreadedGet(string path)` | Waits and releases one matching request, returning the published Resource or rethrowing failure after cleanup. |
| `LoadThreadedGet<TResource>(string path)` | Checks an assignable concrete type; an incompatible get does not consume the request. |

Every accepted request requires get, including failure/cancellation. Conflicting duplicate options reject. There are at most 128 uncollected request paths. Preparation is allocating explicit work; at most 1024 file records and depth 64 are permitted. Independent external dependencies genuinely execute on workers with useSubThreads. Their temporary identities do not appear in the path cache.

Cache modes preserve compatible identities, root cycles, aliases and ownership. Existing resources and Changed callbacks update on the publication owner. SceneTree.Defer and owner polls publish ready graphs; until then status is InProgress. Without a tree the consuming status/get caller publishes. An unpublished scene result cannot be consumed off owner, and a load callback cannot recursively consume another unpublished threaded request. A publication callback also rejects self-collection, preserving its matching consumers. Worker collection can help a specifically awaited older root task with active-stack checks.

CancellationToken is cooperative between stages and before publication. Blocked hooks finish before cleanup; no forced thread abort is used. Caller formats/factories must be thread-safe, return independent owned state and support stored/opaque reference remapping. Root and new dependencies are retained until publication/rollback; borrowed existing cache resources remain borrowed.

```csharp
ResourceLoader.LoadThreadedRequest<PackedScene>(path, useSubThreads: true,
    cancellationToken: cancellation.Token, publicationTree: tree);
// Poll during normal frames; consume when Loaded or Failed.
var status = ResourceLoader.LoadThreadedGetStatus(path, out float progress);
if (status is ResourceLoader.ThreadLoadStatus.Loaded or ResourceLoader.ThreadLoadStatus.Failed)
{
    using var scene = ResourceLoader.LoadThreadedGet<PackedScene>(path);
    root.AddChild(scene.Instantiate());
}
```

A successful scene instance retains its decoded resource graph after template disposal. [AsyncGallery](../../examples/AsyncGallery/README.md) demonstrates fresh-process file authoring, two background image dependencies and actual owner-thread scene consumption. Foreign/AOT/Web bootstrap, native allocations and human acceptance remain separate gates.
