using System.Security.Cryptography;
using System.Text;
namespace Electron2D;

/// <summary>Stores a copied RSA or elliptic-curve public or private key.</summary>
/// <remarks>PEM/DER loading is transactional. Active TLS sessions prevent replacement and disposal.
/// Private encoded buffers are cleared on replacement/disposal. Exported strings belong to the caller and cannot be cleared.</remarks>
public class CryptoKey : Resource
{
    private readonly object _gate = new();
    private byte[] _encoded = [];
    private bool _publicOnly, _elliptic;
    private int _uses;
    /// <summary>Creates an empty key resource.</summary>
    public CryptoKey() { }
    /// <summary>Clears abandoned private encoded key storage.</summary>
    ~CryptoKey() { CryptographicOperations.ZeroMemory(_encoded); }
    /// <summary>Reports whether the loaded key contains only public material.</summary><returns>False initially.</returns>
    public bool IsPublicOnly() { ThrowIfDisposed(); lock (_gate) return _publicOnly; }
    /// <summary>Loads a PEM or DER key from an engine filesystem path.</summary><param name="path">OS, res:// or user:// path.</param><param name="publicOnly">Whether only a public key is accepted.</param>
    /// <exception cref="CryptographicException">The key is malformed or unsupported.</exception>
    public void Load(string path, bool publicOnly = false)
    {
        ThrowIfDisposed(); using var file = FileAccess.Open(path, FileAccessModeFlags.Read); var data = file.ReadBytes(checked((int)file.Length));
        try { if (data.AsSpan().StartsWith("-----"u8)) LoadFromString(Encoding.UTF8.GetString(data), publicOnly); else LoadDER(data, publicOnly); }
        finally { CryptographicOperations.ZeroMemory(data); }
    }
    /// <summary>Loads one unencrypted PEM key.</summary><param name="key">PKCS#1, PKCS#8, EC private or subject-public-key PEM text.</param><param name="publicOnly">Whether private-key PEM must be rejected.</param>
    /// <exception cref="InvalidOperationException">A TLS session is using this key.</exception>
    public void LoadFromString(string key, bool publicOnly = false)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(key);
        if (publicOnly ? key.Contains("PRIVATE KEY", StringComparison.Ordinal) : !key.Contains("PRIVATE KEY", StringComparison.Ordinal)) throw new CryptographicException("The PEM key does not match the requested public/private role.");
        byte[] encoded; bool elliptic;
        try { using var rsa = RSA.Create(); rsa.ImportFromPem(key); encoded = publicOnly ? rsa.ExportSubjectPublicKeyInfo() : rsa.ExportPkcs8PrivateKey(); elliptic = false; }
        catch (Exception error) when (error is CryptographicException or ArgumentException) { using var ec = ECDsa.Create(); ec.ImportFromPem(key); encoded = publicOnly ? ec.ExportSubjectPublicKeyInfo() : ec.ExportPkcs8PrivateKey(); elliptic = true; }
        Commit(encoded, publicOnly, elliptic);
    }
    private void LoadDER(ReadOnlySpan<byte> data, bool publicOnly)
    {
        byte[] encoded; bool elliptic;
        try
        {
            using var rsa = RSA.Create(); int consumed;
            if (publicOnly) { try { rsa.ImportSubjectPublicKeyInfo(data, out consumed); } catch (CryptographicException) { rsa.ImportRSAPublicKey(data, out consumed); } }
            else { try { rsa.ImportPkcs8PrivateKey(data, out consumed); } catch (CryptographicException) { rsa.ImportRSAPrivateKey(data, out consumed); } }
            if (consumed != data.Length) throw new CryptographicException("Trailing DER key data."); encoded = publicOnly ? rsa.ExportSubjectPublicKeyInfo() : rsa.ExportPkcs8PrivateKey(); elliptic = false;
        }
        catch (CryptographicException)
        {
            using var ec = ECDsa.Create(); int consumed;
            if (publicOnly) ec.ImportSubjectPublicKeyInfo(data, out consumed);
            else { try { ec.ImportPkcs8PrivateKey(data, out consumed); } catch (CryptographicException) { ec.ImportECPrivateKey(data, out consumed); } }
            if (consumed != data.Length) throw new CryptographicException("Trailing DER key data."); encoded = publicOnly ? ec.ExportSubjectPublicKeyInfo() : ec.ExportPkcs8PrivateKey(); elliptic = true;
        }
        Commit(encoded, publicOnly, elliptic);
    }

    private void Commit(byte[] encoded, bool publicOnly, bool elliptic)
    {
        try { lock (_gate) { ThrowIfDisposed(); if (_uses != 0) throw new InvalidOperationException("The key is in use by TLS."); CryptographicOperations.ZeroMemory(_encoded); _encoded = encoded; _publicOnly = publicOnly; _elliptic = elliptic; } }
        catch { CryptographicOperations.ZeroMemory(encoded); throw; }
        EmitChanged();
    }
    /// <summary>Saves PEM key text to an engine filesystem path.</summary><param name="path">Destination path.</param><param name="publicOnly">Whether to export only public material.</param>
    public void Save(string path, bool publicOnly = false) { var text = SaveToString(publicOnly); using var file = FileAccess.Open(path, FileAccessModeFlags.Write); file.WriteString(text); }
    /// <summary>Exports copied PEM key text.</summary><param name="publicOnly">Whether to export only public material.</param><returns>Subject-public-key or PKCS#8 PEM.</returns>
    /// <exception cref="CryptographicException">The key is empty or a public key was requested as private.</exception>
    public string SaveToString(bool publicOnly = false)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_encoded.Length == 0 || _publicOnly && !publicOnly) throw new CryptographicException("No private key is available.");
            if (publicOnly && !_publicOnly)
            {
                byte[] encoded;
                if (_elliptic) { using var ec = ECDsa.Create(); ec.ImportPkcs8PrivateKey(_encoded, out _); encoded = ec.ExportSubjectPublicKeyInfo(); }
                else { using var rsa = RSA.Create(); rsa.ImportPkcs8PrivateKey(_encoded, out _); encoded = rsa.ExportSubjectPublicKeyInfo(); }
                return PemEncoding.WriteString("PUBLIC KEY", encoded);
            }
            return PemEncoding.WriteString(publicOnly ? "PUBLIC KEY" : "PRIVATE KEY", _encoded);
        }
    }
    internal byte[] RetainPrivateKey() { lock (_gate) { ThrowIfDisposed(); if (_encoded.Length == 0 || _publicOnly) throw new CryptographicException("TLS requires a private key."); var copy = (byte[])_encoded.Clone(); _uses++; return copy; } }
    internal void ReleaseTLSUse() { lock (_gate) _uses--; }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); lock (_gate) if (_uses != 0) throw new InvalidOperationException("The key is in use by TLS."); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) { CryptographicOperations.ZeroMemory(_encoded); _encoded = []; } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override void OnResetState() { lock (_gate) if (_uses != 0) throw new InvalidOperationException("The key is in use by TLS."); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CryptoKey();
    internal void ReloadFrom(CryptoKey source) { byte[] encoded; bool publicOnly, elliptic; lock (source._gate) { source.ThrowIfDisposed(); encoded = (byte[])source._encoded.Clone(); publicOnly = source._publicOnly; elliptic = source._elliptic; } Commit(encoded, publicOnly, elliptic); }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    => ((CryptoKey)target).ReloadFrom(this);
}
