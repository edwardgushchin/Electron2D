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

        Check(profile is "vulkan" or "direct3d12", "Use the vulkan or direct3d12 label profile.");
        var direct3D = profile == "direct3d12";
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
        Check(OpenTrace(native, direct3D ? 1 : 0) == 1, $"Install the {profile} command observer.");
        try
        {
            Reject<ArgumentNullException>(() => device.DrawCommandBeginLabel(null!, Colors.White));
            Reject<ArgumentException>(() => device.DrawCommandBeginLabel("bad\0name", Colors.White));
            Reject<ArgumentOutOfRangeException>(() => device.DrawCommandBeginLabel("nan", new(float.NaN, 0, 0)));
            Reject<ArgumentOutOfRangeException>(() => device.DrawCommandBeginLabel("range", new(1.1f, 0, 0)));
            Reject<ArgumentOutOfRangeException>(() => device.DrawCommandBeginLabel("alpha", new(1, 0, 0, -.1f)));
            Reject<InvalidOperationException>(() => Task.Run(device.DrawCommandEndLabel).GetAwaiter().GetResult());
            device.DrawCommandEndLabel();
            Check(TraceCount(0) == 0 && TraceCount(1) == 0, "Invalid input and an idle end emit no events.");

            Begin("Кадр — α — 100% \U0001F680", new(.25f, .5f, .75f, .125f), 0xff4080bf);
            var draw = device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Blue]);
            device.DrawListBindRenderPipeline(draw, pipeline);
            device.DrawListDraw(draw, false, 1, 3);
            Reject<ArgumentException>(() => device.DrawCommandBeginLabel("bad\0replacement", Colors.White));
            Check(TraceCount(0) == 1 && TraceCount(1) == 0 && TraceCount(2) == 1,
                "The first draw runs inside its region and invalid replacement leaves that region open.");
            Begin("replacement", Colors.Red, 0xffff0000);
            Check(TraceCount(0) == 2 && TraceCount(1) == 1 && TraceCount(2) == 1,
                "Replacement ends the first region before the second draw.");
            device.DrawListDraw(draw, false, 1, 3);
            device.DrawCommandEndLabel();
            device.DrawCommandEndLabel();
            Check(TraceCount(1) == 2 && TraceCount(2) == 2, "Explicit end and repeated end close the replacement once.");
            device.DrawListEnd();

            Begin("dispatch and copy", new Color(0, 1, 0), 0xff00ff00);
            var commands = device.ComputeListBegin();
            device.ComputeListBindComputePipeline(commands, computePipeline);
            device.ComputeListBindUniformSet(commands, set, 0);
            device.ComputeListDispatch(commands, 1, 1, 1);
            device.ComputeListEnd();
            device.BufferCopy(source, destination, 0, 0, 48);
            Check(TraceCount(1) == 2 && TraceCount(3) == 1 && TraceCount(4) == 1,
                "The next region spans dispatch and buffer copy.");
            device.Submit();
            device.Sync();
            device.DrawCommandEndLabel();
            Check(TraceCount(1) == 3, "Submission closes the region once.");
            Begin("", default, 0xff000000);
            var copied = MemoryMarshal.Cast<byte, uint>(device.BufferGetData(destination));
            Check(copied[0] == 3 && copied[1] == 1 && copied[8] == 3 && copied[9] == 1,
                "Labeled dispatch and copy preserve compute output.");
            device.DrawCommandEndLabel();
            Check(TraceCount(0) == 4 && TraceCount(1) == 4 && TraceCount(2) == 2 &&
                  TraceCount(3) == 1 && TraceCount(4) >= 2 && TraceCount(5) == 0,
                "Balanced native regions contain two draws, a dispatch and copies across explicit and implicit submissions.");

            var pixels = device.TextureGetData(output, 0);
            Check(Enumerable.Range(0, pixels.Length / 4).Any(i => pixels[i * 4] == 255 && pixels[i * 4 + 2] == 0),
                "Labeled draws preserve graphics output.");

            if (direct3D)
            {
                var full = new string('x', 233) + "\U0001F680";
                Begin(full, Colors.White, 0xffffffff);
                device.DrawCommandEndLabel();
                var cut = new string('x', 234);
                Begin(cut + "\U0001F680!", Colors.White, 0xffffffff, cut);
                device.DrawCommandEndLabel();
                device.Submit();
                device.Sync();
                Begin("cancelled recording", Colors.White, 0xffffffff);
                device.Dispose();
                Check(TraceCount(0) == 7 && TraceCount(1) == 7 && TraceCount(5) == 0 && TraceCount(6) == 1,
                    "Bounded UTF-16 names are balanced and disposal ends the region before native cancellation.");
            }
        }
        finally
        {
            CloseTrace();
        }

        if (!direct3D)
        {
            device.DrawCommandBeginLabel("cancelled recording", Colors.White);
            device.Dispose();
        }
        Reject<ObjectDisposedException>(device.DrawCommandEndLabel);
        receiver.DrawCommandBeginLabel("other device recording", Colors.White);
        receiver.DrawCommandEndLabel();
        receiver.Submit();
        receiver.Sync();
        Console.WriteLine($"{profile} command label Unicode, colors, replacement, draw, dispatch, copy, submission and cancellation passed.");

        void Begin(string name, Color color, uint rgb, string? expectedName = null)
        {
            var result = direct3D
                ? ExpectDirect3D(expectedName ?? name, rgb)
                : Expect(name, color.R, color.G, color.B, color.A);
            Check(result == 1, "Configure the next expected label.");
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
    private static partial int OpenTrace(nint device, int direct3D);
    [LibraryImport("command-label-trace", EntryPoint = "e2d_test_labels_expect_d3d12", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int ExpectDirect3D(string name, uint rgb);
    [LibraryImport("command-label-trace", EntryPoint = "e2d_test_labels_expect", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Expect(string name, float r, float g, float b, float a);
    [LibraryImport("command-label-trace", EntryPoint = "e2d_test_labels_count")]
    private static partial int TraceCount(int eventIndex);
    [LibraryImport("command-label-trace", EntryPoint = "e2d_test_labels_close")]
    private static partial void CloseTrace();
}
