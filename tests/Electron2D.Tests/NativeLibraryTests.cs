using NativeLibraries = Electron2D.NativeLibraries;

internal static class NativeLibraryTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows()) return;
        var names = OperatingSystem.IsWindows() ? new[] { "Electron2DTextBreak.dll", "FAudio.dll", "Electron2DENet.dll", "libcrypto-3-Electron2D.dll", "libssl-3-Electron2D.dll", "Electron2DFreeType.dll" } : OperatingSystem.IsMacOS() ? ["libElectron2DTextBreak.dylib", "libFAudio.0.dylib", "libElectron2DENet.dylib", "libElectron2DCrypto.3.dylib", "libElectron2DSSL.3.dylib", "libElectron2DFreeType.dylib"] : ["libElectron2DTextBreak.so", "libFAudio.so.0", "libElectron2DENet.so"];
        foreach (var name in names)
        {
            var file = Path.Combine(AppContext.BaseDirectory, "runtimes", NativeLibraries.RuntimeRID, "native", name);
            if (!File.Exists(file) || File.Exists(Path.Combine(AppContext.BaseDirectory, name)))
                throw new InvalidOperationException($"Project-referenced native library must exist only under runtimes/RID/native: {name}");
        }
        Console.WriteLine("Project-referenced native text/audio/ENet libraries retain their runtime directory without root copies.");
    }
}
