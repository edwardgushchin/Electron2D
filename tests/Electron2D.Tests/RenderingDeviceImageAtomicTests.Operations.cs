using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceImageAtomicTests
{
    private static void Operations(RenderingDevice device, ShaderLanguage language, bool signed)
    {
        var scalar = signed ? "int" : "uint"; string[] operations = ["Add", "Min", "Max", "And", "Or", "Xor", "Exchange", "CompSwap", "CompSwap"];
        long[] data = [5, signed ? -7 : 7, signed ? -2 : 2147483649, 31, 32, 7, 123, 99, 88]; int[] expected = signed ? [100, 105, -7, -2, 30, 62, 57, 123, 99] : [int.MinValue, unchecked((int)0x80000005), 7, unchecked((int)0x80000001), 1, 33, 38, 123, 99];
        var expressions = new List<string>();
        for (var i = 0; i < operations.Length; i++)
        {
            var literal = data[i].ToString(System.Globalization.CultureInfo.InvariantCulture) + (signed ? "" : "u"); var compare = (i == 7 ? "123" : "111") + (signed ? "" : "u");
            expressions.Add(language == ShaderLanguage.GLSL ? "result[i*9+" + i + "]=imageAtomic" + operations[i] + "(images[i],ivec2(0)," + (i >= 7 ? compare + "," : "") + literal + ");" :
                "Interlocked" + (i >= 7 ? "CompareExchange" : operations[i]) + "(images[i][int2(0,0)]," + (i >= 7 ? compare + "," : "") + literal + ",result[i*9+" + i + "]);");
        }
        var source = language == ShaderLanguage.GLSL ? "#version 450\nlayout(local_size_x=1)in;layout(set=3,binding=7,r32" + (signed ? "i" : "ui") + ")uniform coherent " + (signed ? "i" : "u") + "image2D images[2];layout(set=4,binding=2,std430)buffer Output{" + scalar + " result[];};void main(){for(uint i=0;i<2;i++){" + string.Concat(expressions) + "}}" :
            "[[vk::binding(7,3)]] globallycoherent RWTexture2D<" + scalar + "> images[2];[[vk::binding(2,4)]] RWStructuredBuffer<" + scalar + "> result;[numthreads(1,1,1)]void main(){for(uint i=0;i<2;i++){" + string.Concat(expressions) + "}}";
        using var module = Compile(device, source, "Operations" + signed, language); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        var a = Image(device, signed); var b = Image(device, signed); var inputs = Set(device, shader, 3, 7, UniformType.Image, a, b); var output = device.StorageBufferCreate(72); var outputs = Set(device, shader, 4, 2, UniformType.StorageBuffer, output);
        device.TextureUpdate(a, 0, BitConverter.GetBytes(signed ? 100 : int.MinValue)); device.TextureUpdate(b, 0, BitConverter.GetBytes(signed ? 100 : int.MinValue)); Dispatch(device, pipeline, 1, (inputs, 3), (outputs, 4));
        var values = MemoryMarshal.Cast<byte, int>(device.BufferGetData(output).AsSpan()); Check(values[..9].SequenceEqual(expected) && values[9..].SequenceEqual(expected), "Every image atomic operation returns the previous value with signed/unsigned semantics.");
        Check(BitConverter.ToInt32(device.TextureGetData(a, 0)) == 99 && BitConverter.ToInt32(device.TextureGetData(b, 0)) == 99, "Successful and failed compare exchanges preserve their exact final values.");
        Console.WriteLine(language + " signed=" + signed + ": add/min/max/and/or/xor/exchange and successful/failed compare exchange passed.");
    }

    private static void Capabilities(RenderingDevice device)
    {
        foreach (var format in new[] { DataFormat.R32UINT, DataFormat.R32SINT }) Check(device.TextureIsFormatSupportedForUsage(format, TextureUsageBits.Storage | TextureUsageBits.StorageAtomic | TextureUsageBits.CanUpdate | TextureUsageBits.CanCopyFrom), "Atomic format support matches actual allocation/execution.");
        foreach (var format in new[] { DataFormat.R8UINT, DataFormat.R16UINT, DataFormat.R32SFLOAT, DataFormat.R32G32UINT, DataFormat.R8G8B8A8UNORM, DataFormat.D32SFLOAT })
        {
            Check(!device.TextureIsFormatSupportedForUsage(format, TextureUsageBits.Storage | TextureUsageBits.StorageAtomic), "Unsupported atomic formats reject before native allocation.");
            using var description = new RDTextureFormat { Format = format, UsageBits = TextureUsageBits.Storage | TextureUsageBits.StorageAtomic }; using var view = new RDTextureView(); Reject<NotSupportedException>(() => device.TextureCreate(description, view));
        }
        using var source = new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1)in;layout(set=3,binding=7,r32ui)uniform uimage2D images[9];void main(){imageAtomicAdd(images[gl_GlobalInvocationID.x%9],ivec2(0),1);}" };
        using var excess = device.ShaderCompileSPIRVFromSource(source, false); Check(excess.CompileErrorCompute.Contains("backend limits", StringComparison.Ordinal), "The ninth native storage image rejects before native creation.");
    }
}
