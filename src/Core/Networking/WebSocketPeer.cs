using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using CryptoRandom = System.Security.Cryptography.RandomNumberGenerator;
using System.Text;
namespace Electron2D;

/// <summary>Describes a WebSocket connection independently of its transport.</summary>
public enum WebSocketState
{
    /// <summary>Transport or HTTP upgrade establishment is pending.</summary>
    Connecting = 0,
    /// <summary>Complete text and binary messages can be exchanged.</summary>
    Open = 1,
    /// <summary>A close handshake is pending; keep polling.</summary>
    Closing = 2,
    /// <summary>No active connection exists.</summary>
    Closed = 3
}
/// <summary>Selects the WebSocket message payload contract.</summary>
public enum WebSocketWriteMode
{
    /// <summary>Payload bytes must be valid UTF-8 text.</summary>
    Text = 0,
    /// <summary>Payload bytes are arbitrary binary data.</summary>
    Binary = 1
}

/// <summary>Exchanges complete WebSocket messages over polled WS/WSS or a borrowed ordered stream.</summary>
/// <remarks>Calls and disposal require the constructing thread. ConnectToURL owns its HTTP/TCP/TLS state;
/// AcceptStream borrows its stream and never disposes it. Poll advances handshakes, messages and close/control frames.
/// Prepared span sends/reads and polling use bounded storage; snapshots, handshakes and close diagnostics are cold work.</remarks>
public class WebSocketPeer : PacketPeer
{
    private static readonly UTF8Encoding TextEncoding = new(false, true);
    private const int MaximumCapacity = 64 * 1024 * 1024;
    private int _inboundSize = 65535, _outboundSize = 65535, _maxPackets = 4096;
    private double _heartbeat;
    private long _lastHeartbeat;
    private bool _heartbeatWaiting, _server, _requestSent, _responsePending, _wasText, _closeSent, _closeReceived, _stopReading;
    private string[] _protocols = [], _headers = [], _activeProtocols = [];
    private string _url = "", _selectedProtocol = "", _key = "", _closeReason = "";
    private int _closeCode = -1;
    private WebSocketState _state = WebSocketState.Closed;
    private HTTPClient? _http;
    private StreamPeer? _stream;
    private StreamPeerTCP? _tcp;
    private readonly byte[] _handshake = new byte[4096], _frame = new byte[14], _control = new byte[125];
    private byte[] _handshakeResponse = [], _message = [], _send = [];
    private int _handshakeCount, _responseOffset, _frameCount, _frameNeeded = 2, _payloadSize, _payloadRead, _messageSize, _messageOpcode, _opcode;
    private bool _final, _masked, _frameReady, _messageReady;
    private readonly WebSocketPacketQueue _input = new(), _output = new();
    /// <summary>Creates a closed WebSocket peer with default configuration.</summary>
    public WebSocketPeer() { }
    private void Idle() { CheckPacketPeer(); if (_state != WebSocketState.Closed) throw new InvalidOperationException("Buffer configuration requires a closed WebSocket."); }
    private static void Capacity(int value) { if (value < 0 || value > MaximumCapacity) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <summary>Gets or sets the maximum incoming message/queued payload bytes.</summary><value>65535 initially; zero accepts only empty data messages. Configurable while closed, up to 64 MiB.</value>
    /// <exception cref="ArgumentOutOfRangeException">The byte budget is outside zero through 64 MiB.</exception>
    /// <exception cref="InvalidOperationException">The connection is active.</exception>
    public int InboundBufferSize { get { CheckPacketPeer(); return _inboundSize; } set { Idle(); Capacity(value); _inboundSize = value; } }
    /// <summary>Gets or sets the outgoing message/queued payload budget.</summary><value>65535 initially; zero selects the explicit 64 MiB prepared ceiling. Configurable while closed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The byte budget is outside zero through 64 MiB.</exception>
    /// <exception cref="InvalidOperationException">The connection is active.</exception>
    public int OutboundBufferSize { get { CheckPacketPeer(); return _outboundSize; } set { Idle(); Capacity(value); _outboundSize = value; } }
    /// <summary>Gets or sets the number of queued application messages in either direction.</summary><value>4096 initially; zero through 65536 while closed. Control traffic uses four additional reserved records.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside zero through 65536.</exception>
    /// <exception cref="InvalidOperationException">The connection is active.</exception>
    public int MaxQueuedPackets { get { CheckPacketPeer(); return _maxPackets; } set { Idle(); if (value is < 0 or > 65536) throw new ArgumentOutOfRangeException(nameof(value)); _maxPackets = value; } }
    /// <summary>Gets or sets the ping interval in monotonic seconds.</summary><value>Zero disables heartbeat. Finite nonnegative values reset the timer; an unanswered ping at the next interval aborts.</value>
    /// <exception cref="ArgumentOutOfRangeException">The interval is negative, nonfinite or exceeds the monotonic clock range.</exception>
    public double HeartbeatInterval { get { CheckPacketPeer(); return _heartbeat; } set { CheckPacketPeer(); if (!double.IsFinite(value) || value < 0 || value > long.MaxValue / 1000d) throw new ArgumentOutOfRangeException(nameof(value)); _heartbeat = value; _lastHeartbeat = Environment.TickCount64; _heartbeatWaiting = false; } }
    /// <summary>Gets or sets copied, trimmed, unique subprotocol tokens for the next handshake.</summary><value>Empty initially. A configured protocol list requires a negotiated member.</value>
    /// <exception cref="ArgumentException">A protocol is null, duplicated or not an HTTP token.</exception>
    public string[] SupportedProtocols
    {
        get { CheckPacketPeer(); return (string[])_protocols.Clone(); }
        set { CheckPacketPeer(); ArgumentNullException.ThrowIfNull(value); var copy = value.Select(p => (p ?? throw new ArgumentException("Null protocol.")).Trim()).ToArray(); if (copy.Any(p => !Token(p)) || copy.Distinct(StringComparer.Ordinal).Count() != copy.Length) throw new ArgumentException("Unique HTTP protocol tokens are required.", nameof(value)); _protocols = copy; }
    }
    /// <summary>Gets or sets copied extra field lines for the next client or server handshake.</summary><value>Empty initially. Required framing/upgrade fields cannot be overridden.</value>
    /// <exception cref="ArgumentException">A header is invalid or overrides a reserved handshake field.</exception>
    public string[] HandshakeHeaders
    {
        get { CheckPacketPeer(); return (string[])_headers.Clone(); }
        set { CheckPacketPeer(); ArgumentNullException.ThrowIfNull(value); HTTPClient.ValidateRequest(HTTPMethod.Get, "/", value, out var copy); foreach (var headerLine in copy) { var name = headerLine[..headerLine.IndexOf(':')]; if (name.Equals("Host", StringComparison.OrdinalIgnoreCase) || name.Equals("Upgrade", StringComparison.OrdinalIgnoreCase) || name.Equals("Connection", StringComparison.OrdinalIgnoreCase) || name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) || name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Sec-WebSocket-", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("A reserved WebSocket handshake field cannot be overridden.", nameof(value)); } _headers = copy; }
    }
    private static bool Token(string value) => value.Length > 0 && !value.AsSpan().ContainsAnyExcept("!#$%&'*+-.^_`|~0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".AsSpan());
    private void Prepare()
    {
        var outgoing = _outboundSize == 0 ? MaximumCapacity : _outboundSize;
        var message = new byte[_inboundSize]; var send = new byte[Math.Max(125, outgoing) + 14];
        _input.Prepare(_inboundSize, _maxPackets); _output.Prepare(checked(outgoing + 14 * (_maxPackets + 4) + 500), _maxPackets + 4);
        _message = message; _send = send; _activeProtocols = (string[])_protocols.Clone();
    }
    /// <summary>Starts nonblocking WS/WSS connection and HTTP upgrade.</summary><param name="url">URL, optionally without ws://; credentials are forbidden and fragments are not sent.</param><param name="tlsClientOptions">Borrowed client options for WSS; null uses system trust/name validation.</param>
    /// <exception cref="ArgumentException">The URL or TLS role is invalid.</exception>
    /// <exception cref="InvalidOperationException">A connection is already active.</exception>
    public void ConnectToURL(string url, TLSOptions? tlsClientOptions = null)
    {
        CheckPacketPeer(); if (_state is not (WebSocketState.Closed or WebSocketState.Closing)) throw new InvalidOperationException("A WebSocket is already active.");
        ArgumentException.ThrowIfNullOrEmpty(url); var normalized = url.Contains("://", StringComparison.Ordinal) ? url : "ws://" + url;
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) || uri.Scheme is not ("ws" or "wss") || uri.Host.Length == 0 || uri.UserInfo.Length != 0) throw new ArgumentException("A WS/WSS URL without credentials is required.", nameof(url));
        if (tlsClientOptions?.IsServer() == true) throw new ArgumentException("Client TLS options are required.", nameof(tlsClientOptions));
        Reset(); Prepare(); _url = url; _server = false;
        Span<byte> nonce = stackalloc byte[16]; CryptoRandom.Fill(nonce); _key = Convert.ToBase64String(nonce);
        _http = new HTTPClient { MaxResponseHeaderBytes = _handshake.Length };
        try { _http.ConnectToHost((uri.Scheme == "wss" ? "https://" : "http://") + uri.IdnHost, uri.Port, tlsClientOptions); _state = WebSocketState.Connecting; }
        catch { Abort(); throw; }
        _target = uri.PathAndQuery;
    }
    private string _target = "/";
    /// <summary>Starts a server upgrade over a borrowed live stream, including an established/pending TLS stream.</summary><param name="stream">Ordered stream; custom transports support framing but have no native endpoint or TCP option.</param>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    /// <exception cref="ObjectDisposedException">The borrowed stream is disposed.</exception>
    /// <exception cref="InvalidOperationException">A connection is already active.</exception>
    public void AcceptStream(StreamPeer stream)
    {
        CheckPacketPeer(); ArgumentNullException.ThrowIfNull(stream); if (stream.IsDisposed) throw new ObjectDisposedException(nameof(stream));
        if (_state is not (WebSocketState.Closed or WebSocketState.Closing)) throw new InvalidOperationException("A WebSocket is already active.");
        Reset(); Prepare(); _stream = stream; _tcp = BaseTCP(stream); _server = true; _state = WebSocketState.Connecting; _tcp?.SetNoDelay(true);
    }
    private static StreamPeerTCP? BaseTCP(StreamPeer? stream) => stream as StreamPeerTCP ?? (stream as StreamPeerTLS)?.GetStream() as StreamPeerTCP;
    /// <summary>Gets the latest explicit WebSocket state.</summary><returns>The cached state; this does not poll.</returns>
    public WebSocketState GetReadyState() { CheckPacketPeer(); return _state; }
    /// <summary>Gets the received close status or a locally detected protocol-error status.</summary><returns>Minus one for transport/forced closure; 1005 for a received empty close. Query while closed.</returns>
    /// <exception cref="InvalidOperationException">The WebSocket has not closed.</exception>
    public int GetCloseCode() { CheckPacketPeer(); if (_state != WebSocketState.Closed) throw new InvalidOperationException("Close metadata is available after closure."); return _closeCode; }
    /// <summary>Gets the close reason after closure.</summary><returns>Received UTF-8 reason or local protocol diagnostic; empty after an unclean close.</returns>
    /// <exception cref="InvalidOperationException">The WebSocket has not closed.</exception>
    public string GetCloseReason() { CheckPacketPeer(); if (_state != WebSocketState.Closed) throw new InvalidOperationException("Close metadata is available after closure."); return _closeReason; }
    /// <summary>Gets the underlying native remote address.</summary><returns>IP literal, or empty for closed/custom streams.</returns>
    public string GetConnectedHost() { CheckPacketPeer(); return _tcp?.GetConnectedHost() ?? ""; }
    /// <summary>Gets the underlying native remote port.</summary><returns>Port, or zero for closed/custom streams.</returns>
    public int GetConnectedPort() { CheckPacketPeer(); return _tcp?.GetConnectedPort() ?? 0; }
    /// <summary>Gets the requested client URL or server URL assembled from Host and target.</summary><returns>Empty before a handshake; retained after closure until the next connection.</returns>
    public string GetRequestedURL() { CheckPacketPeer(); return _url; }
    /// <summary>Gets the negotiated subprotocol.</summary><returns>Empty if no subprotocol was negotiated.</returns>
    public string GetSelectedProtocol() { CheckPacketPeer(); return _selectedProtocol; }
    /// <summary>Gets application payload bytes retained in the outbound queue.</summary><returns>Includes a partially sent message until its frame drains; excludes control/frame overhead.</returns>
    public int GetCurrentOutboundBufferedAmount() { CheckPacketPeer(); return _output.PayloadBytes; }
    /// <summary>Sets TCP_NODELAY on the live native transport.</summary><param name="enabled">Whether to bypass Nagle aggregation; enabled automatically at connection.</param>
    /// <exception cref="NotSupportedException">A custom stream has no TCP transport.</exception>
    public void SetNoDelay(bool enabled) { CheckPacketPeer(); (_tcp ?? throw new NotSupportedException("The WebSocket has no native TCP transport.")).SetNoDelay(enabled); }
    /// <summary>Reports the text marker of the most recently consumed application message.</summary><returns>False initially; too-small/failed reads do not change it.</returns>
    public bool WasStringPacket() { CheckPacketPeer(); return _wasText; }
    /// <inheritdoc />
    public override int GetAvailablePacketCount() { CheckPacketPeer(); return _state == WebSocketState.Open ? _input.Count : 0; }
    /// <inheritdoc />
    public override int GetMaxPacketSize() { CheckPacketPeer(); return _outboundSize == 0 ? MaximumCapacity : _outboundSize; }
    /// <inheritdoc />
    protected override int NextPacketSize() => _state == WebSocketState.Open ? _input.NextSize : -1;
    /// <inheritdoc />
    protected override void ReadPacketCore(Span<byte> destination) { _wasText = _input.NextText; _input.Take(destination); }
    /// <inheritdoc />
    public override void PutPacket(ReadOnlySpan<byte> data) => Send(data);
    /// <summary>Copies and queues one complete message, then attempts immediate nonblocking transmission.</summary><param name="message">Borrowed payload.</param><param name="writeMode">Binary initially; text requires strict UTF-8.</param>
    /// <exception cref="InvalidOperationException">Not open or prepared queue capacity is exhausted; the message is not queued.</exception>
    /// <exception cref="ArgumentException">The payload exceeds its budget or text is invalid UTF-8.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The write mode is not text or binary.</exception>
    public void Send(ReadOnlySpan<byte> message, WebSocketWriteMode writeMode = WebSocketWriteMode.Binary)
    {
        CheckPacketPeer(); if ((uint)writeMode > 1) throw new ArgumentOutOfRangeException(nameof(writeMode)); if (_state != WebSocketState.Open) throw new InvalidOperationException("An open WebSocket is required.");
        if (message.Length > GetMaxPacketSize()) throw new ArgumentException("Message exceeds the prepared output limit.", nameof(message));
        if (writeMode == WebSocketWriteMode.Text && !ValidUTF8(message)) throw new ArgumentException("Text payload must be valid UTF-8.", nameof(message));
        QueueFrame(writeMode == WebSocketWriteMode.Text ? 1 : 2, message); Flush();
    }
    /// <summary>Encodes and sends one UTF-8 text message through prepared storage.</summary><param name="message">Text with valid UTF-16; no temporary encoded array is allocated.</param>
    /// <exception cref="EncoderFallbackException">The string contains invalid UTF-16.</exception>
    /// <exception cref="ArgumentException">Encoded text exceeds the output budget.</exception>
    /// <exception cref="InvalidOperationException">Not open or the prepared queue capacity is exhausted.</exception>
    public void SendText(string message)
    {
        CheckPacketPeer(); ArgumentNullException.ThrowIfNull(message); if (_state != WebSocketState.Open) throw new InvalidOperationException("An open WebSocket is required.");
        var count = TextEncoding.GetByteCount(message); if (count > GetMaxPacketSize()) throw new ArgumentException("Text exceeds the prepared output limit.", nameof(message));
        TextEncoding.GetBytes(message.AsSpan(), _send.AsSpan(14, count)); QueueFrame(1, _send.AsSpan(14, count)); Flush();
    }
    private static bool ValidUTF8(ReadOnlySpan<byte> bytes) { while (!bytes.IsEmpty) { if (Rune.DecodeFromUtf8(bytes, out _, out var consumed) != OperationStatus.Done) return false; bytes = bytes[consumed..]; } return true; }
    private void QueueFrame(int opcode, ReadOnlySpan<byte> payload)
    {
        var application = opcode is 1 or 2;
        if (application && (_output.ApplicationCount >= _maxPackets || _output.PayloadBytes > GetMaxPacketSize() - payload.Length)) throw new InvalidOperationException("Prepared WebSocket output capacity is exhausted.");
        var header = payload.Length < 126 ? 2 : payload.Length <= ushort.MaxValue ? 4 : 10; var size = header + (_server ? 0 : 4) + payload.Length;
        if (!_output.CanStore(size)) throw new InvalidOperationException("Prepared WebSocket frame/control capacity is exhausted.");
        payload.CopyTo(_send.AsSpan(header + (_server ? 0 : 4))); _send[0] = (byte)(0x80 | opcode); _send[1] = (byte)((_server ? 0 : 0x80) | (header == 2 ? payload.Length : header == 4 ? 126 : 127));
        if (header == 4) BinaryPrimitives.WriteUInt16BigEndian(_send.AsSpan(2), (ushort)payload.Length); else if (header == 10) BinaryPrimitives.WriteUInt64BigEndian(_send.AsSpan(2), (ulong)payload.Length);
        if (!_server) { var mask = _send.AsSpan(header, 4); CryptoRandom.Fill(mask); for (var i = 0; i < payload.Length; i++) _send[header + 4 + i] ^= mask[i & 3]; }
        _output.Store(_send.AsSpan(0, size), application ? payload.Length : 0, opcode == 1, application);
    }
    private void Flush()
    {
        try { while (_stream is not null && _output.Count != 0) { var count = _stream.PutPartialData(_output.Peek()); if (count == 0) return; _output.Consume(count); } }
        catch { Abort(); throw; }
    }
    /// <summary>Starts a close handshake, or immediately aborts for a negative code.</summary><param name="code">1000 initially; valid protocol/application status. Negative aborts silently.</param><param name="reason">Valid UTF-8 text, at most 123 encoded bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">The nonnegative close code is reserved or invalid.</exception>
    /// <exception cref="ArgumentException">The encoded reason exceeds 123 bytes.</exception>
    /// <exception cref="EncoderFallbackException">The reason contains invalid UTF-16.</exception>
    public void Close(int code = 1000, string reason = "")
    {
        CheckPacketPeer(); ArgumentNullException.ThrowIfNull(reason); if (code < 0) { Abort(); return; }
        if (!ValidCloseCode(code)) throw new ArgumentOutOfRangeException(nameof(code)); var length = TextEncoding.GetByteCount(reason); if (length > 123) throw new ArgumentException("Close reason exceeds 123 UTF-8 bytes.", nameof(reason));
        if (_state == WebSocketState.Connecting) { Abort(); return; }
        if (_state != WebSocketState.Open) return;
        BinaryPrimitives.WriteUInt16BigEndian(_control, (ushort)code); TextEncoding.GetBytes(reason.AsSpan(), _control.AsSpan(2)); QueueFrame(8, _control.AsSpan(0, length + 2)); _closeSent = true; _state = WebSocketState.Closing; Flush();
    }
    private static bool ValidCloseCode(int code) => code is 1000 or 1001 or 1002 or 1003 or >= 1007 and <= 1014 or >= 3000 and <= 4999;
    /// <summary>Advances transport/upgrade, complete-message queues, heartbeat and close handshake without waiting.</summary>
    /// <exception cref="IOException">Transport or handshake validation fails; owned state is released.</exception>
    public void Poll()
    {
        CheckPacketPeer(); if (_state == WebSocketState.Closed) return;
        try
        {
            if (_state == WebSocketState.Connecting) { if (_server) ServerHandshake(); else ClientHandshake(); if (_state != WebSocketState.Open) return; }
            PollStream(); if (TransportClosed()) { Abort(); return; }
            Flush(); if (!_stopReading) Receive(); Flush();
            if (_state == WebSocketState.Open && _heartbeat > 0 && (Environment.TickCount64 - _lastHeartbeat) / 1000d >= _heartbeat)
            {
                if (_heartbeatWaiting) { Abort(); return; }
                QueueFrame(9, ReadOnlySpan<byte>.Empty); _heartbeatWaiting = true; _lastHeartbeat = Environment.TickCount64; Flush();
            }
            if (_state == WebSocketState.Closing && _output.Count == 0 && (_closeReceived || _stopReading)) FinishClose();
        }
        catch { Abort(); throw; }
    }
    private void PollStream() { _tcp?.Poll(); if (_stream is StreamPeerTLS tls) tls.Poll(); }
    private bool TransportClosed() => _stream is null || _stream.IsDisposed || (_stream is StreamPeerTLS tls ? tls.GetStatus() is TLSStatus.Disconnected or TLSStatus.Error or TLSStatus.ErrorHostnameMismatch : _tcp?.GetStatus() == StreamSocketStatus.None);
    private int Read(Span<byte> bytes) { if (TransportClosed()) return -1; try { var read = _stream!.GetPartialData(bytes); return read == 0 && TransportClosed() ? -1 : read; } catch (EndOfStreamException) { return -1; } }
    private void ClientHandshake()
    {
        _http!.Poll();
        if (!_requestSent && _http.GetStatus() == HTTPStatus.Connected)
        {
            var fields = new List<string> { "Upgrade: websocket", "Connection: Upgrade", "Sec-WebSocket-Key: " + _key, "Sec-WebSocket-Version: 13" }; if (_activeProtocols.Length > 0) fields.Add("Sec-WebSocket-Protocol: " + string.Join(",", _activeProtocols)); fields.AddRange(_headers);
            _http.Request(HTTPMethod.Get, _target, fields); _requestSent = true; return;
        }
        if (!_http.HasResponse()) { if (_http.GetStatus() == HTTPStatus.Disconnected) throw new EndOfStreamException("WebSocket upgrade ended early."); return; }
        if (!_http.ResponseUsesHTTP11 || _http.GetResponseCode() != HTTPResponseCode.SwitchingProtocols) throw new InvalidDataException("WebSocket upgrade requires HTTP 101.");
        var headers = ParseFields(_http.GetResponseHeaders()); RequireUpgrade(headers); if (Field(headers, "Sec-WebSocket-Accept") != AcceptKey(_key) || Field(headers, "Sec-WebSocket-Extensions").Length != 0) throw new InvalidDataException("Invalid WebSocket accept or unrequested extension.");
        SelectProtocol(headers, false); _stream = _http.Connection; _tcp = BaseTCP(_stream); _tcp?.SetNoDelay(true); Open();
    }
    private static Dictionary<string, string> ParseFields(IEnumerable<string> fields)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in fields) { var colon = line.IndexOf(':'); if (colon <= 0 || !Token(line[..colon]) || line.Any(c => c < 32 && c != '\t' || c == 127)) throw new InvalidDataException("Invalid upgrade field."); var name = line[..colon]; var value = line[(colon + 1)..].Trim(); if (result.TryGetValue(name, out var previous)) value = previous + "," + value; result[name] = value; }
        return result;
    }
    private static string Field(Dictionary<string, string> fields, string name) => fields.TryGetValue(name, out var value) ? value : "";
    private static bool HasToken(string value, string expected) => value.Split(',').Any(t => t.Trim().Equals(expected, StringComparison.OrdinalIgnoreCase));
    private static void RequireUpgrade(Dictionary<string, string> fields) { if (!HasToken(Field(fields, "Connection"), "Upgrade") || !HasToken(Field(fields, "Upgrade"), "websocket")) throw new InvalidDataException("Required WebSocket upgrade tokens are absent."); }
    private static string AcceptKey(string key) => Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
    private void SelectProtocol(Dictionary<string, string> headers, bool server)
    {
        var offered = Field(headers, "Sec-WebSocket-Protocol");
        if (server) { if (offered.Length != 0) { foreach (var token in offered.Split(',')) { var protocol = token.Trim(); if (!Token(protocol)) throw new InvalidDataException("Invalid subprotocol token."); if (_activeProtocols.Contains(protocol, StringComparer.Ordinal)) { _selectedProtocol = protocol; break; } } } }
        else { if (offered.Length != 0 && !Token(offered)) throw new InvalidDataException("One subprotocol must be selected."); _selectedProtocol = offered; }
        if (_activeProtocols.Length == 0 ? offered.Length != 0 : _selectedProtocol.Length == 0 || !_activeProtocols.Contains(_selectedProtocol, StringComparer.Ordinal)) throw new InvalidDataException("The requested subprotocol was not negotiated.");
    }
    private void ServerHandshake()
    {
        PollStream(); if (TransportClosed()) throw new EndOfStreamException("WebSocket transport ended before upgrade."); if (_stream is StreamPeerTLS tls && tls.GetStatus() != TLSStatus.Connected) return;
        Span<byte> one = stackalloc byte[1]; Span<byte> nonce = stackalloc byte[32];
        while (!_responsePending)
        {
            var read = Read(one); if (read == 0) return; if (read < 0) throw new EndOfStreamException(); if (_handshakeCount == _handshake.Length) throw new InvalidDataException("WebSocket upgrade exceeds 4096 bytes."); _handshake[_handshakeCount++] = one[0];
            if (_handshakeCount < 4 || !_handshake.AsSpan(_handshakeCount - 4, 4).SequenceEqual("\r\n\r\n"u8)) continue;
            var lines = Encoding.Latin1.GetString(_handshake, 0, _handshakeCount - 4).Split("\r\n"); var request = lines[0].Split(' '); if (request.Length != 3 || request[0] != "GET" || request[2] != "HTTP/1.1" || !request[1].StartsWith('/') || request[1].Any(c => c <= 32 || c == 127)) throw new InvalidDataException("Invalid WebSocket HTTP request.");
            var headers = ParseFields(lines.Skip(1)); RequireUpgrade(headers); var host = Field(headers, "Host"); if (host.Length == 0 || host.Any(c => c <= 32 || c is '/' or '\\' or '?' or '#' or '@') || Field(headers, "Sec-WebSocket-Version") != "13" || Field(headers, "Transfer-Encoding").Length != 0 || Field(headers, "Content-Length") is not ("" or "0")) throw new InvalidDataException("Invalid WebSocket upgrade metadata.");
            _key = Field(headers, "Sec-WebSocket-Key"); if (_key.Length != 24 || !Convert.TryFromBase64String(_key, nonce, out var bytes) || bytes != 16) throw new InvalidDataException("WebSocket key must encode 16 random bytes.");
            SelectProtocol(headers, true); _url = (_stream is StreamPeerTLS ? "wss://" : "ws://") + host + request[1]; if (!Uri.TryCreate(_url, UriKind.Absolute, out var uri) || uri.UserInfo.Length != 0 || uri.Host.Length == 0) throw new InvalidDataException("Invalid WebSocket Host.");
            var response = "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + AcceptKey(_key) + "\r\n"; if (_selectedProtocol.Length != 0) response += "Sec-WebSocket-Protocol: " + _selectedProtocol + "\r\n"; foreach (var headerLine in _headers) response += headerLine + "\r\n"; _handshakeResponse = Encoding.UTF8.GetBytes(response + "\r\n"); if (_handshakeResponse.Length > _handshake.Length) throw new InvalidDataException("WebSocket response exceeds 4096 bytes."); _responsePending = true;
        }
        while (_responseOffset < _handshakeResponse.Length) { var sent = _stream!.PutPartialData(_handshakeResponse.AsSpan(_responseOffset)); if (sent == 0) return; _responseOffset += sent; }
        Open();
    }
    private void Open() { _state = WebSocketState.Open; _lastHeartbeat = Environment.TickCount64; _handshakeResponse = []; }
    private void Receive()
    {
        while (_state is WebSocketState.Open or WebSocketState.Closing && !_stopReading)
        {
            if (_output.Count >= _maxPackets + 4) return;
            if (_messageReady) { if (!_input.CanStore(_messageSize) || _input.Count >= _maxPackets) return; _input.Store(_message.AsSpan(0, _messageSize), _messageSize, _messageOpcode == 1, true); _messageSize = _messageOpcode = 0; _messageReady = false; }
            if (!_frameReady)
            {
                while (_frameCount < _frameNeeded)
                {
                    var read = Read(_frame.AsSpan(_frameCount, _frameNeeded - _frameCount)); if (read == 0) return; if (read < 0) { Abort(); return; }
                    _frameCount += read;
                    if (_frameCount == 2 && _frameNeeded == 2) { var length = _frame[1] & 127; _frameNeeded = 2 + (length == 126 ? 2 : length == 127 ? 8 : 0) + ((_frame[1] & 128) != 0 ? 4 : 0); }
                }
                if (!BeginFrame()) return;
            }
            var control = _opcode >= 8;
            if (_payloadRead < _payloadSize)
            {
                var target = control ? _control.AsSpan(_payloadRead, _payloadSize - _payloadRead) : _message.AsSpan(_messageSize + _payloadRead, _payloadSize - _payloadRead);
                var count = Read(target); if (count == 0) return; if (count < 0) { Abort(); return; }
                if (_masked) for (var i = 0; i < count; i++) target[i] ^= _frame[_frameNeeded - 4 + ((_payloadRead + i) & 3)]; _payloadRead += count; if (_payloadRead != _payloadSize) continue;
            }
            if (control) { if (!ControlFrame()) return; Flush(); }
            else { _messageSize += _payloadSize; if (_final) { if (_messageOpcode == 1 && !ValidUTF8(_message.AsSpan(0, _messageSize))) { ProtocolError(1007, "Invalid UTF-8"); return; } _messageReady = true; } }
            _frameReady = false; _frameCount = 0; _frameNeeded = 2;
        }
    }
    private bool BeginFrame()
    {
        _final = (_frame[0] & 128) != 0; _masked = (_frame[1] & 128) != 0; _opcode = _frame[0] & 15; var length = _frame[1] & 127;
        if ((_frame[0] & 112) != 0 || _masked != _server || _opcode is not (0 or 1 or 2 or 8 or 9 or 10) || _opcode >= 8 && (!_final || length > 125)) { ProtocolError(1002, "Invalid frame flags"); return false; }
        var size = length == 126 ? BinaryPrimitives.ReadUInt16BigEndian(_frame.AsSpan(2)) : length == 127 ? BinaryPrimitives.ReadUInt64BigEndian(_frame.AsSpan(2)) : (ulong)length;
        if (length == 126 && size < 126 || length == 127 && (size <= ushort.MaxValue || (size & (1UL << 63)) != 0)) { ProtocolError(1002, "Noncanonical frame length"); return false; }
        if (_opcode < 8)
        {
            if (_opcode == 0 ? _messageOpcode == 0 : _messageOpcode != 0) { ProtocolError(1002, "Invalid continuation"); return false; }
            if (size > (ulong)(_message.Length - _messageSize)) { ProtocolError(1009, "Message too big"); return false; }
            if (_opcode != 0) _messageOpcode = _opcode;
        }
        _payloadSize = (int)size; _payloadRead = 0; _frameReady = true; return true;
    }
    private bool ControlFrame()
    {
        var payload = _control.AsSpan(0, _payloadSize);
        if (_opcode == 9) { if (!_closeSent) QueueFrame(10, payload); return true; }
        if (_opcode == 10) { _heartbeatWaiting = false; return true; }
        if (_payloadSize == 1 || _payloadSize > 1 && !ValidCloseCode(BinaryPrimitives.ReadUInt16BigEndian(payload))) { ProtocolError(1002, "Invalid close status"); return false; }
        if (_payloadSize > 2 && !ValidUTF8(payload[2..])) { ProtocolError(1007, "Invalid close UTF-8"); return false; }
        _closeCode = _payloadSize == 0 ? 1005 : BinaryPrimitives.ReadUInt16BigEndian(payload); _closeReason = _payloadSize <= 2 ? "" : TextEncoding.GetString(payload[2..]);
        _closeReceived = true; if (!_closeSent) { QueueFrame(8, payload); _closeSent = true; }
        _state = WebSocketState.Closing; _stopReading = true; return false;
    }
    private void ProtocolError(int code, string reason)
    {
        _closeCode = code; _closeReason = reason; if (!_closeSent) { BinaryPrimitives.WriteUInt16BigEndian(_control, (ushort)code); QueueFrame(8, _control.AsSpan(0, 2)); _closeSent = true; }
        _state = WebSocketState.Closing; _stopReading = true;
    }
    private void Abort() { if (!_closeReceived && !_stopReading) { _closeCode = -1; _closeReason = ""; } FinishClose(); }
    private void FinishClose() { _http?.Dispose(); _http = null; _stream = null; _tcp = null; _state = WebSocketState.Closed; _input.Clear(); _output.Clear(); _heartbeatWaiting = false; }
    private void Reset()
    {
        FinishClose(); _closeCode = -1; _closeReason = _url = _selectedProtocol = _key = ""; _handshakeCount = _responseOffset = _frameCount = _payloadSize = _payloadRead = _messageSize = _messageOpcode = 0; _frameNeeded = 2;
        _heartbeatWaiting = _requestSent = _responsePending = _wasText = _closeSent = _closeReceived = _stopReading = _frameReady = _messageReady = false; _handshakeResponse = [];
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<WebSocketPeer, int>(nameof(InboundBufferSize), p => p.InboundBufferSize, (p, v) => p.InboundBufferSize = v, _ => 65535, stored: true);
        yield return new PropertyDescriptor<WebSocketPeer, int>(nameof(OutboundBufferSize), p => p.OutboundBufferSize, (p, v) => p.OutboundBufferSize = v, _ => 65535, stored: true);
        yield return new PropertyDescriptor<WebSocketPeer, int>(nameof(MaxQueuedPackets), p => p.MaxQueuedPackets, (p, v) => p.MaxQueuedPackets = v, _ => 4096, stored: true);
        yield return new PropertyDescriptor<WebSocketPeer, double>(nameof(HeartbeatInterval), p => p.HeartbeatInterval, (p, v) => p.HeartbeatInterval = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<WebSocketPeer, string[]>(nameof(SupportedProtocols), p => p.SupportedProtocols, (p, v) => p.SupportedProtocols = v, _ => [], stored: true);
        yield return new PropertyDescriptor<WebSocketPeer, string[]>(nameof(HandshakeHeaders), p => p.HandshakeHeaders, (p, v) => p.HandshakeHeaders = v, _ => [], stored: true);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { FinishClose(); _message = _send = []; } base.Dispose(disposing); }
}

