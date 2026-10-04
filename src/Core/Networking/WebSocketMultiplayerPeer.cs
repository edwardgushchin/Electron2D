using System.Buffers.Binary;
using System.Security.Authentication;
namespace Electron2D;

/// <summary>Runs multiplayer server/client endpoints over reliable single-channel WebSocket messages.</summary>
/// <remarks>Poll completes the HTTP/TLS upgrade and four-byte little-endian client identity assignment before
/// connection events. Server targeting supports broadcast, positive IDs and negative exclusions; clients send to
/// the server. Transfer mode/channel settings are retained but actual packets are Reliable/channel zero. The
/// higher multiplayer layer, rather than this transport, owns peer-to-peer relay and scene replication.</remarks>
public class WebSocketMultiplayerPeer : MultiplayerPeer
{
    private readonly WebSocketPeer _config = new();
    private readonly List<WebSocketMultiplayerConnection> _connections = new(16);
    private readonly Dictionary<int, WebSocketMultiplayerConnection> _byID = new(16);
    private readonly WebSocketPacketQueue _incoming = new();
    private byte[] _scratch = [];
    private TCPServer? _listener;
    private TLSOptions? _serverTLS;
    private int _uniqueID, _target, _generation;
    private bool _polling, _closing;
    private double _timeout = 3;
    private MultiplayerConnectionStatus _status;
    /// <summary>Creates a disconnected multiplayer endpoint with default WebSocket settings.</summary>
    public WebSocketMultiplayerPeer() { }
    private void Idle() { CheckPacketPeer(); if (_closing || _status != MultiplayerConnectionStatus.Disconnected) throw new InvalidOperationException("Configure or create an endpoint while disconnected and outside cleanup."); }
    /// <summary>Gets or sets copied subprotocol tokens used for future connections.</summary><value>Empty initially; connected transports retain their prepared configuration.</value>
    public string[] SupportedProtocols { get { CheckPacketPeer(); return _config.SupportedProtocols; } set { CheckPacketPeer(); _config.SupportedProtocols = value; } }
    /// <summary>Gets or sets copied extra upgrade headers for future connections.</summary><value>Empty initially; validation follows WebSocketPeer.</value>
    public string[] HandshakeHeaders { get { CheckPacketPeer(); return _config.HandshakeHeaders; } set { CheckPacketPeer(); _config.HandshakeHeaders = value; } }
    /// <summary>Gets or sets per-connection input and aggregate queued-payload capacity.</summary><value>65535 initially; at most 64 MiB while disconnected. Connection setup requires at least four bytes for identity.</value>
    public int InboundBufferSize { get { CheckPacketPeer(); return _config.InboundBufferSize; } set { Idle(); _config.InboundBufferSize = value; } }
    /// <summary>Gets or sets the per-connection outgoing payload budget.</summary><value>65535 initially; zero selects 64 MiB. Configurable while disconnected; setup needs four identity bytes.</value>
    public int OutboundBufferSize { get { CheckPacketPeer(); return _config.OutboundBufferSize; } set { Idle(); _config.OutboundBufferSize = value; } }
    /// <summary>Gets or sets per-connection and aggregate queued-message capacity.</summary><value>4096 initially; up to 65536 while disconnected. Setup requires at least one identity-message slot.</value>
    public int MaxQueuedPackets { get { CheckPacketPeer(); return _config.MaxQueuedPackets; } set { Idle(); _config.MaxQueuedPackets = value; } }
    /// <summary>Gets or sets the whole connection/identity handshake deadline in monotonic seconds.</summary><value>Three initially; finite positive values. Live changes apply to all pending connections.</value>
    /// <exception cref="ArgumentOutOfRangeException">The timeout is nonpositive, nonfinite or exceeds the clock range.</exception>
    public double HandshakeTimeout { get { CheckPacketPeer(); return _timeout; } set { CheckPacketPeer(); if (!double.IsFinite(value) || value <= 0 || value > long.MaxValue / 1000d) throw new ArgumentOutOfRangeException(nameof(value)); _timeout = value; } }
    private void Prepare()
    {
        if (InboundBufferSize < 4 || _config.GetMaxPacketSize() < 4 || MaxQueuedPackets < 1) throw new InvalidOperationException("Identity assignment requires four-byte buffers and one packet slot.");
        _scratch = new byte[InboundBufferSize]; _incoming.Prepare(InboundBufferSize, MaxQueuedPackets);
    }
    private WebSocketPeer NewPeer() => new() { SupportedProtocols = _config.SupportedProtocols, HandshakeHeaders = _config.HandshakeHeaders, InboundBufferSize = InboundBufferSize, OutboundBufferSize = OutboundBufferSize, MaxQueuedPackets = MaxQueuedPackets };
    /// <summary>Starts a WS/WSS multiplayer client.</summary><param name="url">Server URL; credentials are forbidden.</param><param name="tlsClientOptions">Borrowed client TLS options, or null for normal trust/name validation.</param>
    /// <exception cref="InvalidOperationException">Already active or prepared capacities cannot carry identity assignment.</exception>
    public void CreateClient(string url, TLSOptions? tlsClientOptions = null)
    {
        Idle(); if (tlsClientOptions?.IsServer() == true) throw new ArgumentException("Client TLS options are required.", nameof(tlsClientOptions));
        CloseCore(); Prepare(); var peer = NewPeer();
        try { peer.ConnectToURL(url, tlsClientOptions); var connection = new WebSocketMultiplayerConnection(TargetPeerServer, peer); _connections.Add(connection); _byID.Add(TargetPeerServer, connection); _status = MultiplayerConnectionStatus.Connecting; }
        catch { peer.Dispose(); CloseCore(); throw; }
    }
    /// <summary>Starts an owned native TCP/TLS multiplayer listener.</summary><param name="port">Local port; zero requests an ephemeral port, otherwise the caller chooses the addressable port.</param><param name="bindAddress">IP literal or wildcard.</param><param name="tlsServerOptions">Borrowed server identity options, or null for WS.</param>
    /// <exception cref="ArgumentException">The supplied TLS options describe a client.</exception>
    public void CreateServer(int port, string bindAddress = "*", TLSOptions? tlsServerOptions = null)
    {
        Idle(); if (tlsServerOptions is not null && !tlsServerOptions.IsServer()) throw new ArgumentException("Server TLS options are required.", nameof(tlsServerOptions));
        CloseCore(); Prepare(); var listener = new TCPServer();
        try { listener.Listen(port, bindAddress); _listener = listener; _serverTLS = tlsServerOptions; _uniqueID = TargetPeerServer; _status = MultiplayerConnectionStatus.Connected; }
        catch { listener.Dispose(); CloseCore(); throw; }
    }
    /// <summary>Gets an associated transport without transferring ownership.</summary><param name="peerID">Remote identity; client ID one is available while connecting.</param><returns>A borrowed peer, or null when absent. Direct reading/disposal can disrupt the owning multiplayer protocol.</returns>
    public WebSocketPeer? GetPeer(int peerID) { CheckPacketPeer(); return _byID.TryGetValue(peerID, out var connection) ? connection.Peer : null; }
    /// <summary>Gets a connected peer's native remote address.</summary><param name="id">Remote peer identity.</param><returns>IP literal or empty when absent.</returns>
    public string GetPeerAddress(int id) { CheckPacketPeer(); return GetPeer(id)?.GetConnectedHost() ?? ""; }
    /// <summary>Gets a connected peer's native remote port.</summary><param name="id">Remote peer identity.</param><returns>Port or zero when absent.</returns>
    public int GetPeerPort(int id) { CheckPacketPeer(); return GetPeer(id)?.GetConnectedPort() ?? 0; }
    /// <inheritdoc />
    public override void SetTargetPeer(int id) { CheckPacketPeer(); if (id == int.MinValue) throw new ArgumentOutOfRangeException(nameof(id)); _target = id; }
    /// <inheritdoc />
    public override int GetPacketPeer() { CheckPacketPeer(); return _incoming.Count == 0 ? TargetPeerServer : _incoming.NextSource; }
    /// <inheritdoc />
    public override int GetPacketChannel() { CheckPacketPeer(); return 0; }
    /// <inheritdoc />
    public override TransferMode GetPacketMode() { CheckPacketPeer(); return Electron2D.TransferMode.Reliable; }
    /// <inheritdoc />
    public override int GetUniqueID() { CheckPacketPeer(); return _uniqueID; }
    /// <inheritdoc />
    public override MultiplayerConnectionStatus GetConnectionStatus() { CheckPacketPeer(); return _status; }
    /// <inheritdoc />
    public override bool IsServer() { CheckPacketPeer(); return _listener is not null; }
    /// <inheritdoc />
    public override bool IsServerRelaySupported() { CheckPacketPeer(); return true; }
    /// <inheritdoc />
    public override int GetAvailablePacketCount() { CheckPacketPeer(); return _incoming.Count; }
    /// <summary>Gets the outgoing application limit, retaining nine bytes of multiplayer protocol reserve.</summary><returns>Nonnegative effective outgoing budget minus nine; zero output configuration uses the prepared 64 MiB ceiling.</returns>
    public override int GetMaxPacketSize() { CheckPacketPeer(); return Math.Max(0, _config.GetMaxPacketSize() - 9); }
    /// <inheritdoc />
    protected override int NextPacketSize() => _status == MultiplayerConnectionStatus.Connected ? _incoming.NextSize : -1;
    /// <inheritdoc />
    protected override void ReadPacketCore(Span<byte> destination) => _incoming.Take(destination);
    /// <summary>Sends a copied binary message to the configured target.</summary><param name="data">Borrowed application bytes; empty messages are ignored.</param>
    /// <exception cref="InvalidOperationException">Disconnected, target queue is full or a target is closing.</exception>
    /// <exception cref="ArgumentException">Payload exceeds the advertised limit or the positive server target is absent.</exception>
    /// <exception cref="AggregateException">Broadcast recipients failed after all selected recipients were attempted; successful recipients retain their packets.</exception>
    public override void PutPacket(ReadOnlySpan<byte> data)
    {
        CheckPacketPeer(); if (_status != MultiplayerConnectionStatus.Connected) throw new InvalidOperationException("A connected multiplayer endpoint is required.");
        if (data.Length > GetMaxPacketSize()) throw new ArgumentException("Packet exceeds the multiplayer payload limit.", nameof(data)); if (data.IsEmpty) return;
        if (_listener is null) { _byID[TargetPeerServer].Peer.PutPacket(data); return; }
        if (_target > 0) { if (!_byID.TryGetValue(_target, out var target)) throw new ArgumentException("The target peer is absent."); target.Peer.PutPacket(data); return; }
        List<Exception>? errors = null; foreach (var connection in _connections) if (connection.Ready && !connection.Retired && connection.ID != -_target) { try { connection.Peer.PutPacket(data); } catch (Exception error) { (errors ??= []).Add(error); } }
        if (errors is not null) throw new AggregateException(errors);
    }
    /// <summary>Advances admission, identity assignment, message queues and transport/close lifecycle without waiting.</summary>
    /// <remarks>Transport failure drops only that connection. Events see committed state, all subscribers run, and
    /// callback failures aggregate after remaining peers. Recursive Poll rejects; callback Close/recreation invalidates
    /// the old iteration. Deadline includes an open socket still waiting for its identity message.</remarks>
    /// <exception cref="AggregateException">Connection-event subscribers failed.</exception>
    /// <exception cref="InvalidOperationException">Poll reentered itself.</exception>
    public override void Poll()
    {
        CheckPacketPeer(); if (_polling) throw new InvalidOperationException("Multiplayer polling cannot reenter itself."); if (_status == MultiplayerConnectionStatus.Disconnected) return;
        _polling = true; var generation = _generation; List<Exception>? errors = null;
        try
        {
            if (_listener is not null && !RefuseNewConnections && _listener.IsConnectionAvailable()) Accept();
            var count = _connections.Count; Span<byte> identity = stackalloc byte[4];
            for (var i = 0; i < count && generation == _generation && !IsDisposed; i++)
            {
                var connection = _connections[i]; if (connection.Retired) continue; var connectedEvent = false; var drop = false;
                try
                {
                    connection.Peer.Poll(); var state = connection.Peer.GetReadyState();
                    if (!connection.Ready && Expired(connection)) drop = true;
                    else if (state == WebSocketState.Closed) drop = true;
                    else if (!connection.Ready && state == WebSocketState.Open)
                    {
                        if (_listener is not null)
                        {
                            if (RefuseNewConnections) drop = true;
                            else { BinaryPrimitives.WriteInt32LittleEndian(identity, connection.ID); connection.Peer.PutPacket(identity); connection.Ready = true; _byID.Add(connection.ID, connection); connectedEvent = true; }
                        }
                        else if (connection.Peer.GetAvailablePacketCount() > 0)
                        {
                            if (connection.Peer.NextMessageSize != 4) drop = true;
                            else { connection.Peer.GetPacket(_scratch.AsSpan(0, 4)); var id = BinaryPrimitives.ReadInt32LittleEndian(_scratch); if (connection.Peer.WasStringPacket() || id < 2) drop = true; else { _uniqueID = id; connection.Ready = true; _status = MultiplayerConnectionStatus.Connected; connectedEvent = true; } }
                        }
                    }
                }
                catch (Exception error) when (TransportFailure(error)) { drop = true; }
                if (drop) { var emit = connection.Ready && !connection.SuppressEvent; try { Drop(connection); if (_listener is null) CloseCore(); } catch (Exception error) { (errors ??= []).Add(error); } if (emit && !IsDisposed) Dispatch(connection.ID, false, ref errors); continue; }
                if (connectedEvent) Dispatch(connection.ID, true, ref errors);
                if (generation != _generation || IsDisposed || connection.Retired) continue;
                if (connection.Ready) Drain(connection);
            }
        }
        finally { if (generation == _generation) for (var i = _connections.Count - 1; i >= 0; i--) if (_connections[i].Retired) _connections.RemoveAt(i); _polling = false; }
        if (errors is not null) throw new AggregateException(errors);
    }
    private static bool TransportFailure(Exception error) => error is IOException or AuthenticationException or ObjectDisposedException or System.Net.Sockets.SocketException or TimeoutException or InvalidOperationException;
    private bool Expired(WebSocketMultiplayerConnection connection) => (Environment.TickCount64 - connection.Started) / 1000d >= _timeout;
    private void Accept()
    {
        var tcp = _listener!.TakeConnection(); if (tcp is null) return; var id = GenerateUniqueID(); while (_connections.Any(c => !c.Retired && c.ID == id)) id = GenerateUniqueID();
        var peer = NewPeer(); var connection = new WebSocketMultiplayerConnection(id, peer) { TCP = tcp };
        try { if (_serverTLS is not null) { var tls = new StreamPeerTLS(); connection.TLS = tls; tls.AcceptStream(tcp, _serverTLS); peer.AcceptStream(tls); } else peer.AcceptStream(tcp); _connections.Add(connection); }
        catch (Exception error) when (TransportFailure(error) || error is ArgumentException) { connection.Dispose(); }
    }
    private void Drain(WebSocketMultiplayerConnection connection)
    {
        while (!connection.Peer.IsDisposed && connection.Peer.GetAvailablePacketCount() > 0)
        {
            var size = connection.Peer.NextMessageSize;
            if (size == 0) { connection.Peer.GetPacket(Span<byte>.Empty); continue; }
            if (size > _scratch.Length || _incoming.Count >= MaxQueuedPackets || !_incoming.CanStore(size)) return;
            connection.Peer.GetPacket(_scratch.AsSpan(0, size)); _incoming.Store(_scratch.AsSpan(0, size), size, false, true, connection.ID);
        }
    }
    private void Dispatch(int id, bool connected, ref List<Exception>? errors)
    {
        try { if (connected) EmitPeerConnected(id); else EmitPeerDisconnected(id); }
        catch (Exception error) { (errors ??= []).Add(error); }
    }
    private void Drop(WebSocketMultiplayerConnection connection) { connection.Retired = true; _byID.Remove(connection.ID); connection.Dispose(); }
    /// <inheritdoc />
    public override void DisconnectPeer(int peer, bool force = false)
    {
        CheckPacketPeer(); if (!_byID.TryGetValue(peer, out var connection)) throw new ArgumentException("The remote peer is absent.", nameof(peer));
        if (!force) { if (!connection.Peer.IsDisposed) connection.Peer.Close(); return; }
        connection.SuppressEvent = true; try { Drop(connection); } finally { if (_listener is null) CloseCore(); else if (!_polling) _connections.Remove(connection); }
    }
    /// <inheritdoc />
    public override void Close() { CheckPacketPeer(); CloseCore(); }
    private void CloseCore()
    {
        if (_closing) return; _closing = true;
        try
        {
            _generation++; _status = MultiplayerConnectionStatus.Disconnected; _uniqueID = 0; _listener?.Dispose(); _listener = null; _serverTLS = null; _byID.Clear();
            List<Exception>? errors = null; foreach (var connection in _connections) { try { connection.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); } }
            _connections.Clear(); _incoming.Clear(); _scratch = [];
            if (errors is not null) throw new AggregateException(errors);
        }
        finally { _closing = false; }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<WebSocketMultiplayerPeer, int>(nameof(InboundBufferSize), p => p.InboundBufferSize, (p, v) => p.InboundBufferSize = v, _ => 65535, stored: true);
        yield return new PropertyDescriptor<WebSocketMultiplayerPeer, int>(nameof(OutboundBufferSize), p => p.OutboundBufferSize, (p, v) => p.OutboundBufferSize = v, _ => 65535, stored: true);
        yield return new PropertyDescriptor<WebSocketMultiplayerPeer, int>(nameof(MaxQueuedPackets), p => p.MaxQueuedPackets, (p, v) => p.MaxQueuedPackets = v, _ => 4096, stored: true);
        yield return new PropertyDescriptor<WebSocketMultiplayerPeer, double>(nameof(HandshakeTimeout), p => p.HandshakeTimeout, (p, v) => p.HandshakeTimeout = v, _ => 3, stored: true);
        yield return new PropertyDescriptor<WebSocketMultiplayerPeer, string[]>(nameof(SupportedProtocols), p => p.SupportedProtocols, (p, v) => p.SupportedProtocols = v, _ => [], stored: true);
        yield return new PropertyDescriptor<WebSocketMultiplayerPeer, string[]>(nameof(HandshakeHeaders), p => p.HandshakeHeaders, (p, v) => p.HandshakeHeaders = v, _ => [], stored: true);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { try { if (disposing) CloseCore(); } finally { try { if (disposing) _config.Dispose(); } finally { base.Dispose(disposing); } } }
}

internal sealed class WebSocketMultiplayerConnection : IDisposable
{
    internal readonly int ID;
    internal readonly WebSocketPeer Peer;
    internal readonly long Started = Environment.TickCount64;
    internal StreamPeerTCP? TCP;
    internal StreamPeerTLS? TLS;
    internal bool Ready, Retired, SuppressEvent;
    internal WebSocketMultiplayerConnection(int id, WebSocketPeer peer) { ID = id; Peer = peer; }
    public void Dispose() { try { Peer.Dispose(); } finally { try { TLS?.Dispose(); } finally { TCP?.Dispose(); } } }
}
