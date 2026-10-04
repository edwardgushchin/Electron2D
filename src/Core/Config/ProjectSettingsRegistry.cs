using IOPath = System.IO.Path;
using System.Globalization;
using System.Runtime.InteropServices;
using static Electron2D.ProjectSettings;

namespace Electron2D;

/// <summary>Owns an independent typed project settings registry and its persistence state.</summary>
/// <remarks>Use ProjectSettings for the permanent runtime registry. Caller-created registries are disposable and independent.</remarks>
public partial class ProjectSettingsRegistry : ElectronObject
{

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
    public ProjectSettingsRegistry(string projectRoot)
        : this(projectRoot, GetDefaultUserDataRoot(projectRoot), processWide: false)
    {
    }

    /// <summary>Initializes an isolated registry with explicit project and user-data directories.</summary>
    /// <param name="projectRoot">An existing project directory.</param>
    /// <param name="userDataRoot">The user-writable data directory; it is not created automatically.</param>
    /// <exception cref="ArgumentNullException"><paramref name="projectRoot"/> or <paramref name="userDataRoot"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty or invalid, or both paths resolve to the same directory.</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="projectRoot"/> does not exist.</exception>
    public ProjectSettingsRegistry(string projectRoot, string userDataRoot)
        : this(projectRoot, userDataRoot, processWide: false)
    {
    }

