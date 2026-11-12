using System.Runtime.InteropServices;
using Electron2D;
using DataFormat = Electron2D.RenderingDevice.DataFormat;
using ResourceKind = Electron2D.RenderingDevice.DriverResource;
using Samples = Electron2D.RenderingDevice.TextureSamples;
using Shape = Electron2D.RenderingDevice.TextureType;
using Usage = Electron2D.RenderingDevice.TextureUsageBits;

internal static unsafe partial class VulkanTextureImportTests
{
    private const uint Width = 7, Height = 5, Levels = 3;
    private const Usage Transfers = Usage.Sampling | Usage.CanCopyFrom | Usage.CanCopyTo | Usage.CanUpdate | Usage.CPURead;

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct ProducerContext(nint Instance, nint Physical, nint Device, nint Queue, uint Family);

    [StructLayout(LayoutKind.Sequential)]
    private record struct ProducerImage(ulong Image, ulong Memory);

    internal static void Run(string profile)
    {
        var path = Environment.GetEnvironmentVariable("ELECTRON2D_VULKAN_IMPORT_FIXTURE");
        var library = NativeLibrary.Load(path ?? throw new InvalidOperationException("Set ELECTRON2D_VULKAN_IMPORT_FIXTURE."));
        try
        {
            if (profile == "canvas") Canvas(library);
            else if (profile == "server") Server(library);
            else if (profile == "writable") WritableScene(library);
            else if (profile == "writable-preceding")
            {
                using var device = RenderingServer.CreateLocalRenderingDevice();
                VerifyWritableRefusal(device, library);
            }
            else
            {
                using var device = RenderingServer.CreateLocalRenderingDevice();
                Local(device, library, preceding: profile == "preceding");
                device.Dispose();
                Reject<ObjectDisposedException>(() => Import(device, 1));
            }
        }
        finally { NativeLibrary.Free(library); }
        Console.WriteLine($"Vulkan texture import {profile} passed.");
    }

