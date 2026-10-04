# ResourceUID

Last updated: 2026-10-05

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.ResourceUID`. **Source:** [ResourceUID.cs](../../src/Core/IO/ResourceUID.cs).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Maintains typed portable resource identities and their registered file paths.

Static operations use a permanent retained identity/path catalog. Saved archives retain UIDs; importing or loading registers them in each process. This catalog owns no resources.

See [typed resource files](../components/resource-files.md) for the complete storage, type/factory, graph, UID, cache, failure, ownership and thread contract.

## Example

This public excerpt requires its existing file/directory or the named application extension types; ResourceArchiveTests supplies executable complete producers and consumers.

```csharp
long id = ResourceUID.CreateID();
ResourceUID.AddID(id, "res://level.e2dscene");
string text = ResourceUID.IDToText(id);
string path = ResourceUID.EnsurePath(text);
ResourceUID.RemoveID(id);
```

## Constant summary

| Complete C# signature | Contract |
| --- | --- |
| `public const System.Int64 InvalidID = -1` | Identifies an absent or invalid UID. |

## Constant Descriptions

<a id="member-3f399669e1b4"></a>
### InvalidID

`public const System.Int64 InvalidID = -1`

Identifies an absent or invalid UID.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public static System.Void AddID(System.Int64 id, System.String path)` | Registers a previously absent identity. |
| `public static System.Int64 CreateID()` | Generates a nonnegative identity absent from the registered catalog. |
| `public static System.Int64 CreateIDForPath(System.String path)` | Derives a stable identity from the path and current project name. |
| `public static System.String EnsurePath(System.String pathOrUID)` | Resolves UID notation while preserving ordinary paths. |
| `public static System.String GetIDPath(System.Int64 id)` | Reads a registered identity's path. |
| `public static System.Boolean HasID(System.Int64 id)` | Reports registered identity membership. |
| `public static System.String IDToText(System.Int64 id)` | Formats a nonnegative UID in portable uid:// notation. |
| `public static System.String PathToUID(System.String path)` | Converts a known path to portable UID notation and preserves unknown paths. |
| `public static System.Void RemoveID(System.Int64 id)` | Removes one registered identity. |
| `public static System.Void SetID(System.Int64 id, System.String path)` | Assigns or replaces an identity's path. |
| `public static System.Int64 TextToID(System.String textID)` | Parses portable identity text. |
| `public static System.String UIDToPath(System.String uid)` | Resolves UID notation to a path and preserves ordinary paths. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-f60def9d9bdc"></a>
### AddID

`public static System.Void AddID(System.Int64 id, System.String path)`

Registers a previously absent identity.

- `id`: Nonnegative UID.
- `path`: Resource path.

<a id="member-7d4f4294eb97"></a>
### CreateID

`public static System.Int64 CreateID()`

Generates a nonnegative identity absent from the registered catalog.

Returns: A random portable UID; registration remains explicit.

<a id="member-4f858998ee44"></a>
### CreateIDForPath

`public static System.Int64 CreateIDForPath(System.String path)`

Derives a stable identity from the path and current project name.

Returns: A deterministic nonnegative UID.

- `path`: Portable resource path.

<a id="member-65042e6b81f2"></a>
### EnsurePath

`public static System.String EnsurePath(System.String pathOrUID)`

Resolves UID notation while preserving ordinary paths.

Returns: Registered path; unknown tokens throw.

- `pathOrUID`: Path or UID token.

<a id="member-4d015dc872ce"></a>
### GetIDPath

`public static System.String GetIDPath(System.Int64 id)`

Reads a registered identity's path.

Returns: The registered path.

- `id`: UID.

<a id="member-05976352f2de"></a>
### HasID

`public static System.Boolean HasID(System.Int64 id)`

Reports registered identity membership.

Returns: Whether it is known.

- `id`: UID.

<a id="member-432342ed66d7"></a>
### IDToText

`public static System.String IDToText(System.Int64 id)`

Formats a nonnegative UID in portable uid:// notation.

Returns: Portable identity text.

- `id`: UID or InvalidID.

<a id="member-ae79fc0b01e6"></a>
### PathToUID

`public static System.String PathToUID(System.String path)`

Converts a known path to portable UID notation and preserves unknown paths.

Returns: UID notation when a registered identity exists.

- `path`: Resource path or UID notation.

<a id="member-284938983c15"></a>
### RemoveID

`public static System.Void RemoveID(System.Int64 id)`

Removes one registered identity.

- `id`: Known UID.

<a id="member-8848c8a0df7f"></a>
### SetID

`public static System.Void SetID(System.Int64 id, System.String path)`

Assigns or replaces an identity's path.

- `id`: Nonnegative UID.
- `path`: Resource path.

<a id="member-3bb7d0cd256a"></a>
### TextToID

`public static System.Int64 TextToID(System.String textID)`

Parses portable identity text.

Returns: UID or InvalidID for malformed/overflow input.

- `textID`: uid:// token.

<a id="member-467113dd155f"></a>
### UIDToPath

`public static System.String UIDToPath(System.String uid)`

Resolves UID notation to a path and preserves ordinary paths.

Returns: Resolved path.

- `uid`: Path or UID notation.

<a id="member-a1d36228b998"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Lifecycle, failures and verification

Permanent service objects cannot be disposed or unregistered; format extensions are caller-owned and borrowed only while registered. Factories must return fresh exact live identities; node factories are detached, without caller-owned tree membership. Unknown schemas, incompatible property/resource types, invalid flags/UIDs, missing files and corrupt payloads reject explicitly. Metadata, registration, snapshots, save/load and scene instantiation allocate outside the frame interval. File cache publication follows complete decoding; arbitrary custom copy or observer failure follows Resource commitment rules. Read the component for concrete bounds and remaining payload integrations.

ResourceArchiveTests exercises registered public formats, graph/scene persistence, separate-process lifecycle, UID/dependency rewrite, cache replacement, font/theme/pixel/geometry consumers and rollback/lifetime edges on Linux x64. It does not establish editor, rendered archive scenes, foreign-host/AOT, all-resource payloads, unmeasured native allocations or human acceptance.
