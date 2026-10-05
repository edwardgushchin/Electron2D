using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static class NativeLibraries
{
    private static readonly Lazy<nint> Sdl = new(() => NativeLibrary.Load(OperatingSystem.IsMacOS() ? "libSDL3.0.dylib" : "libSDL3.so.0", typeof(NativeLibraries).Assembly, null));

    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "Register the assembly's native resolver before any SDL binding can load a second core library.")]
    internal static void Initialize()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeLibraries).Assembly, (name, assembly, path) =>
        {
            if (name == "HarfBuzzSharp") return NativeLibrary.Load("libHarfBuzzSharp", assembly, path);
            if (OperatingSystem.IsMacOS())
            {
                if (name == "Electron2DCrypto") return LoadRuntime("libElectron2DCrypto.3.dylib", assembly, path);
                if (name == "Electron2DSSL") return LoadRuntime("libElectron2DSSL.3.dylib", assembly, path);
                if (name == "Electron2DENet") return LoadRuntime("libElectron2DENet.dylib", assembly, path);
                if (name == "Electron2DTextBreak") return LoadRuntime("libElectron2DTextBreak.dylib", assembly, path);
                if (name is not ("SDL3" or "SDL3_image" or "SDL3_shadercross" or "FAudio")) return 0;
                var macCore = Sdl.Value;
                return name == "SDL3" ? macCore : name == "FAudio" ? LoadRuntime("libFAudio.0.dylib", assembly, path) : 0;
            }
            if (!OperatingSystem.IsLinux()) return 0;
            if (name == "Electron2DCrypto") return NativeLibrary.Load("libcrypto.so.3", assembly, path);
            if (name == "Electron2DSSL") return NativeLibrary.Load("libssl.so.3", assembly, path);
            if (name == "Electron2DENet") return LoadRuntime("libElectron2DENet.so", assembly, path);
            if (name == "Electron2DTextBreak") return LoadRuntime("libElectron2DTextBreak.so", assembly, path);
            if (name is not ("SDL3" or "SDL3_image" or "SDL3_shadercross" or "FAudio")) return 0;
            // NuGet aliases are separate files. Use the same SONAME as native dependents and load core first,
            // otherwise two SDL copies disagree about the ownership of surfaces, windows and GPU objects.
            var core = Sdl.Value;
            if (name == "FAudio") return LoadRuntime("libFAudio.so.0", assembly, path);
            return name == "SDL3" ? core : 0;
        });
    }

    private static nint LoadRuntime(string name, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        var file = System.IO.Path.Combine(AppContext.BaseDirectory,
            "runtimes", RuntimeInformation.RuntimeIdentifier, "native", name);
        // Project references preserve the runtime directory; NuGet RID publishes can flatten native assets.
        return NativeLibrary.Load(File.Exists(file) ? file : name, assembly, searchPath);
    }
}
