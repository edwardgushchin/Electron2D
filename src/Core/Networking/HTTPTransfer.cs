using System.Security.Authentication;
namespace Electron2D;

internal sealed class HTTPTransfer : IDisposable
{
    private Uri _url;
    private HTTPMethod _method;
    private string[] _requestHeaders;
    private byte[] _body;
    private readonly byte[] _wire, _decoded;
    private readonly long _limit;
    private readonly string _destination;
    private string? _temporary;
    private FileStream? _file;
    private MemoryStream? _memory;
    private StreamPeerGZIP? _decoder;
    private bool _sent, _headersReady, _wireDone;
    private int _redirects, _pendingOffset, _pendingCount, _done;
    private long _outputBytes;
    private HTTPProxySettings? _appliedProxy;
    internal readonly object LifecycleGate = new();
    internal int Cancelled, WorkerStopped, AcceptGZIP, MaxRedirects, ClientStatus;
    internal long BodySize = -1, Downloaded;
    internal TLSOptions? TLSOptions;
    internal HTTPProxySettings Proxies = new("", -1, "", -1);
    internal HTTPRequestResult Result { get; private set; }
    internal HTTPResponseCode Code { get; private set; }
    internal string[] ResponseHeaders { get; private set; } = [];
    internal byte[] ResponseBody { get; private set; } = [];
    internal bool IsDone => Volatile.Read(ref _done) != 0;
    internal HTTPTransfer(Uri uri, HTTPMethod method, string[] headers, byte[] body, int chunk, long limit, string destination)
    {
        _url = uri; _method = method; _requestHeaders = headers; _body = body; _wire = new byte[chunk]; _decoded = new byte[chunk]; _limit = limit; _destination = destination;
    }
    private HTTPClient? _ownerClient;
    private HTTPClient Client => _ownerClient!;
    internal static Uri ParseURL(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url); if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.Host.Length == 0 || uri.UserInfo.Length != 0) throw new ArgumentException("An absolute HTTP/HTTPS URL without embedded credentials is required.", nameof(url)); return uri;
    }
    internal void Start()
    {
        try { _ownerClient = new HTTPClient { ReadChunkSize = _wire.Length }; Connect(); }
        catch { Complete(FailureResult()); }
    }
    private void Connect()
    {
        Client.Close(); ApplyProxies(); Client.ConnectToHost((_url.Scheme == "https" ? "https://" : "http://") + _url.IdnHost, _url.Port, _url.Scheme == "https" ? Volatile.Read(ref TLSOptions) : null);
        _sent = _headersReady = _wireDone = false; Volatile.Write(ref BodySize, -1); Volatile.Write(ref Downloaded, 0); _pendingOffset = _pendingCount = 0; _outputBytes = 0; UpdateStatus();
    }
    private void ApplyProxies()
    {
        var proxies = Volatile.Read(ref Proxies); if (ReferenceEquals(_appliedProxy, proxies)) return; Client.SetHTTPProxy(proxies.HTTPHost, proxies.HTTPPort); Client.SetHTTPSProxy(proxies.HTTPSHost, proxies.HTTPSPort); _appliedProxy = proxies;
    }
    private void UpdateStatus() => Volatile.Write(ref ClientStatus, (int)Client.GetStatus());
    internal void Step()
    {
        if (IsDone || Volatile.Read(ref Cancelled) != 0) return;
        try
        {
            ApplyProxies();
            if (!_headersReady)
            {
                Client.Poll(); UpdateStatus();
                if (!_sent && Client.GetStatus() == HTTPStatus.Connected) { Client.RequestRaw(_method, _method == HTTPMethod.Connect ? _url.Authority : _url.PathAndQuery, _requestHeaders, _body); _sent = true; UpdateStatus(); return; }
                if (!_sent || !Client.HasResponse()) { if (_sent && Client.GetStatus() == HTTPStatus.Disconnected) Complete(HTTPRequestResult.NoResponse); return; }
                Code = Client.GetResponseCode(); ResponseHeaders = Client.GetResponseHeaders();
                if (Redirect()) return;
                _headersReady = true; Volatile.Write(ref BodySize, Client.GetResponseBodyLength());
                if (_limit >= 0 && BodySize > _limit) { Complete(HTTPRequestResult.BodySizeLimitExceeded); return; }
                if (!OpenSink()) return;
                var encoding = Volatile.Read(ref AcceptGZIP) != 0 ? Header("Content-Encoding") : "";
                if (encoding.Equals("gzip", StringComparison.OrdinalIgnoreCase) || encoding.Equals("deflate", StringComparison.OrdinalIgnoreCase)) { _decoder = new StreamPeerGZIP(); _decoder.StartDecompression(encoding.Equals("deflate", StringComparison.OrdinalIgnoreCase), _wire.Length); }
                if (Client.GetStatus() is HTTPStatus.Connected or HTTPStatus.Disconnected) _wireDone = true;
            }
            if (_decoder is not null && _pendingOffset < _pendingCount)
            {
                _pendingOffset += _decoder.PutPartialData(_wire.AsSpan(_pendingOffset, _pendingCount - _pendingOffset));
                if (!DrainDecoder()) return;
                if (_pendingOffset < _pendingCount) return;
            }
            if (!_wireDone)
            {
                Client.Poll(); var count = Client.ReadResponseBodyChunk(_wire); UpdateStatus(); Volatile.Write(ref Downloaded, Downloaded + count);
                if (_limit >= 0 && Downloaded > _limit) { Complete(HTTPRequestResult.BodySizeLimitExceeded); return; }
                if (_decoder is null) { if (count != 0 && !Store(_wire.AsSpan(0, count))) return; }
                else { _pendingOffset = 0; _pendingCount = count; if (count != 0) { _pendingOffset = _decoder.PutPartialData(_wire.AsSpan(0, count)); if (!DrainDecoder()) return; } }
                _wireDone = Client.GetStatus() is HTTPStatus.Connected or HTTPStatus.Disconnected;
            }
            if (_decoder is not null && !DrainDecoder()) return;
            if (!_wireDone || _pendingOffset < _pendingCount) return;
            if (_decoder is not null)
            {
                if (_decoder.GetAvailableBytes() != 0) return;
                if (!_decoder.IsFinished()) { Complete(HTTPRequestResult.BodyDecompressFailed); return; }
            }
            Complete(HTTPRequestResult.Success);
        }
        catch (EndOfStreamException) { Complete(_headersReady ? HTTPRequestResult.ChunkedBodySizeMismatch : HTTPRequestResult.NoResponse); }
        catch (AuthenticationException) { Complete(HTTPRequestResult.TLSHandshakeError); }
        catch (InvalidDataException) { Complete(_decoder is null ? HTTPRequestResult.ConnectionError : HTTPRequestResult.BodyDecompressFailed); }
        catch { Complete(FailureResult()); }
    }
    private HTTPRequestResult FailureResult() => _ownerClient?.GetStatus() switch
    {
        HTTPStatus.CantResolve => HTTPRequestResult.CantResolve,
        HTTPStatus.CantConnect => HTTPRequestResult.CantConnect,
        HTTPStatus.TLSHandshakeError => HTTPRequestResult.TLSHandshakeError,
        HTTPStatus.ConnectionError => HTTPRequestResult.ConnectionError,
        _ => HTTPRequestResult.RequestFailed
    };
    private bool DrainDecoder() { var count = _decoder!.GetPartialData(_decoded); return count == 0 || Store(_decoded.AsSpan(0, count)); }
    private string Header(string name)
    {
        foreach (var field in ResponseHeaders) { var colon = field.IndexOf(':'); if (field.AsSpan(0, colon).Equals(name, StringComparison.OrdinalIgnoreCase)) return field[(colon + 1)..].Trim(); }
        return "";
    }
    private bool Redirect()
    {
        var code = (int)Code; var safe = _method is HTTPMethod.Get or HTTPMethod.Head or HTTPMethod.Options or HTTPMethod.Trace;
        if (_method == HTTPMethod.Connect || !(code is 301 or 302 or 303 or 305 || code is 307 or 308 && safe)) return false;
        var location = Header("Location"); if (location.Length == 0) return false;
        var maximum = Volatile.Read(ref MaxRedirects); if (maximum >= 0 && _redirects >= maximum) { Complete(HTTPRequestResult.RedirectLimitReached); return true; }
        var next = new Uri(_url, location); if (next.Scheme is not ("http" or "https") || next.UserInfo.Length != 0) { Complete(HTTPRequestResult.RequestFailed); return true; }
        var crossOrigin = next.Scheme != _url.Scheme || next.IdnHost != _url.IdnHost || next.Port != _url.Port;
        if (!safe) { _method = HTTPMethod.Get; _body = []; _requestHeaders = _requestHeaders.Where(h => !ContentHeader(h)).ToArray(); }
        if (crossOrigin) _requestHeaders = _requestHeaders.Where(h => !SensitiveHeader(h)).ToArray();
        _url = next; _redirects++; Connect(); return true;
    }
    private static bool ContentHeader(string header) { var name = header[..header.IndexOf(':')]; return name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) || name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) || name.Equals("Content-Location", StringComparison.OrdinalIgnoreCase) || name.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase) || name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || name.Equals("Connection", StringComparison.OrdinalIgnoreCase) || name.Equals("Authorization", StringComparison.OrdinalIgnoreCase); }
    private static bool SensitiveHeader(string header) { var name = header[..header.IndexOf(':')]; return name.Equals("Authorization", StringComparison.OrdinalIgnoreCase) || name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase) || name.Equals("Cookie", StringComparison.OrdinalIgnoreCase) || name.Equals("Host", StringComparison.OrdinalIgnoreCase); }
    private bool OpenSink()
    {
        if (_destination.Length == 0) { _memory = new MemoryStream(); return true; }
        try { _temporary = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_destination)!, ".e2d-download-" + System.IO.Path.GetRandomFileName()); _file = new FileStream(_temporary, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None); return true; }
        catch { Complete(HTTPRequestResult.DownloadFileCantOpen); return false; }
    }
    private bool Store(ReadOnlySpan<byte> bytes)
    {
        if (_limit >= 0 && _outputBytes > _limit - bytes.Length) { Complete(HTTPRequestResult.BodySizeLimitExceeded); return false; }
        try { if (_file is not null) _file.Write(bytes); else _memory!.Write(bytes); _outputBytes += bytes.Length; return true; }
        catch { Complete(_file is null ? HTTPRequestResult.RequestFailed : HTTPRequestResult.DownloadFileWriteError); return false; }
    }
    private void Complete(HTTPRequestResult result)
    {
        if (IsDone) return;
        lock (LifecycleGate)
        {
            if (Volatile.Read(ref Cancelled) != 0) return;
            if (result == HTTPRequestResult.Success)
            {
                try { _file?.Dispose(); _file = null; if (_temporary is not null) { File.Move(_temporary, _destination, true); _temporary = null; } else ResponseBody = _memory?.ToArray() ?? []; }
                catch { result = HTTPRequestResult.DownloadFileWriteError; }
            }
            Result = result; Dispose(); Volatile.Write(ref _done, 1);
        }
    }
    public void Dispose()
    {
        _decoder?.Dispose(); _decoder = null; _ownerClient?.Dispose(); _ownerClient = null; _file?.Dispose(); _file = null; _memory?.Dispose(); _memory = null;
        if (_temporary is not null) { try { File.Delete(_temporary); } catch (IOException) { } _temporary = null; }
    }
}
