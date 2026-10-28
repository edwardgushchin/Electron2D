using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private int? _rasterSupport;
    private nint CreateGraphicsPipeline(in SDL.GPUGraphicsPipelineCreateInfo info, bool discard, float lineWidth, NativeMultisampleState samples, NativeDepthStencilState depth)
    {
        var outline = info.RasterizerState.FillMode == SDL.GPUFillMode.Line && _context.Driver == "vulkan";
        if (!discard && lineWidth == 1 && !outline && samples.Flags == 0 && depth.Flags == 0) return SDL.CreateGPUGraphicsPipeline(Device, in info);
        if (lineWidth != 1 && !SDL.GetBooleanProperty(SDL.GetGPUDeviceProperties(Device), GPUDeviceFactory.WideLinesProperty, false)) throw new NotSupportedException("Nonunit line width requires native wide-line rasterization.");
        _rasterSupport ??= NativeRasterSupport(Device, 1);
        if (_rasterSupport != 1) throw new NotSupportedException("Raster state requires the native graphics-pipeline adapter.");
        if (depth.Flags != 0) return NativeDepthPipeline(Device, 1, in info, lineWidth, discard ? 1u : 0u, in samples, in depth);
        if (samples.Flags != 0) return NativeMultisamplePipeline(Device, 1, in info, lineWidth, discard ? 1u : 0u, in samples);
        return lineWidth == 1 && !outline ? NativeRasterPipeline(Device, 1, in info) : NativeLinePipeline(Device, 1, in info, lineWidth, discard ? 1u : 0u);
    }
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_line_pipeline")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeLinePipeline(nint device, uint abi, in SDL.GPUGraphicsPipelineCreateInfo info, float lineWidth, uint discard);
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
