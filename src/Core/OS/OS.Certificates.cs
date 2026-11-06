using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Exports the operating system's trusted certification authorities as concatenated PEM certificates.</summary>
    /// <returns>A fresh public-certificate snapshot, or empty when the supported source contains no anchors.</returns>
    /// <remarks>This cold operation opens no window and does not change trust stores. Windows reads the current-user
    /// ROOT collection, including inherited machine roots, and omits arrived or empty native disallow markers.
    /// Desktop Unix reads the first existing standard CA bundle; macOS copies the native anchor collection.
    /// The snapshot does not preserve all native trust policy or later changes. TLSOptions.Client with no custom chain
    /// retains live operating-system validation; pass an explicitly loaded X509Certificate to select this snapshot.</remarks>
    /// <exception cref="IOException">The native source or selected certificate bundle cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The selected bundle cannot be accessed.</exception>
    /// <exception cref="PlatformNotSupportedException">The host has no implemented certificate-export adapter.</exception>
    public static string GetSystemCACertificates()
    {
        Service.ThrowIfDisposed();
        if (OperatingSystem.IsWindows()) return SystemCertificates.Windows();
        if (OperatingSystem.IsMacOS()) return SystemCertificates.Apple();
        if ((OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()) || OperatingSystem.IsFreeBSD())
        {
            foreach (var path in new[] { "/etc/ssl/certs/ca-certificates.crt", "/etc/pki/ca-trust/extracted/pem/tls-ca-bundle.pem", "/etc/ca-certificates/extracted/tls-ca-bundle.pem", "/var/lib/ca-certificates/ca-bundle.pem", "/etc/ssl/cert.pem" })
                if (File.Exists(path)) return File.ReadAllText(path);
            return "";
        }
        throw new PlatformNotSupportedException("This host requires a native certificate-export adapter.");
    }

    private static unsafe partial class SystemCertificates
    {
        private const int NotFound = unchecked((int)0x80092004);
        [StructLayout(LayoutKind.Sequential)]
        private struct CertificateContext { internal uint Encoding; internal nint Encoded; internal uint Length; internal nint Info, Store; }

        internal static string Windows()
        {
            nint store;
            // Open the effective user collection read-only, without creating a missing store.
            fixed (char* name = "ROOT") store = CertOpenStore(10, 0, 0, 0x1c000, name);
            if (store == 0) throw NativeError("open the system root certificate store");
            nint current = 0;
            try
            {
                var result = new StringBuilder(); var now = (ulong)DateTime.UtcNow.ToFileTimeUtc();
                while ((current = CertEnumCertificatesInStore(store, current)) != 0)
                {
                    uint size = 8;
                    if (CertGetCertificateContextProperty(current, 104, out var disallowed, ref size) != 0)
                    {
                        // Windows also publishes empty disallow markers; never interpret unwritten time bytes as trust.
                        if (size == 0) continue;
                        if (size != 8) throw new IOException("The native certificate disallow time has an invalid size.");
                        if (now >= disallowed) continue;
                    }
                    else if (Marshal.GetLastPInvokeError() != NotFound) throw NativeError("read a certificate's trust metadata");
                    var certificate = *(CertificateContext*)current;
                    Append(result, certificate.Encoded, certificate.Length);
                }
                if (Marshal.GetLastPInvokeError() != NotFound) throw NativeError("enumerate system root certificates");
                return result.ToString();
            }
            finally
            {
                if (current != 0) CertFreeCertificateContext(current);
                CertCloseStore(store, 0);
            }
        }
        private static IOException NativeError(string operation) => new($"Windows could not {operation} (error 0x{Marshal.GetLastPInvokeError():X8}).");
        private static void Append(StringBuilder result, nint data, long length)
        {
            if (data == 0 || length is <= 0 or > 1024 * 1024) throw new IOException("The native root certificate has an invalid encoding length.");
            result.Append(PemEncoding.WriteString("CERTIFICATE", new ReadOnlySpan<byte>((void*)data, (int)length))).Append('\n');
        }
        internal static string Apple()
        {
            var status = SecTrustCopyAnchorCertificates(out var anchors);
            if (status != 0) { if (anchors != 0) CFRelease(anchors); throw new IOException($"The system anchor query failed (OSStatus {status})."); }
            if (anchors == 0) throw new IOException("The system anchor query returned no collection.");
            try
            {
                var result = new StringBuilder(); var count = CFArrayGetCount(anchors);
                if (count < 0) throw new IOException("The system anchor collection has an invalid size.");
                for (nint index = 0; index < count; index++)
                {
                    var certificate = CFArrayGetValueAtIndex(anchors, index);
                    if (certificate == 0) throw new IOException("The system anchor collection contains no certificate.");
                    var data = SecCertificateCopyData(certificate);
                    if (data == 0) throw new IOException("The system anchor encoding is unavailable.");
                    try { Append(result, CFDataGetBytePtr(data), CFDataGetLength(data)); }
                    finally { CFRelease(data); }
                }
                return result.ToString();
            }
            finally { CFRelease(anchors); }
        }
        [LibraryImport("crypt32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial nint CertOpenStore(nint provider, uint encoding, nint cryptography, uint flags, char* parameter);
        [LibraryImport("crypt32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial nint CertEnumCertificatesInStore(nint store, nint previous);
        [LibraryImport("crypt32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CertGetCertificateContextProperty(nint context, uint property, out ulong value, ref uint size);
        [LibraryImport("crypt32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CertFreeCertificateContext(nint context);
        [LibraryImport("crypt32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static partial int CertCloseStore(nint store, uint flags);
        private const string Security = "/System/Library/Frameworks/Security.framework/Security", Foundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        [LibraryImport(Security)] private static partial int SecTrustCopyAnchorCertificates(out nint anchors);
        [LibraryImport(Security)] private static partial nint SecCertificateCopyData(nint certificate);
        [LibraryImport(Foundation)] private static partial nint CFArrayGetCount(nint array);
        [LibraryImport(Foundation)] private static partial nint CFArrayGetValueAtIndex(nint array, nint index);
        [LibraryImport(Foundation)] private static partial nint CFDataGetLength(nint data);
        [LibraryImport(Foundation)] private static partial nint CFDataGetBytePtr(nint data);
        [LibraryImport(Foundation)] private static partial void CFRelease(nint value);
    }
}
