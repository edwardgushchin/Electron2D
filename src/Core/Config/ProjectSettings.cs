using IOPath = System.IO.Path;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Identifies one strongly typed project-wide setting and its default value.</summary>
/// <typeparam name="T">The non-null value type stored for the setting.</typeparam>
/// <remarks>
/// Reuse one instance for each logical setting. Names are case-sensitive paths such as
/// <c>application/config/name</c>. Values are serialized snapshots, so mutable values returned from
/// <see cref="DefaultValue"/> do not mutate the stored default. A validator can run concurrently on caller threads;
/// it must therefore be deterministic, thread-safe, and free of registry mutations.
/// </remarks>
public sealed class ProjectSetting<T>
    where T : notnull
{
    private readonly string _defaultSerialized;
    private readonly Func<T, bool>? _validator;
    private readonly object _cacheGate = new();
    private readonly bool _canCacheValue = typeof(T).IsEnum || Type.GetTypeCode(typeof(T)) != TypeCode.Object;
    private string? _cachedSerialized;
    private T? _cachedValue;

    /// <summary>Initializes a strongly typed project setting.</summary>
    /// <param name="name">The full case-sensitive category path.</param>
    /// <param name="defaultValue">The non-null value returned while no explicit value is stored.</param>
    /// <param name="validator">An optional predicate that must accept every stored value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="defaultValue"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid category path.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="validator"/> rejects <paramref name="defaultValue"/>.</exception>
    /// <exception cref="NotSupportedException"><typeparamref name="T"/> is not a supported configuration value type.</exception>
    /// <exception cref="System.Text.Json.JsonException"><paramref name="defaultValue"/> cannot be serialized as <typeparamref name="T"/>.</exception>
    public ProjectSetting(string name, T defaultValue, Func<T, bool>? validator = null)
    {
        ValidateName(name);
        ArgumentNullException.ThrowIfNull(defaultValue);
        ConfigFile.ValidateValueType(typeof(T));

        Name = name;
        _validator = validator;
        _defaultSerialized = ConfigFile.SerializeSnapshot(defaultValue);
        ValidateSerialized(_defaultSerialized);
    }

    /// <summary>Gets the full setting path.</summary>
    /// <value>A case-sensitive category path containing at least one slash.</value>
    public string Name { get; }

    /// <summary>Gets the declared value type.</summary>
    /// <value><typeparamref name="T"/>.</value>
    public Type ValueType => typeof(T);

    /// <summary>Gets an independent copy of the default value.</summary>
    /// <value>The value supplied during construction, deserialized as a new snapshot.</value>
    public T DefaultValue => Deserialize(_defaultSerialized);

    internal string DefaultSerialized => _defaultSerialized;

    internal T Deserialize(string serialized)
    {
        if (!_canCacheValue)
            return ConfigFile.DeserializeSnapshot<T>(serialized, Name);

        lock (_cacheGate)
        {
            if (StringComparer.Ordinal.Equals(_cachedSerialized, serialized))
                return _cachedValue!;

            var value = ConfigFile.DeserializeSnapshot<T>(serialized, Name);
            _cachedSerialized = serialized;
            _cachedValue = value;
            return value;
        }
    }

    internal string SerializeAndValidate(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var serialized = ConfigFile.SerializeSnapshot(value);
        ValidateSerialized(serialized);
        return serialized;
    }

    internal void ValidateSerialized(string serialized)
    {
        var value = Deserialize(serialized);
        if (_validator is not null && !_validator(value))
            throw new ArgumentOutOfRangeException(nameof(value), $"Value is invalid for project setting '{Name}'.");
    }

    private static void ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (name.Length == 0 || name[0] == '/' || name[^1] == '/' || !name.Contains('/') ||
            name.Contains("//", StringComparison.Ordinal) || name.Contains('\\') || name.Contains('.') ||
            name.Any(char.IsControl) || name.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException(
                "A project setting name must be a nonempty slash-separated category path without whitespace, backslashes, or periods.",
                nameof(name));
        }
    }
}

