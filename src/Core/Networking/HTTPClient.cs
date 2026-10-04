using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
namespace Electron2D;

/// <summary>Executes polled HTTP/1.1 requests over owned TCP/TLS or a borrowed StreamPeer.</summary>
/// <remarks>Calls require the constructing thread. Request/header snapshots allocate outside body-span polling.
/// Caller-span body reads reuse prepared storage, remove transfer framing and preserve response byte order.
/// Header budgets reject oversized input. GetResponseHeaders consumes the header snapshot; status queries do not poll.</remarks>
public class HTTPClient : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private StreamPeer? _connection;
    private StreamPeerTCP? _tcp;
    private StreamPeerTLS? _tls;
    private TLSOptions? _options;
    private bool _ownOptions;
    private Task<IPAddress[]>? _resolve;
    private CancellationTokenSource? _resolveCancel;
    private IPAddress[] _addresses = [];
    private int _addressIndex, _port, _serverPort;
    private string _host = "", _serverHost = "", _httpProxy = "", _httpsProxy = "";
    private int _httpProxyPort = -1, _httpsProxyPort = -1;
    private bool _proxyTunnel, _proxyResponse, _headRequest, _connectRequest, _closeAfterBody, _chunked, _untilEOF, _hasResponse;
    private HTTPStatus _status;
    private byte[] _request = [], _header = new byte[65536];
    private int _requestOffset, _headerCount, _readChunkSize = 65536;
    private readonly byte[] _line = new byte[4096];
    private int _lineCount, _separator, _trailerBytes;
    private bool _trailers;
    private long _length = -1, _remaining, _chunkRemaining;
    private HTTPResponseCode _code;
    private string[] _headers = [];
    private bool _blocking;
    private static readonly string[] MethodNames = ["GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS", "TRACE", "CONNECT", "PATCH"];
    /// <summary>Creates a disconnected HTTP client with prepared header and chunk-line storage.</summary>
    public HTTPClient() { }
    private void Check() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("HTTP access requires its constructing thread."); }
    /// <summary>Gets or sets whether send/header/body operations may wait for progress.</summary><value>False initially; connection/DNS polling remains nonblocking.</value>
    public bool BlockingModeEnabled { get { Check(); return _blocking; } set { Check(); _blocking = value; } }
    /// <summary>Gets or sets the maximum decoded body bytes returned per read.</summary><value>65536 initially; 256 through 16 MiB.</value>
    public int ReadChunkSize { get { Check(); return _readChunkSize; } set { Check(); if (value is < 256 or > 16777216) throw new ArgumentOutOfRangeException(nameof(value)); _readChunkSize = value; } }
    /// <summary>Gets or sets the prepared response-header byte budget.</summary><value>65536 initially; 256 through 16 MiB, configurable while disconnected or idle.</value>
    public int MaxResponseHeaderBytes { get { Check(); return _header.Length; } set { Check(); if (_status is not (HTTPStatus.Disconnected or HTTPStatus.Connected)) throw new InvalidOperationException("HTTP headers are active."); if (value is < 256 or > 16777216) throw new ArgumentOutOfRangeException(nameof(value)); _header = new byte[value]; } }
    /// <summary>Gets the attached transport or borrows a live replacement.</summary><value>Null while closed. Replacing the transport closes owned state and discards the previous response.</value>
    /// <exception cref="ArgumentNullException">A null replacement is supplied.</exception>
    public StreamPeer? Connection
    {
        get { Check(); return _connection; }
        set { Check(); ArgumentNullException.ThrowIfNull(value); if (value.IsDisposed) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(value, _connection)) return; CloseCore(); _connection = value; _status = HTTPStatus.Connected; }
    }
    /// <summary>Starts connection to a host, optionally through a configured proxy and TLS.</summary><param name="host">DNS/IP host, optionally prefixed by http:// or https://.</param><param name="port">Remote port; negative selects 80 or 443.</param><param name="tlsOptions">Client TLS configuration; null selects plaintext unless https:// is supplied.</param>
    public void ConnectToHost(string host, int port = -1, TLSOptions? tlsOptions = null)
    {
        Check(); ArgumentException.ThrowIfNullOrEmpty(host);
        var ownNew = false;
        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) { host = host[7..]; tlsOptions = null; }
        else if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) { host = host[8..]; if (tlsOptions is null) { tlsOptions = TLSOptions.Client(); ownNew = true; } }
        try
        {
            if (host.Length == 0 || host.IndexOfAny(['/', '\r', '\n', '\0', ' ']) >= 0) throw new ArgumentException("A host name or IP is required.", nameof(host));
            if (tlsOptions?.IsServer() == true) throw new ArgumentException("Client TLS options are required.", nameof(tlsOptions));
            var selectedPort = port < 0 ? tlsOptions is null ? 80 : 443 : port; NetworkSockets.Port(selectedPort, true);
            CloseCore(); _ownOptions = ownNew; _options = tlsOptions; _host = host.Trim('[', ']'); _port = selectedPort;
        }
        catch { if (ownNew) tlsOptions?.Dispose(); throw; }
        _proxyTunnel = tlsOptions is not null && _httpsProxyPort >= 0;
        _serverHost = _proxyTunnel ? _httpsProxy : tlsOptions is null && _httpProxyPort >= 0 ? _httpProxy : _host;
        _serverPort = _proxyTunnel ? _httpsProxyPort : tlsOptions is null && _httpProxyPort >= 0 ? _httpProxyPort : _port;
        try
        {
            if (IPAddress.TryParse(_serverHost, out var address)) { _addresses = [address]; StartNextAddress(); }
            else { _resolveCancel = new(); _resolve = Dns.GetHostAddressesAsync(_serverHost, AddressFamily.Unspecified, _resolveCancel.Token); _status = HTTPStatus.Resolving; }
        }
        catch { Fail(HTTPStatus.CantConnect); throw; }
    }
    private bool StartNextAddress()
    {
        while (_addressIndex < _addresses.Length)
        {
            _tcp?.Dispose(); _tcp = new StreamPeerTCP(); _connection = _tcp;
            try { _tcp.ConnectToHost(_addresses[_addressIndex++].ToString(), _serverPort); _status = HTTPStatus.Connecting; return true; }
            catch (SocketException) { }
        }
        Fail(HTTPStatus.CantConnect); throw new SocketException((int)SocketError.HostUnreachable);
    }
    /// <summary>Sets a plaintext forward proxy, or clears it.</summary><param name="host">Empty clears.</param><param name="port">Minus one clears; otherwise a valid remote port.</param>
    public void SetHTTPProxy(string host, int port) { Check(); ConfigureProxy(host, port); _httpProxy = host; _httpProxyPort = host.Length == 0 ? -1 : port; }
    /// <summary>Sets a CONNECT tunnel proxy for HTTPS, or clears it.</summary><param name="host">Empty clears.</param><param name="port">Minus one clears; otherwise a valid remote port.</param>
    public void SetHTTPSProxy(string host, int port) { Check(); ConfigureProxy(host, port); _httpsProxy = host; _httpsProxyPort = host.Length == 0 ? -1 : port; }
    private static void ConfigureProxy(string host, int port) { ArgumentNullException.ThrowIfNull(host); if (host.Length != 0 && port != -1) NetworkSockets.Port(port, true); if (host.IndexOfAny(['\r', '\n', '\0']) >= 0) throw new ArgumentException("Invalid proxy host.", nameof(host)); }
    /// <summary>Returns the cached HTTP phase.</summary><returns>The last polled phase.</returns>
    public HTTPStatus GetStatus() { Check(); return _status; }
    /// <summary>Reports whether an unread response header snapshot exists, including headerless responses.</summary><returns>True until a header accessor consumes it.</returns>
    public bool HasResponse() { Check(); return _hasResponse; }
    /// <summary>Reports whether response transfer framing is chunked.</summary><returns>False before a response.</returns>
    public bool IsResponseChunked() { Check(); return _chunked; }
    /// <summary>Gets the response status code.</summary><returns>Zero before response; unknown valid numeric codes are preserved.</returns>
    public HTTPResponseCode GetResponseCode() { Check(); return _code; }
    /// <summary>Gets the declared wire-body length.</summary><returns>Minus one for chunked/close-delimited bodies; zero for bodyless responses.</returns>
    public long GetResponseBodyLength() { Check(); return _length; }
    /// <summary>Consumes the response header snapshot.</summary><returns>A caller-owned array preserving field order and duplicates.</returns>
    public string[] GetResponseHeaders() { Check(); var result = _headers; _headers = []; _hasResponse = false; return result; }
    /// <summary>Consumes response headers into a typed dictionary.</summary><returns>Ordinal field-name keys; the last identically spelled duplicate wins.</returns>
    public Dictionary<string, string> GetResponseHeadersAsDictionary() { var result = new Dictionary<string, string>(StringComparer.Ordinal); foreach (var line in GetResponseHeaders()) { var colon = line.IndexOf(':'); result[line[..colon].Trim()] = line[(colon + 1)..].Trim(); } return result; }
    /// <summary>Starts one request with UTF-8 body encoding.</summary><param name="method">Request method.</param><param name="url">Origin/absolute request target, CONNECT authority or OPTIONS asterisk.</param><param name="headers">Field lines copied for this request.</param><param name="body">UTF-8 request data.</param>
    public void Request(HTTPMethod method, string url, IEnumerable<string> headers, string body = "") { ArgumentNullException.ThrowIfNull(body); RequestRaw(method, url, headers, Encoding.UTF8.GetBytes(body)); }
    /// <summary>Starts one request with copied raw body bytes.</summary><param name="method">Request method.</param><param name="url">HTTP request target.</param><param name="headers">Copied field lines.</param><param name="body">Borrowed for this call, then copied.</param>
    public void RequestRaw(HTTPMethod method, string url, IEnumerable<string> headers, ReadOnlySpan<byte> body)
    {
        Check(); if (_status != HTTPStatus.Connected || _connection is null) throw new InvalidOperationException("An idle connected HTTP stream is required.");
        ValidateRequest(method, url, headers, out var fields); PrepareRequest(method, url, fields, body); _proxyResponse = false; _status = HTTPStatus.Requesting;
    }
    internal static void ValidateRequest(HTTPMethod method, string url, IEnumerable<string> headers, out string[] fields)
    {
        ArgumentException.ThrowIfNullOrEmpty(url); ArgumentNullException.ThrowIfNull(headers);
        if ((uint)method >= 9 || url.Any(c => c <= ' ' || c == 127)) throw new ArgumentException("Invalid request method or target.");
        if (method == HTTPMethod.Connect ? !url.Contains(':') : !(url.StartsWith('/') || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || method == HTTPMethod.Options && url == "*")) throw new ArgumentException("Invalid request-target form.");
        fields = headers.ToArray(); foreach (var field in fields) ValidateField(field);
    }
    private static void ValidateField(string field)
    {
        ArgumentNullException.ThrowIfNull(field); var colon = field.IndexOf(':'); if (colon <= 0 || field.AsSpan(0, colon).ContainsAnyExcept("!#$%&'*+-.^_`|~0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".AsSpan()) || field.Any(c => c is '\r' or '\n' or '\0' || c < 32 && c != '\t' || c == 127)) throw new ArgumentException("Invalid HTTP field line.");
    }
    private void PrepareRequest(HTTPMethod method, string url, string[] fields, ReadOnlySpan<byte> body)
    {
        var builder = new StringBuilder(); var target = _options is null && _httpProxyPort >= 0 && !_proxyResponse && !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "http://" + Authority() + url : url;
        builder.Append(MethodNames[(int)method]).Append(' ').Append(target).Append(" HTTP/1.1\r\n");
        bool host = false, length = false, user = false, accept = false;
        foreach (var field in fields)
        {
            var colon = field.IndexOf(':'); var name = field[..colon]; var value = field[(colon + 1)..].Trim();
            if (name.Equals("Host", StringComparison.OrdinalIgnoreCase)) host = true;
            if (name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Request transfer coding is not supported; supply raw bytes and Content-Length.");
            if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) { if (length || !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n != body.Length) throw new ArgumentException("Content-Length does not match the request body."); length = true; }
            if (name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase)) user = true; if (name.Equals("Accept", StringComparison.OrdinalIgnoreCase)) accept = true;
            builder.Append(field).Append("\r\n");
        }
        if (!host) builder.Append("Host: ").Append(Authority()).Append("\r\n"); if (!length && body.Length > 0) builder.Append("Content-Length: ").Append(body.Length.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
        if (!user) builder.Append("User-Agent: Electron2D\r\n"); if (!accept) builder.Append("Accept: */*\r\n"); builder.Append("\r\n");
        var head = Encoding.UTF8.GetBytes(builder.ToString()); _request = new byte[checked(head.Length + body.Length)]; head.CopyTo(_request, 0); body.CopyTo(_request.AsSpan(head.Length));
        _requestOffset = _headerCount = _lineCount = _separator = _trailerBytes = 0; _headRequest = method == HTTPMethod.Head; _connectRequest = method == HTTPMethod.Connect; _hasResponse = _chunked = _trailers = false; _headers = []; _length = -1; _remaining = _chunkRemaining = 0; _code = 0;
    }
    private string Authority() { var host = _host.Length == 0 ? "localhost" : _host.Contains(':') ? "[" + _host + "]" : _host; return _port == (_options is null ? 80 : 443) || _port == 0 ? host : host + ":" + _port.ToString(CultureInfo.InvariantCulture); }
    /// <summary>Advances DNS/connect/TLS/proxy and request/header progress.</summary><exception cref="IOException">Transport or HTTP framing fails; the phase records the failure.</exception>
    public void Poll()
    {
        Check(); if (_status == HTTPStatus.Resolving)
        {
            if (!_resolve!.IsCompleted) return;
            try { _addresses = _resolve.GetAwaiter().GetResult(); _resolve = null; }
            catch { Fail(HTTPStatus.CantResolve); throw; }
            StartNextAddress();
        }
        try
        {
            if (_status == HTTPStatus.Connecting)
            {
                try { _tcp!.Poll(); } catch (SocketException) { StartNextAddress(); return; }
                if (_tcp!.GetStatus() != StreamSocketStatus.Connected) return;
                if (_proxyTunnel) { _proxyResponse = true; PrepareRequest(HTTPMethod.Connect, Authority(), [], ReadOnlySpan<byte>.Empty); _status = HTTPStatus.Requesting; }
                else if (_tls is null) StartTLSOrConnected();
            }
            if (_connection is StreamPeerTLS activeTLS) { activeTLS.Poll(); if (_status == HTTPStatus.Connecting) { if (activeTLS.GetStatus() == TLSStatus.Connected) _status = HTTPStatus.Connected; else return; } }
            else if (_tcp is not null && _status != HTTPStatus.Connecting) _tcp.Poll();
            if (_status == HTTPStatus.Requesting)
            {
                while (_requestOffset < _request.Length) { var sent = _connection!.PutPartialData(_request.AsSpan(_requestOffset)); _requestOffset += sent; if (sent == 0) { if (!_blocking) return; Thread.Yield(); } }
                Span<byte> one = stackalloc byte[1];
                while (_status == HTTPStatus.Requesting)
                {
                    var read = ReadTransport(one); if (read == 0) { if (!_blocking) return; Thread.Yield(); continue; }
                    if (read < 0) throw new EndOfStreamException("Response ended before its headers.");
                    if (_headerCount == _header.Length) throw new InvalidDataException("HTTP response headers exceed the prepared budget."); _header[_headerCount++] = one[0];
                    if (_headerCount >= 4 && _header.AsSpan(_headerCount - 4, 4).SequenceEqual("\r\n\r\n"u8) || _headerCount >= 2 && _header.AsSpan(_headerCount - 2, 2).SequenceEqual("\n\n"u8)) ParseHeaders();
                }
            }
            if (_status == HTTPStatus.Connected && IsClosed()) { ReleaseOwned(); _connection = null; _status = HTTPStatus.Disconnected; }
        }
        catch (AuthenticationException) { Fail(_status == HTTPStatus.Connecting ? HTTPStatus.TLSHandshakeError : HTTPStatus.ConnectionError); throw; }
        catch { if (_status is not (HTTPStatus.CantConnect or HTTPStatus.CantResolve)) Fail(HTTPStatus.ConnectionError); throw; }
    }
    private void StartTLSOrConnected()
    {
        if (_options is null) { _status = HTTPStatus.Connected; return; }
        _tls = new StreamPeerTLS(); _tls.ConnectToStream(_tcp!, _host, _options); _connection = _tls; _status = HTTPStatus.Connecting;
    }
    private void ParseHeaders()
    {
        var lines = Encoding.Latin1.GetString(_header, 0, _headerCount).Split('\n'); var status = lines[0].TrimEnd('\r').Split(' ', 3);
        if (status.Length < 2 || status[0] is not ("HTTP/1.1" or "HTTP/1.0") || !int.TryParse(status[1], NumberStyles.None, CultureInfo.InvariantCulture, out var code) || code is < 100 or > 999) throw new InvalidDataException("Invalid HTTP status line.");
        var headers = new List<string>(); long length = -1; bool chunked = false, close = status[0] == "HTTP/1.0";
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r'); if (line.Length == 0) continue; try { ValidateField(line); } catch (ArgumentException error) { throw new InvalidDataException("Invalid response field line.", error); }
            headers.Add(line); var colon = line.IndexOf(':'); var name = line.AsSpan(0, colon); var value = line.AsSpan(colon + 1).Trim();
            if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) { foreach (var token in value.ToString().Split(',')) { if (!long.TryParse(token.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 0 || length >= 0 && length != n) throw new InvalidDataException("Conflicting or invalid Content-Length."); length = n; } }
            if (name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) { if (chunked || !value.Equals("chunked", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsupported or duplicated transfer coding."); chunked = true; }
            if (name.Equals("Connection", StringComparison.OrdinalIgnoreCase)) { foreach (var token in value.ToString().Split(',')) { if (token.Trim().Equals("close", StringComparison.OrdinalIgnoreCase)) close = true; else if (token.Trim().Equals("keep-alive", StringComparison.OrdinalIgnoreCase) && status[0] == "HTTP/1.0") close = false; } }
        }
        if (chunked && length >= 0) throw new InvalidDataException("Ambiguous transfer framing.");
        _headerCount = 0; if (code is >= 100 and < 200 && code != 101) return;
        if (_proxyResponse) { if (code is < 200 or >= 300) { Fail(HTTPStatus.CantConnect); throw new IOException("HTTPS proxy refused CONNECT."); } _proxyResponse = _proxyTunnel = false; StartTLSOrConnected(); return; }
        _code = (HTTPResponseCode)code; _headers = headers.ToArray(); _hasResponse = true; _closeAfterBody = close;
        var bodyless = _headRequest || _connectRequest && code is >= 200 and < 300 || code is 101 or 204 or 205 or 304; _chunked = !bodyless && chunked; _length = bodyless ? 0 : chunked ? -1 : length; _remaining = _length; _untilEOF = !bodyless && !chunked && length < 0;
        _status = _length == 0 ? HTTPStatus.Connected : HTTPStatus.Body;
    }
    private bool IsClosed() => _connection is null || _connection.IsDisposed || _connection is StreamPeerSocket socket && socket.GetStatus() == StreamSocketStatus.None || _connection is StreamPeerTLS tls && tls.GetStatus() == TLSStatus.Disconnected;
    private int ReadTransport(Span<byte> destination)
    {
        if (IsClosed()) return -1;
        try { var count = _connection!.GetPartialData(destination); return count == 0 && IsClosed() ? -1 : count; } catch (EndOfStreamException) { return -1; }
    }
    /// <summary>Reads a copied decoded body prefix.</summary><returns>A caller-owned array, empty while waiting or after completion.</returns>
    public byte[] ReadResponseBodyChunk() { Check(); var bytes = new byte[_readChunkSize]; var count = ReadResponseBodyChunk(bytes.AsSpan()); if (count != bytes.Length) Array.Resize(ref bytes, count); return bytes; }
    /// <summary>Reads a decoded body prefix into reused caller storage.</summary><param name="destination">Borrowed output storage.</param><returns>Decoded bytes, or zero while waiting/after completion; inspect GetStatus.</returns>
    public int ReadResponseBodyChunk(Span<byte> destination)
    {
        Check(); if (_status != HTTPStatus.Body || destination.IsEmpty) return 0;
        try
        {
            if (_tls is not null) _tls.Poll(); var output = destination[..Math.Min(destination.Length, _readChunkSize)]; Span<byte> one = stackalloc byte[1];
            if (_chunked)
            {
                while (_chunkRemaining == 0)
                {
                    if (_separator > 0) { var n = ReadTransport(one); if (n == 0) { if (!_blocking) return 0; Thread.Yield(); continue; } if (n < 0 || one[0] != (_separator == 2 ? '\r' : '\n')) throw new InvalidDataException("Invalid chunk separator."); _separator--; continue; }
                    if (!ReadChunkLine()) return 0;
                    if (_trailers) { _trailerBytes += _lineCount; if (_trailerBytes > _header.Length) throw new InvalidDataException("Trailers exceed the header budget."); if (_lineCount == 2) { _lineCount = 0; CompleteBody(); return 0; } _lineCount = 0; continue; }
                    var line = _line.AsSpan(0, _lineCount - 2); var semicolon = line.IndexOf((byte)';'); if (semicolon >= 0) line = line[..semicolon]; long size = 0; if (line.IsEmpty) throw new InvalidDataException("Empty chunk size.");
                    foreach (var b in line) { var digit = b is >= (byte)'0' and <= (byte)'9' ? b - '0' : b is >= (byte)'a' and <= (byte)'f' ? b - 'a' + 10 : b is >= (byte)'A' and <= (byte)'F' ? b - 'A' + 10 : -1; if (digit < 0 || size > (long.MaxValue - digit) / 16) throw new InvalidDataException("Invalid chunk size."); size = size * 16 + digit; }
                    _lineCount = 0; _chunkRemaining = size; if (size == 0) { _trailers = true; continue; }
                }
                output = output[..(int)Math.Min(output.Length, _chunkRemaining)];
            }
            else if (!_untilEOF) output = output[..(int)Math.Min(output.Length, _remaining)];
            var read = ReadTransport(output); while (read == 0 && _blocking) { Thread.Yield(); read = ReadTransport(output); }
            if (read < 0) { if (!_untilEOF) throw new EndOfStreamException("Response body is shorter than its framing."); CompleteBody(); return 0; }
            if (_chunked) { _chunkRemaining -= read; if (_chunkRemaining == 0) _separator = 2; }
            else if (!_untilEOF) { _remaining -= read; if (_remaining == 0) CompleteBody(); }
            return read;
        }
        catch { Fail(HTTPStatus.ConnectionError); throw; }
    }
    private bool ReadChunkLine()
    {
        Span<byte> one = stackalloc byte[1]; while (true) { var read = ReadTransport(one); if (read < 0) throw new EndOfStreamException("Chunk framing ended early."); if (read == 0) { if (!_blocking) return false; Thread.Yield(); continue; } if (_lineCount == _line.Length) throw new InvalidDataException("Chunk line exceeds its budget."); _line[_lineCount++] = one[0]; if (_lineCount >= 2 && _line[_lineCount - 2] == '\r' && _line[_lineCount - 1] == '\n') return true; }
    }
    private void CompleteBody() { if (_closeAfterBody || _untilEOF) { ReleaseOwned(); _connection = null; _status = HTTPStatus.Disconnected; } else _status = HTTPStatus.Connected; }
    private void Fail(HTTPStatus status) { ReleaseOwned(); _connection = null; _status = status; }
    private void ReleaseOwned() { _resolveCancel?.Cancel(); _resolveCancel?.Dispose(); _resolveCancel = null; _resolve = null; _tls?.Dispose(); _tls = null; _tcp?.Dispose(); _tcp = null; if (_ownOptions) _options?.Dispose(); _ownOptions = false; _options = null; }
    /// <summary>Closes owned network state and forgets a borrowed connection without disposing it.</summary>
    public void Close() { Check(); CloseCore(); }
    private void CloseCore() { ReleaseOwned(); _connection = null; _status = HTTPStatus.Disconnected; _request = []; _headers = []; _hasResponse = false; _headerCount = _requestOffset = _lineCount = _separator = _trailerBytes = _addressIndex = 0; _length = -1; _remaining = _chunkRemaining = 0; _chunked = _untilEOF = _trailers = _proxyTunnel = _proxyResponse = false; _code = 0; }
    /// <summary>Builds an escaped typed query string, preserving input order and valueless null entries.</summary><param name="values">Typed scalar query entries.</param><returns>Query text without a leading question mark.</returns>
    public static string QueryStringFromDict(IEnumerable<KeyValuePair<string, string?>> values) { ArgumentNullException.ThrowIfNull(values); return string.Join('&', values.Select(v => Uri.EscapeDataString(v.Key) + (v.Value is null ? "" : "=" + Uri.EscapeDataString(v.Value)))); }
    /// <summary>Builds an escaped query with repeated values for each key.</summary><param name="values">Typed array-valued query entries.</param><returns>Query text without a leading question mark.</returns>
    public static string QueryStringFromDict(IEnumerable<KeyValuePair<string, string?[]>> values) { ArgumentNullException.ThrowIfNull(values); return string.Join('&', values.SelectMany(v => v.Value.Select(item => Uri.EscapeDataString(v.Key) + "=" + Uri.EscapeDataString(item ?? "")))); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<HTTPClient, bool>(nameof(BlockingModeEnabled), c => c.BlockingModeEnabled, (c, v) => c.BlockingModeEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<HTTPClient, int>(nameof(ReadChunkSize), c => c.ReadChunkSize, (c, v) => c.ReadChunkSize = v, _ => 65536, stored: true);
        yield return new PropertyDescriptor<HTTPClient, int>(nameof(MaxResponseHeaderBytes), c => c.MaxResponseHeaderBytes, (c, v) => c.MaxResponseHeaderBytes = v, _ => 65536, stored: true);
        yield return new PropertyDescriptor<HTTPClient, StreamPeer?>(nameof(Connection), c => c.Connection, (c, v) => c.Connection = v, _ => null);
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); Check(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) CloseCore(); base.Dispose(disposing); }
}
