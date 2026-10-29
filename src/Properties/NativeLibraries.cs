using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static partial class NativeLibraries
{
#if IOS || TVOS
    // Static native references are linked into the application executable.
    internal const string SDLLibrary = "__Internal", SDLImageLibrary = "__Internal", SDLShaderCrossLibrary = "__Internal",
        AudioLibrary = "__Internal", FreeTypeLibrary = "__Internal", HarfBuzzLibrary = "__Internal",
        SSLLibrary = "__Internal", CryptoLibrary = "__Internal", ENetLibrary = "__Internal", WebRTCLibrary = "__Internal", TextBreakLibrary = "__Internal", WebPLibrary = "__Internal", OGVLibrary = "__Internal", KTXLibrary = "__Internal", MeshLibrary = "__Internal", RenderLibrary = "__Internal";
#elif ELECTRON2D_BROWSER_NATIVE
    // Mono/WASM resolves the archive modules through generated P/Invoke tables, not dlopen(self).
    internal const string SDLLibrary = "libSDL3", SDLImageLibrary = "libSDL3_image", SDLShaderCrossLibrary = "libSDL3_shadercross",
        AudioLibrary = "libFAudio", FreeTypeLibrary = "libElectron2DFreeType", HarfBuzzLibrary = "libElectron2DHarfBuzz",
        SSLLibrary = "libElectron2DSSL", CryptoLibrary = "libElectron2DCrypto", ENetLibrary = "libElectron2DENet", WebRTCLibrary = "libElectron2DWebRTC", TextBreakLibrary = "libElectron2DTextBreak", WebPLibrary = "libElectron2DWebP", KTXLibrary = "libElectron2DKTX", OGVLibrary = "libElectron2DOGV", MeshLibrary = "libElectron2DMesh", RenderLibrary = "libElectron2DRender";
#else
    internal const string SDLLibrary = "SDL3", SDLImageLibrary = "SDL3_image", SDLShaderCrossLibrary = "SDL3_shadercross",
        AudioLibrary = "FAudio", FreeTypeLibrary = "freetype", HarfBuzzLibrary = "HarfBuzzSharp",
        SSLLibrary = "Electron2DSSL", CryptoLibrary = "Electron2DCrypto", ENetLibrary = "Electron2DENet", WebRTCLibrary = "Electron2DWebRTC", TextBreakLibrary = "Electron2DTextBreak", WebPLibrary = "Electron2DWebP", KTXLibrary = "Electron2DKTX", OGVLibrary = "Electron2DOGV", MeshLibrary = "Electron2DMesh", RenderLibrary = "Electron2DRender";
#endif
    // Distribution runtimes may report arch-x64 or another nonportable RID.
    private static readonly string? DesktopPlatform = OperatingSystem.IsWindows() ? "win" :
        OperatingSystem.IsMacOS() ? "osx" : OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid() ? "linux" : null;
    internal static string RuntimeRID { get; } = DesktopPlatform is null ? RuntimeInformation.RuntimeIdentifier :
        DesktopPlatform + "-" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
    private static readonly Lazy<nint> Sdl = new(LoadSDLCore);
    private static nint LoadSDLCore()
    {
        // Original native allocators must resolve before SDL can initialize any service.
        NativeAllocation.Prepare();
        var handle = LoadRuntime(OperatingSystem.IsWindows() ? "SDL3.dll" : OperatingSystem.IsAndroid() ? "libSDL3.so" : OperatingSystem.IsMacOS() ? "libSDL3.0.dylib" : "libSDL3.so.0", typeof(NativeLibraries).Assembly, null);
        RuntimeDiagnostics.Instance.SDLAvailable(handle);
        return handle;
    }

    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "Register the assembly's native resolver before any SDL binding can load a second core library.")]
    internal static void Initialize()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsBrowser()) return;
        NativeLibrary.SetDllImportResolver(typeof(NativeLibraries).Assembly, (name, assembly, path) =>
        {
            if (name == "spirv-cross-c-shared")
            {
                if (OperatingSystem.IsAndroid()) return NativeLibrary.Load("libspirv-cross-c-shared.so", assembly, path);
                if (OperatingSystem.IsWindows()) return LoadRuntime("spirv-cross-c-shared.dll", assembly, path);
                if (OperatingSystem.IsMacOS()) return LoadRuntime("libspirv-cross-c-shared.0.dylib", assembly, path);
                if (OperatingSystem.IsLinux()) return LoadRuntime("libspirv-cross-c-shared.so.0", assembly, path);
                return 0;
            }
            if (OperatingSystem.IsAndroid())
            {
                if (name == "HarfBuzzSharp") return NativeLibrary.Load("libElectron2DHarfBuzz.so", assembly, path);
                if (name == "freetype") return NativeLibrary.Load("libElectron2DFreeType.so", assembly, path);
                if (name == "Electron2DSSL")
                {
                    NativeLibrary.Load("libElectron2DCrypto.so", assembly, path);
                    return NativeLibrary.Load("libElectron2DSSL.so", assembly, path);
                }
                if (name is "Electron2DCrypto" or "Electron2DENet" or "Electron2DWebRTC" or "Electron2DTextBreak" or "Electron2DMesh" or "Electron2DRender" or "Electron2DWebP" or "Electron2DOGV" or "Electron2DKTX")
                    return NativeLibrary.Load("lib" + name + ".so", assembly, path);
                if (name is not ("SDL3" or "SDL3_image" or "SDL3_shadercross" or "FAudio")) return 0;
                var androidCore = Sdl.Value;
                return name == "SDL3" ? androidCore : name == "FAudio" ? NativeLibrary.Load("libFAudio.so", assembly, path) : 0;
            }
            if (OperatingSystem.IsWindows())
            {
                if (name == "HarfBuzzSharp") return LoadRuntime("libHarfBuzzSharp.dll", assembly, path);
                if (name == "freetype") return LoadRuntime("Electron2DFreeType.dll", assembly, path);
                if (name == "Electron2DCrypto") return LoadRuntime("libcrypto-4-Electron2D.dll", assembly, path);
                if (name == "Electron2DSSL")
                {
                    LoadRuntime("libcrypto-4-Electron2D.dll", assembly, path);
                    return LoadRuntime("libssl-4-Electron2D.dll", assembly, path);
                }
                if (name == "Electron2DENet") return LoadRuntime("Electron2DENet.dll", assembly, path);
                if (name == "Electron2DWebRTC") return LoadRuntime("Electron2DWebRTC.dll", assembly, path);
                if (name == "Electron2DTextBreak") return LoadRuntime("Electron2DTextBreak.dll", assembly, path);
                if (name == "Electron2DMesh") return LoadRuntime("Electron2DMesh.dll", assembly, path);
                if (name == "Electron2DRender") return LoadRuntime("Electron2DRender.dll", assembly, path);
                if (name == "Electron2DWebP") return LoadRuntime("Electron2DWebP.dll", assembly, path);
                if (name == "Electron2DKTX") return LoadRuntime("Electron2DKTX.dll", assembly, path);
                if (name == "Electron2DOGV") return LoadRuntime("Electron2DOGV.dll", assembly, path);
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
                if (name == "Electron2DCrypto") return LoadRuntime("libElectron2DCrypto.4.dylib", assembly, path);
                if (name == "Electron2DSSL") return LoadRuntime("libElectron2DSSL.4.dylib", assembly, path);
                if (name == "Electron2DENet") return LoadRuntime("libElectron2DENet.dylib", assembly, path);
                if (name == "Electron2DWebRTC") return LoadRuntime("libElectron2DWebRTC.dylib", assembly, path);
                if (name == "Electron2DTextBreak") return LoadRuntime("libElectron2DTextBreak.dylib", assembly, path);
                if (name == "Electron2DMesh") return LoadRuntime("libElectron2DMesh.dylib", assembly, path);
                if (name == "Electron2DRender") return LoadRuntime("libElectron2DRender.dylib", assembly, path);
                if (name == "Electron2DWebP") return LoadRuntime("libElectron2DWebP.dylib", assembly, path);
                if (name == "Electron2DKTX") return LoadRuntime("libElectron2DKTX.dylib", assembly, path);
                if (name == "Electron2DOGV") return LoadRuntime("libElectron2DOGV.dylib", assembly, path);
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
            if (name == "freetype") return LoadRuntime("libElectron2DFreeType.so", assembly, path);
            if (name == "Electron2DCrypto") return LoadRuntime("libcrypto.so.4", assembly, path);
            if (name == "Electron2DSSL")
            {
                LoadRuntime("libcrypto.so.4", assembly, path);
                return LoadRuntime("libssl.so.4", assembly, path);
            }
            if (name == "Electron2DENet") return LoadRuntime("libElectron2DENet.so", assembly, path);
            if (name == "Electron2DWebRTC") return LoadRuntime("libElectron2DWebRTC.so", assembly, path);
            if (name == "Electron2DTextBreak") return LoadRuntime("libElectron2DTextBreak.so", assembly, path);
            if (name == "Electron2DMesh") return LoadRuntime("libElectron2DMesh.so", assembly, path);
            if (name == "Electron2DRender") return LoadRuntime("libElectron2DRender.so", assembly, path);
            if (name == "Electron2DWebP") return LoadRuntime("libElectron2DWebP.so", assembly, path);
            if (name == "Electron2DKTX") return LoadRuntime("libElectron2DKTX.so", assembly, path);
            if (name == "Electron2DOGV") return LoadRuntime("libElectron2DOGV.so", assembly, path);
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
            "runtimes", RuntimeRID, "native", name);
#if !IOS && !TVOS && !ANDROID && !ELECTRON2D_BROWSER_NATIVE
        // Keep packaged font implementations isolated from Fontconfig dependency symbols on Linux.
        if (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid() && name is "libElectron2DFreeType.so" or "libHarfBuzzSharp.so")
        {
            var handle = DLOpen(File.Exists(file) ? file : name, 2 | 8); // NOW, LOCAL and DEEPBIND.
            if (handle == 0) throw new DllNotFoundException(Marshal.PtrToStringUTF8(DLError()) ?? "Font library could not be loaded.");
            return handle;
        }
#endif
        // Platform packages preserve this directory; ordinary resolution also supports older flat layouts.
        return NativeLibrary.Load(File.Exists(file) ? file : name, assembly, searchPath);
    }
#if !IOS && !TVOS && !ANDROID && !ELECTRON2D_BROWSER_NATIVE
    [LibraryImport("libdl.so.2", EntryPoint = "dlopen", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint DLOpen(string path, int flags);
    [LibraryImport("libdl.so.2", EntryPoint = "dlerror"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint DLError();
#endif
}