/// <summary>Stores globally accessible, strongly typed project settings and resolves project virtual paths.</summary>
/// <remarks>
/// <para>
/// Static operations address the permanent runtime registry. Use <see cref="ProjectSettingsRegistry"/> for isolated
/// tooling, tests, or multiple unopened projects. All public operations are thread-safe; disk and serialization
/// operations are not suitable for real-time callbacks.
/// </para>
/// <para>
/// Settings must be registered before typed access. Unknown values loaded from disk remain preserved and become
/// available when a matching typed setting is later registered. The registry never exposes an untyped value API.
/// </para>
/// </remarks>
public sealed partial class ProjectSettings : ProjectSettingsRegistry
{
    /// <summary>Gets the conventional project settings file name.</summary>
    public const string ProjectFileName = "project.e2d";

    /// <summary>Gets the conventional optional runtime override file name.</summary>
    public const string OverrideFileName = "override.cfg";

    /// <summary>Gets the project-local directory name reserved for generated engine data.</summary>
    public const string ProjectDataDirectoryName = ".electron2d";

    /// <summary>Defines the human-readable application name.</summary>
    public static ProjectSetting<string> ApplicationName { get; } =
        new("application/config/name", string.Empty);

    /// <summary>Defines the application version string.</summary>
    public static ProjectSetting<string> ApplicationVersion { get; } =
        new("application/config/version", string.Empty);

    /// <summary>Allows recording-device activation by AudioServer and microphone playbacks.</summary>
    /// <value>The typed audio/driver/enable_input setting, false initially; checked before each new capture request.</value>
    public static ProjectSetting<bool> AudioDriverEnableInput { get; } = new("audio/driver/enable_input", false);
    /// <summary>Selects streaming or native sampling for scene players using Default playback type.</summary>
    /// <value>audio/general/default_playback_type; Stream (zero) initially, Sample is one.</value>
    public static ProjectSetting<AudioDefaultPlaybackType> AudioGeneralDefaultPlaybackType { get; } = new("audio/general/default_playback_type", AudioDefaultPlaybackType.Stream, value => value is AudioDefaultPlaybackType.Stream or AudioDefaultPlaybackType.Sample);
    /// <summary>Sets the global width of two-dimensional spatial audio panning.</summary>
    /// <value>audio/general/2d_panning_strength; 0.5 initially, read by new spatial players.</value>
    public static ProjectSetting<float> AudioGeneral2DPanningStrength { get; } = new("audio/general/2d_panning_strength", .5f, value => float.IsFinite(value) && value >= 0);
    /// <summary>Gets the threshold below which unused bus stereo pairs become inactive after their timeout.</summary>
    /// <value>audio/buses/channel_disable_threshold_db; minus 60 dB initially. Read at output preparation.</value>
    public static ProjectSetting<float> AudioBusesChannelDisableThresholdDB { get; } = new("audio/buses/channel_disable_threshold_db", -60f, float.IsFinite);
    /// <summary>Gets the silent unused-bus activity timeout in seconds.</summary>
    /// <value>audio/buses/channel_disable_time; two seconds initially. Read at output preparation.</value>
    public static ProjectSetting<float> AudioBusesChannelDisableTime { get; } = new("audio/buses/channel_disable_time", 2f, value => float.IsFinite(value) && value >= 0);

    /// <summary>Defines the fixed-step callback frequency used by <see cref="Engine"/>.</summary>
    public static ProjectSetting<int> PhysicsTicksPerSecond { get; } =
        new("physics/common/physics_ticks_per_second", 60, value => value > 0);

    /// <summary>Defines the two-dimensional world's default gravity strength in scene units per second squared.</summary>
    /// <value>The typed setting with default 980; finite signed values are accepted by the registry.</value>
    public static ProjectSetting<float> Physics2DDefaultGravity { get; } =
        new("physics/2d/default_gravity", 980f, float.IsFinite);

    /// <summary>Defines the two-dimensional world's default gravity direction without implicit normalization.</summary>
    /// <value>The typed setting with default (0, 1); components must be finite.</value>
    public static ProjectSetting<Vector2> Physics2DDefaultGravityVector { get; } =
        new("physics/2d/default_gravity_vector", new Vector2(0, 1), value => value.IsFinite());