    internal ProjectSettingsRegistry(string projectRoot, string userDataRoot, bool processWide)
    {
        (_projectRoot, _userDataRoot) = NormalizeRoots(projectRoot, userDataRoot);
        _processWide = processWide;
        AddBuiltInFeatures(_builtInFeatures);
        RegisterInternal(ApplicationName, isBasic: true);
        RegisterInternal(ApplicationVersion, isBasic: true);
        RegisterInternal(AudioDriverEnableInput, isBasic: true);
        RegisterInternal(AudioGeneralDefaultPlaybackType, isBasic: true);
        RegisterInternal(AudioGeneral2DPanningStrength, isBasic: false);
        RegisterInternal(AudioBusesChannelDisableThresholdDB, isBasic: false);
        RegisterInternal(AudioBusesChannelDisableTime, isBasic: false);
        RegisterInternal(PhysicsTicksPerSecond, isBasic: true);
        RegisterInternal(Physics2DDefaultGravity, isBasic: true);
        RegisterInternal(Physics2DDefaultGravityVector, isBasic: true);
        RegisterInternal(Physics2DDefaultLinearDamp, isBasic: false);
        RegisterInternal(Physics2DDefaultAngularDamp, isBasic: false);
        RegisterInternal(MaxPhysicsStepsPerFrame, isBasic: false);
        RegisterInternal(PhysicsJitterFix, isBasic: false);
        RegisterInternal(RenderingMethod, isBasic: true);
        RegisterInternal(RenderingFallback, isBasic: false);
        RegisterInternal(RenderingTimeRolloverSeconds, isBasic: false);
        RegisterInternal(SnapTransformsToPixel, isBasic: false);
        RegisterInternal(SnapVerticesToPixel, isBasic: false);
        RegisterInternal(UseNearestMipmapFilter, isBasic: false);
        RegisterInternal(AnisotropicFilteringLevel, isBasic: false);
        RegisterInternal(DefaultClearColor, isBasic: true);
        RegisterInternal(DebugPathsColor, isBasic: false);
        RegisterInternal(LocaleTest, isBasic: false);
        RegisterInternal(LocaleFallback, isBasic: false);
        RegisterInternal(RootNodeAutoTranslate, isBasic: false);
        RegisterInternal(PhysicsInterpolation, isBasic: false);
        RegisterInternal(PseudolocalizationEnabled, isBasic: false);
        RegisterInternal(PseudolocalizationReplaceWithAccents, isBasic: false);
        RegisterInternal(PseudolocalizationDoubleVowels, isBasic: false);
        RegisterInternal(PseudolocalizationFakeBIDI, isBasic: false);
        RegisterInternal(PseudolocalizationOverride, isBasic: false);
        RegisterInternal(PseudolocalizationExpansionRatio, isBasic: false);
        RegisterInternal(PseudolocalizationPrefix, isBasic: false);
        RegisterInternal(PseudolocalizationSuffix, isBasic: false);
        RegisterInternal(PseudolocalizationSkipPlaceholders, isBasic: false);
        RegisterInternal(IgnoreJoypadOnUnfocusedApplication, isBasic: false);
        RegisterInternal(InputUIFocusNext, isBasic: false);
        RegisterInternal(InputUIFocusPrev, isBasic: false);
        RegisterInternal(InputUILeft, isBasic: false);
        RegisterInternal(InputUIUp, isBasic: false);
        RegisterInternal(InputUIRight, isBasic: false);
        RegisterInternal(InputUIDown, isBasic: false);
        RegisterInternal(InputUIHome, isBasic: false);
        RegisterInternal(InputUIEnd, isBasic: false);
        RegisterInternal(InputUISelect, isBasic: false);
        RegisterInternal(InputUIPageUp, isBasic: false);
        RegisterInternal(InputUIPageDown, isBasic: false);
        RegisterInternal(InputUIMenu, isBasic: false);
        RegisterInternal(InputUIAccept, isBasic: false);
        RegisterInternal(InputUICancel, isBasic: false);
        RegisterInternal(TCPConnectTimeoutSeconds, isBasic: false);
        RegisterInternal(UDSConnectTimeoutSeconds, isBasic: false);
        RegisterInternal(InputUITextSubmit, isBasic: false);
        RegisterInternal(InputUITextSelectAll, isBasic: false);
        RegisterInternal(InputUICopy, isBasic: false);
        RegisterInternal(InputUICut, isBasic: false);
        RegisterInternal(InputUIPaste, isBasic: false);
        RegisterInternal(InputUIUndo, isBasic: false);
        RegisterInternal(InputUIRedo, isBasic: false);
        RegisterInternal(InputUITextBackspace, isBasic: false);
        RegisterInternal(InputUITextBackspaceWord, isBasic: false);
        RegisterInternal(InputUITextBackspaceAllToLeft, isBasic: false);
        RegisterInternal(InputUITextDelete, isBasic: false);
        RegisterInternal(InputUITextDeleteWord, isBasic: false);
        RegisterInternal(InputUITextDeleteAllToRight, isBasic: false);
        RegisterInternal(InputUITextCaretLeft, isBasic: false);
        RegisterInternal(InputUITextCaretRight, isBasic: false);
        RegisterInternal(InputUITextCaretWordLeft, isBasic: false);
        RegisterInternal(InputUITextCaretWordRight, isBasic: false);
        RegisterInternal(InputUITextCaretLineStart, isBasic: false);
        RegisterInternal(InputUITextCaretLineEnd, isBasic: false);
        RegisterInternal(InputUITextCaretUp, isBasic: false);
        RegisterInternal(InputUITextCaretDown, isBasic: false);
        RegisterInternal(InputUITextCaretPageUp, isBasic: false);
        RegisterInternal(InputUITextCaretPageDown, isBasic: false);
        RegisterInternal(InputUISwapInputDirection, isBasic: false);

        RegisterInternal(ButtonShortcutFeedbackHighlightTime, isBasic: false);
        RegisterInternal(TooltipDelaySeconds, isBasic: false);
        RegisterInternal(IncrementalSearchMaxIntervalMsec, isBasic: false);
        RegisterInternal(TooltipPositionOffset, isBasic: false);
        RegisterInternal(DefaultScrollDeadzone, isBasic: false);
        RegisterInternal(DefaultGUIDragThreshold, isBasic: false);
    }

    internal string ProjectRootCore
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

    internal string UserDataRootCore
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

