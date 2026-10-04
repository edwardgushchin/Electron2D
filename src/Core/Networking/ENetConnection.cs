using System.Net;
namespace Electron2D;

/// <summary>Owns an ENet protocol host, its typed peers and native UDP/DTLS transport.</summary>
/// <remarks>Owner-thread Service advances reliability/events and queues received packets on associated peers.
/// Constructors, host/channel/peer/codec preparation allocate; ready service/span packet loops reuse managed
/// storage. ENet/FastLZ are unchanged native dependency sources; native allocator totals remain separate.</remarks>
public class ENetConnection : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private ENetHandle? _handle;
    private ENetTransport? _transport;
    private ENetPacketPeer?[] _peers = [];
    private bool _servicing, _clearing;
    private TLSOptions? _ownedOptions;
    internal ENetHandle Handle => _handle ?? throw new InvalidOperationException("ENet host is inactive.");
    /// <summary>Creates an inactive ENet host owner.</summary>
    public ENetConnection() { }
    internal void CheckOwner() { if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("ENet host access requires its constructing thread."); }
    private void Check(bool active = true) { ThrowIfDisposed(); CheckOwner(); if (_clearing) throw new InvalidOperationException("ENet cleanup cannot reenter host operations."); if (active && _handle is null) throw new InvalidOperationException("ENet host is inactive."); }
    /// <summary>Creates a native host bound to an automatically selected local UDP port.</summary><param name="maxPeers">One through 4095.</param><param name="maxChannels">Zero selects 255; otherwise one through 255.</param><param name="inBandwidth">Incoming bytes/second; zero unlimited.</param><param name="outBandwidth">Outgoing bytes/second; zero unlimited.</param>
    public void CreateHost(int maxPeers = 32, int maxChannels = 0, uint inBandwidth = 0, uint outBandwidth = 0) => Create("*", 0, true, maxPeers, maxChannels, inBandwidth, outBandwidth);
    /// <summary>Creates a bound native host.</summary><param name="bindAddress">IPv4/IPv6 literal or wildcard.</param><param name="port">Zero selects an ephemeral port; otherwise zero through 65535.</param><param name="maxPeers">One through 4095.</param><param name="maxChannels">Zero selects 255.</param><param name="inBandwidth">Incoming byte rate, zero unlimited.</param><param name="outBandwidth">Outgoing byte rate, zero unlimited.</param>
    public void CreateHostBound(string bindAddress, int port, int maxPeers = 32, int maxChannels = 0, uint inBandwidth = 0, uint outBandwidth = 0) => Create(bindAddress, port, true, maxPeers, maxChannels, inBandwidth, outBandwidth);
    private void Create(string address, int port, bool bound, int peers, int channels, uint input, uint output)
    {
        Check(false); if (_handle is not null) throw new InvalidOperationException("ENet host is already active."); if (peers is < 1 or > 4095 || channels is < 0 or > 255) throw new ArgumentOutOfRangeException(nameof(peers)); NetworkSockets.Port(port); ENetNative.Prepare(); var transport = new ENetTransport(address, port, bound, peers);
        try { var pointer = ENetNative.Create(transport.ID, bound ? 1 : 0, (ushort)port, peers, channels, input, output); if (pointer != 0) _handle = new ENetHandle { Pointer = pointer }; transport.CheckError(); if (pointer == 0) throw new IOException("Native ENet host creation failed."); _transport = transport; _peers = new ENetPacketPeer?[peers]; transport.InUse = TokenInUse; }
        catch { _handle?.Dispose(); _handle = null; transport.Dispose(); throw; }
    }
    private bool TokenInUse(uint token) { if (_handle is null) return false; for (var i = 0; i < _peers.Length; i++) if (ENetNative.Peer(_handle, i, 0, 0, 0, 0) != 0 && ENetNative.Peer(_handle, i, 2, 0, 0, 0) == token) return true; return false; }
    /// <summary>Starts one outgoing connection from this host.</summary><param name="address">Native IP literal or DNS name.</param><param name="port">One through 65535.</param><param name="channels">Zero selects 255, otherwise one through 255.</param><param name="data">Remote connection-event data.</param><returns>A borrowed connecting peer; host owns its native lifetime.</returns><remarks>The host must have no current peers, matching its outgoing-client role.</remarks>
    public ENetPacketPeer ConnectToHost(string address, int port, int channels = 0, uint data = 0)
    {
        Check(); if (_servicing || _peers.Any(p => p?.Active == true)) throw new InvalidOperationException("Host has current peers or is servicing."); NetworkSockets.Port(port, true); if (channels is < 0 or > 255) throw new ArgumentOutOfRangeException(nameof(channels)); var ip = NetworkSockets.Resolve(address); var token = _transport!.Address(ip, port); _transport.ConnectTLS(token); var index = ENetNative.Connect(Handle, token, (ushort)port, channels == 0 ? 255 : channels, data); _transport.CheckError(); if (index < 0) throw new IOException("ENet peer capacity exhausted."); return _peers[index] = new ENetPacketPeer(this, index);
    }
    /// <summary>Destroys host/peer state and owned transports; idle destruction is valid.</summary>
    public void Destroy() { Check(false); if (_servicing) throw new InvalidOperationException("ENet destruction cannot reenter service."); Clear(); }
    private void Clear()
    {
        _clearing = true;
        try
        {
            var peers = _peers; _peers = []; var handle = _handle; _handle = null; var transport = _transport; _transport = null; var options = _ownedOptions; _ownedOptions = null; List<Exception>? errors = null;
            foreach (var peer in peers) if (peer is not null) try { peer.Deactivate(); } catch (Exception error) { (errors ??= []).Add(error); }
            try { handle?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
            try { transport?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
            try { options?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
            if (errors is not null) throw new AggregateException(errors);
        }
        finally { _clearing = false; }
    }
    /// <summary>Services one event, optionally waiting for network progress.</summary><param name="timeout">Nonnegative milliseconds; zero is nonblocking.</param><returns>A typed event; Receive payloads queue on Peer for inherited packet reads.</returns><exception cref="IOException">Native service or transport fails.</exception>
    public ENetEvent Service(int timeout = 0) { Check(); ArgumentOutOfRangeException.ThrowIfNegative(timeout); return ServiceCore((uint)timeout, false); }
    internal ENetEvent CheckEvents() => ServiceCore(0, true);
    private ENetEvent ServiceCore(uint timeout, bool check)
    {
        Check(); if (_servicing) throw new InvalidOperationException("ENet service cannot reenter."); _servicing = true;
        try { var result = ENetNative.Service(Handle, out var value, timeout, check ? 1 : 0); try { _transport!.CheckError(); } catch { if (value.Packet != 0) ENetNative.ReleasePacket(value.Packet); throw; } if (result < 0) throw new IOException("Native ENet service failed."); if (result == 0 || value.Type == 0) return default; var peer = _peers[value.Peer] ??= new ENetPacketPeer(this, value.Peer); if (value.Type == 3) peer.Queue(value.Packet); else if (value.Type == 2) Detach(peer); return new((ENetEventType)value.Type, peer, value.Data, value.Channel); }
        finally { _servicing = false; }
    }
    /// <summary>Flushes queued outgoing protocol data without waiting for events.</summary>
    public void Flush() { Check(); ENetNative.Flush(Handle); _transport!.CheckError(); }
    /// <summary>Sets native incoming/outgoing rate limits.</summary><param name="inBandwidth">Bytes/second; zero unlimited.</param><param name="outBandwidth">Bytes/second; zero unlimited.</param>
    public void BandwidthLimit(uint inBandwidth = 0, uint outBandwidth = 0) { Check(); ENetNative.Host(Handle, 1, inBandwidth, outBandwidth); }
    /// <summary>Sets the channel limit for future connections.</summary><param name="maxChannels">Zero selects 255; otherwise one through 255.</param>
    public void ChannelLimit(int maxChannels) { Check(); if (maxChannels is < 0 or > 255) throw new ArgumentOutOfRangeException(nameof(maxChannels)); ENetNative.Host(Handle, 2, (uint)maxChannels, 0); }
    /// <summary>Runs native bandwidth-throttle adaptation.</summary>
    public void BandwidthThrottle() { Check(); ENetNative.Host(Handle, 3, 0, 0); }
    /// <summary>Replaces the native codec used for subsequent packets.</summary><param name="mode">The ENet compression domain; peers must agree.</param><remarks>Codec setup allocates; the prepared native adapter uses bounded buffers.</remarks>
    public void Compress(ENetCompressionMode mode) { Check(); if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode)); if (ENetNative.Compress(Handle, (int)mode) != 0) throw new IOException("ENet codec preparation failed."); }
    /// <summary>Returns and resets one native host statistic.</summary><param name="statistic">Host statistic domain.</param><returns>The counter before reset as an exact double.</returns>
    public double PopStatistic(ENetHostStatistic statistic) { Check(); if (!Enum.IsDefined(statistic)) throw new ArgumentOutOfRangeException(nameof(statistic)); return ENetNative.Statistic(Handle, -1, (int)statistic); }
    /// <summary>Returns the current native channel limit.</summary><returns>One through 255.</returns>
    public int GetMaxChannels() { Check(); return (int)ENetNative.Host(Handle, 0, 0, 0); }
    /// <summary>Returns the bound/auto-bound local port.</summary><returns>The active host local UDP port.</returns>
    public int GetLocalPort() { Check(); return _transport!.LocalPort; }
    /// <summary>Copies the current logical peer membership.</summary><returns>Borrowed peers, including connecting/disconnecting entries.</returns>
    public ENetPacketPeer[] GetPeers() { Check(); return _peers.Where(p => p?.Active == true).Cast<ENetPacketPeer>().ToArray(); }
    /// <summary>Rejects previously unknown inbound endpoints while preserving current peers.</summary><param name="refuse">Whether to refuse new admission.</param>
    public void RefuseNewConnections(bool refuse) { Check(); ENetNative.Host(Handle, 4, refuse ? 1u : 0u, 0); _transport!.Refuse = refuse; }
    /// <summary>Upgrades an idle bound host to DTLS server transport.</summary><param name="serverOptions">Server key/certificate configuration.</param>
    public void DTLSServerSetup(TLSOptions serverOptions) { Check(); if (_peers.Any(p => p?.Active == true)) throw new InvalidOperationException("Configure DTLS before peers."); _transport!.ServerTLS(serverOptions); }
    /// <summary>Selects validating/unsafe DTLS for the next outgoing connection.</summary><param name="hostname">Expected DNS/IP identity.</param><param name="clientOptions">Client options, null selects retained default system-trust options.</param>
    public void DTLSClientSetup(string hostname, TLSOptions? clientOptions = null) { Check(); ArgumentNullException.ThrowIfNull(hostname); if (_peers.Any(p => p?.Active == true)) throw new InvalidOperationException("Configure DTLS before peers."); if (clientOptions is null) { var owned = TLSOptions.Client(); try { _transport!.ClientTLS(hostname, owned); _ownedOptions = owned; } catch { owned.Dispose(); throw; } } else _transport!.ClientTLS(hostname, clientOptions); }
    /// <summary>Sends one raw native transport datagram.</summary><param name="destinationAddress">IP literal or resolvable DNS name.</param><param name="destinationPort">One through 65535.</param><param name="packet">Borrowed packet bytes.</param><remarks>This bypasses ENet reliability/framing and uses configured transport encryption if applicable.</remarks>
    public void SocketSend(string destinationAddress, int destinationPort, ReadOnlySpan<byte> packet) { Check(); NetworkSockets.Port(destinationPort, true); var token = _transport!.Address(NetworkSockets.Resolve(destinationAddress), destinationPort); _transport.SocketSend(token, packet); }
    /// <summary>Queues a packet to every currently connected peer.</summary><param name="channel">Native channel.</param><param name="packet">Borrowed bytes copied for each recipient.</param><param name="flags">ENet packet flags.</param>
    public void Broadcast(int channel, ReadOnlySpan<byte> packet, ENetPacketFlags flags) { Check(); List<Exception>? errors = null; foreach (var peer in _peers) if (peer?.GetState() == ENetPeerState.Connected) try { peer.Send(channel, packet, flags); } catch (Exception error) { (errors ??= []).Add(error); } if (errors is not null) throw new AggregateException(errors); }
    internal ENetEndpoint Endpoint(uint token) => _transport!.Endpoint(token);
    internal uint Peer(int index, int operation, uint a = 0, uint b = 0, uint c = 0) { Check(); var result = ENetNative.Peer(Handle, index, operation, a, b, c); _transport!.CheckError(); return result; }
    internal void Send(ENetPacketPeer peer, int channel, ReadOnlySpan<byte> data, ENetPacketFlags flags) { if (ENetNative.SendPacket(Handle, peer.Index, (byte)channel, data, (uint)flags) < 0) throw new IOException("ENet could not queue the complete packet."); _transport!.CheckError(); }
    internal void Detach(ENetPacketPeer peer) { peer.Deactivate(); if (ReferenceEquals(_peers[peer.Index], peer)) _peers[peer.Index] = null; }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); CheckOwner(); if (_servicing) throw new InvalidOperationException("ENet disposal cannot reenter service."); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) Clear(); base.Dispose(disposing); }
}
