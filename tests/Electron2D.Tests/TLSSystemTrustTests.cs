using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Electron2D;
using Path = System.IO.Path;

internal static unsafe class TLSSystemTrustTests
{
    internal static void Run()
    {
        var name = OperatingSystem.IsWindows() ? "libcrypto-3-Electron2D.dll" : OperatingSystem.IsMacOS() ? "libElectron2DCrypto.3.dylib" : "libcrypto.so.3";
        var path = OperatingSystem.IsLinux() ? name : Path.Combine(AppContext.BaseDirectory, "runtimes", NativeLibraries.RuntimeRID, "native", name);
        var library = NativeLibrary.Load(path);
        try
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=Untrusted native TLS fixture", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var untrusted = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
            Check(untrusted, false);
            Check(null, false);
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates;
            try
            {
                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.DisableCertificateDownloads = true;
                chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
                var root = certificates.FirstOrDefault(certificate => chain.Build(certificate)) ?? throw new InvalidOperationException("The system TLS root store is empty.");
                Check(root, true);
            }
            finally { foreach (var certificate in certificates) certificate.Dispose(); }
            Console.WriteLine("Native TLS system validation accepts an OS anchor and rejects an unknown root or missing certificate without modifying trust stores.");

            void Check(X509Certificate2? certificate, bool expected)
            {
                var newStore = (delegate* unmanaged[Cdecl]<nint>)NativeLibrary.GetExport(library, "X509_STORE_new");
                var newContext = (delegate* unmanaged[Cdecl]<nint>)NativeLibrary.GetExport(library, "X509_STORE_CTX_new");
                var decode = (delegate* unmanaged[Cdecl]<nint, byte**, CLong, nint>)NativeLibrary.GetExport(library, "d2i_X509");
                var initialize = (delegate* unmanaged[Cdecl]<nint, nint, nint, nint, int>)NativeLibrary.GetExport(library, "X509_STORE_CTX_init");
                var freeCertificate = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "X509_free");
                var freeContext = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "X509_STORE_CTX_free");
                var freeStore = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "X509_STORE_free");
                var store = newStore(); var context = newContext(); nint native = 0;
                try
                {
                    if (store == 0 || context == 0) throw new OutOfMemoryException();
                    if (certificate is not null)
                    {
                        var bytes = certificate.RawData;
                        fixed (byte* start = bytes) { var cursor = start; native = decode(0, &cursor, new CLong(bytes.Length)); }
                        if (native == 0) throw new InvalidOperationException("Fixture certificate import failed.");
                    }
                    if (initialize(context, store, native, 0) != 1) throw new InvalidOperationException("Fixture verification context failed.");
                    if ((TLSNative.VerifySystemCertificate(context) == 1) != expected) throw new InvalidOperationException("Native TLS system trust result differs from the OS certificate policy.");
                }
                finally { freeContext(context); freeCertificate(native); freeStore(store); }
            }
        }
        finally { NativeLibrary.Free(library); }
    }
}
