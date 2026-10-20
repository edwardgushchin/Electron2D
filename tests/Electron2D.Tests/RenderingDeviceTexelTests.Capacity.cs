using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceTexelTests
{
    private static void Capacity(RenderingDevice device)
    {
        string Code(int count) => "#version 450\nlayout(local_size_x=1) in;" + string.Concat(Enumerable.Range(0, count).Select(i => "layout(set=3,binding=" + (i + 1) + ") uniform samplerBuffer input" + i + ";")) + "layout(set=4,binding=9,std430)buffer Output{float result;};void main(){result=" + string.Join('+', Enumerable.Range(0, count).Select(i => "texelFetch(input" + i + ",0).x")) + ";}";
        using var source = new RDShaderSource { SourceCompute = Code(7) }; using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute); Export(module, "Capacity8");
        source.SourceCompute = Code(8); using var excessive = device.ShaderCompileSPIRVFromSource(source, false); Check(excessive.CompileErrorCompute.Contains("eight storage buffers", StringComparison.Ordinal), "Texel and ordinary resources share the actual eight-buffer native limit.");
        source.SourceCompute = "#version 450\nlayout(local_size_x=1)in;layout(set=0,binding=0)uniform samplerBuffer values[2];layout(set=0,binding=1,std430)buffer Output{vec4 result;};void main(){result=texelFetch(values[1],0);}";
        using var arrays = device.ShaderCompileSPIRVFromSource(source, false); Check(arrays.CompileErrorCompute.Contains("resource-array integration", StringComparison.Ordinal), "Descriptor arrays report their exact remaining integration.");
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1") return;
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var descriptions = new RDUniform[7];
        for (var i = 0; i < descriptions.Length; i++)
        {
            descriptions[i] = new RDUniform { Binding = i + 1, UniformType = UniformType.SamplerWithTextureBuffer };
            descriptions[i].AddID(sampler); descriptions[i].AddID(device.TextureBufferCreate(1, DataFormat.R32SFLOAT, Words(BitConverter.SingleToUInt32Bits(i + 1))));
        }
        var inputs = device.UniformSetCreate(descriptions, shader, 3);
        foreach (var description in descriptions) { description.ClearIDs(); description.Dispose(); }
        var output = device.StorageBufferCreate(4); var outputs = Set(device, shader, 4, 9, UniformType.StorageBuffer, output);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, inputs, 3); device.ComputeListBindUniformSet(list, outputs, 4); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        Check(MemoryMarshal.Cast<byte, float>(device.BufferGetData(output).AsSpan())[0] == 28, "Eight actual native buffer slots and copied many-to-one sampler descriptions execute.");
    }

    private static void Specialization(RenderingDevice device)
    {
        using var source = new RDShaderSource { SourceCompute = """
            #version 450
            layout(local_size_x=1)in;
            layout(constant_id=13)const uint selected=0;
            layout(set=3,binding=7)uniform samplerBuffer inputValues;
            layout(set=4,binding=2,std430)buffer Output{vec4 result;};
            void main(){result=texelFetch(inputValues,int(selected));}
            """ };
        using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1") return;
        var original = module.BytecodeCompute; var shader = device.ShaderCreateFromSPIRV(module);
        using var selected = new RDPipelineSpecializationConstant<uint> { ConstantID = 13, Value = 1 }; var pipeline = device.ComputePipelineCreate(shader, [selected]); selected.Value = 0;
        var input = device.TextureBufferCreate(2, DataFormat.R32SFLOAT, Words(0x3f800000, 0x40000000)); using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state);
        var inputs = Set(device, shader, 3, 7, UniformType.SamplerWithTextureBuffer, sampler, input); var output = device.StorageBufferCreate(16); var outputs = Set(device, shader, 4, 2, UniformType.StorageBuffer, output);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, inputs, 3); device.ComputeListBindUniformSet(list, outputs, 4); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        Check(MemoryMarshal.Cast<byte, float>(device.BufferGetData(output).AsSpan())[0] == 2 && original.SequenceEqual(module.BytecodeCompute), "Copied specialized indices execute without changing public texel module bytes.");
    }
}
