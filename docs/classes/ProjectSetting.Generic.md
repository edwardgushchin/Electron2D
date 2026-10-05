# ProjectSetting\<T\>

Last updated: 2026-10-05

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Config/ProjectSettings.cs`](../../src/Core/Config/ProjectSettings.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class ProjectSetting<T>`

> Identifies one strongly typed project-wide setting and its default value.

## Description

Identifies one strongly typed project-wide setting and its default value.

`ProjectSetting<T>` is the immutable public identity of one project-wide value. It binds a full slash-separated, ordinal case-sensitive name to a compile-time value type, a serialized default snapshot, and an optional validator. The same instance is intentionally reusable across isolated [`ProjectSettings`](ProjectSettings.md) registries.

The type owns no file, engine object, or disposable resource. It caches only the most recently decoded scalar/string value; reference-shaped values are decoded for every read so callers cannot mutate a stored default or registry value through an alias.

Reuse one instance for each logical setting. Names are case-sensitive paths such as
`application/config/name`. Values are serialized snapshots, so mutable values returned from
[`ProjectSetting`1.DefaultValue`](ProjectSetting.Generic.md#p-electron2d-projectsetting-1-defaultvalue) do not mutate the stored default. A validator can run concurrently on caller threads;
it must therefore be deterministic, thread-safe, and free of registry mutations.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var difficulty = new ProjectSetting<int>("game/difficulty", 1);
int current = difficulty.DefaultValue;
```

## Constructors

| Member | Description |
| --- | --- |
| [`public ProjectSetting<T>(string name, T defaultValue, Func<T, bool> validator = null)`](#m-electron2d-projectsetting-1-ctor-system-string-0-system-func-0-system-boolean) | Initializes a strongly typed project setting. |
| `public ProjectSetting<T>(string name, T defaultValue, Func<T, bool>? validator, JsonTypeInfo<T>? typeInfo)` | Carries compiled custom-model metadata through defaults, validation, base values and feature overrides. |

## Properties

| Member | Description |
| --- | --- |
| [`public string Name { get; }`](#p-electron2d-projectsetting-1-name) | Gets the full setting path. |
| [`public Type ValueType { get; }`](#p-electron2d-projectsetting-1-valuetype) | Gets the declared value type. |
| [`public T DefaultValue { get; }`](#p-electron2d-projectsetting-1-defaultvalue) | Gets an independent copy of the default value. |

## Constructor Descriptions

<a id="constructor-metadata"></a>
### `public ProjectSetting<T>(string name, T defaultValue, Func<T, bool>? validator, JsonTypeInfo<T>? typeInfo)`

The first three arguments preserve the existing constructor's validation and snapshot contract. `typeInfo` is complete `System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>` metadata, normally from an application source-generated context. It is validated, frozen and retained through registration, default decoding and both ordinary and feature-specific keys. For a custom model in trimmed/AOT hosts use `new ProjectSetting<Model>("game/model", initial, validator: null, typeInfo: AppJSONContext.Default.Model)`.

Null metadata chooses the built-in catalog or the reflection-enabled host's resolver. Other null/invalid arguments and validator rejection preserve the original exceptions. Indented metadata throws `ArgumentException`; untyped or engine-object members throw `NotSupportedException`. There is no public untyped metadata/value getter.

<a id="m-electron2d-projectsetting-1-ctor-system-string-0-system-func-0-system-boolean"></a>
### `public ProjectSetting<T>(string name, T defaultValue, Func<T, bool> validator = null)`

Initializes a strongly typed project setting.

**Parameters**

- `name`: The full case-sensitive category path.
- `defaultValue`: The non-null value returned while no explicit value is stored.
- `validator`: An optional predicate that must accept every stored value.

**Exceptions**

- `ArgumentNullException`: `name` or `defaultValue` is `null`.
- `ArgumentException`: `name` is not a valid category path.
- `ArgumentOutOfRangeException`: `validator` rejects `defaultValue`.
- `NotSupportedException`: `T` is not a supported configuration value type.
- `Text.Json.JsonException`: `defaultValue` cannot be serialized as `T`.

## Property Descriptions

<a id="p-electron2d-projectsetting-1-name"></a>
### `public string Name { get; }`

Gets the full setting path.

**Value:** A case-sensitive category path containing at least one slash.

<a id="p-electron2d-projectsetting-1-valuetype"></a>
### `public Type ValueType { get; }`

Gets the declared value type.

**Value:** `T`.

<a id="p-electron2d-projectsetting-1-defaultvalue"></a>
### `public T DefaultValue { get; }`

Gets an independent copy of the default value.

**Value:** The value supplied during construction, deserialized as a new snapshot.

## Lifecycle, invariants, and errors

Construction is the only state transition. A successful instance is immutable and thread-safe. Null defaults are rejected even when `T` is annotated inconsistently by a caller. Serialization failure propagates without creating an instance. A validator returning false raises `ArgumentOutOfRangeException`; a validator exception propagates unchanged.

The validator may run during construction, assignment, registration against previously loaded data, and transactional load. It runs synchronously on the caller's thread and may run concurrently, so it must be deterministic, thread-safe, and free of registry mutations. During a `ProjectSettings` load an attempted registry mutation is rejected so a failed validator cannot partially replace the active document.

## Dependencies and interactions

The type reuses the serializer and type-safety rules of [`ConfigFile`](ConfigFile.md). A registry initially uses `DefaultValue` as its initial/revert value, but can later replace that per-registry initial value without mutating the shared definition. Registration, persistence, feature overrides, metadata, unsaved-value tracking, path resolution, and change events belong to `ProjectSettings` rather than this identity object.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` verifies malformed names, forbidden types, rejected defaults/writes, exact-definition identity, reuse across registries, and mutable default/read isolation. There is no public custom serializer or equality comparer; values must have a stable representation supported by the built-in JSON serializer.
