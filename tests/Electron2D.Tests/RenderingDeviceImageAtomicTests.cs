using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceImageAtomicTests
{
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL })
            foreach (var signed in new[] { false, true })
            {
                Concurrent(device, language, signed, false); Concurrent(device, language, signed, true); Operations(device, language, signed);
            }
        Concurrent(device, ShaderLanguage.GLSL, false, true, 8); Concurrent(device, ShaderLanguage.HLSL, true, true, 8);
        Grid(device, ShaderLanguage.GLSL); Grid(device, ShaderLanguage.HLSL); Mixed(device, ShaderLanguage.GLSL); Mixed(device, ShaderLanguage.HLSL); Capabilities(device);
        Bounds(device); MemorySemantics(device);
        Console.WriteLine("Integer image atomics passed on " + device.GetDeviceName());
    }

    private static RID Set(RenderingDevice device, RID shader, uint set, int binding, UniformType type, params RID[] resources)
    {
        using var uniform = new RDUniform { Binding = binding, UniformType = type }; foreach (var resource in resources) uniform.AddID(resource);
        return device.UniformSetCreate([uniform], shader, set);
    }

    private static RID Image(RenderingDevice device, bool signed, bool atomic = true)
    {
        using var format = new RDTextureFormat { Format = signed ? DataFormat.R32SINT : DataFormat.R32UINT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.CanUpdate | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | (atomic ? TextureUsageBits.StorageAtomic : 0) };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }

    private static RDShaderSPIRV Compile(RenderingDevice device, string source, string name, ShaderLanguage language)
    {
        using var code = new RDShaderSource { Language = language, SourceCompute = source }; var module = device.ShaderCompileSPIRVFromSource(code, false);
        Check(module.CompileErrorCompute == "", module.CompileErrorCompute);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_IMAGE_ATOMIC_OUTPUT") is { } path)
        {
            Directory.CreateDirectory(path); var original = module.BytecodeCompute;
            var result = typeof(RenderingDevice).GetMethod("Remap", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [original, ShaderStage.Compute])!;
            File.WriteAllBytes(System.IO.Path.Combine(path, name + language + ".spv"), (byte[])result.GetType().GetField("Item1")!.GetValue(result)!);
            Check(original.SequenceEqual(module.BytecodeCompute), "Atomic lowering preserves exported module bytes.");
        }
        return module;
    }

    private static void Dispatch(RenderingDevice device, RID pipeline, uint groups, params (RID Set, uint Index)[] sets)
    {
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline);
        foreach (var set in sets) device.ComputeListBindUniformSet(list, set.Set, set.Index);
        device.ComputeListDispatch(list, groups, 1, 1); device.ComputeListEnd();
    }

    private static void Concurrent(RenderingDevice device, ShaderLanguage language, bool signed, bool array, int count = 2)
    {
        var scalar = signed ? "int" : "uint"; var image = array ? "images[index%" + count + "]" : "images";
        var source = language == ShaderLanguage.GLSL ? "#version 450\nlayout(local_size_x=32)in;layout(set=3,binding=7,r32" + (signed ? "i" : "ui") + ")uniform coherent " + (signed ? "i" : "u") + "image2D images" + (array ? "[" + count + "]" : "") + ";layout(set=4,binding=2,std430)buffer Output{" + scalar + " result[];};" + scalar + " increment(uint index){return imageAtomicAdd(" + image + ",ivec2(0),1);}" + "void main(){uint index=gl_GlobalInvocationID.x;result[index]=increment(index);}" :
            "[[vk::binding(7,3)]] globallycoherent RWTexture2D<" + scalar + "> images" + (array ? "[" + count + "]" : "") + ";[[vk::binding(2,4)]] RWStructuredBuffer<" + scalar + "> result;" + scalar + " increment(uint index){" + scalar + " previous;InterlockedAdd(" + image + "[int2(0,0)],1,previous);return previous;}[numthreads(32,1,1)]void main(uint3 id:SV_DispatchThreadID){result[id.x]=increment(id.x);}";
        using var module = Compile(device, source, "Concurrent" + signed + array + (array && count != 2 ? count.ToString() : ""), language); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        var images = Enumerable.Range(0, array ? count : 1).Select(_ => Image(device, signed, atomic: array)).ToArray(); var read = Set(device, shader, 3, 7, UniformType.Image, images);
        var output = device.StorageBufferCreate(256); var write = Set(device, shader, 4, 2, UniformType.StorageBuffer, output); var sets = new[] { (read, 3u), (write, 4u) };
        var bytes = new byte[256]; var pixels = images.Select(_ => new byte[4]).ToArray(); var initial = BitConverter.GetBytes(signed ? -64 / images.Length : 0);
        void Execute()
        {
            foreach (var image in images) device.TextureUpdate(image, 0, initial);
            Dispatch(device, pipeline, 2, sets); device.BufferGetData(output, bytes.AsSpan());
            for (var i = 0; i < images.Length; i++) { device.TextureGetData(images[i], 0, pixels[i].AsSpan()); Check(BitConverter.ToInt32(pixels[i]) == (signed ? 0 : 64 / images.Length), "Concurrent image increments retain every invocation."); }
        }
        Execute();
        for (var imageIndex = 0; imageIndex < images.Length; imageIndex++)
        {
            var previous = Enumerable.Range(0, 64).Where(i => i % images.Length == imageIndex).Select(i => BitConverter.ToInt32(bytes, i * 4)).Order().ToArray();
            Check(previous.SequenceEqual(Enumerable.Range(signed ? -previous.Length : 0, previous.Length)), "Atomic return values contain each prior counter value exactly once.");
        }
        for (var i = 0; i < 8; i++) Execute(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute();
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared atomic update/dispatch/readback allocates zero managed owner bytes.");
        device.FreeRID(images[^1]); Check(!device.UniformSetIsValid(read), "Retiring the final array element invalidates its atomic set.");
        Console.WriteLine(language + " signed=" + signed + " array=" + array + ": 64 contending invocations and 32 zero-allocation cycles passed.");
    }

    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
