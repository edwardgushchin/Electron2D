using SDL3;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class RenderFormatSupport
{
    private const int Count = (int)SDL.GPUTextureFormat.ASTC12X12Float + 1;
    private readonly byte[] _formats = new byte[Count];
    private readonly nint _device;

    internal RenderFormatSupport(nint device)
    {
        _device = device;
        fixed (byte* output = _formats)
        {
            var result = ReadNative(device, 1, output, Count);
            if (result == 0) throw new NotSupportedException("Format filter queries require the pinned SDL 3.4.18 Vulkan adapter.");
            if (result != 1) throw new InvalidOperationException("The native device could not report format filter support.");
        }
    }

    internal bool Supports(SDL.GPUTextureFormat format, RenderingDevice.SamplerFilter filter)
    {
        RDTextureConfiguration.Filter(filter);
        var bit = filter == RenderingDevice.SamplerFilter.Linear ? 2 : 1;
        return (uint)format < _formats.Length && (_formats[(int)format] & bit) != 0;
    }

    internal void Require(nint texture, bool linear, bool comparison = false)
    {
        var format = TextureFormat(_device, 1, texture);
        if (format <= 0) throw new NotSupportedException("The native texture has no compatible format-query adapter.");
        if (!Supports((SDL.GPUTextureFormat)format, RenderingDevice.SamplerFilter.Nearest) ||
            linear && !comparison && !Supports((SDL.GPUTextureFormat)format, RenderingDevice.SamplerFilter.Linear))
            throw new ArgumentException("The native texture format does not support the configured sampler filter.");
    }

    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_filter_support")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int ReadNative(nint device, uint abi, byte* output, uint count);

    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_texture_format")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int TextureFormat(nint device, uint abi, nint texture);
}
