using System.Net;
using System.Net.Sockets;
namespace Electron2D;

/// <summary>Describes a polled stream-socket connection.</summary>
public enum StreamSocketStatus
{
    /// <summary>No active connection, including graceful remote closure.</summary>
    None = 0,
    /// <summary>A nonblocking connection attempt is in progress.</summary>
    Connecting = 1,
    /// <summary>The socket can transfer stream bytes.</summary>
    Connected = 2,
    /// <summary>The last polled connection failed.</summary>
    Error = 3
}

/// <summary>Owns a nonblocking native stream socket with explicit connection polling.</summary>
/// <remarks>Partial operations do not wait. Full reads/writes can block until the peer supplies progress or closes.
    /// Poll detects connection completion, nonzero socket errors and FIN after queued bytes drain.
    /// Readiness without a socket error preserves queued data. Status queries are cached.
/// Calls and disposal require the constructing thread.</remarks>
public abstract class StreamPeerSocket : StreamPeer
{
    private Socket? _socket;
    private StreamSocketStatus _status;
    private long _deadline;
    /// <summary>Creates a disconnected stream socket.</summary>
    protected StreamPeerSocket() { }
    private protected Socket? NativeSocket => _socket;
    private protected void SetSocket(Socket socket, StreamSocketStatus status)
    {
        if (_socket is not null) throw new InvalidOperationException("The socket is already open.");
        _socket = socket; _status = status;
    }
    private protected void Connect(EndPoint address, int timeoutSeconds)
    {
        CheckStream(); if (_status != StreamSocketStatus.None) throw new InvalidOperationException("A connection is already active.");
        _deadline = Environment.TickCount64 + (long)timeoutSeconds * 1000;
        try { _socket!.Connect(address); _status = StreamSocketStatus.Connected; }
        catch (SocketException error) when (NetworkSockets.Busy(error.SocketErrorCode)) { _status = StreamSocketStatus.Connecting; }
        catch { DisconnectCore(); throw; }
    }
    /// <summary>Advances connection completion and detects errors or drained remote closure.</summary>
    /// <exception cref="SocketException">The connection fails; state becomes Error and native resources close.</exception>
    /// <exception cref="TimeoutException">The configured connection deadline expires.</exception>
    public void Poll()
    {
        CheckStream(); if (_socket is null || _status is StreamSocketStatus.None or StreamSocketStatus.Error) return;
        try
        {
            if (_status == StreamSocketStatus.Connecting)
            {
                if (_socket.Poll(0, SelectMode.SelectWrite) || _socket.Poll(0, SelectMode.SelectError))
                {
                    var error = (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error)!;
                    if (error != 0) throw new SocketException(error); _status = StreamSocketStatus.Connected;
                }
                else if (Environment.TickCount64 > _deadline) throw new TimeoutException("Stream connection deadline expired.");
            }
            else
            {
                if (_socket.Poll(0, SelectMode.SelectError))
                {
                    var error = (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error)!;
                    if (error != 0) throw new SocketException(error);
                }
                if (_socket.Poll(0, SelectMode.SelectRead) && _socket.Available == 0) DisconnectCore();
            }
        }
        catch { DisconnectCore(); _status = StreamSocketStatus.Error; throw; }
    }
    /// <summary>Returns the cached connection phase without polling.</summary><returns>The latest explicit phase.</returns>
    public StreamSocketStatus GetStatus() { CheckStream(); return _status; }
    /// <summary>Closes native connection and binding state and resets the phase.</summary>
    public void DisconnectFromHost() { CheckStream(); DisconnectCore(); }
    private void DisconnectCore() { var socket = _socket; _socket = null; _status = StreamSocketStatus.None; _deadline = 0; socket?.Dispose(); }
    /// <inheritdoc />
    public override int GetAvailableBytes() { CheckStream(); return _socket?.Available ?? 0; }
    private Socket ConnectedSocket()
    {
        if (_socket is null || _status != StreamSocketStatus.Connected) throw new InvalidOperationException("A connected stream is required.");
        return _socket;
    }
    /// <inheritdoc />
    protected override int ReadCore(Span<byte> destination, bool block)
    {
        var socket = ConnectedSocket(); var total = 0;
        try
        {
            while (total < destination.Length)
            {
                var read = socket.Receive(destination[total..], SocketFlags.None, out var error);
                if (NetworkSockets.Busy(error)) { if (!block) return total; socket.Poll(-1, SelectMode.SelectRead); continue; }
                if (error != SocketError.Success) throw new SocketException((int)error);
                if (read == 0) { DisconnectCore(); if (block) throw new EndOfStreamException(); return total; }
                total += read; if (!block && total == destination.Length) break;
            }
            return total;
        }
        catch { DisconnectCore(); throw; }
    }
    /// <inheritdoc />
    protected override int WriteCore(ReadOnlySpan<byte> data, bool block)
    {
        var socket = ConnectedSocket(); var total = 0;
        try
        {
            while (total < data.Length)
            {
                var sent = socket.Send(data[total..], SocketFlags.None, out var error);
                if (NetworkSockets.Busy(error)) { if (!block) return total; socket.Poll(-1, SelectMode.SelectWrite); continue; }
                if (error != SocketError.Success) throw new SocketException((int)error);
                if (sent == 0) throw new IOException("The stream socket made no write progress."); total += sent;
            }
            return total;
        }
        catch { DisconnectCore(); throw; }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) DisconnectCore(); base.Dispose(disposing); }
}
