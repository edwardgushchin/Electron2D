namespace Electron2D;

/// <summary>Describes an immutable TLS client or server role with borrowed key/certificate resources.</summary>
/// <remarks>Factories preserve resource identity. Sessions retain resources during use; disposing options never disposes them.
/// Client uses certificate-chain and hostname validation; ClientUnsafe skips name validation; a supplied trust chain still requires valid certificate-chain verification.</remarks>
public sealed class TLSOptions : ElectronObject
{
    private readonly bool _server, _unsafe;
    private readonly string _name;
    private readonly X509Certificate? _trusted, _own;
    private readonly CryptoKey? _key;
    private TLSOptions(bool server, bool unsafeClient, string name, X509Certificate? trusted, CryptoKey? key, X509Certificate? own) { _server = server; _unsafe = unsafeClient; _name = name; _trusted = trusted; _key = key; _own = own; }
    /// <summary>Creates a validating client configuration.</summary><param name="trustedChain">Custom trust anchors, or null for system trust.</param><param name="commonNameOverride">Expected certificate name override; empty uses ConnectToStream's commonName.</param><returns>A caller-owned configuration.</returns>
    public static TLSOptions Client(X509Certificate? trustedChain = null, string commonNameOverride = "") { ArgumentNullException.ThrowIfNull(commonNameOverride); Validate(trustedChain); return new(false, false, commonNameOverride, trustedChain, null, null); }
    /// <summary>Creates an explicit client configuration without expected-name rejection.</summary><param name="trustedChain">Optional required trust chain; null disables certificate verification.</param><returns>A caller-owned unsafe test configuration.</returns>
    public static TLSOptions ClientUnsafe(X509Certificate? trustedChain = null) { Validate(trustedChain); return new(false, true, "", trustedChain, null, null); }
    /// <summary>Creates a server configuration.</summary><param name="key">A borrowed private key.</param><param name="certificate">A borrowed leaf certificate followed by intermediate certificates.</param><returns>A caller-owned configuration.</returns>
    public static TLSOptions Server(CryptoKey key, X509Certificate certificate) { ArgumentNullException.ThrowIfNull(key); ArgumentNullException.ThrowIfNull(certificate); Validate(key); Validate(certificate); return new(true, false, "", null, key, certificate); }
    private static void Validate(ElectronObject? resource) { if (resource?.IsDisposed == true) throw new ObjectDisposedException(nameof(resource)); }
    /// <summary>Reports whether this configuration accepts incoming TLS sessions.</summary><returns>True for Server.</returns>
    public bool IsServer() { ThrowIfDisposed(); return _server; }
    /// <summary>Reports whether expected-name rejection is disabled.</summary><returns>True for ClientUnsafe.</returns>
    public bool IsUnsafeClient() { ThrowIfDisposed(); return _unsafe; }
    /// <summary>Gets the expected-name override.</summary><returns>Empty by default.</returns>
    public string GetCommonNameOverride() { ThrowIfDisposed(); return _name; }
    /// <summary>Gets the borrowed custom trust chain.</summary><returns>Null when system trust is selected.</returns>
    public X509Certificate? GetTrustedCAChain() { ThrowIfDisposed(); return _trusted; }
    /// <summary>Gets the borrowed server private key.</summary><returns>Null for clients.</returns>
    public CryptoKey? GetPrivateKey() { ThrowIfDisposed(); return _key; }
    /// <summary>Gets the borrowed server certificate chain.</summary><returns>Null for clients.</returns>
    public X509Certificate? GetOwnCertificate() { ThrowIfDisposed(); return _own; }
}
