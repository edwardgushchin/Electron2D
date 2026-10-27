using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class ShaderSamplerChecks
{
    private static RDSamplerState CoordinateState() => new() { UnnormalizedUVW = true, MaxLOD = 0 };
    private static void Coordinates()
    {
        var cases = 0;
        foreach (var name in new[] { "float", "signed", "unsigned", "pairs", "array" })
        {
            using var device = RenderingServer.CreateLocalRenderingDevice();
            using var module = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Device", $"coords-{name}.comp.glsl.spv")) };
            var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            var integer = name is "signed" or "unsigned"; var count = name == "array" ? 2 : 1;
            using var format = new RDTextureFormat { Width = 2, Height = 2, Format = integer ? name == "signed" ? DataFormat.R32G32B32A32SINT : DataFormat.R32G32B32A32UINT : DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling };
            using var view = new RDTextureView(); var numbers = Enumerable.Range(1, 16).Select(n => name == "signed" ? -n : n).ToArray(); var values = numbers.Select(n => (float)n).ToArray();
            var bytes = integer ? MemoryMarshal.AsBytes(numbers.AsSpan()).ToArray() : MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
            var texture = device.TextureCreate(format, view, [bytes]); var otherValues = values.Select(n => n + 100).ToArray(); var second = device.TextureCreate(format, view, [name == "array" ? MemoryMarshal.AsBytes(otherValues.AsSpan()).ToArray() : bytes]);
            var output = device.StorageBufferCreate((uint)(count * 128)); using var target = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; target.AddID(output); var targets = device.UniformSetCreate([target], shader, 1); var data = new byte[count * 128];
            foreach (var border in integer ? new[] { SamplerBorderColor.IntTransparentBlack, SamplerBorderColor.IntOpaqueBlack, SamplerBorderColor.IntOpaqueWhite } : new[] { SamplerBorderColor.FloatTransparentBlack, SamplerBorderColor.FloatOpaqueBlack, SamplerBorderColor.FloatOpaqueWhite })
                foreach (var clamp in new[] { SamplerRepeatMode.ClampToEdge, SamplerRepeatMode.ClampToBorder })
                    foreach (var linear in integer ? new[] { false } : new[] { false, true })
                    {
                        using var state = CoordinateState(); state.BorderColor = border; state.RepeatU = clamp; state.RepeatV = clamp; state.MinFilter = state.MagFilter = linear ? SamplerFilter.Linear : SamplerFilter.Nearest;
                        var sampler = device.SamplerCreate(state); state.UnnormalizedUVW = false; state.Dispose();
                        using var images = new RDUniform { Binding = 0, UniformType = name == "pairs" ? UniformType.Texture : UniformType.SamplerWithTexture };
                        if (name != "pairs") images.AddID(sampler); images.AddID(texture); if (count == 2) { images.AddID(sampler); images.AddID(second); }
                        using var samplers = new RDUniform { Binding = 1, UniformType = UniformType.Sampler }; samplers.AddID(sampler);
                        var sources = device.UniformSetCreate(name == "pairs" ? [images, samplers] : [images], shader, 0);
                        void Dispatch()
                        {
                            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 0); device.ComputeListBindUniformSet(list, targets, 1); device.ComputeListDispatch(list, (uint)(8 * count), 1, 1); device.ComputeListEnd(); device.BufferGetData(output, data.AsSpan());
                        }
                        Dispatch(); var actual = MemoryMarshal.Cast<byte, float>(data.AsSpan());
                        for (var element = 0; element < count; element++)
                            for (var sample = 0; sample < 8; sample++)
                                for (var lane = 0; lane < 4; lane++)
                                {
                                    var color = (int)border / 2 == 2 ? 1 : lane == 3 && (int)border / 2 == 1 ? 1 : 0;
                                    var pixel = sample switch { 0 or 5 or 6 => 0, 1 => 1, 2 => 2, _ => 3 };
                                    var source = values[pixel * 4 + lane] + element * 100;
                                    var expected = linear && sample == 4 ? (values[lane] + values[4 + lane] + values[8 + lane] + values[12 + lane]) / 4f + element * 100 :
                                        clamp == SamplerRepeatMode.ClampToBorder && sample >= 6 ? color :
                                        linear && clamp == SamplerRepeatMode.ClampToBorder && sample == 5 ? (source + color) / 2f : source;
                                    Check(MathF.Abs(actual[element * 32 + sample * 4 + lane] - expected) < .0001f, $"{name}/{border}/{clamp}/{linear}, sample {sample}, lane {lane}: {actual[element * 32 + sample * 4 + lane]}, expected {expected}.");
                                }
                        for (var i = 0; i < 4; i++) Dispatch(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 8; i++) Dispatch(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared texel-coordinate dispatch allocates no owner managed bytes.");
                        device.FreeRID(sources); device.FreeRID(sampler); cases++;
                    }
            if (integer)
            {
                using var linear = CoordinateState(); linear.MinFilter = linear.MagFilter = SamplerFilter.Linear; var linearSampler = device.SamplerCreate(linear);
                using var linearImage = new RDUniform { Binding = 0, UniformType = UniformType.SamplerWithTexture }; linearImage.AddID(linearSampler); linearImage.AddID(texture);
                Reject<ArgumentException>(() => device.UniformSetCreate([linearImage], shader, 0)); device.FreeRID(linearSampler);
            }
            using var coordinate = CoordinateState(); var point = device.SamplerCreate(coordinate);
            format.MIPMaps = 2; var mipBytes = new byte[80]; bytes.CopyTo(mipBytes, 0); bytes.AsSpan(0, 16).CopyTo(mipBytes.AsSpan(64)); var mip = device.TextureCreate(format, view, [mipBytes]);
            using var bad = new RDUniform { Binding = 0, UniformType = name == "pairs" ? UniformType.Texture : UniformType.SamplerWithTexture }; if (name != "pairs") bad.AddID(point); bad.AddID(mip); if (count == 2) { bad.AddID(point); bad.AddID(second); }
            if (name is not ("pairs" or "array")) Reject<ArgumentException>(() => device.UniformSetCreate([bad], shader, 0));
            else
            {
                using var onlyPoint = new RDUniform { Binding = 1, UniformType = UniformType.Sampler }; onlyPoint.AddID(point);
                var badSources = device.UniformSetCreate(name == "pairs" ? [bad, onlyPoint] : [bad], shader, 0);
                var badList = device.ComputeListBegin(); device.ComputeListBindComputePipeline(badList, pipeline); device.ComputeListBindUniformSet(badList, badSources, 0); device.ComputeListBindUniformSet(badList, targets, 1);
                Reject<ArgumentException>(() => device.ComputeListDispatch(badList, 1, 1, 1)); device.ComputeListEnd(); device.FreeRID(badSources);
            }
            format.MIPMaps = 1; format.TextureType = TextureType.TextureArray; format.ArrayLayers = 1; var array = device.TextureCreate(format, view, [bytes]);
            using var wrong = new RDUniform { Binding = 0, UniformType = UniformType.SamplerWithTexture }; wrong.AddID(point); wrong.AddID(array); if (count == 2) { wrong.AddID(point); wrong.AddID(second); }
            if (name != "pairs") Reject<ArgumentException>(() => device.UniformSetCreate([wrong], shader, 0));
        }
        CoordinateOperations(); CoordinateSamplerless();
        Console.WriteLine($"Texel-coordinate sampling: {cases} copied configurations, float/signed/unsigned images, both filters, borders, separate bindings and descriptor arrays; 8 prepared zero-allocation dispatches per case.");
    }

    private static void CoordinateOperations()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var module = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Device", "coords-gather.comp.glsl.spv")) };
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling };
        using var view = new RDTextureView(); var values = Enumerable.Range(1, 16).Select(n => (float)n).ToArray(); var image = device.TextureCreate(format, view, [MemoryMarshal.AsBytes(values.AsSpan()).ToArray()]);
        using var state = CoordinateState(); var sampler = device.SamplerCreate(state); var output = device.StorageBufferCreate(64);
        using var source = new RDUniform { Binding = 0, UniformType = UniformType.SamplerWithTexture }; source.AddID(sampler); source.AddID(image); var sources = device.UniformSetCreate([source], shader, 0);
        using var target = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; target.AddID(output); var targets = device.UniformSetCreate([target], shader, 1);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 0); device.ComputeListBindUniformSet(list, targets, 1); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); var result = device.BufferGetData(output); var actual = MemoryMarshal.Cast<byte, float>(result);
        float[] expected = [9, 13, 5, 1, 1, 2, 3, 4, 5, 6, 7, 8, 2, 2, 1, 0];
        Check(actual.SequenceEqual(expected), "Gather without offsets, explicit gradients, samplerless fetch and size/level queries preserve their texel-coordinate roles.");
        using var excessiveBias = new RDSamplerState { LODBias = float.MaxValue }; Reject<InvalidOperationException>(() => device.SamplerCreate(excessiveBias));
        excessiveBias.LODBias = -float.MaxValue; Reject<InvalidOperationException>(() => device.SamplerCreate(excessiveBias));
        module.BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Device", "coords-offset.comp.glsl.spv")); var offset = device.ShaderCreateFromSPIRV(module); Reject<ArgumentException>(() => device.UniformSetCreate([source], offset, 0));
        Console.WriteLine("Texel-coordinate operations: native gather/gradient/fetch/query pixels passed; an offset inside a reusable helper rejects before binding.");
    }
    private static void CoordinateSamplerless()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var module = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Device", "coords-fetch.comp.glsl.spv")) };
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var format = new RDTextureFormat { Width = 2, Height = 2, MIPMaps = 2, TextureType = TextureType.TextureArray, ArrayLayers = 1, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling };
        using var view = new RDTextureView(); var numbers = Enumerable.Range(1, 16).Select(n => (float)n).Concat(new float[] { 21, 22, 23, 24 }).ToArray(); var image = device.TextureCreate(format, view, [MemoryMarshal.AsBytes(numbers.AsSpan()).ToArray()]);
        using var state = CoordinateState(); var sampler = device.SamplerCreate(state); var output = device.StorageBufferCreate(64);
        using var source = new RDUniform { Binding = 0, UniformType = UniformType.SamplerWithTexture }; source.AddID(sampler); source.AddID(image); var sources = device.UniformSetCreate([source], shader, 0);
        using var target = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; target.AddID(output); var targets = device.UniformSetCreate([target], shader, 1);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 0); device.ComputeListBindUniformSet(list, targets, 1); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); var bytes = device.BufferGetData(output); var actual = MemoryMarshal.Cast<byte, float>(bytes);
        float[] expected = [1, 2, 3, 4, 5, 6, 7, 8, 21, 22, 23, 24, 1, 1, 1, 2]; Check(actual.SequenceEqual(expected), "Samplerless image access ignores coordinate mode and retains array views, explicit mip selection and queries.");
        Console.WriteLine("Samplerless array fetch/query: explicit mip one remains accessible while the unused sampler has zero LOD bounds and texel coordinates.");
    }

}