    /// <summary>Defines the two-dimensional world's default linear damping per second.</summary>
    /// <value>The typed setting with default 0.1; finite signed values are accepted by the registry.</value>
    public static ProjectSetting<float> Physics2DDefaultLinearDamp { get; } =
        new("physics/2d/default_linear_damp", 0.1f, float.IsFinite);

    /// <summary>Defines the two-dimensional world's default angular damping per second.</summary>
    /// <value>The typed setting with default one; finite signed values are accepted by the registry.</value>
    public static ProjectSetting<float> Physics2DDefaultAngularDamp { get; } =
        new("physics/2d/default_angular_damp", 1f, float.IsFinite);

    /// <summary>Defines the maximum fixed-step callbacks processed during one frame.</summary>
    public static ProjectSetting<int> MaxPhysicsStepsPerFrame { get; } =
        new("physics/common/max_physics_steps_per_frame", 8, value => value > 0);

    /// <summary>Defines the finite non-negative fixed-step boundary tolerance.</summary>
    public static ProjectSetting<double> PhysicsJitterFix { get; } =
        new("physics/common/physics_jitter_fix", 0.5d, value => double.IsFinite(value) && value >= 0d);

    /// <summary>Selects the canvas rendering method sampled when Engine.Run opens its window.</summary>
    public static ProjectSetting<string> RenderingMethod { get; } =
        new("rendering/renderer/rendering_method", "gpu", value => value is "gpu" or "compatibility");

    /// <summary>Allows startup to use the compatibility renderer when GPU initialization fails.</summary>
    /// <remarks>Set false to require the programmable GPU path. Shader materials reject compatibility rendering.</remarks>
    public static ProjectSetting<bool> RenderingFallback { get; } =
        new("rendering/rendering_device/fallback_to_opengl3", true);

    /// <summary>Defines the render-clock wrap period in seconds.</summary>
    /// <remarks>Defaults to 3600. Must be finite and positive. Active feature overrides apply on each submitted frame.
    /// Drives canvas animation intervals and the optional float32 TIME built-in in GPU fragment shaders.
    /// Shader TIME loses precision as values grow; a clock outside finite float32 range fails before drawing that shader.</remarks>
    public static ProjectSetting<double> RenderingTimeRolloverSeconds { get; } =
        new("rendering/limits/time/time_rollover_secs", 3600d, value => double.IsFinite(value) && value > 0);

    /// <summary>Defines the initial canvas transform snapping choice for newly constructed windows.</summary>
    /// <remarks>False by default. Active feature overrides apply; existing windows keep their own property value.</remarks>
    public static ProjectSetting<bool> SnapTransformsToPixel { get; } =
        new("rendering/2d/snap/snap_2d_transforms_to_pixel", false);

    /// <summary>Defines the initial canvas vertex snapping choice for newly constructed windows.</summary>
    /// <remarks>False by default. Active feature overrides apply; existing windows keep their own property value.</remarks>
    public static ProjectSetting<bool> SnapVerticesToPixel { get; } =
        new("rendering/2d/snap/snap_2d_vertices_to_pixel", false);

    /// <summary>Selects nearest rather than linear interpolation between canvas mip levels.</summary>
    /// <remarks>False by default. Sampled when the GPU renderer opens.</remarks>
    public static ProjectSetting<bool> UseNearestMipmapFilter { get; } =
        new("rendering/textures/default_filters/use_nearest_mipmap_filter", false);

    /// <summary>Defines the initial viewport anisotropy limit as a power of two.</summary>
    /// <remarks>Zero disables anisotropy, 1..4 select 2..16 samples; default 2 selects four samples.
    /// Sampled when a viewport is constructed. Existing viewports retain their own setting.</remarks>
    public static ProjectSetting<int> AnisotropicFilteringLevel { get; } =
        new("rendering/textures/default_filters/anisotropic_filtering_level", 2, value => value is >= 0 and <= 4);

