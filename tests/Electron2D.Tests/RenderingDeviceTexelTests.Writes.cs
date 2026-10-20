using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceTexelTests
{
    private static void Writes(RenderingDevice device, ShaderLanguage language)
    {
        using var source = new RDShaderSource { Language = language, SourceCompute = language == ShaderLanguage.GLSL ? """
            #version 450
            layout(local_size_x=32) in;
            layout(set=3,binding=7,rgba8) uniform imageBuffer normalized;
            layout(set=5,binding=9,r32ui) uniform coherent uimageBuffer counter;
            void main() {
                int index=int(gl_GlobalInvocationID.x);
                imageStore(normalized,index,vec4(0,1,0.5,1));
                imageAtomicAdd(counter,0,1);
                memoryBarrierImage();
            }
            """ : """
            [[vk::binding(7,3)]] [[vk::image_format("rgba8")]] RWBuffer<float4> normalized;
            [[vk::binding(9,5)]] RWBuffer<uint> counter;
            [numthreads(32,1,1)] void main(uint3 index : SV_DispatchThreadID) { normalized[index.x]=float4(0,1,0.5,1); InterlockedAdd(counter[0],1); DeviceMemoryBarrier(); }
            """ };
        using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute);
        Export(module, "Writes" + language);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1") return;
        var normalized = device.TextureBufferCreate(32, DataFormat.R8G8B8A8UNORM); var counter = device.TextureBufferCreate(1, DataFormat.R32UINT);
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        var wrongFormat = device.TextureBufferCreate(32, DataFormat.R32G32B32A32SFLOAT); Reject<ArgumentException>(() => Set(device, shader, 3, 7, UniformType.ImageBuffer, wrongFormat)); device.FreeRID(wrongFormat);
        var first = Set(device, shader, 3, 7, UniformType.ImageBuffer, normalized); var second = Set(device, shader, 5, 9, UniformType.ImageBuffer, counter);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, first, 3); device.ComputeListBindUniformSet(list, second, 5);
        device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        var pixels = device.BufferGetData(normalized);
        for (var i = 0; i < 32; i++) Check(pixels[i * 4] == 0 && pixels[i * 4 + 1] == 255 && pixels[i * 4 + 2] == 128 && pixels[i * 4 + 3] == 255, "Image stores preserve normalized byte encoding.");
        Check(MemoryMarshal.Cast<byte, uint>(device.BufferGetData(counter).AsSpan())[0] == 32, "Concurrent image atomics reach the original encoded buffer.");
    }
}
