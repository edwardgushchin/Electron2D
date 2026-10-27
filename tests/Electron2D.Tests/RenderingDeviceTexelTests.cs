using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceTexelTests
{
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL }) { Basic(device, language); Writes(device, language); Formats(device, language); Graphics(device, language); }
        Transfers(device);
        NativeFormatParity(device); Atomics(device);
        Capacity(device); Specialization(device);
        Graphics(device, ShaderLanguage.GLSL, true); Graphics(device, ShaderLanguage.GLSL, storage: true);
        Console.WriteLine((Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1" ? "Texel modules exported on " : "Formatted texel buffers passed on ") + device.GetDeviceName());
    }

    private static void Export(RDShaderSPIRV module, string name, ShaderStage stage = ShaderStage.Compute)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_OUTPUT") is not { } path) return;
        Directory.CreateDirectory(path);
        var source = module.GetStageBytecode(stage);
        var result = typeof(RenderingDevice).GetMethod("Remap", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [source, stage])!;
        File.WriteAllBytes(System.IO.Path.Combine(path, name + ".spv"), (byte[])result.GetType().GetField("Item1")!.GetValue(result)!);
    }

    private static RID Set(RenderingDevice device, RID shader, uint set, int slot, UniformType type, params RID[] resources)
    {
        using var uniform = new RDUniform { Binding = slot, UniformType = type };
        foreach (var resource in resources) uniform.AddID(resource);
        return device.UniformSetCreate([uniform], shader, set);
    }

    private static void Basic(RenderingDevice device, ShaderLanguage language)
    {
        byte[] pixels = [255, 0, 0, 255, 0, 128, 255, 64, 7, 8, 9, 10];
        var input = device.TextureBufferCreate(3, DataFormat.R8G8B8A8UNORM, pixels);
        Check(device.BufferGetData(input).SequenceEqual(pixels), "Element counts and raw data exclude private texel metadata.");
        using var source = new RDShaderSource { Language = language, SourceCompute = language == ShaderLanguage.GLSL ? """
            #version 450
            layout(local_size_x=1) in;
            layout(set=3,binding=7) uniform samplerBuffer inputValues;
            layout(set=4,binding=2,std430) buffer Output { vec4 result[]; };
            vec4 fetchValue(samplerBuffer value, int index) { return texelFetch(value,index); }
            void main() { result[0]=fetchValue(inputValues,1); result[1]=vec4(textureSize(inputValues)); }
            """ : """
            [[vk::binding(7,3)]] Buffer<float4> inputValues;
            [[vk::binding(2,4)]] RWStructuredBuffer<float4> result;
            float4 fetchValue(Buffer<float4> value,int index) { return value.Load(index); }
            [numthreads(1,1,1)] void main() { uint count; inputValues.GetDimensions(count); result[0]=fetchValue(inputValues,1); result[1]=count; }
            """ };
        using var module = device.ShaderCompileSPIRVFromSource(source, false);
        Check(module.CompileErrorCompute == "", module.CompileErrorCompute);
        Export(module, "Basic" + language);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1") return;
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var state = new RDSamplerState { MinFilter = SamplerFilter.Linear, MagFilter = SamplerFilter.Linear };
        var sampler = device.SamplerCreate(state); var output = device.StorageBufferCreate(32);
        var first = language == ShaderLanguage.GLSL ? Set(device, shader, 3, 7, UniformType.SamplerWithTextureBuffer, sampler, input) : Set(device, shader, 3, 7, UniformType.TextureBuffer, input);
        var second = Set(device, shader, 4, 2, UniformType.StorageBuffer, output);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline);
        device.ComputeListBindUniformSet(list, first, 3); device.ComputeListBindUniformSet(list, second, 4); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        var values = MemoryMarshal.Cast<byte, float>(device.BufferGetData(output).AsSpan());
        Check(values[0] == 0 && Math.Abs(values[1] - 128f / 255) < .00001f && Math.Abs(values[2] - 1) < .00001f && Math.Abs(values[3] - 64f / 255) < .00001f && values[4] == 3 && values[7] == 3, "Formatted fetches/helpers and element queries reach actual GPU storage.");
        var result = new byte[32]; byte[] change = [0, 0, 0, 255];
        void Execute(bool red)
        {
            change[0] = red ? (byte)255 : (byte)0; change[1] = red ? (byte)0 : (byte)255;
            device.BufferUpdate(input, 4, 4, change);
            var current = device.ComputeListBegin(); device.ComputeListBindComputePipeline(current, pipeline); device.ComputeListBindUniformSet(current, first, 3); device.ComputeListBindUniformSet(current, second, 4); device.ComputeListDispatch(current, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, result.AsSpan());
            var pixels = MemoryMarshal.Cast<byte, float>(result.AsSpan()); Check(Math.Abs(pixels[0] - (red ? 1 : 0)) < .00001f && Math.Abs(pixels[1] - (red ? 0 : 1)) < .00001f && pixels[4] == 3, "Changing compute input preserves encoded data and element metadata.");
        }
        for (var i = 0; i < 8; i++) Execute(i % 2 == 0);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute(i % 2 == 0);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared changing texel compute/upload/readback allocates zero managed owner bytes.");
        Reject<ArgumentException>(() => Set(device, shader, 3, 7, UniformType.StorageBuffer, input));
        var wrong = device.TextureBufferCreate(1, DataFormat.R8UINT, [1]);
        Reject<ArgumentException>(() => { if (language == ShaderLanguage.GLSL) Set(device, shader, 3, 7, UniformType.SamplerWithTextureBuffer, sampler, wrong); else Set(device, shader, 3, 7, UniformType.TextureBuffer, wrong); });
        Reject<InvalidOperationException>(() => Task.Run(() => device.BufferGetData(input)).GetAwaiter().GetResult());
        var missing = device.ComputeListBegin(); device.ComputeListBindComputePipeline(missing, pipeline); Reject<InvalidOperationException>(() => device.ComputeListDispatch(missing, 1, 1, 1)); device.ComputeListEnd(); Execute(true);
        using (var foreign = RenderingServer.CreateLocalRenderingDevice())
        {
            var other = foreign.TextureBufferCreate(1, DataFormat.R8UNORM, [1]);
            Reject<ArgumentException>(() => { if (language == ShaderLanguage.GLSL) Set(device, shader, 3, 7, UniformType.SamplerWithTextureBuffer, sampler, other); else Set(device, shader, 3, 7, UniformType.TextureBuffer, other); });
        }
        if (language == ShaderLanguage.GLSL) { Reject<ArgumentException>(() => Set(device, shader, 3, 7, UniformType.SamplerWithTextureBuffer, input, sampler)); device.FreeRID(sampler); Check(!device.UniformSetIsValid(first), "Retired samplers invalidate their combined texel sets."); }
        device.FreeRID(input); Check(!device.UniformSetIsValid(first) && device.UniformSetIsValid(second), "Texel retirement invalidates only dependent sets.");
        device.FreeRID(shader); Check(!device.ComputePipelineIsValid(pipeline) && !device.UniformSetIsValid(second), "Shader retirement invalidates its texel pipeline and sets.");
        Console.WriteLine(language + ": changing compute, copied resource roles/lifetimes and 32 zero-allocation cycles passed.");
    }
}
