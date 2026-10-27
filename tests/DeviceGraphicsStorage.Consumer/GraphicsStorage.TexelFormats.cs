using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static byte[] Words(params uint[] values) => MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
    private static byte[] Formatless(byte[] bytes)
    {
        var words = MemoryMarshal.Cast<byte, uint>(bytes).ToArray(); var capability = false;
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var op = words[at] & 65535;
            if (op == 25 && words[at + 3] == 5) words[at + 8] = 0;
            capability |= op == 17 && words[at + 1] == 56;
        }
        if (!capability) { var expanded = new uint[words.Length + 2]; words.AsSpan(0, 5).CopyTo(expanded); expanded[5] = (2u << 16) | 17; expanded[6] = 56; words.AsSpan(5).CopyTo(expanded.AsSpan(7)); words = expanded; }
        return MemoryMarshal.AsBytes(words.AsSpan()).ToArray();
    }
    private static bool Encoded(DataFormat format, ReadOnlySpan<byte> actual, ReadOnlySpan<byte> expected)
    {
        if (actual.SequenceEqual(expected)) return true;
        if (format == DataFormat.R16SNORM) return Math.Abs((int)BinaryPrimitives.ReadInt16LittleEndian(actual) - BinaryPrimitives.ReadInt16LittleEndian(expected)) <= 1;
        if (format == DataFormat.R8G8SNORM) return Math.Abs((int)unchecked((sbyte)actual[0]) - unchecked((sbyte)expected[0])) <= 1 && actual[1] == expected[1];
        if (format == DataFormat.R8G8B8A8SRGB) return Math.Abs(actual[0] - expected[0]) <= 1 && actual[1..].SequenceEqual(expected[1..]);
        return false;
    }
    private static void TexelFormats(RenderingDevice device, string language)
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
        using var format = new RDTextureFormat { Width = 4, Height = 4, UsageBits = TextureUsageBits.ColorAttachment }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        foreach (var scalar in new[] { 0, 1, 2 })
        {
            var name = new[] { "float", "signed", "unsigned" }[scalar]; using var original = Code("texel-" + name, language); using var module = new RDShaderSPIRV { BytecodeVertex = Formatless(original.BytecodeVertex), BytecodeFragment = Formatless(original.BytecodeFragment) };
            var directory = Environment.GetEnvironmentVariable("ELECTRON2D_GRAPHICS_TEXEL_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); File.WriteAllBytes(System.IO.Path.Combine(directory, "formatless-" + name + ".vert." + language + ".spv"), module.BytecodeVertex); File.WriteAllBytes(System.IO.Path.Combine(directory, "formatless-" + name + ".frag." + language + ".spv"), module.BytecodeFragment); }
            var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(module)); var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
            foreach (var item in cases.Where(c => c.Scalar == scalar))
            {
                var initial = new byte[item.Data.Length * 21]; initial.AsSpan().Fill(0xa5); var texels = device.TextureBufferCreate(21, item.Format, initial); var set = TexelSet(device, shader, 3, 7, UniformType.ImageBuffer, texels); var push = new uint[4]; for (var lane = 0; lane < 4; lane++) push[lane] = scalar == 0 ? BitConverter.SingleToUInt32Bits((float)item.Expected[lane]) : unchecked((uint)(long)item.Expected[lane]);
                void Draw() { var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 3); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 16); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); }
                Draw(); var data = device.BufferGetData(texels); Check(data.Length == initial.Length, "Formatted readback excludes metadata and tail alignment bytes.");
                for (var element = 0; element < 21; element++) { var bytes = data.AsSpan(element * item.Data.Length, item.Data.Length); Check(element is 3 or 20 ? bytes.SequenceEqual(initial.AsSpan(element * item.Data.Length, item.Data.Length)) : Encoded(item.Format, bytes, item.Data), language + ": independent graphics encoded vector " + item.Format + " element " + element + " actual=" + Convert.ToHexString(bytes)); }
                for (var i = 0; i < 8; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 16; i++) { Draw(); device.BufferGetData(texels, data.AsSpan()); }
                Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared changing packed graphics/readback allocates zero owner bytes.");
                device.FreeRID(set); device.FreeRID(texels);
            }
            device.FreeRID(pipeline); device.FreeRID(shader);
        }
        device.FreeRID(target); device.FreeRID(image); Console.WriteLine("PASS " + language + ": 21 independent graphics store formats, signed/unsigned/float, packed-field neighbors, odd capacity/tail and zero owner bytes.");
    }
}
