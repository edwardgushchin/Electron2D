using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class GPUCommandLabelTests
{
    internal static void Run(string profile)
    {
        using var receiver = RenderingServer.CreateLocalRenderingDevice();
        using var device = receiver.CreateLocalDevice();
        device.DrawCommandEndLabel();
        if (profile == "unavailable")
        {
            Reject<NotSupportedException>(() => device.DrawCommandBeginLabel("missing", Colors.White));
            device.DrawCommandEndLabel();
            var buffer = device.StorageBufferCreate(4, [1, 2, 3, 4]);
            Check(device.BufferGetData(buffer).SequenceEqual(new byte[] { 1, 2, 3, 4 }), "Refusal preserves ordinary GPU work.");
            Console.WriteLine("Colored GPU command labels report an unavailable backend.");
            return;
        }

        Check(profile == "vulkan", "Use the Vulkan label profile.");
        device.DrawCommandBeginLabel("probe", Colors.White);
        device.DrawCommandEndLabel();
        device.Submit();
        device.Sync();

        using var format = new RDTextureFormat
        {
            Width = 8,
            Height = 8,
            Format = RenderingDevice.DataFormat.R8G8B8A8UNORM,
            UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom,
        };
        using var view = new RDTextureView();
        var output = device.TextureCreate(format, view);
        var framebuffer = device.FramebufferCreate([output]);
        using var graphics = RenderingDeviceGraphicsTests.Load("DeviceProcedural");
        var shader = device.ShaderCreateFromSPIRV(graphics);
        using var raster = new RDPipelineRasterizationState();
        using var attachment = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1,
            RenderingDevice.RenderPrimitive.Triangles, raster, blend);

        using var compute = new RDShaderSPIRV();
        using var input = typeof(GPUCommandLabelTests).Assembly.GetManifestResourceStream("Electron2D.Compute.DeviceArguments.comp.spv")!;
        using var stream = new MemoryStream();
        input.CopyTo(stream);
        compute.BytecodeCompute = stream.ToArray();
        var computeShader = device.ShaderCreateFromSPIRV(compute);
        var computePipeline = device.ComputePipelineCreate(computeShader);
        var source = device.StorageBufferCreate(48);
        var destination = device.StorageBufferCreate(48);
        using var data = new RDUniform { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 0 };
        data.AddID(source);
        var set = device.UniformSetCreate([data], computeShader, 0);
        device.Submit();
        device.Sync();

        var native = (nint)typeof(RenderingDevice).GetProperty("Device", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(device)!;
        Check(OpenTrace(native) == 1, "Install the Vulkan command observer.");
        try
        {
            Reject<ArgumentNullException>(() => device.DrawCommandBeginLabel(null!, Colors.White));
            Reject<ArgumentException>(() => device.DrawCommandBeginLabel("bad\0name", Colors.White));
            Reject<ArgumentOutOfRangeException>(() => device.DrawCommandBeginLabel("nan", new(float.NaN, 0, 0)));
            Reject<ArgumentOutOfRangeException>(() => device.DrawCommandBeginLabel("range", new(1.1f, 0, 0)));
            Reject<InvalidOperationException>(() => Task.Run(device.DrawCommandEndLabel).GetAwaiter().GetResult());
            device.DrawCommandEndLabel();

            Begin("Кадр — α", new(.25f, .5f, .75f, 1));
            var draw = device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Blue]);
            device.DrawListBindRenderPipeline(draw, pipeline);
            device.DrawListDraw(draw, false, 1, 3);
            Begin("replacement", Colors.Red);
            device.DrawListDraw(draw, false, 1, 3);
            device.DrawCommandEndLabel();
            device.DrawListEnd();

            Begin("dispatch and copy", Colors.Green);
            var commands = device.ComputeListBegin();
            device.ComputeListBindComputePipeline(commands, computePipeline);
            device.ComputeListBindUniformSet(commands, set, 0);
            device.ComputeListDispatch(commands, 1, 1, 1);
            device.ComputeListEnd();
            device.BufferCopy(source, destination, 0, 0, 48);
            device.Submit();
            device.Sync();
            device.DrawCommandEndLabel();
            Begin("", default);
            var copied = MemoryMarshal.Cast<byte, uint>(device.BufferGetData(destination));
            Check(copied[0] == 3 && copied[1] == 1 && copied[8] == 3 && copied[9] == 1,
                "Labeled dispatch and copy preserve compute output.");
            device.DrawCommandEndLabel();
            Check(TraceCount(0) == 4 && TraceCount(1) == 4 && TraceCount(2) == 2 &&
                  TraceCount(3) == 1 && TraceCount(4) >= 2 && TraceCount(5) == 0,
                "Balanced native regions contain two draws, a dispatch and copies across explicit and implicit submissions.");
        }
        finally { CloseTrace(); }

        var pixels = device.TextureGetData(output, 0);
        Check(Enumerable.Range(0, pixels.Length / 4).Any(i => pixels[i * 4] == 255 && pixels[i * 4 + 2] == 0),
            "Labeled draws preserve graphics output.");
        device.DrawCommandBeginLabel("cancelled recording", Colors.White);
        device.Dispose();
        Reject<ObjectDisposedException>(device.DrawCommandEndLabel);
        receiver.DrawCommandBeginLabel("other device recording", Colors.White);
        receiver.DrawCommandEndLabel();
        receiver.Submit();
        receiver.Sync();
        Console.WriteLine("Vulkan command label colors, replacement, draw, dispatch, copy, submission and cancellation passed.");

        void Begin(string name, Color color)
        {
            Check(Expect(name, color.R, color.G, color.B, color.A) == 1, "Configure the next expected label.");
            device.DrawCommandBeginLabel(name, color);
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

    [LibraryImport("command-label-trace", EntryPoint = "e2d_test_labels_open")]
    private static partial int OpenTrace(nint device);
    [LibraryImport("command-label-trace", EntryPoint = "e2d_test_labels_expect", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Expect(string name, float r, float g, float b, float a);
    [LibraryImport("command-label-trace", EntryPoint = "e2d_test_labels_count")]
    private static partial int TraceCount(int eventIndex);
    [LibraryImport("command-label-trace", EntryPoint = "e2d_test_labels_close")]
    private static partial void CloseTrace();
}
