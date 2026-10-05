using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
namespace Electron2D;

/// <summary>Transfers complete UDP datagrams with bounded queued receive storage.</summary>
/// <remarks>Standalone peers poll while reading/counting packets. Server-created peers share the listener and
/// receive only their endpoint's packets. Closing such a peer detaches it without closing the listener.
/// Bind/connection/destination resolution and first endpoint queries are cold operations; caller-span packet cycles reuse storage.
/// Native send/receive buffers hold at least 65536 bytes, preserving larger platform defaults.</remarks>
public class PacketPeerUDP : PacketPeer
{
    internal long ConnectionGeneration { get; private set; }
    internal DatagramAddress ConnectedAddress { get { CheckPacketPeer(); if (!_connected || _remote is null) throw new InvalidOperationException("A connected UDP endpoint is required."); return DatagramAddress.Capture(_remote); } }
    private Socket? _socket;
    private SocketAddress? _remote, _receiveAddress;
    private IPAddress? _destinationAddress;
    private int _destinationPort;
    private UDPServer? _server;
    private DatagramAddress _serverAddress, _lastAddress;
    private readonly DatagramQueue _queue = new();
    private readonly byte[] _receive = new byte[65536];
    private bool _connected, _broadcast;
    private int _localPort;
    private string? _lastHost;
    /// <summary>Creates an unopened UDP peer with a 65536-byte receive queue.</summary>
    public PacketPeerUDP() { _queue.Prepare(65536); }
    /// <summary>Binds a native local UDP socket and configures its receive queue.</summary><param name="port">Zero through 65535; zero requests an ephemeral port.</param><param name="bindAddress">IP literal or wildcard.</param><param name="receiveBufferSize">Zero through 64 MiB, rounded up to a power of two; each queued packet charges 24 metadata bytes.</param>
    public void Bind(int port, string bindAddress = "*", int receiveBufferSize = 65536)
    {
        CheckPacketPeer(); NetworkSockets.Port(port); if (_socket is not null) throw new InvalidOperationException("The UDP socket is already open.");
        var address = NetworkSockets.BindAddress(bindAddress); _queue.Prepare(receiveBufferSize);
        var socket = NetworkSockets.Create(address.AddressFamily, SocketType.Dgram);
        try { socket.EnableBroadcast = _broadcast; socket.Bind(new IPEndPoint(address, port)); Open(socket); } catch { socket.Dispose(); throw; }
    }
    private void Open(Socket socket) { _socket = socket; _receiveAddress = new SocketAddress(socket.AddressFamily); _localPort = socket.LocalEndPoint is IPEndPoint endpoint ? endpoint.Port : 0; }
    private Socket SocketFor(IPAddress address)
    {
        if (_socket is null) { var socket = NetworkSockets.Create(address.AddressFamily, SocketType.Dgram); try { socket.EnableBroadcast = _broadcast; Open(socket); } catch { socket.Dispose(); throw; } }
        return _socket!;
    }
    /// <summary>Chooses a UDP send destination without connecting the socket.</summary><param name="host">IP literal or resolvable name.</param><param name="port">Remote UDP port, zero through 65535.</param>
    public void SetDestAddress(string host, int port)
    {
        CheckPacketPeer(); NetworkSockets.Port(port); if (_connected) throw new InvalidOperationException("A connected UDP destination cannot change.");
        var address = NetworkSockets.Resolve(host); _destinationAddress = address; _destinationPort = port; _remote = _socket is null ? null : NetworkSockets.Endpoint(_socket, address, port).Serialize();
    }
    /// <summary>Connects UDP to one remote IP endpoint, filtering other senders.</summary><param name="host">Remote IP literal.</param><param name="port">Remote port, zero through 65535.</param>
    public void ConnectToHost(string host, int port)
    {
        CheckPacketPeer(); NetworkSockets.Port(port); if (_connected) throw new InvalidOperationException("UDP is already connected.");
        if (!IPAddress.TryParse(host, out var address)) throw new ArgumentException("UDP ConnectToHost requires an IP literal.", nameof(host));
        var socket = SocketFor(address); var endpoint = NetworkSockets.Endpoint(socket, address, port);
        try { socket.Connect(endpoint); _remote = endpoint.Serialize(); _connected = true; _destinationAddress = address; _destinationPort = port; _queue.Clear(); _localPort = ((IPEndPoint)socket.LocalEndPoint!).Port; } catch { CloseCore(); throw; }
    }
    /// <summary>Reports whether a local UDP socket is open.</summary><returns>True after binding or implicit send/connection opening.</returns>
    public bool IsBound() { CheckPacketPeer(); return _socket is not null; }
    /// <summary>Reports whether UDP filters to one connected endpoint.</summary><returns>True for a native connection or server-created peer.</returns>
    public bool IsSocketConnected() { CheckPacketPeer(); return _connected; }
    /// <summary>Returns the current local port or zero while closed.</summary><returns>The port.</returns>
    public int GetLocalPort() { CheckPacketPeer(); return _socket is null ? 0 : _localPort; }
    /// <summary>Returns the sender IP of the last consumed packet.</summary><returns>A normalized IP literal; empty before receipt.</returns>
    public string GetPacketIP() { CheckPacketPeer(); return _lastHost ??= (_lastAddress == default ? "" : NetworkSockets.Host(_lastAddress.Address())); }
    /// <summary>Returns the sender port of the last consumed packet.</summary><returns>The port, zero initially.</returns>
    public int GetPacketPort() { CheckPacketPeer(); return _lastAddress.Port; }
    /// <summary>Enables or disables native UDP broadcasting.</summary><param name="enabled">Whether broadcast sends are allowed.</param>
    public void SetBroadcastEnabled(bool enabled) { CheckPacketPeer(); if (_server is not null) throw new InvalidOperationException("Shared UDP peers cannot change listener options."); _broadcast = enabled; if (_socket is not null) _socket.EnableBroadcast = enabled; }
    /// <summary>Closes or detaches UDP state, clears packets and restores default queue capacity.</summary>
    public void Close() { CheckPacketPeer(); CloseCore(); _queue.Prepare(65536); }
    private void CloseCore()
    {
        ConnectionGeneration++; var server = _server; _server = null; if (server is not null) server.RemovePeer(_serverAddress, this); else _socket?.Dispose();
        _socket = null; _remote = null; _receiveAddress = null; _connected = false; _localPort = 0; _queue.Clear();
    }
    private void PollPackets()
    {
        if (_server is not null) { _server.Poll(); return; }
        if (_socket is null) return;
        while (_socket.Poll(0, SelectMode.SelectRead))
        {
            int received;
            try { received = _socket.ReceiveFrom(_receive, SocketFlags.None, _receiveAddress!); }
            catch (SocketException error) when (NetworkSockets.Busy(error.SocketErrorCode)) { break; }
            _queue.Store(DatagramAddress.Capture(_receiveAddress!), _receive.AsSpan(0, received));
        }
    }
    /// <inheritdoc />
    public override int GetAvailablePacketCount() { CheckPacketPeer(); PollPackets(); return _queue.Count; }
    /// <inheritdoc />
    protected override int NextPacketSize() { PollPackets(); return _queue.NextSize; }
    /// <inheritdoc />
    protected override void ReadPacketCore(Span<byte> destination) { var address = _queue.Take(destination); if (address != _lastAddress) { _lastAddress = address; _lastHost = null; } }
    /// <inheritdoc />
    public override int GetMaxPacketSize() { CheckPacketPeer(); return 65507; }
    /// <inheritdoc />
    public override void PutPacket(ReadOnlySpan<byte> data)
    {
        CheckPacketPeer(); if (_destinationAddress is null && _remote is null) throw new InvalidOperationException("A UDP destination is required.");
        if (_socket is null) { var socket = SocketFor(_destinationAddress!); _remote = NetworkSockets.Endpoint(socket, _destinationAddress!, _destinationPort).Serialize(); }
        var native = _socket!; var remote = _remote ??= NetworkSockets.Endpoint(native, _destinationAddress!, _destinationPort).Serialize();
        if (!native.IsBound) native.Bind(new IPEndPoint(native.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0));
        while (true)
        {
            try { var sent = _connected && _server is null ? native.Send(data, SocketFlags.None) : native.SendTo(data, SocketFlags.None, remote); if (sent != data.Length) throw new IOException("A UDP packet was not sent completely."); break; }
            catch (SocketException error) when (NetworkSockets.Busy(error.SocketErrorCode)) { native.Poll(-1, SelectMode.SelectWrite); }
        }
        if (_localPort == 0) _localPort = ((IPEndPoint)native.LocalEndPoint!).Port;
    }
    /// <summary>Polls, then waits for one native readable notification if this peer has no queued packet.</summary>
    /// <exception cref="InvalidOperationException">The UDP socket is closed.</exception>
    /// <remarks>Queue overflow or traffic for another shared-server endpoint can leave this peer empty after the notification.</remarks>
    public void Wait()
    {
        CheckPacketPeer(); if (_socket is null) throw new InvalidOperationException("An open UDP socket is required.");
        if (_queue.Count > 0) return; PollPackets(); if (_queue.Count > 0) return; _socket.Poll(-1, SelectMode.SelectRead); PollPackets();
    }
    /// <summary>Joins a multicast group on a named native interface.</summary><param name="multicastAddress">Multicast IP literal.</param><param name="interfaceName">Native interface name or identifier; empty chooses the default interface.</param>
    public void JoinMulticastGroup(string multicastAddress, string interfaceName) => Membership(multicastAddress, interfaceName, true);
    /// <summary>Leaves a previously joined multicast group.</summary><param name="multicastAddress">Multicast IP literal.</param><param name="interfaceName">Native interface name or identifier; empty chooses the default interface.</param>
    public void LeaveMulticastGroup(string multicastAddress, string interfaceName) => Membership(multicastAddress, interfaceName, false);
    private void Membership(string host, string interfaceName, bool join)
    {
        CheckPacketPeer(); ArgumentNullException.ThrowIfNull(interfaceName); if (_server is not null) throw new InvalidOperationException("Shared UDP peers cannot change multicast membership.");
        if (!IPAddress.TryParse(host, out var address)) throw new ArgumentException("A multicast IP literal is required.", nameof(host));
        if (!join && _socket is null) throw new InvalidOperationException("Leaving multicast requires an open socket.");
        var native = SocketFor(address);
        NetworkInterface? nic = null;
        if (interfaceName.Length > 0) { foreach (var item in NetworkInterface.GetAllNetworkInterfaces()) if (item.Name == interfaceName || item.Id == interfaceName) { nic = item; break; } if (nic is null) throw new ArgumentException("The network interface was not found.", nameof(interfaceName)); }
        var option = join ? SocketOptionName.AddMembership : SocketOptionName.DropMembership;
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var first = address.GetAddressBytes()[0]; if (first < 224 || first > 239) throw new ArgumentException("The IP address is not multicast.", nameof(host));
            var local = IPAddress.Any; if (nic is not null) { var found = false; foreach (var unicast in nic.GetIPProperties().UnicastAddresses) if (unicast.Address.AddressFamily == AddressFamily.InterNetwork) { local = unicast.Address; found = true; break; } if (!found) throw new ArgumentException("The interface has no IPv4 address.", nameof(interfaceName)); }
            native.SetSocketOption(SocketOptionLevel.IP, option, new MulticastOption(address, local));
        }
        else { if (!address.IsIPv6Multicast) throw new ArgumentException("The IP address is not multicast.", nameof(host)); native.SetSocketOption(SocketOptionLevel.IPv6, option, new IPv6MulticastOption(address, nic?.GetIPProperties().GetIPv6Properties()?.Index ?? 0)); }
    }
    internal void Attach(UDPServer server, Socket socket, DatagramAddress address, int localPort)
    {
        ConnectionGeneration++; _server = server; _socket = socket; _serverAddress = address; _connected = true; _localPort = localPort;
        _lastAddress = address; _lastHost = null; _destinationAddress = address.Address(); _destinationPort = address.Port; _remote = NetworkSockets.Endpoint(socket, address.Address(), address.Port).Serialize();
    }
    internal void Store(DatagramAddress address, ReadOnlySpan<byte> packet) => _queue.Store(address, packet);
    internal void DetachServer() { ConnectionGeneration++; _server = null; _socket = null; _remote = null; _connected = false; _localPort = 0; _queue.Clear(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) CloseCore(); base.Dispose(disposing); }
}
