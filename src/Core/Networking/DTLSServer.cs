using System.Buffers.Binary;
using System.Security.Cryptography;
namespace Electron2D;

/// <summary>Prepares a DTLS server identity and address-bound cookie secret for borrowed UDP peers.</summary>
/// <remarks>Calls/disposal require the constructing thread. Setup retains key/certificate payloads; accepted
/// peers independently retain them and survive server disposal. The helper owns no UDP socket. Native setup
/// and cookie/session preparation allocate; repeated authenticated packet polling uses prepared buffers.</remarks>
public class DTLSServer : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private TLSHandle? _context;
    private CryptoKey? _key;
    private X509Certificate? _certificate;
    private readonly byte[] _secret = new byte[32];
    /// <summary>Creates an unconfigured server helper.</summary>
    public DTLSServer() { }
    /// <summary>Releases abandoned native identity and resource retention without touching any UDP peer.</summary>
    ~DTLSServer() { Cleanup(); }
    private void Check() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("DTLS server access requires its constructing thread."); }
    /// <summary>Configures one server identity and a fresh cookie secret.</summary><param name="serverOptions">Live server options containing a matching private key and certificate chain.</param><exception cref="InvalidOperationException">The server is already configured.</exception><exception cref="ArgumentException">Options have the wrong role.</exception><remarks>Failure releases partial state, allowing a later valid setup. Options are consumed during setup; resources stay retained until helper and accepted sessions release them.</remarks>
    public void Setup(TLSOptions serverOptions)
    {
        Check(); ArgumentNullException.ThrowIfNull(serverOptions); if (_context is not null) throw new InvalidOperationException("The DTLS server is already configured."); if (!serverOptions.IsServer()) throw new ArgumentException("Server options are required.", nameof(serverOptions));
        try
        {
            _context = TLSNative.CreateContext(serverOptions, true); var certificate = serverOptions.GetOwnCertificate()!; var chain = certificate.RetainCertificates(); _certificate = certificate;
            var key = serverOptions.GetPrivateKey()!; var bytes = key.RetainPrivateKey(); _key = key; try { TLSNative.SetIdentity(_context.Pointer, chain, bytes); } finally { CryptographicOperations.ZeroMemory(bytes); }
            TLSNative.ConfigureDTLSCookies(_context.Pointer); System.Security.Cryptography.RandomNumberGenerator.Fill(_secret);
        }
        catch { Cleanup(); throw; }
    }
    /// <summary>Creates a caller-owned DTLS server peer and starts its polled cookie/handshake exchange.</summary><param name="udpPeer">Borrowed live connected UDP endpoint, including an accepted UDPServer peer.</param><returns>A handshaking or connected DTLS peer; cookie challenges continue on the same logical peer.</returns><exception cref="InvalidOperationException">Setup has not succeeded or UDP is unconnected.</exception><remarks>The caller owns both logical peers. A transport generation change rejects later DTLS use; authentication failures release session state and preserve the UDP owner.</remarks>
    public PacketPeerDTLS TakeConnection(PacketPeerUDP udpPeer)
    {
        Check(); ArgumentNullException.ThrowIfNull(udpPeer); if (_context is null) throw new InvalidOperationException("DTLS server setup is required."); var remote = udpPeer.ConnectedAddress; Span<byte> identity = stackalloc byte[28]; BinaryPrimitives.WriteUInt64BigEndian(identity, remote.High); BinaryPrimitives.WriteUInt64BigEndian(identity[8..], remote.Low); BinaryPrimitives.WriteInt64BigEndian(identity[16..], remote.Scope); BinaryPrimitives.WriteInt32BigEndian(identity[24..], remote.Port); Span<byte> cookie = stackalloc byte[32]; HMACSHA256.HashData(_secret, identity, cookie);
        var result = new PacketPeerDTLS(); try { result.Accept(udpPeer, _context.Pointer, _key!, _certificate!, cookie); return result; } catch { result.Dispose(); throw; } finally { CryptographicOperations.ZeroMemory(cookie); GC.KeepAlive(this); }
    }
    private void Cleanup() { _context?.Dispose(); _context = null; _key?.ReleaseTLSUse(); _key = null; _certificate?.ReleaseTLSUse(); _certificate = null; CryptographicOperations.ZeroMemory(_secret); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); Check(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) Cleanup(); base.Dispose(disposing); }
}
