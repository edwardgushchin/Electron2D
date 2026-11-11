using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;

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

            using var module = new RDShaderSPIRV();
            using var input = typeof(GPUResourceNameTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalCompute.comp.spv")!;
            using var stream = new MemoryStream();
            input.CopyTo(stream);
            module.BytecodeCompute = stream.ToArray();
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
}
