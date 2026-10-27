using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class ShaderSamplerChecks
{
    private static void BorderDepth(bool linearFilter = false)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var code = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Device", "border-depth.comp.glsl.spv")) };
        var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.D32SFLOAT, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.DepthStencilAttachment };
        using var view = new RDTextureView(); var image = device.TextureCreate(format, view);
        var output = device.StorageBufferCreate(16);
        using var target = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; target.AddID(output); var targets = device.UniformSetCreate([target], shader, 1);
        foreach (var color in new[] { SamplerBorderColor.FloatTransparentBlack, SamplerBorderColor.FloatOpaqueBlack, SamplerBorderColor.FloatOpaqueWhite })
            foreach (var compare in new[] { CompareOperator.Less, CompareOperator.Greater })
            {
                using var state = new RDSamplerState { MinFilter = linearFilter ? SamplerFilter.Linear : SamplerFilter.Nearest, MagFilter = linearFilter ? SamplerFilter.Linear : SamplerFilter.Nearest, EnableCompare = true, CompareOp = compare, BorderColor = color, RepeatU = SamplerRepeatMode.ClampToBorder, RepeatV = SamplerRepeatMode.ClampToBorder };
                var sampler = device.SamplerCreate(state);
                using var source = new RDUniform { Binding = 0, UniformType = UniformType.SamplerWithTexture }; source.AddID(sampler); source.AddID(image); var sources = device.UniformSetCreate([source], shader, 0);
                var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 0); device.ComputeListBindUniformSet(list, targets, 1); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
                var bytes = device.BufferGetData(output); var result = MemoryMarshal.Cast<byte, float>(bytes);
                var expected = (color == SamplerBorderColor.FloatOpaqueWhite) == (compare == CompareOperator.Less) ? 1f : 0f;
                Check(result[0] == expected && result[1] == expected && result[2] == (compare == CompareOperator.Greater ? 1 : 0) && result[3] == 1, "Native border depth comparison preserves its operator and initialized depth.");
                device.FreeRID(sources); device.FreeRID(sampler);
            }
        Console.WriteLine("Native depth border sampling: three floating-point colors, U/V borders and two comparison operators passed.");
    }
}
