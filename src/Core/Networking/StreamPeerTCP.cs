using System.Net;
using System.Net.Sockets;
namespace Electron2D;

/// <summary>Transfers ordered binary stream data through a native TCP connection.</summary>
/// <remarks>Bind accepts an IP literal or wildcard. ConnectToHost resolves a name before starting a nonblocking
/// connect. The local port is captured at binding without querying pending connection state. Poll is required while connecting and for FIN/error detection. Timeout is sampled from project settings.</remarks>
public class StreamPeerTCP : StreamPeerSocket
{
    private string _host = "";
    private int _port, _localPort;
    /// <summary>Creates an unbound disconnected TCP peer.</summary>
    public StreamPeerTCP() { }
    internal StreamPeerTCP(Socket socket) { SetSocket(socket, StreamSocketStatus.Connected); var remote = (IPEndPoint)socket.RemoteEndPoint!; _host = NetworkSockets.Host(remote.Address); _port = remote.Port; _localPort = ((IPEndPoint)socket.LocalEndPoint!).Port; }
    /// <summary>Binds a local address and port before connecting.</summary><param name="port">Zero through 65535; zero requests an ephemeral port.</param><param name="host">IP literal or wildcard.</param>
    public void Bind(int port, string host = "*")
    {
        CheckStream(); NetworkSockets.Port(port); if (NativeSocket is not null) throw new InvalidOperationException("The socket is already open.");
        var address = NetworkSockets.BindAddress(host); var socket = NetworkSockets.Create(address.AddressFamily, SocketType.Stream);
        try { socket.Bind(new IPEndPoint(address, port)); SetSocket(socket, StreamSocketStatus.None); _localPort = ((IPEndPoint)socket.LocalEndPoint!).Port; } catch { socket.Dispose(); throw; }
    }
    /// <summary>Starts a connection, reusing an explicitly bound socket.</summary><param name="host">IP literal or resolvable host name.</param><param name="port">Remote port, one through 65535.</param>
    public void ConnectToHost(string host, int port)
    {
        CheckStream(); NetworkSockets.Port(port, true); if (GetStatus() != StreamSocketStatus.None) throw new InvalidOperationException("A connection is already active.");
        var address = NetworkSockets.Resolve(host); if (NativeSocket is null)
        {
            var socket = NetworkSockets.Create(address.AddressFamily, SocketType.Stream);
            try { socket.Bind(new IPEndPoint(address.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0)); _localPort = ((IPEndPoint)socket.LocalEndPoint!).Port; SetSocket(socket, StreamSocketStatus.None); } catch { socket.Dispose(); throw; }
        }
        _host = NetworkSockets.Host(address); _port = port;
        Connect(NetworkSockets.Endpoint(NativeSocket!, address, port), ProjectSettings.Instance.Get(ProjectSettings.TCPConnectTimeoutSeconds));
    }
    /// <summary>Returns the selected remote IP, or empty after disconnection.</summary><returns>A normalized IP literal.</returns>
    public string GetConnectedHost() { CheckStream(); return NativeSocket is null ? "" : _host; }
    /// <summary>Returns the selected remote port, or zero after disconnection.</summary><returns>The port.</returns>
    public int GetConnectedPort() { CheckStream(); return NativeSocket is null ? 0 : _port; }
    /// <summary>Returns the local bound port or zero before opening.</summary><returns>The port.</returns>
    public int GetLocalPort() { CheckStream(); return NativeSocket is null ? 0 : _localPort; }
    /// <summary>Enables or disables TCP_NODELAY on an open socket.</summary><param name="enabled">Whether to bypass Nagle aggregation.</param>
    public void SetNoDelay(bool enabled) { CheckStream(); (NativeSocket ?? throw new InvalidOperationException("An open socket is required.")).NoDelay = enabled; }
}
