# ConfigKey\<T\>

Last updated: 2026-09-24

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/IO/ConfigFile.cs`](../../src/Core/IO/ConfigFile.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class ConfigKey<T>`

> Identifies one strongly typed value in a [`ConfigFile`](ConfigFile.md).

## Description

Identifies one strongly typed value in a [`ConfigFile`](ConfigFile.md).

`ConfigKey<T>` is an immutable, reusable identity for one entry in a [`ConfigFile`](ConfigFile.md). It binds a case-sensitive section and entry name to the compile-time value type used for serialization and deserialization. The key owns no configuration value or native resource and does not require disposal.

The empty section addresses entries before the first section header. Section and entry names must be non-null and may be empty. Empty entry names are quoted by `ConfigFile` when encoded; other unsafe text-format characters are quoted as well.

Reuse one key instance for each logical setting. The empty section addresses entries before the first section header.
Values are serialized with the declared type rather than a runtime-wide universal value container.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var fullscreen = new ConfigKey<bool>("display", "fullscreen");
using var config = new ConfigFile();
config.SetValue(fullscreen, true);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public ConfigKey<T>(string section, string name)`](#m-electron2d-configkey-1-ctor-system-string-system-string) | Initializes a typed configuration key. |

## Properties

| Member | Description |
| --- | --- |
| [`public string Section { get; }`](#p-electron2d-configkey-1-section) | Gets the case-sensitive section name. |
| [`public string Name { get; }`](#p-electron2d-configkey-1-name) | Gets the case-sensitive entry name. |

## Methods

| Member | Description |
| --- | --- |
| [`public override string ToString()`](#m-electron2d-configkey-1-tostring) | Returns the section and entry name for diagnostics. |

## Constructor Descriptions

<a id="m-electron2d-configkey-1-ctor-system-string-system-string"></a>
### `public ConfigKey<T>(string section, string name)`

Initializes a typed configuration key.

**Parameters**

- `section`: The case-sensitive section name, or an empty string for a sectionless entry.
- `name`: The case-sensitive entry name, which may be empty.

**Exceptions**

- `ArgumentNullException`: `section` or `name` is `null`.
- `NotSupportedException`: `T` is an untyped JSON DOM value, `Object`, a delegate, or an engine object.

## Property Descriptions

<a id="p-electron2d-configkey-1-section"></a>
### `public string Section { get; }`

Gets the case-sensitive section name.

**Value:** The section name, or an empty string for a sectionless entry.

<a id="p-electron2d-configkey-1-name"></a>
### `public string Name { get; }`

Gets the case-sensitive entry name.

**Value:** The entry name, which may be empty.

## Method Descriptions

<a id="m-electron2d-configkey-1-tostring"></a>
### `public override string ToString()`

Returns the section and entry name for diagnostics.

**Returns:** `section/name`, or only the entry name for a sectionless key.

## Type contract

`T` may be any concrete value or model that the built-in `System.Text.Json` serializer can encode and decode with public properties/fields. The following deliberately fail during key construction:

- `object`, including inside arrays and generic type arguments, because it would recreate a universal untyped value;
- `JsonElement`, `JsonDocument`, and `JsonNode` types, because they are untyped document models;
- delegates;
- `ElectronObject` and every derived engine type, because configuration does not own or serialize live engine objects.

Other unsupported serializer shapes fail when a value is assigned or decoded. A stored JSON token has no embedded CLR type name; callers must reuse the same logical typed key. Constructing two keys for the same section/name with incompatible `T` values is allowed structurally, but incompatible decoding fails with `InvalidDataException`.

## Lifecycle, invariants, and threading

- Construction either succeeds completely or throws without creating a usable key.
- `Section`, `Name`, and the declared type never change.
- The type contains no mutable state and is safe to share between threads.
- Equality is ordinary reference equality; callers should normally keep one static key instance per setting.

## Error behavior

- Null section/name: `ArgumentNullException`.
- Universal, document-model, delegate, or engine-object value type: `NotSupportedException`.

## Dependencies and interactions

The key depends only on Core type validation. `ConfigFile` consumes it for lookup, mutation, removal, and deserialization diagnostics.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` verifies null/empty identifier rejection, forbidden value types, sectionless and sectioned diagnostic strings, typed round trips, and incompatible typed lookup. The key does not carry a default value, validator, feature override, or change event; those belong to [`ProjectSetting<T>`](ProjectSetting.Generic.md) and [`ProjectSettings`](ProjectSettings.md), not the generic file format.
