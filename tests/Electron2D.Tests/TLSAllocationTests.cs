using Electron2D;
using System.Security.Cryptography;

internal static class TLSAllocationTests
{
    internal static void Run()
    {
        // BCL calls must reach the selected original allocator before engine TLS use.
        for (var i = 0; i < 4; i++) Exercise(false);
        var before = NativeAllocation.Snapshot();
        for (var i = 0; i < 8; i++) Exercise(true);
        var after = NativeAllocation.Snapshot();
        Check(after.Current == before.Current, "Disposed BCL keys must retire their native ownership.");
        Check(after.Peak >= before.Peak, "Native high-water survives BCL key retirement.");
        Console.WriteLine($"BCL cryptography: live keys counted, disposed keys retired; retained={after.Current} B, peak={after.Peak} B.");
    }

    private static void Exercise(bool check)
    {
        var before = NativeAllocation.Snapshot().Current;
        using (var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        using (var rsa = RSA.Create(2048))
        {
            var payload = "native key ownership"u8;
            var ecSignature = ec.SignData(payload, HashAlgorithmName.SHA256);
            var rsaSignature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Check(ec.VerifyData(payload, ecSignature, HashAlgorithmName.SHA256), "ECDSA vector verifies.");
            Check(rsa.VerifyData(payload, rsaSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1), "RSA vector verifies.");
            if (check) Check(NativeAllocation.Snapshot().Current > before, "Live BCL key storage must enter the common native ledger.");
        }
        if (check) Check(NativeAllocation.Snapshot().Current == before, "Every BCL key pair must retire before returning.");
    }

    private static void Check(bool valid, string message)
    { if (!valid) throw new InvalidOperationException(message); }
}
