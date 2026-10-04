using System.Buffers.Binary;
using System.Text;
namespace Electron2D;

/// <summary>Polls admitted scene peers, typed RPCs, authentication and custom packets with server relay.</summary>
/// <remarks>The transport remains caller-owned. Packet/authentication spans are borrowed during callbacks.
/// Root paths locate nodes; immutable typed RPC tokens replace reflection. Preparation/path discovery allocates,
/// while repeated ready span/message processing reuses bounded buffers. Calls/disposal require the owner thread.</remarks>
public class SceneMultiplayer : MultiplayerAPI
{
    private MultiplayerPeer? _peer;
    private readonly Dictionary<int, PendingMultiplayerPeer> _pending = [];
    private readonly HashSet<int> _connected = [];
    private readonly List<int> _sendPeers = [];
    private readonly List<int> _expired = [];
    private readonly Dictionary<Node, byte[]> _outPaths = [];
    private readonly Dictionary<ulong, MultiplayerPathCache> _inPaths = [];
    private readonly Action<int> _onConnected, _onDisconnected;
    private byte[] _send = [], _receive = [], _relay = [];
    private int _capacity = 65536, _sender, _generation;
    private bool _polling, _sending, _serverRelay = true;
    private string _rootPath = "";
    private MultiplayerConnectionStatus _last;
    private MultiplayerPacketHandler? _auth;
    private double _timeout = 3;
    /// <summary>Creates a scene interface with local offline authority.</summary>
    public SceneMultiplayer() { _onConnected = AddPeer; _onDisconnected = RemovePeer; _peer = new OfflineMultiplayerPeer(); Subscribe(); _last = MultiplayerConnectionStatus.Connected; _ownsOffline = true; }
    private bool _ownsOffline;
    /// <summary>Gets or replaces a borrowed connecting/connected transport; replacement clears network/path state.</summary>
    /// <exception cref="InvalidOperationException">A disconnected or disposed peer is supplied.</exception>
    public override MultiplayerPeer? MultiplayerPeer
    {
        get { CheckMultiplayer(); return _peer; }
        set
        {
            CheckMultiplayer(); if (_sending) throw new InvalidOperationException("Cannot replace a transport during outgoing encoding."); if (ReferenceEquals(value, _peer)) return;
            if (value is not null && value.GetConnectionStatus() == MultiplayerConnectionStatus.Disconnected) throw new InvalidOperationException("A supplied transport must be connecting or connected.");
            var send = value is null || value is OfflineMultiplayerPeer ? Array.Empty<byte>() : new byte[_capacity]; var receive = send.Length == 0 ? Array.Empty<byte>() : new byte[_capacity]; var relay = send.Length == 0 ? Array.Empty<byte>() : new byte[_capacity];
            Unsubscribe(); var old = _peer; var owned = _ownsOffline; _ownsOffline = false; ClearCore(); _peer = value; _send = send; _receive = receive; _relay = relay; Subscribe(); _last = value?.GetConnectionStatus() ?? MultiplayerConnectionStatus.Disconnected; if (owned) old?.Dispose();
        }
    }
    /// <summary>Gets or sets the complete encoded message budget prepared on transport assignment.</summary><value>65536 initially; 64 through 64 MiB. Change while no live network transport is configured.</value>
    public int MaxPacketBytes { get { CheckMultiplayer(); return _capacity; } set { CheckMultiplayer(); if (_polling || _sending || _peer is not (null or OfflineMultiplayerPeer)) throw new InvalidOperationException("Change the packet budget before assigning a network transport."); if (value is < 64 or > 67108864) throw new ArgumentOutOfRangeException(nameof(value)); _capacity = value; } }
    /// <summary>Gets or sets a borrowed authentication-data callback; null automatically admits new peers.</summary><value>Changes apply to new authentication sessions; pending sessions retain the current callback.</value>
    public MultiplayerPacketHandler? AuthCallback { get { CheckMultiplayer(); return _auth; } set { CheckMultiplayer(); _auth = value; } }
    /// <summary>Gets or sets the monotonic authentication deadline in seconds.</summary><value>3 initially; finite nonnegative, zero disables expiration.</value>
    public double AuthTimeout { get { CheckMultiplayer(); return _timeout; } set { CheckMultiplayer(); if (!double.IsFinite(value) || value < 0 || value > long.MaxValue / 1000d) throw new ArgumentOutOfRangeException(nameof(value)); _timeout = value; } }
    /// <summary>Gets or sets the absolute scene root used by relative node RPC paths.</summary><value>Empty until SceneTree assignment. Changing it invalidates prepared path discovery.</value>
    public string RootPath { get { CheckMultiplayer(); return _rootPath; } set { CheckMultiplayer(); if (_sending) throw new InvalidOperationException("Cannot change root during outgoing encoding."); ArgumentNullException.ThrowIfNull(value); if (value.Length > 0 && !value.StartsWith('/') || value.Contains('\0')) throw new ArgumentException("The scene multiplayer root must be an absolute path or empty.", nameof(value)); _rootPath = value; InvalidatePaths(); } }
    /// <summary>Gets or sets whether supported transports notify clients and relay messages through the server.</summary><value>True initially; changing live topology is caller-controlled.</value>
    public bool ServerRelay { get { CheckMultiplayer(); return _serverRelay; } set { CheckMultiplayer(); _serverRelay = value; } }
    /// <summary>Gets or sets admission policy on the configured transport.</summary><value>False without a peer; setting without a peer fails.</value>
    public bool RefuseNewConnections { get { CheckMultiplayer(); return _peer?.RefuseNewConnections ?? false; } set { CheckMultiplayer(); RequirePeer().RefuseNewConnections = value; } }
    /// <summary>Occurs before admission when a direct peer begins authentication.</summary>
    public event Action<int>? PeerAuthenticating;
    /// <summary>Occurs after a pending peer fails/disconnects/expires.</summary>
    public event Action<int>? PeerAuthenticationFailed;
    /// <summary>Delivers custom bytes with original sender identity; storage is borrowed only during the callback.</summary>
    public event MultiplayerPacketHandler? PeerPacket;
    private MultiplayerPeer RequirePeer() => _peer is { } peer && peer.GetConnectionStatus() == MultiplayerConnectionStatus.Connected ? peer : throw new InvalidOperationException("A connected multiplayer transport is required.");
    private bool RelaySupported => _serverRelay && (_peer?.IsServerRelaySupported() ?? false);
    private void Subscribe() { if (_peer is not null) { _peer.PeerConnected += _onConnected; _peer.PeerDisconnected += _onDisconnected; } }
    private void Unsubscribe() { if (_peer is not null) { _peer.PeerConnected -= _onConnected; _peer.PeerDisconnected -= _onDisconnected; } }
    /// <inheritdoc />
    public override int GetUniqueID() { CheckMultiplayer(); return _peer?.GetUniqueID() ?? 0; }
    /// <inheritdoc />
    public override int[] GetPeers() { CheckMultiplayer(); return _connected.ToArray(); }
    /// <summary>Returns a caller-owned snapshot of direct peers awaiting bilateral authentication.</summary><returns>Pending identities, without admitted peers.</returns>
    public int[] GetAuthenticatingPeers() { CheckMultiplayer(); return _pending.Keys.ToArray(); }
    /// <inheritdoc />
    public override int GetRemoteSenderID() { CheckMultiplayer(); return _sender; }
    /// <summary>Clears admitted/pending/path state without closing the borrowed transport.</summary>
    public void Clear() { CheckMultiplayer(); if (_sending) throw new InvalidOperationException("Cannot reset during outgoing encoding."); ClearCore(); }
    private void ClearCore() { _generation++; _pending.Clear(); _connected.Clear(); InvalidatePaths(); _last = MultiplayerConnectionStatus.Disconnected; }
    internal void InvalidatePaths() { _outPaths.Clear(); _inPaths.Clear(); }
    /// <inheritdoc />
    public override void ObjectConfigurationAdd(string rootPath) => RootPath = rootPath;
    /// <inheritdoc />
    public override void ObjectConfigurationRemove(string rootPath) { CheckMultiplayer(); if (_rootPath != rootPath) throw new ArgumentException("Root configuration does not match.", nameof(rootPath)); RootPath = ""; }
    internal override void ValidateDetachment() { base.ValidateDetachment(); if (_sending) throw new InvalidOperationException("Cannot detach during outgoing encoding."); }
    internal override void Detach() { base.Detach(); _generation++; InvalidatePaths(); }
    private void AddPeer(int id)
    {
        if (id <= 0 || id == GetUniqueID() || _connected.Contains(id) || _pending.ContainsKey(id)) throw new InvalidDataException("Invalid or duplicate transport peer identity.");
        _sendPeers.EnsureCapacity(_connected.Count + _pending.Count + 1); _expired.EnsureCapacity(_connected.Count + _pending.Count + 1);
        if (_auth is not null) { _pending.Add(id, new PendingMultiplayerPeer(Environment.TickCount64, _auth)); Emit(PeerAuthenticating, id); }
        else Admit(id);
    }
    private void SnapshotSendPeers() { _sendPeers.Clear(); foreach (var id in _connected) _sendPeers.Add(id); }
    private void Admit(int id)
    {
        if (id <= 0 || id == GetUniqueID() || !_connected.Add(id)) return;
        _sendPeers.EnsureCapacity(_connected.Count + _pending.Count); var generation = _generation; List<Exception>? errors = null;
        if (RelaySupported && IsServer())
        {
            Span<byte> message = stackalloc byte[6]; message[0] = 7; message[1] = 1;
            foreach (var other in _connected.ToArray())
            {
                if (other == id) continue;
                try { BinaryPrimitives.WriteInt32LittleEndian(message[2..], id); Direct(other, message, TransferMode.Reliable, 0); BinaryPrimitives.WriteInt32LittleEndian(message[2..], other); Direct(id, message, TransferMode.Reliable, 0); } catch (Exception e) { (errors ??= []).Add(e); }
            }
        }
        if (id == 1) try { EmitConnectedToServer(); } catch (Exception e) { (errors ??= []).Add(e); }
        if (generation == _generation && _connected.Contains(id)) try { EmitPeerConnected(id); } catch (Exception e) { (errors ??= []).Add(e); }
        if (errors is not null) throw new AggregateException(errors);
    }
    private void RemovePeer(int id) => RemovePeer(id, true);
    private void RemovePeer(int id, bool notify)
    {
        if (_pending.Remove(id)) { if (notify) Emit(PeerAuthenticationFailed, id); return; }
        if (!_connected.Remove(id)) return;
        InvalidatePaths(); List<Exception>? errors = null;
        if (RelaySupported && IsServer())
        {
            Span<byte> message = stackalloc byte[6]; message[0] = 7; message[1] = 2; BinaryPrimitives.WriteInt32LittleEndian(message[2..], id);
            foreach (var other in _connected.ToArray()) try { Direct(other, message, TransferMode.Reliable, 0); } catch (Exception e) { (errors ??= []).Add(e); }
        }
        if (notify) try { EmitPeerDisconnected(id); } catch (Exception e) { (errors ??= []).Add(e); }
        if (errors is not null) throw new AggregateException(errors);
    }
    /// <summary>Removes a direct peer and closes its connection, suppressing this interface's local removal event.</summary><param name="id">Direct remote identity; relayed client peers cannot be closed by another client.</param>
    public void DisconnectPeer(int id) { CheckMultiplayer(); var peer = RequirePeer(); try { RemovePeer(id, false); } finally { peer.DisconnectPeer(id); } }
    private void Direct(int id, ReadOnlySpan<byte> packet, TransferMode mode, int channel)
    { var peer = RequirePeer(); if (packet.Length > _capacity || packet.Length > peer.GetMaxPacketSize()) throw new ArgumentException("Encoded packet exceeds the prepared/transport budget."); peer.TransferMode = mode; peer.TransferChannel = channel; peer.SetTargetPeer(id); peer.PutPacket(packet); }
    private void SendCommand(int target, ReadOnlySpan<byte> packet, TransferMode mode, int channel)
    {
        if (target == int.MinValue) throw new ArgumentOutOfRangeException(nameof(target));
        if (RelaySupported && !IsServer() && target != 1)
        {
            if (target > 0 && !_connected.Contains(target)) throw new ArgumentException("Target peer is not admitted.", nameof(target));
            if (packet.Length + 6 > _relay.Length) throw new ArgumentException("Relayed packet exceeds the prepared budget."); _relay[0] = 7; _relay[1] = 3; BinaryPrimitives.WriteInt32LittleEndian(_relay.AsSpan(2), target); packet.CopyTo(_relay.AsSpan(6)); Direct(1, _relay.AsSpan(0, packet.Length + 6), mode, channel); return;
        }
        if (target > 0) { if (!_connected.Contains(target)) throw new ArgumentException("Target peer is not admitted.", nameof(target)); Direct(target, packet, mode, channel); }
        else
        {
            List<Exception>? errors = null;
            SnapshotSendPeers(); for (var i = 0; i < _sendPeers.Count; i++) { var id = _sendPeers[i]; if (id != -target && _connected.Contains(id)) try { Direct(id, packet, mode, channel); } catch (Exception e) { (errors ??= []).Add(e); } }
            if (errors is not null) throw new AggregateException(errors);
        }
    }
    private void BeginSend() { CheckMultiplayer(); RequirePeer(); if (_sending) throw new InvalidOperationException("Outgoing codec/message construction cannot reenter."); _sending = true; }
    /// <summary>Sends nonempty custom bytes to admitted peers.</summary><param name="bytes">Borrowed payload.</param><param name="id">Zero broadcasts, positive targets, negative excludes.</param><param name="mode">Requested transport mode.</param><param name="channel">Nonnegative channel.</param>
    public void SendBytes(ReadOnlySpan<byte> bytes, int id = 0, TransferMode mode = TransferMode.Reliable, int channel = 0)
    {
        BeginSend(); try { if (bytes.IsEmpty || bytes.Length >= _send.Length) throw new ArgumentException("Custom bytes must be nonempty and fit the prepared budget.", nameof(bytes)); if ((uint)mode > 2 || channel < 0) throw new ArgumentOutOfRangeException(nameof(mode)); _send[0] = 3; bytes.CopyTo(_send.AsSpan(1)); SendCommand(id, _send.AsSpan(0, bytes.Length + 1), mode, channel); } finally { _sending = false; }
    }
    /// <summary>Sends nonempty authentication bytes before either participant completes authentication.</summary><param name="id">Pending direct remote identity.</param><param name="data">Borrowed nonempty bytes.</param>
    public void SendAuth(int id, ReadOnlySpan<byte> data)
    {
        BeginSend(); try { if (!_pending.TryGetValue(id, out var pending) || pending.Local || pending.Remote) throw new InvalidOperationException("Authentication session is unavailable or completed."); if (data.IsEmpty || data.Length + 2 > _send.Length) throw new ArgumentException("Authentication bytes must be nonempty and fit the budget.", nameof(data)); _send[0] = 7; _send[1] = 0; data.CopyTo(_send.AsSpan(2)); Direct(id, _send.AsSpan(0, data.Length + 2), TransferMode.Reliable, 0); } finally { _sending = false; }
    }
    /// <summary>Commits local authentication and sends its completion; admission waits for remote completion.</summary><param name="id">Pending direct identity.</param>
    public void CompleteAuth(int id)
    {
        CheckMultiplayer(); RequirePeer(); if (!_pending.TryGetValue(id, out var pending) || pending.Local) throw new InvalidOperationException("Authentication is unavailable or already locally complete.");
        Span<byte> message = stackalloc byte[] { 7, 0 }; Direct(id, message, TransferMode.Reliable, 0); pending.Local = true;
        if (pending.Remote) { _pending.Remove(id); Admit(id); } else _pending[id] = pending;
    }
    private void UpdateStatus()
    {
        var status = _peer?.GetConnectionStatus() ?? MultiplayerConnectionStatus.Disconnected; if (status == _last) return; var previous = _last; _last = status;
        if (status == MultiplayerConnectionStatus.Disconnected) { ClearCore(); if (previous == MultiplayerConnectionStatus.Connecting) EmitConnectionFailed(); else EmitServerDisconnected(); }
    }
    /// <inheritdoc />
    public override void Poll()
    {
        CheckMultiplayer(); if (_polling) throw new InvalidOperationException("Multiplayer polling cannot reenter."); _polling = true; List<Exception>? errors = null;
        try
        {
            UpdateStatus(); var peer = _peer; if (peer is null || _last == MultiplayerConnectionStatus.Disconnected) return; var generation = _generation;
            try { peer.Poll(); } catch (Exception e) { (errors ??= []).Add(e); }
            if (generation != _generation || !ReferenceEquals(peer, _peer)) return;
            UpdateStatus(); if (_last != MultiplayerConnectionStatus.Connected) return;
            while (peer.GetAvailablePacketCount() > 0 && generation == _generation)
            {
                var sender = peer.GetPacketPeer(); var mode = peer.GetPacketMode(); var channel = peer.GetPacketChannel();
                try
                {
                    var count = peer.GetPacket(_receive); var packet = _receive.AsSpan(0, count);
                    if (_pending.TryGetValue(sender, out var pending))
                    {
                        if (count < 2 || packet[0] != 7 || packet[1] != 0) continue;
                        if (count == 2) { pending.Remote = true; if (pending.Local) { _pending.Remove(sender); Admit(sender); } else _pending[sender] = pending; }
                        else if (!pending.Remote) pending.Callback(sender, packet[2..]);
                    }
                    else if (_connected.Contains(sender)) ProcessPacket(sender, packet, mode, channel);
                }
                catch (Exception e) { (errors ??= []).Add(e); if (peer.GetPacketError() != PacketReadStatus.OK) { try { _ = peer.GetPacket(); } catch (Exception drainError) { errors.Add(drainError); break; } } }
            }
            if (generation == _generation && _timeout > 0)
            {
                _expired.Clear(); var now = Environment.TickCount64; foreach (var entry in _pending) if (now - entry.Value.Start >= _timeout * 1000) _expired.Add(entry.Key);
                for (var i = 0; i < _expired.Count && generation == _generation; i++) { var id = _expired[i]; if (!_pending.Remove(id)) continue; try { peer.DisconnectPeer(id, true); } catch (Exception e) { (errors ??= []).Add(e); } try { Emit(PeerAuthenticationFailed, id); } catch (Exception e) { (errors ??= []).Add(e); } }
            }
            if (generation == _generation) UpdateStatus();
        }
        finally { _sender = 0; _polling = false; if (errors is not null) throw new AggregateException(errors); }
    }
    private void ProcessPacket(int sender, ReadOnlySpan<byte> packet, TransferMode mode, int channel)
    {
        if (packet.IsEmpty) throw new InvalidDataException("Empty scene packet.");
        if (packet[0] == 7) { ProcessSystem(sender, packet, mode, channel); return; }
        var previous = _sender; _sender = sender;
        try
        {
            if (packet[0] == 3) { if (packet.Length < 2) throw new InvalidDataException("Empty custom packet."); List<Exception>? errors = null; foreach (var callback in Delegate.EnumerateInvocationList(PeerPacket)) try { callback(sender, packet[1..]); } catch (Exception e) { (errors ??= []).Add(e); } if (errors is not null) throw new AggregateException(errors); }
            else if (packet[0] == 0) ReceiveRPC(sender, packet);
            else throw new InvalidDataException("Unknown or unavailable scene command.");
        }
        finally { _sender = previous; }
    }
    private void ProcessSystem(int sender, ReadOnlySpan<byte> packet, TransferMode mode, int channel)
    {
        if (packet.Length == 2 && packet[1] == 0) return;
        if (!RelaySupported || packet.Length < 6) throw new InvalidDataException("Malformed or unsupported scene system message."); var id = BinaryPrimitives.ReadInt32LittleEndian(packet[2..]);
        if (packet[1] is 1 or 2) { if (IsServer() || sender != 1 || packet.Length != 6 || id <= 1 || id == GetUniqueID()) throw new InvalidDataException("Untrusted relay peer notification."); if (packet[1] == 1) Admit(id); else RemovePeer(id); return; }
        if (packet[1] != 3 || packet.Length < 7) throw new InvalidDataException("Unknown relay command.");
        if (packet[6] == 7) throw new InvalidDataException("Nested system relay is invalid.");
        if (!IsServer()) { if (sender != 1 || !_connected.Contains(id)) throw new InvalidDataException("Untrusted relayed source."); ProcessPacket(id, packet[6..], mode, channel); return; }
        if (id == int.MinValue) throw new InvalidDataException("Invalid relay exclusion."); if (id > 0 && !_connected.Contains(id) && id != 1) return;
        Exception? forwardingError = null; _sending = true;
        try
        {
            packet.CopyTo(_relay); BinaryPrimitives.WriteInt32LittleEndian(_relay.AsSpan(2), sender);
            if (id > 0) { if (id != 1 && id != sender) Direct(id, _relay.AsSpan(0, packet.Length), mode, channel); }
            else { SnapshotSendPeers(); List<Exception>? errors = null; for (var i = 0; i < _sendPeers.Count; i++) { var other = _sendPeers[i]; if (other != sender && other != -id && _connected.Contains(other)) try { Direct(other, _relay.AsSpan(0, packet.Length), mode, channel); } catch (Exception e) { (errors ??= []).Add(e); } } if (errors is not null) throw new AggregateException(errors); }
        }
        catch (Exception error) { forwardingError = error; }
        finally { _sending = false; }
        if (id == 1 || id <= 0 && id != -1) try { ProcessPacket(sender, packet[6..], mode, channel); } catch (Exception error) { if (forwardingError is not null) throw new AggregateException(forwardingError, error); throw; }
        if (forwardingError is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(forwardingError).Throw();
    }
    private byte[] PathFor(Node node)
    {
        var tree = AttachedTree ?? throw new InvalidOperationException("Assign the interface to a SceneTree before RPC."); if (!ReferenceEquals(node.Tree, tree) || !ReferenceEquals(node.Multiplayer, this)) throw new InvalidOperationException("RPC node must belong to this multiplayer scene branch.");
        if (_outPaths.TryGetValue(node, out var path)) return path;
        var root = tree.Root.GetNode(_rootPath); if (!ReferenceEquals(node, root) && !root.IsAncestorOf(node)) throw new InvalidOperationException("RPC node must be below its configured root."); var relative = root.GetPathTo(node); path = Encoding.UTF8.GetBytes(relative); if (path.Length > 4096) throw new ArgumentException("RPC node path exceeds 4096 UTF8 bytes."); _outPaths.Add(node, path); return path;
    }
    /// <inheritdoc />
    public override void RPC<TNode, T>(int peer, TNode node, RPCMethod<TNode, T> method, T arguments)
    {
        ArgumentNullException.ThrowIfNull(node); ArgumentNullException.ThrowIfNull(method); BeginSend(); RPCOptions options; var localID = GetUniqueID(); Exception? sendError = null;
        try
        {
            if (peer == int.MinValue) throw new ArgumentOutOfRangeException(nameof(peer)); var registration = node.RequireRPC(method.ID); if (!ReferenceEquals(registration.Method, method) || !method.Accepts(node)) throw new ArgumentException("RPC method token does not match node configuration."); options = registration.Options;
            if (options.Mode == RPCMode.Disabled) throw new InvalidOperationException("RPC is disabled."); if (peer == localID && !options.CallLocal) throw new InvalidOperationException("This RPC does not permit a local-only call."); var path = PathFor(node);
            if (peer != localID && _connected.Count > 0)
            {
                var count = method.GetEncodedSize(arguments); var total = checked(7 + path.Length + count); if (total > _send.Length) throw new ArgumentException("RPC arguments exceed prepared message storage."); _send[0] = 0; BinaryPrimitives.WriteUInt32LittleEndian(_send.AsSpan(1), method.ID); BinaryPrimitives.WriteUInt16LittleEndian(_send.AsSpan(5), (ushort)path.Length); path.CopyTo(_send.AsSpan(7)); method.Encode(arguments, _send.AsSpan(7 + path.Length, count)); try { SendCommand(peer, _send.AsSpan(0, total), options.TransferMode, options.Channel); } catch (Exception error) { sendError = error; }
            }
            else if (peer > 0 && peer != localID) throw new ArgumentException("RPC target is not admitted.", nameof(peer));
        }
        finally { _sending = false; }
        if (options.CallLocal && (peer == 0 || peer == localID || peer < 0 && peer != -localID)) { var previous = _sender; _sender = localID; try { method.Invoke(node, arguments); } catch (Exception error) { if (sendError is not null) throw new AggregateException(sendError, error); throw; } finally { _sender = previous; } }
        if (sendError is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(sendError).Throw();
    }
    private void ReceiveRPC(int sender, ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 7) throw new InvalidDataException("Truncated RPC header."); var methodID = BinaryPrimitives.ReadUInt32LittleEndian(packet[1..]); var length = BinaryPrimitives.ReadUInt16LittleEndian(packet[5..]); if (length == 0 || length > 4096 || length > packet.Length - 7) throw new InvalidDataException("Invalid RPC node path width."); var path = packet.Slice(7, length); ulong hash = 14695981039346656037; foreach (var b in path) hash = unchecked((hash ^ b) * 1099511628211);
        if (!_inPaths.TryGetValue(hash, out var route) || !path.SequenceEqual(route.Path))
        {
            var tree = AttachedTree ?? throw new InvalidOperationException("RPC requires an assigned scene tree."); var text = new UTF8Encoding(false, true).GetString(path); if (text.StartsWith('/') || text.Contains('\0')) throw new InvalidDataException("RPC path must be a relative scene node path."); var root = tree.Root.GetNode(_rootPath); var node = root.GetNode(text); if (!ReferenceEquals(node.Multiplayer, this) || !ReferenceEquals(node, root) && !root.IsAncestorOf(node)) throw new InvalidDataException("RPC path escapes its configured scene branch."); route = new MultiplayerPathCache(node, path.ToArray()); _inPaths[hash] = route;
        }
        var registration = route.Node.RequireRPC(methodID); if (registration.Options.Mode == RPCMode.Disabled || registration.Options.Mode == RPCMode.Authority && route.Node.GetMultiplayerAuthority() != sender) throw new InvalidDataException("RPC sender is not authorized by the receiving node."); registration.Method.InvokeEncoded(route.Node, packet[(7 + length)..]);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<SceneMultiplayer, int>(nameof(MaxPacketBytes), p => p.MaxPacketBytes, (p, v) => p.MaxPacketBytes = v, _ => 65536);
        yield return new PropertyDescriptor<SceneMultiplayer, string>(nameof(RootPath), p => p.RootPath, (p, v) => p.RootPath = v, _ => "");
        yield return new PropertyDescriptor<SceneMultiplayer, double>(nameof(AuthTimeout), p => p.AuthTimeout, (p, v) => p.AuthTimeout = v, _ => 3);
        yield return new PropertyDescriptor<SceneMultiplayer, MultiplayerPacketHandler?>(nameof(AuthCallback), p => p.AuthCallback, (p, v) => p.AuthCallback = v, _ => null);
        yield return new PropertyDescriptor<SceneMultiplayer, bool>(nameof(ServerRelay), p => p.ServerRelay, (p, v) => p.ServerRelay = v, _ => true);
        yield return new PropertyDescriptor<SceneMultiplayer, bool>(nameof(RefuseNewConnections), p => p.RefuseNewConnections, (p, v) => p.RefuseNewConnections = v, _ => false);
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); if (_polling || _sending) throw new InvalidOperationException("Multiplayer cannot dispose during message dispatch."); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { Unsubscribe(); if (_ownsOffline) _peer?.Dispose(); _peer = null; ClearCore(); PeerAuthenticating = null; PeerAuthenticationFailed = null; PeerPacket = null; _send = _receive = _relay = []; } base.Dispose(disposing); }
}
internal struct PendingMultiplayerPeer(long start, MultiplayerPacketHandler callback) { internal readonly long Start = start; internal readonly MultiplayerPacketHandler Callback = callback; internal bool Local, Remote; }
internal sealed record MultiplayerPathCache(Node Node, byte[] Path);
