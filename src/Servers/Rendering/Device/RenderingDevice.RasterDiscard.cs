using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private int? _rasterSupport;
    private nint CreateGraphicsPipeline(in SDL.GPUGraphicsPipelineCreateInfo info, bool discard)
    {
        if (!discard) return SDL.CreateGPUGraphicsPipeline(Device, in info);
        _rasterSupport ??= NativeRasterSupport(Device, 1);
        if (_rasterSupport != 1) throw new NotSupportedException("Primitive discard requires the native graphics-pipeline adapter.");
        return NativeRasterPipeline(Device, 1, in info);
    }
    // SDL retains two shader objects; this private unused stage never enters a discard pipeline's native stage list.
    private static byte[] EmptyFragmentModule() => MemoryMarshal.AsBytes<uint>([
        0x07230203, 0x00010000, 0, 5, 0,
        0x00020011, 1, 0x0003000e, 0, 1,
        0x0005000f, 4, 1, 0x6e69616d, 0, 0x00030010, 1, 7,
        0x00020013, 2, 0x00030021, 3, 2,
        0x00050036, 2, 1, 0, 3, 0x000200f8, 4, 0x000100fd, 0x00010038
    ]).ToArray();
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_raster_support")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeRasterSupport(nint device, uint abi);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_raster_pipeline")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeRasterPipeline(nint device, uint abi, in SDL.GPUGraphicsPipelineCreateInfo info);
}