internal sealed class WebSocketPacketQueue
{
    private byte[] _bytes = [];
    private (int Size, int Payload, bool Text, bool Application)[] _records = [];
    private int _read, _write, _used, _first, _next, _remaining;
    internal int Count { get; private set; }
    internal int PayloadBytes { get; private set; }
    internal int ApplicationCount { get; private set; }
    internal int NextSize => Count == 0 ? -1 : _records[_first].Size;
    internal bool NextText => _records[_first].Text;
    internal void Prepare(int bytes, int records) { _bytes = new byte[Math.Max(1, bytes)]; _records = new (int, int, bool, bool)[Math.Max(1, records)]; Clear(); }
    internal void Clear() { _read = _write = _used = _first = _next = _remaining = Count = PayloadBytes = ApplicationCount = 0; }
    internal bool CanStore(int bytes) => bytes <= _bytes.Length - _used && Count < _records.Length;
    internal void Store(ReadOnlySpan<byte> bytes, int payload, bool text, bool application)
    {
        var first = Math.Min(bytes.Length, _bytes.Length - _write); bytes[..first].CopyTo(_bytes.AsSpan(_write)); bytes[first..].CopyTo(_bytes); _write = (_write + bytes.Length) % _bytes.Length; _used += bytes.Length;
        _records[_next] = (bytes.Length, payload, text, application); _next = (_next + 1) % _records.Length; if (Count++ == 0) _remaining = bytes.Length; PayloadBytes += payload; if (application) ApplicationCount++;
    }
    internal ReadOnlySpan<byte> Peek() => _bytes.AsSpan(_read, Math.Min(_remaining, _bytes.Length - _read));
    internal void Consume(int bytes)
    {
        _read = (_read + bytes) % _bytes.Length; _used -= bytes; _remaining -= bytes;
        if (_remaining != 0) return; var record = _records[_first]; PayloadBytes -= record.Payload; if (record.Application) ApplicationCount--; _first = (_first + 1) % _records.Length; Count--; _remaining = Count == 0 ? 0 : _records[_first].Size;
    }
    internal void Take(Span<byte> destination)
    {
        var size = NextSize; var first = Math.Min(size, _bytes.Length - _read); _bytes.AsSpan(_read, first).CopyTo(destination); _bytes.AsSpan(0, size - first).CopyTo(destination[first..]); Consume(size);
    }
}
