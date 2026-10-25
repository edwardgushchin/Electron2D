using System.Runtime.InteropServices;
using Electron2D;
using IOPath = System.IO.Path;
using static Electron2D.RenderingDevice;

internal static partial class TextureSwizzleRuntimeChecks
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static RID Set(RenderingDevice device, RID shader, uint set, int binding, UniformType type, params RID[] resources)
    {
        using var uniform = new RDUniform { Binding = binding, UniformType = type }; foreach (var rid in resources) uniform.AddID(rid);
        return device.UniformSetCreate([uniform], shader, set);
    }
    private static RDShaderSPIRV Module(string folder, string name) => new() { BytecodeCompute = File.ReadAllBytes(IOPath.Combine(folder, name + ".comp.glsl.spv")) };
    private static void Equal(ReadOnlySpan<float> actual, ReadOnlySpan<float> expected, string operation, float tolerance = .0001f)
    {
        for (var i = 0; i < expected.Length; i++) if (MathF.Abs(actual[i] - expected[i]) >= tolerance) throw new InvalidOperationException($"{operation} lane {i}: {actual[i]}, expected {expected[i]}.");
    }
    private static void EdgeCases(string folder)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var module = Module(folder, "edges"); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        var output = device.StorageBufferCreate(64); var target = Set(device, shader, 1, 0, UniformType.StorageBuffer, output); var result = new byte[64];
        using var state = new RDSamplerState { MinFilter = SamplerFilter.Linear, MagFilter = SamplerFilter.Linear, MIPFilter = SamplerFilter.Linear, MaxLOD = 1 }; var sampler = device.SamplerCreate(state);
        using var view = new RDTextureView { SwizzleR = TextureSwizzle.A, SwizzleG = TextureSwizzle.B, SwizzleB = TextureSwizzle.R, SwizzleA = TextureSwizzle.One };
        using var identity = new RDTextureView();
        using var format = new RDTextureFormat { Width = 2, Height = 2, MIPMaps = 2, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.CPURead | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | TextureUsageBits.CanUpdate | TextureUsageBits.Storage | TextureUsageBits.ColorAttachment };
        float[] values = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 21, 22, 23, 24]; var bytes = MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
        var image = device.TextureCreate(format, view, [bytes]); var plain = device.TextureCreate(format, identity, [bytes]); var sources = Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, image);
        void Dispatch(RID source)
        {
            var list = device.ComputeListBegin(); try { device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, source, 0); device.ComputeListBindUniformSet(list, target, 1); device.ComputeListDispatch(list, 1, 1, 1); } finally { device.ComputeListEnd(); }
            device.BufferGetData(output, result.AsSpan());
        }
        Dispatch(sources); Equal(MemoryMarshal.Cast<byte, float>(result), [10, 9, 7, 1, 24, 23, 21, 1, 4, 3, 1, 1, 2, 2, 2, 0], "Filtering and explicit mip");
        Check(device.TextureGetData(image, 0).SequenceEqual(bytes), "Readback retains raw storage, independent of sampled maps.");
        device.TextureCopy(image, plain, Vector3.Zero, Vector3.Zero, new(2, 2, 1), 0, 0, 0, 0); Check(device.TextureGetData(plain, 0).SequenceEqual(bytes), "Copies preserve raw image components.");
        var changed = bytes.ToArray(); MemoryMarshal.Cast<byte, float>(changed.AsSpan())[0] = 31; device.TextureUpdate(image, 0, changed); Check(device.TextureGetData(image, 0).SequenceEqual(changed), "Updates write raw component storage.");
        device.TextureClear(image, new(.1f, .2f, .3f, .4f), 0, 2, 0, 1); Dispatch(sources); Equal(MemoryMarshal.Cast<byte, float>(result).Slice(0, 4), [.4f, .3f, .1f, 1], "Clear writes storage before sampling maps");
        Reject<ArgumentException>(() => device.FramebufferCreate([image]));
        using var storageModule = Module(folder, "storage"); var storageShader = device.ShaderCreateFromSPIRV(storageModule); Reject<ArgumentException>(() => Set(device, storageShader, 0, 0, UniformType.Image, image));
        Check(Set(device, storageShader, 0, 0, UniformType.Image, plain).IsValid(), "Identity storage images remain usable.");
        using var inputModule = new RDShaderSPIRV { BytecodeVertex = File.ReadAllBytes(IOPath.Combine(folder, "graphics.vert.glsl.spv")), BytecodeFragment = File.ReadAllBytes(IOPath.Combine(folder, "input.frag.glsl.spv")) };
        var inputShader = device.ShaderCreateFromSPIRV(inputModule); format.MIPMaps = 1; format.UsageBits |= TextureUsageBits.InputAttachment; var inputImage = device.TextureCreate(format, view, [bytes[..64]]);
        Reject<ArgumentException>(() => Set(device, inputShader, 1, 0, UniformType.InputAttachment, inputImage)); device.FreeRID(inputImage);
        var plainSet = Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, plain); Dispatch(plainSet); Equal(MemoryMarshal.Cast<byte, float>(result).Slice(0, 4), [7, 8, 9, 10], "Identity pipeline after mapped pipeline"); Dispatch(sources);
        device.FreeRID(image); Reject<ArgumentException>(() => Dispatch(sources));
        Depth(device, folder); Capacity(device, folder); Formats(device, folder); Variants(device, folder);
        Console.WriteLine("Swizzle edges: filtering/mips, raw transfer/clear, identity restoration, storage/framebuffer guards, freed images, depth and uniform capacity passed.");
    }
    private static void Depth(RenderingDevice device, string folder)
    {
        using var module = Module(folder, "depth"); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(64); var target = Set(device, shader, 1, 0, UniformType.StorageBuffer, output);
        using var compare = new RDSamplerState { EnableCompare = true, CompareOp = CompareOperator.Greater }; using var regular = new RDSamplerState(); var a = device.SamplerCreate(compare); var b = device.SamplerCreate(regular);
        using var format = new RDTextureFormat { Format = DataFormat.D32SFLOAT, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.DepthStencilAttachment }; var data = new byte[64];
        foreach (var swizzle in Enum.GetValues<TextureSwizzle>().Where(v => v != TextureSwizzle.Max))
        {
            using var view = new RDTextureView { SwizzleR = swizzle, SwizzleG = TextureSwizzle.A, SwizzleB = TextureSwizzle.R, SwizzleA = TextureSwizzle.One }; var image = device.TextureCreate(format, view);
            using var shadow = new RDUniform { Binding = 0, UniformType = UniformType.SamplerWithTexture }; shadow.AddID(a); shadow.AddID(image); using var raw = new RDUniform { Binding = 1, UniformType = UniformType.SamplerWithTexture }; raw.AddID(b); raw.AddID(image); var inputs = device.UniformSetCreate([shadow, raw], shader, 0);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, inputs, 0); device.ComputeListBindUniformSet(list, target, 1); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, data.AsSpan()); var values = MemoryMarshal.Cast<byte, float>(data);
            var mapped = swizzle is TextureSwizzle.Identity or TextureSwizzle.R or TextureSwizzle.A or TextureSwizzle.One ? 1f : 0f; for (var i = 0; i < 8; i++) Check(values[i] == mapped, "Depth comparison precedes component mapping, including gather: " + swizzle);
            Equal(values.Slice(8, 4), [swizzle is TextureSwizzle.One or TextureSwizzle.A ? 1 : 0, 1, 0, 1], "Depth fetch expands before mapping"); Equal(values.Slice(12, 4), [1, 1, 1, 0], "Depth query remains unchanged"); device.FreeRID(inputs); device.FreeRID(image);
        }
    }
    private static void Capacity(RenderingDevice device, string folder)
    {
        using var format = new RDTextureFormat { Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling }; using var view = new RDTextureView { SwizzleR = TextureSwizzle.A, SwizzleG = TextureSwizzle.B, SwizzleB = TextureSwizzle.R, SwizzleA = TextureSwizzle.One }; float[] color = [.1f, .2f, .3f, .4f]; var image = device.TextureCreate(format, view, [MemoryMarshal.AsBytes(color.AsSpan()).ToArray()]); using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var output = device.StorageBufferCreate(16); var data = new byte[16];
        using var module = Module(folder, "capacity"); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var images = Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, image); var target = Set(device, shader, 2, 0, UniformType.StorageBuffer, output); var values = new List<RDUniform>();
        try
        {
            float[] weight = [.25f, .25f, .25f, .25f]; for (var i = 0; i < 4; i++) { var buffer = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(weight.AsSpan())); var uniform = new RDUniform { Binding = i, UniformType = UniformType.UniformBuffer }; uniform.AddID(buffer); values.Add(uniform); }
            var uniforms = device.UniformSetCreate(values.ToArray(), shader, 1);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, images, 0); device.ComputeListBindUniformSet(list, uniforms, 1); device.ComputeListBindUniformSet(list, target, 2); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, data.AsSpan()); Equal(MemoryMarshal.Cast<byte, float>(data), [.4f, .3f, .1f, 1], "All four native uniform slots remain available");
        }
        finally { foreach (var value in values) value.Dispose(); }
        using var specialized = Module(folder, "specialized"); shader = device.ShaderCreateFromSPIRV(specialized); using var constant = new RDPipelineSpecializationConstant<float> { ConstantID = 7, Value = 2 }; pipeline = device.ComputePipelineCreate(shader, [constant]); images = Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, image); target = Set(device, shader, 1, 0, UniformType.StorageBuffer, output); float[] push = [.5f, .5f, .5f, .5f]; var indirect = device.StorageBufferCreate(12, MemoryMarshal.AsBytes(new uint[] { 1, 1, 1 }.AsSpan()), StorageBufferUsage.DispatchIndirect);
        var next = device.ComputeListBegin(); device.ComputeListBindComputePipeline(next, pipeline); device.ComputeListBindUniformSet(next, images, 0); device.ComputeListBindUniformSet(next, target, 1); device.ComputeListSetPushConstant(next, MemoryMarshal.AsBytes(push.AsSpan()), 16); device.ComputeListDispatchIndirect(next, indirect, 0); device.ComputeListEnd(); device.BufferGetData(output, data.AsSpan()); Equal(MemoryMarshal.Cast<byte, float>(data), [.4f, .3f, .1f, 1], "Specialization, push and indirect dispatch retain sampled variants");
    }
    private static void Formats(RenderingDevice device, string folder)
    {
        using var module = Module(folder, "edges"); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(64); var target = Set(device, shader, 1, 0, UniformType.StorageBuffer, output); using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var data = new byte[64];
        foreach (var format in new[] { DataFormat.R8UNORM, DataFormat.R8G8UNORM, DataFormat.R8G8B8A8SRGB })
        {
            using var description = new RDTextureFormat { Format = format, UsageBits = TextureUsageBits.Sampling }; using var view = new RDTextureView { SwizzleR = TextureSwizzle.A, SwizzleG = TextureSwizzle.B, SwizzleB = TextureSwizzle.R, SwizzleA = TextureSwizzle.One }; byte[] raw = format == DataFormat.R8UNORM ? [128] : format == DataFormat.R8G8UNORM ? [128, 64] : [128, 64, 32, 192]; var image = device.TextureCreate(description, view, [raw]); var images = Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, image);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, images, 0); device.ComputeListBindUniformSet(list, target, 1); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, data.AsSpan()); var value = MemoryMarshal.Cast<byte, float>(data); var srgb = format == DataFormat.R8G8B8A8SRGB; var r = srgb ? MathF.Pow((128f / 255 + .055f) / 1.055f, 2.4f) : 128f / 255; var blue = srgb ? MathF.Pow((32f / 255 + .055f) / 1.055f, 2.4f) : 0; Equal(value.Slice(0, 4), [srgb ? 192f / 255 : 1, blue, r, 1], "Format expansion and sRGB conversion precede swizzle", srgb ? .002f : .0001f);
        }
    }
    private static void Variants(RenderingDevice device, string folder)
    {
        using var module = Module(folder, "edges"); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var format = new RDTextureFormat { Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling };
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var output = device.StorageBufferCreate(64); var target = Set(device, shader, 1, 0, UniformType.StorageBuffer, output);
        float[] color = [.1f, .2f, .3f, .4f]; var bytes = MemoryMarshal.AsBytes(color.AsSpan()).ToArray(); var data = new byte[64];
        TextureSwizzle[] red = [TextureSwizzle.Zero, TextureSwizzle.One, TextureSwizzle.G, TextureSwizzle.B, TextureSwizzle.A, TextureSwizzle.Identity];
        TextureSwizzle[] green = [TextureSwizzle.Zero, TextureSwizzle.One, TextureSwizzle.R, TextureSwizzle.B, TextureSwizzle.A, TextureSwizzle.Identity];
        float Value(TextureSwizzle value, int lane) => value switch { TextureSwizzle.Zero => 0, TextureSwizzle.One => 1, TextureSwizzle.Identity => color[lane], _ => color[(int)value - (int)TextureSwizzle.R] };
        for (var step = 0; step < 76; step++)
        {
            var index = step % 72;
            using var view = new RDTextureView { SwizzleR = red[index % 6], SwizzleG = green[index / 6 % 6], SwizzleB = index < 36 ? TextureSwizzle.Zero : TextureSwizzle.One, SwizzleA = TextureSwizzle.One };
            var image = device.TextureCreate(format, view, [bytes]); var source = Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, image);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, source, 0); device.ComputeListBindUniformSet(list, target, 1); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, data.AsSpan());
            Equal(MemoryMarshal.Cast<byte, float>(data).Slice(0, 4), [Value(view.SwizzleR, 0), Value(view.SwizzleG, 1), Value(view.SwizzleB, 2), 1], "Native variant eviction and recreation");
            device.FreeRID(source); device.FreeRID(image);
        }
        device.FreeRID(pipeline); device.FreeRID(shader);
        Console.WriteLine("Swizzle native cache: 72 distinct mappings and four recreated evicted variants retained exact values.");
    }
    private static void Graphics(string folder)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice(); using var module = new RDShaderSPIRV { BytecodeVertex = File.ReadAllBytes(IOPath.Combine(folder, "graphics.vert.glsl.spv")), BytecodeFragment = File.ReadAllBytes(IOPath.Combine(folder, "graphics.frag.glsl.spv")) }; var shader = device.ShaderCreateFromSPIRV(module);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R32G32B32A32SFLOAT, TextureType = TextureType.TextureArray, ArrayLayers = 2, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var identity = new RDTextureView(); var output = device.TextureCreate(format, identity); using var pass = new RDFramebufferPass { ColorAttachments = [-1, 0] }; var framebuffer = device.FramebufferCreateMultipass([output], [pass], viewCount: 2);
        format.Width = format.Height = 1; format.TextureType = TextureType.Texture; format.ArrayLayers = 1; format.UsageBits = TextureUsageBits.Sampling; float[] color = [.2f, .4f, .6f, .8f]; var bytes = MemoryMarshal.AsBytes(color.AsSpan()).ToArray(); using var view = new RDTextureView { SwizzleR = TextureSwizzle.A, SwizzleG = TextureSwizzle.B, SwizzleB = TextureSwizzle.R, SwizzleA = TextureSwizzle.One }; var mapped = device.TextureCreate(format, view, [bytes]); var plain = device.TextureCreate(format, identity, [bytes]); using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var vertex = Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, mapped); var fragment = Set(device, shader, 1, 0, UniformType.SamplerWithTexture, sampler, mapped); var identityFragment = Set(device, shader, 1, 0, UniformType.SamplerWithTexture, sampler, plain);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment, attachment] }; using var constant = new RDPipelineSpecializationConstant<float> { ConstantID = 7, Value = 2 }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [constant]); float[] push = [.5f, .5f, .5f, .5f]; var data = new byte[64];
        void Draw(bool alternate)
        {
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, vertex, 0); device.DrawListBindUniformSet(list, alternate ? identityFragment : fragment, 1); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 16); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            for (uint layer = 0; layer < 2; layer++) { device.TextureGetData(output, layer, data.AsSpan()); var v = layer + 1; Equal(MemoryMarshal.Cast<byte, float>(data).Slice(0, 4), alternate ? [.16f * v, .24f * v, .12f * v + layer * .1f, .8f * v] : [.64f * v, .36f * v, .04f * v + layer * .1f, v], "Sparse/multiview vertex and fragment mappings"); }
        }
        for (var i = 0; i < 16; i++) Draw((i & 1) != 0); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw((i & 1) != 0); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared sampled graphics uses zero managed owner bytes."); device.FreeRID(pipeline); device.FreeRID(shader);
        Console.WriteLine("Swizzle graphics: sparse output, two views, independent vertex/fragment maps, specialization/push, descriptor changes and 32 zero-allocation draws passed.");
    }
}
