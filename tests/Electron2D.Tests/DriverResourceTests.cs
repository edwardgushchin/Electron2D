using System.Runtime.InteropServices;
using Electron2D;
using DriverResource = Electron2D.RenderingDevice.DriverResource;
using DataFormat = Electron2D.RenderingDevice.DataFormat;
using Usage = Electron2D.RenderingDevice.TextureUsageBits;

internal static unsafe class DriverResourceTests
{
    internal static void Run(string profile)
    {
        if (profile == "canvas")
        {
            Canvas();
            return;
        }

        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (profile == "unavailable")
        {
            Reject<NotSupportedException>(() => device.GetDriverResource(DriverResource.LogicalDevice, default, 0));
            var buffer = device.StorageBufferCreate(4, [1, 2, 3, 4]);
            Check(device.BufferGetData(buffer).AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 4 }), "Refusal preserves ordinary GPU operations.");
            Console.WriteLine("Missing driver resource query adapter refuses access without affecting GPU work.");
            return;
        }

        Local(device);
        device.Dispose();
        Reject<ObjectDisposedException>(() => device.GetDriverResource(DriverResource.LogicalDevice, default, 0));
        Console.WriteLine("Vulkan driver resources: native instance/device/queue, image and buffer requirements, typed views, pipelines, ownership, and zero warm owner bytes passed.");
    }

    private static void Local(RenderingDevice device)
    {
        using var format = new RDTextureFormat
        {
            Width = 8,
            Height = 8,
            Format = DataFormat.R8G8B8A8UNORM,
            UsageBits = Usage.Sampling | Usage.ColorAttachment | Usage.CanCopyFrom,
        };
        format.AddShareableFormat(DataFormat.R8G8B8A8UNORM);
        format.AddShareableFormat(DataFormat.R8G8B8A8SRGB);
        using var view = new RDTextureView();
        var texture = device.TextureCreate(format, view);
        view.FormatOverride = DataFormat.R8G8B8A8SRGB;
        var shared = device.TextureCreateShared(view, texture);
        var image = device.GetDriverResource(DriverResource.Texture, texture, 0);
        var imageView = device.GetDriverResource(DriverResource.TextureView, texture, 0);
        Check(image == device.GetDriverResource(DriverResource.Texture, shared, 0), "Shared views expose the same VkImage.");
        Check(imageView != device.GetDriverResource(DriverResource.TextureView, shared, 0), "Shared views expose distinct VkImageView objects.");
        Check(device.GetDriverResource(DriverResource.TextureDataFormat, texture, 0) == 37 &&
            device.GetDriverResource(DriverResource.TextureDataFormat, shared, 0) == 43, "Formats are the native VkFormat codes of both views.");

        var buffer = device.StorageBufferCreate(48);
        var queue = device.GetDriverResource(DriverResource.CommandQueue, default, 0);
        VerifyVulkanHandles(device, texture, buffer);
        Check(device.GetDriverResource(DriverResource.CommandQueue, texture, int.MinValue) == queue, "Queue ignores the RID and reserved index.");

        using var samplerState = new RDSamplerState();
        var sampler = device.SamplerCreate(samplerState);
        Check(device.GetDriverResource(DriverResource.Sampler, sampler, 0) != 0, "Sampler has a VkSampler.");
        Reject<ArgumentException>(() => device.GetDriverResource(DriverResource.Texture, sampler, 0));
        Reject<ArgumentOutOfRangeException>(() => device.GetDriverResource((DriverResource)13, default, 0));
        Reject<InvalidOperationException>(() => Task.Run(() => device.GetDriverResource(DriverResource.Texture, texture, 0)).GetAwaiter().GetResult());
        using (var foreign = RenderingServer.CreateLocalRenderingDevice())
        {
            Reject<ArgumentException>(() => foreign.GetDriverResource(DriverResource.Buffer, buffer, 0));
            Check(foreign.GetDriverResource(DriverResource.LogicalDevice, default, 0) ==
                device.GetDriverResource(DriverResource.LogicalDevice, default, 0), "Local namespaces share the actual device.");
        }

        var framebuffer = device.FramebufferCreate([texture]);
        using var graphics = RenderingDeviceGraphicsTests.Load("DeviceProcedural");
        var graphicsShader = device.ShaderCreateFromSPIRV(graphics);
        using var raster = new RDPipelineRasterizationState();
        using var attachment = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var render = device.RenderPipelineCreate(graphicsShader, device.FramebufferGetFormat(framebuffer), -1,
            RenderingDevice.RenderPrimitive.Triangles, raster, blend);
        var renderHandle = device.GetDriverResource(DriverResource.RenderPipeline, render, 0);
        var draw = device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Blue]);
        device.DrawListBindRenderPipeline(draw, render);
        Check(renderHandle != 0 && renderHandle == device.GetDriverResource(DriverResource.RenderPipeline, render, 1), "Query is read-only during an active draw.");
        device.DrawListDraw(draw, false, 1, 3);
        device.DrawListEnd();

        using var compute = new RDShaderSPIRV();
        using var input = typeof(DriverResourceTests).Assembly.GetManifestResourceStream("Electron2D.Compute.DeviceArguments.comp.spv")!;
        using var bytes = new MemoryStream();
        input.CopyTo(bytes);
        compute.BytecodeCompute = bytes.ToArray();
        var computeShader = device.ShaderCreateFromSPIRV(compute);
        var pipeline = device.ComputePipelineCreate(computeShader);
        Check(device.GetDriverResource(DriverResource.ComputePipeline, pipeline, 0) != 0, "Compute pipeline resolves the native program.");
        using var uniform = new RDUniform { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 0 };
        uniform.AddID(buffer);
        var set = device.UniformSetCreate([uniform], computeShader, 0);
        Reject<NotSupportedException>(() => device.GetDriverResource(DriverResource.UniformSet, set, 0));
        var list = device.ComputeListBegin();
        device.ComputeListBindComputePipeline(list, pipeline);
        device.ComputeListBindUniformSet(list, set, 0);
        device.ComputeListDispatch(list, 1, 1, 1);
        device.ComputeListEnd();
        device.Submit();
        device.Sync();

        var result = MemoryMarshal.Cast<byte, uint>(device.BufferGetData(buffer));
        Check(result[0] == 3 && result[1] == 1, "Compute remains executable.");
        var pixels = device.TextureGetData(texture, 0);
        Check(Enumerable.Range(0, pixels.Length / 4).Any(i => pixels[i * 4] == 255 && pixels[i * 4 + 2] == 0),
            "Graphics remains executable.");
        for (var i = 0; i < 64; i++) _ = device.GetDriverResource(DriverResource.TextureView, shared, i);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) _ = device.GetDriverResource(DriverResource.TextureView, shared, i);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warm native queries allocate no managed owner bytes.");

        device.FreeRID(framebuffer);
        device.FreeRID(texture);
        Reject<ArgumentException>(() => device.GetDriverResource(DriverResource.Texture, texture, 0));
        Reject<ArgumentException>(() => device.GetDriverResource(DriverResource.TextureView, shared, 0));
        device.FreeRID(computeShader);
        Reject<ArgumentException>(() => device.GetDriverResource(DriverResource.ComputePipeline, pipeline, 0));
    }

    private static void VerifyVulkanHandles(RenderingDevice device, RID texture, RID buffer)
    {
        var library = NativeLibrary.Load(OperatingSystem.IsWindows() ? "vulkan-1.dll" : "libvulkan.so.1");
        try
        {
            var instance = (nint)device.GetDriverResource(DriverResource.TopmostObject, default, 0);
            var physical = (nint)device.GetDriverResource(DriverResource.PhysicalDevice, default, 0);
            var logical = (nint)device.GetDriverResource(DriverResource.LogicalDevice, default, 0);
            var family = checked((uint)device.GetDriverResource(DriverResource.QueueFamily, default, 0));
            var enumerate = (delegate* unmanaged<nint, uint*, nint*, int>)NativeLibrary.GetExport(library, "vkEnumeratePhysicalDevices");
            uint count = 0;
            Check(enumerate(instance, &count, null) == 0 && count is > 0 and <= 64, "The Vulkan loader recognizes the instance.");
            var devices = stackalloc nint[(int)count];
            Check(enumerate(instance, &count, devices) == 0 && new ReadOnlySpan<nint>(devices, (int)count).Contains(physical),
                "The exported physical device belongs to the native instance.");
            var getFamilies = (delegate* unmanaged<nint, uint*, nint, void>)NativeLibrary.GetExport(library, "vkGetPhysicalDeviceQueueFamilyProperties");
            uint families = 0;
            getFamilies(physical, &families, 0);
            Check(family < families, "The native queue family exists on the physical device.");
            var getQueue = (delegate* unmanaged<nint, uint, uint, nint*, void>)NativeLibrary.GetExport(library, "vkGetDeviceQueue");
            nint queue = 0;
            getQueue(logical, family, 0, &queue);
            Check(queue != 0 && (ulong)queue == device.GetDriverResource(DriverResource.CommandQueue, default, 0), "Vulkan returns the same queue.");

            var image = device.GetDriverResource(DriverResource.Texture, texture, 0);
            var storage = device.GetDriverResource(DriverResource.Buffer, buffer, 0);
            var imageRequirements = (delegate* unmanaged<nint, ulong, MemoryRequirements*, void>)NativeLibrary.GetExport(library, "vkGetImageMemoryRequirements");
            var bufferRequirements = (delegate* unmanaged<nint, ulong, MemoryRequirements*, void>)NativeLibrary.GetExport(library, "vkGetBufferMemoryRequirements");
            MemoryRequirements imageInfo = default, bufferInfo = default;
            imageRequirements(logical, image, &imageInfo);
            bufferRequirements(logical, storage, &bufferInfo);
            Check(imageInfo.Size >= 8 * 8 * 4 && imageInfo.Alignment != 0 && imageInfo.MemoryTypes != 0 &&
                bufferInfo.Size >= 48 && bufferInfo.Alignment != 0 && bufferInfo.MemoryTypes != 0,
                "Independent Vulkan memory queries recognize both live resources.");
        }
        finally { NativeLibrary.Free(library); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryRequirements
    {
        internal ulong Size, Alignment;
        internal uint MemoryTypes;
    }

    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Title = "Viewport driver resources", Size = new(48, 32) };
        var child = new SubViewport { Size = new(12, 12), RenderTargetUpdateMode = ViewportUpdateMode.Always };
        child.AddChild(new ColorRect { Color = Colors.Red, Size = new(20, 20) });
        root.AddChild(child);
        using var texture = child.GetTexture();
        var passed = false;
        root.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingDriverName() == "vulkan", "Vulkan canvas is active.");
            RenderingServer.ForceDraw();
            var device = RenderingServer.GetRenderingDevice()!;
            var borrowed = RenderingServer.TextureGetRDTexture(texture.GetRID());
            var first = device.GetDriverResource(DriverResource.Texture, borrowed, 0);
            RenderingServer.ForceDraw();
            var second = device.GetDriverResource(DriverResource.Texture, borrowed, 0);
            Check(second != first, "The same borrowed RID follows the completed ping-pong image.");
            RenderingServer.ForceDraw();
            Check(device.GetDriverResource(DriverResource.Texture, borrowed, 0) == first, "The completed allocation is observed each frame.");
            child.Size = new(16, 16);
            RenderingServer.ForceDraw();
            Reject<ArgumentException>(() => device.GetDriverResource(DriverResource.Texture, borrowed, 0));
            var resized = RenderingServer.TextureGetRDTexture(texture.GetRID());
            Check(device.GetDriverResource(DriverResource.TextureDataFormat, resized, 0) == 37, "Resized storage exposes its current format.");
            using var pixels = texture.GetImage()!;
            Check(pixels.GetPixel(8, 8) == Colors.Red, "The native viewport still renders its canvas.");
            passed = true;
            root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && passed, "Viewport driver resource scenario completed.");
        Console.WriteLine("Vulkan viewport resource follows completed images and expires on resize.");
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
