namespace Electron2D;

/// <summary>Replicates authored PackedScene children or explicit typed custom factories from its authority.</summary>
/// <remarks>Registered scenes/factory tokens are borrowed. Spawn construction and membership are cold work.
/// Remote ownership belongs to the configured parent; Spawned/Despawned run only on remote participants.</remarks>
public class MultiplayerSpawner : Node
{
    private readonly List<PackedScene> _scenes = [];
    private string _spawnPath = "";
    private uint _limit;
    private SpawnFactory? _factory;
    private Node? _parent;
    private readonly Action<Node, Node> _onAdded;
    private readonly Dictionary<Node, SpawnedNodeRecord> _tracked = [];
    internal MultiplayerAPI? API;
    private bool _creating;
    /// <summary>Creates an unconfigured spawner.</summary>
    public MultiplayerSpawner() { _onAdded = (_, child) => AutoTrack(child); }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void Mutate() { Check(); EnsureMutable(); if (_creating) throw new InvalidOperationException("Cannot reconfigure during spawn encoding/construction."); }
    /// <summary>Gets or sets the path to the direct spawn parent.</summary><value>Empty initially, disabling spawning.</value>
    public string SpawnPath { get { Check(); return _spawnPath; } set { Mutate(); ArgumentNullException.ThrowIfNull(value); if (value.Contains('\0')) throw new ArgumentException("Spawn path contains NUL."); _spawnPath = value; Rebind(); UpdateConfigurationWarnings(); } }
    /// <summary>Gets or sets the maximum tracked node count.</summary><value>Zero initially means unlimited; lowering it leaves existing nodes alive.</value>
    public uint SpawnLimit { get { Check(); return _limit; } set { Mutate(); _limit = value; } }
    /// <summary>Gets or sets immutable borrowed custom factory/codec configuration.</summary><value>Null initially.</value>
    public SpawnFactory? SpawnFunction { get { Check(); return _factory; } set { Mutate(); if (_creating) throw new InvalidOperationException("Cannot replace a factory during spawn construction."); _factory = value; } }
    /// <summary>Occurs after a remote node has entered and readied.</summary>
    public event Action<Node>? Spawned;
    /// <summary>Occurs when a remote instance is removed by its authority.</summary>
    public event Action<Node>? Despawned;
    /// <summary>Registers a borrowed in-memory PackedScene template for automatic direct-child replication.</summary><param name="scene">Template; ordered indices must match across peers.</param><remarks>Instantiate this same template and add its root beneath SpawnPath for automatic local spawning. File scene loading remains the resource loader's separate contract.</remarks>
    public void AddSpawnableScene(PackedScene scene) { Mutate(); ArgumentNullException.ThrowIfNull(scene); ObjectDisposedException.ThrowIf(scene.IsDisposed, scene); if (_scenes.Count >= 255) throw new InvalidOperationException("At most 255 ordered spawnable scenes are supported."); _scenes.Add(scene); }
    /// <summary>Clears template registration without despawning existing nodes.</summary>
    public void ClearSpawnableScenes() { Mutate(); _scenes.Clear(); }
    /// <summary>Gets an ordered borrowed scene template.</summary><param name="index">Registered position.</param><returns>The original resource.</returns>
    public PackedScene GetSpawnableScene(int index) { Check(); if ((uint)index >= (uint)_scenes.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _scenes[index]; }
    /// <summary>Gets the registered template count.</summary><returns>Zero through 255.</returns>
    public int GetSpawnableSceneCount() { Check(); return _scenes.Count; }
    /// <summary>Creates a custom node using default typed arguments, then adds it beneath SpawnPath.</summary><returns>The locally parent-owned node.</returns>
    public Node Spawn() { ValidateSpawn(); var factory = _factory ?? throw new InvalidOperationException("A custom spawn factory is required."); _creating = true; try { return SpawnEncoded(factory, factory.DefaultArguments()); } finally { _creating = false; } }
    /// <summary>Creates a custom node with a concrete typed argument model.</summary><typeparam name="T">Factory argument type.</typeparam><param name="data">Arguments copied through the configured codec.</param><returns>The locally parent-owned node.</returns>
    public Node Spawn<T>(T data) { ValidateSpawn(); if (_factory is not SpawnFactory<T> factory) throw new ArgumentException("Spawn argument type does not match the configured factory.", nameof(data)); _creating = true; try { return SpawnEncoded(factory, factory.Encode(data)); } finally { _creating = false; } }
    private void ValidateSpawn() { Check(); if (_creating || Tree is null || !IsMultiplayerAuthority() || _parent is null || API is null || !API.HasMultiplayerPeer()) throw new InvalidOperationException("An authoritative attached spawner with a valid parent/factory is required."); Limit(); }
    private Node SpawnEncoded(SpawnFactory factory, byte[] arguments)
    {
        Check(); Node? node = null;
        try { node = factory.Instantiate(arguments); if (Tree is null || _parent is null || _parent.IsDisposed || API is null || API.IsDisposed || !IsMultiplayerAuthority()) throw new InvalidOperationException("Spawn scene lifetime or authority changed during construction."); var original = node.Name; var ordinal = 2; while (_parent!.HasNode(node.Name)) node.Name = original + ordinal++; var record = Track(node, -1, factory.ID, arguments); _parent!.AddChild(node); if (API is SceneMultiplayer api) api.SpawnReady(record); return node; }
        catch { if (node is not null && node.Parent is null && !node.IsDisposed) { if (_tracked.TryGetValue(node, out var record)) Untrack(record); node.Dispose(); } throw; }
        finally { _creating = false; }
    }
    internal void Limit() { if (_limit > 0 && _tracked.Count >= _limit) throw new InvalidOperationException("Spawner node limit has been reached."); }
    private void AutoTrack(Node node)
    {
        if (_creating || _tracked.ContainsKey(node) || API is null || !IsMultiplayerAuthority()) return; var index = _scenes.FindIndex(scene => ReferenceEquals(scene, node.SpawnSceneIdentity)); if (index < 0) return; Limit(); var record = Track(node, index, 0, []); if (API is SceneMultiplayer api) api.SpawnReady(record);
    }
    private SpawnedNodeRecord Track(Node node, int scene, uint factory, byte[] arguments)
    {
        var record = new SpawnedNodeRecord(this, node, scene, factory, arguments); _tracked.Add(node, record); record.Exit = _ => Untrack(record); node.TreeExiting += record.Exit;
        try { API!.ObjectConfigurationAdd(node, this); return record; } catch { node.TreeExiting -= record.Exit; _tracked.Remove(node); throw; }
    }
    private void Untrack(SpawnedNodeRecord record) { if (!_tracked.Remove(record.Node)) return; record.Node.TreeExiting -= record.Exit; if (API is not null && !API.IsDisposed) API.ObjectConfigurationRemove(record.Node, this); }
    internal SpawnedNodeRecord GetRecord(Node node) => _tracked.TryGetValue(node, out var record) ? record : throw new KeyNotFoundException("Spawner node is not tracked.");
    internal Node InstantiateRemote(int scene, uint factory, ReadOnlySpan<byte> arguments)
    { Limit(); return scene >= 0 ? GetSpawnableScene(scene).Instantiate() : _factory is { } configured && configured.ID == factory ? configured.Instantiate(arguments) : throw new InvalidDataException("Remote spawn factory identity does not match."); }
    internal SpawnedNodeRecord TrackRemote(Node node, int source, uint id, int scene, uint factory, byte[] arguments)
    { var record = new SpawnedNodeRecord(this, node, scene, factory, arguments) { RemoteSource = source, ID = id }; _tracked.Add(node, record); record.Exit = _ => _tracked.Remove(node); node.TreeExiting += record.Exit; return record; }
    internal Node SpawnParent => _parent ?? throw new InvalidOperationException("SpawnPath does not select a valid parent.");
    internal void EmitSpawned(Node node) => Emit(Spawned, node);
    internal void EmitDespawned(Node node) => Emit(Despawned, node);
    private static void Emit(Action<Node>? handlers, Node node) { List<Exception>? errors = null; foreach (var callback in Delegate.EnumerateInvocationList(handlers)) try { callback(node); } catch (Exception e) { (errors ??= []).Add(e); } if (errors is not null) throw new AggregateException(errors); }
    internal void Rebind()
    {
        if (Tree is null) return; var parent = _spawnPath.Length == 0 ? null : GetNodeOrNull(_spawnPath); var api = Multiplayer; if (ReferenceEquals(_parent, parent) && ReferenceEquals(API, api)) return;
        var previous = API; if (_parent is not null) _parent.ChildEnteredTree -= _onAdded; foreach (var record in _tracked.Values.ToArray()) { if (record.RemoteSource == 0 && previous is not null && !previous.IsDisposed) previous.ObjectConfigurationRemove(record.Node, this); }
        _parent = parent; API = api; if (parent is not null) parent.ChildEnteredTree += _onAdded; foreach (var record in _tracked.Values.ToArray()) if (record.RemoteSource == 0) api.ObjectConfigurationAdd(record.Node, this);
    }
    /// <inheritdoc />
    protected override void OnEnterTree() { base.OnEnterTree(); Rebind(); }
    /// <inheritdoc />
    protected override void OnExitTree() { try { if (_parent is not null) _parent.ChildEnteredTree -= _onAdded; foreach (var record in _tracked.Values.ToArray()) Untrack(record); _parent = null; API = null; } finally { base.OnExitTree(); } }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(MultiplayerSpawner) ? CreateInstance : base.CreateSceneInstanceFactory();
    private static Node CreateInstance() => new MultiplayerSpawner();
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings() => _spawnPath.Length == 0 || GetNodeOrNull(_spawnPath) is null ? ["A valid SpawnPath is required for multiplayer spawning."] : base.GetConfigurationWarnings();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<MultiplayerSpawner, string>(nameof(SpawnPath), n => n.SpawnPath, (n, v) => n.SpawnPath = v, _ => "", stored: true);
        yield return new PropertyDescriptor<MultiplayerSpawner, uint>(nameof(SpawnLimit), n => n.SpawnLimit, (n, v) => n.SpawnLimit = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<MultiplayerSpawner, SpawnFactory?>(nameof(SpawnFunction), n => n.SpawnFunction, (n, v) => n.SpawnFunction = v, _ => null);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _scenes.Clear(); Spawned = null; Despawned = null; } base.Dispose(disposing); }
}
internal sealed class SpawnedNodeRecord(MultiplayerSpawner spawner, Node node, int scene, uint factory, byte[] arguments)
{
    internal readonly MultiplayerSpawner Spawner = spawner;
    internal readonly Node Node = node;
    internal readonly int Scene = scene;
    internal readonly uint Factory = factory;
    internal readonly byte[] Arguments = arguments;
    internal uint ID;
    internal int RemoteSource;
    internal Action<Node>? Exit;
    internal readonly HashSet<int> Visible = [];
    internal bool Ready => Node.Tree is not null && Node.IsNodeReady;
}
