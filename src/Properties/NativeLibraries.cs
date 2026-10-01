using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static class NativeLibraries
{
    private static readonly Lazy<nint> Sdl = new(() => NativeLibrary.Load("libSDL3.so.0", typeof(NativeLibraries).Assembly, null));

    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "Register the assembly's native resolver before any SDL binding can load a second core library.")]
    internal static void Initialize()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeLibraries).Assembly, (name, assembly, path) =>
        {
            if (name == "HarfBuzzSharp") return NativeLibrary.Load("libHarfBuzzSharp", assembly, path);
            if (!OperatingSystem.IsLinux()) return 0;
            if (name is not ("SDL3" or "SDL3_image" or "SDL3_shadercross" or "FAudio")) return 0;
            // NuGet aliases are separate files. Use the same SONAME as native dependents and load core first,
            // otherwise two SDL copies disagree about the ownership of surfaces, windows and GPU objects.
            var core = Sdl.Value;
            if (name == "FAudio") return NativeLibrary.Load("libFAudio.so.0", assembly, path);
            return name == "SDL3" ? core : 0;
        });
    }
}
