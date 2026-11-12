using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static class NativeUniformTests
{
    internal static void Run(string profile)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (profile == "unavailable")
        {
            Reject<NotSupportedException>(() => device.UniformBufferCreate(16, creationBits: BufferCreationBits.AsStorage));
            Console.WriteLine("Native uniform creation reported an unavailable adapter.");
            return;
        }

        var direct3D = profile == "direct3d12";
        Check(direct3D || profile == "vulkan", "Unknown native uniform test profile.");
        if (direct3D)
        {
            Check(!device.HasFeature(Features.SupportsBufferDeviceAddress), "Native uniforms do not enable physical pointer shaders.");
            Reject<NotSupportedException>(() => device.UniformBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress));
        }
        Reject<ArgumentOutOfRangeException>(() => device.UniformBufferCreate(3, creationBits: BufferCreationBits.AsStorage));
        using var module = new RDShaderSPIRV();
        using var input = typeof(NativeUniformTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalCompute.comp.spv")!;
        using var stream = new MemoryStream();
        input.CopyTo(stream);
        module.BytecodeCompute = stream.ToArray();
        var shader = device.ShaderCreateFromSPIRV(module);
        var pipeline = device.ComputePipelineCreate(shader);
        var native = device.UniformBufferCreate(16, FloatBytes([1, 2, 3, 4]), BufferCreationBits.AsStorage);
        Check(device.BufferGetDeviceAddress(native) == 0, "The storage-only role has no public address.");
        var host = device.UniformBufferCreate(16, FloatBytes([1, 4, 1, 0]));
        var output = device.StorageBufferCreate(16, FloatBytes([1, 2, 3, 4]));
        var indirect = device.StorageBufferCreate(12, MemoryMarshal.AsBytes<uint>([1, 1, 1]), StorageBufferUsage.DispatchIndirect);
        var nativeInput = Set(device, shader, 2, 4, UniformType.UniformBuffer, native);
        var hostInput = Set(device, shader, 2, 4, UniformType.UniformBuffer, host);
        var producer = Set(device, shader, 3, 7, UniformType.StorageBuffer, native);
        var consumer = Set(device, shader, 3, 7, UniformType.StorageBuffer, output);
        var list = device.ComputeListBegin();
        device.ComputeListBindComputePipeline(list, pipeline);
        device.ComputeListBindUniformSet(list, producer, 3);
        if (direct3D)
        {
            device.ComputeListBindUniformSet(list, nativeInput, 2);
            Reject<InvalidOperationException>(() => device.ComputeListDispatch(list, 1, 1, 1));
        }
        device.ComputeListBindUniformSet(list, hostInput, 2);
        device.ComputeListDispatch(list, 1, 1, 1);
        device.ComputeListBindUniformSet(list, consumer, 3);
        device.ComputeListBindUniformSet(list, nativeInput, 2);
        device.ComputeListDispatchIndirect(list, indirect, 0);
        device.ComputeListBindUniformSet(list, hostInput, 2);
        device.ComputeListDispatch(list, 1, 1, 1);
        device.ComputeListBindUniformSet(list, nativeInput, 2);
        device.ComputeListDispatch(list, 1, 1, 1);
        device.ComputeListEnd();

        CheckGraphics(device, native, host);
        var copy = device.StorageBufferCreate(16);
        device.BufferCopy(native, copy, 0, 0, 16);
        Reject<ArgumentException>(() => device.BufferCopy(native, native, 0, 4, 8));
        device.BufferCopy(native, native, 0, 12, 4);
        device.BufferCopy(native, native, 8, 0, 8);
        Check(Floats(device.BufferGetData(native)).SequenceEqual([4, 2, 4, 2]), "Ordered disjoint copies preserve source bytes across scratch growth.");
        device.BufferClear(native, 4, 4);
        device.BufferUpdate(native, 8, 4, FloatBytes([7]));
        var callbacks = 0;
        device.BufferGetDataAsync(native, data =>
        {
            Check(Floats(data).SequenceEqual([4, 0, 7, 2]), "Partial clear and update reached the native uniform allocation.");
            callbacks++;
        });
        device.FreeRID(native);
        Check(callbacks == 1 && !device.UniformSetIsValid(nativeInput), "Retirement delivers the queued readback and invalidates the set.");
        Check(Floats(device.BufferGetData(output)).SequenceEqual([18, 22, 26, 5]), "GPU writes feed native and host uniform rebindings.");
        Check(Floats(device.BufferGetData(copy)).SequenceEqual([2, 3, 4, 5]), "GPU copy preserves the written uniform bytes.");
        CheckPreparedDispatch(device, shader, pipeline, copy, consumer);
        Console.WriteLine($"Native uniform compute, vertex and fragment passed on {profile} / {device.GetDeviceName()}.");
    }

    private static void CheckPreparedDispatch(RenderingDevice device, RID shader, RID pipeline, RID copy, RID outputSet)
    {
        var native = device.UniformBufferCreate(16, creationBits: BufferCreationBits.AsStorage);
        device.BufferCopy(copy, native, 0, 0, 16);
        var input = Set(device, shader, 2, 4, UniformType.UniformBuffer, native);
        void Cycle()
        {
            var list = device.ComputeListBegin();
            device.ComputeListBindComputePipeline(list, pipeline);
            device.ComputeListBindUniformSet(list, input, 2);
            device.ComputeListBindUniformSet(list, outputSet, 3);
            device.ComputeListDispatch(list, 1, 1, 1);
            device.ComputeListEnd();
            device.Submit();
            device.Sync();
        }

        for (var i = 0; i < 16; i++) Cycle();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) Cycle();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Prepared native uniform dispatch allocated {allocated} owner-thread managed bytes.");
    }

    private static void CheckGraphics(RenderingDevice device, RID native, RID host)
    {
        using var module = RenderingDeviceGraphicsTests.Load("DeviceNativeUniform");
        var shader = device.ShaderCreateFromSPIRV(module);
        var nativeSet = Set(device, shader, 0, 0, UniformType.UniformBuffer, native);
        var hostSet = Set(device, shader, 0, 0, UniformType.UniformBuffer, host);
        using var format = new RDTextureFormat
        {
            Width = 3,
            Height = 1,
            Format = DataFormat.R32G32B32A32SFLOAT,
            UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom,
        };
        using var view = new RDTextureView();
        var texture = device.TextureCreate(format, view);
        var framebuffer = device.FramebufferCreate([texture]);
        using var raster = new RDPipelineRasterizationState();
        using var attachment = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1,
            RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [Colors.Black]);
        device.DrawListBindRenderPipeline(list, pipeline);
        for (var x = 0; x < 3; x++)
        {
            device.DrawListBindUniformSet(list, x == 1 ? hostSet : nativeSet, 0);
            device.DrawListEnableScissor(list, new(x, 0, 1, 1));
            device.DrawListDraw(list, false, 1, 3);
        }
        device.DrawListEnd();
        var pixels = Floats(device.TextureGetData(texture, 0));
        Check(pixels.SequenceEqual([2, 3, 4, 5, 1, 4, 1, 0, 2, 3, 4, 5]),
            $"Native/host/native graphics CBV rebinding produced {string.Join(", ", pixels.ToArray())}.");
    }

    private static RID Set(RenderingDevice device, RID shader, uint set, int binding, UniformType type, RID buffer)
    {
        using var uniform = new RDUniform { Binding = binding, UniformType = type };
        uniform.AddID(buffer);
        return device.UniformSetCreate([uniform], shader, set);
    }

    private static ReadOnlySpan<byte> FloatBytes(ReadOnlySpan<float> values) => MemoryMarshal.AsBytes(values);
    private static ReadOnlySpan<float> Floats(byte[] bytes) => MemoryMarshal.Cast<byte, float>(bytes);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action operation) where T : Exception
    {
        try { operation(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
