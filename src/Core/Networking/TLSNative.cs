using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
namespace Electron2D;

internal sealed class TLSHandle : SafeHandle
{
    private readonly Action<nint> _release;
    internal TLSHandle(nint pointer, Action<nint> release) : base(0, true) { SetHandle(pointer); _release = release; }
    public override bool IsInvalid => handle == 0;
    internal nint Pointer => handle;
    protected override bool ReleaseHandle() { _release(handle); return true; }
}

internal static unsafe partial class TLSNative
{
    private const string SSL = "libssl.so.3", Crypto = "libcrypto.so.3";
    internal static void CheckBackend()
    {
        if (!OperatingSystem.IsLinux() || IntPtr.Size != 8) throw new PlatformNotSupportedException("TLS currently requires the 64-bit Linux OpenSSL 3 backend.");
        try { if (TLS_method() == 0) throw new PlatformNotSupportedException("OpenSSL TLS is unavailable."); }
        catch (DllNotFoundException error) { throw new PlatformNotSupportedException("TLS requires system OpenSSL 3 libraries.", error); }
    }
    internal static TLSHandle CreateContext(TLSOptions options)
    {
        CheckBackend(); ERR_clear_error(); var pointer = SSL_CTX_new(TLS_method()); if (pointer == 0) throw Failure("TLS context creation failed.");
        var context = new TLSHandle(pointer, SSL_CTX_free);
        try
        {
            Require(SSL_CTX_ctrl(pointer, 123, 0x303, 0), "TLS 1.2 minimum configuration failed.");
            SSL_CTX_set_verify(pointer, options.IsServer() || options.IsUnsafeClient() && options.GetTrustedCAChain() is null ? 0 : 1, 0);
            if (!options.IsServer() && options.GetTrustedCAChain() is null && !options.IsUnsafeClient()) Require(SSL_CTX_set_default_verify_paths(pointer), "System TLS trust is unavailable.");
            return context;
        }
        catch { context.Dispose(); throw; }
    }
    internal static void AddTrust(nint context, byte[][] certificates)
    {
        var store = SSL_CTX_get_cert_store(context);
        foreach (var bytes in certificates) { using var certificate = DecodeCertificate(bytes); Require(X509_STORE_add_cert(store, certificate.Pointer), "Trust certificate import failed."); }
    }
    internal static void SetIdentity(nint context, byte[][] certificates, byte[] key)
    {
        using var leaf = DecodeCertificate(certificates[0]); Require(SSL_CTX_use_certificate(context, leaf.Pointer), "TLS leaf certificate import failed.");
        foreach (var bytes in certificates.Skip(1))
        {
            var certificate = DecodeCertificate(bytes);
            try { Require(SSL_CTX_ctrl(context, 14, 0, certificate.Pointer), "TLS intermediate certificate import failed."); certificate.SetHandleAsInvalid(); }
            finally { certificate.Dispose(); }
        }
        fixed (byte* p = key) { var cursor = p; var pointer = d2i_AutoPrivateKey(0, ref cursor, key.Length); if (pointer == 0) throw Failure("TLS private key import failed."); using var nativeKey = new TLSHandle(pointer, EVP_PKEY_free); Require(SSL_CTX_use_PrivateKey(context, pointer), "TLS private key configuration failed."); }
        Require(SSL_CTX_check_private_key(context), "TLS key does not match the certificate.");
    }
    private static TLSHandle DecodeCertificate(byte[] data)
    {
        fixed (byte* p = data) { var cursor = p; var pointer = d2i_X509(0, ref cursor, data.Length); if (pointer == 0) throw Failure("TLS certificate import failed."); return new(pointer, X509_free); }
    }
    internal static TLSHandle CreateSession(nint context, bool server, string name, bool validateName, out TLSHandle network)
    {
        var pointer = SSL_new(context); if (pointer == 0) throw Failure("TLS session creation failed."); var session = new TLSHandle(pointer, SSL_free); network = null!;
        try
        {
            Require(BIO_new_bio_pair(out var inner, 65536, out var outer, 65536), "TLS I/O allocation failed.");
            network = new(outer, BIO_free_void); SSL_set_bio(pointer, inner, inner);
            SSL_ctrl(pointer, 33, 3, 0);
            if (server) SSL_set_accept_state(pointer);
            else
            {
                SSL_set_connect_state(pointer);
                if (name.Length != 0)
                {
                    var encoded = Encoding.UTF8.GetBytes(name + '\0'); fixed (byte* p = encoded)
                    {
                        if (System.Net.IPAddress.TryParse(name, out _)) { if (validateName) Require(X509_VERIFY_PARAM_set1_ip_asc(SSL_get0_param(pointer), p), "TLS expected IP configuration failed."); }
                        else { Require(SSL_ctrl(pointer, 55, 0, (nint)p), "TLS SNI configuration failed."); if (validateName) Require(SSL_set1_host(pointer, p), "TLS expected hostname configuration failed."); }
                    }
                }
            }
            return session;
        }
        catch { session.Dispose(); network?.Dispose(); throw; }
    }
    private static void BIO_free_void(nint value) => BIO_free(value);
    internal static void Require(long result, string message) { if (result <= 0) throw Failure(message); }
    internal static AuthenticationException Failure(string message)
    {
        Span<byte> text = stackalloc byte[256]; var error = ERR_get_error(); fixed (byte* p = text) ERR_error_string_n(error, p, (nuint)text.Length);
        var end = text.IndexOf((byte)0); return new AuthenticationException(message + " " + Encoding.UTF8.GetString(end < 0 ? text : text[..end]));
    }
    internal static int ReadBIO(nint bio, Span<byte> bytes) { fixed (byte* p = bytes) return BIO_read(bio, p, bytes.Length); }
    internal static int WriteBIO(nint bio, ReadOnlySpan<byte> bytes) { fixed (byte* p = bytes) return BIO_write(bio, p, bytes.Length); }
    internal static int Read(nint ssl, Span<byte> bytes) { fixed (byte* p = bytes) return SSL_read(ssl, p, bytes.Length); }
    internal static int Write(nint ssl, ReadOnlySpan<byte> bytes) { fixed (byte* p = bytes) return SSL_write(ssl, p, bytes.Length); }
    internal static int Peek(nint ssl) { byte value; return SSL_peek(ssl, &value, 1); }
    [LibraryImport(SSL)] internal static partial nint TLS_method();
    [LibraryImport(SSL)] private static partial nint SSL_CTX_new(nint method);
    [LibraryImport(SSL)] private static partial void SSL_CTX_free(nint context);
    [LibraryImport(SSL)] private static partial long SSL_CTX_ctrl(nint context, int command, long value, nint pointer);
    [LibraryImport(SSL)] private static partial void SSL_CTX_set_verify(nint context, int mode, nint callback);
    [LibraryImport(SSL)] private static partial int SSL_CTX_set_default_verify_paths(nint context);
    [LibraryImport(SSL)] private static partial nint SSL_CTX_get_cert_store(nint context);
    [LibraryImport(SSL)] private static partial int SSL_CTX_use_certificate(nint context, nint certificate);
    [LibraryImport(SSL)] private static partial int SSL_CTX_use_PrivateKey(nint context, nint key);
    [LibraryImport(SSL)] private static partial int SSL_CTX_check_private_key(nint context);
    [LibraryImport(SSL)] private static partial nint SSL_new(nint context);
    [LibraryImport(SSL)] private static partial void SSL_free(nint ssl);
    [LibraryImport(SSL)] private static partial void SSL_set_bio(nint ssl, nint read, nint write);
    [LibraryImport(SSL)] private static partial void SSL_set_connect_state(nint ssl);
    [LibraryImport(SSL)] private static partial void SSL_set_accept_state(nint ssl);
    [LibraryImport(SSL)] private static partial long SSL_ctrl(nint ssl, int command, long value, nint pointer);
    [LibraryImport(SSL)] private static partial int SSL_set1_host(nint ssl, byte* name);
    [LibraryImport(SSL)] private static partial nint SSL_get0_param(nint ssl);
    [LibraryImport(SSL)] internal static partial int SSL_do_handshake(nint ssl);
    [LibraryImport(SSL)] internal static partial int SSL_get_error(nint ssl, int result);
    [LibraryImport(SSL)] internal static partial long SSL_get_verify_result(nint ssl);
    [LibraryImport(SSL)] internal static partial int SSL_pending(nint ssl);
    [LibraryImport(SSL)] internal static partial int SSL_shutdown(nint ssl);
    [LibraryImport(SSL)] private static partial int SSL_read(nint ssl, byte* buffer, int size);
    [LibraryImport(SSL)] private static partial int SSL_write(nint ssl, byte* buffer, int size);
    [LibraryImport(SSL)] private static partial int SSL_peek(nint ssl, byte* buffer, int size);
    [LibraryImport(Crypto)] private static partial int BIO_new_bio_pair(out nint first, nuint firstSize, out nint second, nuint secondSize);
    [LibraryImport(Crypto)] private static partial int BIO_free(nint bio);
    [LibraryImport(Crypto)] private static partial int BIO_read(nint bio, byte* buffer, int length);
    [LibraryImport(Crypto)] private static partial int BIO_write(nint bio, byte* buffer, int length);
    [LibraryImport(Crypto)] internal static partial long BIO_ctrl(nint bio, int command, long value, nint pointer);
    [LibraryImport(Crypto)] private static partial nint d2i_X509(nint ignored, ref byte* data, long size);
    [LibraryImport(Crypto)] private static partial void X509_free(nint certificate);
    [LibraryImport(Crypto)] private static partial int X509_STORE_add_cert(nint store, nint certificate);
    [LibraryImport(Crypto)] private static partial int X509_VERIFY_PARAM_set1_ip_asc(nint parameters, byte* address);
    [LibraryImport(Crypto)] private static partial nint d2i_AutoPrivateKey(nint ignored, ref byte* data, long size);
    [LibraryImport(Crypto)] private static partial void EVP_PKEY_free(nint key);
    [LibraryImport(Crypto)] internal static partial void ERR_clear_error();
    [LibraryImport(Crypto)] private static partial ulong ERR_get_error();
    [LibraryImport(Crypto)] private static partial void ERR_error_string_n(ulong error, byte* buffer, nuint size);
}
