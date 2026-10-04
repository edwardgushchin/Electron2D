namespace Electron2D;

/// <summary>Selects when a synchronizer reevaluates its peer visibility filters.</summary>
public enum VisibilityUpdateMode
{
    /// <summary>Updates during internal process frames.</summary>
    Idle = 0,
    /// <summary>Updates during internal physics frames.</summary>
    Physics = 1,
    /// <summary>Updates only through explicit calls.</summary>
    None = 2
}
/// <summary>Replicates explicitly typed properties from its authority and controls peer visibility.</summary>
/// <remarks>Root/configuration/factories are authored through the public scene API. Codecs and property tokens
/// remain borrowed. Interval values are monotonic seconds. Lifecycle/configuration prepares bounded state;
/// stable capture/encode/decode/poll reuses it. Attached operations require the scene owner.</remarks>
public class MultiplayerSynchronizer : Node
{
    private string _rootPath = "..";
    private SceneReplicationConfig? _config;
    private double _interval, _deltaInterval;
    private VisibilityUpdateMode _visibilityMode;
    private readonly HashSet<int> _visible = [0];
    private readonly List<Func<int, bool>> _filters = [];
    private Func<int, bool>[] _filterSnapshot = [];
    internal MultiplayerAPI? API;
    internal Node? RootNode;
    internal BoundReplicationProperty[] Bindings = [];
    internal readonly Dictionary<int, ReplicationPeerState> Peers = [];
    private long _configRevision = -1, _lastAlways;
    internal bool AlwaysDue;
    /// <summary>Creates a parent-root synchronizer with public visibility and zero intervals.</summary>
    public MultiplayerSynchronizer() { }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void Mutate() { Check(); EnsureMutable(); if (API is SceneMultiplayer { ReplicationProcessing: true }) throw new InvalidOperationException("Cannot reconfigure while encoding replication state."); }
    /// <summary>Gets or sets the relative target root path.</summary><value>Parent path initially; empty disables synchronization.</value>
    public string RootPath { get { Check(); return _rootPath; } set { Mutate(); ArgumentNullException.ThrowIfNull(value); if (value.Contains('\0')) throw new ArgumentException("Root path contains NUL."); if (_rootPath == value) return; Stop(); _rootPath = value; Rebind(); UpdateConfigurationWarnings(); } }
    /// <summary>Gets or sets the borrowed ordered property configuration.</summary><value>Null initially, providing no property updates.</value>
    public SceneReplicationConfig? ReplicationConfig { get { Check(); return _config; } set { Mutate(); if (ReferenceEquals(_config, value)) return; if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value); _config = value; _configRevision = -1; Prepare(); } }
    /// <summary>Gets or sets the interval between Always updates in seconds.</summary><value>Zero initially; zero sends each network process frame.</value>
    public double ReplicationInterval { get { Check(); return _interval; } set { Mutate(); Interval(value); _interval = value; } }
    /// <summary>Gets or sets the minimum interval between OnChange updates per peer in seconds.</summary><value>Zero initially; zero checks each network process frame.</value>
    public double DeltaInterval { get { Check(); return _deltaInterval; } set { Mutate(); Interval(value); _deltaInterval = value; } }
    private static void Interval(double value) { if (!double.IsFinite(value) || value < 0 || value > long.MaxValue / 1000d) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <summary>Gets or sets whether every peer is visible in addition to explicit peer entries.</summary><value>True initially. False allows explicit positive peer visibility.</value>
    public bool PublicVisibility { get => GetVisibilityFor(0); set => SetVisibilityFor(0, value); }
    /// <summary>Gets or sets the phase for automatic visibility filter refresh.</summary><value>Idle initially; processing is enabled only when filters and a valid root exist.</value>
    public VisibilityUpdateMode VisibilityUpdateMode { get { Check(); return _visibilityMode; } set { Mutate(); if ((uint)value > 2) throw new ArgumentOutOfRangeException(nameof(value)); _visibilityMode = value; UpdateProcessing(); } }
    /// <summary>Occurs after an Always state has been applied on a receiving peer.</summary>
    public event Action? Synchronized;
    /// <summary>Occurs after changed property state has been applied on a receiving peer.</summary>
    public event Action? DeltaSynchronized;
    /// <summary>Occurs after explicit or automatic visibility reevaluation.</summary>
    public event Action<int>? VisibilityChanged;
    /// <summary>Adds a unique conjunctive peer predicate.</summary><param name="filter">Direct peer-ID predicate, called on the owner thread.</param>
    public void AddVisibilityFilter(Func<int, bool> filter) { Mutate(); ArgumentNullException.ThrowIfNull(filter); if (!_filters.Contains(filter)) { _filters.Add(filter); _filterSnapshot = _filters.ToArray(); UpdateProcessing(); } }
    /// <summary>Removes a visibility predicate if present.</summary><param name="filter">Previously added predicate.</param>
    public void RemoveVisibilityFilter(Func<int, bool> filter) { Mutate(); ArgumentNullException.ThrowIfNull(filter); if (_filters.Remove(filter)) { _filterSnapshot = _filters.ToArray(); UpdateProcessing(); } }
    /// <summary>Reports explicit membership, independently of public visibility and filters.</summary><param name="peer">Zero selects public membership, otherwise a positive peer.</param><returns>True if the explicit entry exists.</returns>
    public bool GetVisibilityFor(int peer) { Check(); if (peer < 0) throw new ArgumentOutOfRangeException(nameof(peer)); return _visible.Contains(peer); }
    /// <summary>Changes explicit peer/public membership and reevaluates visibility.</summary><param name="peer">Zero means public.</param><param name="visible">New explicit membership.</param>
    public void SetVisibilityFor(int peer, bool visible) { Mutate(); if (peer < 0) throw new ArgumentOutOfRangeException(nameof(peer)); if (visible ? !_visible.Add(peer) : !_visible.Remove(peer)) return; UpdateVisibility(peer); }
    /// <summary>Reevaluates one or all peers and delivers the visibility event.</summary><param name="forPeer">Zero reevaluates all admitted peers.</param>
    public void UpdateVisibility(int forPeer = 0) { Check(); if (forPeer < 0) throw new ArgumentOutOfRangeException(nameof(forPeer)); if (API is SceneMultiplayer api) api.UpdateReplicationVisibility(this, forPeer); MultiplayerAPI.Emit(VisibilityChanged, forPeer); }
    internal bool IsVisibleTo(int peer) { var snapshot = _filterSnapshot; foreach (var filter in snapshot) if (!filter(peer)) return false; return _visible.Contains(0) || _visible.Contains(peer); }
    private void UpdateProcessing() { SetInternalProcessing(RootNode is not null && _filters.Count > 0 && _visibilityMode == VisibilityUpdateMode.Idle, RootNode is not null && _filters.Count > 0 && _visibilityMode == VisibilityUpdateMode.Physics); }
    internal void Rebind()
    {
        if (Tree is null || _rootPath.Length == 0) { Stop(); return; }
        var root = GetNodeOrNull(_rootPath); var api = Multiplayer;
        if (ReferenceEquals(API, api) && ReferenceEquals(RootNode, root)) return; Stop(); RootNode = root; if (root is not null) { API = api; api.ObjectConfigurationAdd(root, this); Prepare(); }
        UpdateProcessing();
    }
    private void Stop() { var api = API; var root = RootNode; API = null; RootNode = null; Bindings = []; Peers.Clear(); _configRevision = -1; if (api is not null && !api.IsDisposed && root is not null) api.ObjectConfigurationRemove(root, this); }
    internal void Prepare()
    {
        if (RootNode is null) return; var revision = _config?.ChangeRevision ?? 0; if (_configRevision == revision) return; var entries = _config?.Snapshot() ?? [];
        long budget = 0; foreach (var entry in entries) budget += (long)entry.Property.MaxEncodedBytes * 2; if (budget > 67108864 || entries.Length > ushort.MaxValue) throw new ArgumentException("Synchronizer preparation exceeds its 64 MiB/count budget.");
        var bindings = new BoundReplicationProperty[entries.Length]; for (var i = 0; i < entries.Length; i++) { var entry = entries[i]; var bound = entry.Property.Bind(RootNode); bound.Index = i; bound.Spawn = entry.Spawn; bound.Mode = entry.Mode; bindings[i] = bound; }
        Bindings = bindings; _configRevision = revision; Peers.Clear(); _lastAlways = 0;
        if (API is SceneMultiplayer api) api.PrepareReplicationPeers(this);
    }
    internal void PrepareDetached(Node root) { RootNode = root; _configRevision = -1; Prepare(); }
    internal void CaptureFrame(long now) { Prepare(); AlwaysDue = now - _lastAlways >= _interval * System.Diagnostics.Stopwatch.Frequency; if (AlwaysDue) _lastAlways = now; foreach (var binding in Bindings) { if (binding.Mode == ReplicationMode.OnChange) binding.Watch(); else if (binding.Mode == ReplicationMode.Always && AlwaysDue) binding.Capture(); } }
    internal ReplicationPeerState PeerState(int peer) { if (!Peers.TryGetValue(peer, out var state)) { state = new ReplicationPeerState(Bindings.Length); Peers.Add(peer, state); } return state; }
    internal bool DeltaDue(ReplicationPeerState peer, long now) => now - peer.LastDelta >= _deltaInterval * System.Diagnostics.Stopwatch.Frequency;
    internal void EmitState(bool delta) { List<Exception>? errors = null; foreach (var subscriber in Delegate.EnumerateInvocationList(delta ? DeltaSynchronized : Synchronized)) try { subscriber(); } catch (Exception e) { (errors ??= []).Add(e); } if (errors is not null) throw new AggregateException(errors); }
    /// <inheritdoc />
    public override void SetMultiplayerAuthority(int id, bool recursive = true) { Mutate(); Stop(); base.SetMultiplayerAuthority(id, recursive); Rebind(); }
    /// <inheritdoc />
    protected override void OnEnterTree() { base.OnEnterTree(); Rebind(); }
    /// <inheritdoc />
    protected override void OnExitTree() { try { Stop(); } finally { base.OnExitTree(); } }
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if (what is NotificationInternalProcess or NotificationInternalPhysicsProcess && Tree is not null) UpdateVisibility(); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(MultiplayerSynchronizer) ? CreateInstance : base.CreateSceneInstanceFactory();
    private static Node CreateInstance() => new MultiplayerSynchronizer();
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings() => _rootPath.Length == 0 || GetNodeOrNull(_rootPath) is null ? ["A valid RootPath is required for property synchronization."] : base.GetConfigurationWarnings();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<MultiplayerSynchronizer, string>(nameof(RootPath), n => n.RootPath, (n, v) => n.RootPath = v, _ => "..", stored: true);
        yield return new PropertyDescriptor<MultiplayerSynchronizer, SceneReplicationConfig?>(nameof(ReplicationConfig), n => n.ReplicationConfig, (n, v) => n.ReplicationConfig = v, _ => null, stored: true);
        yield return new PropertyDescriptor<MultiplayerSynchronizer, double>(nameof(ReplicationInterval), n => n.ReplicationInterval, (n, v) => n.ReplicationInterval = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<MultiplayerSynchronizer, double>(nameof(DeltaInterval), n => n.DeltaInterval, (n, v) => n.DeltaInterval = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<MultiplayerSynchronizer, bool>(nameof(PublicVisibility), n => n.PublicVisibility, (n, v) => n.PublicVisibility = v, _ => true, stored: true);
        yield return new PropertyDescriptor<MultiplayerSynchronizer, VisibilityUpdateMode>(nameof(VisibilityUpdateMode), n => n.VisibilityUpdateMode, (n, v) => n.VisibilityUpdateMode = v, _ => VisibilityUpdateMode.Idle, stored: true);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { Stop(); _filterSnapshot = []; _filters.Clear(); _visible.Clear(); Synchronized = null; DeltaSynchronized = null; VisibilityChanged = null; } base.Dispose(disposing); }
}
internal sealed class ReplicationPeerState(int count) { internal readonly uint[] Sent = new uint[count]; internal long LastDelta; internal uint LastSequence; internal bool HasSequence; }
