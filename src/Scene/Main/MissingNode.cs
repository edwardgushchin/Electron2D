namespace Electron2D;

/// <summary>Retains authored state when a saved node's compiled factory or scene is unavailable.</summary>
/// <remarks>Only tolerant scene loading creates this role. It has ordinary Node behavior and no unavailable
/// compiled behavior. Stored-value and connection recording are bounded typed authoring operations.
/// Recorded connections do not execute until the original endpoint and adapter are restored.
/// Access is confined to the attached scene's owner thread and fails after disposal. Mutations also
/// observe the ordinary editor authoring guard and synchronous editor-change callbacks.</remarks>
public sealed partial class MissingNode : Node
{
    internal MissingNode() { }
    internal readonly MissingResourceData Data = new();
    private readonly HashSet<string> _signals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Node?> _nodeReferences = new(StringComparer.Ordinal);
    private string _originalClass = "", _originalScene = "";
    private bool _recordingSignals;

    /// <summary>Gets or sets the unavailable compiled factory identity used when this node is saved.</summary>
    /// <value>The original portable factory ID; initially empty.</value>
    /// <exception cref="ArgumentNullException">The assigned identity is null.</exception>
    /// <exception cref="InvalidOperationException">The scene owner thread or authoring guard rejects access.</exception>
    /// <exception cref="ObjectDisposedException">This node has been disposed.</exception>
    public string OriginalClass { get { EnsurePlaceholderAccess(); return _originalClass; } set { EnsurePendingPropertyMutation(); ArgumentNullException.ThrowIfNull(value); _originalClass = value; NotifyPendingPropertyChanged(); } }
    /// <summary>Gets or sets the unavailable linked scene path for configuration diagnostics.</summary>
    /// <value>The unavailable scene path, or empty for an unavailable class.</value>
    /// <remarks>Saving this role with a scene identity discards that absent instance and its unavailable overrides.</remarks>
    /// <exception cref="ArgumentNullException">The assigned path is null.</exception>
    /// <exception cref="InvalidOperationException">The scene owner thread or authoring guard rejects access.</exception>
    /// <exception cref="ObjectDisposedException">This node has been disposed.</exception>
    public string OriginalScene { get { EnsurePlaceholderAccess(); return _originalScene; } set { EnsurePendingPropertyMutation(); ArgumentNullException.ThrowIfNull(value); _originalScene = value; NotifyPendingPropertyChanged(); } }
    /// <summary>Gets or sets whether new archived property names may be recorded.</summary>
    /// <value>False after loading. Existing names remain editable regardless of this flag.</value>
    /// <exception cref="InvalidOperationException">The scene owner thread or authoring guard rejects access.</exception>
    /// <exception cref="ObjectDisposedException">This node has been disposed.</exception>
    public bool RecordingProperties { get { EnsurePlaceholderAccess(); return Data.Recording; } set { EnsurePendingPropertyMutation(); Data.Recording = value; } }
    /// <summary>Gets or sets whether new archived event identities may be recorded.</summary>
    /// <value>False after loading. Existing event identities may receive additional connection records.</value>
    /// <exception cref="InvalidOperationException">The scene owner thread or authoring guard rejects access.</exception>
    /// <exception cref="ObjectDisposedException">This node has been disposed.</exception>
    public bool RecordingSignals { get { EnsurePlaceholderAccess(); return _recordingSignals; } set { EnsurePendingPropertyMutation(); _recordingSignals = value; } }

