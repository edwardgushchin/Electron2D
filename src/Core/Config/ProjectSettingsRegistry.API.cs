namespace Electron2D;

public partial class ProjectSettingsRegistry
{
    /// <summary>Gets the current absolute project resource directory.</summary>
    /// <value>The directory to which <c>res://</c> resolves.</value>
    public string ProjectRoot
    {
        get => ProjectRootCore;
    }

    /// <summary>Gets the current absolute user-data directory.</summary>
    /// <value>The directory to which <c>user://</c> resolves. The directory may not exist yet.</value>
    public string UserDataRoot
    {
        get => UserDataRootCore;
    }

    /// <summary>Gets the absolute path of the current project settings file.</summary>
    /// <value><see cref="ProjectSettings.ProjectFileName"/> inside <see cref="ProjectRoot"/>.</value>
    public string ProjectFilePath
    {
        get => ProjectFilePathCore;
    }

    /// <summary>Gets the absolute path of the optional runtime override file.</summary>
    /// <value><see cref="ProjectSettings.OverrideFileName"/> inside <see cref="ProjectRoot"/>.</value>
    public string OverrideFilePath
    {
        get => OverrideFilePathCore;
    }

    /// <summary>Gets the absolute path reserved for generated project-local engine data.</summary>
    /// <value><see cref="ProjectSettings.ProjectDataDirectoryName"/> inside <see cref="ProjectRoot"/>.</value>
    public string ProjectDataPath
    {
        get => ProjectDataPathCore;
    }

    /// <summary>Gets a monotonically increasing in-process registry version.</summary>
    /// <value>A value starting at one and incremented after registry, value, or active-feature changes.</value>
    public ulong Version
    {
        get => VersionCore;
    }

    /// <summary>Occurs after one or more settings change and <see cref="FlushChanges"/> is called.</summary>
    /// <remarks>
    /// Delivery is synchronous on the flushing thread and is coalesced to one invocation per flush. A change made by
    /// a handler is retained for the next flush. The process-wide <see cref="Engine"/> flushes at the end of each
    /// successfully processed frame. Handler exceptions propagate after the pending invocation has been consumed.
    /// </remarks>
    public event Action<ProjectSettingsRegistry>? SettingsChanged
    {
        add => SettingsChangedCore += value;
        remove => SettingsChangedCore -= value;
    }

    /// <summary>Changes the project and user-data roots and clears all loaded or explicitly stored values.</summary>
    /// <param name="projectRoot">An existing project directory.</param>
    /// <param name="userDataRoot">The user-writable data directory; it is not created automatically.</param>
    /// <remarks>Registered setting definitions and metadata are retained. Call this during host setup before runtime use.</remarks>
    /// <exception cref="ArgumentNullException">A path is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty or invalid, or both paths resolve to the same directory.</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="projectRoot"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">Unsaved setting changes exist or a load is already running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void ConfigurePaths(string projectRoot, string userDataRoot) => ConfigurePathsCore(projectRoot, userDataRoot);

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
    public void Register<T>(ProjectSetting<T> setting) where T : notnull => RegisterCore<T>(setting);

    /// <summary>Unregisters a setting and removes its stored base value and feature overrides.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <remarks>Built-in settings cannot be unregistered. The removal is tracked as an unsaved change.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">The setting is built in, a different definition owns its name, or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after removal has completed.</exception>
    public void Unregister<T>(ProjectSetting<T> setting) where T : notnull => UnregisterCore<T>(setting);

    /// <summary>Determines whether the exact setting definition is registered.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The definition to inspect.</param>
    /// <returns><see langword="true"/> only when this exact definition owns its name in this registry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool HasSetting<T>(ProjectSetting<T> setting) where T : notnull => HasSettingCore<T>(setting);

