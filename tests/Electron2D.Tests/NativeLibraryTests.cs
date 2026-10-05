using System.Runtime.InteropServices;

internal static class NativeLibraryTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
        var names = OperatingSystem.IsMacOS() ? new[] { "libElectron2DTextBreak.dylib", "libFAudio.0.dylib", "libElectron2DENet.dylib", "libElectron2DCrypto.3.dylib", "libElectron2DSSL.3.dylib", "libElectron2DFreeType.dylib" } : ["libElectron2DTextBreak.so", "libFAudio.so.0", "libElectron2DENet.so"];
        foreach (var name in names)
        {
            var file = Path.Combine(AppContext.BaseDirectory, "runtimes", RuntimeInformation.RuntimeIdentifier, "native", name);
            if (!File.Exists(file) || File.Exists(Path.Combine(AppContext.BaseDirectory, name)))
                throw new InvalidOperationException($"Project-referenced native library must exist only under runtimes/RID/native: {name}");
        }
        Console.WriteLine("Project-referenced native text/audio/ENet libraries retain their runtime directory without root copies.");
    }
}
