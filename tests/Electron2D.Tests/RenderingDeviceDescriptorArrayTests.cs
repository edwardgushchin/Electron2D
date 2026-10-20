using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceDescriptorArrayTests
{
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL })
        {
            Sampled(device, language); Texels(device, language); Images(device, language);
            Graphics(device, language, "sampled"); Graphics(device, language, "input");
        }
        Graphics(device, ShaderLanguage.GLSL, "storage"); Graphics(device, ShaderLanguage.GLSL, "input", true);
        Validation(device);
        Console.WriteLine("Fixed device descriptor arrays passed on " + device.GetDeviceName());
    }

    private static RID Set(RenderingDevice device, RID shader, uint index, int binding, UniformType type, params RID[] ids)
    {
        using var uniform = new RDUniform { Binding = binding, UniformType = type };
        foreach (var id in ids) uniform.AddID(id);
        return device.UniformSetCreate([uniform], shader, index);
    }

    private static RID Image(RenderingDevice device, byte red, TextureUsageBits usage = TextureUsageBits.Sampling)
    {
        using var format = new RDTextureFormat { Format = DataFormat.R8G8B8A8UNORM, UsageBits = usage | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanUpdate };
        using var view = new RDTextureView(); return device.TextureCreate(format, view, [new byte[] { red, 0, 0, 255 }]);
    }

    private static RDShaderSPIRV Compile(RenderingDevice device, string code, string name, ShaderLanguage language)
    {
        using var source = new RDShaderSource { Language = language, SourceCompute = code };
        var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_DESCRIPTOR_OUTPUT") is { } path)
        {
            Directory.CreateDirectory(path); var original = module.BytecodeCompute;
            var result = typeof(RenderingDevice).GetMethod("Remap", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [original, ShaderStage.Compute])!;
            File.WriteAllBytes(System.IO.Path.Combine(path, name + language + ".spv"), (byte[])result.GetType().GetField("Item1")!.GetValue(result)!);
            Check(module.BytecodeCompute.SequenceEqual(original), "Lowering preserves exported shader bytecode.");
        }
        return module;
    }

    private static void Execute(RenderingDevice device, RID pipeline, RID output, byte[] bytes, params (RID Set, uint Index)[] sets)
    {
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline);
        foreach (var set in sets) device.ComputeListBindUniformSet(list, set.Set, set.Index);
        device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, bytes.AsSpan());
    }

    private static void Sampled(RenderingDevice device, ShaderLanguage language)
    {
        foreach (var combined in language == ShaderLanguage.GLSL ? new[] { false, true } : new[] { false })
        {
            var glsl = "#version 450\nlayout(local_size_x=1)in;" + (combined ? "layout(set=3,binding=7)uniform sampler2D images[2];" : "layout(set=3,binding=7)uniform texture2D images[2];layout(set=4,binding=2)uniform sampler filters[2];") + "layout(set=5,binding=1,std430)buffer Output{vec4 result[];};" +
                (combined ? "vec4 readImage(sampler2D value){return textureLod(value,vec2(.5),0);}" : "vec4 readImage(texture2D value,sampler filterValue){return textureLod(sampler2D(value,filterValue),vec2(.5),0);}") +
                "void main(){for(uint i=0;i<2;i++){result[i]=" + (combined ? "readImage(images[i])" : "readImage(images[i],filters[1-i])") + ";}result[2]=vec4(textureSize(" + (combined ? "images[1]" : "sampler2D(images[1],filters[0])") + ",0),0,0);}";
            using var module = Compile(device, language == ShaderLanguage.GLSL ? glsl : """
                [[vk::binding(7,3)]] Texture2D<float4> images[2];
                [[vk::binding(2,4)]] SamplerState filters[2];
                [[vk::binding(1,5)]] RWStructuredBuffer<float4> result;
                float4 readImage(Texture2D<float4> value,SamplerState filterValue){return value.SampleLevel(filterValue,float2(.5,.5),0);}
                [numthreads(1,1,1)]void main(){for(uint i=0;i<2;i++){result[i]=readImage(images[i],filters[1-i]);}uint w,h;images[1].GetDimensions(w,h);result[2]=float4(w,h,0,0);}
                """, "Sampled" + combined, language);
            var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            var a = Image(device, 255); var b = Image(device, 128); using var state = new RDSamplerState(); var first = device.SamplerCreate(state); var second = device.SamplerCreate(state);
            var images = Set(device, shader, 3, 7, combined ? UniformType.SamplerWithTexture : UniformType.Texture, combined ? [first, a, second, b] : [a, b]);
            var samplers = combined ? default : Set(device, shader, 4, 2, UniformType.Sampler, first, second);
            var output = device.StorageBufferCreate(48); var write = Set(device, shader, 5, 1, UniformType.StorageBuffer, output); var bytes = new byte[48];
            var sets = combined ? new[] { (images, 3u), (write, 5u) } : [(images, 3u), (samplers, 4u), (write, 5u)];
            Execute(device, pipeline, output, bytes, sets); var values = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
            Check(values[0] == 1 && Math.Abs(values[4] - 128f / 255) < .00001f && values[8] == 1 && values[9] == 1, "Sampled descriptor indices/helpers select every image and sampler element.");
            Reject<ArgumentException>(() => Set(device, shader, 3, 7, combined ? UniformType.SamplerWithTexture : UniformType.Texture, combined ? [first, a] : [a]));
            device.FreeRID(b); Check(!device.UniformSetIsValid(images), "Retiring a nonzero array element invalidates its copied set.");
            Console.WriteLine(language + " sampled arrays combined=" + combined + " passed.");
        }
    }

    private static void Texels(RenderingDevice device, ShaderLanguage language)
    {
        using var module = Compile(device, language == ShaderLanguage.GLSL ? """
            #version 450
            layout(local_size_x=1)in;
            layout(set=3,binding=7)uniform samplerBuffer inputs[2];
            layout(set=4,binding=2,r32ui)uniform uimageBuffer targets[2];
            layout(set=5,binding=1,std430)buffer Output{vec4 result[];};
            vec4 readValue(samplerBuffer value){return texelFetch(value,0);}
            void main(){for(uint i=0;i<2;i++){result[i]=readValue(inputs[i]);imageStore(targets[i],0,uvec4(17+i));imageAtomicAdd(targets[i],0,5);}result[2]=vec4(textureSize(inputs[1]));}
            """ : """
            [[vk::binding(7,3)]] Buffer<float4> inputs[2];
            [[vk::binding(2,4)]] RWBuffer<uint> targets[2];
            [[vk::binding(1,5)]] RWStructuredBuffer<float4> result;
            float4 readValue(Buffer<float4> value){return value.Load(0);}
            [numthreads(1,1,1)]void main(){for(uint i=0;i<2;i++){result[i]=readValue(inputs[i]);targets[i][0]=17+i;InterlockedAdd(targets[i][0],5);}uint n;inputs[1].GetDimensions(n);result[2]=n;}
            """, "Texel", language);
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        var a = device.TextureBufferCreate(1, DataFormat.R8G8B8A8UNORM, [255, 0, 0, 255]); var b = device.TextureBufferCreate(2, DataFormat.R8G8B8A8UNORM, [128, 0, 0, 255, 0, 0, 0, 0]);
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state);
        var read = Set(device, shader, 3, 7, language == ShaderLanguage.GLSL ? UniformType.SamplerWithTextureBuffer : UniformType.TextureBuffer, language == ShaderLanguage.GLSL ? [sampler, a, sampler, b] : [a, b]);
        var x = device.TextureBufferCreate(1, DataFormat.R32UINT); var y = device.TextureBufferCreate(1, DataFormat.R32UINT); var target = Set(device, shader, 4, 2, UniformType.ImageBuffer, x, y);
        var output = device.StorageBufferCreate(48); var write = Set(device, shader, 5, 1, UniformType.StorageBuffer, output); var bytes = new byte[48];
        Execute(device, pipeline, output, bytes, (read, 3), (target, 4), (write, 5)); var values = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
        Check(values[0] == 1 && Math.Abs(values[4] - 128f / 255) < .00001f && values[8] == 2, "Texel arrays preserve formatting, indexing, helpers and dimensions.");
        Check(BitConverter.ToUInt32(device.BufferGetData(x)) == 22 && BitConverter.ToUInt32(device.BufferGetData(y)) == 23, "Image buffer array stores reach both GPU resources.");
        Console.WriteLine(language + " texel arrays passed.");
    }

    private static void Images(RenderingDevice device, ShaderLanguage language)
    {
        using var module = Compile(device, language == ShaderLanguage.GLSL ? """
            #version 450
            layout(local_size_x=1)in;
            layout(set=3,binding=7,rgba8)uniform image2D images[2];
            layout(set=5,binding=1,std430)buffer Output{vec4 result[];};
                        void main(){for(uint i=0;i<2;i++){result[i]=imageLoad(images[i],ivec2(0));imageStore(images[i],ivec2(0),vec4(0,1,0,1));}result[2]=vec4(imageSize(images[1]),0,0);}
            """ : """
            [[vk::binding(7,3),vk::image_format("rgba8")]] RWTexture2D<float4> images[2];
            [[vk::binding(1,5)]] RWStructuredBuffer<float4> result;
            float4 readValue(RWTexture2D<float4> value){return value[int2(0,0)];}
            [numthreads(1,1,1)]void main(){for(uint i=0;i<2;i++){result[i]=readValue(images[i]);images[i][int2(0,0)]=float4(0,1,0,1);}uint w,h;images[1].GetDimensions(w,h);result[2]=float4(w,h,0,0);}
            """, "Image", language);
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        var a = Image(device, 255, TextureUsageBits.Storage); var b = Image(device, 128, TextureUsageBits.Storage); var images = Set(device, shader, 3, 7, UniformType.Image, a, b);
        var output = device.StorageBufferCreate(48); var write = Set(device, shader, 5, 1, UniformType.StorageBuffer, output); var bytes = new byte[48];
        Execute(device, pipeline, output, bytes, (images, 3), (write, 5)); var values = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
        Check(values[0] == 1 && Math.Abs(values[4] - 128f / 255) < .00001f && values[8] == 1, "Storage image array loads and queries select individual resources.");
        Check(device.TextureGetData(a, 0)[1] == 255 && device.TextureGetData(b, 0)[1] == 255, "Storage image array stores reach every element.");
        Console.WriteLine(language + " image arrays passed.");
    }

    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
