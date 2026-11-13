using System.Runtime.InteropServices;
using NativeLibraries = Electron2D.NativeLibraries;

internal static class NativeLibraryTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows()) return;
        Electron2D.NativeAllocation.Prepare();
        var engine = OperatingSystem.IsWindows() ? "libElectron2D.dll" : OperatingSystem.IsMacOS() ? "libElectron2D.dylib" : "libElectron2D.so";
        var names = OperatingSystem.IsWindows()
            ? new[] { engine, "FAudio.dll", "Electron2DFreeType.dll", "libHarfBuzzSharp.dll", "libcrypto-4-Electron2D.dll", "libssl-4-Electron2D.dll" }
            : OperatingSystem.IsMacOS()
                ? [engine, "libFAudio.0.dylib", "libElectron2DFreeType.dylib", "libHarfBuzzSharp.dylib", "libElectron2DCrypto.4.dylib", "libElectron2DSSL.4.dylib"]
                : [engine, "libFAudio.so.0", "libElectron2DFreeType.so", "libHarfBuzzSharp.so", "libcrypto.so.4", "libssl.so.4"];
        foreach (var name in names)
        {
            var file = Path.Combine(AppContext.BaseDirectory, "runtimes", NativeLibraries.RuntimeRID, "native", name);
            if (!File.Exists(file) || File.Exists(Path.Combine(AppContext.BaseDirectory, name)))
                throw new InvalidOperationException($"Project-referenced native library must exist only under runtimes/RID/native: {name}");
            if (name == engine)
            {
                var library = NativeLibrary.Load(file);
                try
                {
                    var exports = new[] { "e2d_text_init", "e2d_mesh_simplify",
                        "e2d_webp_encode", "e2d_webp_decode", "e2d_ktx_create", "e2d_video_create", "e2d_render_memory",
                        "e2d_render_texture_import", "e2d_render_d3d12_texture_import",
                        "e2d_enet_memory", "e2d_file_encode", "e2d_file_zstd_encode", "e2d_file_zstd_decode", "e2d_gzip_create", "e2d_memory_abi", "e2d_memory_allocate",
                        "e2d_memory_reallocate", "e2d_memory_release", "e2d_memory_snapshot" };
                    foreach (var export in exports)
                        if (!NativeLibrary.TryGetExport(library, export, out _))
                            throw new InvalidOperationException($"Native bridge is missing {export}; use the matching native bundle: {file}");
                }
                finally { NativeLibrary.Free(library); }
            }
        }
        Console.WriteLine("The unified native engine and audio retain their runtime directory; all engine subsystem exports are present.");
    }
}
