using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

internal static class TLSOracleIdentity
{
    // Takes ownership; Schannel needs a named key, deleted when the returned certificate is disposed.
    internal static X509Certificate2 ForServer(X509Certificate2 identity)
    {
        if (!OperatingSystem.IsWindows()) return identity;
        using (identity)
        {
            var pkcs12 = identity.Export(X509ContentType.Pkcs12);
            try { return X509CertificateLoader.LoadPkcs12(pkcs12, null, X509KeyStorageFlags.UserKeySet); }
            finally { CryptographicOperations.ZeroMemory(pkcs12); }
        }
    }
}
