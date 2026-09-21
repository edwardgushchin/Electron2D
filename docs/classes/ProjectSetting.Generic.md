# ProjectSetting<T>

Last updated: 2026-09-21

## Declaration

- Source: [`ProjectSettings.cs`](../../src/Core/Config/ProjectSettings.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class ProjectSetting<T> where T : notnull`
- Domain: [Core](../domains/core.md)
- Component: [Project settings](../components/project-settings.md)

## Responsibility and ownership

`ProjectSetting<T>` is the immutable public identity of one project-wide value. It binds a full slash-separated, ordinal case-sensitive name to a compile-time value type, a serialized default snapshot, and an optional validator. The same instance is intentionally reusable across isolated [`ProjectSettings`](ProjectSettings.md) registries.

The type owns no file, engine object, or disposable resource. It caches only the most recently decoded scalar/string value; reference-shaped values are decoded for every read so callers cannot mutate a stored default or registry value through an alias.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `ProjectSetting(string name, T defaultValue, Func<T, bool>? validator = null)` | Validates the category path and value type, snapshots the non-null default through the configuration serializer, and requires the optional validator to accept it |
| `string Name { get; }` | Full case-sensitive path; requires at least one `/`; forbids empty segments, whitespace, control characters, backslashes, and periods |
| `Type ValueType { get; }` | Exact `typeof(T)` |
| `T DefaultValue { get; }` | Fresh deserialized default for reference-shaped values; safe cached return for scalar/string values |

Periods are reserved for persisted feature-override suffixes and therefore cannot occur in a base setting name. `object`, JSON DOM types, delegates, `ElectronObject` types, and containers recursively containing those types are rejected under the same boundary as [`ConfigKey<T>`](ConfigKey.Generic.md).

## Lifecycle, invariants, and errors

Construction is the only state transition. A successful instance is immutable and thread-safe. Null defaults are rejected even when `T` is annotated inconsistently by a caller. Serialization failure propagates without creating an instance. A validator returning false raises `ArgumentOutOfRangeException`; a validator exception propagates unchanged.

The validator may run during construction, assignment, registration against previously loaded data, and transactional load. It runs synchronously on the caller's thread and may run concurrently, so it must be deterministic, thread-safe, and free of registry mutations. During a `ProjectSettings` load an attempted registry mutation is rejected so a failed validator cannot partially replace the active document.

## Dependencies and interactions

The type reuses the serializer and type-safety rules of [`ConfigFile`](ConfigFile.md). A registry initially uses `DefaultValue` as its initial/revert value, but can later replace that per-registry initial value without mutating the shared definition. Registration, persistence, feature overrides, metadata, unsaved-value tracking, path resolution, and change events belong to `ProjectSettings` rather than this identity object.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` verifies malformed names, forbidden types, rejected defaults/writes, exact-definition identity, reuse across registries, and mutable default/read isolation. There is no public custom serializer or equality comparer; values must have a stable representation supported by the built-in JSON serializer.
