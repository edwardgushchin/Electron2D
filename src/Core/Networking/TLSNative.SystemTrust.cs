using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Electron2D;

internal static unsafe partial class TLSNative
{
    internal static void ConfigureSystemTrust(nint context) => SSL_CTX_set_cert_verify_callback(context, &VerifySystemTrust, 0);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int VerifySystemTrust(nint context, nint argument) => VerifySystemCertificate(context);

    internal static int VerifySystemCertificate(nint context)
    {
        var certificates = new List<X509Certificate2>();
        using var chain = new X509Chain();
        try
        {
            using var leaf = ReadCertificate(X509_STORE_CTX_get0_cert(context));
            var untrusted = X509_STORE_CTX_get0_untrusted(context);
            var count = untrusted == 0 ? 0 : OPENSSL_sk_num(untrusted);
            if (count is < 0 or > 64) throw new CryptographicException("The TLS certificate chain exceeds its budget.");
            for (var index = 0; index < count; index++) certificates.Add(ReadCertificate(OPENSSL_sk_value(untrusted, index)));
            chain.ChainPolicy.ExtraStore.AddRange(certificates.ToArray());
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.DisableCertificateDownloads = true;
            chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
            if (!chain.Build(leaf)) { X509_STORE_CTX_set_error(context, 28); return 0; }

            // Preserve OS trust decisions, then let OpenSSL validate names and its TLS certificate policy.
            var store = X509_STORE_CTX_get0_store(context);
            for (var index = 1; index < chain.ChainElements.Count; index++)
            {
                using var certificate = DecodeCertificate(chain.ChainElements[index].Certificate.RawData);
                Require(X509_STORE_add_cert(store, certificate.Pointer), "System TLS certificate import failed.");
            }
            if (chain.ChainElements.Count == 1) Require(X509_STORE_add_cert(store, X509_STORE_CTX_get0_cert(context)), "System TLS anchor import failed.");
            Require(X509_VERIFY_PARAM_set_flags(X509_STORE_CTX_get0_param(context), 0x80000), "System TLS partial-chain trust failed.");
            return X509_verify_cert(context);
        }
        catch { X509_STORE_CTX_set_error(context, 50); return 0; }
        finally
        {
            foreach (var certificate in certificates) certificate.Dispose();
            foreach (var element in chain.ChainElements) element.Certificate.Dispose();
        }
    }

    private static X509Certificate2 ReadCertificate(nint certificate)
    {
        if (certificate == 0) throw new CryptographicException("The native TLS certificate is missing.");
        var length = i2d_X509(certificate, null);
        if (length is <= 0 or > 1024 * 1024) throw new CryptographicException("Invalid native TLS certificate length.");
        var data = new byte[length];
        fixed (byte* start = data)
        {
            var cursor = start;
            if (i2d_X509(certificate, &cursor) != length) throw new CryptographicException("Native TLS certificate encoding failed.");
        }
        return X509CertificateLoader.LoadCertificate(data);
    }

    [LibraryImport(SSL), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void SSL_CTX_set_cert_verify_callback(nint context, delegate* unmanaged[Cdecl]<nint, nint, int> callback, nint argument);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint X509_STORE_CTX_get0_cert(nint context);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint X509_STORE_CTX_get0_untrusted(nint context);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint X509_STORE_CTX_get0_store(nint context);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint X509_STORE_CTX_get0_param(nint context);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void X509_STORE_CTX_set_error(nint context, int error);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int X509_VERIFY_PARAM_set_flags(nint parameters, ulong flags);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int X509_verify_cert(nint context);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int OPENSSL_sk_num(nint stack);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint OPENSSL_sk_value(nint stack, int index);
    [LibraryImport(Crypto), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int i2d_X509(nint certificate, byte** data);
}
