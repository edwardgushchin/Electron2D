using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class ShaderSamplerChecks
{
    private static readonly float[] MirrorPoints = [-3, -1.25f, -.875f, -.625f, -.375f, -.125f, 0, .125f, .375f, .625f, .875f, 1, 1.125f, 1.625f, 2.25f, 3.25f];

    private static void MirrorClamp(bool unavailable)
    {
        var cases = 0;
        foreach (var name in new[] { "float", "signed", "unsigned", "pairs", "array" })
        {
            using var device = RenderingServer.CreateLocalRenderingDevice();
            using var code = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Device", $"mirror-{name}.comp.glsl.spv")) };
            var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader);
            var integer = name is "signed" or "unsigned"; var sign = name == "signed" ? -1 : 1; var layers = name == "array" ? 2 : 1;
            using var format = new RDTextureFormat { Width = 4, Height = 4, MIPMaps = 2, ArrayLayers = (uint)layers, TextureType = layers == 2 ? TextureType.TextureArray : TextureType.Texture, Format = integer ? sign < 0 ? DataFormat.R32G32B32A32SINT : DataFormat.R32G32B32A32UINT : DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling };
            using var view = new RDTextureView();
            var bytes = new byte[layers][];
            for (var layer = 0; layer < layers; layer++)
            {
                bytes[layer] = new byte[320]; var offset = 0;
                for (var mip = 0; mip < 2; mip++)
                {
                    var width = 4 >> mip;
                    for (var y = 0; y < width; y++) for (var x = 0; x < width; x++) for (var lane = 0; lane < 4; lane++)
                            {
                                var value = sign * ((layer + 1) * 100 + mip * 20 + y * width + x + lane);
                                if (integer) MemoryMarshal.Cast<byte, int>(bytes[layer].AsSpan())[offset++] = value;
                                else MemoryMarshal.Cast<byte, float>(bytes[layer].AsSpan())[offset++] = value;
                            }
                }
            }
            var texture = device.TextureCreate(format, view, bytes); var output = device.StorageBufferCreate(512); var readback = new byte[512];
            using var target = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; target.AddID(output);
            var targets = device.UniformSetCreate([target], shader, 1);
            foreach (var companion in new[] { SamplerRepeatMode.ClampToEdge, SamplerRepeatMode.Repeat, SamplerRepeatMode.MirrorClampToEdge, SamplerRepeatMode.ClampToBorder })
                foreach (var swap in new[] { false, true })
                    foreach (var linear in integer ? new[] { false } : new[] { false, true })
                    {
                        var u = swap ? companion : SamplerRepeatMode.MirrorClampToEdge; var v = swap ? SamplerRepeatMode.MirrorClampToEdge : companion;
                        using var state = new RDSamplerState { RepeatU = u, RepeatV = v, MinFilter = linear ? SamplerFilter.Linear : SamplerFilter.Nearest, MagFilter = linear ? SamplerFilter.Linear : SamplerFilter.Nearest, BorderColor = integer ? SamplerBorderColor.IntOpaqueWhite : SamplerBorderColor.FloatOpaqueWhite };
                        if (unavailable)
                        {
                            Reject<NotSupportedException>(() => device.SamplerCreate(state));
                            u = v = SamplerRepeatMode.ClampToEdge; state.RepeatU = u; state.RepeatV = v;
                        }
                        var sampler = device.SamplerCreate(state); state.RepeatU = SamplerRepeatMode.Repeat; state.Dispose();
                        using var image = new RDUniform { Binding = 0, UniformType = name == "pairs" ? UniformType.Texture : UniformType.SamplerWithTexture };
                        if (name != "pairs") image.AddID(sampler); image.AddID(texture);
                        using var point = new RDUniform { Binding = 1, UniformType = UniformType.Sampler }; point.AddID(sampler);
                        var sources = device.UniformSetCreate(name == "pairs" ? [image, point] : [image], shader, 0);
                        void Dispatch()
                        {
                            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 0); device.ComputeListBindUniformSet(list, targets, 1); device.ComputeListDispatch(list, 32, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, readback.AsSpan());
                        }
                        Dispatch(); var actual = MemoryMarshal.Cast<byte, float>(readback.AsSpan());
                        for (var mip = 0; mip < 2; mip++) for (var sample = 0; sample < 16; sample++) for (var lane = 0; lane < 4; lane++)
                                {
                                    var width = 4 >> mip; var s = MirrorPoints[sample] * width - (linear ? .5f : 0); var t = MirrorPoints[(sample * 5 + 3) % 16] * width - (linear ? .5f : 0);
                                    var x = (int)MathF.Floor(s); var y = (int)MathF.Floor(t); var fx = linear ? s - x : 0; var fy = linear ? t - y : 0;
                                    float Pixel(int px, int py)
                                    {
                                        px = MirrorIndex(px, width, u); py = MirrorIndex(py, width, v);
                                        return px < 0 || py < 0 ? 1 : sign * (layers * 100 + mip * 20 + py * width + px + lane);
                                    }
                                    var expected = (Pixel(x, y) * (1 - fx) + Pixel(x + 1, y) * fx) * (1 - fy) + (Pixel(x, y + 1) * (1 - fx) + Pixel(x + 1, y + 1) * fx) * fy;
                                    Check(MathF.Abs(actual[(mip * 16 + sample) * 4 + lane] - expected) < .001f, $"Mirror {name}/{u}/{v}/{linear}, mip={mip}, sample={sample}, lane={lane}: {actual[(mip * 16 + sample) * 4 + lane]} != {expected}.");
                                }
                        for (var i = 0; i < 8; i++) Dispatch();
                        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 8; i++) Dispatch();
                        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared mirror sampling allocates zero owner-thread managed bytes.");
                        device.FreeRID(sources); device.FreeRID(sampler); cases++;
                    }
        }
        Console.WriteLine($"PASS mirror sampler: {cases} cases, two mips, combined/separate/array, float/signed/unsigned, both axes, nearest/linear and zero prepared owner managed bytes; unavailable={unavailable}.");
    }

    private static int MirrorIndex(int index, int width, SamplerRepeatMode mode) => mode switch
    {
        SamplerRepeatMode.Repeat => (index % width + width) % width,
        SamplerRepeatMode.MirrorClampToEdge => Math.Clamp(index < 0 ? -index - 1 : index, 0, width - 1),
        SamplerRepeatMode.ClampToBorder => index < 0 || index >= width ? -1 : index,
        _ => Math.Clamp(index, 0, width - 1)
    };
}
