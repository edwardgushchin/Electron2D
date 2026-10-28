using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private int? _logicSupport;
    private uint CaptureLogicOp(RDPipelineColorBlendState state)
    {
        if (!state.EnableLogicOp) return 0;
        if ((_logicSupport ??= NativeLogicSupport(Device, 1)) != 1 || !SDL.GetBooleanProperty(SDL.GetGPUDeviceProperties(Device), GPUDeviceFactory.LogicOpProperty, false)) throw new NotSupportedException("Logical color operations require their enabled native feature and pipeline adapter.");
        return (uint)state.LogicOp + 1;
    }
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_logic_support")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeLogicSupport(nint device, uint abi);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_logic_pipeline")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeLogicPipeline(nint device, uint abi, in SDL.GPUGraphicsPipelineCreateInfo info, float lineWidth, uint discard, in NativeMultisampleState samples, in NativeDepthStencilState depth, uint images, uint logic);
}
