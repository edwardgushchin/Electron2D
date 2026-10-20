using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceSamplerTests
{
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var language in new[] { "GLSL", "HLSL" }) { Compute(device, language); Graphics(device, language); }
        Capacity(device);
        Depth(device);
        Feedback(device);
        Console.WriteLine("Independent device Texture/Sampler sets, reusable helpers, shared filters and samplerless fetch/query passed on " + device.GetDeviceName());
    }
    private static RID Image(RenderingDevice device, uint width, uint height, byte[] pixels, DataFormat pixelFormat = DataFormat.R8G8B8A8UNORM)
    {
        using var format = new RDTextureFormat { Width = width, Height = height, Format = pixelFormat, UsageBits = TextureUsageBits.Sampling };
        using var view = new RDTextureView(); return device.TextureCreate(format, view, [pixels]);
    }
    private static RID Set(RenderingDevice device, RID shader, uint set, int binding, UniformType type, RID resource)
    {
        using var uniform = new RDUniform { Binding = binding, UniformType = type }; uniform.AddID(resource); return device.UniformSetCreate([uniform], shader, set);
    }
    private static void Compute(RenderingDevice device, string language)
    {
        using var module = new RDShaderSPIRV(); using var input = typeof(RenderingDeviceSamplerTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalSamplers" + language + ".comp.spv")!;
        using var bytes = new MemoryStream(); input.CopyTo(bytes); module.BytecodeCompute = bytes.ToArray(); var original = module.BytecodeCompute; Export(module, "Compute" + language); var shader = device.ShaderCreateFromSPIRV(module); Check(module.BytecodeCompute.SequenceEqual(original), "Private lowering preserves exported source bytecode.");
        var first = Image(device, 2, 2, [0, 0, 0, 255, 255, 0, 0, 255, 0, 0, 0, 255, 255, 0, 0, 255]);
        var second = Image(device, 1, 1, [0, 255, 0, 255]); var fetch = Image(device, 1, 1, [0, 0, 255, 255]);
        using var point = new RDSamplerState(); using var linear = new RDSamplerState { MinFilter = SamplerFilter.Linear, MagFilter = SamplerFilter.Linear };
        var a = device.SamplerCreate(point); var b = device.SamplerCreate(linear); var output = device.StorageBufferCreate(160);
        using var arrayFormat = new RDTextureFormat { TextureType = TextureType.TextureArray, ArrayLayers = 2, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.Sampling }; using var arrayView = new RDTextureView(); var array = device.TextureCreate(arrayFormat, arrayView, [new byte[] { 255, 0, 0, 255 }, new byte[] { 255, 255, 0, 255 }]);
        var unsigned = Image(device, 1, 1, [201, 17, 9, 255], DataFormat.R8G8B8A8UINT); var signed = Image(device, 1, 1, [254, 5, 128, 1], DataFormat.R8G8B8A8SINT);
        uint[] command = [1, 1, 1]; var indirect = device.StorageBufferCreate(12, MemoryMarshal.AsBytes(command.AsSpan()), StorageBufferUsage.DispatchIndirect); var pipeline = device.ComputePipelineCreate(shader);
        var sets = new[] { Set(device, shader, 3, 7, UniformType.Texture, first), Set(device, shader, 4, 11, UniformType.Texture, second), Set(device, shader, 6, 2, UniformType.Texture, fetch), Set(device, shader, 2, 5, UniformType.Sampler, a), Set(device, shader, 5, 13, UniformType.Sampler, b), Set(device, shader, 9, 4, UniformType.StorageBuffer, output), Set(device, shader, 7, 3, UniformType.Texture, array), Set(device, shader, 8, 1, UniformType.Texture, unsigned), Set(device, shader, 10, 2, UniformType.Texture, signed) };
        uint[] indices = [3, 4, 6, 2, 5, 9, 7, 8, 10]; var result = new byte[160];
        var alternate = Set(device, shader, 5, 13, UniformType.Sampler, a); var ordinary = sets[4];
        void Execute(bool nearest = false)
        {
            sets[4] = nearest ? alternate : ordinary; var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline);
            for (var i = 0; i < sets.Length; i++) device.ComputeListBindUniformSet(list, sets[i], indices[i]);
            if (nearest) device.ComputeListDispatchIndirect(list, indirect, 0); else device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, result.AsSpan());
            var current = MemoryMarshal.Cast<byte, float>(result.AsSpan()); Check(Math.Abs(current[8] - (nearest ? 1 : .5f)) < .003f, "Changing sampled sets/indirect dispatch consume their current filter."); Check(current[28] == (nearest ? 1 : 0) && current[29] == (nearest ? 0 : 1), "Nested image/sampler helpers preserve both runtime control-flow paths.");
        }
        Execute(); var values = MemoryMarshal.Cast<byte, float>(result.AsSpan());
        Check(values[0] == 1 && values[4] == 0 && values[5] == 1 && Math.Abs(values[8] - .5f) < .003f && values[14] == 1 && values[16] == 2 && values[17] == 2 && values[18] == 1 && values[19] == 1 && values[20] == 1 && values[21] == 1 && values[22] == 0 && values[24] == 1 && values[25] == 1 && values[26] == 2 && values[32] == 201 && values[33] == 17 && values[35] == 255 && values[36] == -2 && values[38] == -128, "Independent bindings select their actual image/filter and samplerless dimensions.");
        for (var i = 0; i < 8; i++) Execute(i % 2 == 0); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute(i % 2 == 0); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared independent dispatch/readback allocates zero managed owner bytes.");
        Reject<ArgumentException>(() => Set(device, shader, 3, 7, UniformType.Sampler, a));
        using (var foreign = RenderingServer.CreateLocalRenderingDevice())
        { var other = Image(foreign, 1, 1, [255, 0, 0, 255]); Reject<ArgumentException>(() => Set(device, shader, 3, 7, UniformType.Texture, other)); }
        using (var wrongFormat = new RDTextureFormat { Format = DataFormat.R8G8B8A8UINT, UsageBits = TextureUsageBits.Sampling }) using (var wrongView = new RDTextureView())
        { var integer = device.TextureCreate(wrongFormat, wrongView); Reject<ArgumentException>(() => Set(device, shader, 3, 7, UniformType.Texture, integer)); device.FreeRID(integer); }
        var missing = device.ComputeListBegin(); device.ComputeListBindComputePipeline(missing, pipeline); Reject<InvalidOperationException>(() => device.ComputeListDispatch(missing, 1, 1, 1)); device.ComputeListEnd(); Execute();
        using (var description = new RDUniform { Binding = 7, UniformType = UniformType.Texture })
        { description.AddID(first); var copy = device.UniformSetCreate([description], shader, 3); description.ClearIDs(); description.AddID(second); description.Dispose(); var originalSet = sets[0]; sets[0] = copy; Execute(); sets[0] = originalSet; device.FreeRID(copy); }
        var badFilter = Set(device, shader, 2, 5, UniformType.Sampler, b); var pointSet = sets[3]; sets[3] = badFilter; Reject<ArgumentException>(() => Execute()); device.ComputeListEnd(); sets[3] = pointSet; Execute(); device.FreeRID(badFilter);
        point.EnableCompare = true; var compare = device.SamplerCreate(point); var compareSet = Set(device, shader, 2, 5, UniformType.Sampler, compare); var oldSet = sets[3]; sets[3] = compareSet;
        Reject<ArgumentException>(() => Execute()); device.ComputeListEnd(); sets[3] = oldSet; Execute(); device.FreeRID(compareSet); device.FreeRID(compare);
        device.FreeRID(a); Check(!device.UniformSetIsValid(sets[3]) && device.UniformSetIsValid(sets[0]), "Sampler retirement leaves independent image sets alive.");
        device.FreeRID(first); Check(!device.UniformSetIsValid(sets[0]) && device.UniformSetIsValid(ordinary), "Image retirement preserves the unrelated sampler set.");
        device.FreeRID(shader); Check(!device.ComputePipelineIsValid(pipeline), "Shader retirement invalidates linked programs.");
        foreach (var rid in sets.Append(ordinary).Append(alternate).Distinct()) device.FreeRID(rid);
        foreach (var rid in new[] { second, fetch, array, unsigned, signed, b, output, pipeline, indirect }) device.FreeRID(rid);
        Console.WriteLine(language + ": shared filters, fetch/query and 32 prepared cycles, 0 managed owner bytes.");
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
