using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
namespace Electron2D;

/// <summary>Transfers authenticated DTLS datagrams over a borrowed connected PacketPeerUDP.</summary>
/// <remarks>Calls/disposal require the constructing thread. Linux OpenSSL 3.2 or later supplies DTLS 1.2 and
/// fixed datagram BIO storage. Poll preserves packet boundaries, progresses cookies/handshake/timers and
/// remote closure. Preparation allocates; warmed span packet/poll cycles reuse bounded storage. The borrowed
/// UDP peer must remain connected to its original endpoint and exclusively used for this session.</remarks>
public class PacketPeerDTLS : PacketPeer
{
    private PacketPeerUDP? _udp;
    private long _generation;
    private TLSHandle? _session, _network;
    private TLSStatus _status;
    private X509Certificate? _certificate, _trust;
    private CryptoKey? _key;
    private nint _cookie;
    private readonly byte[] _input = new byte[65536], _output = new byte[65536], _plain = new byte[16384];
    private readonly DatagramQueue _packets = new();
    private int _inputCount;
    private bool _polling;
    /// <summary>Creates a disconnected DTLS peer with prepared receive/record storage.</summary>
    public PacketPeerDTLS() { _packets.Prepare(65536); }
    /// <summary>Releases abandoned native state and resource retention without accessing the borrowed UDP peer.</summary>
    ~PacketPeerDTLS() { Cleanup(); }
    /// <summary>Starts a polled validating or explicitly unsafe client handshake.</summary><param name="packetPeer">Live connected UDP transport borrowed until disconnect/failure.</param><param name="hostname">Expected DNS/IP identity, subject to an options override.</param><param name="clientOptions">Client configuration; null selects system trust.</param><exception cref="AuthenticationException">Setup, trust, expected-name or handshake validation fails.</exception><exception cref="PlatformNotSupportedException">The selected native datagram backend is unavailable.</exception>
    public void ConnectToPeer(PacketPeerUDP packetPeer, string hostname, TLSOptions? clientOptions = null)
    {
        CheckPacketPeer(); ArgumentNullException.ThrowIfNull(hostname); ValidatePeer(packetPeer);
        if (clientOptions is null) { using var defaults = TLSOptions.Client(); BeginClient(packetPeer, hostname, defaults); } else BeginClient(packetPeer, hostname, clientOptions);
    }
    private void ValidatePeer(PacketPeerUDP peer) { if (_polling) throw new InvalidOperationException("DTLS configuration cannot reenter."); ArgumentNullException.ThrowIfNull(peer); if (_session is not null) throw new InvalidOperationException("DTLS is already attached."); _ = peer.ConnectedAddress; }
    private void BeginClient(PacketPeerUDP peer, string name, TLSOptions options)
    {
        if (options.IsServer()) throw new ArgumentException("Client options are required.", nameof(options)); var expected = options.GetCommonNameOverride(); if (expected.Length == 0) expected = name; if (expected.Contains('\0')) throw new ArgumentException("DTLS names cannot contain NUL.", nameof(name));
        try
        {
            using var context = TLSNative.CreateContext(options, true); var trust = options.GetTrustedCAChain(); if (trust is not null) { var certificates = trust.RetainCertificates(); _trust = trust; TLSNative.AddTrust(context.Pointer, certificates); }
            Attach(peer, TLSNative.CreateSession(context.Pointer, false, expected, !options.IsUnsafeClient(), out var network, true), network); Poll();
        }
        catch (EntryPointNotFoundException error) { Cleanup(); _status = TLSStatus.Error; throw new PlatformNotSupportedException("DTLS requires OpenSSL 3.2 datagram BIO support.", error); }
        catch { Fail(); throw; }
    }
    internal unsafe void Accept(PacketPeerUDP peer, nint context, CryptoKey key, X509Certificate certificate, ReadOnlySpan<byte> cookie)
    {
        ValidatePeer(peer);
        try
        {
            _ = certificate.RetainCertificates(); _certificate = certificate; var bytes = key.RetainPrivateKey(); _key = key; CryptographicOperations.ZeroMemory(bytes);
            Attach(peer, TLSNative.CreateSession(context, true, "", false, out var network, true), network); _cookie = (nint)NativeMemory.Alloc(32); cookie.CopyTo(new Span<byte>((void*)_cookie, 32)); TLSNative.Require(TLSNative.SSL_set_ex_data(_session!.Pointer, 0, _cookie), "DTLS cookie binding failed."); TLSNative.SSL_set_options(_session.Pointer, 1UL << 13); Poll();
        }
        catch (EntryPointNotFoundException error) { Cleanup(); _status = TLSStatus.Error; throw new PlatformNotSupportedException("DTLS requires OpenSSL 3.2 datagram BIO support.", error); }
        catch { Fail(); throw; }
    }
    private void Attach(PacketPeerUDP peer, TLSHandle session, TLSHandle network) { _session = session; _network = network; _udp = peer; _generation = peer.ConnectionGeneration; _status = TLSStatus.Handshaking; }
    /// <summary>Gets the cached authenticated session phase without polling.</summary><returns>The shared TLS/DTLS status domain.</returns>
    public TLSStatus GetStatus() { CheckPacketPeer(); return _status; }
    /// <summary>Advances datagram input/output, handshake/cookies, native retransmission timers and remote close.</summary><exception cref="AuthenticationException">Native authentication/record processing fails.</exception><exception cref="InvalidOperationException">Polling reenters or the borrowed UDP connection changes.</exception><remarks>Receive queue pressure drops complete plaintext packets; no partial packet is exposed.</remarks>
    public void Poll()
    {
        CheckPacketPeer(); if (_polling) throw new InvalidOperationException("DTLS polling cannot reenter."); if (_session is null) return; _polling = true;
        try
        {
            if (!_udp!.IsSocketConnected() || _udp.ConnectionGeneration != _generation) throw new InvalidOperationException("The borrowed UDP connection changed.");
            Flush();
            for (var i = 0; i < 32 && _session is not null; i++)
            {
                Feed(); TLSNative.ERR_clear_error(); var result = _status == TLSStatus.Handshaking ? TLSNative.SSL_do_handshake(_session.Pointer) : TLSNative.Read(_session.Pointer, _plain); var error = result > 0 ? 0 : TLSNative.SSL_get_error(_session.Pointer, result);
                if (result > 0) { if (_status == TLSStatus.Handshaking) _status = TLSStatus.Connected; else _packets.Store(default, _plain.AsSpan(0, result)); }
                Handle(error); if (_session is null) break; Flush(); if (result <= 0 && _udp.GetAvailablePacketCount() == 0 && _inputCount == 0) break;
            }
            if (_session is not null && _status == TLSStatus.Handshaking) { TLSNative.ERR_clear_error(); if (TLSNative.SSL_ctrl(_session.Pointer, 74, 0, 0) < 0) throw TLSNative.Failure("DTLS retransmission failed."); Flush(); }
        }
        catch { Fail(); throw; }
        finally { _polling = false; }
    }
    private void Handle(int error)
    {
        if (error is 0 or 2 or 3) return; if (error == 6) { Cleanup(); return; }
        if (_session is not null && TLSNative.SSL_get_verify_result(_session.Pointer) is 62 or 64) _status = TLSStatus.ErrorHostnameMismatch; throw TLSNative.Failure("DTLS handshake or record validation failed.");
    }
    private void Feed()
    {
        if (_inputCount == 0 && _udp!.GetAvailablePacketCount() > 0) _inputCount = _udp.GetPacket(_input);
        if (_inputCount == 0) return; var written = TLSNative.WriteBIO(_network!.Pointer, _input.AsSpan(0, _inputCount)); if (written == _inputCount) _inputCount = 0; else if (written > 0) throw TLSNative.Failure("DTLS BIO truncated an input datagram.");
    }
    private void Flush()
    {
        while (TLSNative.BIO_ctrl(_network!.Pointer, 10, 0, 0) > 0) { var count = TLSNative.ReadBIO(_network.Pointer, _output); if (count <= 0) throw TLSNative.Failure("DTLS output datagram extraction failed."); _udp!.PutPacket(_output.AsSpan(0, count)); }
    }
    /// <inheritdoc />
    public override int GetAvailablePacketCount() { CheckPacketPeer(); return _status == TLSStatus.Connected ? _packets.Count : 0; }
    /// <inheritdoc />
    public override int GetMaxPacketSize() { CheckPacketPeer(); return 488; }
    /// <inheritdoc />
    protected override int NextPacketSize() => _status == TLSStatus.Connected ? _packets.NextSize : -1;
    /// <inheritdoc />
    protected override void ReadPacketCore(Span<byte> destination) => _packets.Take(destination);
    internal void SetTransportMTU(int mtu) { CheckPacketPeer(); if (_session is null) throw new InvalidOperationException("DTLS session required."); TLSNative.Require(TLSNative.SSL_ctrl(_session.Pointer, 17, mtu, 0), "DTLS transport MTU failed."); }
    internal void PutTransportPacket(ReadOnlySpan<byte> data) => SendPacket(data, 16384);
    /// <inheritdoc />
    public override void PutPacket(ReadOnlySpan<byte> data) => SendPacket(data, GetMaxPacketSize());
    private void SendPacket(ReadOnlySpan<byte> data, int limit)
    {
        CheckPacketPeer(); if (_polling) throw new InvalidOperationException("DTLS sending cannot reenter polling."); if (_status != TLSStatus.Connected || _session is null) throw new InvalidOperationException("Authenticated DTLS is required."); if (data.Length > limit) throw new ArgumentException("The DTLS packet exceeds its advertised capacity.", nameof(data)); if (data.IsEmpty) return;
        try { Poll(); if (_session is null) throw new IOException("DTLS closed before sending."); TLSNative.ERR_clear_error(); var result = TLSNative.Write(_session.Pointer, data); var error = result > 0 ? 0 : TLSNative.SSL_get_error(_session.Pointer, result); Handle(error); if (result != data.Length) throw new IOException("DTLS could not accept the complete packet."); Flush(); } catch { Fail(); throw; }
    }
    /// <summary>Sends a best-effort close notification and releases DTLS state without closing UDP.</summary><remarks>Available packets clear; retained security resources are released. Idle calls are valid.</remarks>
    public void DisconnectFromPeer()
    {
        CheckPacketPeer(); if (_polling) throw new InvalidOperationException("DTLS disconnect cannot reenter polling."); try { if (_status == TLSStatus.Connected && _session is not null && _udp?.IsDisposed == false && _udp.IsSocketConnected() && _udp.ConnectionGeneration == _generation) { TLSNative.ERR_clear_error(); TLSNative.SSL_shutdown(_session.Pointer); Flush(); } } catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException) { } finally { Cleanup(); }
    }
    private void Fail() { var status = _status == TLSStatus.ErrorHostnameMismatch ? _status : TLSStatus.Error; Cleanup(); _status = status; }
    private unsafe void Cleanup()
    {
        _session?.Dispose(); _session = null; _network?.Dispose(); _network = null; _udp = null; if (_cookie != 0) { CryptographicOperations.ZeroMemory(new Span<byte>((void*)_cookie, 32)); NativeMemory.Free((void*)_cookie); _cookie = 0; }
        _key?.ReleaseTLSUse(); _key = null; _certificate?.ReleaseTLSUse(); _certificate = null; _trust?.ReleaseTLSUse(); _trust = null; _inputCount = 0; _packets.Clear(); _status = TLSStatus.Disconnected;
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); if (_polling) throw new InvalidOperationException("DTLS disposal cannot reenter polling."); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) DisconnectFromPeer(); base.Dispose(disposing); }
}