    /// <summary>Defines the finite initial root-framebuffer clear color.</summary>
    public static ProjectSetting<Color> DefaultClearColor { get; } =
        new("rendering/environment/defaults/default_clear_color", new Color(0.3f, 0.3f, 0.3f, 1f), value => value.IsFinite());

    /// <summary>Defines the color of path curves and tangent markers when scene path diagnostics are enabled.</summary>
    /// <remarks>Defaults to (0.1, 1, 0.7, 0.4). Channels must be finite. Active feature overrides apply at SceneTree
    /// construction; existing trees retain their color. Diagnostics draw through the ordinary canvas backends.</remarks>
    public static ProjectSetting<Color> DebugPathsColor { get; } =
        new("debug/shapes/paths/geometry_color", new Color(0.1f, 1f, 0.7f, 0.4f), value => value.IsFinite());

    /// <summary>Defines an optional locale used instead of the process UI culture when an application loop starts.</summary>
    /// <value>The permanent typed project setting, empty by default.</value>
    public static ProjectSetting<string> LocaleTest { get; } =
        new("internationalization/locale/test", string.Empty, IsValidProjectLocale);

    /// <summary>Defines the catalog locale consulted after the selected locale has no translation.</summary>
    /// <value>The permanent typed project setting, <c>en</c> by default. Empty disables the fallback.</value>
    public static ProjectSetting<string> LocaleFallback { get; } =
        new("internationalization/locale/fallback", "en", IsValidProjectLocale);

    /// <summary>Determines whether a newly activated scene root automatically translates messages.</summary>
    /// <value>The permanent typed project setting, true by default; sampled at scene-tree construction.</value>
    public static ProjectSetting<bool> RootNodeAutoTranslate { get; } =
        new("internationalization/rendering/root_node_auto_translate", true);

    /// <summary>Determines whether new scene trees interpolate 2D canvas transforms between physics ticks.</summary>
    /// <value>False by default; sampled when a SceneTree is constructed.</value>
    public static ProjectSetting<bool> PhysicsInterpolation { get; } =
        new("physics/common/physics_interpolation", false);

    /// <summary>Enables pseudolocalization when an application loop starts.</summary>
    /// <value>The permanent typed project setting, false by default.</value>
    public static ProjectSetting<bool> PseudolocalizationEnabled { get; } =
        new("internationalization/pseudolocalization/use_pseudolocalization", false);

    /// <summary>Replaces Latin letters with accented variants during pseudolocalization.</summary>
    /// <value>The permanent typed project setting, true by default.</value>
    public static ProjectSetting<bool> PseudolocalizationReplaceWithAccents { get; } =
        new("internationalization/pseudolocalization/replace_with_accents", true);

    /// <summary>Doubles unprotected vowels during pseudolocalization.</summary>
    /// <value>The permanent typed project setting, false by default.</value>
    public static ProjectSetting<bool> PseudolocalizationDoubleVowels { get; } =
        new("internationalization/pseudolocalization/double_vowels", false);

    /// <summary>Adds right-to-left direction controls during pseudolocalization.</summary>
    /// <value>The permanent typed project setting, false by default.</value>
    public static ProjectSetting<bool> PseudolocalizationFakeBIDI { get; } =
        new("internationalization/pseudolocalization/fake_bidi", false);

    /// <summary>Replaces unprotected characters with asterisks during pseudolocalization.</summary>
    /// <value>The permanent typed project setting, false by default.</value>
    public static ProjectSetting<bool> PseudolocalizationOverride { get; } =
        new("internationalization/pseudolocalization/override", false);

    /// <summary>Defines the finite, non-negative text expansion ratio.</summary>
    /// <value>The permanent typed project setting, zero by default.</value>
    public static ProjectSetting<float> PseudolocalizationExpansionRatio { get; } =
        new("internationalization/pseudolocalization/expansion_ratio", 0f, value => float.IsFinite(value) && value >= 0f);

    /// <summary>Defines the prefix of pseudolocalized text.</summary>
    /// <value>The permanent typed project setting, <c>[</c> by default.</value>
    public static ProjectSetting<string> PseudolocalizationPrefix { get; } =
        new("internationalization/pseudolocalization/prefix", "[");

