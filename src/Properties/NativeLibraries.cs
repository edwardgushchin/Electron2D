using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static class NativeLibraries
{
#if IOS || TVOS
    // Static native references are linked into the Apple application executable.
    internal const string SDLLibrary = "__Internal", SDLImageLibrary = "__Internal", SDLShaderCrossLibrary = "__Internal",
        AudioLibrary = "__Internal", FreeTypeLibrary = "__Internal", HarfBuzzLibrary = "__Internal",
        SSLLibrary = "__Internal", CryptoLibrary = "__Internal", ENetLibrary = "__Internal", TextBreakLibrary = "__Internal";
#else
    internal const string SDLLibrary = "SDL3", SDLImageLibrary = "SDL3_image", SDLShaderCrossLibrary = "SDL3_shadercross",
        AudioLibrary = "FAudio", FreeTypeLibrary = "freetype", HarfBuzzLibrary = "HarfBuzzSharp",
        SSLLibrary = "Electron2DSSL", CryptoLibrary = "Electron2DCrypto", ENetLibrary = "Electron2DENet", TextBreakLibrary = "Electron2DTextBreak";
#endif
    private static readonly Lazy<nint> Sdl = new(() => LoadRuntime(OperatingSystem.IsWindows() ? "SDL3.dll" : OperatingSystem.IsAndroid() ? "libSDL3.so" : OperatingSystem.IsMacOS() ? "libSDL3.0.dylib" : "libSDL3.so.0", typeof(NativeLibraries).Assembly, null));

    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "Register the assembly's native resolver before any SDL binding can load a second core library.")]
    internal static void Initialize()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()) return;
        NativeLibrary.SetDllImportResolver(typeof(NativeLibraries).Assembly, (name, assembly, path) =>
        {
            if (OperatingSystem.IsAndroid())
            {
                if (name == "HarfBuzzSharp") return NativeLibrary.Load("libElectron2DHarfBuzz.so", assembly, path);
                if (name == "freetype") return NativeLibrary.Load("libElectron2DFreeType.so", assembly, path);
                if (name == "Electron2DSSL")
                {
                    NativeLibrary.Load("libElectron2DCrypto.so", assembly, path);
                    return NativeLibrary.Load("libElectron2DSSL.so", assembly, path);
                }
                if (name is "Electron2DCrypto" or "Electron2DENet" or "Electron2DTextBreak")
                    return NativeLibrary.Load("lib" + name + ".so", assembly, path);
                if (name is not ("SDL3" or "SDL3_image" or "SDL3_shadercross" or "FAudio")) return 0;
                var androidCore = Sdl.Value;
                return name == "SDL3" ? androidCore : name == "FAudio" ? NativeLibrary.Load("libFAudio.so", assembly, path) : 0;
            }
            if (OperatingSystem.IsWindows())
            {
                if (name == "HarfBuzzSharp") return LoadRuntime("libHarfBuzzSharp.dll", assembly, path);
                if (name == "freetype") return LoadRuntime("Electron2DFreeType.dll", assembly, path);
                if (name == "Electron2DCrypto") return LoadRuntime("libcrypto-3-Electron2D.dll", assembly, path);
                if (name == "Electron2DSSL")
                {
                    LoadRuntime("libcrypto-3-Electron2D.dll", assembly, path);
                    return LoadRuntime("libssl-3-Electron2D.dll", assembly, path);
                }
                if (name == "Electron2DENet") return LoadRuntime("Electron2DENet.dll", assembly, path);
                if (name == "Electron2DTextBreak") return LoadRuntime("Electron2DTextBreak.dll", assembly, path);
                if (name is not ("SDL3" or "SDL3_image" or "SDL3_shadercross" or "FAudio")) return 0;
                var windowsCore = Sdl.Value;
                return name switch
                {
                    "SDL3" => windowsCore,
                    "SDL3_image" => LoadRuntime("SDL3_image.dll", assembly, path),
                    "SDL3_shadercross" => LoadRuntime("SDL3_shadercross.dll", assembly, path),
                    _ => LoadRuntime("FAudio.dll", assembly, path)
                };
            }
            if (name == "HarfBuzzSharp") return LoadRuntime(OperatingSystem.IsMacOS() ? "libHarfBuzzSharp.dylib" : "libHarfBuzzSharp.so", assembly, path);
            if (OperatingSystem.IsMacOS())
            {
                if (name == "freetype") return LoadRuntime("libElectron2DFreeType.dylib", assembly, path);
                if (name == "Electron2DCrypto") return LoadRuntime("libElectron2DCrypto.3.dylib", assembly, path);
                if (name == "Electron2DSSL") return LoadRuntime("libElectron2DSSL.3.dylib", assembly, path);
                if (name == "Electron2DENet") return LoadRuntime("libElectron2DENet.dylib", assembly, path);
                if (name == "Electron2DTextBreak") return LoadRuntime("libElectron2DTextBreak.dylib", assembly, path);
                if (name is not ("SDL3" or "SDL3_image" or "SDL3_shadercross" or "FAudio")) return 0;
                var macCore = Sdl.Value;
                return name switch
                {
                    "SDL3" => macCore,
                    "SDL3_image" => LoadRuntime("libSDL3_image.0.dylib", assembly, path),
                    "SDL3_shadercross" => LoadRuntime("libSDL3_shadercross.0.dylib", assembly, path),
                    _ => LoadRuntime("libFAudio.0.dylib", assembly, path)
                };
            }
            if (!OperatingSystem.IsLinux()) return 0;
            if (name == "freetype") return LoadRuntime("libfreetype.so", assembly, path);
            if (name == "Electron2DCrypto") return NativeLibrary.Load("libcrypto.so.3", assembly, path);
            if (name == "Electron2DSSL") return NativeLibrary.Load("libssl.so.3", assembly, path);
            if (name == "Electron2DENet") return LoadRuntime("libElectron2DENet.so", assembly, path);
            if (name == "Electron2DTextBreak") return LoadRuntime("libElectron2DTextBreak.so", assembly, path);
            if (name is not ("SDL3" or "SDL3_image" or "SDL3_shadercross" or "FAudio")) return 0;
            // NuGet aliases are separate files. Use the same SONAME as native dependents and load core first,
            // otherwise two SDL copies disagree about the ownership of surfaces, windows and GPU objects.
            var core = Sdl.Value;
            return name switch
            {
                "SDL3" => core,
                "SDL3_image" => LoadRuntime("libSDL3_image.so.0", assembly, path),
                "SDL3_shadercross" => LoadRuntime("libSDL3_shadercross.so.0", assembly, path),
                _ => LoadRuntime("libFAudio.so.0", assembly, path)
            };
        });
    }

    private static nint LoadRuntime(string name, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        var file = System.IO.Path.Combine(AppContext.BaseDirectory,
            "runtimes", RuntimeInformation.RuntimeIdentifier, "native", name);
        // Platform packages preserve this directory; ordinary resolution also supports older flat layouts.
        return NativeLibrary.Load(File.Exists(file) ? file : name, assembly, searchPath);
    }
}
