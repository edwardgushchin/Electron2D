using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal static unsafe partial class GPUTextureViews
{
    private const string Cache = "Electron2D.render.texture_views";
    private static bool Active(nint device) => SDL.GetPointerProperty(SDL.GetGPUDeviceProperties(device), Cache, 0) != 0;
    internal static void CollectIfActive(nint device) { if (Active(device)) Collect(device, 1); }
    internal static SDL.GPUTextureRegion Region(nint device, SDL.GPUTextureRegion region)
    {
        if (Active(device)) { region.Texture = NativeRegion(device, 1, region.Texture, ref region.Layer, ref region.MipLevel); if (region.Texture == 0) throw GPUPhysicsDevice.Failure("resolve texture-view region"); }
        return region;
    }
    internal static SDL.GPUTextureLocation Location(nint device, SDL.GPUTextureLocation location)
    {
        if (Active(device)) { location.Texture = NativeRegion(device, 1, location.Texture, ref location.Layer, ref location.MipLevel); if (location.Texture == 0) throw GPUPhysicsDevice.Failure("resolve texture-view location"); }
        return location;
    }
    internal static void Copy(nint device, nint pass, SDL.GPUTextureLocation source, SDL.GPUTextureLocation destination, uint width, uint height, uint depth, bool cycle) =>
        SDL.CopyGPUTextureToTexture(pass, Location(device, source), Location(device, destination), width, height, depth, cycle);
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct TextureRange(nint texture, uint layers, uint levels, uint level = 0)
    {
        internal readonly nint Texture = texture;
        internal readonly uint Layer = 0, Layers = layers, Level = level, Levels = levels;
    }
    internal static bool Overlap(nint device, nint a, uint aLayers, uint aLevels, nint b, uint bLayers, uint bLevels, uint bLevel = 0)
    {
        if (a == b) return true;
        if (!Active(device)) return false;
        var x = new TextureRange(a, aLayers, aLevels); var y = new TextureRange(b, bLayers, bLevels, bLevel);
        var result = NativeOverlap(device, 1, in x, in y); if (result < 0) throw GPUPhysicsDevice.Failure("compare texture-view ranges"); return result != 0;
    }
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_view_support")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int Support(nint device, uint abi);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_view_format_support")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int FormatSupport(nint device, uint abi, in SDL.GPUTextureCreateInfo info, SDL.GPUTextureFormat format);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_texture_create")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial nint Create(nint device, uint abi, in SDL.GPUTextureCreateInfo info, nint formats, uint count);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_view_create")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial nint View(nint device, uint abi, nint source, SDL.GPUTextureFormat format, uint layer, uint layers, uint level, uint levels, uint array);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_view_region")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeRegion(nint device, uint abi, nint texture, ref uint layer, ref uint level);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_view_overlap")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeOverlap(nint device, uint abi, in TextureRange a, in TextureRange b);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_view_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void Destroy(nint device, uint abi, nint texture);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_views_collect")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void Collect(nint device, uint abi);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_views_release")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void Release(nint device, uint abi);
}
