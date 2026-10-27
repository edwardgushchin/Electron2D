using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceTexelTests
{
    private static void NativeFormatParity(RenderingDevice device)
    {
        (DataFormat Format, string Layout, string Scalar, string Value)[] cases = [
            (DataFormat.R8G8B8A8UNORM, "rgba8", "", "vec4(-1,0.5,1.5,0.25)"),
            (DataFormat.R8G8B8A8SNORM, "rgba8_snorm", "", "vec4(-1,-0.5,0.5,1)"),
            (DataFormat.R16G16B16A16SFLOAT, "rgba16f", "", "vec4(0.3333333,-2,65504,0.000000059604645)"),
            (DataFormat.R32G32B32A32SFLOAT, "rgba32f", "", "vec4(-1,0.5,1.5,0.25)"),
            (DataFormat.R8G8B8A8UINT, "rgba8ui", "u", "uvec4(300,0xfffffffeu,0x80000000u,256)"),
            (DataFormat.R8G8B8A8SINT, "rgba8i", "i", "ivec4(300,-300,-128,127)"),
            (DataFormat.A2B10G10R10UNORMPACK32, "rgb10_a2", "", "vec4(0.5,1,0,0.75)"),
            (DataFormat.B10G11R11UFLOATPACK32, "r11f_g11f_b10f", "", "vec4(0.5,1,2,1)")
        ];
        foreach (var item in cases)
        {
            var usage = TextureUsageBits.Storage | TextureUsageBits.CanCopyFrom;
            if (!device.TextureIsFormatSupportedForUsage(item.Format, usage)) continue;
            using var source = new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1)in;layout(set=3,binding=7," + item.Layout + ")uniform " + item.Scalar + "imageBuffer texels; layout(set=4,binding=8," + item.Layout + ")uniform " + item.Scalar + "image2D nativeImage; void main(){ imageStore(texels,0," + item.Value + ");imageStore(nativeImage,ivec2(0)," + item.Value + "); }" };
            using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute); Export(module, "Oracle" + item.Layout);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1") continue;
            var texels = device.TextureBufferCreate(1, item.Format);
            using var format = new RDTextureFormat { Format = item.Format, UsageBits = usage }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view);
            var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            var first = Set(device, shader, 3, 7, UniformType.ImageBuffer, texels); var second = Set(device, shader, 4, 8, UniformType.Image, image);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, first, 3); device.ComputeListBindUniformSet(list, second, 4); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
            var actual = device.BufferGetData(texels); var expected = device.TextureGetData(image, 0);
            var normalized = item.Format is DataFormat.R8G8B8A8UNORM or DataFormat.R8G8B8A8SNORM or DataFormat.A2B10G10R10UNORMPACK32;
            Check(normalized ? NormalizedStore(item.Format, actual) && NormalizedStore(item.Format, expected) : actual.SequenceEqual(expected), "Native image-store parity for " + item.Layout + ": texel=" + Convert.ToHexString(actual) + " native=" + Convert.ToHexString(expected));
            Console.WriteLine("Native store parity " + item.Layout + ": " + Convert.ToHexString(actual));
            foreach (var resource in new[] { first, second, pipeline, shader, texels, image }) device.FreeRID(resource);
        }
    }

    private static bool NormalizedStore(DataFormat format, ReadOnlySpan<byte> bytes)
    {
        // Native normalized conversion permits either adjacent integer; saturated endpoints stay exact.
        if (bytes.Length != 4) return false;
        if (format == DataFormat.R8G8B8A8UNORM)
            return bytes[0] == 0 && bytes[1] is 127 or 128 && bytes[2] == 255 && bytes[3] is 63 or 64;
        if (format == DataFormat.R8G8B8A8SNORM)
            return bytes[0] == 129 && bytes[1] is 192 or 193 && bytes[2] is 63 or 64 && bytes[3] == 127;
        var packed = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        return (packed & 1023) is 511 or 512 && ((packed >> 10) & 1023) == 1023 && ((packed >> 20) & 1023) == 0 && (packed >> 30) is 2 or 3;
    }

    private static void Atomics(RenderingDevice device)
    {
        foreach (var signed in new[] { false, true })
        {
            var scalar = signed ? "i" : "u"; var word = signed ? "int" : "uint"; var layout = signed ? "r32i" : "r32ui";
            var operations = signed ? "imageAtomicMin(values,0,-20);imageAtomicMax(values,1,5);imageAtomicAdd(values,2,-5);imageAtomicExchange(values,3,-2147483647-1);imageAtomicCompSwap(values,4,-10,7);" :
                "imageAtomicAdd(values,0,5);imageAtomicMin(values,1,3);imageAtomicMax(values,2,20);imageAtomicAnd(values,3,6);imageAtomicOr(values,4,3);imageAtomicXor(values,5,3);imageAtomicExchange(values,6,12);imageAtomicCompSwap(values,7,10,7);";
            using var source = new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1)in;layout(set=3,binding=7," + layout + ")uniform " + scalar + "imageBuffer values;void main(){" + operations + "}" };
            using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute); Export(module, "Atomics" + word);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1") continue;
            var texels = device.TextureBufferCreate(8, signed ? DataFormat.R32SINT : DataFormat.R32UINT, Words(Enumerable.Repeat(signed ? unchecked((uint)-10) : 10u, 8).ToArray()));
            var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var set = Set(device, shader, 3, 7, UniformType.ImageBuffer, texels);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 3); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
            var result = MemoryMarshal.Cast<byte, int>(device.BufferGetData(texels).AsSpan());
            int[] expected = signed ? [-20, 5, -15, int.MinValue, 7, -10, -10, -10] : [15, 3, 20, 2, 11, 9, 12, 7];
            Check(result.SequenceEqual(expected), "Signed/unsigned image atomic operations preserve their value and memory semantics.");
            foreach (var resource in new[] { set, pipeline, shader, texels }) device.FreeRID(resource);
        }
    }
}
