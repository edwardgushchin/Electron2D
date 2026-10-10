# ResourceFormatLoader

Last updated: 2026-10-05

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.ResourceFormatLoader`. **Source:** [ResourceFormatLoader.cs](../../src/Core/IO/ResourceFormatLoader.cs).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Defines a caller-owned typed resource-file loading extension.

ResourceLoader borrows ordered registration. Hooks execute on the loading thread and return actual caller-owned resources; dependency and cache contracts remain explicit.

See [typed resource files](../components/resource-files.md) for the complete storage, type/factory, graph, UID, cache, failure, ownership and thread contract.

## Example

This public excerpt requires its existing file/directory or the named application extension types; ResourceArchiveTests supplies executable complete producers and consumers.

```csharp
using var loader = new NameLoader();
ResourceLoader.AddResourceFormatLoader(loader, atFront: true);
try { using var value = ResourceLoader.Load<Resource>("user://name.name"); }
finally { ResourceLoader.RemoveResourceFormatLoader(loader); }
// NameLoader supplies the matching typed format hooks.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected ResourceFormatLoader()` | Constructs a managed format extension. |

## Constructor Descriptions

<a id="member-e684fe15e779"></a>
### .ctor

`protected ResourceFormatLoader()`

Constructs a managed format extension.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public virtual System.Boolean Exists(System.String path)` | Reports recognized existing-file availability. |
| `public virtual System.Type[] GetClassesUsed(System.String path)` | Returns compiled types used by this file. |
| `public virtual System.String[] GetDependencies(System.String path, System.Boolean addTypes = false)` | Returns portable external resource dependencies. |
| `public abstract System.String[] GetRecognizedExtensions()` | Returns copied supported extensions without dots. |
| `public virtual System.String GetResourceScriptClass(System.String path)` | Returns the compiled script-class identity stored by the format. |
| `public abstract System.Type GetResourceType(System.String path)` | Determines the file's exact resource type without constructing resources. |
| `public virtual System.Int64 GetResourceUID(System.String path)` | Reports the stored resource UID. |
| `public abstract System.Boolean HandlesType(System.Type type)` | Reports support for a requested concrete/base resource type. |
| `public abstract Electron2D.Resource Load(System.String path, System.String originalPath, System.Boolean useSubThreads, Electron2D.ResourceLoader.CacheMode cacheMode)` | Loads a recognized resource file or throws its concrete failure. |
| `public virtual System.Boolean RecognizePath(System.String path, System.Type type = null)` | Reports path/type support. |
| `public virtual System.Void RenameDependencies(System.String path, System.Collections.Generic.IReadOnlyDictionary<System.String, System.String> renames)` | Rewrites external dependency paths atomically. |

## Method Descriptions

<a id="member-325b36509109"></a>
### Exists

`public virtual System.Boolean Exists(System.String path)`

Reports recognized existing-file availability.

Returns: Native file existence by default.

- `path`: Source path.

<a id="member-c83a0fa02da7"></a>
### GetClassesUsed

`public virtual System.Type[] GetClassesUsed(System.String path)`

Returns compiled types used by this file.

Returns: Copied exact type census.

- `path`: Source path.

<a id="member-b88ee06bbd84"></a>
### GetDependencies

`public virtual System.String[] GetDependencies(System.String path, System.Boolean addTypes = false)`

Returns portable external resource dependencies.

Returns: Copied dependency tokens.

- `path`: Source path.
- `addTypes`: Whether to append stable type identity to dependency tokens.

<a id="member-60c75e66c834"></a>
### GetRecognizedExtensions

`public abstract System.String[] GetRecognizedExtensions()`

Returns copied supported extensions without dots.

Returns: Format extensions.

<a id="member-afa3c9e8f881"></a>
### GetResourceScriptClass

`public virtual System.String GetResourceScriptClass(System.String path)`

Returns the compiled script-class identity stored by the format.

Returns: Empty when no script identity exists.

- `path`: Source path.

<a id="member-695fbce38c01"></a>
### GetResourceType

`public abstract System.Type GetResourceType(System.String path)`

Determines the file's exact resource type without constructing resources.

Returns: Exact resource type, or null when unrecognized.

- `path`: Source file.

<a id="member-0af68ed96800"></a>
### GetResourceUID

`public virtual System.Int64 GetResourceUID(System.String path)`

Reports the stored resource UID.

Returns: InvalidID when no UID is retained.

- `path`: Source path.

<a id="member-0b0df0bfc7b9"></a>
### HandlesType

`public abstract System.Boolean HandlesType(System.Type type)`

Reports support for a requested concrete/base resource type.

Returns: Whether the type is supported.

- `type`: CLR resource type.

<a id="member-0e92bad45f07"></a>
### Load

`public abstract Electron2D.Resource Load(System.String path, System.String originalPath, System.Boolean useSubThreads, Electron2D.ResourceLoader.CacheMode cacheMode)`

Loads a recognized resource file or throws its concrete failure.

Returns: A caller-owned resource.

- `path`: Resolved source path.
- `originalPath`: Caller path identity.
- `useSubThreads`: Whether dependency preparation may use subthreads; implementations may stay synchronous.
- `cacheMode`: Root/dependency cache policy.

<a id="member-f60b2f782587"></a>
### RecognizePath

`public virtual System.Boolean RecognizePath(System.String path, System.Type type = null)`

Reports path/type support.

Returns: Extension/type recognition by default.

- `path`: Source path.
- `type`: Requested resource type; null means no hint.

<a id="member-69d1813e600f"></a>
### RenameDependencies

`public virtual System.Void RenameDependencies(System.String path, System.Collections.Generic.IReadOnlyDictionary<System.String, System.String> renames)`

Rewrites external dependency paths atomically.

- `path`: Source file.
- `renames`: Old-to-new path mapping.

## Lifecycle, failures and verification

Permanent service objects cannot be disposed or unregistered; format extensions are caller-owned and borrowed only while registered. Factories must return fresh exact live identities; node factories are detached, without caller-owned tree membership. Unknown schemas, incompatible property/resource types, invalid flags/UIDs, missing files and corrupt payloads reject explicitly. Metadata, registration, snapshots, save/load and scene instantiation allocate outside the frame interval. File cache publication follows complete decoding; arbitrary custom copy or observer failure follows Resource commitment rules. Read the component for concrete bounds and remaining payload integrations.

ResourceArchiveTests exercises registered public formats, graph/scene persistence, separate-process lifecycle, UID/dependency rewrite, cache replacement, font/theme/pixel/geometry consumers and rollback/lifetime edges on Linux x64. It does not establish editor, rendered archive scenes, foreign-host/AOT, all-resource payloads, unmeasured native allocations or human acceptance.

## Threaded preparation

Threaded requests may invoke borrowed format hooks concurrently. Retain each registered loader until its pending requests are collected, return independent owned payload and keep registered resources/scene/native state out of worker preparation. The hook receives the selected subthread/cache options; typed ResourceLoader dependency calls join the staged graph.
