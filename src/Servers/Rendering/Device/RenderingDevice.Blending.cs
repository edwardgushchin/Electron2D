using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBlendState { internal uint Count; internal fixed uint Factors[32]; }
    private NativeBlendState CaptureBlend(Entry program, FramebufferPassLayout target, RDPipelineColorBlendStateAttachment[] attachments, bool discard)
    {
        var state = new NativeBlendState(); var needed = false; var dual = !discard && program.Outputs.Any(output => output.Index == 1);
        for (var native = 0; native < target.ColorSlots.Length; native++)
        {
            var logical = target.ColorSlots[native]; var attachment = attachments[logical];
            ReadOnlySpan<BlendFactor> factors = [attachment.SrcColorBlendFactor, attachment.DstColorBlendFactor, attachment.SrcAlphaBlendFactor, attachment.DstAlphaBlendFactor];
            for (var i = 0; i < factors.Length; i++)
            {
                var value = factors[i];
                if (value < BlendFactor.Zero || value >= BlendFactor.Max) throw new ArgumentOutOfRangeException(nameof(attachments));
                needed |= value is BlendFactor.ConstantAlpha or BlendFactor.OneMinusConstantAlpha || value >= BlendFactor.Src1Color;
                if (value >= BlendFactor.Src1Color)
                {
                    dual = true;
                    if (!discard && !program.Outputs.Any(output => output.Index == 1 && output.Location == logical)) throw new ArgumentException("Second-source factors require a secondary fragment output for that color slot.", nameof(attachments));
                }
                state.Factors[native * 4 + i] = (uint)value;
            }
        }
        if (!needed && !dual) return default;
        int support; uint maximum;
        try { support = GPUDeviceFactory.ReadBlendSupport(Device, 1, out maximum); }
        catch (EntryPointNotFoundException) { throw new NotSupportedException("Color factors require the native blending adapter."); }
        if ((support & 1) == 0 || support is not (1 or 3)) throw new NotSupportedException("Color factors require the native blending adapter.");
        if (dual && ((support & 2) == 0 || !SDL.GetBooleanProperty(SDL.GetGPUDeviceProperties(Device), GPUDeviceFactory.DualSourceProperty, false) || target.ColorSlots.Length > maximum)) throw new NotSupportedException("Dual-source blending requires its enabled native feature and attachment limit.");
        state.Count = (uint)target.ColorSlots.Length; return state;
    }
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_blend_pipeline")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeBlendPipeline(nint device, uint abi, in SDL.GPUGraphicsPipelineCreateInfo info, float width, uint discard, in NativeMultisampleState samples, in NativeDepthStencilState depth, uint images, uint logic, in NativeBlendState blend);
}