    /// <summary>Defines the suffix of pseudolocalized text.</summary>
    /// <value>The permanent typed project setting, <c>]</c> by default.</value>
    public static ProjectSetting<string> PseudolocalizationSuffix { get; } =
        new("internationalization/pseudolocalization/suffix", "]");

    /// <summary>Preserves formatting placeholders during pseudolocalization.</summary>
    /// <value>The permanent typed project setting, true by default.</value>
    public static ProjectSetting<bool> PseudolocalizationSkipPlaceholders { get; } =
        new("internationalization/pseudolocalization/skip_placeholders", true);

    /// <summary>Defines whether native controller input and effects are suppressed while the application is unfocused.</summary>
    /// <value>The permanent typed project setting, false by default.</value>
    public static ProjectSetting<bool> IgnoreJoypadOnUnfocusedApplication { get; } =
        new("input_devices/joypads/ignore_joypad_on_unfocused_application", false);

    /// <summary>Defines the default Tab binding for moving GUI focus forward.</summary>
    /// <value>The permanent typed <c>input/ui_focus_next</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIFocusNext { get; } =
        CreateDefaultKeyAction("ui_focus_next", Key.Tab);

    /// <summary>Defines the default Shift+Tab binding for moving GUI focus backward.</summary>
    /// <value>The permanent typed <c>input/ui_focus_prev</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIFocusPrev { get; } =
        CreateDefaultKeyAction("ui_focus_prev", Key.Tab, KeyModifierMask.Shift);

    /// <summary>Defines left-arrow, D-pad left, and left-stick-left GUI navigation.</summary>
    /// <value>The permanent typed <c>input/ui_left</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUILeft { get; } =
        CreateDefaultDirectionalAction("ui_left", Key.Left, JoyButton.DpadLeft, JoyAxis.LeftX, -1f);

    /// <summary>Defines up-arrow, D-pad up, and left-stick-up GUI navigation.</summary>
    /// <value>The permanent typed <c>input/ui_up</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIUp { get; } =
        CreateDefaultDirectionalAction("ui_up", Key.Up, JoyButton.DpadUp, JoyAxis.LeftY, -1f);

    /// <summary>Defines right-arrow, D-pad right, and left-stick-right GUI navigation.</summary>
    /// <value>The permanent typed <c>input/ui_right</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIRight { get; } =
        CreateDefaultDirectionalAction("ui_right", Key.Right, JoyButton.DpadRight, JoyAxis.LeftX, 1f);

    /// <summary>Defines down-arrow, D-pad down, and left-stick-down GUI navigation.</summary>
    /// <value>The permanent typed <c>input/ui_down</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIDown { get; } =
        CreateDefaultDirectionalAction("ui_down", Key.Down, JoyButton.DpadDown, JoyAxis.LeftY, 1f);

    /// <summary>Defines the default Home binding for moving a GUI value or caret to its beginning.</summary>
    /// <value>The permanent typed <c>input/ui_home</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIHome { get; } =
        CreateDefaultKeyAction("ui_home", Key.Home);

    /// <summary>Defines the default End binding for moving a GUI value or caret to its end.</summary>
    /// <value>The permanent typed <c>input/ui_end</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIEnd { get; } =
        CreateDefaultKeyAction("ui_end", Key.End);

