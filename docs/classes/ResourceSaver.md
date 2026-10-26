# ResourceSaver

Last updated: 2026-10-26

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.ResourceSaver`. **Source:** [ResourceSaver.cs](../../src/Core/IO/ResourceSaver.cs).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Saves typed resources with ordered registered formats and portable archive defaults.

Static operations use permanent retained state. Format registration borrows caller-owned savers. Encoding, file I/O, UID discovery and snapshots allocate; no save operation is a frame-loop primitive.

See [typed resource files](../components/resource-files.md) for the complete storage, type/factory, graph, UID, cache, failure, ownership and thread contract.

## Example

This public excerpt requires its existing file/directory or the named application extension types; ResourceArchiveTests supplies executable complete producers and consumers.

```csharp
using var root = new Node { Name = "Level" };
using var scene = new PackedScene();
scene.Pack(root);
ResourceSaver.Save(scene, "user://level.e2dscene", SaverFlags.Compress);
```

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public static System.Void AddResourceFormatSaver(Electron2D.ResourceFormatSaver formatSaver, System.Boolean atFront = false)` | Registers a borrowed saver without duplicating its identity. |
| `public static System.String[] GetRecognizedExtensions(Electron2D.Resource resource)` | Returns recognized save extensions in format priority order. |
| `public static System.Int64 GetResourceIDForPath(System.String path, System.Boolean generate = false)` | Reads a persisted UID or prepares a path UID when requested. |
| `public static System.Void RemoveResourceFormatSaver(Electron2D.ResourceFormatSaver formatSaver)` | Removes borrowed registration without disposing the saver. |
| `public static System.Void Save(Electron2D.Resource resource, System.String path = "", Electron2D.SaverFlags flags = None)` | Saves a resource using the first successful recognized saver. |
| `public static System.Void SaveScene(Electron2D.Node root, System.String path = "", Electron2D.SaverFlags flags = None)` | Performs live-scene authoring notifications, RESET-aware packing, saving and cleanup. |
| `public static System.Void SetUID(System.String resource, System.Int64 uid)` | Changes a recognized persisted file UID. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-f3c5746662a4"></a>
### AddResourceFormatSaver

`public static System.Void AddResourceFormatSaver(Electron2D.ResourceFormatSaver formatSaver, System.Boolean atFront = false)`

Registers a borrowed saver without duplicating its identity.

- `formatSaver`: Live extension.
- `atFront`: Whether it precedes existing formats.

<a id="member-0d783c0b36e9"></a>
### GetRecognizedExtensions

`public static System.String[] GetRecognizedExtensions(Electron2D.Resource resource)`

Returns recognized save extensions in format priority order.

Returns: A copied ordered extension census.

- `resource`: Source resource.

<a id="member-e8fa4138202b"></a>
### GetResourceIDForPath

`public static System.Int64 GetResourceIDForPath(System.String path, System.Boolean generate = false)`

Reads a persisted UID or prepares a path UID when requested.

Returns: UID or ResourceUID.InvalidID.

- `path`: Resource path.
- `generate`: Whether an absent UID is generated and registered.

<a id="member-4e75aa8f0327"></a>
### RemoveResourceFormatSaver

`public static System.Void RemoveResourceFormatSaver(Electron2D.ResourceFormatSaver formatSaver)`

Removes borrowed registration without disposing the saver.

- `formatSaver`: Extension identity.

<a id="member-92d1e9a616e6"></a>
### Save

`public static System.Void Save(Electron2D.Resource resource, System.String path = "", Electron2D.SaverFlags flags = None)`

Saves a resource using the first successful recognized saver.

ChangePath is temporary and restored even after failure. A built-in archive encode replaces a destination only after complete validation/encoding.

- `resource`: Caller-owned live resource.
- `path`: Explicit destination, or empty selects ResourcePath.
- `flags`: Known persistence flags.

<a id="member-91112cda2cbc"></a>
### SetUID

`public static System.Void SetUID(System.String resource, System.Int64 uid)`

Changes a recognized persisted file UID.

- `resource`: Existing resource path.
- `uid`: Nonnegative identity.

<a id="member-405ae4361a3d"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Lifecycle, failures and verification

Permanent service objects cannot be disposed or unregistered; format extensions are caller-owned and borrowed only while registered. Factories must return fresh exact live identities; node factories are detached, without caller-owned tree membership. Unknown schemas, incompatible property/resource types, invalid flags/UIDs, missing files and corrupt payloads reject explicitly. Metadata, registration, snapshots, save/load and scene instantiation allocate outside the frame interval. File cache publication follows complete decoding; arbitrary custom copy or observer failure follows Resource commitment rules. Read the component for concrete bounds and remaining payload integrations.

ResourceArchiveTests exercises registered public formats, graph/scene persistence, separate-process lifecycle, UID/dependency rewrite, cache replacement, font/theme/pixel/geometry consumers and rollback/lifetime edges on Linux x64. It does not establish editor, rendered archive scenes, foreign-host/AOT, all-resource payloads, unmeasured native allocations or human acceptance.

ShaderInclude source (`.hlslinc`/`.glslinc`) participates in the existing ordered format and cache lifecycle. Source saves use atomic replacement; typed archives store Code. See [ShaderInclude](ShaderInclude.md) for byte limits and build-time dependency consumption.

## Live-scene authoring

`ResourceSaver.SaveScene(root, "user://level.e2dscene")` is the explicit authoring operation. It sends parent-first Node.NotificationEditorPreSave (9001), captures the owned scene using PackForSave, writes through the existing registered saver and updates the root SceneFilePath on successful write. Node.NotificationEditorPostSave (9002) is attempted once pre-save delivery has begun, even after callback/capture/write failure. Internal/unowned descendants receive notifications although owner filtering still decides what is persisted. Temporary packed data retires before post-save cleanup.

Input, owner and capture-access rejection before pre-save sends no notifications. Same-root recursive/concurrent saves reject without a second lifecycle. Callback changes are application-owned; post-save callbacks can restore temporary state. A post-save or origin-observer error does not roll back an already committed file. Independent operation/retirement/cleanup exceptions retain their identity and order. Plain Save, Pack and PackForSave retain their existing notification-free behavior. See [the executable contract](../components/resource-files.md#live-scene-authoring-save).

[Version-8 material recovery](../components/material-placeholders.md) preserves typed missing-material fields and graphs for compiled restoration, with strict default loading, legacy readers and explicit missing-payload provenance. See [resource-file layout](../components/resource-files.md#version-8-material-recovery).
