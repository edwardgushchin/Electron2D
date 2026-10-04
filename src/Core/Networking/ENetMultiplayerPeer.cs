namespace Electron2D;

/// <summary>Provides server/client and manually connected mesh multiplayer over native ENet UDP channels.</summary>
/// <remarks>Channel zero uses separate reliable/unreliable native channels; positive channels use additional
/// negotiated channels. Client/server topology supports higher-layer relay. Mesh hosts transfer their active
/// protocol lifetime to this endpoint: Close destroys them while leaving borrowed objects undisposed.
/// Incoming native packets retain exact metadata until consumed. All calls require the constructing thread.</remarks>
public class ENetMultiplayerPeer : MultiplayerPeer
{
    private readonly Dictionary<int, ENetPacketPeer> _peers = new();
    private readonly Dictionary<ENetPacketPeer, int> _ids = new();
    private readonly List<KeyValuePair<int, ENetConnection>> _mesh = new();
    private readonly nint[] _packets = new nint[4096];
    private readonly int[] _sources = new int[4096], _channels = new int[4096];
    private int _first, _next, _count, _id, _target, _mode, _generation;
    private long _bytes;
    private string _bind = "*";
    private ENetConnection? _host;
    private bool _polling, _closing;
    private MultiplayerConnectionStatus _status;
    /// <summary>Creates a disconnected ENet multiplayer endpoint.</summary>
    public ENetMultiplayerPeer() { }
    /// <summary>Releases unread native packets when deterministic disposal was omitted.</summary>
    ~ENetMultiplayerPeer() { ClearPackets(); }
    private void Idle() { CheckPacketPeer(); if (_mode != 0 || _closing) throw new InvalidOperationException("Create an endpoint while disconnected and outside cleanup."); }
    private static int Channels(int channels) { if (channels is < 0 or > 253) throw new ArgumentOutOfRangeException(nameof(channels)); return channels == 0 ? 0 : channels + 2; }
    /// <summary>Gets the current client/server host.</summary><value>Borrowed active host, or null in idle/mesh mode. Direct Service/Destroy/disposal disrupts the multiplayer endpoint.</value>
    public ENetConnection? Host { get { CheckPacketPeer(); return _host; } }
    /// <summary>Sets the bind IP for future client/server hosts.</summary><param name="ip">IPv4/IPv6 literal or wildcard.</param>
    public void SetBindIP(string ip) { Idle(); NetworkSockets.BindAddress(ip); _bind = ip; }
    /// <summary>Creates a UDP server.</summary><param name="port">Zero selects an ephemeral port.</param><param name="maxClients">One through 4095.</param><param name="maxChannels">Zero selects all channels; one through 253 adds two reserved channels.</param><param name="inBandwidth">Incoming byte rate; zero unlimited.</param><param name="outBandwidth">Outgoing byte rate; zero unlimited.</param>
    public void CreateServer(int port, int maxClients = 32, int maxChannels = 0, uint inBandwidth = 0, uint outBandwidth = 0)
    {
        Idle(); var channels = Channels(maxChannels); var host = new ENetConnection(); try { host.CreateHostBound(_bind, port, maxClients, channels, inBandwidth, outBandwidth); _host = host; _mode = 1; _id = 1; _status = MultiplayerConnectionStatus.Connected; base.RefuseNewConnections = false; } catch { host.Dispose(); throw; }
    }
    /// <summary>Creates a UDP client and begins peer negotiation.</summary><param name="address">DNS name or IPv4/IPv6 literal.</param><param name="port">Server port.</param><param name="channelCount">Zero selects all channels; one through 253 adds two reserved channels.</param><param name="inBandwidth">Incoming byte rate; zero unlimited.</param><param name="outBandwidth">Outgoing byte rate; zero unlimited.</param><param name="localPort">Zero selects automatic binding.</param>
    public void CreateClient(string address, int port, int channelCount = 0, uint inBandwidth = 0, uint outBandwidth = 0, int localPort = 0)
    {
        Idle(); var channels = Channels(channelCount); var host = new ENetConnection();
        try { if (localPort == 0) host.CreateHost(1, channels, inBandwidth, outBandwidth); else host.CreateHostBound(_bind, localPort, 1, channels, inBandwidth, outBandwidth); var id = GenerateUniqueID(); var peer = host.ConnectToHost(address, port, channels, (uint)id); _peers.Add(1, peer); _ids.Add(peer, 1); _host = host; _id = id; _mode = 2; _status = MultiplayerConnectionStatus.Connecting; base.RefuseNewConnections = false; }
        catch { _peers.Clear(); _ids.Clear(); host.Dispose(); throw; }
    }
    /// <summary>Creates a manually connected mesh endpoint.</summary><param name="uniqueID">Positive local identity.</param>
    public void CreateMesh(int uniqueID) { Idle(); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(uniqueID); _mode = 3; _id = uniqueID; _status = MultiplayerConnectionStatus.Connected; base.RefuseNewConnections = false; }
    /// <summary>Adds an already negotiated single-peer host to a mesh.</summary><param name="peerID">Positive remote identity distinct from local/existing identities.</param><param name="host">Host with exactly one connected peer. Close destroys its protocol state.</param><remarks>Membership is committed before the connection event. The host must not be shared with another mesh.</remarks>
    public void AddMeshPeer(int peerID, ENetConnection host)
    {
        CheckPacketPeer(); ArgumentNullException.ThrowIfNull(host); if (_mode != 3 || _closing) throw new InvalidOperationException("Mesh mode required."); if (peerID <= 0 || peerID == _id || _peers.ContainsKey(peerID) || _mesh.Any(p => ReferenceEquals(p.Value, host))) throw new ArgumentException("Mesh identity/host is already assigned."); var peers = host.GetPeers(); if (peers.Length != 1 || peers[0].GetState() != ENetPeerState.Connected) throw new ArgumentException("Mesh host requires exactly one connected peer."); _mesh.Add(new(peerID, host)); _peers.Add(peerID, peers[0]); _ids.Add(peers[0], peerID); EmitPeerConnected(peerID);
    }
    /// <summary>Returns an associated borrowed native peer.</summary><param name="id">Remote identity.</param><returns>Peer, or null when absent.</returns>
    public ENetPacketPeer? GetPeer(int id) { CheckPacketPeer(); return _peers.GetValueOrDefault(id); }
    /// <inheritdoc />
    public override bool RefuseNewConnections { get => base.RefuseNewConnections; set { CheckPacketPeer(); _host?.RefuseNewConnections(value); base.RefuseNewConnections = value; } }
    /// <inheritdoc />
    public override void SetTargetPeer(int id) { CheckPacketPeer(); if (id == int.MinValue) throw new ArgumentOutOfRangeException(nameof(id)); _target = id; }
    /// <inheritdoc />
    public override int GetUniqueID() { CheckPacketPeer(); return _id; }
    /// <inheritdoc />
    public override MultiplayerConnectionStatus GetConnectionStatus() { CheckPacketPeer(); return _status; }
    /// <inheritdoc />
    public override bool IsServer() { CheckPacketPeer(); return _mode == 1; }
    /// <inheritdoc />
    public override bool IsServerRelaySupported() { CheckPacketPeer(); return _mode is 1 or 2; }
    /// <inheritdoc />
    public override int GetAvailablePacketCount() { CheckPacketPeer(); return _count; }
    /// <inheritdoc />
    public override int GetPacketPeer() { CheckPacketPeer(); return _count == 0 ? 1 : _sources[_first]; }
    /// <inheritdoc />
    public override int GetPacketChannel() { CheckPacketPeer(); return _count == 0 ? 0 : _channels[_first]; }
    /// <inheritdoc />
    public override TransferMode GetPacketMode() { CheckPacketPeer(); if (_count == 0) return TransferMode.Reliable; ENetNative.PacketData(_packets[_first], out var flags); return (flags & 1) != 0 ? TransferMode.Reliable : (flags & 2) != 0 ? TransferMode.Unreliable : TransferMode.UnreliableOrdered; }
    /// <summary>Gets the maximum application packet capacity.</summary><returns>16 MiB minus nine bytes reserved by higher multiplayer protocols.</returns>
    public override int GetMaxPacketSize() { CheckPacketPeer(); return 16777216 - 9; }
    /// <inheritdoc />
    protected override int NextPacketSize() => _count == 0 ? -1 : ENetNative.PacketData(_packets[_first], out _).Length;
    /// <inheritdoc />
    protected override void ReadPacketCore(Span<byte> destination) { var packet = _packets[_first]; var bytes = ENetNative.PacketData(packet, out _); bytes.CopyTo(destination); _bytes -= bytes.Length; _packets[_first] = 0; _first = (_first + 1) % _packets.Length; _count--; ENetNative.ReleasePacket(packet); }
    /// <inheritdoc />
    public override void PutPacket(ReadOnlySpan<byte> data)
    {
        CheckPacketPeer(); if (_status != MultiplayerConnectionStatus.Connected) throw new InvalidOperationException("Connected multiplayer endpoint required."); if (data.Length > GetMaxPacketSize()) throw new ArgumentException("Multiplayer packet exceeds capacity.");
        var flags = TransferMode == TransferMode.Reliable ? ENetPacketFlags.Reliable : TransferMode == TransferMode.Unreliable ? ENetPacketFlags.Unsequenced | ENetPacketFlags.UnreliableFragment : ENetPacketFlags.UnreliableFragment; var channel = TransferChannel > 0 ? checked(TransferChannel + 1) : TransferMode == TransferMode.Reliable ? 0 : 1;
        if (_target != 0 && !_peers.ContainsKey(Math.Abs(_target))) throw new ArgumentException("Target peer is absent."); if (_mode == 2) { _peers[1].Send(channel, data, flags); return; }
        if (_target > 0) { _peers[_target].Send(channel, data, flags); return; }
        List<Exception>? errors = null; foreach (var pair in _peers) if (pair.Key != -_target) try { pair.Value.Send(channel, data, flags); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException(errors);
    }
    private void Store(ENetPacketPeer peer, int source, int channel)
    {
        var packet = peer.TakePacket(); var size = ENetNative.PacketData(packet, out _).Length;
        if (_count == _packets.Length || _bytes + size > 67108864) { ENetNative.ReleasePacket(packet); throw new IOException("Multiplayer incoming packet budget exceeded."); }
        _packets[_next] = packet; _sources[_next] = source; _channels[_next] = channel < 2 ? 0 : channel - 1; _next = (_next + 1) % _packets.Length; _count++; _bytes += size;
    }
    private void ClearPackets() { while (_count > 0) { ENetNative.ReleasePacket(_packets[_first]); _packets[_first] = 0; _first = (_first + 1) % _packets.Length; _count--; } _first = _next = 0; _bytes = 0; }
    /// <inheritdoc />
    public override void Poll()
    {
        CheckPacketPeer(); if (_polling) throw new InvalidOperationException("Multiplayer polling cannot reenter itself."); if (_mode == 0) return; _polling = true; var generation = _generation; List<Exception>? errors = null;
        try
        {
            for (var i = 0; i < _mesh.Count && generation == _generation;) { var entry = _mesh[i]; PollHost(entry.Value, entry.Key, generation, ref errors); if (i < _mesh.Count && _mesh[i].Key == entry.Key) i++; }
            if (_host is { } host && generation == _generation) PollHost(host, 0, generation, ref errors);
        }
        finally { _polling = false; }
        if (errors is not null) throw new AggregateException(errors);
    }
    private void PollHost(ENetConnection host, int meshID, int generation, ref List<Exception>? errors)
    {
        if (host.IsDisposed) { if (meshID != 0) Drop(meshID, true, ref errors); else Close(); return; }
        ENetEvent e; try { e = host.Service(); } catch (Exception error) { (errors ??= []).Add(error); if (meshID != 0) Drop(meshID, true, ref errors); else Close(); return; }
        while (generation == _generation && e.Type != ENetEventType.None)
        {
            if (e.Type == ENetEventType.Connect && e.Peer is { } joined)
            {
                if (_mode == 1) { if (RefuseNewConnections || e.Data is < 2 or > int.MaxValue || _peers.ContainsKey((int)e.Data)) joined.Reset(); else { var id = (int)e.Data; _peers.Add(id, joined); _ids.Add(joined, id); Emit(id, true, ref errors); } }
                else if (_mode == 2) { _status = MultiplayerConnectionStatus.Connected; Emit(1, true, ref errors); } else joined.Reset();
            }
            else if (e.Type == ENetEventType.Receive && e.Peer is { } received) { if (_ids.TryGetValue(received, out var source)) Store(received, source, e.Channel); else ENetNative.ReleasePacket(received.TakePacket()); }
            else if (e.Type == ENetEventType.Disconnect && e.Peer is { } departed && _ids.TryGetValue(departed, out var id)) Drop(id, true, ref errors);
            if (generation != _generation || IsDisposed) break; try { e = host.CheckEvents(); } catch (Exception error) { (errors ??= []).Add(error); if (meshID != 0) Drop(meshID, true, ref errors); else Close(); return; }
        }
        if (generation != _generation || IsDisposed) return;
        // A borrowed peer can be reset or disposed without a native disconnection event.
        int inactive; do { inactive = 0; foreach (var pair in _peers) if (pair.Value.IsDisposed || !pair.Value.IsActive()) { inactive = pair.Key; break; } if (inactive != 0) Drop(inactive, true, ref errors); } while (inactive != 0 && generation == _generation);
    }
    private void Emit(int id, bool connected, ref List<Exception>? errors) { try { if (connected) EmitPeerConnected(id); else EmitPeerDisconnected(id); } catch (Exception error) { (errors ??= []).Add(error); } }
    private void Drop(int id, bool signal, ref List<Exception>? errors)
    {
        if (!_peers.Remove(id, out var peer)) return; _ids.Remove(peer); if (_mode == 3) { var index = _mesh.FindIndex(p => p.Key == id); if (index >= 0) { var host = _mesh[index].Value; _mesh.RemoveAt(index); if (!host.IsDisposed) try { host.Destroy(); } catch (Exception error) { (errors ??= []).Add(error); } } }
        var client = _mode == 2; var connected = _status == MultiplayerConnectionStatus.Connected; if (client) Close(); if (signal && (!client || connected)) Emit(id, false, ref errors);
    }
    /// <inheritdoc />
    public override void DisconnectPeer(int peer, bool force = false) { CheckPacketPeer(); if (!_peers.TryGetValue(peer, out var value)) throw new ArgumentException("Peer is absent."); if (force) { value.PeerDisconnectNow(); List<Exception>? errors = null; Drop(peer, false, ref errors); } else value.PeerDisconnect(); }
    /// <inheritdoc />
    public override void Close()
    {
        CheckPacketPeer(); if (_closing) return; _closing = true; _generation++; List<Exception>? errors = null;
        try { ClearPackets(); foreach (var pair in _mesh) try { pair.Value.Destroy(); } catch (Exception error) { (errors ??= []).Add(error); } _mesh.Clear(); _peers.Clear(); _ids.Clear(); var host = _host; _host = null; _mode = _id = _target = 0; _status = MultiplayerConnectionStatus.Disconnected; base.RefuseNewConnections = false; try { host?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); } }
        finally { _closing = false; }
        if (errors is not null) throw new AggregateException(errors);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) Close(); base.Dispose(disposing); }
}
