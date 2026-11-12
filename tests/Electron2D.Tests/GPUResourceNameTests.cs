using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using DriverResource = Electron2D.RenderingDevice.DriverResource;

internal static partial class GPUResourceNameTests
{
    private const uint Buffer = 9, Image = 10, ImageView = 14, ShaderModule = 15, Pipeline = 19, Sampler = 21;

    internal static void Run(string profile)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        byte[] bytes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        var storage = device.StorageBufferCreate(16, bytes);
        if (profile == "unavailable")
        {
            Reject<NotSupportedException>(() => device.SetResourceName(storage, "unsupported"));
            Check(device.BufferGetData(storage).SequenceEqual(bytes), "Unsupported naming preserves the buffer.");
            Console.WriteLine("GPU resource naming reports an unavailable backend.");
            return;
        }

        if (profile == "direct3d12")
        {
            CheckDirect3D12(device, storage, bytes);
            return;
        }
        Check(profile == "vulkan", "Unknown resource name test profile.");
        var native = (nint)typeof(RenderingDevice).GetProperty("Device", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(device)!;
        Check(OpenTrace(native) == 1, "Install the native Vulkan name observer.");
        try
        {
            Name(storage, Buffer, "GPU–данные");
            Name(storage, Buffer, "replacement");
            Name(storage, Buffer, "");
            Name(device.UniformBufferCreate(16, creationBits: RenderingDevice.BufferCreationBits.AsStorage), Buffer, "native uniform");
            Name(device.TextureBufferCreate(4, RenderingDevice.DataFormat.R32UINT), Buffer, "texel");
            Name(device.VertexBufferCreate(16), Buffer, "vertex");
            Name(device.IndexBufferCreate(3, RenderingDevice.IndexBufferFormat.UInt16), Buffer, "index");
            using var sampler = new RDSamplerState();
            Name(device.SamplerCreate(sampler), Sampler, "sampler");

            using var textureFormat = new RDTextureFormat
            {
                Width = 8,
                Height = 8,
                Format = RenderingDevice.DataFormat.R8G8B8A8UNORM,
                UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.ColorAttachment,
            };
            using var view = new RDTextureView();
            var texture = device.TextureCreate(textureFormat, view);
            var shared = device.TextureCreateShared(view, texture);
            Name(texture, Image, "image");
            Name(shared, ImageView, "shared view");
            var framebuffer = device.FramebufferCreate([texture]);

            using var module = LoadCompute();
            var compute = device.ShaderCreateFromSPIRV(module);
            Name(compute, ShaderModule, "compute shader");
            Name(device.ComputePipelineCreate(compute), Pipeline, "compute pipeline");
            using var graphics = RenderingDeviceGraphicsTests.Load("DeviceProcedural");
            var shader = device.ShaderCreateFromSPIRV(graphics);
            Name(shader, ShaderModule, "graphics stages", 2);
            using var raster = new RDPipelineRasterizationState();
            using var attachment = new RDPipelineColorBlendStateAttachment();
            using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1,
                RenderingDevice.RenderPrimitive.Triangles, raster, blend);
            Name(pipeline, Pipeline, "graphics pipeline");

            Check(Expect(Buffer, "rejected", 1) == 1, "Configure the driver rejection.");
            Reject<InvalidOperationException>(() => device.SetResourceName(storage, "rejected"));
            Check(TraceCount() == 1, "A native refusal reaches the caller.");
            Name(storage, Buffer, "retry");
            Check(device.BufferGetData(storage).SequenceEqual(bytes), "Labels preserve the native buffer bytes.");

            using var foreign = device.CreateLocalDevice();
            var foreignID = foreign.StorageBufferCreate(16);
            Check(Expect(Buffer, "", 0) == 1, "Reset native call observation.");
            Reject<ArgumentNullException>(() => device.SetResourceName(storage, null!));
            Reject<ArgumentException>(() => device.SetResourceName(storage, "a\0b"));
            Reject<ArgumentException>(() => device.SetResourceName(foreignID, "foreign"));
            Reject<NotSupportedException>(() => device.SetResourceName(device.UniformBufferCreate(16), "host uniform"));
            Reject<NotSupportedException>(() => device.SetResourceName(framebuffer, "framebuffer"));
            Reject<NotSupportedException>(() => device.SetResourceName(device.ShaderCreatePlaceholder(), "placeholder"));
            Reject<InvalidOperationException>(() => Task.Run(() => device.SetResourceName(storage, "wrong thread")).GetAwaiter().GetResult());
            device.FreeRID(storage);
            Reject<ArgumentException>(() => device.SetResourceName(storage, "retired"));
            Check(TraceCount() == 0, "Invalid and unsupported calls do not submit native labels.");
        }
        finally { CloseTrace(); }
        device.Dispose();
        Reject<ObjectDisposedException>(() => device.SetResourceName(storage, "disposed"));
        Console.WriteLine("Vulkan buffer, image, view, sampler, shader and pipeline labels observed.");

