using System.Net;
using System.Net.Sockets;
namespace Electron2D;

/// <summary>Owns a nonblocking native stream listener and transfers accepted peers to callers.</summary>
/// <remarks>Server calls require the constructing thread. Stopping the listener leaves accepted stream peers alive.
/// TakeSocketConnection returns null when no connection is queued. Socket handles remain internal.</remarks>
public abstract class SocketServer : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private Socket? _socket;
    /// <summary>Creates a stopped listener.</summary>
    protected SocketServer() { }
    /// <summary>Checks listener lifetime and thread ownership.</summary>
    protected void CheckServer() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("Listener access requires its constructing thread."); }
    private protected Socket? NativeSocket => _socket;
    private protected void Start(Socket socket, EndPoint address)
    {
        CheckServer(); if (_socket is not null) { socket.Dispose(); throw new InvalidOperationException("The listener is already open."); }
        try { socket.Bind(address); socket.Listen(8); _socket = socket; } catch { socket.Dispose(); throw; }
    }
    /// <summary>Reports whether an accepted connection can be taken immediately.</summary><returns>True when the listening socket is readable.</returns>
    public bool IsConnectionAvailable() { CheckServer(); return _socket?.Poll(0, SelectMode.SelectRead) ?? false; }
    /// <summary>Reports whether a listener is open.</summary><returns>True after a successful Listen until Stop.</returns>
    public bool IsListening() { CheckServer(); return _socket is not null; }
    /// <summary>Stops accepting new connections while leaving caller-owned accepted peers alive.</summary>
    public void Stop() { CheckServer(); StopCore(); }
    private void StopCore() { var socket = _socket; _socket = null; socket?.Dispose(); }
    /// <summary>Takes one pending connection through the shared stream-socket interface.</summary><returns>A caller-owned peer, or null when none is available.</returns>
    public StreamPeerSocket? TakeSocketConnection()
    {
        CheckServer(); if (!IsConnectionAvailable()) return null;
        Socket? accepted = null;
        try { accepted = _socket!.Accept(); accepted.Blocking = false; return CreateAcceptedPeer(accepted); }
        catch (SocketException error) when (NetworkSockets.Busy(error.SocketErrorCode)) { accepted?.Dispose(); return null; }
        catch { accepted?.Dispose(); throw; }
    }
    private protected abstract StreamPeerSocket CreateAcceptedPeer(Socket socket);
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); CheckServer(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) StopCore(); base.Dispose(disposing); }
}

/// <summary>Accepts native TCP connections on an IP literal or wildcard.</summary>
public class TCPServer : SocketServer
{
    private int _port;
    /// <summary>Creates a stopped TCP listener.</summary>
    public TCPServer() { }
    /// <summary>Listens on a local port and bind address.</summary><param name="port">Zero through 65535; zero selects an ephemeral port.</param><param name="bindAddress">IP literal or wildcard.</param>
    /// <exception cref="SocketException">Binding or listening fails.</exception>
    public void Listen(int port, string bindAddress = "*")
    {
        CheckServer(); NetworkSockets.Port(port); if (IsListening()) throw new InvalidOperationException("The listener is already open.");
        var address = NetworkSockets.BindAddress(bindAddress); var socket = NetworkSockets.Create(address.AddressFamily, SocketType.Stream);
        try { NetworkSockets.ConfigureListener(socket); Start(socket, new IPEndPoint(address, port)); _port = ((IPEndPoint)socket.LocalEndPoint!).Port; } catch { socket.Dispose(); throw; }
    }
    /// <summary>Returns the listening local port or zero after stopping.</summary><returns>The port.</returns>
    public int GetLocalPort() { CheckServer(); return IsListening() ? _port : 0; }
    /// <summary>Takes a queued TCP connection.</summary><returns>A caller-owned peer, or null.</returns>
    public StreamPeerTCP? TakeConnection() => (StreamPeerTCP?)TakeSocketConnection();
    private protected override StreamPeerSocket CreateAcceptedPeer(Socket socket) => new StreamPeerTCP(socket);
}

/// <summary>Accepts native Unix-domain stream connections.</summary>
/// <remarks>Endpoint support and path limits follow the native runtime. Existing namespace entries are not replaced.</remarks>
public class UDSServer : SocketServer
{
    /// <summary>Creates a stopped Unix-domain listener.</summary>
    public UDSServer() { }
    /// <summary>Listens on a nonempty local Unix-domain endpoint.</summary><param name="path">The native endpoint path.</param>
    /// <exception cref="PlatformNotSupportedException">Unix-domain sockets are unavailable.</exception>
    public void Listen(string path) { CheckServer(); ArgumentException.ThrowIfNullOrEmpty(path); if (IsListening()) throw new InvalidOperationException("The listener is already open."); var endpoint = new UnixDomainSocketEndPoint(path); Start(NetworkSockets.Create(AddressFamily.Unix, SocketType.Stream), endpoint); }
    /// <summary>Takes a queued Unix-domain connection.</summary><returns>A caller-owned peer, or null.</returns>
    public StreamPeerUDS? TakeConnection() => (StreamPeerUDS?)TakeSocketConnection();
    private protected override StreamPeerSocket CreateAcceptedPeer(Socket socket) => new StreamPeerUDS(socket);
}