    private static void Local(RenderingDevice device, nint library, bool preceding)
    {
        var context = GetContext(device);
        var image = Create(library, context, 2);
        RID imported = default;
        try
        {
            var handle = image.Image;
            if (preceding)
            {
                Reject<NotSupportedException>(() => Import(device, handle, 2));
                CheckOrdinaryDeviceWork(device);
                return;
            }

            Reject<ArgumentOutOfRangeException>(() => Import(device, 0));
            Reject<ArgumentOutOfRangeException>(() => Import(device, handle, 2, mipmaps: 4));
            Reject<NotSupportedException>(() => Import(device, handle, 2, samples: Samples.Samples2));
            Reject<NotSupportedException>(() => Import(device, handle, 2, usage: Usage.Sampling | Usage.InputAttachment));
            Reject<NotSupportedException>(() => Import(device, handle, 2, usage: Usage.CanCopyFrom));
            Reject<InvalidOperationException>(() => Task.Run(() => Import(device, handle, 2)).GetAwaiter().GetResult());
            device.ComputeListBegin();
            try { Reject<InvalidOperationException>(() => Import(device, handle, 2)); }
            finally { device.ComputeListEnd(); }
            device.Submit();
            device.Sync();

            imported = Import(device, handle, 2);
            Check(device.GetDriverResource(ResourceKind.Texture, imported, 0) == handle, "The importer retains the producer's exact VkImage.");
            Reject<ArgumentException>(() => Import(device, handle, 2));
            using (var other = RenderingServer.CreateLocalRenderingDevice())
            {
                Check(GetContext(other).Device == context.Device, "Both namespaces use the same native device.");
                Reject<InvalidOperationException>(() => Import(other, handle, 2));
            }

            using var format = device.TextureGetFormat(imported);
            Check(format.TextureType == Shape.TextureArray && format.Width == Width && format.Height == Height &&
                format.ArrayLayers == 2 && format.MIPMaps == Levels && format.UsageBits == Transfers, "The retained shape and roles are exact.");
            Check(device.TextureGetData(imported, 0).AsSpan().SequenceEqual(MipChain(0)), "The producer's layer zero has all three native mips.");
            Check(device.TextureGetData(imported, 1).AsSpan().SequenceEqual(MipChain(2)), "Layer one retains independent mip bytes.");

            using var view = new RDTextureView();
            var slice = device.TextureCreateSharedFromSlice(view, imported, 1, 1);
            Check(device.GetDriverResource(ResourceKind.TextureView, slice, 0) != device.GetDriverResource(ResourceKind.TextureView, imported, 0),
                "The slice has a distinct native view of the same allocation.");
            using var copyFormat = new RDTextureFormat
            {
                Width = Width >> 1,
                Height = Height >> 1,
                Format = DataFormat.R8G8B8A8UNORM,
                UsageBits = Transfers
            };
            var copy = device.TextureCreate(copyFormat, view);
            try
            {
                var engineImage = device.GetDriverResource(ResourceKind.Texture, copy, 0);
                Reject<ArgumentException>(() => Import(device, engineImage));
                device.TextureCopy(slice, copy, default, default, new(3, 2, 1), 0, 0, 0, 0);
                var mipBytes = (int)((Width >> 1) * (Height >> 1) * 4);
                Check(device.TextureGetData(copy, 0).AsSpan().SequenceEqual(MipChain(2).AsSpan((int)(Width * Height * 4), mipBytes)),
                    "Copy from an imported slice uses the selected physical mip.");
            }
            finally { device.FreeRID(copy); }

            for (var i = 0; i < 64; i++) _ = device.GetDriverResource(ResourceKind.Texture, imported, 0);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) _ = device.GetDriverResource(ResourceKind.Texture, imported, 0);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared imported image queries allocate no managed owner bytes.");

            var written = MipChain(1);
            device.TextureUpdate(imported, 1, written);
            byte[]? snapshot = null;
            device.TextureGetDataAsync(imported, 1, bytes => snapshot = bytes);
            device.TextureUpdate(imported, 1, MipChain(0));
            device.FreeRID(imported);
            Check(snapshot is not null && snapshot.AsSpan().SequenceEqual(written), "Free completes pending work and returns request-time bytes.");
            Check(!device.TextureIsValid(slice) && !device.TextureIsValid(imported), "Free retires the imported RID and its dependent slice.");
            imported = default;

            Clear(library, context, image, 2, phase: 1);
            imported = Import(device, handle, 2);
            Check(device.TextureGetData(imported, 0).AsSpan().SequenceEqual(MipChain(1)), "The producer can write the same live image after wrapper release and reimport it.");
            VerifyOrdinary(device, library, context);
        }
        finally
        {
            if (imported.IsValid() && device.TextureIsValid(imported)) device.FreeRID(imported);
            Destroy(library, context, ref image);
        }
    }

    private static void VerifyOrdinary(RenderingDevice device, nint library, ProducerContext context)
    {
        var image = Create(library, context, 1);
        RID imported = default;
        try
        {
            imported = Import(device, image.Image, usage: Usage.Sampling);
            var ordinary = imported;
            using var format = device.TextureGetFormat(ordinary);
            Check(format.TextureType == Shape.Texture && format.ArrayLayers == 1, "An ordinary import retains its distinct image shape.");
            Reject<InvalidOperationException>(() => device.TextureGetData(ordinary, 0));
            Reject<InvalidOperationException>(() => device.TextureUpdate(ordinary, 0, MipChain(0)));
        }
        finally
        {
            if (imported.IsValid()) device.FreeRID(imported);
            Destroy(library, context, ref image);
        }
    }

    private static void Canvas(nint library)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(40, 28), Title = "Vulkan texture import" };
        using var output = root.GetTexture();
        var complete = false;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice() ?? throw new InvalidOperationException("Expected the retained GPU device.");
            var context = GetContext(device);
            var image = Create(library, context, 2);
            var imported = Import(device, image.Image, 2);
            using var view = new RDTextureView();
            var slice = device.TextureCreateSharedFromSlice(view, imported, 1, 1);
            var canvasTexture = RenderingServer.TextureRDCreate(slice);
            var layeredTexture = RenderingServer.TextureRDCreate(imported);
            var canvas = RenderingServer.CanvasCreate();
            var item = RenderingServer.CanvasItemCreate();
            try
            {
                RenderingServer.ViewportAttachCanvas(root.GetViewportRID(), canvas);
                RenderingServer.CanvasItemSetParent(item, canvas);
                RenderingServer.CanvasItemSetDefaultTextureFilter(item, TextureFilter.Nearest);
                RenderingServer.CanvasItemAddTextureRect(item, new(0, 0, 20, 12), canvasTexture);
                RenderingServer.ForceDraw();
                using (var drawn = output.GetImage())
                    Check(drawn?.GetPixel(3, 3) == Colors.Yellow, "The retained canvas samples the producer's second layer and mip.");
                using (var layer = RenderingServer.Texture2DLayerGet(layeredTexture, 1))
                    Check(layer?.GetPixel(1, 1) == Colors.Blue, "The layered canvas wrapper exposes the imported producer layer.");
                Check(RenderingServer.TextureGetRDTexture(canvasTexture) == slice &&
                    RenderingServer.TextureGetNativeHandle(canvasTexture) == image.Image, "Canvas aliases report the imported physical image.");
                for (var i = 0; i < 24; i++) RenderingServer.ForceDraw(swapBuffers: false);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) RenderingServer.ForceDraw(swapBuffers: false);
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Check(allocated == 0, $"64 prepared canvas frames allocated {allocated} managed owner bytes.");
            }
            finally
            {
                RenderingServer.FreeRID(item);
                RenderingServer.FreeRID(canvas);
                RenderingServer.FreeRID(canvasTexture);
                RenderingServer.FreeRID(layeredTexture);
                device.FreeRID(imported);
                Destroy(library, context, ref image);
            }
            complete = true;
            root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && complete, "The native canvas scene completed.");
    }

    private static RID Import(RenderingDevice device, ulong image, uint layers = 1, uint mipmaps = Levels,
        Samples samples = Samples.Samples1, Usage usage = Transfers) =>
        device.TextureCreateFromExtension(layers == 1 ? Shape.Texture : Shape.TextureArray, DataFormat.R8G8B8A8UNORM,
            samples, usage, image, Width, Height, 1, layers, mipmaps);

    private static ProducerContext GetContext(RenderingDevice device) => new(
        (nint)device.GetDriverResource(ResourceKind.TopmostObject, default, 0),
        (nint)device.GetDriverResource(ResourceKind.PhysicalDevice, default, 0),
        (nint)device.GetDriverResource(ResourceKind.LogicalDevice, default, 0),
        (nint)device.GetDriverResource(ResourceKind.CommandQueue, default, 0),
        checked((uint)device.GetDriverResource(ResourceKind.QueueFamily, default, 0)));

    private static ProducerImage Create(nint library, ProducerContext context, uint layers, uint levels = Levels, uint format = 37, uint extraUsage = 0)
    {
        ProducerImage image;
        var create = (delegate* unmanaged[Cdecl]<ProducerContext*, uint, uint, uint, uint, uint, uint, ProducerImage*, int>)
            NativeLibrary.GetExport(library, "CreateImage");
        var result = create(&context, Width, Height, layers, levels, format, extraUsage, &image);
        Check(result == 0 && image.Image != 0 && image.Memory != 0, $"The external Vulkan producer returned {result}.");
        return image;
    }

    private static void Clear(nint library, ProducerContext context, ProducerImage image, uint layers, uint phase, uint levels = Levels)
    {
        var clear = (delegate* unmanaged[Cdecl]<ProducerContext*, ProducerImage*, uint, uint, uint, uint, int>)
            NativeLibrary.GetExport(library, "ClearImage");
        var result = clear(&context, &image, layers, levels, phase, 5); // VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
        Check(result == 0, $"The external producer reused the returned image with result {result}.");
    }

    private static void Destroy(nint library, ProducerContext context, ref ProducerImage image)
    {
        var destroy = (delegate* unmanaged[Cdecl]<ProducerContext*, ProducerImage*, void>)NativeLibrary.GetExport(library, "DestroyImage");
        fixed (ProducerImage* pointer = &image) destroy(&context, pointer);
    }

    private static byte[] MipChain(int phase)
    {
        uint[] colors = [0xff0000ff, 0xff00ff00, 0xffff0000, 0xff00ffff];
        var count = (int)(Width * Height + (Width >> 1) * (Height >> 1) + (Width >> 2) * (Height >> 2));
        var pixels = new uint[count];
        var offset = 0;
        for (var mip = 0; mip < Levels; mip++)
        {
            var length = (int)(Math.Max(1, Width >> mip) * Math.Max(1, Height >> mip));
            pixels.AsSpan(offset, length).Fill(colors[(phase + mip) % colors.Length]);
            offset += length;
        }
        return MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray();
    }

    private static void CheckOrdinaryDeviceWork(RenderingDevice device)
    {
        var buffer = device.StorageBufferCreate(4, [1, 2, 3, 4]);
        try { Check(device.BufferGetData(buffer).AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 4 }), "Refusal preserves ordinary native transfers."); }
        finally { device.FreeRID(buffer); }
    }

    private static void Check(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
