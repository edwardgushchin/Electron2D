# ProjectSettings

Last updated: 2026-10-09

**Inherits:** [ProjectSettingsRegistry](ProjectSettingsRegistry.md)

**Inherited By:** —

- **Source:** [`src/Core/Config/ProjectSettings.cs`](../../src/Core/Config/ProjectSettings.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed partial class ProjectSettings : ProjectSettingsRegistry`

> Stores globally accessible, strongly typed project settings and resolves project virtual paths.

Public static declarations are in [`ProjectSettings.API.cs`](../../src/Core/Config/ProjectSettings.API.cs).

## Description

Public operations and events use static access to the retained object under [ADR 0095](../decisions/singleton-services.md#adr-0095). Object state, identity, property discovery and the owning domain lifetime rules remain intact.

Stores globally accessible, strongly typed project settings and resolves project virtual paths.

`ProjectSettings` owns a typed registry of project-wide values, metadata, feature overrides, independent unsaved/change-notification state, atomic persistence through [`ConfigFile`](ConfigFile.md), and lexical resolution of `res://` and `user://`. Static operations address the permanent runtime registry, registered as the built-in `ProjectSettings` entry in [`Engine`](Engine.md). Construct [`ProjectSettingsRegistry`](ProjectSettingsRegistry.md) for disposable isolated registries used by tools or unopened projects.

The retained registry owns its in-memory `ConfigFile`. It does not own project/user directories and does not create the user directory. Unknown entry values loaded from disk are retained semantically in canonical encoding and can be claimed later by registering a compatible typed definition.

[`ProjectSettings`](ProjectSettings.md) is the process-wide runtime registry. Additional `ProjectSettingsRegistry` objects may be created for isolated
tooling, tests, or multiple unopened projects. All public static operations are thread-safe; disk and serialization
operations are not suitable for real-time callbacks.

Settings must be registered before typed access. Unknown values loaded from disk remain preserved and become
available when a matching typed setting is later registered. The registry never exposes an untyped value API.

Six non-unregisterable built-in `ProjectSetting<InputActionSettings>` definitions store GUI focus actions under `input/ui_*`. Other actions may be registered with the same value type and `input/<action>` key. After `Load`, call [`InputMap.LoadFromProjectSettings`](InputMap.md#m-electron2d-inputmap-loadfromprojectsettings) during project setup to replace the live map. A settings change alone does not reload it.

Two built-in locale settings select an optional test locale and a fallback catalog locale. Nine further built-in settings configure the main translation domain's pseudolocalization; one setting selects the initial scene-root auto-translation mode. `Engine.Run` samples catalog settings before creating its scene; `Engine.Start` samples them before attaching the supplied loop. `SceneTree` samples the root mode during construction. The locale and pseudolocalization enablement settings are sampled at startup, while [`TranslationServer.ReloadPseudolocalization`](TranslationServer.md#reloadpseudolocalization) reloads the eight transform options. Assign `TranslationServer.Culture` or `TranslationServer.PseudolocalizationEnabled` for an immediate runtime change.

## Examples

The following focused snippet uses the current public static API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
string resourcePath = ProjectSettings.GlobalizePath("res://levels/intro.scene");
```

## Properties

| Member | Description |
| --- | --- |
| [`public static ProjectSetting<Color> DebugPathsColor { get; }`](#diagnostics-debugpathscolor) | Defines the color of path curves and tangent markers when scene path diagnostics are enabled. |
| [`public static ProjectSetting<string> ApplicationName { get; }`](#p-electron2d-projectsettings-applicationname) | Defines the human-readable application name. |
| [`public static ProjectSetting<string> ApplicationVersion { get; }`](#p-electron2d-projectsettings-applicationversion) | Defines the application version string. |
| [`public static ProjectSetting<int> PhysicsTicksPerSecond { get; }`](#p-electron2d-projectsettings-physicstickspersecond) | Defines the fixed-step callback frequency used by [`Engine`](Engine.md). |
| [`public static ProjectSetting<float> Physics2DDefaultGravity { get; }`](#p-electron2d-projectsettings-physics2ddefaultgravity) | Default 2D gravity strength, 980 scene units/s². |
| [`public static ProjectSetting<Vector2> Physics2DDefaultGravityVector { get; }`](#p-electron2d-projectsettings-physics2ddefaultgravityvector) | Default 2D gravity direction, (0, 1) without normalization. |
| [`public static ProjectSetting<float> Physics2DDefaultLinearDamp { get; }`](#p-electron2d-projectsettings-physics2ddefaultlineardamp) | Default 2D linear damping, 0.1/s. |
| [`public static ProjectSetting<float> Physics2DDefaultConstraintBias { get; }`](#physics2ddefaultconstraintbias) | Default joint positional correction fraction, 0.2. |
| [`public static ProjectSetting<float> Physics2DSleepThresholdLinear { get; }`](#physics2dsleepthresholdlinear) | World linear sleep threshold, 2 scene units/s. |
| [`public static ProjectSetting<float> Physics2DSleepThresholdAngular { get; }`](#physics2dsleepthresholdangular) | World angular sleep threshold, 0.13962634 rad/s. |
| [`public static ProjectSetting<float> Physics2DTimeBeforeSleep { get; }`](#physics2dtimebeforesleep) | World quiet duration, 0.5 s. |
| [`public static ProjectSetting<float> Physics2DDefaultAngularDamp { get; }`](#p-electron2d-projectsettings-physics2ddefaultangulardamp) | Default 2D angular damping, 1/s. |
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
| [`public static ProjectSetting<string> LocaleTest { get; }`](#localetest) | Optional startup locale override; empty by default. |
| [`public static ProjectSetting<string> LocaleFallback { get; }`](#localefallback) | Fallback catalog locale; `en` by default. |
| [`public static ProjectSetting<bool> RootNodeAutoTranslate { get; }`](#rootnodeautotranslate) | Initial scene-root automatic translation; true by default. |
| [`public static ProjectSetting<bool> PhysicsInterpolation { get; }`](#physicsinterpolation) | Initializes scene-wide 2D physics presentation interpolation; false by default. |
| [`public static ProjectSetting<bool> PseudolocalizationEnabled { get; }`](#pseudolocalizationenabled) | Startup enablement; false by default. |
| [`public static ProjectSetting<bool> PseudolocalizationReplaceWithAccents { get; }`](#pseudolocalizationreplacewithaccents) | Accent substitution; true by default. |
| [`public static ProjectSetting<bool> PseudolocalizationDoubleVowels { get; }`](#pseudolocalizationdoublevowels) | Vowel doubling; false by default. |
| [`public static ProjectSetting<bool> PseudolocalizationFakeBIDI { get; }`](#pseudolocalizationfakebidi) | Direction controls; false by default. |
| [`public static ProjectSetting<bool> PseudolocalizationOverride { get; }`](#pseudolocalizationoverride) | Character masking; false by default. |
| [`public static ProjectSetting<float> PseudolocalizationExpansionRatio { get; }`](#pseudolocalizationexpansionratio) | Non-negative finite expansion; zero by default. |
| [`public static ProjectSetting<string> PseudolocalizationPrefix { get; }`](#pseudolocalizationprefix) | Text prefix; `[` by default. |
| [`public static ProjectSetting<string> PseudolocalizationSuffix { get; }`](#pseudolocalizationsuffix) | Text suffix; `]` by default. |
| [`public static ProjectSetting<bool> PseudolocalizationSkipPlaceholders { get; }`](#pseudolocalizationskipplaceholders) | Placeholder preservation; true by default. |
| [`public static ProjectSetting<bool> IgnoreJoypadOnUnfocusedApplication { get; }`](#ignorejoypadonunfocusedapplication) | Initial native controller focus policy; false by default. |
| [`public static ProjectSetting<InputActionSettings> InputUIFocusNext { get; }`](#inputuifocusnext) | Defines Tab focus navigation. |
| [`public static ProjectSetting<InputActionSettings> InputUIFocusPrev { get; }`](#inputuifocusprev) | Defines Shift+Tab focus navigation. |
| [`public static ProjectSetting<InputActionSettings> InputUILeft { get; }`](#inputuileft) | Defines left-arrow, D-pad left and left-stick-left focus navigation. |
| [`public static ProjectSetting<InputActionSettings> InputUIUp { get; }`](#inputuiup) | Defines up-arrow, D-pad up and left-stick-up focus navigation. |
| [`public static ProjectSetting<InputActionSettings> InputUIRight { get; }`](#inputuiright) | Defines right-arrow, D-pad right and left-stick-right focus navigation. |
| [`public static ProjectSetting<InputActionSettings> InputUIDown { get; }`](#inputuidown) | Defines down-arrow, D-pad down and left-stick-down focus navigation. |
| [`public static ProjectSetting<InputActionSettings> InputUIHome { get; }`](#inputuihome) | Defines the Home endpoint action. |
| [`public static ProjectSetting<InputActionSettings> InputUIEnd { get; }`](#inputuiend) | Defines the End endpoint action. |
| [`public static string ProjectRoot { get; }`](#p-electron2d-projectsettings-projectroot) | Gets the current absolute project resource directory. |
| [`public static string UserDataRoot { get; }`](#p-electron2d-projectsettings-userdataroot) | Gets the current absolute user-data directory. |
| [`public static string ProjectFilePath { get; }`](#p-electron2d-projectsettings-projectfilepath) | Gets the absolute path of the current project settings file. |
| [`public static string OverrideFilePath { get; }`](#p-electron2d-projectsettings-overridefilepath) | Gets the absolute path of the optional runtime override file. |
| [`public static string ProjectDataPath { get; }`](#p-electron2d-projectsettings-projectdatapath) | Gets the absolute path reserved for generated project-local engine data. |
| [`public static ulong Version { get; }`](#p-electron2d-projectsettings-version) | Gets a monotonically increasing in-process registry version. |

## Methods

| Member | Description |
| --- | --- |
| [`public static void ConfigurePaths(string projectRoot, string userDataRoot)`](#m-electron2d-projectsettings-configurepaths-system-string-system-string) | Changes the project and user-data roots and clears all loaded or explicitly stored values. |
| [`public static void Register<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-register-1-electron2d-projectsetting-0) | Registers a setting definition for typed access and tooling discovery. |
| [`public static void Unregister<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-unregister-1-electron2d-projectsetting-0) | Unregisters a setting and removes its stored base value and feature overrides. |
| [`public static bool HasSetting<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-hassetting-1-electron2d-projectsetting-0) | Determines whether the exact setting definition is registered. |
| [`public static IReadOnlyList<string> GetSettingNames(bool includeInternal = true)`](#m-electron2d-projectsettings-getsettingnames-system-boolean) | Gets registered setting names ordered by explicit order and then by name. |
| [`public static T Get<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-get-1-electron2d-projectsetting-0) | Gets a setting value without applying feature overrides. |
| [`public static T GetWithOverride<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-getwithoverride-1-electron2d-projectsetting-0) | Gets a setting after applying the first override matching an active platform, build, or custom feature. |
| [`public static T GetWithOverride<T>(ProjectSetting<T> setting, IEnumerable<string> features)`](#m-electron2d-projectsettings-getwithoverride-1-electron2d-projectsetting-0-system-collections-generic-ienumerable-system-string) | Gets a setting after applying overrides against caller-supplied feature tags. |
| [`public static void Set<T>(ProjectSetting<T> setting, T value)`](#m-electron2d-projectsettings-set-1-electron2d-projectsetting-0-0) | Stores a base value for a registered setting. |
| [`public static void Reset<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-reset-1-electron2d-projectsetting-0) | Removes an explicit base value so the setting returns its current initial value. |
| [`public static void SetInitialValue<T>(ProjectSetting<T> setting, T value)`](#m-electron2d-projectsettings-setinitialvalue-1-electron2d-projectsetting-0-0) | Changes the value used by [`ProjectSettings.Reset``1(ProjectSetting{``0})`](ProjectSettings.md#m-electron2d-projectsettings-reset-1-electron2d-projectsetting-0) and typed property reversion. |
| [`public static void SetFeatureOverride<T>(ProjectSetting<T> setting, string feature, T value)`](#m-electron2d-projectsettings-setfeatureoverride-1-electron2d-projectsetting-0-system-string-0) | Stores a value selected when the named feature is active. |
| [`public static bool ClearFeatureOverride<T>(ProjectSetting<T> setting, string feature)`](#m-electron2d-projectsettings-clearfeatureoverride-1-electron2d-projectsetting-0-system-string) | Removes one stored feature override. |
| [`public static IReadOnlyList<string> GetFeatureOverrides<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-getfeatureoverrides-1-electron2d-projectsetting-0) | Gets the stored feature tags for one setting in override precedence order. |
| [`public static bool AddCustomFeature(string feature)`](#m-electron2d-projectsettings-addcustomfeature-system-string) | Adds a custom feature to the process feature set. |
| [`public static bool RemoveCustomFeature(string feature)`](#m-electron2d-projectsettings-removecustomfeature-system-string) | Removes a custom feature from the process feature set. |
| [`public static bool HasFeature(string feature)`](#m-electron2d-projectsettings-hasfeature-system-string) | Determines whether a platform, build, architecture, or custom feature is active. |
| [`public static IReadOnlyList<string> GetActiveFeatures()`](#m-electron2d-projectsettings-getactivefeatures) | Gets all active platform, build, architecture, and custom features. |
| [`public static int GetOrder<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-getorder-1-electron2d-projectsetting-0) | Gets the persistence order assigned to a registered setting. |
| [`public static void SetOrder<T>(ProjectSetting<T> setting, int order)`](#m-electron2d-projectsettings-setorder-1-electron2d-projectsetting-0-system-int32) | Changes the persistence and tooling order of a registered setting. |
| [`public static bool IsBasic<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-isbasic-1-electron2d-projectsetting-0) | Gets whether a setting should appear in a reduced basic-settings view. |
| [`public static void SetAsBasic<T>(ProjectSetting<T> setting, bool basic)`](#m-electron2d-projectsettings-setasbasic-1-electron2d-projectsetting-0-system-boolean) | Sets whether a setting should appear in a reduced basic-settings view. |
| [`public static bool IsInternal<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-isinternal-1-electron2d-projectsetting-0) | Gets whether a setting is hidden from ordinary tooling discovery. |
| [`public static void SetAsInternal<T>(ProjectSetting<T> setting, bool internalSetting)`](#m-electron2d-projectsettings-setasinternal-1-electron2d-projectsetting-0-system-boolean) | Sets whether a setting is hidden from ordinary tooling discovery. |
| [`public static bool IsRestartRequired<T>(ProjectSetting<T> setting)`](#m-electron2d-projectsettings-isrestartrequired-1-electron2d-projectsetting-0) | Gets whether changing a setting requires the host application to restart. |
| [`public static void SetRestartIfChanged<T>(ProjectSetting<T> setting, bool restart)`](#m-electron2d-projectsettings-setrestartifchanged-1-electron2d-projectsetting-0-system-boolean) | Sets whether changing a setting requires the host application to restart. |
| [`public static IReadOnlyList<string> GetChangedSettings()`](#m-electron2d-projectsettings-getchangedsettings) | Gets setting names included in the pending or currently delivered change notification. |
| [`public static bool CheckChangedSettingsInGroup(string prefix)`](#m-electron2d-projectsettings-checkchangedsettingsingroup-system-string) | Checks whether any changed setting starts with a category prefix. |
| [`public static void Load()`](#m-electron2d-projectsettings-load) | Loads the project file and then merges the conventional override file when it exists. |
| [`public static void LoadCustom(string path)`](#m-electron2d-projectsettings-loadcustom-system-string) | Merges a custom configuration file into the current settings after full validation. |
| [`public static void Save()`](#m-electron2d-projectsettings-save) | Saves the current document to [`ProjectSettings.ProjectFilePath`](ProjectSettings.md#p-electron2d-projectsettings-projectfilepath) using atomic replacement. |
| [`public static void SaveCustom(string path)`](#m-electron2d-projectsettings-savecustom-system-string) | Saves the current document to an explicit file using atomic replacement. |
| [`public static bool FlushChanges()`](#m-electron2d-projectsettings-flushchanges) | Raises one coalesced [`ProjectSettings.SettingsChanged`](ProjectSettings.md#e-electron2d-projectsettings-settingschanged) notification when changes are pending. |
| [`public static string GlobalizePath(string path)`](#m-electron2d-projectsettings-globalizepath-system-string) | Converts a virtual or ordinary path to an absolute native operating-system path. |
| [`public static string LocalizePath(string path)`](#m-electron2d-projectsettings-localizepath-system-string) | Converts an ordinary path inside a configured root to a virtual project or user path. |
| [`public static string FindProjectRoot(string startPath)`](#m-electron2d-projectsettings-findprojectroot-system-string) | Searches an existing directory and its parents for [`ProjectSettings.ProjectFileName`](ProjectSettings.md#f-electron2d-projectsettings-projectfilename). |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-projectsettings-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override void ValidateDisposal()`](#m-electron2d-projectsettings-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-projectsettings-dispose-system-boolean) | Releases resources owned by a derived class. |

## Events

| Member | Description |
| --- | --- |
| [`public static event Action<ProjectSettingsRegistry> SettingsChanged`](#e-electron2d-projectsettings-settingschanged) | Occurs after one or more settings change and [`ProjectSettings.FlushChanges`](ProjectSettings.md#m-electron2d-projectsettings-flushchanges) is called. |

## Constants

| Member | Description |
| --- | --- |
| [`public const string ProjectFileName = "project.e2d"`](#f-electron2d-projectsettings-projectfilename) | Gets the conventional project settings file name. |
| [`public const string OverrideFileName = "override.cfg"`](#f-electron2d-projectsettings-overridefilename) | Gets the conventional optional runtime override file name. |
| [`public const string ProjectDataDirectoryName = ".electron2d"`](#f-electron2d-projectsettings-projectdatadirectoryname) | Gets the project-local directory name reserved for generated engine data. |

## Constructor Descriptions

## Audio settings

| Full signature | Contract |
| --- | --- |
| `public static ProjectSetting<bool> AudioDriverEnableInput { get; }` | Built-in audio/driver/enable_input, default false. |
| `public static ProjectSetting<float> AudioGeneral2DPanningStrength { get; }` | Built-in audio/general/2d_panning_strength, default 0.5; sampled by new spatial players. |

<a id="audiogeneral2dpanningstrength"></a>
### AudioGeneral2DPanningStrength

`audio/general/2d_panning_strength` is a nonnegative finite float, 0.5 by default. Each new [AudioStreamEmitter](AudioStreamEmitter.md) samples the current effective value during construction; later setting changes do not alter an existing player's panning. Invalid values reject before storage. This typed built-in follows the same project load, feature override and persistence rules as other settings.

<a id="audiodriverenableinput"></a>
### AudioDriverEnableInput

Allows AudioServer or AudioStreamMicrophone to activate the recording device. Set it in ProjectSettings before Start/Play/manual activation; feature overrides resolve through GetWithOverride. False rejects a new request before activation, while existing capture continues until explicit stop, last automatic release or engine closure. Merely querying/preparing input frequency or selecting generator Input mode opens a paused device and does not record. OS permission/device failure is separate from this typed setting. Isolated registries register independent values and built-in metadata; normal persistence/default suppression applies. Native recording acceptance is documented in the [audio component](../components/audio-playback.md#recording-input).

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

<a id="physics2ddefaults"></a>
<a id="p-electron2d-projectsettings-physics2ddefaultgravity"></a>
### `Physics2DDefaultGravity`

Defines `physics/2d/default_gravity`, default 980 scene units/s². Finite signed strengths are accepted; a nonfinite value is rejected before registry mutation.

<a id="p-electron2d-projectsettings-physics2ddefaultgravityvector"></a>
### `Physics2DDefaultGravityVector`

Defines `physics/2d/default_gravity_vector`, default (0, 1). Finite components are required; the vector is not normalized before multiplication by the gravity strength.

<a id="p-electron2d-projectsettings-physics2ddefaultlineardamp"></a>
### `Physics2DDefaultLinearDamp`

Defines `physics/2d/default_linear_damp`, default 0.1/s. Finite signed rates are accepted; the body's damping mode can combine or replace this value.

<a id="p-electron2d-projectsettings-physics2ddefaultangulardamp"></a>
### `Physics2DDefaultAngularDamp`

Defines `physics/2d/default_angular_damp`, default 1/s. Finite signed rates are accepted independently of linear damping. A SceneTree physics world samples active feature overrides for all four keys when its first body or area attaches; later setting changes do not retroactively alter that world. Areas can override the sampled values by field and priority. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks default identities, world sampling, behavior and restoration.

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

Defines `rendering/textures/default_filters/use_nearest_mipmap_filter`, default false. The GPU renderer reads the active feature override on construction: false linearly interpolates mip levels for canvas samplers, true chooses the nearest mip. Changes apply to the next renderer run. Non-mipmap filters remain restricted to level zero. Named material samplers use fixed linear filtering and remain restricted to level zero, so this setting does not change their output.

### AnisotropicFilteringLevel

`public static ProjectSetting<int> AnisotropicFilteringLevel { get; }`

Defines `rendering/textures/default_filters/anisotropic_filtering_level`, default 2. Values 0..4 mean disabled, 2, 4, 8 or 16 samples; invalid writes throw ArgumentOutOfRangeException before mutation. New Viewports read the active feature override at construction. Change Viewport.AnisotropicFilteringLevel to update an existing viewport. Only anisotropic canvas filters use the limit.

<a id="p-electron2d-projectsettings-defaultclearcolor"></a>
### `public static ProjectSetting<Color> DefaultClearColor { get; }`

Defines `rendering/environment/defaults/default_clear_color`, default `(0.3, 0.3, 0.3, 1)`. Every channel must be finite; invalid writes fail before mutation. The renderer reads the active feature override during startup. Use RenderingServer.SetDefaultClearColor to change the active renderer's color; it does not change this stored setting. Normalized framebuffer output clamps channels to `[0, 1]`.

<a id="localetest"></a>
### `public static ProjectSetting<string> LocaleTest { get; }`

Defines `internationalization/locale/test`, default empty. A nonempty supported .NET culture name overrides the managed UI culture at the next Engine startup. Underscore separators are accepted. Unsupported names fail validation before storage; changing this setting does not alter a running lookup culture.

<a id="localefallback"></a>
### `public static ProjectSetting<string> LocaleFallback { get; }`

Defines `internationalization/locale/fallback`, default `en`. Engine startup samples the active feature override for resource-catalog and direct-entry fallback after primary locale lookup. Empty disables fallback. Supported .NET culture names and underscore separators are accepted; unsupported names fail validation. Changing the setting takes effect on the next Engine startup.

<a id="rootnodeautotranslate"></a>
### `public static ProjectSetting<bool> RootNodeAutoTranslate { get; }`

Defines `internationalization/rendering/root_node_auto_translate`, default true. `SceneTree` samples the active value when constructing a root whose `Node.AutoTranslateMode` is still `Inherit`, assigning `Always` or `Disabled`. Existing roots retain their selected mode when the setting changes.

<a id="physicsinterpolation"></a>
### `public static ProjectSetting<bool> PhysicsInterpolation { get; }`

Defines `physics/common/physics_interpolation`, default false. `SceneTree` samples the active feature override at construction. Existing trees retain their flag when this setting changes; set `SceneTree.PhysicsInterpolation` for an immediate change. The setting affects presentation of 2D canvas and camera transforms after fixed physics ticks, leaving logical coordinates current.

<a id="pseudolocalizationenabled"></a>
### `public static ProjectSetting<bool> PseudolocalizationEnabled { get; }`

Defines `internationalization/pseudolocalization/use_pseudolocalization`, default false. An Engine run samples the active override at startup. A later setting change does not toggle the active domain; assign `TranslationServer.PseudolocalizationEnabled` for an immediate change.

<a id="pseudolocalizationreplacewithaccents"></a>
### `public static ProjectSetting<bool> PseudolocalizationReplaceWithAccents { get; }`

Defines `internationalization/pseudolocalization/replace_with_accents`, default true.

<a id="pseudolocalizationdoublevowels"></a>
### `public static ProjectSetting<bool> PseudolocalizationDoubleVowels { get; }`

Defines `internationalization/pseudolocalization/double_vowels`, default false.

<a id="pseudolocalizationfakebidi"></a>
### `public static ProjectSetting<bool> PseudolocalizationFakeBIDI { get; }`

Defines `internationalization/pseudolocalization/fake_bidi`, default false.

<a id="pseudolocalizationoverride"></a>
### `public static ProjectSetting<bool> PseudolocalizationOverride { get; }`

Defines `internationalization/pseudolocalization/override`, default false.

<a id="pseudolocalizationexpansionratio"></a>
### `public static ProjectSetting<float> PseudolocalizationExpansionRatio { get; }`

Defines `internationalization/pseudolocalization/expansion_ratio`, default zero. Values must be finite and non-negative. A negative value fails with `ArgumentOutOfRangeException`; non-finite values fail during JSON serialization before storage.

<a id="pseudolocalizationprefix"></a>
### `public static ProjectSetting<string> PseudolocalizationPrefix { get; }`

Defines `internationalization/pseudolocalization/prefix`, default `[`. Null values are rejected by the typed setting boundary.

<a id="pseudolocalizationsuffix"></a>
### `public static ProjectSetting<string> PseudolocalizationSuffix { get; }`

Defines `internationalization/pseudolocalization/suffix`, default `]`. Null values are rejected by the typed setting boundary.

<a id="pseudolocalizationskipplaceholders"></a>
### `public static ProjectSetting<bool> PseudolocalizationSkipPlaceholders { get; }`

Defines `internationalization/pseudolocalization/skip_placeholders`, default true. These eight transform settings are read on startup or by `TranslationServer.ReloadPseudolocalization`; setting them alone does not change a running domain.

<a id="ignorejoypadonunfocusedapplication"></a>
### `public static ProjectSetting<bool> IgnoreJoypadOnUnfocusedApplication { get; }`

Defines `input_devices/joypads/ignore_joypad_on_unfocused_application`, false by default. `Engine.Run` samples the active project value before opening the SDL controller host. Runtime changes to [Input.IgnoreJoypadOnUnfocusedApplication](Input.md) take effect immediately; changing only this stored setting requires a later run to be sampled.

<a id="inputuifocusnext"></a>
### `public static ProjectSetting<InputActionSettings> InputUIFocusNext { get; }`

Defines `input/ui_focus_next`, default Tab.

<a id="inputuifocusprev"></a>
### `public static ProjectSetting<InputActionSettings> InputUIFocusPrev { get; }`

Defines `input/ui_focus_prev`, default Shift+Tab.

<a id="inputuileft"></a>
### `public static ProjectSetting<InputActionSettings> InputUILeft { get; }`

Defines `input/ui_left`, default left arrow, D-pad left and left-stick X negative.

<a id="inputuiup"></a>
### `public static ProjectSetting<InputActionSettings> InputUIUp { get; }`

Defines `input/ui_up`, default up arrow, D-pad up and left-stick Y negative.

<a id="inputuiright"></a>
### `public static ProjectSetting<InputActionSettings> InputUIRight { get; }`

Defines `input/ui_right`, default right arrow, D-pad right and left-stick X positive.

<a id="inputuidown"></a>
### `public static ProjectSetting<InputActionSettings> InputUIDown { get; }`

Defines `input/ui_down`, default down arrow, D-pad down and left-stick Y positive. Each directional definition has three ordered bindings; the controller bindings match all devices. These definitions are registered in every registry; an active feature override participates in the next explicit map load. Their `InputActionSettings` values are snapshotted through project-setting serialization.

<a id="p-electron2d-projectsettings-projectroot"></a>
### `public static string ProjectRoot { get; }`

Gets the current absolute project resource directory.

**Value:** The directory to which `res://` resolves.

<a id="p-electron2d-projectsettings-userdataroot"></a>
### `public static string UserDataRoot { get; }`

Gets the current absolute user-data directory.

**Value:** The directory to which `user://` resolves. The directory may not exist yet.

<a id="p-electron2d-projectsettings-projectfilepath"></a>
### `public static string ProjectFilePath { get; }`

Gets the absolute path of the current project settings file.

**Value:** [`ProjectSettings.ProjectFileName`](ProjectSettings.md#f-electron2d-projectsettings-projectfilename) inside [`ProjectSettings.ProjectRoot`](ProjectSettings.md#p-electron2d-projectsettings-projectroot).

<a id="p-electron2d-projectsettings-overridefilepath"></a>
### `public static string OverrideFilePath { get; }`

Gets the absolute path of the optional runtime override file.

**Value:** [`ProjectSettings.OverrideFileName`](ProjectSettings.md#f-electron2d-projectsettings-overridefilename) inside [`ProjectSettings.ProjectRoot`](ProjectSettings.md#p-electron2d-projectsettings-projectroot).

<a id="p-electron2d-projectsettings-projectdatapath"></a>
### `public static string ProjectDataPath { get; }`

Gets the absolute path reserved for generated project-local engine data.

**Value:** [`ProjectSettings.ProjectDataDirectoryName`](ProjectSettings.md#f-electron2d-projectsettings-projectdatadirectoryname) inside [`ProjectSettings.ProjectRoot`](ProjectSettings.md#p-electron2d-projectsettings-projectroot).

<a id="p-electron2d-projectsettings-version"></a>
### `public static ulong Version { get; }`

Gets a monotonically increasing in-process registry version.

**Value:** A value starting at one and incremented after registry, value, or active-feature changes.

### RenderingTimeRolloverSeconds

`public static ProjectSetting<double> RenderingTimeRolloverSeconds { get; }`

Defines `rendering/limits/time/time_rollover_secs`, default 3600 seconds. Finite positive values are accepted; zero, negative or nonserializable values fail before committing a setting change. The active feature override is read on each submitted frame, so changes apply to an existing renderer. The render clock adds the scaled scheduled process step and takes its remainder by this limit. It starts at zero for each Engine.Run, freezes when rendering is disabled/hidden, and continues while the tree is paused.

This setting drives [canvas animation intervals](CanvasItem.md#drawanimationslice) and the optional GPU fragment [TIME built-in](../components/shader-materials.md#render-time). The clock is double precision; shader upload converts it to float32, with the usual loss of precision at large values. A TIME-using shader rejects an out-of-range clock before drawing. CanvasTimingTests checks default/validation; CanvasTimingRenderingTests checks the clock on both Linux backends; ShaderTimeRenderingTests checks actual HLSL/GLSL output, live rollover, pause, time scale and cleanup on Wayland/Vulkan.


## Method Descriptions

<a id="m-electron2d-projectsettings-configurepaths-system-string-system-string"></a>
### `public static void ConfigurePaths(string projectRoot, string userDataRoot)`

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
### `public static void Register<T>(ProjectSetting<T> setting)`

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
### `public static void Unregister<T>(ProjectSetting<T> setting)`

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
### `public static bool HasSetting<T>(ProjectSetting<T> setting)`

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
### `public static IReadOnlyList<string> GetSettingNames(bool includeInternal = true)`

Gets registered setting names ordered by explicit order and then by name.

**Parameters**

- `includeInternal`: Whether settings marked as internal are included.

**Returns:** An immutable snapshot of full case-sensitive setting paths.

**Exceptions**

- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-get-1-electron2d-projectsetting-0"></a>
### `public static T Get<T>(ProjectSetting<T> setting)`

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
### `public static T GetWithOverride<T>(ProjectSetting<T> setting)`

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
### `public static T GetWithOverride<T>(ProjectSetting<T> setting, IEnumerable<string> features)`

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
### `public static void Set<T>(ProjectSetting<T> setting, T value)`

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
### `public static void Reset<T>(ProjectSetting<T> setting)`

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
### `public static void SetInitialValue<T>(ProjectSetting<T> setting, T value)`

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
### `public static void SetFeatureOverride<T>(ProjectSetting<T> setting, string feature, T value)`

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
### `public static bool ClearFeatureOverride<T>(ProjectSetting<T> setting, string feature)`

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
### `public static IReadOnlyList<string> GetFeatureOverrides<T>(ProjectSetting<T> setting)`

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
### `public static bool AddCustomFeature(string feature)`

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
### `public static bool RemoveCustomFeature(string feature)`

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
### `public static bool HasFeature(string feature)`

Determines whether a platform, build, architecture, or custom feature is active.

**Parameters**

- `feature`: The feature tag to inspect.

**Returns:** `true` when the normalized feature is active.

**Exceptions**

- `ArgumentNullException`: `feature` is `null`.
- `ArgumentException`: `feature` is invalid.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-getactivefeatures"></a>
### `public static IReadOnlyList<string> GetActiveFeatures()`

Gets all active platform, build, architecture, and custom features.

**Returns:** An immutable ordinally sorted snapshot of normalized feature tags.

**Exceptions**

- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-getorder-1-electron2d-projectsetting-0"></a>
### `public static int GetOrder<T>(ProjectSetting<T> setting)`

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
### `public static void SetOrder<T>(ProjectSetting<T> setting, int order)`

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
### `public static bool IsBasic<T>(ProjectSetting<T> setting)`

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
### `public static void SetAsBasic<T>(ProjectSetting<T> setting, bool basic)`

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
### `public static bool IsInternal<T>(ProjectSetting<T> setting)`

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
### `public static void SetAsInternal<T>(ProjectSetting<T> setting, bool internalSetting)`

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
### `public static bool IsRestartRequired<T>(ProjectSetting<T> setting)`

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
### `public static void SetRestartIfChanged<T>(ProjectSetting<T> setting, bool restart)`

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
### `public static IReadOnlyList<string> GetChangedSettings()`

Gets setting names included in the pending or currently delivered change notification.

**Returns:** An immutable ordinally sorted snapshot, cleared after the notification finishes.

**Exceptions**

- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-checkchangedsettingsingroup-system-string"></a>
### `public static bool CheckChangedSettingsInGroup(string prefix)`

Checks whether any changed setting starts with a category prefix.

**Parameters**

- `prefix`: The case-sensitive prefix to inspect.

**Returns:** `true` when at least one changed setting starts with `prefix`.

**Exceptions**

- `ArgumentNullException`: `prefix` is `null`.
- `ObjectDisposedException`: The registry is disposing or disposed.

<a id="m-electron2d-projectsettings-load"></a>
### `public static void Load()`

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
### `public static void LoadCustom(string path)`

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
### `public static void Save()`

Saves the current document to [`ProjectSettings.ProjectFilePath`](ProjectSettings.md#p-electron2d-projectsettings-projectfilepath) using atomic replacement.

**Exceptions**

- `IO.IOException`: The file cannot be written or replaced.
- `UnauthorizedAccessException`: The caller cannot write the file.
- `IO.DirectoryNotFoundException`: The project directory no longer exists.
- `ObjectDisposedException`: The registry is disposing or disposed.

**Remarks:** A successful save clears internal unsaved-value tracking. Change-notification names remain until
[`ProjectSettings.FlushChanges`](ProjectSettings.md#m-electron2d-projectsettings-flushchanges) completes; a failed save preserves both states.

<a id="m-electron2d-projectsettings-savecustom-system-string"></a>
### `public static void SaveCustom(string path)`

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
### `public static bool FlushChanges()`

Raises one coalesced [`ProjectSettings.SettingsChanged`](ProjectSettings.md#e-electron2d-projectsettings-settingschanged) notification when changes are pending.

**Returns:** `true` when an event invocation was consumed; otherwise `false`.

**Exceptions**

- `ObjectDisposedException`: The registry is disposing or disposed.
- `Exception`: An event handler throws.

**Remarks:** The callback runs without the registry lock, so handlers may read or modify settings.

<a id="m-electron2d-projectsettings-globalizepath-system-string"></a>
### `public static string GlobalizePath(string path)`

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
### `public static string LocalizePath(string path)`

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
### `public static event Action<ProjectSettingsRegistry> SettingsChanged`

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

All public static registry operations preserve internal state under concurrency. Value/metadata transitions and document swaps are linearized by one lock. User validators run synchronously on caller threads and must themselves be deterministic, thread-safe, and free of registry mutations. Serialization, validators, file I/O, snapshots, and path operations may allocate/block and are not frame-hot APIs. Scalar/string setting definitions cache their most recently decoded representation; Engine timing reads therefore remain allocation-free after warm-up. Mutable reference values are never returned from that cache.

Isolated instances dispose their owned document and clear subscribers/state. The process singleton rejects disposal. Disposal during an active load validator is rejected so the transaction cannot be invalidated re-entrantly.

## Verification and known limitations

The executable harness covers malformed definitions/features/paths, exact registration identity, built-ins, mutable snapshot isolation, validators and rollback, initial/revert behavior, metadata/property discovery and post-commit callback failure, override precedence/current/custom features, changed groups/version/no-op writes, event coalescing/re-entry/failure, OS/virtual path round trips/traversal, root discovery, save/load/override/late registration, failed I/O preservation, re-entrant validator rejection, concurrency, reconfiguration, disposal, Engine feature lookup/event flushing, and warmed zero-allocation Engine frames. `LocalizationProjectSettingsTests` checks locale and pseudolocalization defaults, persistence, validation, startup sampling, fallback selection and live transform reload.

`PhysicsInterpolationTests` verifies the typed interpolation setting's key/default, exact registration, isolated project-file round trip and SceneTree startup sampling. Native canvas and camera pixels run through the same setting on Linux dummy compatibility and Wayland compatibility/GPU.

Tests do not prove crash durability on every filesystem, symbolic-link confinement, editor presentation, packed exports, or unavailable domain settings.

<a id="inputuihome"></a><a id="inputuiend"></a>
### `InputUIHome` and `InputUIEnd`

Permanent typed `input/ui_home` and `input/ui_end` definitions use one default binding each, Key.Home and Key.End. They participate in every registry, explicit InputMap loading and typed project-file round trips; live actions remain ordinary rebindable InputMap data. Slider consumes exact matching presses as MinValue/MaxValue requests through Range. Adding these defaults does not claim an implemented text caret or Font renderer.

## Button and tooltip settings

| Definition | Stored key and default |
| --- | --- |
| `public static ProjectSetting<InputActionSettings> InputUIAccept { get; }` | input/ui_accept: Enter, keypad Enter, Space and gamepad A on every device. |
| `public static ProjectSetting<InputActionSettings> InputUIColorPickerDeletePreset { get; }` | input/ui_colorpicker_delete_preset: Delete and gamepad X on every device; focused color swatch removal. |
| `public static ProjectSetting<InputActionSettings> InputUICancel { get; }` | input/ui_cancel: Escape and gamepad B on every device. |
| `public static ProjectSetting<InputActionSettings> InputUISelect { get; }` | input/ui_select: Space and gamepad Y on every device. |
| `public static ProjectSetting<InputActionSettings> InputUIPageUp { get; }` | input/ui_page_up: Page Up. |
| `public static ProjectSetting<InputActionSettings> InputUIPageDown { get; }` | input/ui_page_down: Page Down. |
| `public static ProjectSetting<InputActionSettings> InputUIMenu { get; }` | input/ui_menu: Menu key. |
| `public static ProjectSetting<int> IncrementalSearchMaxIntervalMsec { get; }` | gui/timers/incremental_search_max_interval_msec: 2000 ms, nonnegative; ItemList reads the active override for incremental search. |
| `public static ProjectSetting<double> ButtonShortcutFeedbackHighlightTime { get; }` | gui/timers/button_shortcut_feedback_highlight_time: 0.2 seconds, finite and positive. |
| `public static ProjectSetting<double> TooltipDelaySeconds { get; }` | gui/timers/tooltip_delay_sec: 0.5 seconds, finite and nonnegative. |
| `public static ProjectSetting<Vector2> TooltipPositionOffset { get; }` | display/mouse_cursor/tooltip_position_offset: (10,10), finite. |
| `public static ProjectSetting<int> DefaultScrollDeadzone { get; }` | gui/common/default_scroll_deadzone: 0 logical pixels, signed values retained; sampled by each new ScrollContainer. |
| `public static ProjectSetting<int> DefaultGUIDragThreshold { get; }` | gui/common/drag_threshold: 10 logical pixels, signed values retained; sampled by each new Viewport. |

These definitions are permanently registered alongside other built-in typed settings. InputUIAccept participates in initial InputMap construction and explicit project-action reload. A button samples its feedback duration on first shortcut activation; the tooltip host samples delay when scheduling and offset when placing content, using active feature overrides. Durations are unscaled frame seconds, and offset is in root viewport pixels. Invalid values fail before replacing the stored setting.


<a id="audiobusactivity"></a>
## Audio bus activity settings

| Signature | Key and default |
| --- | --- |
| `public static ProjectSetting<float> AudioBusesChannelDisableThresholdDB { get; }` | audio/buses/channel_disable_threshold_db; -60 dB. |
| `public static ProjectSetting<float> AudioBusesChannelDisableTime { get; }` | audio/buses/channel_disable_time; 2 seconds. |
| `public static ProjectSetting<string> AudioBusesDefaultBusLayout { get; }` | audio/buses/default_bus_layout; res://default_bus_layout.e2dres, applied before startup/autoplay. |

<a id="audiobuseschanneldisablethresholddb"></a>
### AudioBusesChannelDisableThresholdDB

Typed built-in threshold for unused stereo bus activity. Registry validation accepts finite decibels. The actual native output preparation converts it to a linear peak threshold; unused pairs below it stop normal effects after the timeout. Active sources and upstream sends retain usage. Capture and other ProcessSilence hooks continue to execute on inactive buses.

<a id="audiobuseschanneldisabletime"></a>
### AudioBusesChannelDisableTime

Typed built-in nonnegative finite timeout in seconds, read during output preparation and counted using actual mix frames. Zero permits expiry on the next unused silent quantum. Values that cannot produce a representable native frame duration reject at preparation. Edits apply after native output closes/reopens; existing prepared state retains its settings. AudioEffectTests checks defaults, invalid registry values, native silent-source usage, tail expiry and configuration restoration.


## AudioGeneralDefaultPlaybackType

`public static ProjectSetting<AudioDefaultPlaybackType> AudioGeneralDefaultPlaybackType { get; }` registers `audio/general/default_playback_type`, default Stream=0. Sample=1 selects native finite buffers for sample-capable streams and falls back to streaming otherwise. Undefined values reject; .web overrides retain the existing feature-override mechanism/platform gate. [AudioDefaultPlaybackType](AudioDefaultPlaybackType.md) has a distinct value set from the per-request server selector.

## Text editing actions

The `InputUIText*`, `InputUICopy`, `InputUICut`, `InputUIPaste`, `InputUIUndo`, `InputUIRedo` and `InputUISwapInputDirection` permanent typed InputActionSettings entries provide the default [LineEdit](LineEdit.md) keyboard actions. Their stored definitions participate in the existing InputMap transaction and can be remapped by a project. Shift extends caret movement after matching without Shift. LineEditTests exercises submission, movement, deletion and history on the prepared scene GUI path.

## Native stream connection timeouts

`public static ProjectSetting<int> TCPConnectTimeoutSeconds { get; }` maps `network/limits/tcp/connect_timeout_seconds`; `public static ProjectSetting<int> UDSConnectTimeoutSeconds { get; }` maps `network/limits/unix/connect_timeout_seconds`. Both start at 30 seconds, reject negative values and are permanent typed settings sampled on a new ConnectToHost attempt. StreamPeerSocket.Poll enforces the captured deadline; later registry changes do not retime an active attempt. NetworkingTests verifies actual connection/poll behavior. Full I/O blocking is separate from this connection deadline.

## Default audio bus layout

`public static ProjectSetting<string> AudioBusesDefaultBusLayout { get; }` defines audio/buses/default_bus_layout, initially `res://default_bus_layout.e2dres`. The retained registry registers it as a built-in basic setting. Empty or missing paths preserve existing buses; Engine.Start/Run apply a valid typed AudioBusLayout archive before initialization/autoplay, including active feature overrides. Invalid/corrupt files report startup failure. File-owned effects remain retained by the applied configuration. See [saved bus layouts](../components/audio-playback.md#saved-bus-layouts).

`public static ProjectSetting<InputActionSettings> InputUICloseDialog { get; }` permanently defines input/ui_close_dialog independently of input/ui_cancel. Its typed InputMap definition defaults to Escape and adds Command-W on macOS; project reload and feature overrides follow the existing input-action contracts. AcceptDialog uses exact, non-echo activation.

`public static ProjectSetting<System.Int32> SwapCancelOK { get; }` defines gui/common/swap_cancel_ok with validated values Auto (0, default), Cancel First (1) and OK First (2). AddCancelButton samples the effective definition when creating each button; explicit settings override the DisplayServer platform convention.

Six typed InputUIFileDialog definitions supply independently rebindable file browser actions through the existing runtime registry and InputMap builtin loader. See the [file-dialog component](../components/file-dialogs.md) for its exercised flow and limits.

## File-browser input definitions

| Complete declaration | Contract |
| --- | --- |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUIFileDialogDelete { get;  }` | Defines Delete for the file browser's recoverable trash command. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUIFileDialogFind { get;  }` | Defines Command/Control-F for the filename filter. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUIFileDialogFocusPath { get;  }` | Defines Command/Control-L for the current path field. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUIFileDialogRefresh { get;  }` | Defines F5 for file-browser refresh. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUIFileDialogShowHidden { get;  }` | Defines H for toggling hidden files. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUIFileDialogUpOneLevel { get;  }` | Defines Backspace for file-browser parent navigation. |

The [code-authoring slice](../components/code-authoring.md) registers typed InputUITextCompletionQuery (Ctrl+Space), InputUITextCompletionAccept (Tab/Enter/keypad Enter), InputUITextCompletionReplace (Shift variants), InputUITextIndent/InputUITextDedent (Tab/Shift+Tab), InputUITextNewlineBlank (command+Enter) and InputUITextNewlineAbove (command+Shift+Enter). InputUITextNewline additionally accepts Shift+Enter and Shift+keypad Enter. Commands execute through InputMap remapping.

## Code-authoring input definitions

| Complete declaration | Contract |
| --- | --- |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUITextCompletionAccept { get;  }` | Defines the default ui_text_completion_accept keyboard action. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUITextCompletionQuery { get;  }` | Defines the default ui_text_completion_query keyboard action. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUITextCompletionReplace { get;  }` | Defines the default ui_text_completion_replace keyboard action. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUITextDedent { get;  }` | Defines the default ui_text_dedent keyboard action. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUITextIndent { get;  }` | Defines the default ui_text_indent keyboard action. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUITextNewlineAbove { get;  }` | Defines the default ui_text_newline_above keyboard action. |
| `public static Electron2D.ProjectSetting<Electron2D.InputActionSettings> InputUITextNewlineBlank { get;  }` | Defines the default ui_text_newline_blank keyboard action. |

Graph controls consume the permanent `InputUIGraphDuplicate`, `InputUIGraphDelete`, `InputUIGraphFollowLeft` and `InputUIGraphFollowRight` settings through InputMap. The registry is initialized after all typed definitions, including definitions in partial source files. Follow shortcuts use Alt on macOS and CommandOrControl elsewhere; feature overrides retain the existing registry protocol.

| Graph action declaration | Default |
| --- | --- |
| `public static ProjectSetting<InputActionSettings> InputUIGraphDuplicate { get; }` | CommandOrControl+D |
| `public static ProjectSetting<InputActionSettings> InputUIGraphDelete { get; }` | Delete |
| `public static ProjectSetting<InputActionSettings> InputUIGraphFollowLeft { get; }` | Alt+Left on macOS; CommandOrControl+Left elsewhere |
| `public static ProjectSetting<InputActionSettings> InputUIGraphFollowRight { get; }` | Alt+Right on macOS; CommandOrControl+Right elsewhere |


### `Physics2DDefaultConstraintBias`

`public static ProjectSetting<float> Physics2DDefaultConstraintBias { get; }` exposes physics/2d/solver/default_constraint_bias, initial 0.2, accepting only finite [0,1]. Each new physics space samples its current feature override. Existing spaces retain their captured value; PhysicsServer.SpaceSetConstraintDefaultBias changes one live space. Joint.Bias=0 inherits that space value. The registry includes the setting as a built-in key and preserves it through project settings storage.

<a id="physics2dsleepthresholdlinear"></a>
<a id="physics2dsleepthresholdangular"></a>
<a id="physics2dtimebeforesleep"></a>
### Physics2DSleepThresholdLinear, Physics2DSleepThresholdAngular and Physics2DTimeBeforeSleep

Each is a `public static ProjectSetting<float>` get-only key:

| Key | Storage name | Default / units |
| --- | --- | --- |
| `Physics2DSleepThresholdLinear` | `physics/2d/sleep_threshold_linear` | 2 scene units/s |
| `Physics2DSleepThresholdAngular` | `physics/2d/sleep_threshold_angular` | 0.13962634 rad/s |
| `Physics2DTimeBeforeSleep` | `physics/2d/time_before_sleep` | 0.5 s |

Values must be finite and nonnegative; positive linear values must remain nonzero
after backend unit conversion. New worlds sample current feature overrides. Existing
worlds preserve captured values; use the corresponding PhysicsServer.SpaceSetBody...
operations to change them. Zero speed thresholds disable automatic sleep, while
zero time permits it after a positive quiet interval. CPU and independent resident
GPU creation share the same capture helper. PhysicsSleepPolicyTests verifies capture,
new/existing world distinction and live behavior.

## Contact correction defaults

<a id="physics2ddefaultcontactbias"></a>
`public static ProjectSetting<float> Physics2DDefaultContactBias { get; }` defines
`physics/2d/solver/default_contact_bias`, default 0.8, finite in [0,1].

<a id="physics2dcontactmaxallowedpenetration"></a>
`public static ProjectSetting<float> Physics2DContactMaxAllowedPenetration { get; }`
defines `physics/2d/solver/contact_max_allowed_penetration`, default 0.3 scene units.
It accepts finite nonnegative distances within the backend range; nonzero values
must survive conversion. Both are registered built-ins and support feature overrides.
Each world captures current values at construction. Existing worlds use the typed
[PhysicsServer](PhysicsServer.md#world-contact-correction) setters instead. Shape zero
bias inherits the world; slack controls correction only, not contact creation.

<a id="physics2dsolveriterations"></a>
## `Physics2DSolverIterations`

`public static ProjectSetting<int> Physics2DSolverIterations { get; }` is the
registered built-in `physics/2d/solver/solver_iterations`, default sixteen. It accepts
positive integers and supports feature overrides. New CPU and independent GPU worlds
capture it; existing worlds retain their count until their typed world setter is
called. The count controls contact/joint sweeps, not time substeps. See
[PhysicsServer](PhysicsServer.md#solver-iteration-count) for semantics and errors.

## Contact history defaults

<a id="physics2dcontactrecycleradius"></a>
`public static ProjectSetting<float> Physics2DContactRecycleRadius { get; }` defines
`physics/2d/solver/contact_recycle_radius`, default one scene unit.

<a id="physics2dcontactmaxseparation"></a>
`public static ProjectSetting<float> Physics2DContactMaxSeparation { get; }` defines
`physics/2d/solver/contact_max_separation`, default 1.5 scene units.

Both registered built-ins support feature overrides and are sampled by new CPU and
independent GPU worlds. Existing worlds retain their values until changed through
[PhysicsServer](PhysicsServer.md#contact-history-limits). Values must be finite and
nonnegative with representable squared distances; positive backend squares cannot
underflow to zero. Zero radius disables history reuse; zero maximum separation
retains only cached contacts without positive normal gap or tangential drift.


## Root physics picking

`public static ProjectSetting<bool> PhysicsObjectPicking { get; }` represents
`physics/common/enable_object_picking`, default true. SceneTree activation samples
its feature-resolved value for a root Viewport whose picking property has not been
explicitly authored. The supplied root object's explicit value takes precedence.
Already active roots and ordinary child viewports are unaffected by later project
edits. Each registry registers the definition with ordinary typed metadata and
persistence. See [Physics picking](../components/physics-picking.md).

## Physics diagnostic keys

| Key | Typed member | Default and consumer |
| --- | --- | --- |
| `debug/shapes/collision/shape_color` | `ProjectSetting<Color> DebugCollisionShapeColor` | Finite `(0, .6, .7, .42)`, sampled by new CollisionShape nodes and SceneTree instances with feature overrides. |
| `debug/shapes/collision/draw_2d_outlines` | `ProjectSetting<bool> DebugCollisionDrawOutlines` | `true`; filled Shape drawing adds opaque one-pixel outlines. Applies on next recording, requiring redraw for existing commands. |

Both keys use the existing typed persistence/override registry and are not basic
settings. [Physics diagnostics](../components/physics-debug.md) documents their
consumers and native verification. Contact color/limit settings remain unimplemented.
