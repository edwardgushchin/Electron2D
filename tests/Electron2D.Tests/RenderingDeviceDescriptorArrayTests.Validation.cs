using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceDescriptorArrayTests
{
    private static void Validation(RenderingDevice device)
    {
        using var module = Compile(device, """
            #version 450
            layout(local_size_x=1)in;
            layout(set=3,binding=7)uniform sampler2D images[2];
            layout(set=3,binding=9)uniform sampler2D neighbor;
            layout(set=5,binding=1,std430)buffer Output{vec4 result[];};
            vec4 readArray(sampler2D values[2],uint index){return textureLod(values[index],vec2(.5),0);}
            void main(){result[0]=readArray(images,0);result[1]=readArray(images,1);result[2]=readArray(images,2);result[3]=readArray(images,0xffffffffu);result[4]=textureLod(neighbor,vec2(.5),0);}
            """, "BoundsHelpers", ShaderLanguage.GLSL);
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var a = Image(device, 255); var b = Image(device, 128);
        using var array = new RDUniform { Binding = 7, UniformType = UniformType.SamplerWithTexture }; foreach (var rid in new[] { sampler, a, sampler, b }) array.AddID(rid);
        using var adjacent = new RDUniform { Binding = 9, UniformType = UniformType.SamplerWithTexture }; adjacent.AddID(sampler); adjacent.AddID(a);
        var images = device.UniformSetCreate([array, adjacent], shader, 3); array.ClearIDs(); adjacent.ClearIDs();
        var output = device.StorageBufferCreate(80); var write = Set(device, shader, 5, 1, UniformType.StorageBuffer, output); var bytes = new byte[80]; var sets = new[] { (images, 3u), (write, 5u) };
        for (var i = 0; i < 8; i++) Execute(device, pipeline, output, bytes, sets);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute(device, pipeline, output, bytes, sets);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared descriptor-array dispatch/readback allocates zero managed owner bytes.");
        var values = MemoryMarshal.Cast<byte, float>(bytes.AsSpan()); Check(values[0] == 1 && values[4] > .5f && values[8] == 0 && values[12] == 0 && values[16] == 1, "Invalid array indices cannot wrap or select a neighboring descriptor through array parameters.");
        using var symbolicSource = new RDShaderSource { SourceCompute = """
            #version 450
            layout(local_size_x=1)in;layout(constant_id=13)const uint count=17;
            layout(set=3,binding=7)uniform sampler2D images[count];
            layout(set=5,binding=1,std430)buffer Output{vec4 result;};
            void main(){result=textureLod(images[count-1],vec2(.5),0);}
            """ };
        using var symbolic = device.ShaderCompileSPIRVFromSource(symbolicSource, false); Check(symbolic.CompileErrorCompute == "", symbolic.CompileErrorCompute);
        var specializedShader = device.ShaderCreateFromSPIRV(symbolic); Reject<NotSupportedException>(() => device.ComputePipelineCreate(specializedShader));
        using var count = new RDPipelineSpecializationConstant<uint> { ConstantID = 13, Value = 2 }; var specialized = device.ComputePipelineCreate(specializedShader, [count]);
        var corrected = Set(device, specializedShader, 3, 7, UniformType.SamplerWithTexture, sampler, a, sampler, b); var specializedWrite = Set(device, specializedShader, 5, 1, UniformType.StorageBuffer, output);
        Execute(device, specialized, output, bytes, (corrected, 3), (specializedWrite, 5)); Check(MemoryMarshal.Cast<byte, float>(bytes.AsSpan())[0] > .5f, "A typed pipeline override repairs an unusable descriptor-count default.");
        var incorrect = Set(device, specializedShader, 3, 7, UniformType.SamplerWithTexture, sampler, a);
        Reject<ArgumentException>(() => Execute(device, specialized, output, bytes, (incorrect, 3), (specializedWrite, 5))); device.ComputeListEnd();
        foreach (var runtime in new[] { false, true })
        {
            using var unsupportedSource = new RDShaderSource { SourceCompute = "#version 450\n#extension GL_EXT_nonuniform_qualifier:require\nlayout(local_size_x=1)in;layout(set=3,binding=7)uniform sampler2D images" + (runtime ? "[]" : "[2][2]") + ";layout(set=5,binding=1,std430)buffer Output{vec4 result;};void main(){result=textureLod(images[nonuniformEXT(gl_GlobalInvocationID.x)]" + (runtime ? "" : "[0]") + ",vec2(.5),0);}" };
            using var unsupported = device.ShaderCompileSPIRVFromSource(unsupportedSource, false);
            Check(unsupported.CompileErrorCompute.Contains(runtime ? "variable descriptor-count" : "UniformConstant-04655", StringComparison.Ordinal), "Unsupported descriptor shape runtime=" + runtime + ": " + unsupported.CompileErrorCompute);
        }
        foreach (var length in new[] { 1, 16, 17 })
        {
            using var capacitySource = new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1)in;layout(set=3,binding=7)uniform sampler2D images[" + length + "];layout(set=5,binding=1,std430)buffer Output{vec4 result;};void main(){result=textureLod(images[gl_GlobalInvocationID.x%" + length + "],vec2(.5),0);}" };
            using var capacity = device.ShaderCompileSPIRVFromSource(capacitySource, false);
            if (length == 17) { Check(capacity.CompileErrorCompute.Contains("sixteen", StringComparison.Ordinal), "The seventeenth descriptor rejects before native creation."); continue; }
            Check(capacity.CompileErrorCompute == "", capacity.CompileErrorCompute); var capacityShader = device.ShaderCreateFromSPIRV(capacity); var capacityPipeline = device.ComputePipelineCreate(capacityShader);
            var capacitySet = Set(device, capacityShader, 3, 7, UniformType.SamplerWithTexture, Enumerable.Range(0, length).SelectMany(_ => new[] { sampler, a }).ToArray()); var capacityWrite = Set(device, capacityShader, 5, 1, UniformType.StorageBuffer, output);
            Execute(device, capacityPipeline, output, bytes, (capacitySet, 3), (capacityWrite, 5)); Check(MemoryMarshal.Cast<byte, float>(bytes.AsSpan())[0] == 1, "Length-one and full-capacity descriptor arrays execute.");
        }
        Console.WriteLine("Bounds, array parameters, copied sets, 32 zero-allocation cycles and specialization repair passed.");
    }
}
