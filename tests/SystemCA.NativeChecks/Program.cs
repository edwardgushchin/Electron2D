using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using HostOS = Electron2D.OS;

if (args.Length != 3) throw new ArgumentException("fixture.dll, mode and output directory are required.");
Directory.CreateDirectory(args[2]);
Environment.SetEnvironmentVariable("ELECTRON2D_CA_FIXTURE_MODE", args[1]);
Environment.SetEnvironmentVariable("ELECTRON2D_CA_FIXTURE_DIRECTORY", Path.GetFullPath(args[2]));
NativeLibrary.SetDllImportResolver(typeof(HostOS).Assembly, (name, _, _) => name == "crypt32.dll" ? NativeLibrary.Load(Path.GetFullPath(args[0])) : 0);
for (var index = 0; index < 3; index++)
{
    using var key = RSA.Create(2048); var request = new CertificateRequest("CN=Private root " + index, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
    using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
    File.WriteAllBytes(Path.Combine(args[2], index + ".der"), certificate.RawData);
}
if (args[1] is "filter" or "zero-time" or "empty")
{
    var pem = HostOS.GetSystemCACertificates(); File.WriteAllText(Path.Combine(args[2], "snapshot.pem"), pem);
    var certificates = new X509Certificate2Collection();
    try
    {
        certificates.ImportFromPem(pem);
        if (args[1] == "empty") { if (pem.Length != 0) throw new Exception("Empty source must export empty text."); }
        else
        {
            var actual = certificates.Select(value => Convert.ToHexString(SHA256.HashData(value.RawData))).Order().ToArray();
            var expected = new[] { 1, 2 }.Select(index => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(args[2], index + ".der"))))).Order().ToArray();
            if (!actual.SequenceEqual(expected)) throw new Exception("Past disallow time must be excluded; future and absent disallow times must remain.");
        }
    }
    finally { foreach (var certificate in certificates) certificate.Dispose(); }
    Environment.SetEnvironmentVariable("ELECTRON2D_CA_FIXTURE_MODE", "empty");
    if (HostOS.GetSystemCACertificates().Length != 0) throw new Exception("A subsequent query must observe a changed native source.");
}
else
{
    try { HostOS.GetSystemCACertificates(); throw new Exception("The native failure must reject the snapshot."); }
    catch (IOException) { }
}
if (File.ReadAllText(Path.Combine(args[2], "closed.txt")).Trim() != "outstanding=0") throw new Exception("Native certificates/store did not drain.");
Console.WriteLine("Native CA source: " + args[1] + "; exact linked producer; all contexts and stores drained.");
