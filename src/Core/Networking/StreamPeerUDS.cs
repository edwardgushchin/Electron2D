using System.Net.Sockets;
namespace Electron2D;

/// <summary>Transfers ordered binary stream data through a native Unix-domain socket.</summary>
/// <remarks>Requires runtime Unix-domain socket support. Paths follow the platform endpoint limits. The caller
/// owns filesystem namespace permissions; the socket backend owns bound endpoint cleanup.</remarks>
public class StreamPeerUDS : StreamPeerSocket
{
    private string _path = "";
    /// <summary>Creates a disconnected Unix-domain peer.</summary>
    public StreamPeerUDS() { }
    internal StreamPeerUDS(Socket socket) { SetSocket(socket, StreamSocketStatus.Connected); _path = socket.RemoteEndPoint?.ToString() ?? ""; }
    /// <summary>Binds a local Unix-domain endpoint before connecting.</summary><param name="path">Nonempty platform endpoint path.</param>
    public void Bind(string path)
    {
        CheckStream(); ArgumentException.ThrowIfNullOrEmpty(path); if (NativeSocket is not null) throw new InvalidOperationException("The socket is already open.");
        var socket = NetworkSockets.Create(AddressFamily.Unix, SocketType.Stream);
        try { socket.Bind(new UnixDomainSocketEndPoint(path)); SetSocket(socket, StreamSocketStatus.None); } catch { socket.Dispose(); throw; }
    }
    /// <summary>Starts a nonblocking connection to a Unix-domain endpoint.</summary><param name="path">Nonempty platform endpoint path.</param>
    public void ConnectToHost(string path)
    {
        CheckStream(); ArgumentException.ThrowIfNullOrEmpty(path); if (GetStatus() != StreamSocketStatus.None) throw new InvalidOperationException("A connection is already active.");
        var endpoint = new UnixDomainSocketEndPoint(path); if (NativeSocket is null) SetSocket(NetworkSockets.Create(AddressFamily.Unix, SocketType.Stream), StreamSocketStatus.None);
        _path = path; Connect(endpoint, ProjectSettings.Instance.Get(ProjectSettings.UDSConnectTimeoutSeconds));
    }
    /// <summary>Returns the selected remote path, or empty after disconnection.</summary><returns>The endpoint path.</returns>
    public string GetConnectedPath() { CheckStream(); return NativeSocket is null ? "" : _path; }
}
