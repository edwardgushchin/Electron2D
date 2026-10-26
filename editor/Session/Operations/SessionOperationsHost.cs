using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Electron2D.Editor;

/// <summary>Owner-confirmed local operation activity without arguments, source or private tokens.</summary>
/// <param name="Method">The exercised typed operation.</param><param name="Origin">Attributed shared client.</param>
/// <param name="Succeeded">Whether dispatch returned a value.</param><param name="ErrorCode">Structured failure code, or empty on success.</param>
public sealed record SessionOperationActivity(string Method, string Origin, bool Succeeded, string ErrorCode);

public sealed partial class ProjectSession
{
    private SessionOperationsHost? _operations;
    /// <summary>Publishes one private local operations endpoint for this existing owner.</summary>
    /// <param name="project">This owner's exact project description.</param><param name="sdkDirectory">Qualified binary SDK used by submitted jobs.</param>
    /// <returns>A caller-owned host; call Pump on this owner thread and dispose it before retiring the owner.</returns>
    public SessionOperationsHost StartOperations(SessionProject project, string sdkDirectory)
    {
        Check(); if (_busy || _readingCode || _operations is not null) throw new InvalidOperationException("Start one operations endpoint on the idle owner.");
        ArgumentNullException.ThrowIfNull(project);
        if (project.Root != ProjectRoot || project.SceneFile != _state.ScenePath || project.CodeFile != _state.CodePath) throw new ArgumentException("The description belongs to different authoring documents.", nameof(project));
        return _operations = new(this, project, sdkDirectory);
    }
    internal void DetachOperations(SessionOperationsHost host) { if (_thread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Use the owner thread."); if (ReferenceEquals(_operations, host)) _operations = null; }
}

/// <summary>Authenticated local operation ingress over an existing shared project owner.</summary>
/// <remarks>Socket workers only frame, authenticate and queue requests. Pump alone invokes the shared typed
/// clients on the creating thread. Private discovery expires with this host; clients never acquire a replacement
/// writer after connection failure. This first transport profile requires Linux Unix sockets.</remarks>
public sealed partial class SessionOperationsHost : IDisposable
{
    private readonly ProjectSession _session;
    private readonly SessionProject _project;
    private readonly string _sdk, _socketDirectory, _discovery;
    private readonly OperationEndpoint _endpoint;
    private readonly Socket _listener;
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentQueue<Pending> _pending = new();
    private readonly Queue<SessionOperationActivity> _activity = new();
    private readonly HashSet<string> _stoppedCapabilities = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Capability> _capabilities = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, string> _connectionCapabilities = new();
    private readonly ConcurrentQueue<Guid> _disconnected = new();
    private readonly ConcurrentDictionary<Guid, Socket> _sockets = new();
    private readonly Dictionary<Guid, SessionClient> _clients = [];
    private readonly Dictionary<Guid, HashSet<Guid>> _clientJobs = [];
    private readonly List<Task> _workers = [];
    private readonly int _thread = Environment.CurrentManagedThreadId;
    private int _queued;
    private bool _disposed;
    internal SessionOperationsHost(ProjectSession session, SessionProject project, string sdk)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Local operation transport currently requires Linux Unix sockets.");
        _session = session; _project = project; _sdk = IOPath.GetFullPath(sdk);
        _discovery = OperationWire.DiscoveryPath(project.Root);
        _socketDirectory = IOPath.Combine(IOPath.GetTempPath(), "e2d-ops-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_socketDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = IOPath.Combine(_socketDirectory, "owner.sock");
        _endpoint = new(1, Guid.NewGuid(), project.Root, path, Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        _capabilities[_endpoint.Token] = new(null, false, false);
        _listener = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            _listener.Bind(new UnixDomainSocketEndPoint(path)); _listener.Listen(16);
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            SessionStorage.AtomicWrite(_discovery, JsonSerializer.SerializeToUtf8Bytes(_endpoint, OperationWire.JSON));
            File.SetUnixFileMode(_discovery, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            Completion = Accept();
        }
        catch { _listener.Dispose(); Directory.Delete(_socketDirectory, true); throw; }
    }
    /// <summary>Gets the endpoint generation; a new owner/host never reuses this identity.</summary>
    public Guid OwnerID => _endpoint.OwnerID;
    /// <summary>Gets completion of socket-worker retirement after Dispose.</summary>
    public Task Completion { get; }
    private void Owner()
    {
        if (_thread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Pump and dispose on the session owner thread.");
    }
    /// <summary>Drains bounded pending requests on the authoring owner without waiting on socket/model/game IO.</summary>
    /// <param name="maximum">Maximum requests to attempt, from one through 128.</param><returns>Number of dequeued requests, including cancelled ones.</returns>
    public int Pump(int maximum = 16)
    {
        Owner(); ObjectDisposedException.ThrowIf(_disposed, this);
        if (maximum is < 1 or > 128) throw new ArgumentOutOfRangeException(nameof(maximum));
        while (_disconnected.TryDequeue(out var id)) RetireClient(id);
        var count = 0;
        while (count < maximum && _pending.TryDequeue(out var pending))
        {
            count++; Interlocked.Decrement(ref _queued);
            if (pending.Reply.Task.IsCompleted || pending.Cancellation.IsCancellationRequested) { pending.Reply.TrySetCanceled(); continue; }
            try
            {
                if (!_connectionCapabilities.TryGetValue(pending.Connection, out var capability) || !_capabilities.ContainsKey(capability)) throw new UnauthorizedAccessException("The operation capability expired before dispatch.");
                if (_stoppedCapabilities.Contains(capability) && !OperationCatalog.Definitions.Any(d => d.Name == pending.Call.Method && d.ReadOnly)) throw new OperationCanceledException("This client was stopped; resume explicitly before submitting new mutations.");
                if (!_clients.TryGetValue(pending.Connection, out var client))
                {
                    client = _session.Connect("external:" + pending.Name + ":" + pending.Connection.ToString("N")[..8]);
                    _clients.Add(pending.Connection, client); _clientJobs.Add(pending.Connection, []);
                }
                pending.Reply.TrySetResult(new(Execute(client, pending.Call, _clientJobs[pending.Connection], pending.Scope)));
                Activity(new(pending.Call.Method, client.Origin, true, ""));
            }
            catch (Exception error)
            {
                var failure = OperationWire.Failure(error); pending.Reply.TrySetResult(new(Error: failure));
                Activity(new(pending.Call.Method ?? "", pending.Name, false, failure.Code));
            }
        }
        return count;
    }
    private void Activity(SessionOperationActivity item) { if (_activity.Count == 128) _activity.Dequeue(); _activity.Enqueue(item); }
    /// <summary>Drains at most 128 copied dispatch notices on the owner thread, excluding request contents.</summary>
    /// <returns>The latest bounded activity batch; shared history retains committed document changes.</returns>
    public IReadOnlyList<SessionOperationActivity> DrainActivity()
    {
        Owner(); var result = _activity.ToArray(); _activity.Clear(); return result;
    }
    private async Task Accept()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var socket = await _listener.AcceptAsync(_stop.Token).ConfigureAwait(false);
                if (_sockets.Count >= 16) { socket.Dispose(); continue; }
                var id = Guid.NewGuid(); _sockets[id] = socket; _workers.Add(Serve(id, socket));
                _workers.RemoveAll(t => t.IsCompletedSuccessfully);
            }
        }
        catch (Exception error) when (_stop.IsCancellationRequested && error is OperationCanceledException or ObjectDisposedException or SocketException) { }
        finally { await Task.WhenAll(_workers).ConfigureAwait(false); }
    }
    private async Task Serve(Guid id, Socket socket)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
        var cancellation = linked.Token;
        await using var stream = new NetworkStream(socket, ownsSocket: true);
        var channel = Channel.CreateBounded<Pending>(1); var inflight = 0;
        Task? sender = null;
        try
        {
            using var handshake = CancellationTokenSource.CreateLinkedTokenSource(cancellation); handshake.CancelAfter(5000);
            var hello = await OperationWire.Read<OperationHello>(stream, handshake.Token).ConfigureAwait(false);
            if (hello.Version != 1 || hello.OwnerID != _endpoint.OwnerID || string.IsNullOrWhiteSpace(hello.ClientName) || hello.ClientName.Length > 64 || hello.ClientName.Any(char.IsControl) ||
                hello.Token is not { Length: 64 } || !_capabilities.TryGetValue(hello.Token, out var scope) || scope.Name is not null && scope.Name != hello.ClientName)
            { await OperationWire.Write(stream, new OperationReply(Error: new("UNAUTHORIZED", "Endpoint generation or capability is invalid.")), cancellation).ConfigureAwait(false); return; }
            _connectionCapabilities[id] = hello.Token;
            await OperationWire.Write(stream, new OperationReply(OperationWire.Element(true)), cancellation).ConfigureAwait(false);
            sender = Send();
            while (!cancellation.IsCancellationRequested)
            {
                var call = await OperationWire.Read<OperationCall>(stream, cancellation).ConfigureAwait(false);
                if (Interlocked.Increment(ref inflight) != 1) throw new InvalidDataException("One outstanding operation per connection is supported.");
                var pending = new Pending(id, hello.ClientName, call, scope, cancellation);
                if (Interlocked.Increment(ref _queued) > 128) { Interlocked.Decrement(ref _queued); pending.Dispose(); throw new IOException("The owner ingress is full."); }
                _pending.Enqueue(pending); await channel.Writer.WriteAsync(pending, cancellation).ConfigureAwait(false);
            }
        }
        catch (Exception error) when (error is IOException or SocketException or OperationCanceledException or ObjectDisposedException or JsonException or InvalidDataException) { }
        finally
        {
            linked.Cancel(); channel.Writer.TryComplete();
            if (sender is not null) { try { await sender.ConfigureAwait(false); } catch (Exception error) when (error is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { } }
            _sockets.TryRemove(id, out _); _connectionCapabilities.TryRemove(id, out _); _disconnected.Enqueue(id);
        }
        async Task Send()
        {
            await foreach (var pending in channel.Reader.ReadAllAsync(cancellation).ConfigureAwait(false))
            {
                using (pending)
                {
                    var reply = await pending.Reply.Task.ConfigureAwait(false);
                    Interlocked.Exchange(ref inflight, 0);
                    try { await OperationWire.Write(stream, reply, cancellation).ConfigureAwait(false); }
                    catch (InvalidDataException) { await OperationWire.Write(stream, new OperationReply(Error: new("RESULT_TOO_LARGE", "The response exceeds the frame budget. A committed edit remains in history; inspect current state before retrying a mutation.")), cancellation).ConfigureAwait(false); }
                }
            }
        }
    }
    private void RetireClient(Guid id)
    {
        if (_clientJobs.Remove(id, out var jobs))
            _session.CancelJobs(jobs);
        if (_clients.Remove(id, out var client)) client.Dispose();
    }
    /// <summary>Issues a named, owner-lifetime capability with explicitly selected publication/discard rights.</summary>
    /// <param name="clientName">Bounded external actor name; authentication binds it to this capability.</param>
    /// <param name="allowFileWrites">Whether Save may publish project documents.</param>
    /// <param name="allowDiscard">Whether UseDisk may discard unsaved documents.</param>
    /// <returns>A secret capability token; pass privately to the chosen executor and never log it.</returns>
    public string GrantClient(string clientName, bool allowFileWrites = false, bool allowDiscard = false)
    {
        Owner(); ObjectDisposedException.ThrowIf(_disposed, this); ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        if (clientName.Length > 64 || clientName.Any(char.IsControl) || _capabilities.Count >= 128) throw new ArgumentException("Invalid actor name or capability budget.", nameof(clientName));
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        _capabilities[token] = new(clientName, allowFileWrites, allowDiscard); return token;
    }
    /// <summary>Expires an issued capability and disconnects its clients without rolling back committed authoring.</summary>
    /// <param name="capability">Secret returned by GrantClient; default discovery rights expire only with this host.</param>
    public void RevokeClient(string capability)
    {
        Owner(); ObjectDisposedException.ThrowIf(_disposed, this);
        if (capability == _endpoint.Token) throw new ArgumentException("Dispose the host to expire its default endpoint.", nameof(capability));
        CancelClientWork(capability);
        _capabilities.TryRemove(capability, out _); _stoppedCapabilities.Remove(capability);
        foreach (var pair in _connectionCapabilities) if (pair.Value == capability && _sockets.TryGetValue(pair.Key, out var socket)) socket.Dispose();
    }
    /// <summary>Cancels queued requests/jobs and stops new mutations for an issued capability, retaining reads, conversation and committed edits.</summary>
    /// <param name="capability">Secret returned by GrantClient.</param>
    public void CancelClientWork(string capability)
    {
        Owner(); ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_capabilities.ContainsKey(capability)) return;
        _stoppedCapabilities.Add(capability);
        var connections = _connectionCapabilities.Where(p => p.Value == capability).Select(p => p.Key).ToHashSet();
        foreach (var pending in _pending)
            if (connections.Contains(pending.Connection)) pending.Reply.TrySetResult(new(Error: new("CANCELLED", "The owner stopped this request before dispatch. Committed changes remain in history.")));
        foreach (var id in connections) if (_clientJobs.TryGetValue(id, out var jobs)) _session.CancelJobs(jobs);
    }
    /// <summary>Resumes mutation admission for a stopped capability after cancelling any late queued work.</summary>
    /// <param name="capability">The still-valid secret returned by GrantClient.</param>
    public void ResumeClientWork(string capability)
    {
        Owner(); ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_capabilities.ContainsKey(capability)) throw new UnauthorizedAccessException("The operation capability expired.");
        if (!_stoppedCapabilities.Contains(capability)) return;
        CancelClientWork(capability); _stoppedCapabilities.Remove(capability);
    }
    /// <summary>Closes discovery/connections and cancels queued work and connection-owned jobs without rolling back committed edits.</summary>
    public void Dispose()
    {
        if (_disposed) return; Owner(); _disposed = true;
        _stop.Cancel(); _listener.Dispose(); foreach (var socket in _sockets.Values) socket.Dispose();
        while (_pending.TryDequeue(out var pending)) { pending.Reply.TrySetCanceled(); pending.Dispose(); }
        foreach (var id in _clients.Keys.ToArray()) RetireClient(id);
        _session.DetachOperations(this);
        Completion.ContinueWith(_ => _stop.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        try
        {
            if (File.Exists(_discovery) && OperationWire.Discover(_project.Root).OwnerID == OwnerID) File.Delete(_discovery);
        }
        finally { if (Directory.Exists(_socketDirectory)) Directory.Delete(_socketDirectory, true); }
    }
    private sealed record Capability(string? Name, bool FileWrites, bool Discard);
    private sealed class Pending : IDisposable
    {
        internal Guid Connection { get; }
        internal string Name { get; }
        internal OperationCall Call { get; }
        internal Capability Scope { get; }
        internal CancellationToken Cancellation { get; }
        internal TaskCompletionSource<OperationReply> Reply { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenRegistration _registration;
        internal Pending(Guid connection, string name, OperationCall call, Capability scope, CancellationToken cancellation)
        {
            Connection = connection; Name = name; Call = call; Scope = scope; Cancellation = cancellation;
            _registration = cancellation.Register(() => Reply.TrySetCanceled(cancellation));
        }
        public void Dispose() => _registration.Dispose();
    }
}
