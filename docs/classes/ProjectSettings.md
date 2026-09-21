# ProjectSettings

Last updated: 2026-09-21

## Declaration

- Source: [`ProjectSettings.cs`](../../src/Core/Config/ProjectSettings.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class ProjectSettings : ElectronObject`
- Domain: [Core](../domains/core.md)
- Component: [Project settings](../components/project-settings.md)
- Typed key: [`ProjectSetting<T>`](ProjectSetting.Generic.md)

## Responsibility and ownership

`ProjectSettings` owns a typed registry of project-wide values, metadata, feature overrides, independent unsaved/change-notification state, atomic persistence through [`ConfigFile`](ConfigFile.md), and lexical resolution of `res://` and `user://`. `Instance` is the permanent runtime registry and is registered as the built-in `ProjectSettings` entry in [`Engine`](Engine.md). Public constructors create disposable isolated registries for tools, tests, or unopened projects.

Each instance owns its in-memory `ConfigFile`. It does not own project/user directories and does not create the user directory. Unknown entry values loaded from disk are retained semantically in canonical encoding and can be claimed later by registering a compatible typed definition.

## Complete public API

### Identity, built-ins, paths, and event

| Member | Current behavior |
| --- | --- |
| `ProjectFileName` | Constant `project.e2d` |
| `OverrideFileName` | Constant `override.cfg` |
| `ProjectDataDirectoryName` | Constant `.electron2d` |
| `static ProjectSettings Instance` | Process-wide non-disposable registry rooted at the working directory captured on first initialization |
| `ProjectSettings(string projectRoot)` | Isolated registry with a platform local-data path derived from the project directory name |
| `ProjectSettings(string projectRoot, string userDataRoot)` | Isolated registry with explicit roots; project root must exist; user root need not exist |
| `ProjectRoot`, `UserDataRoot` | Normalized absolute roots for `res://` and `user://` |
| `ProjectFilePath`, `OverrideFilePath`, `ProjectDataPath` | Derived absolute paths; getters do not create files/directories |
| `ulong Version` | Starts at 1 and increases after registry, stored-value, active-feature, document replacement, or path-reset changes |
| `event Action<ProjectSettings>? SettingsChanged` | Typed, synchronous, coalesced event consumed by `FlushChanges()` |

Built-in typed definitions are `ApplicationName`, `ApplicationVersion`, `PhysicsTicksPerSecond`, `MaxPhysicsStepsPerFrame`, and `PhysicsJitterFix`. The three timing definitions are the backing source for the corresponding Engine properties. They can be changed at runtime and cannot be unregistered.

### Registry and values

| Member | Current behavior |
| --- | --- |
| `Register<T>(ProjectSetting<T>)` | Registers the exact definition after validating any already-loaded base/override values; publishes `PropertyListChanged` after commit |
| `Unregister<T>(ProjectSetting<T>)` | Removes a non-built-in definition plus its stored base/overrides; marks its name changed; publishes `PropertyListChanged` |
| `HasSetting<T>(ProjectSetting<T>)` | Requires exact definition identity, not only matching name/type |
| `GetSettingNames(bool includeInternal = true)` | Immutable snapshot sorted by explicit order then ordinal name |
| `Get<T>(ProjectSetting<T>)` | Returns the explicit base value or current per-registry initial value; never applies feature overrides |
| `Set<T>(ProjectSetting<T>, T)` | Validates and serializes before one locked commit; equal serialized value is a no-op |
| `Reset<T>(ProjectSetting<T>)` | Restores the current initial/revert value by removing explicit base storage |
| `SetInitialValue<T>(ProjectSetting<T>, T)` | Changes the per-registry implicit/revert value without changing the current value or unsaved state |

All value reads return independent snapshots for mutable models. Missing registration throws `KeyNotFoundException`; a different definition owning the same name throws `InvalidOperationException`. There is deliberately no string/value, `object`, dynamic, or JSON-DOM getter/setter.

### Feature overrides

| Member | Current behavior |
| --- | --- |
| `SetFeatureOverride<T>(setting, feature, value)` | Stores `name.feature`; new tags have lower priority than existing entries |
| `ClearFeatureOverride<T>(setting, feature)` | Removes one entry and reports whether it existed |
| `GetFeatureOverrides<T>(setting)` | Immutable precedence-order tag snapshot |
| `GetWithOverride<T>(setting)` | Uses built-in platform/build/architecture tags plus active custom tags |
| `GetWithOverride<T>(setting, IEnumerable<string> features)` | Uses only the caller-supplied normalized tag set |
| `AddCustomFeature`, `RemoveCustomFeature`, `HasFeature`, `GetActiveFeatures` | Manage/query case-insensitive normalized custom tags without allowing built-in feature removal |

Feature tags start/end with an ASCII letter or digit, may contain ASCII letters, digits, `.`, `_`, and `-`, reject empty dotted segments, and normalize to lowercase. Overrides use first matching persisted/insertion order. Persisted suffixes must already be lowercase. Adding/removing a custom feature versions the registry and queues one change notification only for settings whose selected override changes; it does not create unsaved project values.

### Metadata and changes

| Member | Current behavior |
| --- | --- |
| `GetOrder` / `SetOrder` | Read/change signed save/tooling order |
| `IsBasic` / `SetAsBasic` | Read/change reduced-view metadata |
| `IsInternal` / `SetAsInternal` | Read/change ordinary-tooling visibility |
| `IsRestartRequired` / `SetRestartIfChanged` | Read/change restart-required metadata |
| `GetChangedSettings()` | Immutable ordinal snapshot in the pending/current notification batch; clears after delivery |
| `CheckChangedSettingsInGroup(prefix)` | Ordinal prefix search over the pending/current notification batch |
| `FlushChanges()` | Consumes one pending coalesced event and invokes handlers outside the registry lock |

Metadata changes update `PropertyListChanged` where tooling semantics change, but are not persisted as project values. Internal settings remain registered and persisted but are omitted from the inherited typed property list. During delivery, handlers can inspect the current batch. It is cleared afterward; a handler-created value change remains pending for the next flush. Handler exceptions propagate after clearing the delivered batch.

### Persistence and paths

| Member | Current behavior |
| --- | --- |
| `ConfigurePaths(projectRoot, userDataRoot)` | Requires a clean registry; resets loaded/explicit values while retaining definitions/metadata/features |
| `Load()` | Requires `project.e2d`, then optionally merges `override.cfg`; fully validates before replacement and starts clean |
| `LoadCustom(path)` | Transactionally merges one OS/virtual-path document; pre-existing unsaved state/pending event remain, while file-supplied values are treated as persisted |
| `Save()` | Omits registered base values equal to their initial value, orders entries, atomically saves to `ProjectFilePath`, then clears internal unsaved tracking |
| `SaveCustom(path)` | Same behavior for an explicit OS/virtual path |
| `GlobalizePath(path)` | Resolves `res://`/`user://`, blocks lexical root escape, rejects unknown schemes, or normalizes an ordinary path |
| `LocalizePath(path)` | Chooses the most-specific containing root and returns a forward-slash virtual path, otherwise an absolute native path |
| `static FindProjectRoot(startPath)` | Searches an existing directory/file location and its parents for the nearest `project.e2d` |

Loads serialize all mutation behind one registry lock, reject re-entrant writes from validators, and swap only after parsing, type decoding, override discovery, and every validator succeed. A failed load/save preserves the active document/unsaved state respectively. Save order applies across sections to registered base values and their override entries; unknown entries retain relative insertion order after known entries. Notification batches and unsaved-value tracking are independent: only `FlushChanges()` consumes the former, and only a successful save clears the latter.

Virtual-path confinement is lexical. Symbolic links are not resolved, so this API is path normalization rather than a hostile-filesystem sandbox.

## Lifecycle and threading

All public registry operations preserve internal state under concurrency. Value/metadata transitions and document swaps are linearized by one lock. User validators run synchronously on caller threads and must themselves be deterministic, thread-safe, and free of registry mutations. Serialization, validators, file I/O, snapshots, and path operations may allocate/block and are not frame-hot APIs. Scalar/string setting definitions cache their most recently decoded representation; Engine timing reads therefore remain allocation-free after warm-up. Mutable reference values are never returned from that cache.

Isolated instances dispose their owned document and clear subscribers/state. The process singleton rejects disposal. Disposal during an active load validator is rejected so the transaction cannot be invalidated re-entrantly.

## Reference coverage inventory

The stable reference type and complete `Object` inheritance chain were audited on 2026-09-21.

| Reference area | Electron2D disposition |
| --- | --- |
| Global settings object and `settings_changed` | `Instance`, typed registry, and coalesced `SettingsChanged`; Engine flushes after each successful process frame |
| `get_setting`, `set_setting`, `has_setting`, `clear` | Typed `Get`, `Set`, `HasSetting`, `Reset`, and non-built-in `Unregister`; no universal value boundary |
| Initial/revert value | `SetInitialValue<T>` plus inherited typed property reversion |
| Property info/basic/internal/restart/order | Definition/type plus typed metadata methods and ordered typed property discovery |
| Changed settings/group query | Implemented with immutable snapshots and ordinal prefix matching |
| Feature-tag lookup/current/custom features | Implemented with deterministic typed override APIs and process feature discovery |
| `globalize_path`, `localize_path`, resource/project-data paths | Implemented for directory-backed `res://`/`user://`; generated project-data path is exposed but not created |
| Main/custom save and runtime override file | Implemented with `ConfigFile` text, ordering, atomic replacement, and exceptions |
| Global script class list | Deferred until a scripting language registry exists |
| Resource pack loading and packed/exported virtual filesystems | Deferred until resource-loader and pack-mount domains exist; current `FileAccess` and `DirAccess` remain directory-backed |
| Full editor property-hint dictionaries and editor overrides | Adapted to typed definitions/metadata where executable; UI-specific hints/overrides remain deferred until an editor exists |
| Hundreds of renderer/audio/input/network/XR/3D settings | Not registered: their owner domains are absent; 3D settings are permanently excluded by the 2D product boundary |
| Inherited dynamic/meta/script API | Follows the typed adaptations and exclusions documented by [`ElectronObject`](ElectronObject.md) |

No dependency-blocked member is represented by an empty method or inert setting.

## Verification and known limitations

The executable harness covers malformed definitions/features/paths, exact registration identity, built-ins, mutable snapshot isolation, validators and rollback, initial/revert behavior, metadata/property discovery and post-commit callback failure, override precedence/current/custom features, changed groups/version/no-op writes, event coalescing/re-entry/failure, OS/virtual path round trips/traversal, root discovery, save/load/override/late registration, failed I/O preservation, re-entrant validator rejection, concurrency, reconfiguration, disposal, Engine feature lookup/event flushing, and warmed zero-allocation Engine frames.

Tests do not prove crash durability on every filesystem, symbolic-link confinement, editor presentation, packed exports, or unavailable domain settings.
