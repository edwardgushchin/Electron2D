# ResourceFormatSaver

Last updated: 2026-10-05

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.ResourceFormatSaver`. **Source:** [ResourceFormatSaver.cs](../../src/Core/IO/ResourceFormatSaver.cs).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Defines a caller-owned typed resource-file saving extension.

ResourceSaver borrows registered instances. Hooks execute synchronously on the saving thread; implementations own encoding/atomic replacement and typed failure semantics.

See [typed resource files](../components/resource-files.md) for the complete storage, type/factory, graph, UID, cache, failure, ownership and thread contract.

## Example

This public excerpt requires its existing file/directory or the named application extension types; ResourceArchiveTests supplies executable complete producers and consumers.

```csharp
using var saver = new NameSaver();
ResourceSaver.AddResourceFormatSaver(saver, atFront: true);
try { using var value = new Resource { ResourceName = "example" }; ResourceSaver.Save(value, "user://name.name"); }
finally { ResourceSaver.RemoveResourceFormatSaver(saver); }
// NameSaver implements recognition/extensions/save, as in ResourceArchiveTests.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected ResourceFormatSaver()` | Constructs a typed saver extension. |

## Constructor Descriptions

<a id="member-bcb1a9b6d152"></a>
### .ctor

`protected ResourceFormatSaver()`

Constructs a typed saver extension.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public abstract System.String[] GetRecognizedExtensions(Electron2D.Resource resource)` | Returns copied format extensions without dots. |
| `public abstract System.Boolean Recognize(Electron2D.Resource resource)` | Reports whether this saver handles the typed resource. |
| `public virtual System.Boolean RecognizePath(Electron2D.Resource resource, System.String path)` | Reports whether this saver handles a resource/destination pair. |
| `public abstract System.Void Save(Electron2D.Resource resource, System.String path, Electron2D.SaverFlags flags)` | Saves one typed resource or throws its concrete failure. |
| `public virtual System.Void SetUID(System.String path, System.Int64 uid)` | Replaces the persisted UID without changing resource content. |

## Method Descriptions

<a id="member-73fb6364e977"></a>
### GetRecognizedExtensions

`public abstract System.String[] GetRecognizedExtensions(Electron2D.Resource resource)`

Returns copied format extensions without dots.

Returns: Recognized extensions.

- `resource`: Typed resource.

<a id="member-d7d8686cf16a"></a>
### Recognize

`public abstract System.Boolean Recognize(Electron2D.Resource resource)`

Reports whether this saver handles the typed resource.

Returns: Whether it is recognized.

- `resource`: Typed resource.

<a id="member-32d3ba762616"></a>
### RecognizePath

`public virtual System.Boolean RecognizePath(Electron2D.Resource resource, System.String path)`

Reports whether this saver handles a resource/destination pair.

Returns: Extension recognition by default.

- `resource`: Typed resource.
- `path`: Destination path.

<a id="member-2070f97d11ad"></a>
### Save

`public abstract System.Void Save(Electron2D.Resource resource, System.String path, Electron2D.SaverFlags flags)`

Saves one typed resource or throws its concrete failure.

- `resource`: Source resource.
- `path`: Destination path.
- `flags`: Persistence policies.

<a id="member-24475da696b3"></a>
### SetUID

`public virtual System.Void SetUID(System.String path, System.Int64 uid)`

Replaces the persisted UID without changing resource content.

- `path`: Existing destination.
- `uid`: Nonnegative resource UID.

## Lifecycle, failures and verification

Permanent service objects cannot be disposed or unregistered; format extensions are caller-owned and borrowed only while registered. Factories must return fresh exact live identities; node factories are detached, without caller-owned tree membership. Unknown schemas, incompatible property/resource types, invalid flags/UIDs, missing files and corrupt payloads reject explicitly. Metadata, registration, snapshots, save/load and scene instantiation allocate outside the frame interval. File cache publication follows complete decoding; arbitrary custom copy or observer failure follows Resource commitment rules. Read the component for concrete bounds and remaining payload integrations.

ResourceArchiveTests exercises registered public formats, graph/scene persistence, separate-process lifecycle, UID/dependency rewrite, cache replacement, font/theme/pixel/geometry consumers and rollback/lifetime edges on Linux x64. It does not establish editor, rendered archive scenes, foreign-host/AOT, all-resource payloads, unmeasured native allocations or human acceptance.
