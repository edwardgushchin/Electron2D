using System.Net.Sockets;
using System.Text.Json;

namespace Electron2D.Editor;

/// <summary>Attaches to an existing local owner; it never opens a fallback writable session.</summary>
/// <remarks>Calls use a private authenticated Unix connection. The owner must pump requests on its thread.
/// One request at a time is admitted per client. Disconnection cancels pending calls; already committed operations
/// remain in the shared history and must be inspected before retrying a mutation.</remarks>
public sealed class SessionOperationsClient : IAsyncDisposable
{
    private readonly NetworkStream _stream;
    private readonly SemaphoreSlim _requests = new(1);
    private bool _disposed;
    private SessionOperationsClient(NetworkStream stream) => _stream = stream;
    /// <summary>Attaches to an explicitly existing owner with private discovery and protocol authentication.</summary>
    /// <param name="projectRoot">Existing canonical project root.</param>
    /// <param name="clientName">Bounded display attribution; the owner prefixes it as an external client.</param>
    /// <param name="capability">Optional privately issued named capability; null uses default authoring/job rights without file publication or discard.</param>
    /// <param name="cancellation">Cancels connection/handshake.</param><returns>A caller-owned transport connection.</returns>
    public static async Task<SessionOperationsClient> ConnectAsync(string projectRoot, string clientName, string? capability = null, CancellationToken cancellation = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName); if (clientName.Length > 64) throw new ArgumentOutOfRangeException(nameof(clientName));
        var endpoint = OperationWire.Discover(projectRoot);
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        NetworkStream? stream = null;
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(endpoint.Socket), cancellation).ConfigureAwait(false);
            stream = new NetworkStream(socket, ownsSocket: true);
            await OperationWire.Write(stream, new OperationHello(1, endpoint.OwnerID, capability ?? endpoint.Token, clientName), cancellation).ConfigureAwait(false);
            var accepted = await OperationWire.Read<OperationReply>(stream, cancellation).ConfigureAwait(false);
            if (accepted.Error is not null) throw new IOException(accepted.Error.Code + ": " + accepted.Error.Message);
            return new(stream);
        }
        catch { stream?.Dispose(); socket.Dispose(); throw; }
    }
    /// <summary>Invokes a documented typed operation by name with its JSON transport arguments.</summary>
    /// <param name="method">One advertised operation name; arbitrary runtime member invocation is unavailable.</param>
    /// <param name="arguments">Operation-specific schema data.</param><param name="cancellation">Cancels transport waiting, not a completed commit.</param>
    /// <returns>Copied JSON value or a structured operation/conflict failure; transport failures throw.</returns>
    public async Task<JsonElement> InvokeAsync(string method, JsonElement arguments, CancellationToken cancellation = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ArgumentException.ThrowIfNullOrWhiteSpace(method);
        await _requests.WaitAsync(cancellation).ConfigureAwait(false);
        try
        {
            await OperationWire.Write(_stream, new OperationCall(method, arguments), cancellation).ConfigureAwait(false);
            var reply = await OperationWire.Read<OperationReply>(_stream, cancellation).ConfigureAwait(false);
            return OperationWire.Element(reply);
        }
        catch { _disposed = true; _stream.Dispose(); throw; }
        finally { _requests.Release(); }
    }
    /// <summary>Closes this connection; the owner remains authoritative and recoverable.</summary>
    public ValueTask DisposeAsync() { _disposed = true; _stream.Dispose(); return ValueTask.CompletedTask; }
}
