using IOPath = System.IO.Path;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization.Metadata;

namespace Electron2D;

/// <summary>Identifies one strongly typed project-wide setting and its default value.</summary>
/// <typeparam name="T">The non-null value type stored for the setting.</typeparam>
/// <remarks>
/// Reuse one instance for each logical setting. Names are case-sensitive paths such as
/// <c>application/config/name</c>. Values are serialized snapshots, so mutable values returned from
/// <see cref="DefaultValue"/> do not mutate the stored default. A validator can run concurrently on caller threads;
/// it must therefore be deterministic, thread-safe, and free of registry mutations.
/// Custom models in trimmed or ahead-of-time compiled hosts require explicit JSON metadata at construction.
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
        : this(name, defaultValue, validator, null) { }

    /// <summary>Initializes a setting with optional compiled JSON metadata.</summary>
    /// <param name="name">The full case-sensitive category path.</param>
    /// <param name="defaultValue">The non-null initial value.</param>
    /// <param name="validator">An optional pure validation predicate.</param>
    /// <param name="typeInfo">Compiled metadata for a custom model, or null for the built-in schema or reflection-enabled host.</param>
    /// <remarks>Pass source-generated metadata for custom models in trimmed or ahead-of-time compiled hosts.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="defaultValue"/> is null.</exception>
    /// <exception cref="ArgumentException">The name is invalid or metadata requests indented tokens.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The validator rejects the initial value.</exception>
    /// <exception cref="NotSupportedException">The value or metadata schema is unsupported.</exception>
    /// <exception cref="System.Text.Json.JsonException">The default cannot be serialized.</exception>
    public ProjectSetting(string name, T defaultValue, Func<T, bool>? validator, JsonTypeInfo<T>? typeInfo)
    {
        ValidateName(name);
        ArgumentNullException.ThrowIfNull(defaultValue);
        ConfigFile.ValidateValueType(typeof(T));
        if (typeInfo is not null)
        {
            if (typeInfo.Options.WriteIndented) throw new ArgumentException("Setting metadata must write compact JSON tokens.", nameof(typeInfo));
            JSONTypeInfo = ConfigFile.CheckJSONTypeInfo(typeInfo);
        }

        Name = name;
        _validator = validator;
        _defaultSerialized = ConfigFile.SerializeSnapshot(defaultValue, JSONTypeInfo);
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
    internal JsonTypeInfo<T>? JSONTypeInfo { get; }

    internal T Deserialize(string serialized)
    {
        if (!_canCacheValue)
            return ConfigFile.DeserializeSnapshot(serialized, Name, JSONTypeInfo);

        lock (_cacheGate)
        {
            if (StringComparer.Ordinal.Equals(_cachedSerialized, serialized))
                return _cachedValue!;

            var value = ConfigFile.DeserializeSnapshot(serialized, Name, JSONTypeInfo);
            _cachedSerialized = serialized;
            _cachedValue = value;
            return value;
        }
    }

    internal string SerializeAndValidate(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var serialized = ConfigFile.SerializeSnapshot(value, JSONTypeInfo);
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

    /// <summary>Defines the initial SceneTree policy for root window close requests.</summary>
    /// <value>application/config/auto_accept_quit; true initially. Sampled before scene activation.</value>
    public static ProjectSetting<bool> AutoAcceptQuit { get; } = new("application/config/auto_accept_quit", true);

    /// <summary>Defines the initial SceneTree policy for Android system back requests.</summary>
    /// <value>application/config/quit_on_go_back; true initially, independently of AutoAcceptQuit.</value>
    /// <remarks>Feature overrides apply at tree construction; later setting edits affect subsequent trees.</remarks>
    public static ProjectSetting<bool> QuitOnGoBack { get; } = new("application/config/quit_on_go_back", true);

    /// <summary>Defines whether each owned window session starts with measured frame-delta smoothing enabled.</summary>
    /// <value>application/run/delta_smoothing; true initially.</value>
    /// <remarks>Active feature overrides initialize OS.DeltaSmoothing before readiness. Later project edits apply to the next owned run.</remarks>
    public static ProjectSetting<bool> DeltaSmoothing { get; } = new("application/run/delta_smoothing", true);

    /// <summary>Defines whether a window session starts with rendering-change-driven drawing.</summary>
    /// <value>application/run/low_processor_mode; false initially.</value>
    public static ProjectSetting<bool> LowProcessorMode { get; } = new("application/run/low_processor_mode", false);

    /// <summary>Defines the initial low-processor wait between owned frames, in microseconds.</summary>
    /// <value>application/run/low_processor_mode_sleep_usec; 6900 initially. Negative values are rejected.</value>
    public static ProjectSetting<int> LowProcessorModeSleepUsec { get; } = new("application/run/low_processor_mode_sleep_usec", 6900, validator: value => value >= 0);

    /// <summary>Defines the application version string.</summary>
    public static ProjectSetting<string> ApplicationVersion { get; } =
        new("application/config/version", string.Empty);

    /// <summary>Selects the optional project Theme archive loaded before engine activation.</summary>
    /// <value>gui/theme/custom; empty initially. Resource and operating-system paths are accepted.</value>
    public static ProjectSetting<string> GUIThemeCustom { get; } = new("gui/theme/custom", "");
    /// <summary>Selects an optional project font used by the built-in theme and universal font fallback.</summary>
    /// <value>gui/theme/custom_font; empty initially. Loading occurs at engine startup.</value>
    public static ProjectSetting<string> GUIThemeCustomFont { get; } = new("gui/theme/custom_font", "");

    /// <summary>Allows recording-device activation by AudioServer and microphone playbacks.</summary>
    /// <value>The typed audio/driver/enable_input setting, false initially; checked before each new capture request.</value>
    public static ProjectSetting<bool> AudioDriverEnableInput { get; } = new("audio/driver/enable_input", false);
    /// <summary>Selects streaming or native sampling for scene players using Default playback type.</summary>
    /// <value>audio/general/default_playback_type; Stream (zero) initially, Sample is one.</value>
    public static ProjectSetting<AudioDefaultPlaybackType> AudioGeneralDefaultPlaybackType { get; } = new("audio/general/default_playback_type", AudioDefaultPlaybackType.Stream, value => value is AudioDefaultPlaybackType.Stream or AudioDefaultPlaybackType.Sample);
    /// <summary>Sets the global width of two-dimensional spatial audio panning.</summary>
    /// <value>audio/general/2d_panning_strength; 0.5 initially, read by new spatial players.</value>
    public static ProjectSetting<float> AudioGeneral2DPanningStrength { get; } = new("audio/general/2d_panning_strength", .5f, value => float.IsFinite(value) && value >= 0);
    /// <summary>Defines the optional audio bus archive applied before the main loop initializes.</summary>
    /// <value>audio/buses/default_bus_layout; res://default_bus_layout.e2dres initially. Empty or absent files preserve current configuration.</value>
    public static ProjectSetting<string> AudioBusesDefaultBusLayout { get; } = new("audio/buses/default_bus_layout", "res://default_bus_layout.e2dres");
    /// <summary>Gets the threshold below which unused bus stereo pairs become inactive after their timeout.</summary>
    /// <value>audio/buses/channel_disable_threshold_db; minus 60 dB initially. Read at output preparation.</value>
    public static ProjectSetting<float> AudioBusesChannelDisableThresholdDB { get; } = new("audio/buses/channel_disable_threshold_db", -60f, float.IsFinite);
    /// <summary>Gets the silent unused-bus activity timeout in seconds.</summary>
    /// <value>audio/buses/channel_disable_time; two seconds initially. Read at output preparation.</value>
    public static ProjectSetting<float> AudioBusesChannelDisableTime { get; } = new("audio/buses/channel_disable_time", 2f, value => float.IsFinite(value) && value >= 0);

    /// <summary>Defines the separator used when node names need a readable numeric suffix.</summary>
    /// <value>editor/naming/node_name_num_separator; 0 (none), 1 (space), 2 (underscore), or 3 (dash). Zero initially.</value>
    /// <remarks>Feature overrides are sampled when a collision is resolved; existing names are unchanged.</remarks>
    public static ProjectSetting<int> NodeNameNumberSeparator { get; } = new("editor/naming/node_name_num_separator", 0, value => value is >= 0 and <= 3);

    /// <summary>Defines the fixed video presentation delay sampled when built-in playback starts.</summary>
    /// <value>audio/video/video_delay_compensation_ms; zero milliseconds initially. Positive values delay video relative to queued audio.</value>
    /// <remarks>Feature overrides apply. The compensated playback timestamp may be negative before the delay elapses;
    /// explicit seeks still address the uncompensated source timeline.</remarks>
    public static ProjectSetting<int> AudioVideoDelayCompensationMSEC { get; } = new("audio/video/video_delay_compensation_ms", 0);

    /// <summary>Recording cadence used by Engine.RunMovie and Engine.RunMovieAsync.</summary>
    /// <value>editor/movie_writer/fps; 60 initially, accepts positive frames per second.</value>
    public static ProjectSetting<int> MovieWriterFPS { get; } = new("editor/movie_writer/fps", 60, value => value > 0);
    /// <summary>Output path used by Engine.RunMovie and Engine.RunMovieAsync.</summary>
    /// <value>editor/movie_writer/movie_file; empty initially. A profile run requires a nonblank path.</value>
    public static ProjectSetting<string> MovieWriterFile { get; } = new("editor/movie_writer/movie_file", "");
    /// <summary>Requests immediate presentation when a movie window is prepared.</summary>
    /// <value>editor/movie_writer/disable_vsync; false initially.</value>
    /// <remarks>Applies to configured and explicit movie runs, before renderer creation. Unsupported native policies follow DisplayServer's fallback.</remarks>
    public static ProjectSetting<bool> MovieWriterDisableVSync { get; } = new("editor/movie_writer/disable_vsync", false);

    /// <summary>Defines the built-in movie writer's synchronous audio rate.</summary>
    /// <value>editor/movie_writer/mix_rate; 48000 Hz initially, bounded to 1000 through 200000.</value>
    public static ProjectSetting<int> MovieWriterMixRate { get; } = new("editor/movie_writer/mix_rate", 48000, value => value is >= 1000 and <= 200000);
    /// <summary>Defines the built-in movie writer's channel arrangement.</summary>
    /// <value>editor/movie_writer/speaker_mode; Stereo initially.</value>
    public static ProjectSetting<AudioServer.SpeakerMode> MovieWriterSpeakerMode { get; } = new("editor/movie_writer/speaker_mode", AudioServer.SpeakerMode.Stereo, Enum.IsDefined);

    /// <summary>Lossy video quality sampled when a built-in AVI or OGV recording begins.</summary>
    /// <value>editor/movie_writer/video_quality; 0.75 initially, finite range 0 through 1.</value>
    /// <remarks>AVI uses the JPEG encoder's minimum one-percent quality for values below 0.01. OGV maps the complete range to Theora quality. PNG is lossless.</remarks>
    public static ProjectSetting<float> MovieWriterVideoQuality { get; } = new("editor/movie_writer/video_quality", .75f, value => float.IsFinite(value) && value is >= 0 and <= 1);
    /// <summary>PCM sample depth sampled when a built-in AVI recording begins.</summary>
    /// <value>editor/movie_writer/audio_bit_depth; 16 initially, accepts 16 or 32. PNG/WAV remains 32-bit.</value>
    public static ProjectSetting<int> MovieWriterAudioBitDepth { get; } = new("editor/movie_writer/audio_bit_depth", 16, value => value is 16 or 32);

    /// <summary>Vorbis quality sampled when a built-in OGV recording begins.</summary>
    /// <value>editor/movie_writer/ogv/audio_quality; 0.5 initially, finite range -0.1 through 1.</value>
    public static ProjectSetting<float> MovieWriterOGVAudioQuality { get; } = new("editor/movie_writer/ogv/audio_quality", .5f, value => float.IsFinite(value) && value is >= -.1f and <= 1);
    /// <summary>Theora speed level sampled when a built-in OGV recording begins.</summary>
    /// <value>editor/movie_writer/ogv/encoding_speed; 4 initially, accepts 1 through 4. Higher levels encode faster.</value>
    public static ProjectSetting<int> MovieWriterOGVEncodingSpeed { get; } = new("editor/movie_writer/ogv/encoding_speed", 4, value => value is >= 1 and <= 4);
    /// <summary>Maximum spacing of OGV video keyframes, in recorded frames.</summary>
    /// <value>editor/movie_writer/ogv/keyframe_interval; 64 initially, accepts 1 through 1024.</value>
    public static ProjectSetting<int> MovieWriterOGVKeyframeInterval { get; } = new("editor/movie_writer/ogv/keyframe_interval", 64, value => value is >= 1 and <= 1024);

    /// <summary>Defines the fixed-step callback frequency used by <see cref="Engine"/>.</summary>
    public static ProjectSetting<int> PhysicsTicksPerSecond { get; } =
        new("physics/common/physics_ticks_per_second", 60, value => value > 0);

    /// <summary>Selects the named physics implementation captured by newly created implicit viewport and scene worlds.</summary>
    /// <value>physics/2d/physics_engine; DEFAULT selects the manager's current default. CPU, GPU or registered names are accepted.</value>
    /// <remarks>Names resolve when the implicit world is created, allowing registration after project loading.
    /// Existing worlds and explicit or parameterless caller-created worlds/spaces retain their selection.</remarks>
    public static ProjectSetting<string> Physics2DEngine { get; } =
        new("physics/2d/physics_engine", "DEFAULT", value => !string.IsNullOrWhiteSpace(value));

    /// <summary>Allows newly created implicit worlds to use CPU after unavailable built-in GPU startup.</summary>
    /// <value>physics/2d/allow_cpu_fallback; false by default. Sampled with the project engine selector.</value>
    /// <remarks>Preserves requested/actual identity and the failure reason. Provider errors and started steps never replay on CPU.</remarks>
    public static ProjectSetting<bool> Physics2DAllowCPUFallback { get; } =
        new("physics/2d/allow_cpu_fallback", false);

    /// <summary>Selects a retained separate thread for each new world's numerical physics intervals.</summary>
    /// <value>physics/2d/run_on_separate_thread; false by default. Worlds capture feature overrides before lazy startup.</value>
    /// <remarks>Preparation, queries, publication and game callbacks retain their owner thread. Each solve joins before publication. Hosts without threads reject this mode.</remarks>
    public static ProjectSetting<bool> Physics2DRunOnSeparateThread { get; } =
        new("physics/2d/run_on_separate_thread", false);

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

    /// <summary>Defines the world's linear quiet threshold in scene units per second.</summary>
    /// <value>physics/2d/sleep_threshold_linear; defaults to 2. New worlds sample feature overrides; existing worlds retain their settings.</value>
    public static ProjectSetting<float> Physics2DSleepThresholdLinear { get; } =
        new("physics/2d/sleep_threshold_linear", 2f, PhysicsSleepSettings.ValidLinear);

    /// <summary>Defines the world's angular quiet threshold in radians per second.</summary>
    /// <value>physics/2d/sleep_threshold_angular; defaults to 0.13962634. New worlds sample feature overrides; existing worlds retain their settings.</value>
    public static ProjectSetting<float> Physics2DSleepThresholdAngular { get; } =
        new("physics/2d/sleep_threshold_angular", 0.13962634f, PhysicsSleepSettings.ValidNonnegative);

    /// <summary>Defines the world's quiet duration before automatic sleep in seconds.</summary>
    /// <value>physics/2d/time_before_sleep; defaults to 0.5. New worlds sample feature overrides; existing worlds retain their settings.</value>
    public static ProjectSetting<float> Physics2DTimeBeforeSleep { get; } =
        new("physics/2d/time_before_sleep", 0.5f, PhysicsSleepSettings.ValidNonnegative);

    /// <summary>Defines the world's default outer-tick contact correction fraction.</summary>
    /// <value>physics/2d/solver/default_contact_bias; defaults to .8. New worlds sample feature overrides.</value>
    public static ProjectSetting<float> Physics2DDefaultContactBias { get; } =
        new("physics/2d/solver/default_contact_bias", .8f, PhysicsContactSettings.ValidBias);

    /// <summary>Defines the world's allowed contact penetration before positional correction, in scene units.</summary>
    /// <value>physics/2d/solver/contact_max_allowed_penetration; defaults to .3. New worlds sample feature overrides.</value>
    public static ProjectSetting<float> Physics2DContactMaxAllowedPenetration { get; } =
        new("physics/2d/solver/contact_max_allowed_penetration", .3f, PhysicsContactSettings.ValidPenetration);

    /// <summary>Defines the body-local distance within which a cached contact may reuse its impulse history.</summary>
    /// <value>physics/2d/solver/contact_recycle_radius; defaults to one scene unit. Zero disables reuse. New worlds capture feature overrides.</value>
    public static ProjectSetting<float> Physics2DContactRecycleRadius { get; } =
        new("physics/2d/solver/contact_recycle_radius", 1f, PhysicsContactSettings.ValidPersistenceDistance);

    /// <summary>Defines the maximum normal separation or tangential drift of a cached contact.</summary>
    /// <value>physics/2d/solver/contact_max_separation; defaults to 1.5 scene units. New worlds capture feature overrides.</value>
    public static ProjectSetting<float> Physics2DContactMaxSeparation { get; } =
        new("physics/2d/solver/contact_max_separation", 1.5f, PhysicsContactSettings.ValidPersistenceDistance);

    /// <summary>Defines the number of contact and joint solver sweeps per simulation substep.</summary>
    /// <value>physics/2d/solver/solver_iterations; defaults to sixteen and accepts positive integers. Each new world captures feature overrides.</value>
    public static ProjectSetting<int> Physics2DSolverIterations { get; } =
        new("physics/2d/solver/solver_iterations", 16, value => value > 0);

    /// <summary>Defines the positional correction fraction inherited by joints whose bias is zero.</summary>
    /// <value>physics/2d/solver/default_constraint_bias; defaults to 0.2 and accepts finite values from zero to one. Sampled when a physics space is created.</value>
    public static ProjectSetting<float> Physics2DDefaultConstraintBias { get; } =
        new("physics/2d/solver/default_constraint_bias", 0.2f, value => float.IsFinite(value) && value >= 0 && value <= 1);

    /// <summary>Defines the maximum fixed-step callbacks processed during one frame.</summary>
    public static ProjectSetting<int> MaxPhysicsStepsPerFrame { get; } =
        new("physics/common/max_physics_steps_per_frame", 8, value => value > 0);

    /// <summary>Defines the finite non-negative fixed-step boundary tolerance.</summary>
    public static ProjectSetting<double> PhysicsJitterFix { get; } =
        new("physics/common/physics_jitter_fix", 0.5d, value => double.IsFinite(value) && value >= 0d);

    /// <summary>Selects WebP encoding effort from zero (fastest) through six (smallest output).</summary>
    public static ProjectSetting<int> WebPCompressionMethod { get; } =
        new("rendering/textures/webp_compression/compression_method", 2, value => value is >= 0 and <= 6);

    /// <summary>Selects lossless WebP compression effort from zero through one hundred.</summary>
    /// <remarks>This changes encoding cost and size, while preserving exact RGBA8 pixels.</remarks>
    public static ProjectSetting<float> WebPLosslessCompressionFactor { get; } =
        new("rendering/textures/webp_compression/lossless_compression_factor", 25f, value => float.IsFinite(value) && value is >= 0 and <= 100);

    /// <summary>Selects the canvas rendering method sampled when Engine.Run opens its window.</summary>
    public static ProjectSetting<string> RenderingMethod { get; } =
        new("rendering/renderer/rendering_method", "gpu", value => value is "gpu" or "compatibility");

    /// <summary>Allows startup to use the compatibility renderer when GPU initialization fails.</summary>
    /// <remarks>Set false to require the programmable GPU path. Shader materials reject compatibility rendering.</remarks>
    public static ProjectSetting<bool> RenderingFallback { get; } =
        new("rendering/rendering_device/fallback_to_opengl3", true);

    /// <summary>Selects canvas multisample antialiasing for the root window at engine startup.</summary>
    /// <value>rendering/anti_aliasing/quality/msaa_2d; Disabled initially.</value>
    /// <remarks>Scene and caller-owned offscreen viewports keep their own selections. Enabled MSAA requires a GPU backend with the requested sample count.</remarks>
    public static ProjectSetting<Viewport.MSAA> MSAA2D { get; } =
        new("rendering/anti_aliasing/quality/msaa_2d", Viewport.MSAA.Disabled, value => (uint)value < (uint)Viewport.MSAA.Max);

    /// <summary>Selects linear half-float canvas output for an unassigned root window at startup.</summary>
    /// <value>rendering/viewport/hdr_2d; false initially.</value>
    /// <remarks>Offscreen viewports retain their authored selections. Enabled HDR requires GPU rendering and supported half-float color targets.</remarks>
    public static ProjectSetting<bool> HDR2D { get; } = new("rendering/viewport/hdr_2d", false);

    /// <summary>Requests native HDR output for an unassigned root window at startup.</summary>
    /// <value>display/window/hdr/request_hdr_output; false initially.</value>
    /// <remarks>Explicit Window authoring takes precedence. Native display support determines the active output;
    /// requested windows use effective linear half-float canvases while independent SubViewports retain their policy.</remarks>
    public static ProjectSetting<bool> HDROutputRequested { get; } = new("display/window/hdr/request_hdr_output", false);

    /// <summary>Selects HDR root screen dithering at engine startup.</summary>
    /// <value>rendering/anti_aliasing/quality/use_debanding; false initially.</value>
    /// <remarks>An explicit root assignment wins. Only root HDR screen presentation uses this GPU effect;
    /// offscreen and byte canvas pixels and movie capture retain their original values. Compatibility rejects enabled debanding.</remarks>
    public static ProjectSetting<bool> UseDebanding { get; } = new("rendering/anti_aliasing/quality/use_debanding", false);

    /// <summary>Selects the LCD subpixel arrangement captured when the native text owner first starts.</summary>
    /// <value>gui/theme/lcd_subpixel_layout; HorizontalRGB initially. None through VerticalBGR are valid.</value>
    /// <remarks>Set before first native font use. Later assignments persist the next process's preference and require restart.
    /// None makes LCD fonts rasterize grayscale; enabled component layouts require supported GPU drawing.</remarks>
    public static ProjectSetting<FontLCDSubpixelLayout> LCDSubpixelLayout { get; } =
        new("gui/theme/lcd_subpixel_layout", FontLCDSubpixelLayout.HorizontalRGB, value => (uint)value < (uint)FontLCDSubpixelLayout.Max);

    internal static ProjectSetting<bool> NavigationMapAsyncIterations { get; } = new("navigation/world/map_use_async_iterations", true);
    internal static ProjectSetting<bool> NavigationRegionAsyncIterations { get; } = new("navigation/world/region_use_async_iterations", true);

    /// <summary>Defines the worker count sampled on the first pool submission.</summary>
    /// <value>threading/worker_pool/max_threads; -1 uses max(1, processor count minus one), otherwise a positive count.</value>
    public static ProjectSetting<int> WorkerPoolMaxThreads { get; } =
        new("threading/worker_pool/max_threads", -1, value => value == -1 || value > 0);

    /// <summary>Defines the fraction of workers available for ordinary priority jobs at pool startup.</summary>
    /// <value>threading/worker_pool/low_priority_thread_ratio; finite zero through one, default .5; at least one slot is available and a multi-worker pool reserves one for high priority.</value>
    public static ProjectSetting<float> WorkerPoolLowPriorityThreadRatio { get; } =
        new("threading/worker_pool/low_priority_thread_ratio", .5f, value => float.IsFinite(value) && value >= 0 && value <= 1);

    /// <summary>Defines the positive number of pending task/group records prepared on first submission.</summary>
    /// <value>threading/worker_pool/max_pending_tasks; defaults to 1024. Completed IDs occupy capacity until waited.
    /// Queue ticket capacity is this value times the worker count; checked overflow rejects startup.</value>
    public static ProjectSetting<int> WorkerPoolMaxPendingTasks { get; } =
        new("threading/worker_pool/max_pending_tasks", 1024, value => value > 0);

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
    public static ProjectSetting<bool> UseNearestMIPMapFilter { get; } =
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

    /// <summary>Defines the default native line-breaking strictness for text without explicit locale options.</summary>
    /// <value>internationalization/locale/line_breaking_strictness; 0 Auto, 1 Loose, 2 Normal or 3 Strict. Default 0.</value>
    /// <remarks>The effective feature override is captured at the first nonempty native line-boundary request.
    /// Later edits require process restart. A language tag containing explicit locale options retains those options
    /// instead of receiving this default. Auto delegates tailoring to the native locale rules.</remarks>
    public static ProjectSetting<int> LineBreakingStrictness { get; } =
        new("internationalization/locale/line_breaking_strictness", 0, value => value is >= 0 and <= 3);

    /// <summary>Defines an optional locale used instead of the process UI culture when an application loop starts.</summary>
    /// <value>The permanent typed project setting, empty by default.</value>
    public static ProjectSetting<string> LocaleTest { get; } =
        new("internationalization/locale/test", string.Empty, IsValidProjectLocale);

    /// <summary>Defines the catalog locale consulted after the selected locale has no translation.</summary>
    /// <value>The permanent typed project setting, <c>en</c> by default. Empty disables the fallback.</value>
    public static ProjectSetting<string> LocaleFallback { get; } =
        new("internationalization/locale/fallback", "en", IsValidProjectLocale);

    /// <summary>Defines the direction used when a control or window has no same-domain layout ancestor.</summary>
    /// <value>0 selects application locale (default), 1 LTR, 2 RTL and 3 system UI locale.</value>
    /// <remarks>Sampled with feature overrides at SceneTree construction. Detached nodes use the current setting.</remarks>
    public static ProjectSetting<int> RootNodeLayoutDirection { get; } =
        new("internationalization/rendering/root_node_layout_direction", 0, value => value is >= 0 and <= 3);

    /// <summary>Forces inherited and locale-based control and window layout to right to left.</summary>
    /// <value>False by default. Explicit LTR and RTL choices remain authoritative.</value>
    /// <remarks>Sampled with feature overrides at SceneTree construction. Detached nodes use the current setting.</remarks>
    public static ProjectSetting<bool> ForceRightToLeftLayoutDirection { get; } =
        new("internationalization/rendering/force_right_to_left_layout_direction", false);

    /// <summary>Determines whether a newly activated scene root automatically translates messages.</summary>
    /// <value>The permanent typed project setting, true by default; sampled at scene-tree construction.</value>
    public static ProjectSetting<bool> RootNodeAutoTranslate { get; } =
        new("internationalization/rendering/root_node_auto_translate", true);

    /// <summary>Defines the initial picking policy for a newly activated root viewport without an explicit authored choice.</summary>
    /// <value>True by default; sampled once during scene activation, including feature overrides.</value>
    public static ProjectSetting<bool> PhysicsObjectPicking { get; } =
        new("physics/common/enable_object_picking", true);

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

    /// <summary>Defines the configurable keyboard/assistive drag-and-drop action.</summary>
    /// <value>The permanent input/ui_accessibility_drag_and_drop setting; no bindings initially.</value>
    public static ProjectSetting<InputActionSettings> InputUIAccessibilityDragAndDrop { get; } = new("input/ui_accessibility_drag_and_drop", new InputActionSettings());

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

    /// <summary>Defines Delete and gamepad X for removing a focused color swatch.</summary>
    /// <value>The permanent typed input/ui_colorpicker_delete_preset definition.</value>
    public static ProjectSetting<InputActionSettings> InputUIColorPickerDeletePreset { get; } = new("input/ui_colorpicker_delete_preset", new InputActionSettings
    {
        Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Delete }, new InputBindingSettings { Kind = InputBindingKind.JoypadButton, JoyButtonIndex = JoyButton.X, Device = InputMap.AllDevices }]
    });

    /// <summary>Defines cancellation/acceptance button order for newly added dialog cancellation buttons.</summary>
    /// <value>Zero chooses the platform order; one places Cancel first and two places OK first.</value>
    public static ProjectSetting<int> SwapCancelOK { get; } = new("gui/common/swap_cancel_ok", 0, value => value is >= 0 and <= 2);

    /// <summary>Defines Delete for the file browser's recoverable trash command.</summary><value>The permanent input/ui_filedialog_delete definition.</value>
    public static ProjectSetting<InputActionSettings> InputUIFileDialogDelete { get; } = CreateDefaultKeyAction("ui_filedialog_delete", Key.Delete);
    /// <summary>Defines Backspace for file-browser parent navigation.</summary><value>The permanent input/ui_filedialog_up_one_level definition.</value>
    public static ProjectSetting<InputActionSettings> InputUIFileDialogUpOneLevel { get; } = CreateDefaultKeyAction("ui_filedialog_up_one_level", Key.Backspace);
    /// <summary>Defines F5 for file-browser refresh.</summary><value>The permanent input/ui_filedialog_refresh definition.</value>
    public static ProjectSetting<InputActionSettings> InputUIFileDialogRefresh { get; } = CreateDefaultKeyAction("ui_filedialog_refresh", Key.F5);
    /// <summary>Defines H for toggling hidden files.</summary><value>The permanent input/ui_filedialog_show_hidden definition.</value>
    public static ProjectSetting<InputActionSettings> InputUIFileDialogShowHidden { get; } = CreateDefaultKeyAction("ui_filedialog_show_hidden", Key.H);
    /// <summary>Defines Command/Control-F for the filename filter.</summary><value>The permanent input/ui_filedialog_find definition.</value>
    public static ProjectSetting<InputActionSettings> InputUIFileDialogFind { get; } = CreateDefaultKeyAction("ui_filedialog_find", Key.F, KeyModifierMask.CommandOrControl);
    /// <summary>Defines Command/Control-L for the current path field.</summary><value>The permanent input/ui_filedialog_focus_path definition.</value>
    public static ProjectSetting<InputActionSettings> InputUIFileDialogFocusPath { get; } = new("input/ui_filedialog_focus_path", new InputActionSettings
    {
        Bindings = OperatingSystem.IsMacOS()
            ? [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.G, Modifiers = KeyModifierMask.CommandOrControl }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.L, Modifiers = KeyModifierMask.Meta }]
            : [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.L, Modifiers = KeyModifierMask.CommandOrControl }]
    });

    /// <summary>Defines Escape for closing a dialog, and Command-W additionally on macOS.</summary>
    /// <value>The permanent typed input/ui_close_dialog setting, independent of ui_cancel.</value>
    public static ProjectSetting<InputActionSettings> InputUICloseDialog { get; } = new("input/ui_close_dialog", new InputActionSettings
    {
        Bindings = OperatingSystem.IsMacOS()
            ? [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Escape }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.W, Modifiers = KeyModifierMask.Meta }]
            : [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Escape }]
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

    /// <summary>Defines the maximum committed multiline undo steps sampled when an editor is constructed.</summary><value>The nonnegative gui/common/text_edit_undo_stack_max_size setting; 1024 initially, zero disables committed history.</value>
    public static ProjectSetting<int> TextEditUndoStackMaxSize { get; } = new("gui/common/text_edit_undo_stack_max_size", 1024, value => value >= 0);
    /// <summary>Defines Enter and keypad Enter for multiline newline insertion.</summary><value>The typed input/ui_text_newline definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextNewline { get; } = new("input/ui_text_newline", new InputActionSettings { Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.KeypadEnter }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter, Modifiers = KeyModifierMask.Shift }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.KeypadEnter, Modifiers = KeyModifierMask.Shift }] });
    /// <summary>Defines Command/Control-Home for document start.</summary><value>The typed input/ui_text_caret_document_start definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretDocumentStart { get; } = CreateDefaultKeyAction("ui_text_caret_document_start", Key.Home, KeyModifierMask.CommandOrControl);
    /// <summary>Defines Command/Control-End for document end.</summary><value>The typed input/ui_text_caret_document_end definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCaretDocumentEnd { get; } = CreateDefaultKeyAction("ui_text_caret_document_end", Key.End, KeyModifierMask.CommandOrControl);
    /// <summary>Defines Insert for switching typing mode.</summary><value>The typed input/ui_text_toggle_insert_mode definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextToggleInsertMode { get; } = CreateDefaultKeyAction("ui_text_toggle_insert_mode", Key.Insert, 0);

    /// <summary>Defines the default keyboard binding for ui_swap_input_direction.</summary>
    /// <value>The permanent typed input/ui_swap_input_direction setting; projects may override its bindings.</value>
    public static ProjectSetting<InputActionSettings> InputUISwapInputDirection { get; } = CreateDefaultKeyAction("ui_swap_input_direction", Key.QuoteLeft, KeyModifierMask.CommandOrControl);

    /// <summary>Defines the connection timeout for new TCP stream attempts.</summary>
    /// <value>Thirty nonnegative seconds initially; sampled on ConnectToHost.</value>
    public static ProjectSetting<int> TCPConnectTimeoutSeconds { get; } = new("network/limits/tcp/connect_timeout_seconds", 30, value => value >= 0);

    /// <summary>Defines the connection timeout for new UDS stream attempts.</summary>
    /// <value>Thirty nonnegative seconds initially; sampled on ConnectToHost.</value>
    public static ProjectSetting<int> UDSConnectTimeoutSeconds { get; } = new("network/limits/unix/connect_timeout_seconds", 30, value => value >= 0);

    private static ProjectSettings SharedInstance => ServiceHolder.Value;
    private static class ServiceHolder { internal static readonly ProjectSettings Value = CreateSharedInstance(); }

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
