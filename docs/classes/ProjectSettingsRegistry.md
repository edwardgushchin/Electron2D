# ProjectSettingsRegistry

Last updated: 2026-10-05

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** [ProjectSettings](ProjectSettings.md)

- **Source:** [`ProjectSettingsRegistry.cs`](../../src/Core/Config/ProjectSettingsRegistry.cs), [`ProjectSettingsRegistry.API.cs`](../../src/Core/Config/ProjectSettingsRegistry.API.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public partial class ProjectSettingsRegistry : ElectronObject`

## Description

Owns one typed project-settings registry, metadata, feature overrides, change notifications, virtual path roots and an in-memory ConfigFile. Caller-created registries are independent and disposable. The retained ProjectSettings runtime object derives from this implementation; its static operations address only the permanent registry and reject disposal. Public instance operations on ProjectSettingsRegistry are the explicit isolated-context API under [ADR 0095](../decisions/singleton-services.md#adr-0095).

Each registry borrows ProjectSetting definitions, snapshots serialized values, owns its ConfigFile and never owns project/user directories. Unknown values are preserved until typed definitions claim them. Operations are lock-serialized and disk/serialization work is outside real-time callbacks. SettingsChanged is synchronous on the flushing thread, coalesced, and carries the exact registry as sender. Handler changes remain pending for a later flush; handler errors propagate after the pending delivery is consumed.

## Examples

```csharp
using var settings = new ProjectSettingsRegistry(projectDirectory, userDataDirectory);
settings.Set(ProjectSettings.ApplicationName, "Tool preview");
settings.Save();
string scenePath = settings.GlobalizePath("res://levels/intro.scene");
```

The two directory variables are supplied by the caller. Creating or editing this registry does not change the runtime ProjectSettings service.

## Public surface

### Constructors

| Member | Description |
| --- | --- |
| [`public ProjectSettingsRegistry(System.String projectRoot)`](#member-ce6f5a227ca6) | Initializes an isolated registry using a platform-appropriate user-data directory. |
| [`public ProjectSettingsRegistry(System.String projectRoot, System.String userDataRoot)`](#member-b50fd0337310) | Initializes an isolated registry with explicit project and user-data directories. |

### Properties

| Member | Description |
| --- | --- |
| [`public System.String OverrideFilePath { get;  }`](#member-ee4022f5c19a) | Gets the absolute path of the optional runtime override file. |
| [`public System.String ProjectDataPath { get;  }`](#member-cf2ad4c84a6c) | Gets the absolute path reserved for generated project-local engine data. |
| [`public System.String ProjectFilePath { get;  }`](#member-57ddf2039403) | Gets the absolute path of the current project settings file. |
| [`public System.String ProjectRoot { get;  }`](#member-3d348f8c7c08) | Gets the current absolute project resource directory. |
| [`public System.String UserDataRoot { get;  }`](#member-700e77e52cb7) | Gets the current absolute user-data directory. |
| [`public System.UInt64 Version { get;  }`](#member-97dda0cb7b68) | Gets a monotonically increasing in-process registry version. |

### Methods

| Member | Description |
| --- | --- |
| [`public System.Boolean AddCustomFeature(System.String feature)`](#member-8435da80d470) | Adds a custom feature to the process feature set. |
| [`public System.Boolean CheckChangedSettingsInGroup(System.String prefix)`](#member-de0eb049ab4c) | Checks whether any changed setting starts with a category prefix. |
| [`public System.Boolean ClearFeatureOverride<T>(ProjectSetting<T> setting, System.String feature)`](#member-5300f11cf467) | Removes one stored feature override. |
| [`public System.Void ConfigurePaths(System.String projectRoot, System.String userDataRoot)`](#member-546bcd7d6507) | Changes the project and user-data roots and clears all loaded or explicitly stored values. |
| [`public System.Boolean FlushChanges()`](#member-ff8b56cbe2b0) | Raises one coalesced Electron2D.ProjectSettingsRegistry.SettingsChanged notification when changes are pending. |
| [`public System.Collections.Generic.IReadOnlyList<System.String> GetActiveFeatures()`](#member-5a2878d0c135) | Gets all active platform, build, architecture, and custom features. |
| [`public System.Collections.Generic.IReadOnlyList<System.String> GetChangedSettings()`](#member-6ff41fb535e4) | Gets setting names included in the pending or currently delivered change notification. |
| [`public System.Collections.Generic.IReadOnlyList<System.String> GetFeatureOverrides<T>(ProjectSetting<T> setting)`](#member-ea7723f4ccb1) | Gets the stored feature tags for one setting in override precedence order. |
| [`public System.Int32 GetOrder<T>(ProjectSetting<T> setting)`](#member-1fdabb0323f8) | Gets the persistence order assigned to a registered setting. |
| [`public System.Collections.Generic.IReadOnlyList<System.String> GetSettingNames(System.Boolean includeInternal = true)`](#member-18567f42fbfb) | Gets registered setting names ordered by explicit order and then by name. |
| [`public T GetWithOverride<T>(ProjectSetting<T> setting)`](#member-026b55178a16) | Gets a setting after applying the first override matching an active platform, build, or custom feature. |
| [`public T GetWithOverride<T>(ProjectSetting<T> setting, System.Collections.Generic.IEnumerable<System.String> features)`](#member-f87bddf98906) | Gets a setting after applying overrides against caller-supplied feature tags. |
| [`public T Get<T>(ProjectSetting<T> setting)`](#member-98410054ce44) | Gets a setting value without applying feature overrides. |
| [`public System.String GlobalizePath(System.String path)`](#member-381bb406ff8b) | Converts a virtual or ordinary path to an absolute native operating-system path. |
| [`public System.Boolean HasFeature(System.String feature)`](#member-77721bd609a9) | Determines whether a platform, build, architecture, or custom feature is active. |
| [`public System.Boolean HasSetting<T>(ProjectSetting<T> setting)`](#member-c9822d38b30b) | Determines whether the exact setting definition is registered. |
| [`public System.Boolean IsBasic<T>(ProjectSetting<T> setting)`](#member-20a5eed91650) | Gets whether a setting should appear in a reduced basic-settings view. |
| [`public System.Boolean IsInternal<T>(ProjectSetting<T> setting)`](#member-7182589ee896) | Gets whether a setting is hidden from ordinary tooling discovery. |
| [`public System.Boolean IsRestartRequired<T>(ProjectSetting<T> setting)`](#member-4133ec9dc7e8) | Gets whether changing a setting requires the host application to restart. |
| [`public System.Void Load()`](#member-187f2776bf73) | Loads the project file and then merges the conventional override file when it exists. |
| [`public System.Void LoadCustom(System.String path)`](#member-3353d7b63db9) | Merges a custom configuration file into the current settings after full validation. |
| [`public System.String LocalizePath(System.String path)`](#member-b1e3d1602a7b) | Converts an ordinary path inside a configured root to a virtual project or user path. |
| [`public System.Void Register<T>(ProjectSetting<T> setting)`](#member-e6ef918b72db) | Registers a setting definition for typed access and tooling discovery. |
| [`public System.Boolean RemoveCustomFeature(System.String feature)`](#member-c2049df1bfad) | Removes a custom feature from the process feature set. |
| [`public System.Void Reset<T>(ProjectSetting<T> setting)`](#member-3302105f946b) | Removes an explicit base value so the setting returns its current initial value. |
| [`public System.Void Save()`](#member-249517e5f167) | Saves the current document to Electron2D.ProjectSettingsRegistry.ProjectFilePath using atomic replacement. |
| [`public System.Void SaveCustom(System.String path)`](#member-243674993201) | Saves the current document to an explicit file using atomic replacement. |
| [`public System.Void SetAsBasic<T>(ProjectSetting<T> setting, System.Boolean basic)`](#member-05d7df5d0a01) | Sets whether a setting should appear in a reduced basic-settings view. |
| [`public System.Void SetAsInternal<T>(ProjectSetting<T> setting, System.Boolean internalSetting)`](#member-cfc4dacd2777) | Sets whether a setting is hidden from ordinary tooling discovery. |
| [`public System.Void SetFeatureOverride<T>(ProjectSetting<T> setting, System.String feature, T value)`](#member-3d8c15d286a6) | Stores a value selected when the named feature is active. |
| [`public System.Void SetInitialValue<T>(ProjectSetting<T> setting, T value)`](#member-b4cf56c8a78f) | Changes the value used by Electron2D.ProjectSettingsRegistry.Reset``1(Electron2D.ProjectSetting{``0}) and typed property reversion. |
| [`public System.Void SetOrder<T>(ProjectSetting<T> setting, System.Int32 order)`](#member-888c9e7dbc01) | Changes the persistence and tooling order of a registered setting. |
| [`public System.Void SetRestartIfChanged<T>(ProjectSetting<T> setting, System.Boolean restart)`](#member-f22f05254647) | Sets whether changing a setting requires the host application to restart. |
| [`public System.Void Set<T>(ProjectSetting<T> setting, T value)`](#member-4ffc4aefc082) | Stores a base value for a registered setting. |
| [`public System.Void Unregister<T>(ProjectSetting<T> setting)`](#member-22d77db0c2c3) | Unregisters a setting and removes its stored base value and feature overrides. |

### Events

| Member | Description |
| --- | --- |
| [`public event System.Action<Electron2D.ProjectSettingsRegistry> SettingsChanged`](#member-c4f40772e01d) | Occurs after one or more settings change and Electron2D.ProjectSettingsRegistry.FlushChanges is called. |

## Member descriptions

<a id="member-ce6f5a227ca6"></a>
### `public ProjectSettingsRegistry(System.String projectRoot)`

Initializes an isolated registry using a platform-appropriate user-data directory.

**Parameter `projectRoot`:** An existing project directory.

**Exception `T:System.ArgumentNullException`:** projectRoot is null.

**Exception `T:System.ArgumentException`:** projectRoot is empty or invalid.

**Exception `T:System.IO.DirectoryNotFoundException`:** projectRoot does not exist.

<a id="member-b50fd0337310"></a>
### `public ProjectSettingsRegistry(System.String projectRoot, System.String userDataRoot)`

Initializes an isolated registry with explicit project and user-data directories.

**Parameter `projectRoot`:** An existing project directory.

**Parameter `userDataRoot`:** The user-writable data directory; it is not created automatically.

**Exception `T:System.ArgumentNullException`:** projectRoot or userDataRoot is null.

**Exception `T:System.ArgumentException`:** A path is empty or invalid, or both paths resolve to the same directory.

**Exception `T:System.IO.DirectoryNotFoundException`:** projectRoot does not exist.

<a id="member-c4f40772e01d"></a>
### `public event System.Action<Electron2D.ProjectSettingsRegistry> SettingsChanged`

Occurs after one or more settings change and Electron2D.ProjectSettingsRegistry.FlushChanges is called.

**Remarks:** Delivery is synchronous on the flushing thread and is coalesced to one invocation per flush. A change made by a handler is retained for the next flush. The process-wide Electron2D.Engine flushes at the end of each successfully processed frame. Handler exceptions propagate after the pending invocation has been consumed.

<a id="member-8435da80d470"></a>
### `public System.Boolean AddCustomFeature(System.String feature)`

Adds a custom feature to the process feature set.

**Returns:** true when the active set changed; otherwise false.

**Parameter `feature`:** The feature tag to add.

**Exception `T:System.ArgumentNullException`:** feature is null.

**Exception `T:System.ArgumentException`:** feature is invalid.

**Exception `T:System.InvalidOperationException`:** A load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-de0eb049ab4c"></a>
### `public System.Boolean CheckChangedSettingsInGroup(System.String prefix)`

Checks whether any changed setting starts with a category prefix.

**Returns:** true when at least one changed setting starts with prefix.

**Parameter `prefix`:** The case-sensitive prefix to inspect.

**Exception `T:System.ArgumentNullException`:** prefix is null.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-5300f11cf467"></a>
### `public System.Boolean ClearFeatureOverride<T>(ProjectSetting<T> setting, System.String feature)`

Removes one stored feature override.

**Returns:** true when an override was removed; otherwise false.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Parameter `feature`:** The feature tag to remove.

**Exception `T:System.ArgumentNullException`:** setting or feature is null.

**Exception `T:System.ArgumentException`:** feature is invalid.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name or a load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-546bcd7d6507"></a>
### `public System.Void ConfigurePaths(System.String projectRoot, System.String userDataRoot)`

Changes the project and user-data roots and clears all loaded or explicitly stored values.

**Remarks:** Registered setting definitions and metadata are retained. Call this during host setup before runtime use.

**Parameter `projectRoot`:** An existing project directory.

**Parameter `userDataRoot`:** The user-writable data directory; it is not created automatically.

**Exception `T:System.ArgumentNullException`:** A path is null.

**Exception `T:System.ArgumentException`:** A path is empty or invalid, or both paths resolve to the same directory.

**Exception `T:System.IO.DirectoryNotFoundException`:** projectRoot does not exist.

**Exception `T:System.InvalidOperationException`:** Unsaved setting changes exist or a load is already running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-23b7c17bc08d"></a>
### `protected override System.Void Dispose(System.Boolean disposing)`

<a id="member-ff8b56cbe2b0"></a>
### `public System.Boolean FlushChanges()`

Raises one coalesced Electron2D.ProjectSettingsRegistry.SettingsChanged notification when changes are pending.

**Returns:** true when an event invocation was consumed; otherwise false.

**Remarks:** The callback runs without the registry lock, so handlers may read or modify settings.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

**Exception `T:System.Exception`:** An event handler throws.

<a id="member-5a2878d0c135"></a>
### `public System.Collections.Generic.IReadOnlyList<System.String> GetActiveFeatures()`

Gets all active platform, build, architecture, and custom features.

**Returns:** An immutable ordinally sorted snapshot of normalized feature tags.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-6ff41fb535e4"></a>
### `public System.Collections.Generic.IReadOnlyList<System.String> GetChangedSettings()`

Gets setting names included in the pending or currently delivered change notification.

**Returns:** An immutable ordinally sorted snapshot, cleared after the notification finishes.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-ea7723f4ccb1"></a>
### `public System.Collections.Generic.IReadOnlyList<System.String> GetFeatureOverrides<T>(ProjectSetting<T> setting)`

Gets the stored feature tags for one setting in override precedence order.

**Returns:** An immutable snapshot of normalized feature tags.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-1fdabb0323f8"></a>
### `public System.Int32 GetOrder<T>(ProjectSetting<T> setting)`

Gets the persistence order assigned to a registered setting.

**Returns:** The signed ordering value; lower values appear first.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-7770a7668493"></a>
### `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

<a id="member-18567f42fbfb"></a>
### `public System.Collections.Generic.IReadOnlyList<System.String> GetSettingNames(System.Boolean includeInternal = true)`

Gets registered setting names ordered by explicit order and then by name.

**Returns:** An immutable snapshot of full case-sensitive setting paths.

**Parameter `includeInternal`:** Whether settings marked as internal are included.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-026b55178a16"></a>
### `public T GetWithOverride<T>(ProjectSetting<T> setting)`

Gets a setting after applying the first override matching an active platform, build, or custom feature.

**Returns:** An independent value snapshot.

**Remarks:** Override precedence follows the insertion order of override entries in the loaded or modified document.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-f87bddf98906"></a>
### `public T GetWithOverride<T>(ProjectSetting<T> setting, System.Collections.Generic.IEnumerable<System.String> features)`

Gets a setting after applying overrides against caller-supplied feature tags.

**Returns:** An independent value snapshot.

**Remarks:** Tags are normalized to lowercase. Override precedence follows stored override insertion order.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Parameter `features`:** Feature tags replacing the registry's current active feature set for this lookup.

**Exception `T:System.ArgumentNullException`:** setting, features, or one of its items is null.

**Exception `T:System.ArgumentException`:** A feature tag is invalid.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-98410054ce44"></a>
### `public T Get<T>(ProjectSetting<T> setting)`

Gets a setting value without applying feature overrides.

**Returns:** An independent snapshot of the explicit value, or the current initial value when no explicit value exists.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-381bb406ff8b"></a>
### `public System.String GlobalizePath(System.String path)`

Converts a virtual or ordinary path to an absolute native operating-system path.

**Returns:** An absolute normalized path.

**Remarks:** Virtual paths are lexically confined to their configured roots and reject parent traversal. This method does not resolve symbolic links and is not a filesystem security sandbox.

**Parameter `path`:** A nonempty path, optionally beginning with `res://` or `user://`.

**Exception `T:System.ArgumentNullException`:** path is null.

**Exception `T:System.ArgumentException`:** path is empty or invalid.

**Exception `T:System.UnauthorizedAccessException`:** A virtual path escapes its configured root.

**Exception `T:System.NotSupportedException`:** path uses an unknown virtual-path scheme.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-77721bd609a9"></a>
### `public System.Boolean HasFeature(System.String feature)`

Determines whether a platform, build, architecture, or custom feature is active.

**Returns:** true when the normalized feature is active.

**Parameter `feature`:** The feature tag to inspect.

**Exception `T:System.ArgumentNullException`:** feature is null.

**Exception `T:System.ArgumentException`:** feature is invalid.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-c9822d38b30b"></a>
### `public System.Boolean HasSetting<T>(ProjectSetting<T> setting)`

Determines whether the exact setting definition is registered.

**Returns:** true only when this exact definition owns its name in this registry.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The definition to inspect.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-20a5eed91650"></a>
### `public System.Boolean IsBasic<T>(ProjectSetting<T> setting)`

Gets whether a setting should appear in a reduced basic-settings view.

**Returns:** The current basic-view flag.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-7182589ee896"></a>
### `public System.Boolean IsInternal<T>(ProjectSetting<T> setting)`

Gets whether a setting is hidden from ordinary tooling discovery.

**Returns:** The current internal flag.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-4133ec9dc7e8"></a>
### `public System.Boolean IsRestartRequired<T>(ProjectSetting<T> setting)`

Gets whether changing a setting requires the host application to restart.

**Returns:** The current restart-required flag.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-187f2776bf73"></a>
### `public System.Void Load()`

Loads the project file and then merges the conventional override file when it exists.

**Remarks:** The complete candidate document and every registered typed value are validated before current state is replaced. Successful loading clears unsaved-value tracking and any pending change notification.

**Exception `T:System.IO.FileNotFoundException`:** The project file does not exist.

**Exception `T:System.IO.IOException`:** A file cannot be read.

**Exception `T:System.UnauthorizedAccessException`:** A file cannot be read by the caller.

**Exception `T:System.FormatException`:** A document is malformed.

**Exception `T:System.IO.InvalidDataException`:** A registered value has the wrong type.

**Exception `T:System.ArgumentOutOfRangeException`:** A registered validator rejects a loaded value.

**Exception `T:System.InvalidOperationException`:** A re-entrant load or setting mutation is attempted.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-3353d7b63db9"></a>
### `public System.Void LoadCustom(System.String path)`

Merges a custom configuration file into the current settings after full validation.

**Remarks:** Values already unsaved before the merge remain unsaved because loading another document does not persist them. Values supplied only by the loaded file are treated as persisted and do not add unsaved names.

**Parameter `path`:** An operating-system, `res://`, or `user://` file path.

**Exception `T:System.ArgumentNullException`:** path is null.

**Exception `T:System.ArgumentException`:** path is empty or invalid.

**Exception `T:System.IO.IOException`:** The file cannot be read.

**Exception `T:System.UnauthorizedAccessException`:** The file cannot be read by the caller.

**Exception `T:System.FormatException`:** The document is malformed.

**Exception `T:System.IO.InvalidDataException`:** A registered value has the wrong type.

**Exception `T:System.ArgumentOutOfRangeException`:** A registered validator rejects a loaded value.

**Exception `T:System.InvalidOperationException`:** A re-entrant load or setting mutation is attempted.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-b1e3d1602a7b"></a>
### `public System.String LocalizePath(System.String path)`

Converts an ordinary path inside a configured root to a virtual project or user path.

**Returns:** A `res://` or `user://` path when contained by a configured root; otherwise an absolute native path.

**Parameter `path`:** A nonempty operating-system or already virtual path.

**Exception `T:System.ArgumentNullException`:** path is null.

**Exception `T:System.ArgumentException`:** path is empty or invalid.

**Exception `T:System.NotSupportedException`:** path uses an unknown virtual-path scheme.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-e6ef918b72db"></a>
### `public System.Void Register<T>(ProjectSetting<T> setting)`

Registers a setting definition for typed access and tooling discovery.

**Remarks:** If a matching value was loaded before registration, it and all matching feature overrides are validated before the registry changes. Registration does not mark the setting as modified.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The definition to register.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.InvalidOperationException`:** The name is already registered or a load is running.

**Exception `T:System.IO.InvalidDataException`:** A loaded value cannot be decoded as T.

**Exception `T:System.ArgumentOutOfRangeException`:** The setting validator rejects a loaded value.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

**Exception `T:System.Exception`:** A property-list listener throws after registration has completed.

<a id="member-c2049df1bfad"></a>
### `public System.Boolean RemoveCustomFeature(System.String feature)`

Removes a custom feature from the process feature set.

**Returns:** true when the active set changed; otherwise false.

**Parameter `feature`:** The feature tag to remove.

**Exception `T:System.ArgumentNullException`:** feature is null.

**Exception `T:System.ArgumentException`:** feature is invalid.

**Exception `T:System.InvalidOperationException`:** A load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-3302105f946b"></a>
### `public System.Void Reset<T>(ProjectSetting<T> setting)`

Removes an explicit base value so the setting returns its current initial value.

**Remarks:** Feature overrides are retained. The initial value becomes implicit and is not persisted.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name or a load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-249517e5f167"></a>
### `public System.Void Save()`

Saves the current document to Electron2D.ProjectSettingsRegistry.ProjectFilePath using atomic replacement.

**Remarks:** A successful save clears internal unsaved-value tracking. Change-notification names remain until Electron2D.ProjectSettingsRegistry.FlushChanges completes; a failed save preserves both states.

**Exception `T:System.IO.IOException`:** The file cannot be written or replaced.

**Exception `T:System.UnauthorizedAccessException`:** The caller cannot write the file.

**Exception `T:System.IO.DirectoryNotFoundException`:** The project directory no longer exists.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-243674993201"></a>
### `public System.Void SaveCustom(System.String path)`

Saves the current document to an explicit file using atomic replacement.

**Remarks:** A successful save clears internal unsaved-value tracking. Change-notification names remain until Electron2D.ProjectSettingsRegistry.FlushChanges completes; a failed save preserves both states.

**Parameter `path`:** An operating-system, `res://`, or `user://` file path.

**Exception `T:System.ArgumentNullException`:** path is null.

**Exception `T:System.ArgumentException`:** path is empty or invalid.

**Exception `T:System.IO.IOException`:** The file cannot be written or replaced.

**Exception `T:System.UnauthorizedAccessException`:** The caller cannot write the file.

**Exception `T:System.IO.DirectoryNotFoundException`:** The destination directory does not exist.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-05d7df5d0a01"></a>
### `public System.Void SetAsBasic<T>(ProjectSetting<T> setting, System.Boolean basic)`

Sets whether a setting should appear in a reduced basic-settings view.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Parameter `basic`:** The new basic-view flag.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name or a load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

**Exception `T:System.Exception`:** A property-list listener throws after the flag has changed.

<a id="member-cfc4dacd2777"></a>
### `public System.Void SetAsInternal<T>(ProjectSetting<T> setting, System.Boolean internalSetting)`

Sets whether a setting is hidden from ordinary tooling discovery.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Parameter `internalSetting`:** The new internal flag.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name or a load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

**Exception `T:System.Exception`:** A property-list listener throws after the flag has changed.

<a id="member-3d8c15d286a6"></a>
### `public System.Void SetFeatureOverride<T>(ProjectSetting<T> setting, System.String feature, T value)`

Stores a value selected when the named feature is active.

**Remarks:** New overrides have lower priority than existing overrides for the same setting.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Parameter `feature`:** The platform, build, architecture, or custom feature tag.

**Parameter `value`:** The non-null value to validate and snapshot.

**Exception `T:System.ArgumentNullException`:** An argument is null.

**Exception `T:System.ArgumentException`:** feature is invalid.

**Exception `T:System.ArgumentOutOfRangeException`:** The setting validator rejects value.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name or a load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-b4cf56c8a78f"></a>
### `public System.Void SetInitialValue<T>(ProjectSetting<T> setting, T value)`

Changes the value used by Electron2D.ProjectSettingsRegistry.Reset``1(Electron2D.ProjectSetting{``0}) and typed property reversion.

**Remarks:** The current value and unsaved state do not change. If necessary, the previous current value becomes explicit so changing the implicit value cannot change what callers observe.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Parameter `value`:** The new non-null validated initial value.

**Exception `T:System.ArgumentNullException`:** setting or value is null.

**Exception `T:System.ArgumentOutOfRangeException`:** The setting validator rejects value.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name or a load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-888c9e7dbc01"></a>
### `public System.Void SetOrder<T>(ProjectSetting<T> setting, System.Int32 order)`

Changes the persistence and tooling order of a registered setting.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Parameter `order`:** The signed ordering value.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name or a load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

**Exception `T:System.Exception`:** A property-list listener throws after the order has changed.

<a id="member-f22f05254647"></a>
### `public System.Void SetRestartIfChanged<T>(ProjectSetting<T> setting, System.Boolean restart)`

Sets whether changing a setting requires the host application to restart.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Parameter `restart`:** The new restart-required flag.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name or a load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

**Exception `T:System.Exception`:** A property-list listener throws after the flag has changed.

<a id="member-4ffc4aefc082"></a>
### `public System.Void Set<T>(ProjectSetting<T> setting, T value)`

Stores a base value for a registered setting.

**Remarks:** An equal serialized value is a no-op and does not update change tracking or Electron2D.ProjectSettingsRegistry.Version.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Parameter `value`:** The non-null value to validate and snapshot.

**Exception `T:System.ArgumentNullException`:** setting or value is null.

**Exception `T:System.ArgumentOutOfRangeException`:** The setting validator rejects value.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** A different definition owns the same name or a load is running.

**Exception `T:System.Text.Json.JsonException`:** value cannot be serialized.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

<a id="member-22d77db0c2c3"></a>
### `public System.Void Unregister<T>(ProjectSetting<T> setting)`

Unregisters a setting and removes its stored base value and feature overrides.

**Remarks:** Built-in settings cannot be unregistered. The removal is tracked as an unsaved change.

**Type parameter `T`:** The non-null setting value type.

**Parameter `setting`:** The exact registered definition.

**Exception `T:System.ArgumentNullException`:** setting is null.

**Exception `T:System.Collections.Generic.KeyNotFoundException`:** setting is not registered.

**Exception `T:System.InvalidOperationException`:** The setting is built in, a different definition owns its name, or a load is running.

**Exception `T:System.ObjectDisposedException`:** The registry is disposing or disposed.

**Exception `T:System.Exception`:** A property-list listener throws after removal has completed.

<a id="member-fbc51a532cac"></a>
### `protected override System.Void ValidateDisposal()`

<a id="member-ee4022f5c19a"></a>
### `public System.String OverrideFilePath { get;  }`

Gets the absolute path of the optional runtime override file.

**Value:** Electron2D.ProjectSettings.OverrideFileName inside Electron2D.ProjectSettingsRegistry.ProjectRoot.

<a id="member-cf2ad4c84a6c"></a>
### `public System.String ProjectDataPath { get;  }`

Gets the absolute path reserved for generated project-local engine data.

**Value:** Electron2D.ProjectSettings.ProjectDataDirectoryName inside Electron2D.ProjectSettingsRegistry.ProjectRoot.

<a id="member-57ddf2039403"></a>
### `public System.String ProjectFilePath { get;  }`

Gets the absolute path of the current project settings file.

**Value:** Electron2D.ProjectSettings.ProjectFileName inside Electron2D.ProjectSettingsRegistry.ProjectRoot.

<a id="member-3d348f8c7c08"></a>
### `public System.String ProjectRoot { get;  }`

Gets the current absolute project resource directory.

**Value:** The directory to which `res://` resolves.

<a id="member-700e77e52cb7"></a>
### `public System.String UserDataRoot { get;  }`

Gets the current absolute user-data directory.

**Value:** The directory to which `user://` resolves. The directory may not exist yet.

<a id="member-97dda0cb7b68"></a>
### `public System.UInt64 Version { get;  }`

Gets a monotonically increasing in-process registry version.

**Value:** A value starting at one and incremented after registry, value, or active-feature changes.

## Lifecycle and verification

Constructors register the same built-in definitions and defaults as the permanent runtime. ConfigurePaths requires no unsaved values; load validates candidates before publication, retains unknown values and rejects reentrant load/disposal. Save uses the existing atomic ConfigFile persistence. Independent disposal releases only that registry; the permanent runtime object rejects disposal. Property descriptors address the exact owning registry, including the derived ProjectSettings object.

Program.cs verifies typed registration, defaults, metadata, feature overrides, paths, persistence, callbacks, failure rollback, concurrency, disposal and descriptors. StaticServiceTests additionally checks independence of two explicit registries from the runtime and event sender identity. Filesystem crash behavior, resource packs, editor integration and other platforms retain their existing gates.

See [project-settings component](../components/project-settings.md), [ADR 0019](../decisions/core-data-io.md#adr-0019) and [ADR 0095](../decisions/singleton-services.md#adr-0095).

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.