    internal string ProjectFilePathCore => IOPath.Combine(ProjectRootCore, ProjectFileName);

    internal string OverrideFilePathCore => IOPath.Combine(ProjectRootCore, OverrideFileName);

    internal string ProjectDataPathCore => IOPath.Combine(ProjectRootCore, ProjectDataDirectoryName);

    internal ulong VersionCore
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

    internal event Action<ProjectSettingsRegistry>? SettingsChangedCore;

    internal void ConfigurePathsCore(string projectRoot, string userDataRoot)
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

    internal void RegisterCore<T>(ProjectSetting<T> setting)
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

    internal void UnregisterCore<T>(ProjectSetting<T> setting)
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

    internal bool HasSettingCore<T>(ProjectSetting<T> setting)
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

    internal IReadOnlyList<string> GetSettingNamesCore(bool includeInternal = true)
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

    internal (string Name, T Value)[] GetRegisteredSettingsInGroup<T>(string prefix)
        where T : notnull
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return _settings.Values
                .Where(entry => entry.Name.StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(entry => entry.Order)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .Select(entry =>
                {
                    if (entry is not SettingEntry<T> typed)
                        throw new InvalidDataException($"Project setting '{entry.Name}' has the wrong value type for '{prefix}'.");
                    return (entry.Name, GetWithOverrideCore((ProjectSetting<T>)typed.Definition));
                })
                .ToArray();
        }
    }

    internal T GetCore<T>(ProjectSetting<T> setting)
    where T : notnull
    {
        var serialized = GetSerialized(setting, applyOverrides: false, features: null);
        return setting.Deserialize(serialized);
    }

    internal T GetWithOverrideCore<T>(ProjectSetting<T> setting)
    where T : notnull
    {
        var serialized = GetSerialized(setting, applyOverrides: true, features: null);
        return setting.Deserialize(serialized);
    }

    internal T GetWithOverrideCore<T>(ProjectSetting<T> setting, IEnumerable<string> features)
    where T : notnull
    {
        ArgumentNullException.ThrowIfNull(features);
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var feature in features)
            normalized.Add(NormalizeFeature(feature));

        var serialized = GetSerialized(setting, applyOverrides: true, normalized);
        return setting.Deserialize(serialized);
    }

    internal void SetCore<T>(ProjectSetting<T> setting, T value)
    where T : notnull
    {
        ArgumentNullException.ThrowIfNull(setting);
        var serialized = setting.SerializeAndValidate(value);
        SetSerialized(setting, feature: null, serialized);
    }

    internal void ResetCore<T>(ProjectSetting<T> setting)
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

    internal void SetInitialValueCore<T>(ProjectSetting<T> setting, T value)
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

    internal void SetFeatureOverrideCore<T>(ProjectSetting<T> setting, string feature, T value)
    where T : notnull
    {
        ArgumentNullException.ThrowIfNull(setting);
        var normalizedFeature = NormalizeFeature(feature);
        var serialized = setting.SerializeAndValidate(value);
        SetSerialized(setting, normalizedFeature, serialized);
    }

    internal bool ClearFeatureOverrideCore<T>(ProjectSetting<T> setting, string feature)
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

    internal IReadOnlyList<string> GetFeatureOverridesCore<T>(ProjectSetting<T> setting)
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

    internal bool AddCustomFeatureCore(string feature) => ChangeCustomFeature(feature, add: true);

    internal bool RemoveCustomFeatureCore(string feature) => ChangeCustomFeature(feature, add: false);

    internal bool HasFeatureCore(string feature)
    {
        ThrowIfDisposed();
        var normalized = NormalizeFeature(feature);
        lock (_gate)
        {
            ThrowIfDisposed();
            return _builtInFeatures.Contains(normalized) || _customFeatures.Contains(normalized);
        }
    }

    internal IReadOnlyList<string> GetActiveFeaturesCore()
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

    internal int GetOrderCore<T>(ProjectSetting<T> setting)
    where T : notnull => ReadMetadata(setting, entry => entry.Order);

    internal void SetOrderCore<T>(ProjectSetting<T> setting, int order)
    where T : notnull => UpdateMetadata(setting, entry => entry.Order = order, entry => entry.Order == order);

    internal bool IsBasicCore<T>(ProjectSetting<T> setting)
    where T : notnull => ReadMetadata(setting, entry => entry.IsBasic);

    internal void SetAsBasicCore<T>(ProjectSetting<T> setting, bool basic)
    where T : notnull => UpdateMetadata(setting, entry => entry.IsBasic = basic, entry => entry.IsBasic == basic);

    internal bool IsInternalCore<T>(ProjectSetting<T> setting)
    where T : notnull => ReadMetadata(setting, entry => entry.IsInternal);

    internal void SetAsInternalCore<T>(ProjectSetting<T> setting, bool internalSetting)
    where T : notnull => UpdateMetadata(
        setting,
        entry => entry.IsInternal = internalSetting,
        entry => entry.IsInternal == internalSetting);

    internal bool IsRestartRequiredCore<T>(ProjectSetting<T> setting)
    where T : notnull => ReadMetadata(setting, entry => entry.RestartIfChanged);

    internal void SetRestartIfChangedCore<T>(ProjectSetting<T> setting, bool restart)
    where T : notnull => UpdateMetadata(
        setting,
        entry => entry.RestartIfChanged = restart,
        entry => entry.RestartIfChanged == restart);

    internal IReadOnlyList<string> GetChangedSettingsCore()
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

    internal bool CheckChangedSettingsInGroupCore(string prefix)
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

    internal void LoadCore()
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

    internal void LoadCustomCore(string path)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A path cannot be empty.", nameof(path));

        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureNotLoading();
            var globalPath = GlobalizePathCore(path);
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

    internal void SaveCore() => SaveCore(path: null);

    internal void SaveCustomCore(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A path cannot be empty.", nameof(path));
        SaveCore(path);
    }

    internal bool FlushChangesCore()
    {
        ThrowIfDisposed();
        Action<ProjectSettingsRegistry>? handler;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_deliveringChanges is not null || !_changeNotificationPending)
                return false;

            _changeNotificationPending = false;
            _deliveringChanges = new HashSet<string>(_changedSettings, StringComparer.Ordinal);
            _changedSettings.Clear();
            handler = SettingsChangedCore;
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

    internal string GlobalizePathCore(string path)
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

        if (path.StartsWith("uid://", StringComparison.Ordinal)) return GlobalizePathCore(ResourceUID.EnsurePath(path));
        if (path.StartsWith("res://", StringComparison.Ordinal))
            return ResolveWithinRoot(projectRoot, path[6..]);
        if (path.StartsWith("user://", StringComparison.Ordinal))
            return ResolveWithinRoot(userDataRoot, path[7..]);
        if (path.Contains("://", StringComparison.Ordinal))
            throw new NotSupportedException($"Virtual path scheme in '{path}' is not supported.");

        return IOPath.GetFullPath(path);
    }

    internal string LocalizePathCore(string path)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A path cannot be empty.", nameof(path));
        var isKnownVirtualPath = path.StartsWith("res://", StringComparison.Ordinal) ||
                                 path.StartsWith("user://", StringComparison.Ordinal);
        if (!isKnownVirtualPath && path.Contains("://", StringComparison.Ordinal))
            throw new NotSupportedException($"Virtual path scheme in '{path}' is not supported.");

        var fullPath = isKnownVirtualPath ? GlobalizePathCore(path) : IOPath.GetFullPath(path);
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
            throw new InvalidOperationException("The process-wide ProjectSettingsRegistry instance cannot be disposed.");

        lock (_gate)
        {
            if (_loading)
                throw new InvalidOperationException("ProjectSettingsRegistry cannot be disposed during a load callback.");
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
                SettingsChangedCore = null;
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

    internal static ConfigKey<T> CreateConfigKey<T>(string fullName)
    {
        var separator = fullName.IndexOf('/');
        return new ConfigKey<T>(fullName[..separator], fullName[(separator + 1)..]);
    }

    internal static ProjectSetting<InputActionSettings> CreateDefaultKeyAction(
        string name, Key key, KeyModifierMask modifiers = 0) =>
        new($"input/{name}", new InputActionSettings
        {
            Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = key, Modifiers = modifiers }],
        });

    internal static ProjectSetting<InputActionSettings> CreateDefaultDirectionalAction(
        string name, Key key, JoyButton button, JoyAxis axis, float axisValue) =>
        new($"input/{name}", new InputActionSettings
        {
            Bindings =
            [
                new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = key },
                new InputBindingSettings { Kind = InputBindingKind.JoypadButton, JoyButtonIndex = button, Device = InputMap.AllDevices },
                new InputBindingSettings { Kind = InputBindingKind.JoypadMotion, JoyAxis = axis, AxisValue = axisValue, Device = InputMap.AllDevices },
            ],
        });

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

    internal static string GetDefaultUserDataRoot(string projectRoot)
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
            var destination = path is null ? IOPath.Combine(_projectRoot, ProjectFileName) : GlobalizePathCore(path);
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

    internal static bool IsValidProjectLocale(string locale)
    {
        if (locale is null) return false;
        locale = locale.Trim();
        if (locale.Length == 0) return true;
        try { _ = CultureInfo.GetCultureInfo(locale.Replace('_', '-')); return true; }
        catch (CultureNotFoundException) { return false; }
    }

    private static bool IsBuiltIn<T>(ProjectSetting<T> setting)
        where T : notnull =>
        ReferenceEquals(setting, ApplicationName) ||
        ReferenceEquals(setting, ApplicationVersion) ||
        ReferenceEquals(setting, AudioDriverEnableInput) ||
        ReferenceEquals(setting, AudioGeneralDefaultPlaybackType) ||
        ReferenceEquals(setting, AudioGeneral2DPanningStrength) ||
        ReferenceEquals(setting, AudioBusesChannelDisableThresholdDB) ||
        ReferenceEquals(setting, AudioBusesChannelDisableTime) ||
        ReferenceEquals(setting, PhysicsTicksPerSecond) ||
        ReferenceEquals(setting, Physics2DDefaultGravity) ||
        ReferenceEquals(setting, Physics2DDefaultGravityVector) ||
        ReferenceEquals(setting, Physics2DDefaultLinearDamp) ||
        ReferenceEquals(setting, Physics2DDefaultAngularDamp) ||
        ReferenceEquals(setting, MaxPhysicsStepsPerFrame) ||
        ReferenceEquals(setting, PhysicsJitterFix) ||
        ReferenceEquals(setting, RenderingMethod) ||
        ReferenceEquals(setting, RenderingFallback) ||
        ReferenceEquals(setting, RenderingTimeRolloverSeconds) ||
        ReferenceEquals(setting, SnapTransformsToPixel) ||
        ReferenceEquals(setting, SnapVerticesToPixel) ||
        ReferenceEquals(setting, UseNearestMipmapFilter) ||
        ReferenceEquals(setting, AnisotropicFilteringLevel) ||
        ReferenceEquals(setting, DefaultClearColor) ||
        ReferenceEquals(setting, DebugPathsColor) ||
        ReferenceEquals(setting, LocaleTest) ||
        ReferenceEquals(setting, LocaleFallback) ||
        ReferenceEquals(setting, RootNodeAutoTranslate) ||
        ReferenceEquals(setting, PhysicsInterpolation) ||
        ReferenceEquals(setting, PseudolocalizationEnabled) ||
        ReferenceEquals(setting, PseudolocalizationReplaceWithAccents) ||
        ReferenceEquals(setting, PseudolocalizationDoubleVowels) ||
        ReferenceEquals(setting, PseudolocalizationFakeBIDI) ||
        ReferenceEquals(setting, PseudolocalizationOverride) ||
        ReferenceEquals(setting, PseudolocalizationExpansionRatio) ||
        ReferenceEquals(setting, PseudolocalizationPrefix) ||
        ReferenceEquals(setting, PseudolocalizationSuffix) ||
        ReferenceEquals(setting, PseudolocalizationSkipPlaceholders) ||
        ReferenceEquals(setting, IgnoreJoypadOnUnfocusedApplication) ||
        ReferenceEquals(setting, InputUIFocusNext) ||
        ReferenceEquals(setting, InputUIFocusPrev) ||
        ReferenceEquals(setting, InputUILeft) ||
        ReferenceEquals(setting, InputUIUp) ||
        ReferenceEquals(setting, InputUIRight) ||
        ReferenceEquals(setting, InputUIDown) ||
        ReferenceEquals(setting, InputUIHome) ||
        ReferenceEquals(setting, InputUIEnd) ||
        ReferenceEquals(setting, InputUISelect) ||
        ReferenceEquals(setting, InputUIPageUp) ||
        ReferenceEquals(setting, InputUIPageDown) ||
        ReferenceEquals(setting, InputUIMenu) ||
        ReferenceEquals(setting, InputUIAccept) ||
        ReferenceEquals(setting, InputUICancel) ||
        ReferenceEquals(setting, TCPConnectTimeoutSeconds) ||
        ReferenceEquals(setting, UDSConnectTimeoutSeconds) ||
        ReferenceEquals(setting, InputUITextSubmit) ||
        ReferenceEquals(setting, InputUITextSelectAll) ||
        ReferenceEquals(setting, InputUICopy) ||
        ReferenceEquals(setting, InputUICut) ||
        ReferenceEquals(setting, InputUIPaste) ||
        ReferenceEquals(setting, InputUIUndo) ||
        ReferenceEquals(setting, InputUIRedo) ||
        ReferenceEquals(setting, InputUITextBackspace) ||
        ReferenceEquals(setting, InputUITextBackspaceWord) ||
        ReferenceEquals(setting, InputUITextBackspaceAllToLeft) ||
        ReferenceEquals(setting, InputUITextDelete) ||
        ReferenceEquals(setting, InputUITextDeleteWord) ||
        ReferenceEquals(setting, InputUITextDeleteAllToRight) ||
        ReferenceEquals(setting, InputUITextCaretLeft) ||
        ReferenceEquals(setting, InputUITextCaretRight) ||
        ReferenceEquals(setting, InputUITextCaretWordLeft) ||
        ReferenceEquals(setting, InputUITextCaretWordRight) ||
        ReferenceEquals(setting, InputUITextCaretLineStart) ||
        ReferenceEquals(setting, InputUITextCaretLineEnd) ||
        ReferenceEquals(setting, InputUITextCaretUp) ||
        ReferenceEquals(setting, InputUITextCaretDown) ||
        ReferenceEquals(setting, InputUITextCaretPageUp) ||
        ReferenceEquals(setting, InputUITextCaretPageDown) ||
        ReferenceEquals(setting, InputUISwapInputDirection) ||

        ReferenceEquals(setting, ButtonShortcutFeedbackHighlightTime) ||
        ReferenceEquals(setting, TooltipDelaySeconds) ||
        ReferenceEquals(setting, IncrementalSearchMaxIntervalMsec) ||
        ReferenceEquals(setting, DefaultScrollDeadzone) ||
        ReferenceEquals(setting, DefaultGUIDragThreshold) ||
        ReferenceEquals(setting, TooltipPositionOffset);

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
            new PropertyDescriptor<ProjectSettingsRegistry, T>(
                Name,
                owner => owner.GetCore(Setting),
                (owner, value) => owner.SetCore(Setting, value),
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