        void Name(RID id, uint type, string name, int count = 1)
        {
            Check(Expect(type, name, 0) == 1, "Configure the native name observation.");
            device.SetResourceName(id, name);
            Check(TraceCount() == count, $"Vulkan received {count} matching labels for {name}.");
        }
    }

    private static void CheckDirect3D12(RenderingDevice device, RID storage, byte[] bytes)
    {
        Name(storage, DriverResource.Buffer, "GPU–данные 🌌");
        Name(storage, DriverResource.Buffer, "replacement");
        Name(storage, DriverResource.Buffer, "");
        Check(device.BufferGetData(storage).SequenceEqual(bytes), "Renaming preserves the native buffer bytes.");
        Name(device.TextureBufferCreate(4, RenderingDevice.DataFormat.R32UINT), DriverResource.Buffer, "texel");
        Name(device.VertexBufferCreate(16), DriverResource.Buffer, "vertex");
        Name(device.IndexBufferCreate(3, RenderingDevice.IndexBufferFormat.UInt16), DriverResource.Buffer, "index");

        using var textureFormat = new RDTextureFormat
        {
            Width = 8,
            Height = 8,
            Format = RenderingDevice.DataFormat.R8G8B8A8UNORM,
            UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.ColorAttachment |
                RenderingDevice.TextureUsageBits.CanCopyFrom,
        };
        using var view = new RDTextureView();
        var texture = device.TextureCreate(textureFormat, view);
        var shared = device.TextureCreateShared(view, texture);
        Name(texture, DriverResource.Texture, "color target");
        Reject<NotSupportedException>(() => device.SetResourceName(shared, "shared view"));
        CheckName(texture, DriverResource.Texture, "color target");
        using var sampler = new RDSamplerState();
        var samplerID = device.SamplerCreate(sampler);
        Reject<NotSupportedException>(() => device.SetResourceName(samplerID, "sampler"));
        var framebuffer = device.FramebufferCreate([texture]);
        Reject<NotSupportedException>(() => device.SetResourceName(framebuffer, "framebuffer"));

        using var computeModule = LoadCompute();
        var compute = device.ShaderCreateFromSPIRV(computeModule);
        var computePipeline = device.ComputePipelineCreate(compute);
        var alias = device.ComputePipelineCreate(compute);
        Name(computePipeline, DriverResource.ComputePipeline, "compute pipeline");
        CheckName(alias, DriverResource.ComputePipeline, "compute pipeline");
        Reject<NotSupportedException>(() => device.SetResourceName(compute, "compute shader"));
        CheckName(computePipeline, DriverResource.ComputePipeline, "compute pipeline");
        Name(alias, DriverResource.ComputePipeline, "pipeline alias");
        CheckName(computePipeline, DriverResource.ComputePipeline, "pipeline alias");

        float[] initial = [1, 2, 3, 4], settings = [2, 4, 3, 0];
        var values = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(initial.AsSpan()));
        var uniform = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(settings.AsSpan()));
        using var output = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.StorageBuffer };
        using var input = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.UniformBuffer };
        output.AddID(values);
        input.AddID(uniform);
        var outputs = device.UniformSetCreate([output], compute, 3);
        var inputs = device.UniformSetCreate([input], compute, 2);
        Reject<NotSupportedException>(() => device.SetResourceName(inputs, "logical set"));
        Reject<NotSupportedException>(() => device.SetResourceName(uniform, "host uniform"));
        var list = device.ComputeListBegin();
        device.ComputeListBindComputePipeline(list, alias);
        device.ComputeListBindUniformSet(list, outputs, 3);
        device.ComputeListBindUniformSet(list, inputs, 2);
        device.ComputeListDispatch(list, 1, 1, 1);
        device.ComputeListEnd();
        Check(MemoryMarshal.Cast<byte, float>(device.BufferGetData(values).AsSpan()).SequenceEqual([5, 7, 9, 11]),
            "The named compute pipeline still dispatches.");

        using var graphics = RenderingDeviceGraphicsTests.Load("DeviceStageLink");
        var shader = device.ShaderCreateFromSPIRV(graphics);
        Reject<NotSupportedException>(() => device.SetResourceName(shader, "graphics stages"));
        using var raster = new RDPipelineRasterizationState();
        using var attachment = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1,
            RenderingDevice.RenderPrimitive.Triangles, raster, blend);
        Name(pipeline, DriverResource.RenderPipeline, "graphics pipeline");
        list = device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Black]);
        device.DrawListBindRenderPipeline(list, pipeline);
        device.DrawListDraw(list, false, 1, 3);
        device.DrawListEnd();
        var pixels = device.TextureGetData(texture, 0);
        for (var i = 0; i < pixels.Length; i += 4)
            Check(pixels[i] is >= 63 and <= 64 && pixels[i + 1] is >= 127 and <= 128 &&
                pixels[i + 2] is >= 191 and <= 192 && pixels[i + 3] == 255,
                "The named graphics pipeline still writes the expected pixels.");
        CheckName(texture, DriverResource.Texture, "color target");
        device.FreeRID(storage);
        Reject<ArgumentException>(() => device.SetResourceName(storage, "retired"));
        Console.WriteLine($"Direct3D 12 native buffer, image and pipeline names read back. Dispatch and draw passed on {device.GetDeviceName()}.");

        void Name(RID id, DriverResource kind, string name)
        {
            device.SetResourceName(id, name);
            CheckName(id, kind, name);
        }

        void CheckName(RID id, DriverResource kind, string name)
        {
            var pointer = (nint)device.GetDriverResource(kind, id, 0);
            Check(MatchesDirect3D12Name(pointer, name) == 1, $"Direct3D 12 name readback for {kind}: '{name}'.");
        }
    }

    private static RDShaderSPIRV LoadCompute()
    {
        using var input = typeof(GPUResourceNameTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalCompute.comp.spv")!;
        using var stream = new MemoryStream();
        input.CopyTo(stream);
        return new RDShaderSPIRV { BytecodeCompute = stream.ToArray() };
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    [LibraryImport("resource-name-trace", EntryPoint = "e2d_test_name_trace_open")]
    private static partial int OpenTrace(nint device);
    [LibraryImport("resource-name-trace", EntryPoint = "e2d_test_name_trace_expect", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Expect(uint type, string name, int fail);
    [LibraryImport("resource-name-trace", EntryPoint = "e2d_test_name_trace_count")]
    private static partial int TraceCount();
    [LibraryImport("resource-name-trace", EntryPoint = "e2d_test_name_trace_close")]
    private static partial void CloseTrace();
    [LibraryImport("direct3d12-resource-name-check", EntryPoint = "e2d_test_direct3d12_name_matches",
        StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MatchesDirect3D12Name(nint resource, string expected);
}
