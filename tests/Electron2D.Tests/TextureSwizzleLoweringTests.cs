using System.Runtime.InteropServices;
using Electron2D;
using IOPath = System.IO.Path;
using static Electron2D.RenderingDevice;

internal static class TextureSwizzleLoweringTests
{
    internal static void Run(string folder)
    {
        Cache(File.ReadAllBytes(IOPath.Combine(folder, "float.comp.glsl.spv")));
        foreach (var name in new[] { "float", "signed", "unsigned", "array", "pairs" })
        {
            var original = File.ReadAllBytes(IOPath.Combine(folder, name + ".comp.glsl.spv")); var copy = original.ToArray();
            var maps = new Dictionary<(uint Set, int Binding, int Element), uint> { [(0, 0, 0)] = 1262, [(0, 0, 1)] = 3353 };
            var lowered = CanvasSamplerArrays.LowerDevice(original, swizzles: maps);
            if (!original.SequenceEqual(copy) || lowered.Plan is null) throw new InvalidOperationException("Swizzle lowering preserves caller modules and produces an actual sampled plan.");
            File.WriteAllBytes(IOPath.Combine(folder, name + "-swizzled.spv"), lowered.Code);
            using var device = RenderingServer.CreateLocalRenderingDevice();
            using var module = new RDShaderSPIRV { BytecodeCompute = lowered.Code }; var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            var integer = name != "float"; var signed = name is "signed" or "pairs";
            using var format = new RDTextureFormat { Width = 2, Height = 2, Format = integer ? signed ? DataFormat.R32G32B32A32SINT : DataFormat.R32G32B32A32UINT : DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling };
            using var view = new RDTextureView(); using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
            var samples = Enumerable.Range(1, 16).Select(v => (signed ? -1 : 1) * v).ToArray();
            var floats = samples.Select(v => (float)v).ToArray(); var data = integer ? MemoryMarshal.AsBytes(samples.AsSpan()).ToArray() : MemoryMarshal.AsBytes(floats.AsSpan()).ToArray();
            var texture = device.TextureCreate(format, view, [data]); var output = device.StorageBufferCreate(256);
            var uniforms = new List<RDUniform>();
            try
            {
                for (var slot = 0; slot < lowered.Plan.Pairs.Length; slot++)
                {
                    var uniform = new RDUniform { Binding = slot, UniformType = UniformType.SamplerWithTexture }; uniform.AddID(sampler); uniform.AddID(texture); uniforms.Add(uniform);
                }
                var images = device.UniformSetCreate(uniforms.ToArray(), shader, lowered.Plan.Set);
                using var target = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; target.AddID(output); var targets = device.UniformSetCreate([target], shader, 1);
                var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, images, lowered.Plan.Set); device.ComputeListBindUniformSet(list, targets, 1);
                var count = name is "array" or "pairs" ? 2u : 1u; device.ComputeListDispatch(list, count, 1, 1); device.ComputeListEnd(); var bytes = device.BufferGetData(output); var actual = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
                TextureSwizzleRuntimeChecks.CheckResults(name, actual, count);
                Console.WriteLine("Swizzle lowering native: " + name + " sample/fetch/four gathers/query/opaque call passed.");
            }
            finally { foreach (var uniform in uniforms) uniform.Dispose(); }
        }
    }
    private static void Cache(byte[] code)
    {
        var cache = new TextureSwizzleCache(); cache.BeginFrame();
        var maps = Enumerable.Range(1, 70).Select(v => (uint)(v % 7 | (v / 7 % 7 << 3) | (v / 49 % 7 << 6) | 1024)).ToArray();
        var prepared = maps.Select(v => cache.Get(code, [v])).ToArray();
        for (var frame = 0; frame < 3; frame++)
        {
            cache.BeginFrame();
            for (var i = 0; i < maps.Length; i++) if (!ReferenceEquals(prepared[i], cache.Get(code, [maps[i]]))) throw new InvalidOperationException("Active frame modules survive cache pressure and preserve prepared pipeline identity.");
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < maps.Length; i++) _ = cache.Get(code, [maps[i]]);
        if (GC.GetAllocatedBytesForCurrentThread() != before) throw new InvalidOperationException("Prepared component-map cache lookups allocate no managed bytes.");
        cache.BeginFrame(); cache.BeginFrame(); cache.BeginFrame();
        if (ReferenceEquals(prepared[0], cache.Get(code, [maps[0]]))) throw new InvalidOperationException("Inactive modules return to the cache budget during quiet frames.");
        if (!ReferenceEquals(code, cache.Get(code, [0]))) throw new InvalidOperationException("Identity maps return the original native module.");
        Console.WriteLine("Swizzle cache: 70 simultaneous frame modules, stable prepared identities, zero-allocation hits and inactive retirement passed.");
    }
}
