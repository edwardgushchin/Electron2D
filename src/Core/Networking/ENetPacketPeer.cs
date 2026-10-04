namespace Electron2D;

/// <summary>Exposes an ENet host-owned peer, its complete packet queue and protocol statistics.</summary>
/// <remarks>GetState/IsActive project detached state. Other protocol operations require an active host/peer
/// on the constructing thread. Incoming packets are queued by ENetConnection.Service and read with the
/// inherited caller-span/snapshot contract. Disposal forcefully disconnects this peer without disposing its host.</remarks>
public sealed class ENetPacketPeer : PacketPeer
{
    private readonly ENetConnection _host;
    internal readonly int Index;
    private bool _active = true;
    private readonly nint[] _packets = new nint[4096];
    private int _first, _next, _count;
    private long _bytes;
    internal ENetPacketPeer(ENetConnection host, int index) { _host = host; Index = index; }
    /// <summary>Scales packet-loss statistics.</summary>
    public const uint PacketLossScale = 65536;
    /// <summary>Scales throttle statistics.</summary>
    public const uint PacketThrottleScale = 32;
    private void Check(bool active = true) { CheckPacketPeer(); if (active && (!_active || _host.IsDisposed)) throw new InvalidOperationException("ENet peer is inactive."); _host.CheckOwner(); }
    internal bool Active => _active;
    internal void Queue(nint packet)
    {
        var size = ENetNative.PacketData(packet, out _).Length; if (_count == _packets.Length || _bytes + size > 67108864) { ENetNative.ReleasePacket(packet); throw new IOException("ENet receive queue capacity exceeded."); }
        _packets[_next] = packet; _next = (_next + 1) % _packets.Length; _count++; _bytes += size;
    }
    internal nint TakePacket() { var packet = _packets[_first]; _bytes -= ENetNative.PacketData(packet, out _).Length; _packets[_first] = 0; _first = (_first + 1) % _packets.Length; _count--; return packet; }
    /// <summary>Releases unread native packets when deterministic disposal was omitted.</summary>
    ~ENetPacketPeer() { ClearPackets(); }
    internal void Deactivate() { _active = false; ClearPackets(); }
    private void ClearPackets() { while (_count > 0) { ENetNative.ReleasePacket(_packets[_first]); _packets[_first] = 0; _first = (_first + 1) % _packets.Length; _count--; } _first = _next = 0; _bytes = 0; }
    /// <summary>Returns the negotiated channel count.</summary><returns>One through 255 for an active peer.</returns>
    public int GetChannels() { Check(); return (int)_host.Peer(Index, 1); }
    /// <summary>Returns the flags of the next queued packet without consuming it.</summary><returns>None when the receive queue is empty; receiver flags need not include every sender flag.</returns>
    public ENetPacketFlags GetPacketFlags() { Check(); if (_count == 0) return ENetPacketFlags.None; ENetNative.PacketData(_packets[_first], out var flags); return (ENetPacketFlags)(flags & 11); }
    /// <summary>Returns the peer's cached remote IP literal.</summary><returns>The resolved IPv4/IPv6 address.</returns>
    public string GetRemoteAddress() { Check(); return _host.Endpoint((uint)_host.Peer(Index, 2)).Host; }
    /// <summary>Returns the remote port.</summary><returns>The active peer's port.</returns>
    public int GetRemotePort() { Check(); return (int)_host.Peer(Index, 3); }
    /// <summary>Returns the ENet protocol state without polling.</summary><returns>Disconnected after detachment.</returns>
    public ENetPeerState GetState() { Check(false); return _active && !_host.IsDisposed ? (ENetPeerState)_host.Peer(Index, 0) : ENetPeerState.Disconnected; }
    /// <summary>Returns one current native peer statistic.</summary><param name="statistic">The peer-statistic domain.</param><returns>The unsigned counter projected as an exact double.</returns>
    public double GetStatistic(ENetPeerStatistic statistic) { Check(); if (!Enum.IsDefined(statistic)) throw new ArgumentOutOfRangeException(nameof(statistic)); return ENetNative.Statistic(_host.Handle, Index, (int)statistic); }
    /// <summary>Reports whether this logical peer remains attached to a host.</summary><returns>True includes connecting/disconnecting states.</returns>
    public bool IsActive() { Check(false); return _active && !_host.IsDisposed; }
    /// <summary>Starts a graceful disconnect.</summary><param name="data">Peer-visible disconnection data.</param>
    public void PeerDisconnect(uint data = 0) { Check(); _host.Peer(Index, 4, data); }
    /// <summary>Starts graceful disconnect after queued outgoing data drains.</summary><param name="data">Peer-visible disconnection data.</param>
    public void PeerDisconnectLater(uint data = 0) { Check(); _host.Peer(Index, 5, data); }
    /// <summary>Immediately disconnects locally and sends a best-effort notification.</summary><param name="data">Peer-visible data.</param><remarks>No local disconnected event is generated.</remarks>
    public void PeerDisconnectNow(uint data = 0) { Check(); try { _host.Peer(Index, 6, data); } finally { _host.Detach(this); } }
    /// <summary>Queues a protocol ping.</summary>
    public void Ping() { Check(); _host.Peer(Index, 8); }
    /// <summary>Sets the native ping interval.</summary><param name="interval">Milliseconds; zero restores the native default.</param>
    public void PingInterval(uint interval) { Check(); _host.Peer(Index, 9, interval); }
    /// <summary>Resets locally without sending a disconnect packet.</summary>
    public void Reset() { Check(); try { _host.Peer(Index, 7); } finally { _host.Detach(this); } }
    /// <summary>Queues one complete packet on a negotiated channel.</summary><param name="channel">Existing zero-based native channel.</param><param name="packet">Borrowed bytes copied into native queued storage.</param><param name="flags">Reliability/ordering/fragmentation flags.</param><exception cref="ArgumentException">Channel, flags or payload capacity is invalid.</exception>
    public void Send(int channel, ReadOnlySpan<byte> packet, ENetPacketFlags flags) { Check(); if (channel < 0 || channel >= GetChannels()) throw new ArgumentOutOfRangeException(nameof(channel)); if ((flags & ~((ENetPacketFlags)11)) != 0) throw new ArgumentOutOfRangeException(nameof(flags)); if (packet.Length > GetMaxPacketSize()) throw new ArgumentException("ENet packet exceeds its advertised capacity.", nameof(packet)); _host.Send(this, channel, packet, flags); }
    /// <summary>Sets timeout backoff and minimum/maximum bounds.</summary><param name="timeout">Backoff multiplier; zero selects the native default.</param><param name="timeoutMin">Minimum milliseconds, zero selects 5000.</param><param name="timeoutMax">Maximum milliseconds, zero selects 30000.</param><remarks>The multiplier is independent of the millisecond bounds.</remarks>
    public void SetTimeout(uint timeout, uint timeoutMin, uint timeoutMax) { Check(); if ((timeoutMin == 0 ? 5000 : timeoutMin) > (timeoutMax == 0 ? 30000 : timeoutMax)) throw new ArgumentException("Timeout minimum exceeds maximum."); _host.Peer(Index, 10, timeout, timeoutMin, timeoutMax); }
    /// <summary>Configures native packet-throttle adaptation.</summary><param name="interval">Milliseconds; zero selects native default.</param><param name="acceleration">Acceleration scale.</param><param name="deceleration">Deceleration scale.</param>
    public void ThrottleConfigure(uint interval, uint acceleration, uint deceleration) { Check(); _host.Peer(Index, 11, interval, acceleration, deceleration); }
    /// <inheritdoc />
    public override int GetAvailablePacketCount() { Check(false); return _active ? _count : 0; }
    /// <inheritdoc />
    public override int GetMaxPacketSize() { CheckPacketPeer(); return 16777216; }
    /// <inheritdoc />
    public override void PutPacket(ReadOnlySpan<byte> data) => Send(0, data, ENetPacketFlags.Reliable);
    /// <inheritdoc />
    protected override int NextPacketSize() { Check(); return _count == 0 ? -1 : ENetNative.PacketData(_packets[_first], out _).Length; }
    /// <inheritdoc />
    protected override void ReadPacketCore(Span<byte> destination) { var packet = _packets[_first]; var bytes = ENetNative.PacketData(packet, out _); bytes.CopyTo(destination); _bytes -= bytes.Length; _packets[_first] = 0; _first = (_first + 1) % _packets.Length; _count--; ENetNative.ReleasePacket(packet); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { try { if (_active && !_host.IsDisposed) PeerDisconnectNow(); } finally { ClearPackets(); } } base.Dispose(disposing); }
}
