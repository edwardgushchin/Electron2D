using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class ShaderSamplerChecks
{
    private static void Borders()
    {
        var cases = 0;
        foreach (var name in new[] { "float", "signed", "unsigned", "pairs", "array" })
        {
            using var device = RenderingServer.CreateLocalRenderingDevice();
            using var other = RenderingServer.CreateLocalRenderingDevice();
            using var code = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Device", $"border-{name}.comp.glsl.spv")) };
            var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader);
            var integer = name is "signed" or "unsigned";
            using var format = new RDTextureFormat { Width = 2, Height = 2, MIPMaps = 2, TextureType = name == "array" ? TextureType.TextureArray : TextureType.Texture, ArrayLayers = name == "array" ? 2u : 1u, Format = integer ? name == "signed" ? DataFormat.R32G32B32A32SINT : DataFormat.R32G32B32A32UINT : DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling };
            using var view = new RDTextureView();
            var values = new float[] { .25f, .5f, .75f, 1, .5f, .25f, 1, .75f };
            var numbers = new int[] { 2, 3, 4, 5, 6, 7, 8, 9 };
            if (name == "signed") for (var n = 0; n < numbers.Length; n++) numbers[n] = -numbers[n];
            var bytes = new byte[80];
            for (var pixel = 0; pixel < 5; pixel++)
            {
                var source = pixel == 4 ? 4 : 0;
                if (integer) MemoryMarshal.AsBytes(numbers.AsSpan(source, 4)).CopyTo(bytes.AsSpan(pixel * 16));
                else MemoryMarshal.AsBytes(values.AsSpan(source, 4)).CopyTo(bytes.AsSpan(pixel * 16));
            }
            var texture = device.TextureCreate(format, view, name == "array" ? [new byte[80], bytes] : [bytes]);
            var output = device.StorageBufferCreate(256);
            using var target = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; target.AddID(output);
            var targets = device.UniformSetCreate([target], shader, 1);
            var data = new byte[256];
            foreach (var border in Enum.GetValues<SamplerBorderColor>().Where(c => c != SamplerBorderColor.Max && (((int)c & 1) != 0) == integer))
                foreach (var axes in new[] { 1, 2, 3 })
                    foreach (var linear in integer ? new[] { false } : new[] { false, true })
                    {
                        using var state = new RDSamplerState { BorderColor = border, RepeatU = (axes & 1) != 0 ? SamplerRepeatMode.ClampToBorder : SamplerRepeatMode.ClampToEdge, RepeatV = (axes & 2) != 0 ? SamplerRepeatMode.ClampToBorder : SamplerRepeatMode.Repeat, MinFilter = linear ? SamplerFilter.Linear : SamplerFilter.Nearest, MagFilter = linear ? SamplerFilter.Linear : SamplerFilter.Nearest };
                        var sampler = device.SamplerCreate(state);
                        using var image = new RDUniform { Binding = 0, UniformType = name == "pairs" ? UniformType.Texture : UniformType.SamplerWithTexture };
                        if (name != "pairs") image.AddID(sampler); image.AddID(texture);
                        using var point = new RDUniform { Binding = 1, UniformType = UniformType.Sampler }; point.AddID(sampler);
                        var sources = device.UniformSetCreate(name == "pairs" ? [image, point] : [image], shader, 0);
                        void Dispatch()
                        {
                            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 0); device.ComputeListBindUniformSet(list, targets, 1); device.ComputeListDispatch(list, 16, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, data.AsSpan());
                        }
                        state.BorderColor = integer ? SamplerBorderColor.IntOpaqueBlack : SamplerBorderColor.FloatOpaqueBlack;
                        state.Dispose();
                        Dispatch();
                        var actual = MemoryMarshal.Cast<byte, float>(data.AsSpan());
                        for (var level = 0; level < 2; level++)
                            for (var sample = 0; sample < 8; sample++)
                                for (var lane = 0; lane < 4; lane++)
                                {
                                    var source = integer ? numbers[level * 4 + lane] : values[level * 4 + lane];
                                    var color = (int)border / 2 == 2 ? 1 : lane == 3 && (int)border / 2 == 1 ? 1 : 0;
                                    var outside = (axes & 1) != 0 && sample is 0 or 1 or 6 || (axes & 2) != 0 && sample is 2 or 3 or 6;
                                    var blend = linear && ((axes & 1) != 0 && sample == 5 || (axes & 2) != 0 && sample == 7);
                                    var expected = outside ? color : blend ? (source + color) / 2f : source;
                                    Check(MathF.Abs(actual[level * 32 + sample * 4 + lane] - expected) < .0001f, $"{name}/{border}/{axes}/{linear}, mip {level}, sample {sample}, lane {lane}: {actual[level * 32 + sample * 4 + lane]}, expected {expected}.");
                                }
                        for (var i = 0; i < 4; i++) Dispatch();
                        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 8; i++) Dispatch();
                        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared border sampling allocates no owner-thread managed bytes.");
                        Reject<ArgumentException>(() => other.FreeRID(sampler));
                        device.FreeRID(sources); device.FreeRID(sampler); cases++;
                    }
            using var wrong = new RDSamplerState { RepeatU = SamplerRepeatMode.ClampToBorder, BorderColor = integer ? SamplerBorderColor.FloatOpaqueWhite : SamplerBorderColor.IntOpaqueWhite };
            var wrongSampler = device.SamplerCreate(wrong);
            using var wrongImage = new RDUniform { Binding = 0, UniformType = name == "pairs" ? UniformType.Texture : UniformType.SamplerWithTexture }; if (name != "pairs") wrongImage.AddID(wrongSampler); wrongImage.AddID(texture);
            using var wrongPoint = new RDUniform { Binding = 1, UniformType = UniformType.Sampler }; wrongPoint.AddID(wrongSampler);
            if (name != "pairs") Reject<ArgumentException>(() => device.UniformSetCreate([wrongImage], shader, 0));
            else
            {
                var sources = device.UniformSetCreate([wrongImage, wrongPoint], shader, 0);
                var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 0); device.ComputeListBindUniformSet(list, targets, 1);
                Reject<ArgumentException>(() => device.ComputeListDispatch(list, 1, 1, 1)); device.ComputeListEnd();
            }
        }
        BorderDepth();
        Console.WriteLine($"Border sampling: {cases} configurations, all six colors, both mips, signed/unsigned/float, separate bindings, mixed axes and linear edge blends; 8 prepared dispatches per case allocate zero managed bytes.");
    }
}
