using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace Electron2D;

internal sealed class ENetEndpoint(DatagramAddress address, SocketAddress native, string host)
{
    internal readonly DatagramAddress Address = address;
    internal readonly SocketAddress Native = native;
    internal readonly string Host = host;
    internal PacketPeerUDP? UDP;
    internal PacketPeerDTLS? DTLS;
}
internal sealed class ENetTransport : IDisposable
{
    private static readonly ConcurrentDictionary<int, WeakReference<ENetTransport>> Registry = new();
    private static int _nextID;
    private readonly Dictionary<DatagramAddress, uint> _tokens = [];
    private readonly List<ENetEndpoint?> _endpoints = [null];
    private readonly Queue<uint> _free = [];
    private readonly byte[] _send = new byte[65536];
    private Socket? _socket;
    private SocketAddress _receive;
    private readonly IPAddress _bind;
    private readonly int _maxEndpoints;
    private UDPServer? _udpServer;
    private DTLSServer? _dtlsServer;
    private TLSOptions? _clientOptions;
    private string _hostname = "";
    private int _dtlsIndex = 1;
    internal readonly int ID;
    internal Exception? Error;
    internal Func<uint, bool>? InUse;
    internal bool Refuse;
    internal int LocalPort { get; private set; }
    internal ENetTransport(string bind, int port, bool bound, int peers)
    {
        _bind = NetworkSockets.BindAddress(bind); _socket = NetworkSockets.Create(_bind.AddressFamily, SocketType.Dgram); _receive = new SocketAddress(_socket.AddressFamily); _maxEndpoints = Math.Max(256, peers * 4);
        try { if (bound) { _socket.Bind(new IPEndPoint(_bind, port)); LocalPort = ((IPEndPoint)_socket.LocalEndPoint!).Port; } ID = Interlocked.Increment(ref _nextID); if (ID <= 0) throw new IOException("ENet transport IDs exhausted."); Registry[ID] = new(this); } catch { _socket.Dispose(); throw; }
    }
    ~ENetTransport() { Registry.TryRemove(ID, out _); }
    internal ENetEndpoint Endpoint(uint token) => _endpoints[checked((int)token)] ?? throw new InvalidOperationException("An ENet endpoint is no longer retained.");
    internal uint Address(IPAddress ip, int port) => Token(DatagramAddress.Capture(new IPEndPoint(_bind.AddressFamily == AddressFamily.InterNetworkV6 && ip.AddressFamily == AddressFamily.InterNetwork ? ip.MapToIPv6() : ip, port).Serialize()));
    private uint Token(DatagramAddress address)
    {
        if (_tokens.TryGetValue(address, out var found)) return found;
        if (_tokens.Count >= _maxEndpoints)
        {
            for (var i = 1; i < _endpoints.Count; i++) if (_endpoints[i] is { } old && InUse?.Invoke((uint)i) == false) { old.DTLS?.Dispose(); old.UDP?.Dispose(); _tokens.Remove(old.Address); _endpoints[i] = null; _free.Enqueue((uint)i); }
            if (_tokens.Count >= _maxEndpoints) throw new IOException("ENet endpoint preparation budget exhausted.");
        }
        var ip = address.Address(); var native = new IPEndPoint(_bind.AddressFamily == AddressFamily.InterNetworkV6 && ip.AddressFamily == AddressFamily.InterNetwork ? ip.MapToIPv6() : ip, address.Port).Serialize(); var value = new ENetEndpoint(address, native, NetworkSockets.Host(ip));
        var token = _free.Count > 0 ? _free.Dequeue() : (uint)_endpoints.Count; if (token == _endpoints.Count) _endpoints.Add(value); else _endpoints[(int)token] = value; _tokens.Add(address, token); return token;
    }
    internal void ServerTLS(TLSOptions options)
    {
        if (_udpServer is not null || _clientOptions is not null) throw new InvalidOperationException("ENet transport encryption is already configured."); var helper = new DTLSServer(); var udp = new UDPServer();
        try { helper.Setup(options); var port = LocalPort; _socket!.Dispose(); _socket = null; udp.MaxPendingConnections = _maxEndpoints; udp.Listen(port, _bind.ToString()); _dtlsServer = helper; _udpServer = udp; LocalPort = udp.GetLocalPort(); }
        catch { helper.Dispose(); udp.Dispose(); if (_socket is null) { _socket = NetworkSockets.Create(_bind.AddressFamily, SocketType.Dgram); _socket.Bind(new IPEndPoint(_bind, LocalPort)); } throw; }
    }
    internal void ClientTLS(string hostname, TLSOptions options) { if (_udpServer is not null || _clientOptions is not null) throw new InvalidOperationException("ENet encryption is already configured."); if (options.IsServer()) throw new ArgumentException("Client TLS options required.", nameof(options)); _hostname = hostname; _clientOptions = options; }
    private void SecureClient(uint token)
    {
        var entry = Endpoint(token); if (entry.DTLS is not null) return; var wire = new PacketPeerUDP(); var peer = new PacketPeerDTLS();
        try { wire.Bind(LocalPort, _bind.ToString()); wire.ConnectToHost(entry.Host, entry.Address.Port); peer.ConnectToPeer(wire, _hostname, _clientOptions); peer.SetTransportMTU(1460); entry.UDP = wire; entry.DTLS = peer; LocalPort = wire.GetLocalPort(); }
        catch { peer.Dispose(); wire.Dispose(); throw; }
    }
    internal void ConnectTLS(uint token) { if (_clientOptions is null) return; _socket?.Dispose(); _socket = null; SecureClient(token); }
    private void SecurePoll()
    {
        if (_udpServer is not null)
        {
            _udpServer.Poll(); while (_udpServer.IsConnectionAvailable()) { var wire = _udpServer.TakeConnection()!; if (Refuse) { wire.Dispose(); continue; } try { var token = Token(wire.ConnectedAddress); var entry = Endpoint(token); var peer = _dtlsServer!.TakeConnection(wire); peer.SetTransportMTU(1460); entry.UDP = wire; entry.DTLS = peer; } catch (IOException) { wire.Dispose(); } catch { wire.Dispose(); throw; } }
        }
        for (var i = 1; i < _endpoints.Count; i++) if (_endpoints[i] is { DTLS: { } secure } entry)
            {
                try { secure.Poll(); if (_udpServer is not null && secure.GetStatus() == TLSStatus.Disconnected) { entry.DTLS = null; secure.Dispose(); entry.UDP?.Dispose(); entry.UDP = null; } }
                catch (Exception error) when (_udpServer is not null && error is System.Security.Authentication.AuthenticationException or IOException or InvalidOperationException or System.Net.Sockets.SocketException) { entry.DTLS = null; secure.Dispose(); entry.UDP?.Dispose(); entry.UDP = null; }
            }
    }
    private int Send(uint token, ReadOnlySpan<byte> data)
    {
        if (_clientOptions is not null || _udpServer is not null) { SecurePoll(); var secure = Endpoint(token).DTLS; if (secure?.GetStatus() != TLSStatus.Connected) return 0; secure.PutTransportPacket(data); return data.Length; }
        var socket = _socket!; if (!socket.IsBound) { socket.Bind(new IPEndPoint(_bind, 0)); LocalPort = ((IPEndPoint)socket.LocalEndPoint!).Port; }
        return socket.SendTo(data, SocketFlags.None, Endpoint(token).Native);
    }
    private int Receive(Span<byte> data, out uint token, out ushort port)
    {
        token = 0; port = 0;
        if (_clientOptions is not null || _udpServer is not null)
        {
            SecurePoll(); for (var i = 1; i < _endpoints.Count; i++) { var index = _dtlsIndex; if (++_dtlsIndex >= _endpoints.Count) _dtlsIndex = 1; var entry = _endpoints[index]; if (entry?.DTLS?.GetAvailablePacketCount() > 0) { token = (uint)index; port = (ushort)entry.Address.Port; return entry.DTLS.GetPacket(data); } }
            return 0;
        }
        if (!_socket!.Poll(0, SelectMode.SelectRead)) return 0; var read = NetworkSockets.ReceiveDatagram(_socket, data, _receive); var address = DatagramAddress.Capture(_receive); if (Refuse && !_tokens.ContainsKey(address)) return 0; try { token = Token(address); } catch (IOException) { return 0; }
        port = (ushort)address.Port; return read;
    }
    internal void SocketSend(uint token, ReadOnlySpan<byte> data) { if (data.Length > 65507) throw new ArgumentException("UDP datagram exceeds capacity."); if (Send(token, data) != data.Length) throw new IOException("Transport is not ready to send the complete datagram."); }
    internal void CheckError() { var error = Error; Error = null; if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw(); }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe int SendCallback(int id, uint token, ushort port, ENetNativeBuffer* buffers, nuint count)
    {
        if (!Registry.TryGetValue(id, out var weak) || !weak.TryGetTarget(out var self)) return -1;
        try { var size = 0; for (nuint i = 0; i < count; i++) { if (buffers[i].Length > (nuint)(self._send.Length - size)) throw new IOException("ENet datagram exceeds transport storage."); var length = (int)buffers[i].Length; new ReadOnlySpan<byte>(buffers[i].Data, length).CopyTo(self._send.AsSpan(size)); size += length; } return self.Send(token, self._send.AsSpan(0, size)); }
        catch (SocketException error) when (NetworkSockets.Busy(error.SocketErrorCode)) { return 0; }
        catch (Exception error) { self.Error = error; return -1; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe int ReceiveCallback(int id, uint* token, ushort* port, byte* data, nuint capacity)
    {
        if (!Registry.TryGetValue(id, out var weak) || !weak.TryGetTarget(out var self)) return -1;
        try { var size = self.Receive(new Span<byte>(data, checked((int)capacity)), out var address, out var remotePort); *token = address; *port = remotePort; return size; }
        catch (SocketException error) when (NetworkSockets.Busy(error.SocketErrorCode)) { return 0; }
        catch (Exception error) { self.Error = error; return -1; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe int WaitCallback(int id, uint* conditions, uint timeout)
    {
        if (!Registry.TryGetValue(id, out var weak) || !weak.TryGetTarget(out var self)) return -1;
        try { var due = Stopwatch.GetTimestamp() + timeout * Stopwatch.Frequency / 1000; do { if (self._socket is not null) { if (self._socket.Poll(0, SelectMode.SelectRead)) { *conditions = 2; return 0; } } else { self.SecurePoll(); for (var i = 1; i < self._endpoints.Count; i++) if (self._endpoints[i]?.DTLS?.GetAvailablePacketCount() > 0) { *conditions = 2; return 0; } } if (timeout > 0) Thread.Sleep(1); } while (Stopwatch.GetTimestamp() < due); *conditions = 0; return 0; }
        catch (Exception error) { self.Error = error; return -1; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int ControlCallback(int id, int operation, int value)
    {
        if (!Registry.TryGetValue(id, out var weak) || !weak.TryGetTarget(out var self)) return -1;
        try { if (operation == 200) return self.LocalPort; if (operation == 108) return 0; if (operation > 100) return -1; var socket = self._socket!; switch (operation) { case 1: socket.Blocking = value == 0; break; case 2: socket.EnableBroadcast = value != 0; break; case 3: socket.ReceiveBufferSize = value; break; case 4: socket.SendBufferSize = value; break; case 5: socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value); break; case 6: socket.ReceiveTimeout = value; break; case 7: socket.SendTimeout = value; break; case 10: socket.Ttl = (short)value; break; default: return -1; } return 0; }
        catch (Exception error) { self.Error = error; return -1; }
    }
    public void Dispose()
    {
        Registry.TryRemove(ID, out _); GC.SuppressFinalize(this); List<Exception>? errors = null;
        try { _socket?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        _socket = null;
        foreach (var peer in _endpoints) { try { peer?.DTLS?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); } try { peer?.UDP?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); } }
        try { _udpServer?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        try { _dtlsServer?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        _endpoints.Clear(); _tokens.Clear(); if (errors is not null) throw new AggregateException(errors);
    }
}
