namespace Electron2D;

/// <summary>Contains a copied membership observation with borrowed connection and channel identities.</summary>
public sealed class WebRTCPeerInfo
{
    internal WebRTCPeerInfo(WebRTCPeerConnection connection, bool connected, WebRTCDataChannel[] channels) { Connection = connection; Connected = connected; Channels = Array.AsReadOnly((WebRTCDataChannel[])channels.Clone()); }
    /// <summary>Gets the caller-owned negotiated connection.</summary><value>The same connection supplied to AddPeer.</value>
    public WebRTCPeerConnection Connection { get; }
    /// <summary>Gets whether every transport channel was open at snapshot time.</summary><value>A copied membership flag.</value>
    public bool Connected { get; }
    /// <summary>Gets copied channel membership containing borrowed channel objects.</summary><value>Three reserved channels followed by configured additional channels; membership removal retires them.</value>
    public IReadOnlyList<WebRTCDataChannel> Channels { get; }
}
/// <summary>Routes multiplayer packets through explicitly negotiated WebRTC connections.</summary>
/// <remarks>Connections remain caller-owned. This transport owns its generated channel cohort, and membership removal
/// releases those channels. Offers/candidates travel through the application's signaling path. Poll drives owner events.</remarks>
public sealed class WebRTCMultiplayerPeer : MultiplayerPeer
{
    private sealed class Entry
    {
        internal readonly int ID; internal readonly WebRTCPeerConnection Connection; internal readonly WebRTCDataChannel[] Channels; internal bool Connected;
        internal Entry(int id, WebRTCPeerConnection connection, WebRTCDataChannel[] channels) { ID = id; Connection = connection; Channels = channels; }
    }
    private readonly Dictionary<int, Entry> _peers = []; private readonly List<Entry> _order = [], _snapshot = [];
    private TransferMode[] _modes = [TransferMode.Reliable, TransferMode.UnreliableOrdered, TransferMode.Unreliable];
    private int _mode, _id, _target, _cursor; private bool _polling, _preparing; private Entry? _next; private int _nextChannel;
    private MultiplayerConnectionStatus _status = MultiplayerConnectionStatus.Disconnected;
    /// <summary>Creates an unconfigured multiplayer endpoint.</summary>
    public WebRTCMultiplayerPeer() { }
    private void Edit() { CheckPacketPeer(); if (_preparing) throw new InvalidOperationException("Multiplayer configuration callbacks cannot mutate their owner."); }
    /// <summary>Creates a server endpoint with identity one.</summary><param name="channelsConfig">Copied additional channel modes; null selects only the three reserved channels.</param>
    public void CreateServer(IReadOnlyList<TransferMode>? channelsConfig = null) => Configure(1, 1, channelsConfig);
    /// <summary>Creates a client endpoint which connects to server identity one.</summary><param name="peerID">Local identity greater than one.</param><param name="channelsConfig">Copied additional channel modes.</param>
    public void CreateClient(int peerID, IReadOnlyList<TransferMode>? channelsConfig = null) { if (peerID <= 1) throw new ArgumentOutOfRangeException(nameof(peerID)); Configure(peerID, 2, channelsConfig); }
    /// <summary>Creates an explicitly connected mesh endpoint.</summary><param name="peerID">Positive local identity.</param><param name="channelsConfig">Copied additional channel modes.</param>
    public void CreateMesh(int peerID, IReadOnlyList<TransferMode>? channelsConfig = null) => Configure(peerID, 3, channelsConfig);
    private void Configure(int id, int mode, IReadOnlyList<TransferMode>? config)
    {
        Edit(); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id); _preparing = true; TransferMode[] modes;
        try { var count = config?.Count ?? 0; if (count < 0 || count > 65531) throw new ArgumentOutOfRangeException(nameof(config)); modes = new TransferMode[count + 3]; modes[0] = TransferMode.Reliable; modes[1] = TransferMode.UnreliableOrdered; modes[2] = TransferMode.Unreliable; for (var i = 0; i < count; i++) { var value = config![i]; if (value < TransferMode.Unreliable || value > TransferMode.Reliable) throw new ArgumentOutOfRangeException(nameof(config)); modes[i + 3] = value; } }
        finally { _preparing = false; }
        Close(); _id = id; _mode = mode; _modes = modes; _status = mode == 2 ? MultiplayerConnectionStatus.Connecting : MultiplayerConnectionStatus.Connected;
    }
    /// <summary>Adds a new, caller-owned connection and creates its negotiated channel cohort.</summary>
    /// <param name="peer">Connection in New state; it remains caller-owned.</param><param name="peerID">Positive remote identity distinct from local and existing identities.</param><param name="unreliableLifetime">Reserved unreliable-channel lifetime in milliseconds, zero through 65535.</param>
    public void AddPeer(WebRTCPeerConnection peer, int peerID, int unreliableLifetime = 1)
    {
        Edit(); ArgumentNullException.ThrowIfNull(peer); if (_mode == 0) throw new InvalidOperationException("Create a multiplayer endpoint first.");
        if (RefuseNewConnections) throw new UnauthorizedAccessException("New connections are refused.");
        if (peerID <= 0 || peerID == _id || _peers.ContainsKey(peerID) || _order.Any(e => ReferenceEquals(e.Connection, peer)) || _mode == 2 && peerID != 1) throw new ArgumentException("Invalid or duplicate remote peer identity.", nameof(peerID));
        if (unreliableLifetime is < 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(unreliableLifetime));
        if (peer.GetConnectionState() != WebRTCPeerConnection.ConnectionState.New) throw new InvalidOperationException("The connection must be new.");
        var channels = new List<WebRTCDataChannel>(); _preparing = true;
        try
        {
            for (var i = 0; i < _modes.Length; i++)
            {
                var mode = _modes[i]; var channel = peer.CreateDataChannel(i == 0 ? "reliable" : i == 1 ? "ordered" : i == 2 ? "unreliable" : (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), new() { Negotiated = true, ID = i + 1, Ordered = mode != TransferMode.Unreliable, MaxPacketLifeTime = mode == TransferMode.Reliable ? null : i < 3 ? unreliableLifetime : 1 });
                if (channel is null || channel.IsDisposed || channels.Any(c => ReferenceEquals(c, channel))) throw new InvalidOperationException("The channel factory returned an invalid or reused identity."); channels.Add(channel); channel.WriteMode = PacketWriteMode.Binary;
            }
            var entry = new Entry(peerID, peer, channels.ToArray()); _peers.Add(peerID, entry); _order.Add(entry);
        }
        catch (Exception error)
        {
            List<Exception>? errors = null; Node.CollectException(ref errors, error);
            foreach (var channel in channels) try { channel.Dispose(); } catch (Exception cleanup) { Node.CollectException(ref errors, cleanup); }
            Node.ThrowCollected("Peer channel setup failed.", errors); throw;
        }
        finally { _preparing = false; }
    }
    /// <summary>Reports current membership.</summary><param name="peerID">Remote identity.</param><returns>True while registered, including pending negotiation.</returns>
    public bool HasPeer(int peerID) { CheckPacketPeer(); return _peers.ContainsKey(peerID); }
    /// <summary>Returns a copied observation of one registered peer.</summary><param name="peerID">Remote identity.</param><returns>A snapshot with borrowed resources, or null if absent.</returns>
    public WebRTCPeerInfo? GetPeer(int peerID) { CheckPacketPeer(); return _peers.TryGetValue(peerID, out var e) ? new(e.Connection, e.Connected, e.Channels) : null; }
    /// <summary>Returns copied membership observations keyed by remote identity.</summary><returns>A caller-owned dictionary snapshot containing borrowed resource identities.</returns>
    public IReadOnlyDictionary<int, WebRTCPeerInfo> GetPeers() { CheckPacketPeer(); return _order.ToDictionary(e => e.ID, e => new WebRTCPeerInfo(e.Connection, e.Connected, e.Channels)); }
    /// <summary>Removes one membership and its generated channels, preserving the caller-owned connection.</summary><param name="peerID">Registered remote identity.</param>
    public void RemovePeer(int peerID) { Edit(); if (!_peers.TryGetValue(peerID, out var entry)) throw new KeyNotFoundException("Unknown remote peer."); Remove(entry, true); }
    private void Remove(Entry entry, bool emit)
    {
        _peers.Remove(entry.ID); _order.Remove(entry); if (ReferenceEquals(_next, entry)) _next = null; if (_mode == 2 && entry.ID == 1) _status = MultiplayerConnectionStatus.Disconnected;
        List<Exception>? errors = null; foreach (var channel in entry.Channels) try { channel.Dispose(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        if (emit && entry.Connected) try { EmitPeerDisconnected(entry.ID); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Peer removal failed.", errors);
    }
    /// <inheritdoc />
    public override int TransferChannel { get => base.TransferChannel; set { Edit(); base.TransferChannel = value; } }
    /// <inheritdoc />
    public override TransferMode TransferMode { get => base.TransferMode; set { Edit(); base.TransferMode = value; } }
    /// <inheritdoc />
    public override bool RefuseNewConnections { get => base.RefuseNewConnections; set { Edit(); base.RefuseNewConnections = value; } }
    /// <inheritdoc />
    public override void SetTargetPeer(int id) { Edit(); if (id == int.MinValue) throw new ArgumentOutOfRangeException(nameof(id)); _target = id; }
    /// <inheritdoc />
    public override int GetUniqueID() { CheckPacketPeer(); return _status == MultiplayerConnectionStatus.Disconnected ? 1 : _id; }
    /// <inheritdoc />
    public override MultiplayerConnectionStatus GetConnectionStatus() { CheckPacketPeer(); return _status; }
    /// <inheritdoc />
    public override bool IsServer() { CheckPacketPeer(); return _id == 1; }
    /// <inheritdoc />
    public override bool IsServerRelaySupported() { CheckPacketPeer(); return _mode is 1 or 2; }
    /// <inheritdoc />
    public override void Poll()
    {
        Edit(); if (_polling) throw new InvalidOperationException("Multiplayer polling cannot reenter itself."); _polling = true; _snapshot.Clear(); _snapshot.AddRange(_order); List<Exception>? errors = null;
        try
        {
            foreach (var e in _snapshot)
            {
                if (IsDisposed || !_peers.TryGetValue(e.ID, out var current) || !ReferenceEquals(current, e)) continue;
                try
                {
                    if (e.Connection.IsDisposed || e.Channels.Any(c => c.IsDisposed)) { Remove(e, true); continue; }
                    try { e.Connection.Poll(); } catch (Exception error) { Node.CollectException(ref errors, error); }
                    if (IsDisposed || !_peers.TryGetValue(e.ID, out current) || !ReferenceEquals(current, e)) continue;
                    var state = e.Connection.GetConnectionState(); if (state is WebRTCPeerConnection.ConnectionState.Closed or WebRTCPeerConnection.ConnectionState.Failed or WebRTCPeerConnection.ConnectionState.Disconnected) { Remove(e, true); continue; }
                    var ready = true; var closed = false;
                    foreach (var channel in e.Channels)
                    {
                        try { channel.Poll(); if (IsDisposed || !_peers.TryGetValue(e.ID, out current) || !ReferenceEquals(current, e)) break; var status = channel.GetReadyState(); ready &= status == WebRTCDataChannel.ChannelState.Open; closed |= status is WebRTCDataChannel.ChannelState.Closing or WebRTCDataChannel.ChannelState.Closed; }
                        catch (Exception error) { Node.CollectException(ref errors, error); closed = true; }
                    }
                    if (IsDisposed || !_peers.TryGetValue(e.ID, out current) || !ReferenceEquals(current, e)) continue;
                    if (closed) { Remove(e, true); continue; }
                    if (ready && !e.Connected) { e.Connected = true; if (_mode == 2) _status = MultiplayerConnectionStatus.Connected; EmitPeerConnected(e.ID); }
                }
                catch (Exception error) { Node.CollectException(ref errors, error); }
            }
        }
        finally { _snapshot.Clear(); _polling = false; }
        Node.ThrowCollected("Multiplayer polling failed.", errors);
    }
    private WebRTCDataChannel? Next()
    {
        if (_next is not null && _peers.TryGetValue(_next.ID, out var e) && ReferenceEquals(e, _next) && e.Channels[_nextChannel].GetAvailablePacketCount() > 0) return e.Channels[_nextChannel]; _next = null;
        for (var n = 0; n < _order.Count; n++)
        {
            var index = (_cursor + n) % _order.Count; var entry = _order[index]; if (!entry.Connected) continue;
            for (var c = 0; c < entry.Channels.Length; c++) if (entry.Channels[c].GetAvailablePacketCount() > 0) { _next = entry; _nextChannel = c; _cursor = (index + 1) % _order.Count; return entry.Channels[c]; }
        }
        return null;
    }
    /// <inheritdoc />
    public override int GetAvailablePacketCount() { CheckPacketPeer(); var count = 0; foreach (var e in _order) if (e.Connected) foreach (var channel in e.Channels) count = checked(count + channel.GetAvailablePacketCount()); return count; }
    /// <inheritdoc />
    public override int GetPacketPeer() { CheckPacketPeer(); return Next() is null ? 0 : _next!.ID; }
    /// <inheritdoc />
    public override int GetPacketChannel() { CheckPacketPeer(); return Next() is null ? 0 : _nextChannel < 3 ? 0 : _nextChannel - 2; }
    /// <inheritdoc />
    public override TransferMode GetPacketMode() { CheckPacketPeer(); return Next() is null ? TransferMode.Reliable : _modes[_nextChannel]; }
    /// <inheritdoc />
    public override int GetMaxPacketSize() { CheckPacketPeer(); return 1200; }
    /// <inheritdoc />
    protected override int NextPacketSize() { var channel = Next(); return channel is null ? -1 : channel.PeekPacketSize(); }
    /// <inheritdoc />
    protected override void ReadPacketCore(Span<byte> destination) { var channel = Next() ?? throw new InvalidOperationException("No packet available."); channel.GetPacket(destination); _next = null; }
    /// <inheritdoc />
    public override void PutPacket(ReadOnlySpan<byte> data)
    {
        Edit(); if (_status == MultiplayerConnectionStatus.Disconnected) throw new InvalidOperationException("The multiplayer endpoint is disconnected."); if (data.Length > 1200) throw new ArgumentException("Packet exceeds multiplayer capacity.", nameof(data));
        if (TransferChannel > _modes.Length - 3) throw new ArgumentOutOfRangeException(nameof(TransferChannel));
        var channel = TransferChannel == 0 ? TransferMode == TransferMode.Reliable ? 0 : TransferMode == TransferMode.UnreliableOrdered ? 1 : 2 : TransferChannel + 2; if (channel >= _modes.Length) throw new ArgumentOutOfRangeException(nameof(TransferChannel));
        if (_target > 0) { if (!_peers.TryGetValue(_target, out var e)) throw new KeyNotFoundException("Unknown target peer."); e.Channels[channel].PutPacket(data); }
        else foreach (var e in _order) if (e.Connected && (_target == 0 || e.ID != -_target)) e.Channels[channel].PutPacket(data);
    }
    /// <inheritdoc />
    public override void DisconnectPeer(int peer, bool force = false) { Edit(); if (!_peers.TryGetValue(peer, out var e)) throw new KeyNotFoundException("Unknown remote peer."); if (force) Remove(e, false); else e.Connection.Close(); }
    /// <inheritdoc />
    public override void Close() { Edit(); var items = _order.ToArray(); _peers.Clear(); _order.Clear(); _next = null; _mode = 0; _id = 0; _target = 0; _cursor = 0; _status = MultiplayerConnectionStatus.Disconnected; List<Exception>? errors = null; foreach (var e in items) foreach (var channel in e.Channels) try { channel.Dispose(); } catch (Exception error) { Node.CollectException(ref errors, error); } Node.ThrowCollected("Multiplayer cleanup failed.", errors); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); Edit(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) Close(); base.Dispose(disposing); }
}
