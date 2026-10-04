using System.Net;
using System.Net.Sockets;
namespace Electron2D;

/// <summary>Routes UDP sender endpoints into independent caller-owned packet peers.</summary>
/// <remarks>Poll receives packets and queues new endpoints up to MaxPendingConnections. Pending peers are
/// server-owned; TakeConnection transfers logical ownership and the server keeps only a weak reference.
/// Stop closes the shared socket and detaches accepted peers. Existing accepted endpoints continue receiving
/// when the pending limit is zero. Receive queues retain packet boundaries and drop packets that exceed their budget.</remarks>
public class UDPServer : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private Socket? _socket;
    private SocketAddress? _receiveAddress;
    private readonly byte[] _receive = new byte[65536];
    private readonly Dictionary<DatagramAddress, WeakReference<PacketPeerUDP>> _peers = [];
    private readonly List<PacketPeerUDP> _pending = [];
    private int _maxPendingConnections = 16, _port;
    /// <summary>Creates a stopped UDP listener.</summary>
    public UDPServer() { }
    private void CheckServer() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("UDP listener access requires its constructing thread."); }
    /// <summary>Gets or sets the maximum queued new endpoints.</summary><value>16 initially; zero rejects new endpoints while preserving accepted ones. Lowering trims newest pending peers.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int MaxPendingConnections
    {
        get { CheckServer(); return _maxPendingConnections; }
        set { CheckServer(); ArgumentOutOfRangeException.ThrowIfNegative(value); _maxPendingConnections = value; while (_pending.Count > value) { var peer = _pending[^1]; _pending.RemoveAt(_pending.Count - 1); peer.Dispose(); } }
    }
    /// <summary>Listens on a UDP port and IP bind address.</summary><param name="port">Zero through 65535; zero selects an ephemeral port.</param><param name="bindAddress">IP literal or wildcard.</param>
    public void Listen(int port, string bindAddress = "*")
    {
        CheckServer(); NetworkSockets.Port(port); if (_socket is not null) throw new InvalidOperationException("The UDP listener is already open.");
        var address = NetworkSockets.BindAddress(bindAddress); var socket = NetworkSockets.Create(address.AddressFamily, SocketType.Dgram);
        try { socket.Bind(new IPEndPoint(address, port)); _port = ((IPEndPoint)socket.LocalEndPoint!).Port; _receiveAddress = new SocketAddress(socket.AddressFamily); _socket = socket; } catch { socket.Dispose(); throw; }
    }
    /// <summary>Receives all immediately available datagrams and routes them by remote endpoint.</summary>
    /// <exception cref="InvalidOperationException">The listener is stopped.</exception>
    public void Poll()
    {
        CheckServer(); if (_socket is null) throw new InvalidOperationException("A listening UDP socket is required.");
        while (_socket.Poll(0, SelectMode.SelectRead))
        {
            int count;
            try { count = _socket.ReceiveFrom(_receive, SocketFlags.None, _receiveAddress!); }
            catch (SocketException error) when (NetworkSockets.Busy(error.SocketErrorCode)) { break; }
            var address = DatagramAddress.Capture(_receiveAddress!); PacketPeerUDP? peer = null;
            if (_peers.TryGetValue(address, out var weak)) { if (!weak.TryGetTarget(out peer) || peer.IsDisposed) { _peers.Remove(address); peer = null; } }
            if (peer is null)
            {
                if (_pending.Count >= _maxPendingConnections) continue;
                peer = new PacketPeerUDP();
                try { peer.Attach(this, _socket, address, _port); _peers.Add(address, new(peer)); _pending.Add(peer); } catch { peer.Dispose(); throw; }
            }
            peer.Store(address, _receive.AsSpan(0, count));
        }
    }
    /// <summary>Reports whether a pending endpoint can be taken; does not poll.</summary><returns>True when the pending queue is nonempty.</returns>
    public bool IsConnectionAvailable() { CheckServer(); return _pending.Count > 0; }
    /// <summary>Reports whether the native UDP listener is open.</summary><returns>True while listening.</returns>
    public bool IsListening() { CheckServer(); return _socket is not null; }
    /// <summary>Returns the native listener's local port or zero while stopped.</summary><returns>The port.</returns>
    public int GetLocalPort() { CheckServer(); return _socket is null ? 0 : _port; }
    /// <summary>Transfers the oldest pending endpoint, including its first queued packet.</summary><returns>A caller-owned peer, or null.</returns>
    public PacketPeerUDP? TakeConnection() { CheckServer(); if (_pending.Count == 0) return null; var peer = _pending[0]; _pending.RemoveAt(0); return peer; }
    /// <summary>Closes the shared socket, disposes pending peers and detaches accepted peers.</summary>
    public void Stop() { CheckServer(); StopCore(); }
    private void StopCore()
    {
        var socket = _socket; _socket = null; _receiveAddress = null; _port = 0; socket?.Dispose();
        foreach (var weak in _peers.Values) if (weak.TryGetTarget(out var peer)) peer.DetachServer();
        _peers.Clear(); foreach (var peer in _pending) peer.Dispose(); _pending.Clear();
    }
    internal void RemovePeer(DatagramAddress address, PacketPeerUDP peer)
    {
        if (_peers.TryGetValue(address, out var weak) && weak.TryGetTarget(out var existing) && ReferenceEquals(existing, peer)) _peers.Remove(address);
        _pending.Remove(peer);
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); CheckServer(); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<UDPServer, int>(nameof(MaxPendingConnections), s => s.MaxPendingConnections, (s, v) => s.MaxPendingConnections = v, _ => 16, stored: true);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) StopCore(); base.Dispose(disposing); }
}
