using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceTexelTests
{
    private static byte[] Words(params uint[] values) => MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
    private static void Formats(RenderingDevice device, ShaderLanguage language)
    {
        (DataFormat Format, byte[] Data, int Scalar, double[] Expected)[] cases = [
            (DataFormat.R8UNORM, [128], 0, [128.0 / 255, 0, 0, 1]),
            (DataFormat.R8G8SNORM, [128, 127], 0, [-1, 1, 0, 1]),
            (DataFormat.R8G8B8UNORM, [255, 128, 0], 0, [1, 128.0 / 255, 0, 1]),
            (DataFormat.B8G8R8A8UNORM, [1, 2, 255, 128], 0, [1, 2.0 / 255, 1.0 / 255, 128.0 / 255]),
            (DataFormat.R16G16B16SFLOAT, [0, 56, 0, 192, 0, 60], 0, [.5, -2, 1, 1]),
            (DataFormat.R16UNORM, [0, 128], 0, [32768.0 / 65535, 0, 0, 1]),
            (DataFormat.R16SNORM, [0, 128], 0, [-1, 0, 0, 1]),
            (DataFormat.R32G32SFLOAT, Words(0x3f000000, 0xc0000000), 0, [.5, -2, 0, 1]),
            (DataFormat.R4G4B4A4UNORMPACK16, [0x0f, 0xf8], 0, [1, 8.0 / 15, 0, 1]),
            (DataFormat.A2B10G10R10UNORMPACK32, Words(0xc00003ff), 0, [1, 0, 0, 1]),
            (DataFormat.B10G11R11UFLOATPACK32, Words((14u << 6) | ((15u << 6) << 11) | ((16u << 5) << 22)), 0, [.5, 1, 2, 1]),
            (DataFormat.E5B9G9R9UFLOATPACK32, Words((15u << 27) | (64u << 18) | (128u << 9) | 256), 0, [.5, .25, .125, 1]),
            (DataFormat.R8G8B8A8SRGB, [128, 0, 255, 128], 0, [.2158605, 0, 1, 128.0 / 255]),
            (DataFormat.R8G8USCALED, [201, 17], 0, [201, 17, 0, 1]),
            (DataFormat.R8G8SSCALED, [254, 128], 0, [-2, -128, 0, 1]),
            (DataFormat.R8G8B8UINT, [201, 17, 255], 2, [201, 17, 255, 1]),
            (DataFormat.R8G8B8SINT, [254, 128, 127], 1, [-2, -128, 127, 1]),
            (DataFormat.R16G16UINT, [255, 255, 0, 128], 2, [65535, 32768, 0, 1]),
            (DataFormat.R16G16SINT, [254, 255, 0, 128], 1, [-2, -32768, 0, 1]),
            (DataFormat.R32G32B32UINT, Words(uint.MaxValue, 0x80000000, 17), 2, [4294967295, 2147483648, 17, 1]),
            (DataFormat.R32G32B32SINT, Words(uint.MaxValue, 0x80000000, 17), 1, [-1, -2147483648, 17, 1])
        ];
        foreach (var scalar in new[] { 0, 1, 2 })
        {
            var element = new[] { "float4", "int4", "uint4" }[scalar]; var prefix = new[] { "", "i", "u" }[scalar];
            var expression = scalar == 0 ? "floatBitsToUint(texelFetch(inputValues,1))" : "uvec4(texelFetch(inputValues,1))";
            using var source = new RDShaderSource
            {
                Language = language,
                SourceCompute = language == ShaderLanguage.GLSL ?
                "#version 450\nlayout(local_size_x=1) in; layout(set=3,binding=7) uniform " + prefix + "samplerBuffer inputValues; layout(set=4,binding=2,std430) buffer Output { uvec4 result[]; }; void main(){ result[0]=" + expression + "; result[1]=uvec4(textureSize(inputValues)); }" :
                "[[vk::binding(7,3)]] Buffer<" + element + "> inputValues; [[vk::binding(2,4)]] RWStructuredBuffer<uint4> result; [numthreads(1,1,1)] void main(){uint count; inputValues.GetDimensions(count);result[0]=asuint(inputValues.Load(1));result[1]=count;}"
            };
            using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute); Export(module, "Formats" + scalar + language);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1") continue;
            var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(32); var outputSet = Set(device, shader, 4, 2, UniformType.StorageBuffer, output);
            using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
            foreach (var item in cases.Where(c => c.Scalar == scalar))
            {
                var data = item.Data.Concat(item.Data).ToArray(); var input = device.TextureBufferCreate(2, item.Format, data);
                var inputSet = language == ShaderLanguage.GLSL ? Set(device, shader, 3, 7, UniformType.SamplerWithTextureBuffer, sampler, input) : Set(device, shader, 3, 7, UniformType.TextureBuffer, input);
                var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, inputSet, 3); device.ComputeListBindUniformSet(list, outputSet, 4); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
                var result = device.BufferGetData(output); var words = MemoryMarshal.Cast<byte, uint>(result.AsSpan()); var floats = MemoryMarshal.Cast<byte, float>(result.AsSpan()); var signed = MemoryMarshal.Cast<byte, int>(result.AsSpan());
                for (var lane = 0; lane < 4; lane++)
                {
                    double actual = scalar == 0 ? floats[lane] : scalar == 1 ? signed[lane] : (double)words[lane];
                    Check(Math.Abs(actual - item.Expected[lane]) < .00001, language + ": formatted " + item.Format + " lane " + lane + " actual=" + actual);
                }
                Check(words[4] == 2 && device.BufferGetData(input).SequenceEqual(data), "Format metadata and query preserve original encoded bytes.");
                device.FreeRID(inputSet); device.FreeRID(input);
            }
            device.FreeRID(outputSet); device.FreeRID(output); device.FreeRID(pipeline); device.FreeRID(shader); device.FreeRID(sampler);
        }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") != "1") Console.WriteLine(language + ": 21 independent format vectors across float/signed/unsigned reusable pipelines passed.");
    }
}
