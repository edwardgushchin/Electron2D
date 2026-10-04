namespace Electron2D;

/// <summary>Selects whether a configured property sends periodic, change-only or no ongoing updates.</summary>
public enum ReplicationMode
{
    /// <summary>Does not send ongoing updates; spawn state remains independently configurable.</summary>
    Never = 0,
    /// <summary>Sends at each replication interval.</summary>
    Always = 1,
    /// <summary>Sends changed encoded values at the delta interval.</summary>
    OnChange = 2
}
/// <summary>Stores an ordered, copied list of immutable typed property descriptors and replication policies.</summary>
/// <remarks>New properties spawn and replicate Always. Tokens/codecs remain shared immutable application configuration.
/// List/policy changes emit Changed after commit. Preparation/copy allocates; attached consumers reprepare on change.</remarks>
public class SceneReplicationConfig : Resource
{
    private readonly object _gate = new();
    private List<ReplicationConfigEntry> _properties = [];
    /// <summary>Creates an empty property configuration.</summary>
    public SceneReplicationConfig() { }
    /// <summary>Adds a unique descriptor at an index or appends for any negative index.</summary><param name="property">Shared immutable token.</param><param name="index">Insertion position; negative appends.</param>
    public void AddProperty(ReplicationProperty property, int index = -1)
    {
        ArgumentNullException.ThrowIfNull(property); lock (_gate) { ThrowIfDisposed(); if (_properties.Any(p => p.Property.ID == property.ID)) throw new ArgumentException("Property IDs must be unique.", nameof(property)); if (index > _properties.Count) throw new ArgumentOutOfRangeException(nameof(index)); _properties.Insert(index < 0 ? _properties.Count : index, new(property, true, ReplicationMode.Always)); }
        EmitChanged();
    }
    /// <summary>Removes a descriptor if present; missing descriptors do nothing.</summary><param name="property">Descriptor identity.</param>
    public void RemoveProperty(ReplicationProperty property) { ArgumentNullException.ThrowIfNull(property); bool changed; lock (_gate) { ThrowIfDisposed(); changed = _properties.RemoveAll(p => ReferenceEquals(p.Property, property)) > 0; } if (changed) EmitChanged(); }
    /// <summary>Returns a copied ordered descriptor snapshot.</summary><returns>Shared immutable tokens in configuration order.</returns>
    public ReplicationProperty[] GetProperties() { lock (_gate) { ThrowIfDisposed(); return _properties.Select(p => p.Property).ToArray(); } }
    /// <summary>Tests descriptor membership by immutable token identity.</summary><param name="property">Descriptor to query.</param><returns>True when present.</returns>
    public bool HasProperty(ReplicationProperty property) => PropertyGetIndex(property) >= 0;
    /// <summary>Finds descriptor order.</summary><param name="property">Descriptor to query.</param><returns>Index or minus one when absent.</returns>
    public int PropertyGetIndex(ReplicationProperty property) { ArgumentNullException.ThrowIfNull(property); lock (_gate) { ThrowIfDisposed(); return _properties.FindIndex(p => ReferenceEquals(p.Property, property)); } }
    private int Index(ReplicationProperty property) { var index = PropertyGetIndex(property); return index >= 0 ? index : throw new KeyNotFoundException("Replication property is not configured."); }
    /// <summary>Reports whether the property is included in initial spawn state.</summary><param name="property">Configured descriptor.</param><returns>True initially.</returns>
    public bool PropertyGetSpawn(ReplicationProperty property) { lock (_gate) return _properties[Index(property)].Spawn; }
    /// <summary>Sets initial-spawn inclusion after validating membership.</summary><param name="property">Configured descriptor.</param><param name="enabled">Whether to include it.</param>
    public void PropertySetSpawn(ReplicationProperty property, bool enabled) { lock (_gate) { var index = Index(property); if (_properties[index].Spawn == enabled) return; _properties[index] = _properties[index] with { Spawn = enabled }; } EmitChanged(); }
    /// <summary>Gets ongoing delivery policy.</summary><param name="property">Configured descriptor.</param><returns>Always initially.</returns>
    public ReplicationMode PropertyGetReplicationMode(ReplicationProperty property) { lock (_gate) return _properties[Index(property)].Mode; }
    /// <summary>Sets Never, Always or OnChange.</summary><param name="property">Configured descriptor.</param><param name="mode">Ongoing delivery policy.</param>
    public void PropertySetReplicationMode(ReplicationProperty property, ReplicationMode mode) { if ((uint)mode > 2) throw new ArgumentOutOfRangeException(nameof(mode)); lock (_gate) { var index = Index(property); if (_properties[index].Mode == mode) return; _properties[index] = _properties[index] with { Mode = mode }; } EmitChanged(); }
    /// <summary>Reports whether ongoing mode is Always.</summary><param name="property">Configured descriptor.</param><returns>True for Always only.</returns>
    public bool PropertyGetSync(ReplicationProperty property) => PropertyGetReplicationMode(property) == ReplicationMode.Always;
    /// <summary>Enables Always, or disables it if currently Always.</summary><param name="property">Configured descriptor.</param><param name="enabled">Requested legacy sync selection.</param>
    public void PropertySetSync(ReplicationProperty property, bool enabled) { if (enabled || PropertyGetSync(property)) PropertySetReplicationMode(property, enabled ? ReplicationMode.Always : ReplicationMode.Never); }
    /// <summary>Reports whether ongoing mode is OnChange.</summary><param name="property">Configured descriptor.</param><returns>True for OnChange only.</returns>
    public bool PropertyGetWatch(ReplicationProperty property) => PropertyGetReplicationMode(property) == ReplicationMode.OnChange;
    /// <summary>Enables OnChange, or disables it if currently OnChange.</summary><param name="property">Configured descriptor.</param><param name="enabled">Requested legacy watch selection.</param>
    public void PropertySetWatch(ReplicationProperty property, bool enabled) { if (enabled || PropertyGetWatch(property)) PropertySetReplicationMode(property, enabled ? ReplicationMode.OnChange : ReplicationMode.Never); }
    internal ReplicationConfigEntry[] Snapshot() { lock (_gate) { ThrowIfDisposed(); return _properties.ToArray(); } }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SceneReplicationConfig();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { var entries = Snapshot(); var copy = (SceneReplicationConfig)target; lock (copy._gate) { copy.ThrowIfDisposed(); copy._properties = [.. entries]; } }
    /// <inheritdoc />
    protected override void OnResetState() { lock (_gate) _properties.Clear(); EmitChanged(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) _properties.Clear(); base.Dispose(disposing); }
}
internal readonly record struct ReplicationConfigEntry(ReplicationProperty Property, bool Spawn, ReplicationMode Mode);
