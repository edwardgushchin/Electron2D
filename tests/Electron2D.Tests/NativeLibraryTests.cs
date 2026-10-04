using System.Runtime.InteropServices;

internal static class NativeLibraryTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsLinux()) return;
        foreach (var name in new[] { "libElectron2DTextBreak.so", "libFAudio.so.0", "libElectron2DENet.so" })
        {
            var file = Path.Combine(AppContext.BaseDirectory, "runtimes", RuntimeInformation.RuntimeIdentifier, "native", name);
            if (!File.Exists(file) || File.Exists(Path.Combine(AppContext.BaseDirectory, name)))
                throw new InvalidOperationException($"Project-referenced native library must exist only under runtimes/RID/native: {name}");
        }
        Console.WriteLine("Project-referenced native text/audio/ENet libraries retain their runtime directory without root copies.");
    }
}
