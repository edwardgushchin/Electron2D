using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeMultisampleState(uint mask, uint flags, float minimum)
    {
        internal readonly uint Mask = mask, Flags = flags;
        internal readonly float Minimum = minimum;
    }
    private NativeMultisampleState CaptureMultisample(RDPipelineMultisampleState? description, TextureSamples samples)
    {
        if (description is null) return default;
        var masks = description.BorrowMasks;
        if (!masks.IsEmpty && masks.Length != (1 << (int)samples)) throw new ArgumentException("A nonempty sample-mask array must have one entry per raster sample.", nameof(description));
        var flags = (masks.IsEmpty ? 0u : 1u) | (description.EnableSampleShading ? 2u : 0u) | (description.EnableAlphaToOne ? 4u : 0u);
        var enabled = (uint)SDL.GetNumberProperty(SDL.GetGPUDeviceProperties(Device), GPUDeviceFactory.MultisampleProperty, 0);
        if (((flags >> 1) & ~enabled) != 0) throw new NotSupportedException("Sample shading and alpha-to-one require their independently enabled native features.");
        return new(masks.IsEmpty ? uint.MaxValue : masks[0], flags, description.MinSampleShading);
    }
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_multisample_pipeline")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeMultisamplePipeline(nint device, uint abi, in SDL.GPUGraphicsPipelineCreateInfo info, float lineWidth, uint discard, in NativeMultisampleState samples);
}
