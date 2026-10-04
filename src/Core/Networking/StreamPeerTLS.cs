using System.Security.Authentication;
using System.Security.Cryptography;
namespace Electron2D;

/// <summary>Describes a TLS or DTLS session independently of the underlying transport.</summary>
public enum TLSStatus
{
    /// <summary>No TLS or DTLS session is attached.</summary>
    Disconnected = 0,
    /// <summary>The polled TLS or DTLS handshake is in progress.</summary>
    Handshaking = 1,
    /// <summary>Authenticated TLS bytes or DTLS packets can be transferred.</summary>
    Connected = 2,
    /// <summary>The session failed; the borrowed transport remains caller-owned.</summary>
    Error = 3,
    /// <summary>Certificate validation failed because the expected DNS name or IP did not match.</summary>
    ErrorHostnameMismatch = 4
}

/// <summary>Encrypts ordered bytes over a borrowed StreamPeer through a polled TLS session.</summary>
/// <remarks>Calls require the constructing thread. The current native backend is Linux OpenSSL 3 with TLS 1.2/1.3,
/// system/custom trust and expected-name validation. Poll processes handshake/control records and buffered output.
/// Full I/O may wait; partial I/O retains bounded record buffers. Disconnect/Dispose never disposes the borrowed stream.</remarks>
public class StreamPeerTLS : StreamPeer
{
    private StreamPeer? _stream;
    private TLSHandle? _session, _network;
    private TLSStatus _status;
    private X509Certificate? _certificate, _trust;
    private CryptoKey? _key;
    private readonly byte[] _input = new byte[16384], _output = new byte[65536];
    private int _inputCount, _inputOffset, _outputCount, _outputOffset;
    /// <summary>Creates a disconnected TLS peer with prepared encrypted record buffers.</summary>
    public StreamPeerTLS() { }
    /// <summary>Releases abandoned native TLS state and resource-use retention without touching the borrowed transport.</summary>
    ~StreamPeerTLS() { Cleanup(); }
    /// <summary>Starts a validating or explicitly unsafe client handshake over a borrowed stream.</summary><param name="stream">A live ordered transport; self-attachment is invalid.</param><param name="commonName">Expected DNS name or IP; options can override it.</param><param name="options">Client options, or null for system trust and name validation.</param>
    /// <exception cref="AuthenticationException">TLS initialization or handshake validation fails.</exception>
    /// <exception cref="PlatformNotSupportedException">The current TLS backend is unavailable.</exception>
    public void ConnectToStream(StreamPeer stream, string commonName, TLSOptions? options = null)
    {
        CheckStream(); ArgumentNullException.ThrowIfNull(commonName);
        if (options is null) { using var defaults = TLSOptions.Client(); Begin(stream, commonName, defaults, false); }
        else Begin(stream, commonName, options, false);
    }
    /// <summary>Starts a server handshake over a borrowed stream.</summary><param name="stream">A live ordered transport.</param><param name="options">Server certificate-chain and private-key options.</param>
    /// <exception cref="ArgumentException">Options do not describe a server.</exception>
    public void AcceptStream(StreamPeer stream, TLSOptions options) { CheckStream(); ArgumentNullException.ThrowIfNull(options); Begin(stream, "", options, true); }
    private void Begin(StreamPeer stream, string name, TLSOptions options, bool server)
    {
        ArgumentNullException.ThrowIfNull(stream); if (stream.IsDisposed) throw new ObjectDisposedException(nameof(stream));
        if (ReferenceEquals(stream, this)) throw new ArgumentException("TLS cannot borrow itself.", nameof(stream));
        if (_session is not null) throw new InvalidOperationException("TLS is already attached.");
        if (options.IsServer() != server) throw new ArgumentException("TLS configuration has the wrong role.", nameof(options));
        var expected = options.GetCommonNameOverride(); if (expected.Length == 0) expected = name;
        if (expected.Contains('\0')) throw new ArgumentException("TLS names cannot contain NUL.", nameof(name));
        try
        {
            using var context = TLSNative.CreateContext(options);
            var trust = options.GetTrustedCAChain();
            if (trust is not null) { var certificates = trust.RetainCertificates(); _trust = trust; TLSNative.AddTrust(context.Pointer, certificates); }
            if (server)
            {
                var certificate = options.GetOwnCertificate()!; var certificates = certificate.RetainCertificates(); _certificate = certificate;
                var key = options.GetPrivateKey()!; var encoded = key.RetainPrivateKey(); _key = key;
                try { TLSNative.SetIdentity(context.Pointer, certificates, encoded); } finally { CryptographicOperations.ZeroMemory(encoded); }
            }
            _session = TLSNative.CreateSession(context.Pointer, server, expected, !server && !options.IsUnsafeClient(), out var network); _network = network;
            _stream = stream; _status = TLSStatus.Handshaking; Poll();
        }
        catch { var status = _status == TLSStatus.ErrorHostnameMismatch ? _status : TLSStatus.Error; Cleanup(); _status = status; throw; }
    }
    /// <summary>Gets the cached TLS phase without polling.</summary><returns>The TLS phase.</returns>
    public TLSStatus GetStatus() { CheckStream(); return _status; }
    /// <summary>Gets the currently borrowed transport.</summary><returns>Null after disconnection or failure.</returns>
    public StreamPeer? GetStream() { CheckStream(); return _stream; }
    /// <summary>Advances handshake, encrypted input/output and remote closure without waiting.</summary>
    /// <exception cref="AuthenticationException">TLS authentication or record processing fails.</exception>
    public void Poll()
    {
        CheckStream(); if (_session is null) return;
        try
        {
            FlushOutput(false); FeedInput(); TLSNative.ERR_clear_error(); var pointer = _session.Pointer;
            var result = _status == TLSStatus.Handshaking ? TLSNative.SSL_do_handshake(pointer) : TLSNative.Peek(pointer);
            var error = result > 0 ? 0 : TLSNative.SSL_get_error(pointer, result);
            if (result > 0 && _status == TLSStatus.Handshaking) _status = TLSStatus.Connected;
            HandleResult(error); if (_session is not null) FlushOutput(false);
        }
        catch { if (_status != TLSStatus.ErrorHostnameMismatch) _status = TLSStatus.Error; var status = _status; Cleanup(); _status = status; throw; }
    }
    private void HandleResult(int error)
    {
        if (error is 0 or 2 or 3) return;
        if (error == 6) { Cleanup(); return; }
        if (_session is not null && TLSNative.SSL_get_verify_result(_session.Pointer) is 62 or 64) _status = TLSStatus.ErrorHostnameMismatch;
        throw TLSNative.Failure("TLS handshake or record validation failed.");
    }
    private void FeedInput()
    {
        var network = _network!.Pointer;
        if (_inputOffset < _inputCount)
        {
            var written = TLSNative.WriteBIO(network, _input.AsSpan(_inputOffset, _inputCount - _inputOffset)); if (written > 0) _inputOffset += written;
            if (_inputOffset != _inputCount) return;
        }
        _inputOffset = _inputCount = 0;
        if (_stream is StreamPeerSocket closed && closed.GetStatus() == StreamSocketStatus.None) { TLSNative.BIO_ctrl(network, 142, 0, 0); return; }
        var capacity = (int)TLSNative.BIO_ctrl(network, 140, 0, 0); if (capacity <= 0) return;
        _inputCount = _stream!.GetPartialData(_input.AsSpan(0, Math.Min(capacity, _input.Length)));
        if (_inputCount != 0) { var written = TLSNative.WriteBIO(network, _input.AsSpan(0, _inputCount)); if (written > 0) _inputOffset = written; }
        else if (_stream is StreamPeerSocket socket && socket.GetStatus() == StreamSocketStatus.None) TLSNative.BIO_ctrl(network, 142, 0, 0);
    }
    private void FlushOutput(bool block)
    {
        while (true)
        {
            while (_outputOffset < _outputCount)
            {
                var sent = _stream!.PutPartialData(_output.AsSpan(_outputOffset, _outputCount - _outputOffset)); _outputOffset += sent;
                if (sent == 0) { if (!block) return; Thread.Yield(); }
            }
            _outputOffset = _outputCount = 0;
            if (TLSNative.BIO_ctrl(_network!.Pointer, 10, 0, 0) <= 0) return;
            _outputCount = TLSNative.ReadBIO(_network.Pointer, _output); if (_outputCount <= 0) throw TLSNative.Failure("TLS output extraction failed.");
        }
    }
    /// <inheritdoc />
    public override int GetAvailableBytes() { CheckStream(); return _status == TLSStatus.Connected ? TLSNative.SSL_pending(_session!.Pointer) : 0; }
    private void RequireConnected() { if (_status != TLSStatus.Connected || _session is null) throw new InvalidOperationException("An authenticated TLS stream is required."); }
    /// <inheritdoc />
    protected override int ReadCore(Span<byte> destination, bool block)
    {
        RequireConnected(); if (destination.IsEmpty) return 0;
        try
        {
            while (true)
            {
                Poll(); if (_session is null) { if (block) throw new EndOfStreamException(); return 0; }
                TLSNative.ERR_clear_error(); var result = TLSNative.Read(_session.Pointer, destination); var error = result > 0 ? 0 : TLSNative.SSL_get_error(_session.Pointer, result);
                HandleResult(error); if (result > 0) { FlushOutput(false); return result; }
                if (!block) return 0; if (_session is null) throw new EndOfStreamException(); Thread.Yield();
            }
        }
        catch { var status = _status == TLSStatus.Disconnected ? _status : TLSStatus.Error; Cleanup(); _status = status; throw; }
    }
    /// <inheritdoc />
    protected override int WriteCore(ReadOnlySpan<byte> data, bool block)
    {
        RequireConnected();
        try
        {
            while (true)
            {
                FlushOutput(block); if (_outputCount != 0 || TLSNative.BIO_ctrl(_network!.Pointer, 10, 0, 0) > 0) return 0;
                TLSNative.ERR_clear_error(); var result = TLSNative.Write(_session!.Pointer, data[..Math.Min(16384, data.Length)]); var error = result > 0 ? 0 : TLSNative.SSL_get_error(_session.Pointer, result);
                HandleResult(error); if (_session is null) throw new EndOfStreamException(); FlushOutput(block);
                if (result > 0 || !block) return Math.Max(0, result); FeedInput(); Thread.Yield();
            }
        }
        catch { Cleanup(); _status = TLSStatus.Error; throw; }
    }
    /// <summary>Sends a best-effort close notification and releases TLS state while preserving the borrowed stream.</summary>
    public void DisconnectFromStream()
    {
        CheckStream(); try { if (_status == TLSStatus.Connected && _session is not null && _stream?.IsDisposed == false && (_stream is not StreamPeerSocket socket || socket.GetStatus() == StreamSocketStatus.Connected)) { TLSNative.ERR_clear_error(); TLSNative.SSL_shutdown(_session.Pointer); FlushOutput(false); } } catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException) { } finally { Cleanup(); }
    }
    private void Cleanup()
    {
        _session?.Dispose(); _session = null; _network?.Dispose(); _network = null; _stream = null;
        _key?.ReleaseTLSUse(); _key = null; _certificate?.ReleaseTLSUse(); _certificate = null; _trust?.ReleaseTLSUse(); _trust = null;
        _inputCount = _inputOffset = _outputCount = _outputOffset = 0; _status = TLSStatus.Disconnected;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) DisconnectFromStream(); base.Dispose(disposing); }
}
