using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class ShaderSamplerChecks
{
    private static void Formats(bool masked)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var sibling = RenderingServer.CreateLocalRenderingDevice();
        var formats = Enum.GetValues<DataFormat>().Where(f => f != DataFormat.Max).ToArray(); var supported = 0; var linear = 0;
        foreach (var format in formats)
        {
            var nearest = device.SamplerIsFormatSupportedForFilter(format, SamplerFilter.Nearest); var filtered = device.SamplerIsFormatSupportedForFilter(format, SamplerFilter.Linear);
            Check(!filtered || nearest, "Linear format features also require sampled-image support.");
            Check(sibling.SamplerIsFormatSupportedForFilter(format, SamplerFilter.Nearest) == nearest, "Independent logical devices share actual physical format capabilities.");
            supported += nearest ? 1 : 0; linear += filtered ? 1 : 0;
        }
        Check(device.SamplerIsFormatSupportedForFilter(DataFormat.R8G8B8A8UNORM, SamplerFilter.Linear), "Ordinary normalized color linear filtering is available.");
        Check(device.SamplerIsFormatSupportedForFilter(DataFormat.R32UINT, SamplerFilter.Nearest) && !device.SamplerIsFormatSupportedForFilter(DataFormat.R32UINT, SamplerFilter.Linear), "Native integer sampling preserves nearest-only format features.");
        Check(!device.SamplerIsFormatSupportedForFilter(DataFormat.R8G8B8UNORM, SamplerFilter.Nearest), "An unmapped native image format reports unsupported.");
        Check(device.SamplerIsFormatSupportedForFilter(DataFormat.R32G32B32A32SFLOAT, SamplerFilter.Linear) == !masked, "The controlled native feature mask selects the expected branch.");
        Reject<ArgumentOutOfRangeException>(() => device.SamplerIsFormatSupportedForFilter((DataFormat)(-1), SamplerFilter.Nearest));
        Reject<ArgumentOutOfRangeException>(() => device.SamplerIsFormatSupportedForFilter(DataFormat.Max, SamplerFilter.Nearest));
        Reject<ArgumentOutOfRangeException>(() => device.SamplerIsFormatSupportedForFilter(DataFormat.R8UNORM, (SamplerFilter)99));
        Reject<InvalidOperationException>(() => Task.Run(() => device.SamplerIsFormatSupportedForFilter(DataFormat.R8UNORM, SamplerFilter.Nearest)).GetAwaiter().GetResult());
        for (var i = 0; i < 1024; i++) foreach (var format in formats) _ = device.SamplerIsFormatSupportedForFilter(format, SamplerFilter.Linear);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) foreach (var format in formats) _ = device.SamplerIsFormatSupportedForFilter(format, SamplerFilter.Linear);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Prepared format queries allocate zero owner-thread managed bytes; actual={allocated}.");
        FormatSampling(device, masked); BorderDepth(linearFilter: true);
        device.Dispose(); Check(sibling.SamplerIsFormatSupportedForFilter(DataFormat.R8UNORM, SamplerFilter.Nearest), "A sibling lease survives independent device retirement.");
        Reject<ObjectDisposedException>(() => device.SamplerIsFormatSupportedForFilter(DataFormat.R8UNORM, SamplerFilter.Nearest));
        Console.WriteLine($"Native format features: {formats.Length} public formats, {supported} nearest/{linear} linear, masked={masked}; shared lifetimes, copied filters, binding failure/recovery, depth comparison and zero-allocation queries passed.");
    }

    private static void FormatSampling(RenderingDevice device, bool masked)
    {
        using var module = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Device", "format-filter.comp.glsl.spv")) };
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var format = new RDTextureFormat { Width = 2, Height = 2, MIPMaps = 2, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling };
        using var view = new RDTextureView(); float[] source = [0, 0, 0, 1, 1, 0, 0, 1, 0, 1, 0, 1, 1, 1, 0, 1, 0, 0, 1, 1];
        var texture = device.TextureCreate(format, view, [MemoryMarshal.AsBytes(source.AsSpan()).ToArray()]); var output = device.StorageBufferCreate(32); var bytes = new byte[32];
        using var result = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; result.AddID(output); var target = device.UniformSetCreate([result], shader, 1);
        for (var mode = 0; mode < 5; mode++)
        {
            using var state = new RDSamplerState { MinFilter = mode == 1 ? SamplerFilter.Linear : SamplerFilter.Nearest, MagFilter = mode == 2 ? SamplerFilter.Linear : SamplerFilter.Nearest, MIPFilter = mode == 3 ? SamplerFilter.Linear : SamplerFilter.Nearest, UseAnisotropy = mode == 4, AnisotropyMax = 2 };
            var sampler = device.SamplerCreate(state); state.MinFilter = state.MagFilter = state.MIPFilter = SamplerFilter.Nearest; state.Dispose();
            using var input = new RDUniform { Binding = 0, UniformType = UniformType.SamplerWithTexture }; input.AddID(sampler); input.AddID(texture);
            void Execute()
            {
                RID set = default;
                try
                {
                    set = device.UniformSetCreate([input], shader, 0); var list = device.ComputeListBegin();
                    try { device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListBindUniformSet(list, target, 1); device.ComputeListDispatch(list, 2, 1, 1); }
                    finally { device.ComputeListEnd(); }
                    device.BufferGetData(output, bytes.AsSpan());
                }
                finally { if (set.IsValid()) device.FreeRID(set); }
            }
            if (masked && mode is 1 or 2 or 3) Reject<ArgumentException>(Execute);
            else
            {
                Execute(); var values = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
                Check(values.ToArray().All(v => float.IsFinite(v) && v >= 0 && v <= 1), "Native filtering produces finite normalized colors.");
                if (mode != 4)
                {
                    var magnified = mode == 2 ? .5f : 1; var reduced = mode == 1 ? .5f : mode == 3 ? .75f : 1; var blue = mode == 3 ? .25f : 0;
                    Check(Math.Abs(values[0] - magnified) < .00001f && Math.Abs(values[1] - magnified) < .00001f && values[3] == 1 && Math.Abs(values[4] - reduced) < .00001f && Math.Abs(values[5] - reduced) < .00001f && Math.Abs(values[6] - blue) < .00001f && values[7] == 1, "Copied min/mag/mip filtering executes distinct native sampling results.");
                }
            }
            device.FreeRID(sampler);
        }
        device.FreeRID(target); device.FreeRID(output); device.FreeRID(texture); device.FreeRID(pipeline); device.FreeRID(shader);
    }
}