    /// <summary>Gets an independent ordered snapshot of archived field names.</summary>
    /// <returns>Names retained from the unavailable compiled role.</returns>
    /// <exception cref="InvalidOperationException">Access occurs outside the attached scene's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node has been disposed.</exception>
    public string[] GetStoredPropertyNames() { EnsurePlaceholderAccess(); return Data.Fields.Keys.ToArray(); }
    /// <summary>Reads a retained field through its compatible typed schema.</summary>
    /// <typeparam name="TValue">The requested type; node references support compatible Node values or string paths.</typeparam>
    /// <param name="name">The retained field name.</param>
    /// <returns>The borrowed resource or independent value/array snapshot.</returns>
    /// <exception cref="KeyNotFoundException">The field is absent.</exception>
    /// <exception cref="InvalidCastException">The requested type is incompatible.</exception>
    /// <exception cref="ArgumentException">The field name is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">The field name is null.</exception>
    /// <exception cref="InvalidOperationException">Access occurs outside the attached scene's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node has been disposed.</exception>
    public TValue GetStoredValue<TValue>(string name)
    {
        EnsurePlaceholderAccess(); ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Data.Fields.TryGetValue(name, out var field)) throw new KeyNotFoundException("No retained field has that name.");
        if (field.Role == ResourceFieldRole.Node && typeof(Node).IsAssignableFrom(typeof(TValue)) && _nodeReferences.TryGetValue(name, out var target))
        {
            if (target is null || target.IsDisposed) return default!;
            if (target is TValue typed) return typed;
            throw new InvalidCastException("The retained node does not satisfy the requested compiled role.");
        }
        if (new DeferredPropertyValue(CapturedField(field)).TryGetValue<TValue>(out var value)) return value;
        throw new InvalidCastException("Retained field type is incompatible with the requested type.");
    }
    /// <summary>Records or edits a typed archived field.</summary>
    /// <typeparam name="TValue">The portable value/resource schema.</typeparam>
    /// <param name="name">The retained field name.</param>
    /// <param name="value">The value to snapshot; resources are borrowed.</param>
    /// <remarks>Existing fields may change schema. New names require RecordingProperties. This changes retained
    /// authoring data, not a CLR member addressed by its name. Pass a typed Node to retain a node reference;
    /// its corresponding string property descriptor edits the path without changing that node schema.</remarks>
    /// <exception cref="InvalidOperationException">Recording, scene owner thread or authoring policy rejects the change.</exception>
    /// <exception cref="ArgumentException">The field name is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">The field name is null.</exception>
    /// <exception cref="InvalidDataException">The field schema, role or archive budget is invalid.</exception>
    /// <exception cref="NotSupportedException">No portable codec exists for the requested value type.</exception>
    /// <exception cref="ObjectDisposedException">This node or a supplied referenced object has been disposed.</exception>
    /// <exception cref="Exception">A synchronous editor-change handler throws after the field change.</exception>
    public void SetStoredValue<TValue>(string name, TValue value)
    {
        EnsurePendingPropertyMutation();
        if (typeof(Node).IsAssignableFrom(typeof(TValue)))
        {
            var target = value as Node;
            if (target is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            using var encoded = new StreamPeerBuffer(); ResourceFileTypes.Codec<string>().Write(encoded, target is null ? null! : GetPathTo(target));
            Data.Put(new(name, ResourceFileTypes.ValueID(typeof(TValue)), ResourceFieldRole.Node, false, encoded.DataArray, [], false), allowSchemaChange: true);
            _nodeReferences[name] = target; NotifyPendingPropertyChanged();
        }
        else SetRecordedField(MissingResourceData.Capture(name, value));
    }

    /// <summary>Records a compiled connection for later restoration of the original endpoint.</summary>
    /// <typeparam name="TPublisher">The original compiled publisher role.</typeparam>
    /// <typeparam name="TSubscriber">The compiled receiver role.</typeparam>
    /// <typeparam name="TBindings">The portable bound payload type.</typeparam>
    /// <param name="binding">The registered typed connection definition.</param>
    /// <param name="subscriber">The actual retained receiver.</param>
    /// <param name="bindings">The bound payload to snapshot.</param>
    /// <returns>A record-removal lease; endpoint disposal also removes it.</returns>
    /// <remarks>This authoring record has no executing callback. RecordingSignals gates new event labels.</remarks>
    /// <exception cref="ArgumentNullException">The definition or subscriber is null.</exception>
    /// <exception cref="ArgumentException">A known subscriber does not satisfy the compiled receiver role.</exception>
    /// <exception cref="InvalidOperationException">Recording, scene owner thread or authoring policy rejects the record.</exception>
    /// <exception cref="ObjectDisposedException">An endpoint has been disposed.</exception>
    /// <exception cref="Exception">A synchronous editor-change handler throws; the new record is rolled back.</exception>
    public IDisposable RecordSceneConnection<TPublisher, TSubscriber, TBindings>(SceneConnectionBinding<TPublisher, TSubscriber, TBindings> binding, Node subscriber, TBindings bindings)
        where TPublisher : Node where TSubscriber : Node
    {
        ArgumentNullException.ThrowIfNull(binding); ArgumentNullException.ThrowIfNull(subscriber);
        EnsurePendingPropertyMutation(); subscriber.EnsureConnectionMutation();
        if (subscriber is not MissingNode && subscriber is not TSubscriber) throw new ArgumentException("The receiver does not match its compiled role.", nameof(subscriber));
        var signal = binding.Schema.Signal; var added = !_signals.Contains(signal);
        if (added && !_recordingSignals) throw new InvalidOperationException("RecordingSignals is required for a new retained event identity.");
        var payload = new StoredPropertyValue<TBindings>(StoredPropertyValue<TBindings>.Snapshot(bindings));
        _signals.Add(signal);
        try { return RetainRecordedSceneConnection(binding.Schema, subscriber, payload); }
        catch { if (added) _signals.Remove(signal); throw; }
    }
    /// <summary>Returns diagnostics explaining the unavailable role and save consequences.</summary>
    /// <returns>Base warnings followed by the current recovery warnings.</returns>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        return _originalScene.Length != 0 ? [.. warnings, $"Scene '{_originalScene}' is unavailable.", "Saving discards the unavailable scene instance and its inherited overrides."]
            : _originalClass.Length != 0 ? [.. warnings, $"Compiled node factory '{_originalClass}' is unavailable.", "Its retained fields can be saved until the original compiled role is available again."]
            : [.. warnings, "This node has no recognized recovery identity."];
    }
    internal void SetRecoveryIdentity(string originalClass, string originalScene) { _originalClass = originalClass; _originalScene = originalScene; }
    internal MissingResourceData.Field RecordedField(string name) { EnsurePlaceholderAccess(); return Data.Fields[name]; }
    internal void RememberSignal(string signal) => _signals.Add(signal);
    internal void CopyRecoveryControls(MissingNode source, bool includeInternal)
    {
        SetRecoveryIdentity(source._originalClass, source._originalScene); _signals.UnionWith(source._signals);
        if (includeInternal) { Data.Recording = source.Data.Recording; _recordingSignals = source._recordingSignals; }
    }

    internal void SetRecordedField(MissingResourceData.Field field)
    {
        EnsurePendingPropertyMutation();
        Node? target = null;
        if (field.Role == ResourceFieldRole.Node)
        {
            var path = new DeferredPropertyValue(field).ReadNodePath(); target = string.IsNullOrEmpty(path) ? null : GetNodeOrNull(path);
        }
        Data.Put(field, allowSchemaChange: true);
        if (field.Role == ResourceFieldRole.Node) _nodeReferences[field.Name] = target; else _nodeReferences.Remove(field.Name);
        NotifyPendingPropertyChanged();
    }
    internal void RestoreRecordedField(MissingResourceData.Field field)
    {
        var recording = Data.Recording; Data.Recording = true;
        try { Data.Put(field, allowSchemaChange: true); }
        finally { Data.Recording = recording; }
    }
    internal void ResolveRecordedNodeReferences(Func<string, Node?>? externalReference = null)
    {
        foreach (var field in Data.Fields.Values)
            if (field.Role == ResourceFieldRole.Node)
            {
                var path = new DeferredPropertyValue(field).ReadNodePath();
                _nodeReferences[field.Name] = string.IsNullOrEmpty(path) ? null : GetNodeOrNull(path) ?? externalReference?.Invoke(path);
            }
    }
    internal MissingResourceData.Field CapturedField(MissingResourceData.Field field)
    {
        if (field.Role != ResourceFieldRole.Node || !_nodeReferences.TryGetValue(field.Name, out var target)) return field;
        using var encoded = new StreamPeerBuffer(); ResourceFileTypes.Codec<string>().Write(encoded, target is null || target.IsDisposed ? null! : GetPathTo(target));
        return field with { Bytes = encoded.DataArray, BigEndian = false };
    }
    internal PropertyDescriptor? BaseDescriptor(string name, bool includeInternal = false) => base.GetPropertyDescriptors().FirstOrDefault(p => p.Name == name && (p.IsStored || includeInternal) && !p.IsReadOnly);
    /// <summary>Releases retained field references and recovery metadata after ordinary node cleanup.</summary>
    /// <param name="disposing">Whether deterministic disposal is running.</param>
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { if (disposing) { Data.Fields.Clear(); _nodeReferences.Clear(); _signals.Clear(); _originalClass = _originalScene = ""; } }
    }
    private static Node CreateMissing() => new MissingNode();
    /// <summary>Provides the internal fresh-node factory for captured recovery roles.</summary>
    /// <returns>A new empty recovery node; original identity is restored separately.</returns>
    protected override Func<Node> CreateSceneInstanceFactory() => CreateMissing;
}