    /// <summary>Gets registered setting names ordered by explicit order and then by name.</summary>
    /// <param name="includeInternal">Whether settings marked as internal are included.</param>
    /// <returns>An immutable snapshot of full case-sensitive setting paths.</returns>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public IReadOnlyList<string> GetSettingNames(bool includeInternal = true) => GetSettingNamesCore(includeInternal);

    /// <summary>Gets a setting value without applying feature overrides.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>An independent snapshot of the explicit value, or the current initial value when no explicit value exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public T Get<T>(ProjectSetting<T> setting) where T : notnull => GetCore<T>(setting);

    /// <summary>Gets a setting after applying the first override matching an active platform, build, or custom feature.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>An independent value snapshot.</returns>
    /// <remarks>Override precedence follows the insertion order of override entries in the loaded or modified document.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public T GetWithOverride<T>(ProjectSetting<T> setting) where T : notnull => GetWithOverrideCore<T>(setting);

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
    public T GetWithOverride<T>(ProjectSetting<T> setting, IEnumerable<string> features) where T : notnull => GetWithOverrideCore<T>(setting, features);

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
    public void Set<T>(ProjectSetting<T> setting, T value) where T : notnull => SetCore<T>(setting, value);

    /// <summary>Removes an explicit base value so the setting returns its current initial value.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <remarks>Feature overrides are retained. The initial value becomes implicit and is not persisted.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void Reset<T>(ProjectSetting<T> setting) where T : notnull => ResetCore<T>(setting);

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
    public void SetInitialValue<T>(ProjectSetting<T> setting, T value) where T : notnull => SetInitialValueCore<T>(setting, value);

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
    public void SetFeatureOverride<T>(ProjectSetting<T> setting, string feature, T value) where T : notnull => SetFeatureOverrideCore<T>(setting, feature, value);

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
    public bool ClearFeatureOverride<T>(ProjectSetting<T> setting, string feature) where T : notnull => ClearFeatureOverrideCore<T>(setting, feature);

    /// <summary>Gets the stored feature tags for one setting in override precedence order.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>An immutable snapshot of normalized feature tags.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public IReadOnlyList<string> GetFeatureOverrides<T>(ProjectSetting<T> setting) where T : notnull => GetFeatureOverridesCore<T>(setting);

    /// <summary>Adds a custom feature to the process feature set.</summary>
    /// <param name="feature">The feature tag to add.</param>
    /// <returns><see langword="true"/> when the active set changed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="feature"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">A load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool AddCustomFeature(string feature) => AddCustomFeatureCore(feature);

    /// <summary>Removes a custom feature from the process feature set.</summary>
    /// <param name="feature">The feature tag to remove.</param>
    /// <returns><see langword="true"/> when the active set changed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="feature"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">A load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool RemoveCustomFeature(string feature) => RemoveCustomFeatureCore(feature);

    /// <summary>Determines whether a platform, build, architecture, or custom feature is active.</summary>
    /// <param name="feature">The feature tag to inspect.</param>
    /// <returns><see langword="true"/> when the normalized feature is active.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="feature"/> is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool HasFeature(string feature) => HasFeatureCore(feature);

    /// <summary>Gets all active platform, build, architecture, and custom features.</summary>
    /// <returns>An immutable ordinally sorted snapshot of normalized feature tags.</returns>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public IReadOnlyList<string> GetActiveFeatures() => GetActiveFeaturesCore();

    /// <summary>Gets the persistence order assigned to a registered setting.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>The signed ordering value; lower values appear first.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public int GetOrder<T>(ProjectSetting<T> setting) where T : notnull => GetOrderCore<T>(setting);

    /// <summary>Changes the persistence and tooling order of a registered setting.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="order">The signed ordering value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after the order has changed.</exception>
    public void SetOrder<T>(ProjectSetting<T> setting, int order) where T : notnull => SetOrderCore<T>(setting, order);

    /// <summary>Gets whether a setting should appear in a reduced basic-settings view.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>The current basic-view flag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool IsBasic<T>(ProjectSetting<T> setting) where T : notnull => IsBasicCore<T>(setting);

