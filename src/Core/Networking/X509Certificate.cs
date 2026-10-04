using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
namespace Electron2D;

/// <summary>Stores a copied ordered X.509 certificate chain.</summary>
/// <remarks>Loads append certificates, preserving chain order. Malformed input leaves state unchanged.
/// Active TLS sessions prevent loading/disposal. Duplication copies certificate bytes and never shares native handles.</remarks>
public class X509Certificate : Resource
{
    private readonly object _gate = new();
    private byte[][] _certificates = [];
    private int _uses;
    /// <summary>Creates an empty certificate chain.</summary>
    public X509Certificate() { }
    /// <summary>Loads PEM certificates or one DER certificate from an engine filesystem path.</summary><param name="path">OS, res:// or user:// path.</param>
    public void Load(string path)
    {
        ThrowIfDisposed(); using var file = FileAccess.Open(path, FileAccessModeFlags.Read); var bytes = file.ReadBytes(checked((int)file.Length));
        if (bytes.AsSpan().StartsWith("-----"u8)) LoadFromString(Encoding.UTF8.GetString(bytes));
        else { using var certificate = X509CertificateLoader.LoadCertificate(bytes); Append([certificate.RawData]); }
    }
    /// <summary>Appends every certificate from PEM text.</summary><param name="certificates">One or more certificate blocks in chain order.</param>
    /// <exception cref="CryptographicException">No valid certificate chain is encoded.</exception>
    /// <exception cref="InvalidOperationException">TLS is using this chain.</exception>
    public void LoadFromString(string certificates)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(certificates); var collection = new X509Certificate2Collection();
        try { collection.ImportFromPem(certificates); if (collection.Count == 0) throw new CryptographicException("No certificates were found."); Append(collection.Cast<X509Certificate2>().Select(c => c.RawData).ToArray()); }
        finally { foreach (var certificate in collection) certificate.Dispose(); }
    }
    private void Append(byte[][] certificates) { lock (_gate) { ThrowIfDisposed(); if (_uses != 0) throw new InvalidOperationException("The certificate is in use by TLS."); _certificates = [.. _certificates, .. certificates]; } EmitChanged(); }
    /// <summary>Saves the whole ordered chain as PEM text.</summary><param name="path">Destination engine filesystem path.</param>
    public void Save(string path) { var text = SaveToString(); using var file = FileAccess.Open(path, FileAccessModeFlags.Write); file.WriteString(text); }
    /// <summary>Exports every chain certificate as PEM text.</summary><returns>Empty for an empty resource.</returns>
    public string SaveToString() { lock (_gate) { ThrowIfDisposed(); return string.Join('\n', _certificates.Select(c => PemEncoding.WriteString("CERTIFICATE", c))); } }
    internal byte[][] RetainCertificates() { lock (_gate) { ThrowIfDisposed(); if (_certificates.Length == 0) throw new CryptographicException("TLS requires a nonempty certificate chain."); _uses++; return _certificates; } }
    internal void ReleaseTLSUse() { lock (_gate) _uses--; }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); lock (_gate) if (_uses != 0) throw new InvalidOperationException("The certificate is in use by TLS."); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) _certificates = []; base.Dispose(disposing); }
    /// <inheritdoc />
    protected override void OnResetState() { lock (_gate) if (_uses != 0) throw new InvalidOperationException("The certificate is in use by TLS."); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new X509Certificate();
    internal void ReloadFrom(X509Certificate source) { byte[][] certificates; lock (source._gate) { source.ThrowIfDisposed(); certificates = source._certificates.Select(c => (byte[])c.Clone()).ToArray(); } lock (_gate) { ThrowIfDisposed(); if (_uses != 0) throw new InvalidOperationException("The certificate is in use by TLS."); _certificates = certificates; } EmitChanged(); }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    => ((X509Certificate)target).ReloadFrom(this);
}
