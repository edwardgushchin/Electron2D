using System.Runtime.InteropServices;
using NativeLibraries = Electron2D.NativeLibraries;

internal static class NativeLibraryTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows()) return;
        var names = OperatingSystem.IsWindows() ? new[] { "Electron2DTextBreak.dll", "FAudio.dll", "Electron2DENet.dll", "Electron2DMesh.dll", "Electron2DWebP.dll", "libcrypto-3-Electron2D.dll", "libssl-3-Electron2D.dll", "Electron2DFreeType.dll" } : OperatingSystem.IsMacOS() ? ["libElectron2DTextBreak.dylib", "libFAudio.0.dylib", "libElectron2DENet.dylib", "libElectron2DMesh.dylib", "libElectron2DWebP.dylib", "libElectron2DCrypto.3.dylib", "libElectron2DSSL.3.dylib", "libElectron2DFreeType.dylib"] : ["libElectron2DTextBreak.so", "libFAudio.so.0", "libElectron2DENet.so", "libElectron2DMesh.so", "libElectron2DWebP.so", "libElectron2DFreeType.so"];
        foreach (var name in names)
        {
            var file = Path.Combine(AppContext.BaseDirectory, "runtimes", NativeLibraries.RuntimeRID, "native", name);
            if (!File.Exists(file) || File.Exists(Path.Combine(AppContext.BaseDirectory, name)))
                throw new InvalidOperationException($"Project-referenced native library must exist only under runtimes/RID/native: {name}");
            if (name.Contains("WebP", StringComparison.Ordinal) || name.Contains("FreeType", StringComparison.Ordinal) || name.Contains("ENet", StringComparison.Ordinal))
            {
                var library = NativeLibrary.Load(file);
                try
                {
                    var exports = name.Contains("FreeType", StringComparison.Ordinal) ? new[] { "e2d_ft_hb_memory" } :
                        name.Contains("ENet", StringComparison.Ordinal) ? ["e2d_enet_memory", "e2d_file_encode", "e2d_file_decode", "e2d_gzip_create", "e2d_gzip_process", "e2d_gzip_reset", "e2d_gzip_destroy",
                            "e2d_memory_abi", "e2d_memory_allocate", "e2d_memory_reallocate", "e2d_memory_release", "e2d_memory_snapshot",
                            "e2d_memory_lock", "e2d_memory_unlock", "e2d_memory_managed_locked", "e2d_memory_snapshot_locked"] : ["e2d_webp_encode", "e2d_webp_decode"];
                    foreach (var export in exports)
                        if (!NativeLibrary.TryGetExport(library, export, out _))
                            throw new InvalidOperationException($"Native bridge is missing {export}; use the matching native bundle: {file}");
                }
                finally { NativeLibrary.Free(library); }
            }
        }
        Console.WriteLine("Project-referenced native libraries retain their runtime directory without root copies; WebP, font allocation and ENet/file/GZIP/memory exports are present.");
    }
}
