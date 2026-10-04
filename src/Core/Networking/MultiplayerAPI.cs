namespace Electron2D;

/// <summary>Defines typed scene multiplayer dispatch, peer queries, configuration and connection events.</summary>
/// <remarks>Managed consumers override the public typed hooks directly. Calls/disposal require the constructing thread.
/// The API borrows its transport; SceneTree owns only default interfaces it creates. Applications dispose supplied APIs.</remarks>
public abstract class MultiplayerAPI : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private static Func<MultiplayerAPI> _default = static () => new SceneMultiplayer();
    private SceneTree? _tree;
    private string? _branch;
    /// <summary>Creates an interface on the current owner thread.</summary>
    protected MultiplayerAPI() { }
    /// <summary>Validates owner thread and lifetime.</summary>
    protected void CheckMultiplayer() { ThrowIfDisposed(); if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Multiplayer requires its constructing thread."); }
    /// <summary>Creates a caller-owned instance through the typed default factory.</summary><returns>A live interface owned by the calling thread.</returns>
    public static MultiplayerAPI CreateDefaultInterface() { var result = _default() ?? throw new InvalidOperationException("Default multiplayer factory returned null."); result.CheckMultiplayer(); return result; }
    /// <summary>Gets the currently configured typed default factory.</summary><returns>The shared factory delegate.</returns>
    public static Func<MultiplayerAPI> GetDefaultInterface() => _default;
    /// <summary>Sets the default factory for future trees/interfaces.</summary><param name="factory">Typed constructor; existing interfaces are unaffected.</param>
    public static void SetDefaultInterface(Func<MultiplayerAPI> factory) { ArgumentNullException.ThrowIfNull(factory); _default = factory; }
    /// <summary>Gets or replaces the borrowed connecting/connected transport.</summary>
    public abstract MultiplayerPeer? MultiplayerPeer { get; set; }
    /// <summary>Advances transport and synchronous scene messages.</summary>
    public abstract void Poll();
    /// <summary>Gets the local transport identity.</summary><returns>Zero without a peer; server identity is one.</returns>
    public abstract int GetUniqueID();
    /// <summary>Returns a caller-owned snapshot of admitted remote identities.</summary><returns>Connected peers, excluding authenticating peers.</returns>
    public abstract int[] GetPeers();
    /// <summary>Gets the sender of the currently executing message/local RPC.</summary><returns>Zero outside synchronous dispatch.</returns>
    public abstract int GetRemoteSenderID();
    /// <summary>Reports whether a transport is configured.</summary><returns>True when MultiplayerPeer is nonnull.</returns>
    public bool HasMultiplayerPeer() { CheckMultiplayer(); return MultiplayerPeer is not null; }
    /// <summary>Reports whether the local multiplayer identity is the server.</summary><returns>True when the unique ID equals one.</returns>
    public bool IsServer() { CheckMultiplayer(); return GetUniqueID() == 1; }
    /// <summary>Sends a direct typed Node method invocation.</summary><typeparam name="TNode">Receiver type.</typeparam><typeparam name="T">Concrete arguments.</typeparam><param name="peer">Broadcast, positive target or negative exclusion.</param><param name="node">Local matching scene node.</param><param name="method">Configured immutable method token.</param><param name="arguments">Concrete arguments to encode.</param>
    public abstract void RPC<TNode, T>(int peer, TNode node, RPCMethod<TNode, T> method, T arguments) where TNode : Node;
    /// <summary>Configures a scene-root path through a typed overload.</summary><param name="rootPath">Absolute scene root path, or empty to clear it.</param>
    public abstract void ObjectConfigurationAdd(string rootPath);
    /// <summary>Removes the currently matching root configuration.</summary><param name="rootPath">The configured root path.</param>
    public abstract void ObjectConfigurationRemove(string rootPath);
    /// <summary>Registers an authored spawn object and its owning spawner.</summary><param name="node">Local spawn node, possibly before insertion.</param><param name="spawner">Authoritative typed configuration.</param>
    public abstract void ObjectConfigurationAdd(Node node, MultiplayerSpawner spawner);
    /// <summary>Removes a local spawned object and its matching configuration.</summary><param name="node">Tracked node.</param><param name="spawner">Matching spawner.</param>
    public abstract void ObjectConfigurationRemove(Node node, MultiplayerSpawner spawner);
    /// <summary>Registers a synchronized root and its typed configuration owner.</summary><param name="node">Root node.</param><param name="synchronizer">Attached synchronization component.</param>
    public abstract void ObjectConfigurationAdd(Node node, MultiplayerSynchronizer synchronizer);
    /// <summary>Removes a synchronized root/configuration pair.</summary><param name="node">Root node.</param><param name="synchronizer">Matching component.</param>
    public abstract void ObjectConfigurationRemove(Node node, MultiplayerSynchronizer synchronizer);
    /// <summary>Occurs when a client is admitted by the server.</summary>
    public event Action? ConnectedToServer;
    /// <summary>Occurs when transport connection establishment fails.</summary>
    public event Action? ConnectionFailed;
    /// <summary>Occurs when an admitted peer is committed.</summary>
    public event Action<int>? PeerConnected;
    /// <summary>Occurs after an admitted peer is removed.</summary>
    public event Action<int>? PeerDisconnected;
    /// <summary>Occurs when an established client loses its server.</summary>
    public event Action? ServerDisconnected;
    /// <summary>Delivers all client admission subscribers; failures aggregate.</summary>
    protected void EmitConnectedToServer() { CheckMultiplayer(); Emit(ConnectedToServer); }
    /// <summary>Delivers all establishment failure subscribers; failures aggregate.</summary>
    protected void EmitConnectionFailed() { CheckMultiplayer(); Emit(ConnectionFailed); }
    /// <summary>Delivers all admitted-peer subscribers.</summary><param name="id">Committed identity.</param>
    protected void EmitPeerConnected(int id) { CheckMultiplayer(); Emit(PeerConnected, id); }
    /// <summary>Delivers all removed-peer subscribers.</summary><param name="id">Removed identity.</param>
    protected void EmitPeerDisconnected(int id) { CheckMultiplayer(); Emit(PeerDisconnected, id); }
    /// <summary>Delivers all server loss subscribers.</summary>
    protected void EmitServerDisconnected() { CheckMultiplayer(); Emit(ServerDisconnected); }
    private static void Emit(Action? callbacks) { List<Exception>? errors = null; foreach (var call in Delegate.EnumerateInvocationList(callbacks)) try { call(); } catch (Exception e) { (errors ??= []).Add(e); } if (errors is not null) throw new AggregateException(errors); }
    internal static void Emit(Action<int>? callbacks, int id) { List<Exception>? errors = null; foreach (var call in Delegate.EnumerateInvocationList(callbacks)) try { call(id); } catch (Exception e) { (errors ??= []).Add(e); } if (errors is not null) throw new AggregateException(errors); }
    internal SceneTree? AttachedTree => _tree;
    internal virtual void Attach(SceneTree tree, string branch) { CheckMultiplayer(); if (_tree is not null) throw new InvalidOperationException("A multiplayer interface is already assigned to a scene branch."); _tree = tree; _branch = branch; try { ObjectConfigurationAdd(branch); } catch { _tree = null; _branch = null; throw; } }
    internal virtual void ValidateDetachment() => CheckMultiplayer();
    internal virtual void Detach() { var branch = _branch; _tree = null; _branch = null; if (branch is not null) ObjectConfigurationRemove(branch); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    { foreach (var property in base.GetPropertyDescriptors()) yield return property; yield return new PropertyDescriptor<MultiplayerAPI, MultiplayerPeer?>(nameof(MultiplayerPeer), p => p.MultiplayerPeer, (p, v) => p.MultiplayerPeer = v, _ => null); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); CheckMultiplayer(); if (_tree is not null) throw new InvalidOperationException("Detach the assigned multiplayer interface before disposal."); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { ConnectedToServer = null; ConnectionFailed = null; PeerConnected = null; PeerDisconnected = null; ServerDisconnected = null; } base.Dispose(disposing); }
}