    /// <summary>Defines Space and any controller's Y button for focused list selection.</summary>
    /// <value>The permanent typed <c>input/ui_select</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUISelect { get; } = new("input/ui_select", new InputActionSettings
    {
        Bindings =
        [
            new InputBindingSettings { Kind = InputBindingKind.JoypadButton, JoyButtonIndex = JoyButton.Y, Device = InputMap.AllDevices },
            new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Space }
        ]
    });

    /// <summary>Defines Page Up for moving a focused list by one visible page.</summary>
    /// <value>The permanent typed <c>input/ui_page_up</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIPageUp { get; } =
        CreateDefaultKeyAction("ui_page_up", Key.PageUp);

    /// <summary>Defines Page Down for moving a focused list by one visible page.</summary>
    /// <value>The permanent typed <c>input/ui_page_down</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIPageDown { get; } =
        CreateDefaultKeyAction("ui_page_down", Key.PageDown);

    /// <summary>Defines the keyboard Menu key for focused list context input.</summary>
    /// <value>The permanent typed <c>input/ui_menu</c> setting.</value>
    public static ProjectSetting<InputActionSettings> InputUIMenu { get; } =
        CreateDefaultKeyAction("ui_menu", Key.Menu);

    /// <summary>Defines Enter, keypad Enter, Space and gamepad A activation of a focused GUI control.</summary>
    /// <value>The permanent typed input/ui_accept setting with the standard action deadzone.</value>
    public static ProjectSetting<InputActionSettings> InputUIAccept { get; } = new("input/ui_accept", new InputActionSettings
    {
        Bindings =
        [
            new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter },
            new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.KeypadEnter },
            new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Space },
            new InputBindingSettings { Kind = InputBindingKind.JoypadButton, JoyButtonIndex = JoyButton.A, Device = InputMap.AllDevices }
        ]
    });

    /// <summary>Defines Escape and gamepad B cancellation of transient GUI interactions.</summary>
    /// <value>The permanent typed input/ui_cancel setting with the standard action deadzone.</value>
    public static ProjectSetting<InputActionSettings> InputUICancel { get; } = new("input/ui_cancel", new InputActionSettings
    {
        Bindings =
        [
            new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Escape },
            new InputBindingSettings { Kind = InputBindingKind.JoypadButton, JoyButtonIndex = JoyButton.B, Device = InputMap.AllDevices }
        ]
    });

    /// <summary>Defines the shortcut feedback highlight duration sampled when a button first activates a shortcut.</summary>
    /// <value>A finite positive duration in seconds; 0.2 initially.</value>
    public static ProjectSetting<double> ButtonShortcutFeedbackHighlightTime { get; } =
        new("gui/timers/button_shortcut_feedback_highlight_time", .2, value => double.IsFinite(value) && value > 0);

    /// <summary>Defines the pointer tooltip delay in unscaled seconds.</summary>
    /// <value>A finite nonnegative duration, 0.5 initially, sampled when a tooltip is scheduled.</value>
    public static ProjectSetting<double> TooltipDelaySeconds { get; } =
        new("gui/timers/tooltip_delay_sec", .5, value => double.IsFinite(value) && value >= 0);

    /// <summary>Defines the maximum pause between ItemList incremental-search characters.</summary>
    /// <value>Two thousand milliseconds initially; nonnegative values are accepted.</value>
    public static ProjectSetting<int> IncrementalSearchMaxIntervalMsec { get; } =
        new("gui/timers/incremental_search_max_interval_msec", 2000, value => value >= 0);

    /// <summary>Defines the tooltip offset from the pointer in viewport pixels.</summary>
    /// <value>A finite vector, (10,10) initially. Presentation flips it near viewport edges.</value>
    public static ProjectSetting<Vector2> TooltipPositionOffset { get; } =
        new("display/mouse_cursor/tooltip_position_offset", new(10, 10), value => value.IsFinite());

    /// <summary>Defines the initial touch-scroll deadzone in logical pixels for new scroll containers.</summary>
    /// <value>Zero initially; signed values retain the source setting's permissive contract.</value>
    public static ProjectSetting<int> DefaultScrollDeadzone { get; } =
        new("gui/common/default_scroll_deadzone", 0);

    /// <summary>Defines the initial automatic GUI drag distance for new viewports.</summary>
    /// <value>Ten logical pixels initially; signed values retain the source setting's policy.</value>
    public static ProjectSetting<int> DefaultGUIDragThreshold { get; } =
        new("gui/common/drag_threshold", 10);

    /// <summary>Defines the default keyboard binding for ui_text_submit.</summary>
    /// <value>The permanent typed input/ui_text_submit setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextSubmit { get; } = new("input/ui_text_submit", new InputActionSettings
    {
        Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.KeypadEnter }]
    });

    /// <summary>Defines the default keyboard binding for ui_text_select_all.</summary>
    /// <value>The permanent typed input/ui_text_select_all setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextSelectAll { get; } = CreateDefaultKeyAction("ui_text_select_all", Key.A, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the default keyboard binding for ui_copy.</summary>
    /// <value>The permanent typed input/ui_copy setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUICopy { get; } = CreateDefaultKeyAction("ui_copy", Key.C, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the default keyboard binding for ui_cut.</summary>
    /// <value>The permanent typed input/ui_cut setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUICut { get; } = CreateDefaultKeyAction("ui_cut", Key.X, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the default keyboard binding for ui_paste.</summary>
    /// <value>The permanent typed input/ui_paste setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUIPaste { get; } = CreateDefaultKeyAction("ui_paste", Key.V, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the default keyboard binding for ui_undo.</summary>
    /// <value>The permanent typed input/ui_undo setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUIUndo { get; } = CreateDefaultKeyAction("ui_undo", Key.Z, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the default keyboard binding for ui_redo.</summary>
    /// <value>The permanent typed input/ui_redo setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUIRedo { get; } = CreateDefaultKeyAction("ui_redo", Key.Z, KeyModifierMask.CommandOrControl | KeyModifierMask.Shift);

    /// <summary>Defines the default keyboard binding for ui_text_backspace.</summary>
    /// <value>The permanent typed input/ui_text_backspace setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextBackspace { get; } = CreateDefaultKeyAction("ui_text_backspace", Key.Backspace, 0);

    /// <summary>Defines the default keyboard binding for ui_text_backspace_word.</summary>
    /// <value>The permanent typed input/ui_text_backspace_word setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextBackspaceWord { get; } = CreateDefaultKeyAction("ui_text_backspace_word", Key.Backspace, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the default keyboard binding for ui_text_backspace_all_to_left.</summary>
    /// <value>The permanent typed input/ui_text_backspace_all_to_left setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextBackspaceAllToLeft { get; } = CreateDefaultKeyAction("ui_text_backspace_all_to_left", Key.Backspace, KeyModifierMask.Alt);

    /// <summary>Defines the default keyboard binding for ui_text_delete.</summary>
    /// <value>The permanent typed input/ui_text_delete setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextDelete { get; } = CreateDefaultKeyAction("ui_text_delete", Key.Delete, 0);

    /// <summary>Defines the default keyboard binding for ui_text_delete_word.</summary>
    /// <value>The permanent typed input/ui_text_delete_word setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextDeleteWord { get; } = CreateDefaultKeyAction("ui_text_delete_word", Key.Delete, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the default keyboard binding for ui_text_delete_all_to_right.</summary>
    /// <value>The permanent typed input/ui_text_delete_all_to_right setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextDeleteAllToRight { get; } = CreateDefaultKeyAction("ui_text_delete_all_to_right", Key.Delete, KeyModifierMask.Alt);

    /// <summary>Defines the default keyboard binding for ui_text_caret_left.</summary>
    /// <value>The permanent typed input/ui_text_caret_left setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretLeft { get; } = CreateDefaultKeyAction("ui_text_caret_left", Key.Left, 0);

    /// <summary>Defines the default keyboard binding for ui_text_caret_right.</summary>
    /// <value>The permanent typed input/ui_text_caret_right setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretRight { get; } = CreateDefaultKeyAction("ui_text_caret_right", Key.Right, 0);

    /// <summary>Defines the default keyboard binding for ui_text_caret_word_left.</summary>
    /// <value>The permanent typed input/ui_text_caret_word_left setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretWordLeft { get; } = CreateDefaultKeyAction("ui_text_caret_word_left", Key.Left, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the default keyboard binding for ui_text_caret_word_right.</summary>
    /// <value>The permanent typed input/ui_text_caret_word_right setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretWordRight { get; } = CreateDefaultKeyAction("ui_text_caret_word_right", Key.Right, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the default keyboard binding for ui_text_caret_line_start.</summary>
    /// <value>The permanent typed input/ui_text_caret_line_start setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretLineStart { get; } = CreateDefaultKeyAction("ui_text_caret_line_start", Key.Home, 0);

    /// <summary>Defines the default keyboard binding for ui_text_caret_line_end.</summary>
    /// <value>The permanent typed input/ui_text_caret_line_end setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretLineEnd { get; } = CreateDefaultKeyAction("ui_text_caret_line_end", Key.End, 0);

    /// <summary>Defines the default keyboard binding for ui_text_caret_up.</summary>
    /// <value>The permanent typed input/ui_text_caret_up setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretUp { get; } = CreateDefaultKeyAction("ui_text_caret_up", Key.Up, 0);

    /// <summary>Defines the default keyboard binding for ui_text_caret_down.</summary>
    /// <value>The permanent typed input/ui_text_caret_down setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretDown { get; } = CreateDefaultKeyAction("ui_text_caret_down", Key.Down, 0);

    /// <summary>Defines the default keyboard binding for ui_text_caret_page_up.</summary>
    /// <value>The permanent typed input/ui_text_caret_page_up setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretPageUp { get; } = CreateDefaultKeyAction("ui_text_caret_page_up", Key.PageUp, 0);

    /// <summary>Defines the default keyboard binding for ui_text_caret_page_down.</summary>
    /// <value>The permanent typed input/ui_text_caret_page_down setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretPageDown { get; } = CreateDefaultKeyAction("ui_text_caret_page_down", Key.PageDown, 0);

    /// <summary>Defines the default keyboard binding for ui_swap_input_direction.</summary>
    /// <value>The permanent typed input/ui_swap_input_direction setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUISwapInputDirection { get; } = CreateDefaultKeyAction("ui_swap_input_direction", Key.QuoteLeft, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the connection timeout for new TCP stream attempts.</summary>
    /// <value>Thirty nonnegative seconds initially; sampled on ConnectToHost.</value>
    public static ProjectSetting<int> TCPConnectTimeoutSeconds { get; } = new("network/limits/tcp/connect_timeout_seconds", 30, value => value >= 0);

    /// <summary>Defines the connection timeout for new UDS stream attempts.</summary>
    /// <value>Thirty nonnegative seconds initially; sampled on ConnectToHost.</value>
    public static ProjectSetting<int> UDSConnectTimeoutSeconds { get; } = new("network/limits/unix/connect_timeout_seconds", 30, value => value >= 0);

    private static readonly ProjectSettings SharedInstance = CreateSharedInstance();

    internal static ProjectSettings Service => SharedInstance;

    /// <summary>Searches an existing directory and its parents for <see cref="ProjectFileName"/>.</summary>
    /// <param name="startPath">An existing directory or file-system entry whose containing directory starts the search.</param>
    /// <returns>The absolute directory containing the nearest project file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="startPath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="startPath"/> is empty or invalid.</exception>
    /// <exception cref="DirectoryNotFoundException">The starting location does not exist.</exception>
    /// <exception cref="FileNotFoundException">No project file exists at or above the starting location.</exception>
    public static string FindProjectRoot(string startPath)
    {
        ArgumentNullException.ThrowIfNull(startPath);
        if (startPath.Length == 0)
            throw new ArgumentException("A project search path cannot be empty.", nameof(startPath));

        var fullPath = IOPath.GetFullPath(startPath);
        var directoryPath = File.Exists(fullPath) ? IOPath.GetDirectoryName(fullPath)! : fullPath;
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Project search directory '{directoryPath}' does not exist.");

        for (var current = new DirectoryInfo(directoryPath); current is not null; current = current.Parent)
        {
            if (File.Exists(IOPath.Combine(current.FullName, ProjectFileName)))
                return current.FullName;
        }

        throw new FileNotFoundException($"No '{ProjectFileName}' file exists at or above '{directoryPath}'.");
    }

    private static ProjectSettings CreateSharedInstance()
    {
        var projectRoot = Directory.GetCurrentDirectory();
        return new ProjectSettings(projectRoot, GetDefaultUserDataRoot(projectRoot), processWide: true);
    }

    private ProjectSettings(string projectRoot, string userDataRoot, bool processWide) : base(projectRoot, userDataRoot, processWide) { }
}
