namespace Electron2D;

public partial class Node
{
    private int _multiplayerAuthority = 1;
    private Dictionary<uint, RPCRegistration>? _rpcMethods;
    /// <summary>Gets the interface assigned to the nearest containing scene branch.</summary><value>The default interface or most specific custom override; detached access fails.</value>
    public MultiplayerAPI Multiplayer { get { ThrowIfDisposed(); var tree = Tree ?? throw new InvalidOperationException("Multiplayer requires an attached scene node."); return tree.GetMultiplayer(this); } }
    /// <summary>Gets the local node's configured multiplayer authority.</summary><returns>Server identity one initially.</returns>
    public int GetMultiplayerAuthority() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _multiplayerAuthority; }
    /// <summary>Sets a positive authority identity, optionally recursively for current descendants.</summary><param name="id">Positive peer identity.</param><param name="recursive">True applies to current descendants; later children retain their own default/configuration.</param><remarks>This local configuration does not replicate itself; participants must agree separately.</remarks>
    public void SetMultiplayerAuthority(int id, bool recursive = true) { EnsureMutable(); if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id)); _multiplayerAuthority = id; if (recursive) foreach (var child in _children) child.SetMultiplayerAuthority(id, true); }
    /// <summary>Reports whether this node's authority matches its assigned interface identity.</summary><returns>False for detached nodes.</returns>
    public bool IsMultiplayerAuthority() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return Tree is not null && Multiplayer.GetUniqueID() == _multiplayerAuthority; }
    /// <summary>Configures a typed callable and its invocation policy.</summary><param name="method">Immutable receiver/codec token, unique per node by ID.</param><param name="options">Policy; null removes the configuration.</param><remarks>Tokens/closures are borrowed configuration. Configure derived nodes in their typed scene factory/constructor for scene reconstruction.</remarks>
    public void RPCConfig(RPCMethod method, RPCOptions? options = null)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(method); if (!method.Accepts(this)) throw new ArgumentException("RPC receiver type does not match this node.", nameof(method));
        if (options is null) { _rpcMethods?.Remove(method.ID); return; }
        _rpcMethods ??= []; if (_rpcMethods.TryGetValue(method.ID, out var existing) && !ReferenceEquals(existing.Method, method)) throw new ArgumentException("RPC ID is already assigned to another method token.", nameof(method)); _rpcMethods[method.ID] = new RPCRegistration(method, options.Value);
    }
    /// <summary>Returns caller-owned typed RPC configuration snapshots.</summary><returns>Configured immutable tokens and copied policies.</returns>
    public RPCRegistration[] GetNodeRPCConfig() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _rpcMethods?.Values.ToArray() ?? []; }
    internal RPCRegistration RequireRPC(uint id) { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _rpcMethods is not null && _rpcMethods.TryGetValue(id, out var result) ? result : throw new InvalidOperationException("RPC method is not configured on the node."); }
    /// <summary>Broadcasts a configured typed method invocation.</summary><typeparam name="TNode">Required Node receiver type.</typeparam><typeparam name="T">Concrete arguments.</typeparam><param name="method">Configured token.</param><param name="arguments">Argument model.</param>
    public void RPC<TNode, T>(RPCMethod<TNode, T> method, T arguments) where TNode : Node => RPCID(0, method, arguments);
    /// <summary>Sends a typed method invocation to broadcast, one peer or negative exclusion.</summary><typeparam name="TNode">Required Node receiver type.</typeparam><typeparam name="T">Concrete arguments.</typeparam><param name="id">Target peer or exclusion.</param><param name="method">Configured token.</param><param name="arguments">Argument model.</param>
    public void RPCID<TNode, T>(int id, RPCMethod<TNode, T> method, T arguments) where TNode : Node
    { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(method); if (this is not TNode receiver) throw new ArgumentException("RPC receiver type does not match this node.", nameof(method)); Multiplayer.RPC(id, receiver, method, arguments); }
}
