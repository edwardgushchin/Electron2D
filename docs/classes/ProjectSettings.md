# ProjectSettings

Last updated: 2026-09-23

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Core/Config/ProjectSettings.cs`](../../src/Core/Config/ProjectSettings.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class ProjectSettings : ElectronObject`

> Stores globally accessible, strongly typed project settings and resolves project virtual paths.

## Description

Stores globally accessible, strongly typed project settings and resolves project virtual paths.

`ProjectSettings` owns a typed registry of project-wide values, metadata, feature overrides, independent unsaved/change-notification state, atomic persistence through [`ConfigFile`](ConfigFile.md), and lexical resolution of `res://` and `user://`. `Instance` is the permanent runtime registry and is registered as the built-in `ProjectSettings` entry in [`Engine`](Engine.md). Public constructors create disposable isolated registries for tools, tests, or unopened projects.

Each instance owns its in-memory `ConfigFile`. It does not own project/user directories and does not create the user directory. Unknown entry values loaded from disk are retained semantically in canonical encoding and can be claimed later by registering a compatible typed definition.

[`ProjectSettings.Instance`](ProjectSettings.md#p-electron2d-projectsettings-instance) is the process-wide runtime registry. Additional instances may be created for isolated
tooling, tests, or multiple unopened projects. All public operations are thread-safe; disk and serialization
operations are not suitable for real-time callbacks.

Settings must be registered before typed access. Unknown values loaded from disk remain preserved and become
available when a matching typed setting is later registered. The registry never exposes an untyped value API.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
ProjectSettings settings = ProjectSettings.Instance;
string resourcePath = settings.GlobalizePath("res://levels/intro.scene");
```

## Constructors

| Member | Description |
| --- | --- |
| [`public ProjectSettings(string projectRoot)`](#m-electron2d-projectsettings-ctor-system-string) | Initializes an isolated registry using a platform-appropriate user-data directory. |
| [`public ProjectSettings(string projectRoot, string userDataRoot)`](#m-electron2d-projectsettings-ctor-system-string-system-string) | Initializes an isolated registry with explicit project and user-data directories. |

## Properties

| Member | Description |
| --- | --- |
| [`public static ProjectSetting<Color> DebugPathsColor { get; }`](#diagnostics-debugpathscolor) | Defines the color of path curves and tangent markers when scene path diagnostics are enabled. |
| [`public static ProjectSetting<string> ApplicationName { get; }`](#p-electron2d-projectsettings-applicationname) | Defines the human-readable application name. |
| [`public static ProjectSetting<string> ApplicationVersion { get; }`](#p-electron2d-projectsettings-applicationversion) | Defines the application version string. |
| [`public static ProjectSetting<int> PhysicsTicksPerSecond { get; }`](#p-electron2d-projectsettings-physicstickspersecond) | Defines the fixed-step callback frequency used by [`Engine`](Engine.md). |
| [`public static ProjectSetting<int> MaxPhysicsStepsPerFrame { get; }`](#p-electron2d-projectsettings-maxphysicsstepsperframe) | Defines the maximum fixed-step callbacks processed during one frame. |
| [`public static ProjectSetting<double> PhysicsJitterFix { get; }`](#p-electron2d-projectsettings-physicsjitterfix) | Defines the finite non-negative fixed-step boundary tolerance. |
| [`public static ProjectSetting<string> RenderingMethod { get; }`](#p-electron2d-projectsettings-renderingmethod) | Selects `gpu` or `compatibility` at renderer startup. |
| [`public static ProjectSetting<double> RenderingTimeRolloverSeconds { get; }`](#renderingtimerolloverseconds) | Sets the render-clock wrap period, default 3600 seconds. |
| [`public static ProjectSetting<bool> RenderingFallback { get; }`](#p-electron2d-projectsettings-renderingfallback) | Allows compatibility rendering if GPU initialization fails. |
| [`public static ProjectSetting<bool> SnapTransformsToPixel { get; }`](#snaptransformstopixel) | Initial transform snapping for a new root Window; false. |
| [`public static ProjectSetting<bool> SnapVerticesToPixel { get; }`](#snapverticestopixel) | Initial vertex snapping for a new root Window; false. |
| [`public static ProjectSetting<bool> UseNearestMipmapFilter { get; }`](#usenearestmipmapfilter) | Selects canvas mip interpolation at GPU startup; false by default. |
| [`public static ProjectSetting<int> AnisotropicFilteringLevel { get; }`](#anisotropicfilteringlevel) | Initializes new viewports; exponent 0..4, default 2. |
| [`public static ProjectSetting<Color> DefaultClearColor { get; }`](#p-electron2d-projectsettings-defaultclearcolor) | Defines the initial root-framebuffer clear color. |
| [`public static ProjectSettings Instance { get; }`](#p-electron2d-projectsettings-instance) | Gets the process-wide project settings registry. |
| [`public string ProjectRoot { get; }`](#p-electron2d-projectsettings-projectroot) | Gets the current absolute project resource directory. |
| [`public string UserDataRoot { get; }`](#p-electron2d-projectsettings-userdataroot) | Gets the current absolute user-data directory. |
| [`public string ProjectFilePath { get; }`](#p-electron2d-projectsettings-projectfilepath) | Gets the absolute path of the current project settings file. |
| [`public string OverrideFilePath { get; }`](#p-electron2d-projectsettings-overridefilepath) | Gets the absolute path of the optional runtime override file. |
| [`public string ProjectDataPath { get; }`](#p-electron2d-projectsettings-projectdatapath) | Gets the absolute path reserved for generated project-local engine data. |
| [`public ulong Version { get; }`](#p-electron2d-projectsettings-version) | Gets a monotonically increasing in-process registry version. |

## Methods

| Member | Description |
| --- | --- |
| [`public void ConfigurePaths(string projectRoot, string userDataRoot)`](#m-electron2d-projectsettings-configurepaths-system-string-system-string) | Changes the project and user-data roots and clears all loaded or explicitly stored values. |
| [`public void Register<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-register-1-electron2d-projectsetting-0) | Registers a setting definition for typed access and tooling discovery. |
| [`public void Unregister<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-unregister-1-electron2d-projectsetting-0) | Unregisters a setting and removes its stored base value and feature overrides. |
| [`public bool HasSetting<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-hassetting-1-electron2d-projectsetting-0) | Determines whether the exact setting definition is registered. |
| [`public IReadOnlyList<string> GetSettingNames(bool includeInternal = true)`](#m-electron2d-projectsettings-getsettingnames-system-boolean) | Gets registered setting names ordered by explicit order and then by name. |
| [`public T Get<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-get-1-electron2d-projectsetting-0) | Gets a setting value without applying feature overrides. |
| [`public T GetWithOverride<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-getwithoverride-1-electron2d-projectsetting-0) | Gets a setting after applying the first override matching an active platform, build, or custom feature. |
| [`public T GetWithOverride<T>(ProjectSetting<T> setting, IEnumerable<string> features)`](#m-electron2d-projectsettings-getwithoverride-1-electron2d-projectsetting-0-system-collections-generic-ienumerable-system-string) | Gets a setting after applying overrides against caller-supplied feature tags. |
| [`public void Set<T>(ProjectSetting<T> setting, T value)`](#m-electron2d-projectsettings-set-1-electron2d-projectsetting-0-0) | Stores a base value for a registered setting. |
| [`public void Reset<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-reset-1-electron2d-projectsetting-0) | Removes an explicit base value so the setting returns its current initial value. |
| [`public void SetInitialValue<T>(ProjectSetting<T> setting, T value)`](#m-electron2d-projectsettings-setinitialvalue-1-electron2d-projectsetting-0-0) | Changes the value used by [`ProjectSettings.Reset``1(ProjectSetting{``0})`](ProjectSettings.md#m-electron2d-projectsettings-reset-1-electron2d-projectsetting-0) and typed property reversion. |
| [`public void SetFeatureOverride<T>(ProjectSetting<T> setting, string feature, T value)`](#m-electron2d-projectsettings-setfeatureoverride-1-electron2d-projectsetting-0-system-string-0) | Stores a value selected when the named feature is active. |
| [`public bool ClearFeatureOverride<T>(ProjectSetting<T> setting, string feature)`](#m-electron2d-projectsettings-clearfeatureoverride-1-electron2d-projectsetting-0-system-string) | Removes one stored feature override. |
| [`public IReadOnlyList<string> GetFeatureOverrides<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-getfeatureoverrides-1-electron2d-projectsetting-0) | Gets the stored feature tags for one setting in override precedence order. |
| [`public bool AddCustomFeature(string feature)`](#m-electron2d-projectsettings-addcustomfeature-system-string) | Adds a custom feature to the process feature set. |
| [`public bool RemoveCustomFeature(string feature)`](#m-electron2d-projectsettings-removecustomfeature-system-string) | Removes a custom feature from the process feature set. |
| [`public bool HasFeature(string feature)`](#m-electron2d-projectsettings-hasfeature-system-string) | Determines whether a platform, build, architecture, or custom feature is active. |
| [`public IReadOnlyList<string> GetActiveFeatures()`](#m-electron2d-projectsettings-getactivefeatures) | Gets all active platform, build, architecture, and custom features. |
| [`public int GetOrder<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-getorder-1-electron2d-projectsetting-0) | Gets the persistence order assigned to a registered setting. |
| [`public void SetOrder<T>(ProjectSetting<T> setting, int order)`](#m-electron2d-projectsettings-setorder-1-electron2d-projectsetting-0-system-int32) | Changes the persistence and tooling order of a registered setting. |
| [`public bool IsBasic<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-isbasic-1-electron2d-projectsetting-0) | Gets whether a setting should appear in a reduced basic-settings view. |
| [`public void SetAsBasic<T>(ProjectSetting<T> setting, bool basic)`](#m-electron2d-projectsettings-setasbasic-1-electron2d-projectsetting-0-system-boolean) | Sets whether a setting should appear in a reduced basic-settings view. |
| [`public bool IsInternal<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-isinternal-1-electron2d-projectsetting-0) | Gets whether a setting is hidden from ordinary tooling discovery. |
| [`public void SetAsInternal<T>(ProjectSetting<T> setting, bool internalSetting)`](#m-electron2d-projectsettings-setasinternal-1-electron2d-projectsetting-0-system-boolean) | Sets whether a setting is hidden from ordinary tooling discovery. |
| [`public bool IsRestartRequired<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-isrestartrequired-1-electron2d-projectsetting-0) | Gets whether changing a setting requires the host application to restart. |
| [`public void SetRestartIfChanged<T>(ProjectSetting<T> setting, bool restart)`](#m-electron2d-projectsettings-setrestartifchanged-1-electron2d-projectsetting-0-system-boolean) | Sets whether changing a setting requires the host application to restart. |
| [`public IReadOnlyList<string> GetChangedSettings()`](#m-electron2d-projectsettings-getchangedsettings) | Gets setting names included in the pending or currently delivered change notification. |
| [`public bool CheckChangedSettingsInGroup(string prefix)`](#m-electron2d-projectsettings-checkchangedsettingsingroup-system-string) | Checks whether any changed setting starts with a category prefix. |
| [`public void Load()`](#m-electron2d-projectsettings-load) | Loads the project file and then merges the conventional override file when it exists. |
| [`public void LoadCustom(string path)`](#m-electron2d-projectsettings-loadcustom-system-string) | Merges a custom configuration file into the current settings after full validation. |
| [`public void Save()`](#m-electron2d-projectsettings-save) | Saves the current document to [`ProjectSettings.ProjectFilePath`](ProjectSettings.md#p-electron2d-projectsettings-projectfilepath) using atomic replacement. |
| [`public void SaveCustom(string path)`](#m-electron2d-projectsettings-savecustom-system-string) | Saves the current document to an explicit file using atomic replacement. |
| [`public bool FlushChanges()`](#m-electron2d-projectsettings-flushchanges) | Raises one coalesced [`ProjectSettings.SettingsChanged`](ProjectSettings.md#e-electron2d-projectsettings-settingschanged) notification when changes are pending. |
| [`public string GlobalizePath(string path)`](#m-electron2d-projectsettings-globalizepath-system-string) | Converts a virtual or ordinary path to an absolute native operating-system path. |
| [`public string LocalizePath(string path)`](#m-electron2d-projectsettings-localizepath-system-string) | Converts an ordinary path inside a configured root to a virtual project or user path. |
| [`public static string FindProjectRoot(string startPath)`](#m-electron2d-projectsettings-findprojectroot-system-string) | Searches an existing directory and its parents for [`ProjectSettings.ProjectFileName`](ProjectSettings.md#f-electron2d-projectsettings-projectfilename). |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-projectsettings-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override void ValidateDisposal()`](#m-electron2d-projectsettings-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-projectsettings-dispose-system-boolean) | Releases resources owned by a derived class. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<ProjectSettings> SettingsChanged`](#e-electron2d-projectsettings-settingschanged) | Occurs after one or more settings change and [`ProjectSettings.FlushChanges`](ProjectSettings.md#m-electron2d-projectsettings-flushchanges) is called. |

## Constants

| Member | Description |
| --- | --- |
| [`public const string ProjectFileName = "project.e2d"`](#f-electron2d-projectsettings-projectfilename) | Gets the conventional project settings file name. |
| [`public const string OverrideFileName = "override.cfg"`](#f-electron2d-projectsettings-overridefilename) | Gets the conventional optional runtime override file name. |
| [`public const string ProjectDataDirectoryName = ".electron2d"`](#f-electron2d-projectsettings-projectdatadirectoryname) | Gets the project-local directory name reserved for generated engine data. |

## Constructor Descriptions

<a id="m-electron2d-projectsettings-ctor-system-string"></a>
### `public ProjectSettings(string projectRoot)`

Initializes an isolated registry using a platform-appropriate user-data directory.

**Parameters**

- `projectRoot`: An existing project directory.

**Exceptions**

- `ArgumentNullException`: `projectRoot` is `null`.
- `ArgumentException`: `projectRoot` is empty or invalid.
- `IO.DirectoryNotFoundException`: `projectRoot` does not exist.

<a id="m-electron2d-projectsettings-ctor-system-string-system-string"></a>
### `public ProjectSettings(string projectRoot, string userDataRoot)`

Initializes an isolated registry with explicit project and user-data directories.

**Parameters**

- `projectRoot`: An existing project directory.
- `userDataRoot`: The user-writable data directory; it is not created automatically.

**Exceptions**

- `ArgumentNullException`: `projectRoot` or `userDataRoot` is `null`.
- `ArgumentException`: A path is empty or invalid, or both paths resolve to the same directory.
- `IO.DirectoryNotFoundException`: `projectRoot` does not exist.

## Property Descriptions

<a id="diagnostics-debugpathscolor"></a>
### `public static ProjectSetting<Color> DebugPathsColor { get; }`

Defines the color of path curves and tangent markers when scene path diagnostics are enabled.

Contract: Defaults to (0.1, 1, 0.7, 0.4). Channels must be finite. Active feature overrides apply at SceneTree construction; existing trees retain their color. Diagnostics draw through the ordinary canvas backends.


<a id="p-electron2d-projectsettings-applicationname"></a>
### `public static ProjectSetting<string> ApplicationName { get; }`

Defines the human-readable application name.

<a id="p-electron2d-projectsettings-applicationversion"></a>
### `public static ProjectSetting<string> ApplicationVersion { get; }`

Defines the application version string.

<a id="p-electron2d-projectsettings-physicstickspersecond"></a>
### `public static ProjectSetting<int> PhysicsTicksPerSecond { get; }`

Defines the fixed-step callback frequency used by [`Engine`](Engine.md).

<a id="p-electron2d-projectsettings-maxphysicsstepsperframe"></a>
### `public static ProjectSetting<int> MaxPhysicsStepsPerFrame { get; }`

Defines the maximum fixed-step callbacks processed during one frame.

<a id="p-electron2d-projectsettings-physicsjitterfix"></a>
### `public static ProjectSetting<double> PhysicsJitterFix { get; }`

Defines the finite non-negative fixed-step boundary tolerance.

<a id="p-electron2d-projectsettings-renderingmethod"></a>
### `public static ProjectSetting<string> RenderingMethod { get; }`

Defines `rendering/renderer/rendering_method`, default `gpu`. Only `gpu` and `compatibility` are valid. Engine.Run reads the active feature override when opening the renderer. Changing the setting does not switch an active renderer; the new value applies to the next run. RenderingServer.GetCurrentRenderingMethod reports the method actually selected after startup fallback.

<a id="p-electron2d-projectsettings-renderingfallback"></a>
### `public static ProjectSetting<bool> RenderingFallback { get; }`

Defines `rendering/rendering_device/fallback_to_opengl3`, default `true`. This key controls startup fallback from SDL GPU to SDL_Renderer; the compatibility driver may use a different graphics API. Set `false` to require GPU initialization. Shader materials reject compatibility rendering even if fallback was allowed. This setting provides no live device-loss recovery.

### SnapTransformsToPixel

`public static ProjectSetting<bool> SnapTransformsToPixel { get; }`

Defines `rendering/2d/snap/snap_2d_transforms_to_pixel`, default false. New Window construction reads the active feature override. Load project settings before creating the explicit root window. Existing viewports retain their own value; use Viewport.SnapTransformsToPixel to change them. This is initialization for the typed window host, not a continuously applied global override.

### SnapVerticesToPixel

`public static ProjectSetting<bool> SnapVerticesToPixel { get; }`

Defines `rendering/2d/snap/snap_2d_vertices_to_pixel`, default false. Construction and override timing match SnapTransformsToPixel. Controls final primitive vertices independently of transform rounding.

### UseNearestMipmapFilter

`public static ProjectSetting<bool> UseNearestMipmapFilter { get; }`

Defines `rendering/textures/default_filters/use_nearest_mipmap_filter`, default false. The GPU renderer reads the active feature override on construction: false linearly interpolates mip levels for canvas samplers, true chooses the nearest mip. Changes apply to the next renderer run. Non-mipmap filters remain restricted to level zero. Fixed named-material samplers retain their existing nearest-mip profile.

### AnisotropicFilteringLevel

`public static ProjectSetting<int> AnisotropicFilteringLevel { get; }`

Defines `rendering/textures/default_filters/anisotropic_filtering_level`, default 2. Values 0..4 mean disabled, 2, 4, 8 or 16 samples; invalid writes throw ArgumentOutOfRangeException before mutation. New Viewports read the active feature override at construction. Change Viewport.AnisotropicFilteringLevel to update an existing viewport. Only anisotropic canvas filters use the limit.

<a id="p-electron2d-projectsettings-defaultclearcolor"></a>
### `public static ProjectSetting<Color> DefaultClearColor { get; }`

Defines `rendering/environment/defaults/default_clear_color`, default `(0.3, 0.3, 0.3, 1)`. Every channel must be finite; invalid writes fail before mutation. The renderer reads the active feature override during startup. Use RenderingServer.SetDefaultClearColor to change the active renderer's color; it does not change this stored setting. Normalized framebuffer output clamps channels to `[0, 1]`.

<a id="p-electron2d-projectsettings-instance"></a>
### `public static ProjectSettings Instance { get; }`

Gets the process-wide project settings registry.

**Value:** The same non-disposable instance for the lifetime of the process.

<a id="p-electron2d-projectsettings-projectroot"></a>
### `public string ProjectRoot { get; }`

Gets the current absolute project resource directory.

**Value:** The directory to which `res://` resolves.

<a id="p-electron2d-projectsettings-userdataroot"></a>
### `public string UserDataRoot { get; }`

Gets the current absolute user-data directory.

**Value:** The directory to which `user://` resolves. The directory may not exist yet.

<a id="p-electron2d-projectsettings-projectfilepath"></a>
### `public string ProjectFilePath { get; }`

Gets the absolute path of the current project settings file.

**Value:** [`ProjectSettings.ProjectFileName`](ProjectSettings.md#f-electron2d-projectsettings-projectfilename) inside [`ProjectSettings.ProjectRoot`](ProjectSettings.md#p-electron2d-projectsettings-projectroot).

<a id="p-electron2d-projectsettings-overridefilepath"></a>
### `public string OverrideFilePath { get; }`

Gets the absolute path of the optional runtime override file.

**Value:** [`ProjectSettings.OverrideFileName`](ProjectSettings.md#f-electron2d-projectsettings-overridefilename) inside [`ProjectSettings.ProjectRoot`](ProjectSettings.md#p-electron2d-projectsettings-projectroot).

<a id="p-electron2d-projectsettings-projectdatapath"></a>
### `public string ProjectDataPath { get; }`

Gets the absolute path reserved for generated project-local engine data.

**Value:** [`ProjectSettings.ProjectDataDirectoryName`](ProjectSettings.md#f-electron2d-projectsettings-projectdatadirectoryname) inside [`ProjectSettings.ProjectRoot`](ProjectSettings.md#p-electron2d-projectsettings-projectroot).

<a id="p-electron2d-projectsettings-version"></a>
### `public ulong Version { get; }`

Gets a monotonically increasing in-process registry version.

**Value:** A value starting at one and incremented after registry, value, or active-feature changes.

### RenderingTimeRolloverSeconds

`public static ProjectSetting<double> RenderingTimeRolloverSeconds { get; }`

Defines `rendering/limits/time/time_rollover_secs`, default 3600 seconds. Finite positive values are accepted; zero, negative or nonserializable values fail before committing a setting change. The active feature override is read on each submitted frame, so changes apply to an existing renderer. The render clock adds the scaled scheduled process step and takes its remainder by this limit. It starts at zero for each Engine.Run, freezes when rendering is disabled/hidden, and continues while the tree is paused.

This setting drives [canvas animation intervals](CanvasItem.md#drawanimationslice) and the optional GPU fragment [TIME built-in](../components/shader-materials.md#render-time). The clock is double precision; shader upload converts it to float32, with the usual loss of precision at large values. A TIME-using shader rejects an out-of-range clock before drawing. CanvasTimingTests checks default/validation; CanvasTimingRenderingTests checks the clock on both Linux backends; ShaderTimeRenderingTests checks actual HLSL/GLSL output, live rollover, pause, time scale and cleanup on Wayland/Vulkan.


## Method Descriptions

<a id="m-electron2d-projectsettings-configurepaths-system-string-system-string"></a>
### `public void ConfigurePaths(string projectRoot, string userDataRoot)`

Changes the project and user-data roots and clears all loaded or explicitly stored values.

**Parameters**

- `projectRoot`: An existing project directory.
- `userDataRoot`: The user-writable data directory; it is not created automatically.

**Exceptions**

- `ArgumentNullException`: A path is `null`.
- `ArgumentException`: A path is empty or invalid, or both paths resolve to the same directory.
- `IO.DirectoryNotFoundException`: `projectRoot` does not exist.
- `InvalidOperationException`: Unsaved setting changes exist or a load is already running.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** Registered setting definitions and metadata are retained. Call this during host setup before runtime use.

<a id="m-electron2d-projectsettings-register-1-electron2d-projectsetting-0"></a>
### `public void Register<T>(ProjectSetting<T> setting)`

Registers a setting definition for typed access and tooling discovery.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The definition to register.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `InvalidOperationException`: The name is already registered or a load is running.
- `IO.InvalidDataException`: A loaded value cannot be decoded as `T`.
- `ArgumentOutOfRangeException`: The setting validator rejects a loaded value.
- `ObjectDisposedException`: The registry is disposing or disposed.
- `Exception`: A property-list listener throws after registration has completed.

**Remarks:** If a matching value was loaded before registration, it and all matching feature overrides are validated before
the registry changes. Registration does not mark the setting as modified.

<a id="m-electron2d-projectsettings-unregister-1-electron2d-projectsetting-0"></a>
### `public void Unregister<T>(ProjectSetting<T> setting)`

Unregisters a setting and removes its stored base value and feature overrides.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: The setting is built in, a different definition owns its name, or a load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.
- `Exception`: A property-list listener throws after removal has completed.

**Remarks:** Built-in settings cannot be unregistered. The removal is tracked as an unsaved change.

<a id="m-electron2d-projectsettings-hassetting-1-electron2d-projectsetting-0"></a>
### `public bool HasSetting<T>(ProjectSetting<T> setting)`

Determines whether the exact setting definition is registered.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The definition to inspect.

**Returns:** `true` only when this exact definition owns its name in this registry.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-getsettingnames-system-boolean"></a>
### `public IReadOnlyList<string> GetSettingNames(bool includeInternal = true)`

Gets registered setting names ordered by explicit order and then by name.

**Parameters**

- `includeInternal`: Whether settings marked as internal are included.

**Returns:** An immutable snapshot of full case-sensitive setting paths.

**Exceptions**

- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-get-1-electron2d-projectsetting-0"></a>
### `public T Get<T>(ProjectSetting<T> setting)`

Gets a setting value without applying feature overrides.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.

**Returns:** An independent snapshot of the explicit value, or the current initial value when no explicit value exists.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-getwithoverride-1-electron2d-projectsetting-0"></a>
### `public T GetWithOverride<T>(ProjectSetting<T> setting)`

Gets a setting after applying the first override matching an active platform, build, or custom feature.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.

**Returns:** An independent value snapshot.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** Override precedence follows the insertion order of override entries in the loaded or modified document.

<a id="m-electron2d-projectsettings-getwithoverride-1-electron2d-projectsetting-0-system-collections-generic-ienumerable-system-string"></a>
### `public T GetWithOverride<T>(ProjectSetting<T> setting, IEnumerable<string> features)`

Gets a setting after applying overrides against caller-supplied feature tags.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.
- `features`: Feature tags replacing the registry's current active feature set for this lookup.

**Returns:** An independent value snapshot.

**Exceptions**

- `ArgumentNullException`: `setting`, `features`, or one of its items is `null`.
- `ArgumentException`: A feature tag is invalid.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** Tags are normalized to lowercase. Override precedence follows stored override insertion order.

<a id="m-electron2d-projectsettings-set-1-electron2d-projectsetting-0-0"></a>
### `public void Set<T>(ProjectSetting<T> setting, T value)`

Stores a base value for a registered setting.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.
- `value`: The non-null value to validate and snapshot.

**Exceptions**

- `ArgumentNullException`: `setting` or `value` is `null`.
- `ArgumentOutOfRangeException`: The setting validator rejects `value`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name or a load is running.
- `Text.Json.JsonException`: `value` cannot be serialized.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** An equal serialized value is a no-op and does not update change tracking or [`ProjectSettings.Version`](ProjectSettings.md#p-electron2d-projectsettings-version).

<a id="m-electron2d-projectsettings-reset-1-electron2d-projectsetting-0"></a>
### `public void Reset<T>(ProjectSetting<T> setting)`

Removes an explicit base value so the setting returns its current initial value.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name or a load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** Feature overrides are retained. The initial value becomes implicit and is not persisted.

<a id="m-electron2d-projectsettings-setinitialvalue-1-electron2d-projectsetting-0-0"></a>
### `public void SetInitialValue<T>(ProjectSetting<T> setting, T value)`

Changes the value used by [`ProjectSettings.Reset``1(ProjectSetting{``0})`](ProjectSettings.md#m-electron2d-projectsettings-reset-1-electron2d-projectsetting-0) and typed property reversion.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.
- `value`: The new non-null validated initial value.

**Exceptions**

- `ArgumentNullException`: `setting` or `value` is `null`.
- `ArgumentOutOfRangeException`: The setting validator rejects `value`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name or a load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** The current value and unsaved state do not change. If necessary, the previous current value becomes explicit so
changing the implicit value cannot change what callers observe.

<a id="m-electron2d-projectsettings-setfeatureoverride-1-electron2d-projectsetting-0-system-string-0"></a>
### `public void SetFeatureOverride<T>(ProjectSetting<T> setting, string feature, T value)`

Stores a value selected when the named feature is active.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.
- `feature`: The platform, build, architecture, or custom feature tag.
- `value`: The non-null value to validate and snapshot.

**Exceptions**

- `ArgumentNullException`: An argument is `null`.
- `ArgumentException`: `feature` is invalid.
- `ArgumentOutOfRangeException`: The setting validator rejects `value`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name or a load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** New overrides have lower priority than existing overrides for the same setting.

<a id="m-electron2d-projectsettings-clearfeatureoverride-1-electron2d-projectsetting-0-system-string"></a>
### `public bool ClearFeatureOverride<T>(ProjectSetting<T> setting, string feature)`

Removes one stored feature override.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.
- `feature`: The feature tag to remove.

**Returns:** `true` when an override was removed; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `setting` or `feature` is `null`.
- `ArgumentException`: `feature` is invalid.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name or a load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-getfeatureoverrides-1-electron2d-projectsetting-0"></a>
### `public IReadOnlyList<string> GetFeatureOverrides<T>(ProjectSetting<T> setting)`

Gets the stored feature tags for one setting in override precedence order.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.

**Returns:** An immutable snapshot of normalized feature tags.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-addcustomfeature-system-string"></a>
### `public bool AddCustomFeature(string feature)`

Adds a custom feature to the process feature set.

**Parameters**

- `feature`: The feature tag to add.

**Returns:** `true` when the active set changed; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `feature` is `null`.
- `ArgumentException`: `feature` is invalid.
- `InvalidOperationException`: A load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-removecustomfeature-system-string"></a>
### `public bool RemoveCustomFeature(string feature)`

Removes a custom feature from the process feature set.

**Parameters**

- `feature`: The feature tag to remove.

**Returns:** `true` when the active set changed; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `feature` is `null`.
- `ArgumentException`: `feature` is invalid.
- `InvalidOperationException`: A load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-hasfeature-system-string"></a>
### `public bool HasFeature(string feature)`

Determines whether a platform, build, architecture, or custom feature is active.

**Parameters**

- `feature`: The feature tag to inspect.

**Returns:** `true` when the normalized feature is active.

**Exceptions**

- `ArgumentNullException`: `feature` is `null`.
- `ArgumentException`: `feature` is invalid.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-getactivefeatures"></a>
### `public IReadOnlyList<string> GetActiveFeatures()`

Gets all active platform, build, architecture, and custom features.

**Returns:** An immutable ordinally sorted snapshot of normalized feature tags.

**Exceptions**

- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-getorder-1-electron2d-projectsetting-0"></a>
### `public int GetOrder<T>(ProjectSetting<T> setting)`

Gets the persistence order assigned to a registered setting.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.

**Returns:** The signed ordering value; lower values appear first.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-setorder-1-electron2d-projectsetting-0-system-int32"></a>
### `public void SetOrder<T>(ProjectSetting<T> setting, int order)`

Changes the persistence and tooling order of a registered setting.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.
- `order`: The signed ordering value.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name or a load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.
- `Exception`: A property-list listener throws after the order has changed.

<a id="m-electron2d-projectsettings-isbasic-1-electron2d-projectsetting-0"></a>
### `public bool IsBasic<T>(ProjectSetting<T> setting)`

Gets whether a setting should appear in a reduced basic-settings view.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.

**Returns:** The current basic-view flag.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-setasbasic-1-electron2d-projectsetting-0-system-boolean"></a>
### `public void SetAsBasic<T>(ProjectSetting<T> setting, bool basic)`

Sets whether a setting should appear in a reduced basic-settings view.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.
- `basic`: The new basic-view flag.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name or a load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.
- `Exception`: A property-list listener throws after the flag has changed.

<a id="m-electron2d-projectsettings-isinternal-1-electron2d-projectsetting-0"></a>
### `public bool IsInternal<T>(ProjectSetting<T> setting)`

Gets whether a setting is hidden from ordinary tooling discovery.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.

**Returns:** The current internal flag.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-setasinternal-1-electron2d-projectsetting-0-system-boolean"></a>
### `public void SetAsInternal<T>(ProjectSetting<T> setting, bool internalSetting)`

Sets whether a setting is hidden from ordinary tooling discovery.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.
- `internalSetting`: The new internal flag.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name or a load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.
- `Exception`: A property-list listener throws after the flag has changed.

<a id="m-electron2d-projectsettings-isrestartrequired-1-electron2d-projectsetting-0"></a>
### `public bool IsRestartRequired<T>(ProjectSetting<T> setting)`

Gets whether changing a setting requires the host application to restart.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.

**Returns:** The current restart-required flag.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-setrestartifchanged-1-electron2d-projectsetting-0-system-boolean"></a>
### `public void SetRestartIfChanged<T>(ProjectSetting<T> setting, bool restart)`

Sets whether changing a setting requires the host application to restart.

**Type parameters**

- `T`: The non-null setting value type.

**Parameters**

- `setting`: The exact registered definition.
- `restart`: The new restart-required flag.

**Exceptions**

- `ArgumentNullException`: `setting` is `null`.
- `Collections.Generic.KeyNotFoundException`: `setting` is not registered.
- `InvalidOperationException`: A different definition owns the same name or a load is running.
- `ObjectDisposedException`: The registry is disposing or disposed.
- `Exception`: A property-list listener throws after the flag has changed.

<a id="m-electron2d-projectsettings-getchangedsettings"></a>
### `public IReadOnlyList<string> GetChangedSettings()`

Gets setting names included in the pending or currently delivered change notification.

**Returns:** An immutable ordinally sorted snapshot, cleared after the notification finishes.

**Exceptions**

- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-checkchangedsettingsingroup-system-string"></a>
### `public bool CheckChangedSettingsInGroup(string prefix)`

Checks whether any changed setting starts with a category prefix.

**Parameters**

- `prefix`: The case-sensitive prefix to inspect.

**Returns:** `true` when at least one changed setting starts with `prefix`.

**Exceptions**

- `ArgumentNullException`: `prefix` is `null`.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-load"></a>
### `public void Load()`

Loads the project file and then merges the conventional override file when it exists.

**Exceptions**

- `IO.FileNotFoundException`: The project file does not exist.
- `IO.IOException`: A file cannot be read.
- `UnauthorizedAccessException`: A file cannot be read by the caller.
- `FormatException`: A document is malformed.
- `IO.InvalidDataException`: A registered value has the wrong type.
- `ArgumentOutOfRangeException`: A registered validator rejects a loaded value.
- `InvalidOperationException`: A re-entrant load or setting mutation is attempted.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** The complete candidate document and every registered typed value are validated before current state is replaced.
Successful loading clears unsaved-value tracking and any pending change notification.

<a id="m-electron2d-projectsettings-loadcustom-system-string"></a>
### `public void LoadCustom(string path)`

Merges a custom configuration file into the current settings after full validation.

**Parameters**

- `path`: An operating-system, `res://`, or `user://` file path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The file cannot be read.
- `UnauthorizedAccessException`: The file cannot be read by the caller.
- `FormatException`: The document is malformed.
- `IO.InvalidDataException`: A registered value has the wrong type.
- `ArgumentOutOfRangeException`: A registered validator rejects a loaded value.
- `InvalidOperationException`: A re-entrant load or setting mutation is attempted.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** Values already unsaved before the merge remain unsaved because loading another document does not persist them.
Values supplied only by the loaded file are treated as persisted and do not add unsaved names.

<a id="m-electron2d-projectsettings-save"></a>
### `public void Save()`

Saves the current document to [`ProjectSettings.ProjectFilePath`](ProjectSettings.md#p-electron2d-projectsettings-projectfilepath) using atomic replacement.

**Exceptions**

- `IO.IOException`: The file cannot be written or replaced.
- `UnauthorizedAccessException`: The caller cannot write the file.
- `IO.DirectoryNotFoundException`: The project directory no longer exists.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** A successful save clears internal unsaved-value tracking. Change-notification names remain until
[`ProjectSettings.FlushChanges`](ProjectSettings.md#m-electron2d-projectsettings-flushchanges) completes; a failed save preserves both states.

<a id="m-electron2d-projectsettings-savecustom-system-string"></a>
### `public void SaveCustom(string path)`

Saves the current document to an explicit file using atomic replacement.

**Parameters**

- `path`: An operating-system, `res://`, or `user://` file path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The file cannot be written or replaced.
- `UnauthorizedAccessException`: The caller cannot write the file.
- `IO.DirectoryNotFoundException`: The destination directory does not exist.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** A successful save clears internal unsaved-value tracking. Change-notification names remain until
[`ProjectSettings.FlushChanges`](ProjectSettings.md#m-electron2d-projectsettings-flushchanges) completes; a failed save preserves both states.

<a id="m-electron2d-projectsettings-flushchanges"></a>
### `public bool FlushChanges()`

Raises one coalesced [`ProjectSettings.SettingsChanged`](ProjectSettings.md#e-electron2d-projectsettings-settingschanged) notification when changes are pending.

**Returns:** `true` when an event invocation was consumed; otherwise `false`.

**Exceptions**

- `ObjectDisposedException`: The registry is disposing or disposed.
- `Exception`: An event handler throws.

**Remarks:** The callback runs without the registry lock, so handlers may read or modify settings.

<a id="m-electron2d-projectsettings-globalizepath-system-string"></a>
### `public string GlobalizePath(string path)`

Converts a virtual or ordinary path to an absolute native operating-system path.

**Parameters**

- `path`: A nonempty path, optionally beginning with `res://` or `user://`.

**Returns:** An absolute normalized path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `UnauthorizedAccessException`: A virtual path escapes its configured root.
- `NotSupportedException`: `path` uses an unknown virtual-path scheme.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** Virtual paths are lexically confined to their configured roots and reject parent traversal. This method does not
resolve symbolic links and is not a filesystem security sandbox.

<a id="m-electron2d-projectsettings-localizepath-system-string"></a>
### `public string LocalizePath(string path)`

Converts an ordinary path inside a configured root to a virtual project or user path.

**Parameters**

- `path`: A nonempty operating-system or already virtual path.

**Returns:** A `res://` or `user://` path when contained by a configured root; otherwise an absolute native path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `NotSupportedException`: `path` uses an unknown virtual-path scheme.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-findprojectroot-system-string"></a>
### `public static string FindProjectRoot(string startPath)`

Searches an existing directory and its parents for [`ProjectSettings.ProjectFileName`](ProjectSettings.md#f-electron2d-projectsettings-projectfilename).

**Parameters**

- `startPath`: An existing directory or file-system entry whose containing directory starts the search.

**Returns:** The absolute directory containing the nearest project file.

**Exceptions**

- `ArgumentNullException`: `startPath` is `null`.
- `ArgumentException`: `startPath` is empty or invalid.
- `IO.DirectoryNotFoundException`: The starting location does not exist.
- `IO.FileNotFoundException`: No project file exists at or above the starting location.

<a id="m-electron2d-projectsettings-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

<a id="m-electron2d-projectsettings-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

<a id="m-electron2d-projectsettings-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

## Event Descriptions

<a id="e-electron2d-projectsettings-settingschanged"></a>
### `public event Action<ProjectSettings> SettingsChanged`

Occurs after one or more settings change and [`ProjectSettings.FlushChanges`](ProjectSettings.md#m-electron2d-projectsettings-flushchanges) is called.

**Remarks:** Delivery is synchronous on the flushing thread and is coalesced to one invocation per flush. A change made by
a handler is retained for the next flush. The process-wide [`Engine`](Engine.md) flushes at the end of each
successfully processed frame. Handler exceptions propagate after the pending invocation has been consumed.

## Constant Descriptions

<a id="f-electron2d-projectsettings-projectfilename"></a>
### `public const string ProjectFileName = "project.e2d"`

Gets the conventional project settings file name.

<a id="f-electron2d-projectsettings-overridefilename"></a>
### `public const string OverrideFileName = "override.cfg"`

Gets the conventional optional runtime override file name.

<a id="f-electron2d-projectsettings-projectdatadirectoryname"></a>
### `public const string ProjectDataDirectoryName = ".electron2d"`

Gets the project-local directory name reserved for generated engine data.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Lifecycle and threading

All public registry operations preserve internal state under concurrency. Value/metadata transitions and document swaps are linearized by one lock. User validators run synchronously on caller threads and must themselves be deterministic, thread-safe, and free of registry mutations. Serialization, validators, file I/O, snapshots, and path operations may allocate/block and are not frame-hot APIs. Scalar/string setting definitions cache their most recently decoded representation; Engine timing reads therefore remain allocation-free after warm-up. Mutable reference values are never returned from that cache.

Isolated instances dispose their owned document and clear subscribers/state. The process singleton rejects disposal. Disposal during an active load validator is rejected so the transaction cannot be invalidated re-entrantly.

## Verification and known limitations

The executable harness covers malformed definitions/features/paths, exact registration identity, built-ins, mutable snapshot isolation, validators and rollback, initial/revert behavior, metadata/property discovery and post-commit callback failure, override precedence/current/custom features, changed groups/version/no-op writes, event coalescing/re-entry/failure, OS/virtual path round trips/traversal, root discovery, save/load/override/late registration, failed I/O preservation, re-entrant validator rejection, concurrency, reconfiguration, disposal, Engine feature lookup/event flushing, and warmed zero-allocation Engine frames.

Tests do not prove crash durability on every filesystem, symbolic-link confinement, editor presentation, packed exports, or unavailable domain settings.
