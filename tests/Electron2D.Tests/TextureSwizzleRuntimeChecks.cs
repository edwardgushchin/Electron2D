using System.Runtime.InteropServices;
using Electron2D;
using IOPath = System.IO.Path;
using static Electron2D.RenderingDevice;

internal static partial class TextureSwizzleRuntimeChecks
{
    internal static void CheckResults(string name, ReadOnlySpan<float> actual, uint count)
    {
        var signed = name is "signed" or "pairs";
        var sign = signed ? -1f : 1f;
        float[] expected = [16 * sign, 15 * sign, 13 * sign, 1, 4 * sign, 3 * sign, sign, 1, 12 * sign, 16 * sign, 8 * sign, 4 * sign, 11 * sign, 15 * sign, 7 * sign, 3 * sign, 9 * sign, 13 * sign, 5 * sign, sign, 1, 1, 1, 1, 2, 2, 1, 0, 16 * sign, 15 * sign, 13 * sign, 1];
        for (var i = 0; i < expected.Length; i++) if (MathF.Abs(actual[i] - expected[i]) > .00001f) throw new InvalidOperationException($"Swizzle {name} lane {i}: {actual[i]}, expected {expected[i]}.");
        if (count == 2)
        {
            float[] second = [0, 13 * sign, 14 * sign, 16 * sign, 0, sign, 2 * sign, 4 * sign, 0, 0, 0, 0, 9 * sign, 13 * sign, 5 * sign, sign, 10 * sign, 14 * sign, 6 * sign, 2 * sign, 12 * sign, 16 * sign, 8 * sign, 4 * sign, 2, 2, 1, 0, 0, 13 * sign, 14 * sign, 16 * sign];
            for (var i = 0; i < second.Length; i++) if (MathF.Abs(actual[32 + i] - second[i]) > .00001f) throw new InvalidOperationException($"Swizzle {name} element one lane {i}: {actual[32 + i]}, expected {second[i]}.");
        }
    }
    internal static void Run(string folder)
    {
        foreach (var name in new[] { "float", "signed", "unsigned", "array", "pairs" })
        {
            using var device = RenderingServer.CreateLocalRenderingDevice();
            using var module = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(IOPath.Combine(folder, name + ".comp.glsl.spv")) };
            var original = module.BytecodeCompute; var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            var integer = name != "float"; var signed = name is "signed" or "pairs"; var count = name is "array" or "pairs" ? 2u : 1u;
            using var format = new RDTextureFormat { Width = 2, Height = 2, Format = integer ? signed ? DataFormat.R32G32B32A32SINT : DataFormat.R32G32B32A32UINT : DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling };
            using var view = new RDTextureView { SwizzleR = TextureSwizzle.A, SwizzleG = TextureSwizzle.B, SwizzleB = TextureSwizzle.R, SwizzleA = TextureSwizzle.One }; using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state);
            var numbers = Enumerable.Range(1, 16).Select(v => (signed ? -1 : 1) * v).ToArray(); var values = numbers.Select(v => (float)v).ToArray();
            var bytes = integer ? MemoryMarshal.AsBytes(numbers.AsSpan()).ToArray() : MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
            var first = device.TextureCreate(format, view, [bytes]);
            view.SwizzleR = TextureSwizzle.Zero; view.SwizzleG = TextureSwizzle.R; view.SwizzleB = TextureSwizzle.G; view.SwizzleA = TextureSwizzle.A;
            var second = device.TextureCreate(format, view, [bytes]); view.Dispose();
            using var images = new RDUniform { Binding = 0, UniformType = name == "pairs" ? UniformType.Texture : UniformType.SamplerWithTexture };
            if (name != "pairs") images.AddID(sampler); images.AddID(first);
            if (count == 2) { if (name != "pairs") images.AddID(sampler); images.AddID(second); }
            using var samplers = new RDUniform { Binding = 1, UniformType = UniformType.Sampler }; samplers.AddID(sampler); samplers.AddID(sampler);
            var sources = device.UniformSetCreate(name == "pairs" ? [images, samplers] : [images], shader, 0);
            var output = device.StorageBufferCreate(384); using var target = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; target.AddID(output); var targets = device.UniformSetCreate([target], shader, 1);
            var result = new byte[384];
            void Dispatch()
            {
                var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 0); device.ComputeListBindUniformSet(list, targets, 1); device.ComputeListDispatch(list, count == 2 ? 3u : 1u, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, result.AsSpan());
            }
            for (var i = 0; i < 16; i++) Dispatch(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Dispatch(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated != 0) throw new InvalidOperationException($"Runtime sampled variant {name} allocates {allocated} managed bytes.");
            CheckResults(name, MemoryMarshal.Cast<byte, float>(result.AsSpan()), count);
            if (count == 2) foreach (var lane in MemoryMarshal.Cast<byte, float>(result.AsSpan(256))) if (lane != 0) throw new InvalidOperationException("Invalid sampled descriptor selectors retain zero, including constant component maps.");
            if (!module.BytecodeCompute.SequenceEqual(original)) throw new InvalidOperationException("Runtime component maps must preserve public bytecode.");
            Console.WriteLine("Swizzle runtime variant: " + name + " values passed; 32 prepared dispatches, owner=0 managed bytes.");
        }
        EdgeCases(folder);
        Graphics(folder);
    }
}

