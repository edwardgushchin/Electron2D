using IOPath = System.IO.Path;
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
/// <see cref="Instance"/> is the process-wide runtime registry. Additional instances may be created for isolated
/// tooling, tests, or multiple unopened projects. All public operations are thread-safe; disk and serialization
/// operations are not suitable for real-time callbacks.
/// </para>
/// <para>
/// Settings must be registered before typed access. Unknown values loaded from disk remain preserved and become
/// available when a matching typed setting is later registered. The registry never exposes an untyped value API.
/// </para>
/// </remarks>
public sealed class ProjectSettings : ElectronObject
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

    /// <summary>Defines the fixed-step callback frequency used by <see cref="Engine"/>.</summary>
    public static ProjectSetting<int> PhysicsTicksPerSecond { get; } =
        new("physics/common/physics_ticks_per_second", 60, value => value > 0);

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

    private static readonly ProjectSettings SharedInstance = CreateSharedInstance();

    private readonly object _gate = new();
    private readonly Dictionary<string, SettingEntry> _settings = new(StringComparer.Ordinal);
    private readonly HashSet<string> _changedSettings = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dirtySettings = new(StringComparer.Ordinal);
    private readonly HashSet<string> _builtInFeatures = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _customFeatures = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _processWide;

    private ConfigFile _document = new();
    private string _projectRoot;
    private string _userDataRoot;
    private ulong _version = 1;
    private bool _changeNotificationPending;
    private HashSet<string>? _deliveringChanges;
    private bool _loading;
    private int _nextOrder;

    /// <summary>Initializes an isolated registry using a platform-appropriate user-data directory.</summary>
    /// <param name="projectRoot">An existing project directory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="projectRoot"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="projectRoot"/> is empty or invalid.</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="projectRoot"/> does not exist.</exception>
    public ProjectSettings(string projectRoot)
        : this(projectRoot, GetDefaultUserDataRoot(projectRoot), processWide: false)
    {
    }

    /// <summary>Initializes an isolated registry with explicit project and user-data directories.</summary>
    /// <param name="projectRoot">An existing project directory.</param>
    /// <param name="userDataRoot">The user-writable data directory; it is not created automatically.</param>
    /// <exception cref="ArgumentNullException"><paramref name="projectRoot"/> or <paramref name="userDataRoot"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty or invalid, or both paths resolve to the same directory.</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="projectRoot"/> does not exist.</exception>
    public ProjectSettings(string projectRoot, string userDataRoot)
        : this(projectRoot, userDataRoot, processWide: false)
    {
    }

    private ProjectSettings(string projectRoot, string userDataRoot, bool processWide)
    {
        (_projectRoot, _userDataRoot) = NormalizeRoots(projectRoot, userDataRoot);
        _processWide = processWide;
        AddBuiltInFeatures(_builtInFeatures);
        RegisterInternal(ApplicationName, isBasic: true);
        RegisterInternal(ApplicationVersion, isBasic: true);
        RegisterInternal(PhysicsTicksPerSecond, isBasic: true);
        RegisterInternal(MaxPhysicsStepsPerFrame, isBasic: false);
        RegisterInternal(PhysicsJitterFix, isBasic: false);
        RegisterInternal(RenderingMethod, isBasic: true);
        RegisterInternal(RenderingFallback, isBasic: false);
        RegisterInternal(SnapTransformsToPixel, isBasic: false);
        RegisterInternal(SnapVerticesToPixel, isBasic: false);
        RegisterInternal(UseNearestMipmapFilter, isBasic: false);
        RegisterInternal(AnisotropicFilteringLevel, isBasic: false);
        RegisterInternal(DefaultClearColor, isBasic: true);
    }

    /// <summary>Gets the process-wide project settings registry.</summary>
    /// <value>The same non-disposable instance for the lifetime of the process.</value>
    public static ProjectSettings Instance => SharedInstance;

    /// <summary>Gets the current absolute project resource directory.</summary>
    /// <value>The directory to which <c>res://</c> resolves.</value>
    public string ProjectRoot
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                return _projectRoot;
            }
        }
    }

    /// <summary>Gets the current absolute user-data directory.</summary>
    /// <value>The directory to which <c>user://</c> resolves. The directory may not exist yet.</value>
    public string UserDataRoot
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                return _userDataRoot;
            }
        }
    }

    /// <summary>Gets the absolute path of the current project settings file.</summary>
    /// <value><see cref="ProjectFileName"/> inside <see cref="ProjectRoot"/>.</value>
    public string ProjectFilePath => IOPath.Combine(ProjectRoot, ProjectFileName);

    /// <summary>Gets the absolute path of the optional runtime override file.</summary>
    /// <value><see cref="OverrideFileName"/> inside <see cref="ProjectRoot"/>.</value>
    public string OverrideFilePath => IOPath.Combine(ProjectRoot, OverrideFileName);

    /// <summary>Gets the absolute path reserved for generated project-local engine data.</summary>
    /// <value><see cref="ProjectDataDirectoryName"/> inside <see cref="ProjectRoot"/>.</value>
    public string ProjectDataPath => IOPath.Combine(ProjectRoot, ProjectDataDirectoryName);

    /// <summary>Gets a monotonically increasing in-process registry version.</summary>
    /// <value>A value starting at one and incremented after registry, value, or active-feature changes.</value>
    public ulong Version
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                return _version;
            }
        }
    }

    /// <summary>Occurs after one or more settings change and <see cref="FlushChanges"/> is called.</summary>
    /// <remarks>
    /// Delivery is synchronous on the flushing thread and is coalesced to one invocation per flush. A change made by
    /// a handler is retained for the next flush. The process-wide <see cref="Engine"/> flushes at the end of each
    /// successfully processed frame. Handler exceptions propagate after the pending invocation has been consumed.
    /// </remarks>
    public event Action<ProjectSettings>? SettingsChanged;

    /// <summary>Changes the project and user-data roots and clears all loaded or explicitly stored values.</summary>
    /// <param name="projectRoot">An existing project directory.</param>
    /// <param name="userDataRoot">The user-writable data directory; it is not created automatically.</param>
    /// <remarks>Registered setting definitions and metadata are retained. Call this during host setup before runtime use.</remarks>
    /// <exception cref="ArgumentNullException">A path is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty or invalid, or both paths resolve to the same directory.</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="projectRoot"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">Unsaved setting changes exist or a load is already running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void ConfigurePaths(string projectRoot, string userDataRoot)
    {
        var roots = NormalizeRoots(projectRoot, userDataRoot);
        ThrowIfDisposed();

        ConfigFile oldDocument;
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            if (_dirtySettings.Count != 0)
                throw new InvalidOperationException("Project paths cannot change while unsaved settings exist.");

            oldDocument = _document;
            _document = new ConfigFile();
            _projectRoot = roots.ProjectRoot;
            _userDataRoot = roots.UserDataRoot;
            _changedSettings.Clear();
            _dirtySettings.Clear();
            _changeNotificationPending = false;
            foreach (var entry in _settings.Values)
                entry.OverrideFeatures.Clear();
            _version++;
        }

        oldDocument.Dispose();
    }

    /// <summary>Registers a setting definition for typed access and tooling discovery.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The definition to register.</param>
    /// <remarks>
    /// If a matching value was loaded before registration, it and all matching feature overrides are validated before
    /// the registry changes. Registration does not mark the setting as modified.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The name is already registered or a load is running.</exception>
    /// <exception cref="InvalidDataException">A loaded value cannot be decoded as <typeparamref name="T"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting validator rejects a loaded value.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after registration has completed.</exception>
    public void Register<T>(ProjectSetting<T> setting)
        where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(setting);

        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            if (_settings.ContainsKey(setting.Name))
                throw new InvalidOperationException($"Project setting '{setting.Name}' is already registered.");

            RegisterInternal(setting, isBasic: false);
            _version++;
        }

        NotifyPropertyListChanged();
    }

    /// <summary>Unregisters a setting and removes its stored base value and feature overrides.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <remarks>Built-in settings cannot be unregistered. The removal is tracked as an unsaved change.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">The setting is built in, a different definition owns its name, or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after removal has completed.</exception>
    public void Unregister<T>(ProjectSetting<T> setting)
        where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(setting);

        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            var entry = GetEntry(setting);
            if (entry.IsBuiltIn)
                throw new InvalidOperationException($"Built-in project setting '{setting.Name}' cannot be unregistered.");

            entry.RemoveStoredValues(_document);
            _settings.Remove(setting.Name);
            MarkChangedLocked(setting.Name);
        }

        NotifyPropertyListChanged();
    }

    /// <summary>Determines whether the exact setting definition is registered.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The definition to inspect.</param>
    /// <returns><see langword="true"/> only when this exact definition owns its name in this registry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool HasSetting<T>(ProjectSetting<T> setting)
        where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(setting);

        lock (_gate)
        {
            ThrowIfDisposed();
            return _settings.TryGetValue(setting.Name, out var entry) && ReferenceEquals(entry.Definition, setting);
        }
    }

    /// <summary>Gets registered setting names ordered by explicit order and then by name.</summary>
    /// <param name="includeInternal">Whether settings marked as internal are included.</param>
    /// <returns>An immutable snapshot of full case-sensitive setting paths.</returns>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public IReadOnlyList<string> GetSettingNames(bool includeInternal = true)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return Array.AsReadOnly(_settings.Values
                .Where(entry => includeInternal || !entry.IsInternal)
                .OrderBy(entry => entry.Order)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .Select(entry => entry.Name)
                .ToArray());
        }
    }

    /// <summary>Gets a setting value without applying feature overrides.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>An independent snapshot of the explicit value, or the current initial value when no explicit value exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public T Get<T>(ProjectSetting<T> setting)
        where T : notnull
    {
        var serialized = GetSerialized(setting, applyOverrides: false, features: null);
        return setting.Deserialize(serialized);
    }

    /// <summary>Gets a setting after applying the first override matching an active platform, build, or custom feature.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>An independent value snapshot.</returns>
    /// <remarks>Override precedence follows the insertion order of override entries in the loaded or modified document.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public T GetWithOverride<T>(ProjectSetting<T> setting)
        where T : notnull
    {
        var serialized = GetSerialized(setting, applyOverrides: true, features: null);
        return setting.Deserialize(serialized);
    }

    /// <summary>Gets a setting after applying overrides against caller-supplied feature tags.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="features">Feature tags replacing the registry's current active feature set for this lookup.</param>
    /// <returns>An independent value snapshot.</returns>
    /// <remarks>Tags are normalized to lowercase. Override precedence follows stored override insertion order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/>, <paramref name="features"/>, or one of its items is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A feature tag is invalid.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public T GetWithOverride<T>(ProjectSetting<T> setting, IEnumerable<string> features)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(features);
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var feature in features)
            normalized.Add(NormalizeFeature(feature));

        var serialized = GetSerialized(setting, applyOverrides: true, normalized);
        return setting.Deserialize(serialized);
    }

    /// <summary>Stores a base value for a registered setting.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="value">The non-null value to validate and snapshot.</param>
    /// <remarks>An equal serialized value is a no-op and does not update change tracking or <see cref="Version"/>.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting validator rejects <paramref name="value"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="System.Text.Json.JsonException"><paramref name="value"/> cannot be serialized.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void Set<T>(ProjectSetting<T> setting, T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(setting);
        var serialized = setting.SerializeAndValidate(value);
        SetSerialized(setting, feature: null, serialized);
    }

    /// <summary>Removes an explicit base value so the setting returns its current initial value.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <remarks>Feature overrides are retained. The initial value becomes implicit and is not persisted.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void Reset<T>(ProjectSetting<T> setting)
        where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(setting);

        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            var entry = GetEntry(setting);
            var key = entry.BaseKey;
            if (!_document.TryGetSerializedValue(key, out _))
                return;

            _document.SetSerializedValue(key, null);
            MarkChangedLocked(setting.Name);
        }
    }

    /// <summary>Changes the value used by <see cref="Reset{T}"/> and typed property reversion.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="value">The new non-null validated initial value.</param>
    /// <remarks>
    /// The current value and unsaved state do not change. If necessary, the previous current value becomes explicit so
    /// changing the implicit value cannot change what callers observe.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting validator rejects <paramref name="value"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void SetInitialValue<T>(ProjectSetting<T> setting, T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(setting);
        var serialized = setting.SerializeAndValidate(value);
        ThrowIfDisposed();

        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            var entry = GetEntry(setting);
            if (StringComparer.Ordinal.Equals(entry.InitialSerialized, serialized))
                return;

            var current = _document.TryGetSerializedValue(entry.BaseKey, out var stored)
                ? stored
                : entry.InitialSerialized;
            entry.InitialSerialized = serialized;
            _document.SetSerializedValue(
                entry.BaseKey,
                StringComparer.Ordinal.Equals(current, serialized) ? null : current);
        }

        NotifyPropertyListChanged();
    }

    /// <summary>Stores a value selected when the named feature is active.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="feature">The platform, build, architecture, or custom feature tag.</param>
    /// <param name="value">The non-null value to validate and snapshot.</param>
    /// <remarks>New overrides have lower priority than existing overrides for the same setting.</remarks>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="feature"/> is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting validator rejects <paramref name="value"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void SetFeatureOverride<T>(ProjectSetting<T> setting, string feature, T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(setting);
        var normalizedFeature = NormalizeFeature(feature);
        var serialized = setting.SerializeAndValidate(value);
        SetSerialized(setting, normalizedFeature, serialized);
    }

    /// <summary>Removes one stored feature override.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="feature">The feature tag to remove.</param>
    /// <returns><see langword="true"/> when an override was removed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> or <paramref name="feature"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="feature"/> is invalid.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool ClearFeatureOverride<T>(ProjectSetting<T> setting, string feature)
        where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(setting);
        var normalizedFeature = NormalizeFeature(feature);

        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            var entry = GetEntry(setting);
            if (!entry.OverrideFeatures.Remove(normalizedFeature))
                return false;

            _document.SetSerializedValue(entry.GetOverrideKey(normalizedFeature), null);
            MarkChangedLocked(setting.Name);
            return true;
        }
    }

    /// <summary>Gets the stored feature tags for one setting in override precedence order.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>An immutable snapshot of normalized feature tags.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public IReadOnlyList<string> GetFeatureOverrides<T>(ProjectSetting<T> setting)
        where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(setting);

        lock (_gate)
        {
            ThrowIfDisposed();
            return Array.AsReadOnly(GetEntry(setting).OverrideFeatures.ToArray());
        }
    }

    /// <summary>Adds a custom feature to the process feature set.</summary>
    /// <param name="feature">The feature tag to add.</param>
    /// <returns><see langword="true"/> when the active set changed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="feature"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">A load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool AddCustomFeature(string feature) => ChangeCustomFeature(feature, add: true);

    /// <summary>Removes a custom feature from the process feature set.</summary>
    /// <param name="feature">The feature tag to remove.</param>
    /// <returns><see langword="true"/> when the active set changed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="feature"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">A load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool RemoveCustomFeature(string feature) => ChangeCustomFeature(feature, add: false);

    /// <summary>Determines whether a platform, build, architecture, or custom feature is active.</summary>
    /// <param name="feature">The feature tag to inspect.</param>
    /// <returns><see langword="true"/> when the normalized feature is active.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="feature"/> is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool HasFeature(string feature)
    {
        ThrowIfDisposed();
        var normalized = NormalizeFeature(feature);
        lock (_gate)
        {
            ThrowIfDisposed();
            return _builtInFeatures.Contains(normalized) || _customFeatures.Contains(normalized);
        }
    }

    /// <summary>Gets all active platform, build, architecture, and custom features.</summary>
    /// <returns>An immutable ordinally sorted snapshot of normalized feature tags.</returns>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public IReadOnlyList<string> GetActiveFeatures()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return Array.AsReadOnly(_builtInFeatures
                .Concat(_customFeatures)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(feature => feature, StringComparer.Ordinal)
                .ToArray());
        }
    }

    /// <summary>Gets the persistence order assigned to a registered setting.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>The signed ordering value; lower values appear first.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public int GetOrder<T>(ProjectSetting<T> setting)
        where T : notnull => ReadMetadata(setting, entry => entry.Order);

    /// <summary>Changes the persistence and tooling order of a registered setting.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="order">The signed ordering value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after the order has changed.</exception>
    public void SetOrder<T>(ProjectSetting<T> setting, int order)
        where T : notnull => UpdateMetadata(setting, entry => entry.Order = order, entry => entry.Order == order);

    /// <summary>Gets whether a setting should appear in a reduced basic-settings view.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>The current basic-view flag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool IsBasic<T>(ProjectSetting<T> setting)
        where T : notnull => ReadMetadata(setting, entry => entry.IsBasic);

    /// <summary>Sets whether a setting should appear in a reduced basic-settings view.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="basic">The new basic-view flag.</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after the flag has changed.</exception>
    public void SetAsBasic<T>(ProjectSetting<T> setting, bool basic)
        where T : notnull => UpdateMetadata(setting, entry => entry.IsBasic = basic, entry => entry.IsBasic == basic);

    /// <summary>Gets whether a setting is hidden from ordinary tooling discovery.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>The current internal flag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool IsInternal<T>(ProjectSetting<T> setting)
        where T : notnull => ReadMetadata(setting, entry => entry.IsInternal);

    /// <summary>Sets whether a setting is hidden from ordinary tooling discovery.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="internalSetting">The new internal flag.</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after the flag has changed.</exception>
    public void SetAsInternal<T>(ProjectSetting<T> setting, bool internalSetting)
        where T : notnull => UpdateMetadata(
            setting,
            entry => entry.IsInternal = internalSetting,
            entry => entry.IsInternal == internalSetting);

    /// <summary>Gets whether changing a setting requires the host application to restart.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>The current restart-required flag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool IsRestartRequired<T>(ProjectSetting<T> setting)
        where T : notnull => ReadMetadata(setting, entry => entry.RestartIfChanged);

    /// <summary>Sets whether changing a setting requires the host application to restart.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="restart">The new restart-required flag.</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after the flag has changed.</exception>
    public void SetRestartIfChanged<T>(ProjectSetting<T> setting, bool restart)
        where T : notnull => UpdateMetadata(
            setting,
            entry => entry.RestartIfChanged = restart,
            entry => entry.RestartIfChanged == restart);

    /// <summary>Gets setting names included in the pending or currently delivered change notification.</summary>
    /// <returns>An immutable ordinally sorted snapshot, cleared after the notification finishes.</returns>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public IReadOnlyList<string> GetChangedSettings()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            var names = _deliveringChanges is null
                ? _changedSettings
                : _deliveringChanges.Concat(_changedSettings);
            return Array.AsReadOnly(names.Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());
        }
    }

    /// <summary>Checks whether any changed setting starts with a category prefix.</summary>
    /// <param name="prefix">The case-sensitive prefix to inspect.</param>
    /// <returns><see langword="true"/> when at least one changed setting starts with <paramref name="prefix"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool CheckChangedSettingsInGroup(string prefix)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(prefix);
        lock (_gate)
        {
            ThrowIfDisposed();
            return _changedSettings.Any(name => name.StartsWith(prefix, StringComparison.Ordinal)) ||
                   (_deliveringChanges?.Any(name => name.StartsWith(prefix, StringComparison.Ordinal)) ?? false);
        }
    }

    /// <summary>Loads the project file and then merges the conventional override file when it exists.</summary>
    /// <remarks>
    /// The complete candidate document and every registered typed value are validated before current state is replaced.
    /// Successful loading clears unsaved-value tracking and any pending change notification.
    /// </remarks>
    /// <exception cref="FileNotFoundException">The project file does not exist.</exception>
    /// <exception cref="IOException">A file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">A file cannot be read by the caller.</exception>
    /// <exception cref="FormatException">A document is malformed.</exception>
    /// <exception cref="InvalidDataException">A registered value has the wrong type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A registered validator rejects a loaded value.</exception>
    /// <exception cref="InvalidOperationException">A re-entrant load or setting mutation is attempted.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void Load()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            _loading = true;
            try
            {
                var projectFile = IOPath.Combine(_projectRoot, ProjectFileName);
                var overrideFile = IOPath.Combine(_projectRoot, OverrideFileName);
                if (!File.Exists(projectFile))
                    throw new FileNotFoundException("The project settings file does not exist.", projectFile);

                using var candidate = new ConfigFile();
                candidate.Load(projectFile);
                if (File.Exists(overrideFile))
                    candidate.Load(overrideFile);

                ReplaceDocumentLocked(candidate.EncodeToText(), clearChanges: true);
            }
            finally
            {
                _loading = false;
            }
        }
    }

    /// <summary>Merges a custom configuration file into the current settings after full validation.</summary>
    /// <param name="path">An operating-system, <c>res://</c>, or <c>user://</c> file path.</param>
    /// <remarks>
    /// Values already unsaved before the merge remain unsaved because loading another document does not persist them.
    /// Values supplied only by the loaded file are treated as persisted and do not add unsaved names.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be read by the caller.</exception>
    /// <exception cref="FormatException">The document is malformed.</exception>
    /// <exception cref="InvalidDataException">A registered value has the wrong type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A registered validator rejects a loaded value.</exception>
    /// <exception cref="InvalidOperationException">A re-entrant load or setting mutation is attempted.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void LoadCustom(string path)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A path cannot be empty.", nameof(path));

        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            var globalPath = GlobalizePath(path);
            _loading = true;
            try
            {
                using var candidate = new ConfigFile();
                candidate.Parse(_document.EncodeToText());
                candidate.Load(globalPath);
                ReplaceDocumentLocked(candidate.EncodeToText(), clearChanges: false);
            }
            finally
            {
                _loading = false;
            }
        }
    }

    /// <summary>Saves the current document to <see cref="ProjectFilePath"/> using atomic replacement.</summary>
    /// <remarks>
    /// A successful save clears internal unsaved-value tracking. Change-notification names remain until
    /// <see cref="FlushChanges"/> completes; a failed save preserves both states.
    /// </remarks>
    /// <exception cref="IOException">The file cannot be written or replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot write the file.</exception>
    /// <exception cref="DirectoryNotFoundException">The project directory no longer exists.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void Save() => SaveCore(path: null);

    /// <summary>Saves the current document to an explicit file using atomic replacement.</summary>
    /// <param name="path">An operating-system, <c>res://</c>, or <c>user://</c> file path.</param>
    /// <remarks>
    /// A successful save clears internal unsaved-value tracking. Change-notification names remain until
    /// <see cref="FlushChanges"/> completes; a failed save preserves both states.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The file cannot be written or replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot write the file.</exception>
    /// <exception cref="DirectoryNotFoundException">The destination directory does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void SaveCustom(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A path cannot be empty.", nameof(path));
        SaveCore(path);
    }

    /// <summary>Raises one coalesced <see cref="SettingsChanged"/> notification when changes are pending.</summary>
    /// <returns><see langword="true"/> when an event invocation was consumed; otherwise <see langword="false"/>.</returns>
    /// <remarks>The callback runs without the registry lock, so handlers may read or modify settings.</remarks>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">An event handler throws.</exception>
    public bool FlushChanges()
    {
        ThrowIfDisposed();
        Action<ProjectSettings>? handler;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_deliveringChanges is not null || !_changeNotificationPending)
                return false;

            _changeNotificationPending = false;
            _deliveringChanges = new HashSet<string>(_changedSettings, StringComparer.Ordinal);
            _changedSettings.Clear();
            handler = SettingsChanged;
        }

        try
        {
            handler?.Invoke(this);
            return true;
        }
        finally
        {
            lock (_gate)
                _deliveringChanges = null;
        }
    }

    /// <summary>Converts a virtual or ordinary path to an absolute native operating-system path.</summary>
    /// <param name="path">A nonempty path, optionally beginning with <c>res://</c> or <c>user://</c>.</param>
    /// <returns>An absolute normalized path.</returns>
    /// <remarks>
    /// Virtual paths are lexically confined to their configured roots and reject parent traversal. This method does not
    /// resolve symbolic links and is not a filesystem security sandbox.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">A virtual path escapes its configured root.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unknown virtual-path scheme.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public string GlobalizePath(string path)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A path cannot be empty.", nameof(path));

        string projectRoot;
        string userDataRoot;
        lock (_gate)
        {
            ThrowIfDisposed();
            projectRoot = _projectRoot;
            userDataRoot = _userDataRoot;
        }

        if (path.StartsWith("res://", StringComparison.Ordinal))
            return ResolveWithinRoot(projectRoot, path[6..]);
        if (path.StartsWith("user://", StringComparison.Ordinal))
            return ResolveWithinRoot(userDataRoot, path[7..]);
        if (path.Contains("://", StringComparison.Ordinal))
            throw new NotSupportedException($"Virtual path scheme in '{path}' is not supported.");

        return IOPath.GetFullPath(path);
    }

    /// <summary>Converts an ordinary path inside a configured root to a virtual project or user path.</summary>
    /// <param name="path">A nonempty operating-system or already virtual path.</param>
    /// <returns>
    /// A <c>res://</c> or <c>user://</c> path when contained by a configured root; otherwise an absolute native path.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unknown virtual-path scheme.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public string LocalizePath(string path)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A path cannot be empty.", nameof(path));
        var isKnownVirtualPath = path.StartsWith("res://", StringComparison.Ordinal) ||
                                 path.StartsWith("user://", StringComparison.Ordinal);
        if (!isKnownVirtualPath && path.Contains("://", StringComparison.Ordinal))
            throw new NotSupportedException($"Virtual path scheme in '{path}' is not supported.");

        var fullPath = isKnownVirtualPath ? GlobalizePath(path) : IOPath.GetFullPath(path);
        string projectRoot;
        string userDataRoot;
        lock (_gate)
        {
            ThrowIfDisposed();
            projectRoot = _projectRoot;
            userDataRoot = _userDataRoot;
        }

        var projectRelative = TryGetRelative(projectRoot, fullPath);
        var userRelative = TryGetRelative(userDataRoot, fullPath);
        if (userRelative is not null && (projectRelative is null || userDataRoot.Length > projectRoot.Length))
            return ToVirtualPath("user://", userRelative);
        if (projectRelative is not null)
            return ToVirtualPath("res://", projectRelative);
        return fullPath;
    }

    internal (string ProjectRoot, string UserDataRoot) GetPathRootsSnapshot()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return (_projectRoot, _userDataRoot);
        }
    }

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

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
            yield return property;

        PropertyDescriptor[] settings;
        lock (_gate)
        {
            ThrowIfDisposed();
            settings = _settings.Values
                .Where(entry => !entry.IsInternal)
                .OrderBy(entry => entry.Order)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .Select(entry => entry.CreateDescriptor())
                .ToArray();
        }

        foreach (var property in settings)
            yield return property;
    }

    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        if (_processWide)
            throw new InvalidOperationException("The process-wide ProjectSettings instance cannot be disposed.");

        lock (_gate)
        {
            if (_loading)
                throw new InvalidOperationException("ProjectSettings cannot be disposed during a load callback.");
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_gate)
            {
                _document.Dispose();
                _settings.Clear();
                _changedSettings.Clear();
                _dirtySettings.Clear();
                _customFeatures.Clear();
                SettingsChanged = null;
            }
        }

        base.Dispose(disposing);
    }

    private static void AddBuiltInFeatures(HashSet<string> features)
    {
        features.Add("dotnet");
#if DEBUG
        features.Add("debug");
#else
        features.Add("release");
#endif

        if (OperatingSystem.IsWindows())
            features.Add("windows");
        else if (OperatingSystem.IsLinux())
            features.Add("linux");
        else if (OperatingSystem.IsMacOS())
            features.Add("macos");
        else if (OperatingSystem.IsAndroid())
            features.Add("android");
        else if (OperatingSystem.IsIOS())
            features.Add("ios");
        else if (OperatingSystem.IsBrowser())
            features.Add("web");

        features.Add(RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x86_64",
            Architecture.X86 => "x86_32",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm32",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
        });
    }

    private static ProjectSettings CreateSharedInstance()
    {
        var projectRoot = Directory.GetCurrentDirectory();
        return new ProjectSettings(projectRoot, GetDefaultUserDataRoot(projectRoot), processWide: true);
    }

    private bool ChangeCustomFeature(string feature, bool add)
    {
        ThrowIfDisposed();
        var normalized = NormalizeFeature(feature);
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            if (_builtInFeatures.Contains(normalized))
                return false;

            var previousSelections = _settings.Values.ToDictionary(
                entry => entry,
                entry => entry.GetActiveOverride(_builtInFeatures, _customFeatures));
            var changed = add ? _customFeatures.Add(normalized) : _customFeatures.Remove(normalized);
            if (!changed)
                return false;

            foreach (var entry in _settings.Values)
            {
                if (!StringComparer.Ordinal.Equals(
                        previousSelections[entry],
                        entry.GetActiveOverride(_builtInFeatures, _customFeatures)))
                {
                    QueueChangedLocked(entry.Name);
                }
            }

            _version++;
            return true;
        }
    }

    private static ConfigKey<T> CreateConfigKey<T>(string fullName)
    {
        var separator = fullName.IndexOf('/');
        return new ConfigKey<T>(fullName[..separator], fullName[(separator + 1)..]);
    }

    private void EnsureNotLoading()
    {
        if (_loading)
            throw new InvalidOperationException("Project settings cannot be mutated re-entrantly while a document is loading.");
    }

    private SettingEntry<T> GetEntry<T>(ProjectSetting<T> setting)
        where T : notnull
    {
        if (!_settings.TryGetValue(setting.Name, out var entry))
            throw new KeyNotFoundException($"Project setting '{setting.Name}' is not registered.");
        if (entry is not SettingEntry<T> typed || !ReferenceEquals(entry.Definition, setting))
            throw new InvalidOperationException($"A different project setting definition owns the name '{setting.Name}'.");
        return typed;
    }

    private T GetInitialValue<T>(ProjectSetting<T> setting)
        where T : notnull
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return setting.Deserialize(GetEntry(setting).InitialSerialized);
        }
    }

    private string GetSerialized<T>(ProjectSetting<T> setting, bool applyOverrides, HashSet<string>? features)
        where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(setting);

        lock (_gate)
        {
            ThrowIfDisposed();
            var entry = GetEntry(setting);
            if (applyOverrides)
            {
                foreach (var feature in entry.OverrideFeatures)
                {
                    var active = features is null
                        ? _builtInFeatures.Contains(feature) || _customFeatures.Contains(feature)
                        : features.Contains(feature);
                    if (!active)
                        continue;

                    var overrideKey = entry.GetOverrideKey(feature);
                    if (_document.TryGetSerializedValue(overrideKey, out var overrideValue))
                        return overrideValue;
                }
            }

            return _document.TryGetSerializedValue(entry.BaseKey, out var value)
                ? value
                : entry.InitialSerialized;
        }
    }

    private static string GetDefaultUserDataRoot(string projectRoot)
    {
        ArgumentNullException.ThrowIfNull(projectRoot);
        if (projectRoot.Length == 0)
            throw new ArgumentException("A project root cannot be empty.", nameof(projectRoot));

        var fullRoot = IOPath.GetFullPath(projectRoot);
        var projectName = IOPath.GetFileName(fullRoot.TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar));
        if (projectName.Length == 0)
            projectName = "project";

        foreach (var invalid in IOPath.GetInvalidFileNameChars())
            projectName = projectName.Replace(invalid, '_');

        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (localData.Length == 0)
            localData = IOPath.GetTempPath();

        var userDataRoot = IOPath.Combine(localData, "Electron2D", projectName);
        return PathComparer.Equals(fullRoot, IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(userDataRoot)))
            ? IOPath.Combine(userDataRoot, "user")
            : userDataRoot;
    }

    private void MarkChangedLocked(string name)
    {
        _dirtySettings.Add(name);
        QueueChangedLocked(name);
        _version++;
    }

    private void QueueChangedLocked(string name)
    {
        _changedSettings.Add(name);
        _changeNotificationPending = true;
    }

    private static string NormalizeFeature(string feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (feature.Length == 0 || !char.IsAsciiLetterOrDigit(feature[0]) ||
            !char.IsAsciiLetterOrDigit(feature[^1]) || feature.Contains("..", StringComparison.Ordinal) ||
            feature.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.')))
        {
            throw new ArgumentException(
                "A feature tag must start and end with an ASCII letter or digit and contain only letters, digits, periods, underscores, or hyphens.",
                nameof(feature));
        }

        return feature.ToLowerInvariant();
    }

    private static (string ProjectRoot, string UserDataRoot) NormalizeRoots(string projectRoot, string userDataRoot)
    {
        ArgumentNullException.ThrowIfNull(projectRoot);
        ArgumentNullException.ThrowIfNull(userDataRoot);
        if (projectRoot.Length == 0)
            throw new ArgumentException("A project root cannot be empty.", nameof(projectRoot));
        if (userDataRoot.Length == 0)
            throw new ArgumentException("A user-data root cannot be empty.", nameof(userDataRoot));

        var normalizedProject = IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(projectRoot));
        var normalizedUser = IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(userDataRoot));
        if (!Directory.Exists(normalizedProject))
            throw new DirectoryNotFoundException($"Project root '{normalizedProject}' does not exist.");
        if (PathComparer.Equals(normalizedProject, normalizedUser))
            throw new ArgumentException("Project and user-data roots must be different directories.", nameof(userDataRoot));

        return (normalizedProject, normalizedUser);
    }

    private TResult ReadMetadata<T, TResult>(ProjectSetting<T> setting, Func<SettingEntry<T>, TResult> read)
        where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(setting);
        lock (_gate)
        {
            ThrowIfDisposed();
            return read(GetEntry(setting));
        }
    }

    private void RegisterInternal<T>(ProjectSetting<T> setting, bool isBasic)
        where T : notnull
    {
        var entry = new SettingEntry<T>(setting, _nextOrder++, isBasic, isBuiltIn: IsBuiltIn(setting));
        entry.ReloadOverrides(_document, EnumerateFullNames(_document));
        _settings.Add(setting.Name, entry);
    }

    private void ReplaceDocumentLocked(string encodedDocument, bool clearChanges)
    {
        var replacement = new ConfigFile();
        try
        {
            replacement.Parse(encodedDocument);
            var names = EnumerateFullNames(replacement);
            var overrides = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var entry in _settings.Values)
                overrides.Add(entry.Name, entry.ValidateAndFindOverrides(replacement, names));

            var old = _document;
            _document = replacement;
            replacement = null!;
            foreach (var entry in _settings.Values)
            {
                entry.OverrideFeatures.Clear();
                entry.OverrideFeatures.AddRange(overrides[entry.Name]);
            }

            if (clearChanges)
            {
                _changedSettings.Clear();
                _dirtySettings.Clear();
                _changeNotificationPending = false;
            }
            _version++;
            old.Dispose();
        }
        finally
        {
            replacement?.Dispose();
        }
    }

    private void SaveCore(string? path)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            var destination = path is null ? IOPath.Combine(_projectRoot, ProjectFileName) : GlobalizePath(path);
            var preferredOrder = new List<(string Section, string Name)>();
            foreach (var entry in _settings.Values
                         .OrderBy(entry => entry.Order)
                         .ThenBy(entry => entry.Name, StringComparer.Ordinal))
            {
                entry.RemoveRedundantBaseValue(_document);
                preferredOrder.Add(SplitFullName(entry.Name));
                foreach (var feature in entry.OverrideFeatures)
                    preferredOrder.Add(SplitFullName($"{entry.Name}.{feature}"));
            }

            _document.ReorderEntries(preferredOrder);
            _document.Save(destination);
            _dirtySettings.Clear();
        }
    }

    private static (string Section, string Name) SplitFullName(string fullName)
    {
        var separator = fullName.IndexOf('/');
        return (fullName[..separator], fullName[(separator + 1)..]);
    }

    private void SetSerialized<T>(ProjectSetting<T> setting, string? feature, string serialized)
        where T : notnull
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            var entry = GetEntry(setting);
            var key = feature is null ? entry.BaseKey : entry.GetOverrideKey(feature);
            var current = _document.TryGetSerializedValue(key, out var value)
                ? value
                : feature is null ? entry.InitialSerialized : null;
            if (StringComparer.Ordinal.Equals(current, serialized))
                return;

            _document.SetSerializedValue(
                key,
                feature is null && StringComparer.Ordinal.Equals(serialized, entry.InitialSerialized)
                    ? null
                    : serialized);
            if (feature is not null && !entry.OverrideFeatures.Contains(feature, StringComparer.OrdinalIgnoreCase))
                entry.OverrideFeatures.Add(feature);
            MarkChangedLocked(setting.Name);
        }
    }

    private void UpdateMetadata<T>(
        ProjectSetting<T> setting,
        Action<SettingEntry<T>> update,
        Func<SettingEntry<T>, bool> unchanged)
        where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(setting);
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            var entry = GetEntry(setting);
            if (unchanged(entry))
                return;
            update(entry);
        }

        NotifyPropertyListChanged();
    }

    private static List<string> EnumerateFullNames(ConfigFile document)
    {
        var names = new List<string>();
        foreach (var section in document.GetSections())
        {
            foreach (var key in document.GetSectionKeys(section))
                names.Add(section.Length == 0 ? key : $"{section}/{key}");
        }

        return names;
    }

    private static bool IsBuiltIn<T>(ProjectSetting<T> setting)
        where T : notnull =>
        ReferenceEquals(setting, ApplicationName) ||
        ReferenceEquals(setting, ApplicationVersion) ||
        ReferenceEquals(setting, PhysicsTicksPerSecond) ||
        ReferenceEquals(setting, MaxPhysicsStepsPerFrame) ||
        ReferenceEquals(setting, PhysicsJitterFix) ||
        ReferenceEquals(setting, RenderingMethod) ||
        ReferenceEquals(setting, RenderingFallback) ||
        ReferenceEquals(setting, SnapTransformsToPixel) ||
        ReferenceEquals(setting, SnapVerticesToPixel) ||
        ReferenceEquals(setting, UseNearestMipmapFilter) ||
        ReferenceEquals(setting, AnisotropicFilteringLevel) ||
        ReferenceEquals(setting, DefaultClearColor);

    private static string ResolveWithinRoot(string root, string relativePath)
    {
        var nativeRelative = relativePath.Replace('/', IOPath.DirectorySeparatorChar)
            .Replace('\\', IOPath.DirectorySeparatorChar);
        if (IOPath.IsPathRooted(nativeRelative))
            throw new UnauthorizedAccessException("A virtual path cannot contain an absolute suffix.");

        var resolved = IOPath.GetFullPath(IOPath.Combine(root, nativeRelative));
        if (TryGetRelative(root, resolved) is null)
            throw new UnauthorizedAccessException("A virtual path cannot escape its configured root.");
        return resolved;
    }

    private static string? TryGetRelative(string root, string fullPath)
    {
        var relative = IOPath.GetRelativePath(root, fullPath);
        if (relative == ".")
            return string.Empty;
        if (IOPath.IsPathRooted(relative) || relative == ".." ||
            relative.StartsWith($"..{IOPath.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            relative.StartsWith($"..{IOPath.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return null;
        }

        return relative;
    }

    private static string ToVirtualPath(string prefix, string relative) =>
        relative.Length == 0 ? prefix : prefix + relative.Replace('\\', '/');

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private abstract class SettingEntry
    {
        protected SettingEntry(object definition, string name, string initialSerialized, int order, bool isBasic, bool isBuiltIn)
        {
            Definition = definition;
            Name = name;
            InitialSerialized = initialSerialized;
            Order = order;
            IsBasic = isBasic;
            IsBuiltIn = isBuiltIn;
        }

        internal object Definition { get; }

        internal string Name { get; }

        internal string InitialSerialized { get; set; }

        internal int Order { get; set; }

        internal bool IsBasic { get; set; }

        internal bool IsInternal { get; set; }

        internal bool RestartIfChanged { get; set; }

        internal bool IsBuiltIn { get; }

        internal List<string> OverrideFeatures { get; } = [];

        internal abstract PropertyDescriptor CreateDescriptor();

        internal abstract List<string> ValidateAndFindOverrides(ConfigFile document, IReadOnlyList<string> names);

        internal abstract void RemoveStoredValues(ConfigFile document);

        internal abstract void RemoveRedundantBaseValue(ConfigFile document);

        internal string? GetActiveOverride(IReadOnlySet<string> builtInFeatures, IReadOnlySet<string> customFeatures) =>
            OverrideFeatures.FirstOrDefault(feature =>
                builtInFeatures.Contains(feature) || customFeatures.Contains(feature));

        internal void ReloadOverrides(ConfigFile document, IReadOnlyList<string> names)
        {
            OverrideFeatures.Clear();
            OverrideFeatures.AddRange(ValidateAndFindOverrides(document, names));
        }
    }

    private sealed class SettingEntry<T> : SettingEntry
        where T : notnull
    {
        private readonly Dictionary<string, ConfigKey<T>> _overrideKeys = new(StringComparer.OrdinalIgnoreCase);

        internal SettingEntry(ProjectSetting<T> setting, int order, bool isBasic, bool isBuiltIn)
            : base(setting, setting.Name, setting.DefaultSerialized, order, isBasic, isBuiltIn)
        {
            BaseKey = CreateConfigKey<T>(setting.Name);
        }

        private ProjectSetting<T> Setting => (ProjectSetting<T>)Definition;

        internal ConfigKey<T> BaseKey { get; }

        internal ConfigKey<T> GetOverrideKey(string feature)
        {
            if (_overrideKeys.TryGetValue(feature, out var key))
                return key;

            key = CreateConfigKey<T>($"{Name}.{feature}");
            _overrideKeys.Add(feature, key);
            return key;
        }

        internal override PropertyDescriptor CreateDescriptor() =>
            new PropertyDescriptor<ProjectSettings, T>(
                Name,
                owner => owner.Get(Setting),
                (owner, value) => owner.Set(Setting, value),
                owner => owner.GetInitialValue(Setting));

        internal override List<string> ValidateAndFindOverrides(ConfigFile document, IReadOnlyList<string> names)
        {
            Setting.ValidateSerialized(document.TryGetSerializedValue(BaseKey, out var baseValue)
                ? baseValue
                : Setting.DefaultSerialized);

            var features = new List<string>();
            var prefix = Name + ".";
            foreach (var name in names)
            {
                if (!name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                var persistedFeature = name[prefix.Length..];
                var feature = NormalizeFeature(persistedFeature);
                if (!StringComparer.Ordinal.Equals(persistedFeature, feature))
                    throw new FormatException($"Project setting '{Name}' feature override '{persistedFeature}' must use lowercase.");
                if (features.Contains(feature, StringComparer.OrdinalIgnoreCase))
                    throw new FormatException($"Project setting '{Name}' contains duplicate feature override '{feature}'.");

                var key = GetOverrideKey(feature);
                if (!document.TryGetSerializedValue(key, out var serialized))
                    continue;
                Setting.ValidateSerialized(serialized);
                features.Add(feature);
            }

            return features;
        }

        internal override void RemoveStoredValues(ConfigFile document)
        {
            document.SetSerializedValue(BaseKey, null);
            foreach (var feature in OverrideFeatures)
                document.SetSerializedValue(GetOverrideKey(feature), null);
        }

        internal override void RemoveRedundantBaseValue(ConfigFile document)
        {
            if (document.TryGetSerializedValue(BaseKey, out var value) &&
                StringComparer.Ordinal.Equals(value, InitialSerialized))
            {
                document.SetSerializedValue(BaseKey, null);
            }
        }
    }
}
