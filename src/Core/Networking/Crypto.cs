using System.Formats.Asn1;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CryptoRandom = System.Security.Cryptography.RandomNumberGenerator;
namespace Electron2D;

/// <summary>Generates keys/certificates and performs RSA, digest-signature, HMAC and secure-random operations.</summary>
/// <remarks>Operations/disposal require the constructing thread. Key operations import an independent BCL key
/// snapshot under the resource lock and release it after the call. Generated resources are caller-owned. Key
/// generation, import, asymmetric operations and copied results are cold work; prepared contexts handle streams.</remarks>
public class Crypto : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    /// <summary>Creates a cryptographic operation provider.</summary>
    public Crypto() { }
    private void Check() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("Crypto operations require the constructing thread."); }
    /// <summary>Generates copied cryptographically secure random bytes.</summary><param name="size">Nonnegative requested byte count; zero is valid.</param><returns>A caller-owned array of the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Size is negative.</exception>
    public byte[] GenerateRandomBytes(int size) { Check(); ArgumentOutOfRangeException.ThrowIfNegative(size); var bytes = new byte[size]; GenerateRandomBytes(bytes.AsSpan()); return bytes; }
    /// <summary>Fills caller storage with cryptographically secure random bytes.</summary><param name="destination">Borrowed output storage, including an empty span.</param>
    public void GenerateRandomBytes(Span<byte> destination) { Check(); CryptoRandom.Fill(destination); }
    /// <summary>Generates a private RSA key resource.</summary><param name="size">Key size in bits, subject to the current BCL provider's supported sizes.</param><returns>A caller-owned canonical private key.</returns>
    /// <exception cref="CryptographicException">Key size or generation is unsupported.</exception>
    public CryptoKey GenerateRSA(int size)
    {
        Check(); using var rsa = RSA.Create(size); var encoded = rsa.ExportPkcs8PrivateKey(); var result = new CryptoKey();
        try { result.LoadDER(encoded, false); return result; } catch { result.Dispose(); throw; } finally { CryptographicOperations.ZeroMemory(encoded); }
    }
    /// <summary>Generates a SHA-256 signed X.509 v3 self-signed CA certificate with path length zero.</summary><param name="key">Loaded RSA/EC private key, borrowed during the call.</param><param name="issuerName">Subject/issuer X.500 name containing nonempty CN, O and two-letter C.</param><param name="notBefore">First valid UTC instant as yyyyMMddHHmmss.</param><param name="notAfter">Last valid UTC instant in the same format, strictly later.</param><returns>A caller-owned certificate resource without private-key material.</returns>
    /// <exception cref="ArgumentException">Name or dates are invalid.</exception>
    /// <exception cref="CryptographicException">A usable private key is unavailable.</exception>
    public X509Certificate GenerateSelfSignedCertificate(CryptoKey key, string issuerName = "CN=myserver,O=myorganisation,C=IT", string notBefore = "20140101000000", string notAfter = "20340101000000")
    {
        Check(); ArgumentNullException.ThrowIfNull(key); var name = ParseName(issuerName); var before = Date(notBefore); var after = Date(notAfter); if (after <= before) throw new ArgumentException("Certificate end must follow its start.", nameof(notAfter));
        using var algorithm = key.CreateAlgorithm(true); CertificateRequest request; X509SignatureGenerator generator;
        if (algorithm is RSA rsa) { request = new CertificateRequest(name, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); generator = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1); }
        else { var ec = (ECDsa)algorithm; request = new CertificateRequest(name, ec, HashAlgorithmName.SHA256); generator = X509SignatureGenerator.CreateForECDsa(ec); }
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true)); Span<byte> serial = stackalloc byte[20]; CryptoRandom.Fill(serial); serial[0] &= 0x7f; serial[^1] |= 1;
        using var certificate = request.Create(name, generator, before, after, serial); var result = new X509Certificate();
        try { result.LoadFromString(certificate.ExportCertificatePem()); return result; } catch { result.Dispose(); throw; }
    }
    private static DateTimeOffset Date(string text)
    {
        ArgumentNullException.ThrowIfNull(text); if (text.Length != 14 || !DateTimeOffset.TryParseExact(text, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var instant)) throw new ArgumentException("UTC certificate dates require yyyyMMddHHmmss.", nameof(text)); return instant;
    }
    private static X500DistinguishedName ParseName(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text); if (text.Contains('\0')) throw new ArgumentException("Certificate names cannot contain NUL.", nameof(text)); var name = new X500DistinguishedName(text); var outer = new AsnReader(name.RawData, AsnEncodingRules.DER); var sequence = outer.ReadSequence(); var seen = 0;
        while (sequence.HasData)
        {
            var set = sequence.ReadSetOf(); while (set.HasData)
            {
                var attribute = set.ReadSequence(); var oid = attribute.ReadObjectIdentifier(); var value = attribute.ReadCharacterString((UniversalTagNumber)attribute.PeekTag().TagValue); attribute.ThrowIfNotEmpty(); if (value.Length == 0) throw new ArgumentException("Certificate name attributes cannot be empty.", nameof(text));
                if (oid == "2.5.4.3") seen |= 1; if (oid == "2.5.4.10") seen |= 2; if (oid == "2.5.4.6") { if (value.Length != 2 || value.Any(c => !char.IsAsciiLetter(c))) throw new ArgumentException("Country must contain two letters.", nameof(text)); seen |= 4; }
            }
        }
        if (seen != 7) throw new ArgumentException("Certificate issuer requires CN, O and C attributes.", nameof(text)); return name;
    }
    /// <summary>Signs a precomputed MD5/SHA1/SHA256 digest.</summary><param name="hashType">Digest identity.</param><param name="hash">Exactly 16, 20 or 32 bytes.</param><param name="key">Loaded private RSA/EC key.</param><returns>RSA PKCS#1 v1.5 or ECDSA DER signature bytes.</returns>
    /// <exception cref="ArgumentException">Digest length is invalid.</exception>
    /// <exception cref="CryptographicException">The key or signing operation is invalid.</exception>
    public byte[] Sign(HashType hashType, ReadOnlySpan<byte> hash, CryptoKey key)
    {
        Check(); ArgumentNullException.ThrowIfNull(key); CryptoAlgorithms.ValidateDigest(hashType, hash); using var algorithm = key.CreateAlgorithm(true); var name = CryptoAlgorithms.Name(hashType);
        return algorithm is RSA rsa ? rsa.SignHash(hash, name, RSASignaturePadding.Pkcs1) : ((ECDsa)algorithm).SignHash(hash, DSASignatureFormat.Rfc3279DerSequence);
    }
    /// <summary>Verifies a precomputed digest and RSA PKCS#1/ECDSA DER signature.</summary><param name="hashType">Digest identity.</param><param name="hash">Exactly the selected digest length.</param><param name="signature">Untrusted signature bytes.</param><param name="key">Loaded public or private RSA/EC key.</param><returns>False for a mismatched or malformed signature.</returns>
    /// <exception cref="ArgumentException">The supplied digest has the wrong length.</exception>
    public bool Verify(HashType hashType, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, CryptoKey key)
    {
        Check(); ArgumentNullException.ThrowIfNull(key); CryptoAlgorithms.ValidateDigest(hashType, hash); using var algorithm = key.CreateAlgorithm(); var name = CryptoAlgorithms.Name(hashType);
        try { return algorithm is RSA rsa ? rsa.VerifyHash(hash, signature, name, RSASignaturePadding.Pkcs1) : ((ECDsa)algorithm).VerifyHash(hash, signature, DSASignatureFormat.Rfc3279DerSequence); } catch (CryptographicException) { return false; }
    }
    /// <summary>Encrypts one RSA plaintext using PKCS#1 v1.5 padding.</summary><param name="key">Loaded public or private RSA key.</param><param name="plaintext">At most modulus byte length minus eleven.</param><returns>One modulus-sized ciphertext.</returns>
    /// <exception cref="NotSupportedException">An EC key is supplied.</exception>
    /// <exception cref="CryptographicException">The key or plaintext size is invalid.</exception>
    public byte[] Encrypt(CryptoKey key, ReadOnlySpan<byte> plaintext)
    {
        Check(); ArgumentNullException.ThrowIfNull(key); using var algorithm = key.CreateAlgorithm(); if (algorithm is not RSA rsa) throw new NotSupportedException("Asymmetric encryption requires RSA."); return rsa.Encrypt(plaintext, RSAEncryptionPadding.Pkcs1);
    }
    /// <summary>Decrypts one modulus-sized RSA ciphertext using a private key and PKCS#1 v1.5 padding.</summary><param name="key">Loaded private RSA key.</param><param name="ciphertext">One RSA ciphertext.</param><returns>Caller-owned plaintext bytes.</returns>
    /// <exception cref="CryptographicException">Key role, ciphertext length or padding is invalid.</exception>
    public byte[] Decrypt(CryptoKey key, ReadOnlySpan<byte> ciphertext)
    {
        Check(); ArgumentNullException.ThrowIfNull(key); using var algorithm = key.CreateAlgorithm(true); if (algorithm is not RSA rsa) throw new NotSupportedException("Asymmetric decryption requires RSA."); if (ciphertext.Length != (rsa.KeySize + 7) / 8) throw new CryptographicException("Ciphertext must match the RSA modulus length."); return rsa.Decrypt(ciphertext, RSAEncryptionPadding.Pkcs1);
    }
    /// <summary>Computes a complete HMAC-SHA1/HMAC-SHA256 message digest.</summary><param name="hashType">SHA1 or SHA256.</param><param name="key">Borrowed nonempty authentication key.</param><param name="message">Borrowed nonempty message bytes.</param><returns>Caller-owned authentication bytes.</returns>
    /// <exception cref="NotSupportedException">MD5 is selected.</exception>
    /// <exception cref="ArgumentException">Key or message is empty.</exception>
    public byte[] HMACDigest(HashType hashType, ReadOnlySpan<byte> key, ReadOnlySpan<byte> message) { Check(); using var context = new HMACContext(); context.Start(hashType, key); context.Update(message); return context.Finish(); }
    /// <summary>Compares equal-length byte strings without content-dependent early exit.</summary><param name="trusted">Trusted bytes.</param><param name="received">Received bytes.</param><returns>True for equal bytes, including two empty spans; false for different lengths or content.</returns>
    /// <remarks>Lengths are public to the comparison; different lengths reject before comparing content.</remarks>
    public bool ConstantTimeCompare(ReadOnlySpan<byte> trusted, ReadOnlySpan<byte> received) { Check(); return CryptographicOperations.FixedTimeEquals(trusted, received); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); Check(); }
}
