namespace Electron2D;

public partial class Node
{
    internal PackedScene? SpawnSceneIdentity;
    private int _multiplayerAuthority = 1;
    private Dictionary<uint, RPCRegistration>? _rpcMethods;
    private HashSet<uint>? _removedRPCMethods;
    private Dictionary<uint, RPCOptions>? _rpcPolicies;
    /// <summary>Gets the interface assigned to the nearest containing scene branch.</summary><value>The default interface or most specific custom override; detached access fails.</value>
    public MultiplayerAPI Multiplayer { get { ThrowIfDisposed(); var tree = Tree ?? throw new InvalidOperationException("Multiplayer requires an attached scene node."); return tree.GetMultiplayer(this); } }
    /// <summary>Gets the local node's configured multiplayer authority.</summary><returns>Server identity one initially.</returns>
    public int GetMultiplayerAuthority() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _multiplayerAuthority; }
    /// <summary>Sets a positive authority identity, optionally recursively for current descendants.</summary><param name="id">Positive peer identity.</param><param name="recursive">True applies to current descendants; later children retain their own default/configuration.</param><remarks>This local configuration does not replicate itself; participants must agree separately.</remarks>
    public virtual void SetMultiplayerAuthority(int id, bool recursive = true) { EnsureMutable(); if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id)); _multiplayerAuthority = id; if (recursive) foreach (var child in _children) child.SetMultiplayerAuthority(id, true); }
    /// <summary>Reports whether this node's authority matches its assigned interface identity.</summary><returns>False for detached nodes.</returns>
    public bool IsMultiplayerAuthority() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return Tree is not null && Multiplayer.GetUniqueID() == _multiplayerAuthority; }
    /// <summary>Configures a typed callable and its invocation policy.</summary><param name="method">Immutable receiver/codec token, unique per node by ID.</param><param name="options">Policy; null removes the configuration and suppresses a declared class default.</param><remarks>Instance policies override compiled Script declarations. Tokens/closures are borrowed configuration.
    /// Policies on an active live Script declaration follow its wire ID across replacement without retaining the old handler.
    /// Explicit standalone and CLR-class method configurations retain their token identity.
    /// Configure runtime-only overrides in the typed factory/constructor when they must survive scene reconstruction.</remarks>
    public void RPCConfig(RPCMethod method, RPCOptions? options = null)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(method); if (!method.Accepts(this)) throw new ArgumentException("RPC receiver type does not match this node.", nameof(method));
        if (options is null) { _rpcMethods?.Remove(method.ID); _rpcPolicies?.Remove(method.ID); (_removedRPCMethods ??= []).Add(method.ID); return; }
        if (TryGetRPC(method.ID, out var existing) && !ReferenceEquals(existing.Method, method)) throw new ArgumentException("RPC ID is already assigned to another method token.", nameof(method));
        if (LiveScriptRegistration is { } source && Script.TryGetBindingRPC(GetType(), source.Type, method.ID, out var declared) && ReferenceEquals(declared.Method, method))
        { (_rpcPolicies ??= [])[method.ID] = options.Value; _rpcMethods?.Remove(method.ID); }
        else { (_rpcMethods ??= [])[method.ID] = new RPCRegistration(method, options.Value); _rpcPolicies?.Remove(method.ID); }
        _removedRPCMethods?.Remove(method.ID);
    }
    /// <summary>Returns the effective compiled class declarations and instance RPC overrides.</summary><returns>A caller-owned snapshot of configured immutable tokens and copied policies, excluding locally removed IDs.</returns>
    public RPCRegistration[] GetNodeRPCConfig()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); var declared = Script.GetClassRPCConfig(GetType(), LiveScriptRegistration?.Type);
        if (declared.Length == 0) return _rpcMethods?.Values.ToArray() ?? [];
        var result = new Dictionary<uint, RPCRegistration>();
        foreach (var entry in declared)
            if (_removedRPCMethods?.Contains(entry.Method.ID) != true)
                result.Add(entry.Method.ID, _rpcPolicies is not null && _rpcPolicies.TryGetValue(entry.Method.ID, out var policy) ? new(entry.Method, policy) : entry);
        if (_rpcMethods is not null) foreach (var (id, entry) in _rpcMethods) result[id] = entry;
        return result.Values.ToArray();
    }
    private bool TryGetRPC(uint id, out RPCRegistration registration)
    {
        if (_rpcMethods is not null && _rpcMethods.TryGetValue(id, out registration)) return true;
        registration = default;
        if (_removedRPCMethods?.Contains(id) == true || !TryGetDeclaredRPC(id, out registration)) return false;
        if (_rpcPolicies is not null && _rpcPolicies.TryGetValue(id, out var policy)) registration = new(registration.Method, policy);
        return true;
    }
    private bool TryGetDeclaredRPC(uint id, out RPCRegistration registration) => Script.TryGetClassRPC(GetType(), id, out registration, LiveScriptRegistration?.Type);
    internal RPCRegistration RequireRPC(uint id) { ThrowIfDisposed(); EnsureScriptRPCReady(); Tree?.EnsureOwnerThread(); return TryGetRPC(id, out var result) ? result : throw new InvalidOperationException("RPC method is not configured on the node."); }
    /// <summary>Broadcasts a configured typed method invocation.</summary><typeparam name="TNode">Required Node receiver type.</typeparam><typeparam name="T">Concrete arguments.</typeparam><param name="method">Configured token.</param><param name="arguments">Argument model.</param>
    public void RPC<TNode, T>(RPCMethod<TNode, T> method, T arguments) where TNode : Node => RPCID(0, method, arguments);
    /// <summary>Sends a typed method invocation to broadcast, one peer or negative exclusion.</summary><typeparam name="TNode">Required Node receiver type.</typeparam><typeparam name="T">Concrete arguments.</typeparam><param name="id">Target peer or exclusion.</param><param name="method">Configured token.</param><param name="arguments">Argument model.</param>
    public void RPCID<TNode, T>(int id, RPCMethod<TNode, T> method, T arguments) where TNode : Node
    { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(method); if (this is not TNode receiver) throw new ArgumentException("RPC receiver type does not match this node.", nameof(method)); Multiplayer.RPC(id, receiver, method, arguments); }
}
