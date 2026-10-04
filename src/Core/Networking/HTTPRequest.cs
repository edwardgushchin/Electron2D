using System.Text;
namespace Electron2D;

/// <summary>Runs HTTP/HTTPS transfers from a scene node and delivers completion on its tree owner thread.</summary>
/// <remarks>Requests require tree membership. UseThreads moves an operation and its transports to a dedicated worker;
/// scene polling alone delivers results. Cancellation/tree exit suppress stale completion. File downloads use a sibling
/// temporary file and replace the destination only after successful transfer and decompression.</remarks>
public class HTTPRequest : Node
{
    private HTTPTransfer? _operation;
    private Thread? _worker;
    private bool _threads, _gzip = true;
    private long _bodyLimit = -1;
    private int _chunkSize = 65536, _redirects = 8;
    private string _downloadFile = "";
    private double _timeout, _activeTimeout, _elapsed;
    private TLSOptions? _tls;
    private HTTPProxySettings _proxies = new("", -1, "", -1);
    private long _lastBodySize = -1, _lastDownloaded;
    /// <summary>Creates an idle request node with gzip enabled and eight allowed redirects.</summary>
    public HTTPRequest() { }
    /// <summary>Occurs after state/resources are released; handlers may start another request.</summary>
    public event Action<HTTPRequestResult, HTTPResponseCode, string[], byte[]>? RequestCompleted;
    private void Idle() { EnsureMutable(); if (_operation is not null) throw new InvalidOperationException("Cancel or complete the active request before changing this setting."); }
    /// <summary>Gets or sets dedicated-worker execution.</summary><value>False initially; configurable while idle.</value>
    public bool UseThreads { get { ThrowIfDisposed(); return _threads; } set { Idle(); _threads = value; } }
    /// <summary>Gets or sets gzip/deflate negotiation and decoding.</summary><value>True initially. A current response samples this when its headers arrive.</value>
    public bool AcceptGZIP { get { ThrowIfDisposed(); return _gzip; } set { EnsureMutable(); _gzip = value; if (_operation is not null) Volatile.Write(ref _operation.AcceptGZIP, value ? 1 : 0); } }
    /// <summary>Gets or sets the maximum wire/decoded body bytes.</summary><value>Minus one initially; any negative value disables the limit. Configurable while idle.</value>
    public long BodySizeLimit { get { ThrowIfDisposed(); return _bodyLimit; } set { Idle(); _bodyLimit = value; } }
    /// <summary>Gets or sets the destination engine filesystem path.</summary><value>Empty returns bytes in memory. Existing destinations survive cancellation and failed transfers.</value>
    public string DownloadFile { get { ThrowIfDisposed(); return _downloadFile; } set { Idle(); ArgumentNullException.ThrowIfNull(value); _downloadFile = value; } }
    /// <summary>Gets or sets prepared download/decode chunk capacity.</summary><value>65536 initially; 256 through 16 MiB. Configurable while idle.</value>
    public int DownloadChunkSize { get { ThrowIfDisposed(); return _chunkSize; } set { Idle(); if (value is < 256 or > 16777216) throw new ArgumentOutOfRangeException(nameof(value)); _chunkSize = value; } }
    /// <summary>Gets or sets the automatic redirect limit.</summary><value>Eight initially; negative is unlimited. Changes apply to active response processing.</value>
    public int MaxRedirects { get { ThrowIfDisposed(); return _redirects; } set { EnsureMutable(); _redirects = value; if (_operation is not null) Volatile.Write(ref _operation.MaxRedirects, value); } }
    /// <summary>Gets or sets the request timeout in scene process seconds.</summary><value>Zero disables timeout. Finite nonnegative values are sampled when a request starts.</value>
    public double Timeout { get { ThrowIfDisposed(); return _timeout; } set { EnsureMutable(); if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _timeout = value; } }
    /// <summary>Sets borrowed client TLS options for HTTPS.</summary><param name="options">A live client configuration.</param>
    public void SetTLSOptions(TLSOptions options) { EnsureMutable(); ArgumentNullException.ThrowIfNull(options); if (options.IsServer()) throw new ArgumentException("Client TLS options are required.", nameof(options)); _tls = options; if (_operation is not null) Volatile.Write(ref _operation.TLSOptions, options); }
    /// <summary>Sets or clears the HTTP forward proxy.</summary><param name="host">Empty clears.</param><param name="port">Minus one clears; otherwise a remote port.</param>
    public void SetHTTPProxy(string host, int port) { EnsureMutable(); ValidateProxy(host, port); _proxies = _proxies with { HTTPHost = host, HTTPPort = port }; if (_operation is not null) Volatile.Write(ref _operation.Proxies, _proxies); }
    /// <summary>Sets or clears the HTTPS CONNECT proxy.</summary><param name="host">Empty clears.</param><param name="port">Minus one clears; otherwise a remote port.</param>
    public void SetHTTPSProxy(string host, int port) { EnsureMutable(); ValidateProxy(host, port); _proxies = _proxies with { HTTPSHost = host, HTTPSPort = port }; if (_operation is not null) Volatile.Write(ref _operation.Proxies, _proxies); }
    private static void ValidateProxy(string host, int port) { ArgumentNullException.ThrowIfNull(host); if (host.Length > 0 && port != -1) NetworkSockets.Port(port, true); if (host.IndexOfAny(['\r', '\n', '\0']) >= 0) throw new ArgumentException("Invalid proxy host."); }
    /// <summary>Gets declared response wire-body size.</summary><returns>Minus one before headers or for an unknown length.</returns>
    public long GetBodySize() { ThrowIfDisposed(); return _operation is null ? _lastBodySize : Volatile.Read(ref _operation.BodySize); }
    /// <summary>Gets consumed response wire-body bytes before content decoding.</summary><returns>Zero initially; the most recent operation's count after completion.</returns>
    public long GetDownloadedBytes() { ThrowIfDisposed(); return _operation is null ? _lastDownloaded : Volatile.Read(ref _operation.Downloaded); }
    /// <summary>Gets the cached HTTP transport phase.</summary><returns>Disconnected while idle.</returns>
    public HTTPStatus GetHTTPClientStatus() { ThrowIfDisposed(); return _operation is null ? HTTPStatus.Disconnected : (HTTPStatus)Volatile.Read(ref _operation.ClientStatus); }
    /// <summary>Starts a request to an absolute HTTP/HTTPS URL with a UTF-8 body.</summary><param name="url">URL; fragments are not sent.</param><param name="customHeaders">Optional copied field lines.</param><param name="method">GET initially.</param><param name="requestData">UTF-8 text body.</param>
    public void Request(string url, IEnumerable<string>? customHeaders = null, HTTPMethod method = HTTPMethod.Get, string requestData = "") { ArgumentNullException.ThrowIfNull(requestData); RequestRaw(url, customHeaders, method, Encoding.UTF8.GetBytes(requestData)); }
    /// <summary>Starts a request with copied raw body data.</summary><param name="url">Absolute HTTP/HTTPS URL.</param><param name="customHeaders">Optional copied header lines.</param><param name="method">GET initially.</param><param name="requestData">Raw body, borrowed for this call.</param>
    public void RequestRaw(string url, IEnumerable<string>? customHeaders = null, HTTPMethod method = HTTPMethod.Get, ReadOnlySpan<byte> requestData = default)
    {
        Idle(); if (!IsInsideTree) throw new InvalidOperationException("HTTP requests require a SceneTree.");
        var uri = HTTPTransfer.ParseURL(url); HTTPClient.ValidateRequest(method, method == HTTPMethod.Connect ? uri.Authority : uri.PathAndQuery, customHeaders ?? [], out var headers);
        if (_gzip && !headers.Any(h => h.AsSpan(0, h.IndexOf(':')).Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase))) headers = [.. headers, "Accept-Encoding: gzip, deflate"];
        var path = _downloadFile.Length == 0 ? "" : ProjectSettings.Instance.GlobalizePath(_downloadFile);
        _operation = new HTTPTransfer(uri, method, headers, requestData.ToArray(), _chunkSize, _bodyLimit, path) { AcceptGZIP = _gzip ? 1 : 0, MaxRedirects = _redirects, TLSOptions = _tls, Proxies = _proxies };
        _lastDownloaded = 0; _lastBodySize = -1; _activeTimeout = _timeout; _elapsed = 0; SetInternalProcessing(true, false);
        if (_threads) { var operation = _operation; _worker = new Thread(() => RunWorker(operation)) { IsBackground = true, Name = "Electron2D HTTP" }; _worker.Start(); }
        else _operation.Start();
    }
    private static void RunWorker(HTTPTransfer operation)
    {
        try { operation.Start(); while (!operation.IsDone && Volatile.Read(ref operation.Cancelled) == 0) { operation.Step(); if (!operation.IsDone) Thread.Sleep(1); } }
        finally { operation.Dispose(); Volatile.Write(ref operation.WorkerStopped, 1); }
    }
    /// <summary>Cancels the active operation without emitting completion.</summary>
    public void CancelRequest()
    {
        EnsureMutable(); CancelCore(true);
    }
    private void CancelCore(bool processing)
    {
        var operation = _operation; var worker = _worker; _operation = null; _worker = null;
        if (operation is not null) { lock (operation.LifecycleGate) Volatile.Write(ref operation.Cancelled, 1); if (worker is not null) worker.Join(); else operation.Dispose(); _lastBodySize = Volatile.Read(ref operation.BodySize); _lastDownloaded = Volatile.Read(ref operation.Downloaded); }
        if (processing) SetInternalProcessing(false, false);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationExitTree) { CancelRequest(); return; }
        if (what != NotificationInternalProcess || _operation is null) return;
        var operation = _operation; _elapsed += ProcessDeltaTime;
        if (_activeTimeout > 0 && _elapsed >= _activeTimeout) { CancelRequest(); RequestCompleted?.Invoke(HTTPRequestResult.Timeout, 0, [], []); return; }
        if (!_threads) operation.Step();
        if (!operation.IsDone || _threads && Volatile.Read(ref operation.WorkerStopped) == 0) return;
        var result = operation.Result; var code = operation.Code; var headers = operation.ResponseHeaders; var data = operation.ResponseBody; CancelRequest(); RequestCompleted?.Invoke(result, code, headers, data);
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(HTTPRequest) ? CreateRequestNode : base.CreateSceneInstanceFactory();
    private static Node CreateRequestNode() => new HTTPRequest();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<HTTPRequest, bool>(nameof(UseThreads), n => n.UseThreads, (n, v) => n.UseThreads = v, _ => false, stored: true);
        yield return new PropertyDescriptor<HTTPRequest, bool>(nameof(AcceptGZIP), n => n.AcceptGZIP, (n, v) => n.AcceptGZIP = v, _ => true, stored: true);
        yield return new PropertyDescriptor<HTTPRequest, long>(nameof(BodySizeLimit), n => n.BodySizeLimit, (n, v) => n.BodySizeLimit = v, _ => -1, stored: true);
        yield return new PropertyDescriptor<HTTPRequest, string>(nameof(DownloadFile), n => n.DownloadFile, (n, v) => n.DownloadFile = v, _ => "", stored: true);
        yield return new PropertyDescriptor<HTTPRequest, int>(nameof(DownloadChunkSize), n => n.DownloadChunkSize, (n, v) => n.DownloadChunkSize = v, _ => 65536, stored: true);
        yield return new PropertyDescriptor<HTTPRequest, int>(nameof(MaxRedirects), n => n.MaxRedirects, (n, v) => n.MaxRedirects = v, _ => 8, stored: true);
        yield return new PropertyDescriptor<HTTPRequest, double>(nameof(Timeout), n => n.Timeout, (n, v) => n.Timeout = v, _ => 0, stored: true);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { CancelCore(false); RequestCompleted = null; } base.Dispose(disposing); }
}

internal sealed record HTTPProxySettings(string HTTPHost, int HTTPPort, string HTTPSHost, int HTTPSPort);
