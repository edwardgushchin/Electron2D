# ConfigKey<T>

Last updated: 2026-09-21

## Declaration

- Source: [`ConfigFile.cs`](../../src/Core/IO/ConfigFile.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class ConfigKey<T>`
- Domain: [Core](../domains/core.md)
- Component: [Configuration files](../components/config-files.md)

## Responsibility and ownership

`ConfigKey<T>` is an immutable, reusable identity for one entry in a [`ConfigFile`](ConfigFile.md). It binds a case-sensitive section and entry name to the compile-time value type used for serialization and deserialization. The key owns no configuration value or native resource and does not require disposal.

The empty section addresses entries before the first section header. Entry names must be non-null and nonempty. Section and entry names may otherwise contain arbitrary Unicode text; unsafe text-format characters are quoted by `ConfigFile`.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `ConfigKey(string section, string name)` | Validates the identifier and value type, then stores the immutable section/name pair |
| `string Section { get; }` | Returns the case-sensitive section name, including the empty section |
| `string Name { get; }` | Returns the nonempty case-sensitive entry name |
| `string ToString()` | Returns `section/name`, or only `name` for a sectionless key |

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
- Empty name: `ArgumentException`.
- Universal, document-model, delegate, or engine-object value type: `NotSupportedException`.

## Dependencies and interactions

The key depends only on Core type validation. `ConfigFile` consumes it for lookup, mutation, removal, and deserialization diagnostics.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` verifies null/empty identifier rejection, forbidden value types, sectionless and sectioned diagnostic strings, typed round trips, and incompatible typed lookup. The key does not carry a default value, validator, feature override, or change event; those belong to [`ProjectSetting<T>`](ProjectSetting.Generic.md) and [`ProjectSettings`](ProjectSettings.md), not the generic file format.