    /// <summary>Sets whether a setting should appear in a reduced basic-settings view.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="basic">The new basic-view flag.</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after the flag has changed.</exception>
    public void SetAsBasic<T>(ProjectSetting<T> setting, bool basic) where T : notnull => SetAsBasicCore<T>(setting, basic);

    /// <summary>Gets whether a setting is hidden from ordinary tooling discovery.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>The current internal flag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool IsInternal<T>(ProjectSetting<T> setting) where T : notnull => IsInternalCore<T>(setting);

    /// <summary>Sets whether a setting is hidden from ordinary tooling discovery.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="internalSetting">The new internal flag.</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after the flag has changed.</exception>
    public void SetAsInternal<T>(ProjectSetting<T> setting, bool internalSetting) where T : notnull => SetAsInternalCore<T>(setting, internalSetting);

    /// <summary>Gets whether changing a setting requires the host application to restart.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <returns>The current restart-required flag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool IsRestartRequired<T>(ProjectSetting<T> setting) where T : notnull => IsRestartRequiredCore<T>(setting);

    /// <summary>Sets whether changing a setting requires the host application to restart.</summary>
    /// <typeparam name="T">The non-null setting value type.</typeparam>
    /// <param name="setting">The exact registered definition.</param>
    /// <param name="restart">The new restart-required flag.</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="setting"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">A different definition owns the same name or a load is running.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list listener throws after the flag has changed.</exception>
    public void SetRestartIfChanged<T>(ProjectSetting<T> setting, bool restart) where T : notnull => SetRestartIfChangedCore<T>(setting, restart);

    /// <summary>Gets setting names included in the pending or currently delivered change notification.</summary>
    /// <returns>An immutable ordinally sorted snapshot, cleared after the notification finishes.</returns>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public IReadOnlyList<string> GetChangedSettings() => GetChangedSettingsCore();

    /// <summary>Checks whether any changed setting starts with a category prefix.</summary>
    /// <param name="prefix">The case-sensitive prefix to inspect.</param>
    /// <returns><see langword="true"/> when at least one changed setting starts with <paramref name="prefix"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public bool CheckChangedSettingsInGroup(string prefix) => CheckChangedSettingsInGroupCore(prefix);

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
    public void Load() => LoadCore();

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
    public void LoadCustom(string path) => LoadCustomCore(path);

    /// <summary>Saves the current document to <see cref="ProjectFilePath"/> using atomic replacement.</summary>
    /// <remarks>
    /// A successful save clears internal unsaved-value tracking. Change-notification names remain until
    /// <see cref="FlushChanges"/> completes; a failed save preserves both states.
    /// </remarks>
    /// <exception cref="IOException">The file cannot be written or replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot write the file.</exception>
    /// <exception cref="DirectoryNotFoundException">The project directory no longer exists.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public void Save() => SaveCore();

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
    public void SaveCustom(string path) => SaveCustomCore(path);

    /// <summary>Raises one coalesced <see cref="SettingsChanged"/> notification when changes are pending.</summary>
    /// <returns><see langword="true"/> when an event invocation was consumed; otherwise <see langword="false"/>.</returns>
    /// <remarks>The callback runs without the registry lock, so handlers may read or modify settings.</remarks>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    /// <exception cref="Exception">An event handler throws.</exception>
    public bool FlushChanges() => FlushChangesCore();

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
    public string GlobalizePath(string path) => GlobalizePathCore(path);

    /// <summary>Converts an ordinary path inside a configured root to a virtual project or user path.</summary>
    /// <param name="path">A nonempty operating-system or already virtual path.</param>
    /// <returns>
    /// A <c>res://</c> or <c>user://</c> path when contained by a configured root; otherwise an absolute native path.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unknown virtual-path scheme.</exception>
    /// <exception cref="ObjectDisposedException">The registry is disposing or disposed.</exception>
    public string LocalizePath(string path) => LocalizePathCore(path);

}
