# SaverFlags

Last updated: 2026-10-05

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.SaverFlags`. **Source:** [SaverFlags.cs](../../src/Core/IO/SaverFlags.cs).

**Inherits:** None (static registry or enum).

## Description

Selects portable resource-file dependency, path and binary policies.



See [typed resource files](../components/resource-files.md) for the complete storage, type/factory, graph, UID, cache, failure, ownership and thread contract.

## Example

This public excerpt requires its existing file/directory or the named application extension types; ResourceArchiveTests supplies executable complete producers and consumers.

```csharp
SaverFlags flags = SaverFlags.Compress | SaverFlags.SaveBigEndian | SaverFlags.BundleResources;
ResourceSaver.Save(scene, "user://level.e2dscene", flags);
```

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.SaverFlags BundleResources = 2` | Embeds external resources into this archive. |
| `public const Electron2D.SaverFlags ChangePath = 4` | Temporarily exposes the destination ResourcePath while saving, then restores it. |
| `public const Electron2D.SaverFlags Compress = 32` | Compresses the archive payload with bounded Deflate. |
| `public const Electron2D.SaverFlags None = 0` | Uses ordinary absolute dependency paths and little-endian uncompressed data. |
| `public const Electron2D.SaverFlags OmitEditorProperties = 8` | Omits reserved editor metadata properties. |
| `public const Electron2D.SaverFlags RelativePaths = 1` | Stores external dependency paths relative to the destination. |
| `public const Electron2D.SaverFlags ReplaceSubresourcePaths = 64` | Assigns internal subresource paths after a successful archive replacement. |
| `public const Electron2D.SaverFlags SaveBigEndian = 16` | Stores numeric payload values with big-endian byte ordering. |

## Enumeration Descriptions

<a id="member-d59e3ab69e39"></a>
### BundleResources

`public const Electron2D.SaverFlags BundleResources = 2`

Embeds external resources into this archive.

<a id="member-5beba5d37047"></a>
### ChangePath

`public const Electron2D.SaverFlags ChangePath = 4`

Temporarily exposes the destination ResourcePath while saving, then restores it.

<a id="member-a1b581f370f8"></a>
### Compress

`public const Electron2D.SaverFlags Compress = 32`

Compresses the archive payload with bounded Deflate.

<a id="member-eaf06f57447f"></a>
### None

`public const Electron2D.SaverFlags None = 0`

Uses ordinary absolute dependency paths and little-endian uncompressed data.

<a id="member-4fd8de6b57c9"></a>
### OmitEditorProperties

`public const Electron2D.SaverFlags OmitEditorProperties = 8`

Omits reserved editor metadata properties.

<a id="member-96945f4606cb"></a>
### RelativePaths

`public const Electron2D.SaverFlags RelativePaths = 1`

Stores external dependency paths relative to the destination.

<a id="member-3bb459c1bd8f"></a>
### ReplaceSubresourcePaths

`public const Electron2D.SaverFlags ReplaceSubresourcePaths = 64`

Assigns internal subresource paths after a successful archive replacement.

<a id="member-d84d20fcd326"></a>
### SaveBigEndian

`public const Electron2D.SaverFlags SaveBigEndian = 16`

Stores numeric payload values with big-endian byte ordering.

## Lifecycle, failures and verification

Permanent service objects cannot be disposed or unregistered; format extensions are caller-owned and borrowed only while registered. Factories must return fresh exact live identities; node factories are detached, without caller-owned tree membership. Unknown schemas, incompatible property/resource types, invalid flags/UIDs, missing files and corrupt payloads reject explicitly. Metadata, registration, snapshots, save/load and scene instantiation allocate outside the frame interval. File cache publication follows complete decoding; arbitrary custom copy or observer failure follows Resource commitment rules. Read the component for concrete bounds and remaining payload integrations.

ResourceArchiveTests exercises registered public formats, graph/scene persistence, separate-process lifecycle, UID/dependency rewrite, cache replacement, font/theme/pixel/geometry consumers and rollback/lifetime edges on Linux x64. It does not establish editor, rendered archive scenes, foreign-host/AOT, all-resource payloads, unmeasured native allocations or human acceptance.
