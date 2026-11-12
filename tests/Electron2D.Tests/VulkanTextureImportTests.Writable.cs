using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using Usage = Electron2D.RenderingDevice.TextureUsageBits;

internal static unsafe partial class VulkanTextureImportTests
{
    private const Usage WritableUsage = Transfers | Usage.ColorAttachment | Usage.Storage;
    private const uint NativeWritableUsage = 0x08 | 0x10; // VK_IMAGE_USAGE_STORAGE_BIT and VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT.

    private static void VerifyWritableRefusal(RenderingDevice device, nint library)
    {
        var context = GetContext(device);
        var image = Create(library, context, 2, extraUsage: NativeWritableUsage);
        try
        {
            var handle = image.Image;
            Reject<NotSupportedException>(() => Import(device, handle, 2, usage: WritableUsage));
            var sampled = Import(device, handle, 2);
            try { Check(device.TextureGetData(sampled, 1).AsSpan().SequenceEqual(MipChain(2)), "The preceding importer retains sampled and transfer roles."); }
            finally { device.FreeRID(sampled); }
        }
        finally { Destroy(library, context, ref image); }
    }

    private static void WritableScene(nint library)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(40, 28), Title = "Writable Vulkan import" };
        using var output = root.GetTexture();
        var complete = false;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice() ?? throw new InvalidOperationException("Expected the retained GPU device.");
            Check(RenderingServer.GetCurrentRenderingDriverName() == "vulkan", "Expected GPU Vulkan.");
            VerifyWritable(device, library, root, output, layers: 1);
            VerifyWritable(device, library, root, output, layers: 2);
            complete = true;
            root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && complete, "The writable import scene completed.");
    }

    private static void VerifyWritable(RenderingDevice device, nint library, Window root, Texture output, uint layers)
    {
        var context = GetContext(device);
        var image = Create(library, context, layers, extraUsage: NativeWritableUsage);
        RID imported = default, canvas = default, item = default, texture = default;
        try
        {
            var handle = image.Image;
            Reject<NotSupportedException>(() => Import(device, handle, layers, usage: WritableUsage | Usage.InputAttachment));
            imported = Import(device, handle, layers, usage: WritableUsage);
            Check(device.GetDriverResource(DriverResource.Texture, imported, 0) == handle, "The writable RID identifies the producer allocation.");
            using var format = device.TextureGetFormat(imported);
            Check(format.UsageBits == WritableUsage, "The import publishes the exact writable roles.");

            using var view = new RDTextureView();
            var layer = layers - 1;
            const uint mip = 1;
            var slice = device.TextureCreateSharedFromSlice(view, imported, layer, mip);
            var framebuffer = device.FramebufferCreate([slice]);
            device.DrawListBegin(framebuffer, DrawFlags.ClearColor0, [Colors.Magenta]);
            device.DrawListEnd();
            device.FreeRID(framebuffer);
            WriteImportedImage(device, imported, layers);

            var expectedBase = MipChain((int)layer * 2);
            var mips = MemoryMarshal.Cast<byte, uint>(expectedBase.AsSpan());
            mips[0] = layers == 1 ? 0xff000000u : 0xff0000ffu;
            mips.Slice((int)(Width * Height), (int)((Width >> 1) * (Height >> 1))).Fill(0xffff00ffu);
            Check(device.TextureGetData(imported, layer).AsSpan().SequenceEqual(expectedBase),
                "Framebuffer clear and compute read/write preserve neighboring texels and mips.");
            if (layers > 1)
            {
                var other = MipChain(0);
                MemoryMarshal.Cast<byte, uint>(other.AsSpan())[0] = 0xffff0000u;
                Check(device.TextureGetData(imported, 0).AsSpan().SequenceEqual(other), "Array compute binds both layers, and the framebuffer clear stays in its own layer.");
            }

            texture = RenderingServer.TextureRDCreate(slice);
            canvas = RenderingServer.CanvasCreate();
            item = RenderingServer.CanvasItemCreate();
            RenderingServer.ViewportAttachCanvas(root.GetViewportRID(), canvas);
            RenderingServer.CanvasItemSetParent(item, canvas);
            RenderingServer.CanvasItemAddTextureRect(item, new(0, 0, 12, 8), texture);
            RenderingServer.ForceDraw();
            using (var rendered = output.GetImage())
                Check(rendered?.GetPixel(5, 4) == Colors.Magenta, "The canvas samples the framebuffer result from the native slice.");

            RenderingServer.FreeRID(texture);
            texture = default;
            byte[]? pending = null;
            device.TextureGetDataAsync(imported, layer, bytes => pending = bytes);
            device.FreeRID(imported);
            Check(pending is not null && pending.AsSpan().SequenceEqual(expectedBase), "Free drains queued readback and expires the native view.");
            Check(!device.TextureIsValid(slice), "Retirement expires the slice.");
            imported = Import(device, handle, layers);
            Check(device.TextureGetData(imported, layer).AsSpan().SequenceEqual(expectedBase), "A fresh wrapper sees the same bytes on the producer image.");
            device.FreeRID(imported);
            imported = default;
            Clear(library, context, image, layers, phase: 1);
            imported = Import(device, handle, layers);
            Check(device.TextureGetData(imported, layer).AsSpan().SequenceEqual(MipChain(1 + (int)layer * 2)),
                "The producer can transition the returned image from its sampled layout and replace the contents.");
        }
        finally
        {
            if (item.IsValid()) RenderingServer.FreeRID(item);
            if (canvas.IsValid()) RenderingServer.FreeRID(canvas);
            if (texture.IsValid()) RenderingServer.FreeRID(texture);
            if (imported.IsValid() && device.TextureIsValid(imported)) device.FreeRID(imported);
            Destroy(library, context, ref image);
        }
    }

    private static void WriteImportedImage(RenderingDevice device, RID texture, uint layers)
    {
        var name = layers == 1 ? "VulkanImportWrite" : "VulkanImportWriteArray";
        using var stream = typeof(VulkanTextureImportTests).Assembly.GetManifestResourceStream($"Electron2D.Compute.{name}.comp.spv")!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        using var module = new RDShaderSPIRV { BytecodeCompute = bytes.ToArray() };
        using var binding = new RDUniform { Binding = 0, UniformType = UniformType.Image };
        binding.AddID(texture);
        var shader = device.ShaderCreateFromSPIRV(module);
        try
        {
            var pipeline = device.ComputePipelineCreate(shader);
            var set = device.UniformSetCreate([binding], shader, 0);
            var list = device.ComputeListBegin();
            try
            {
                device.ComputeListBindComputePipeline(list, pipeline);
                device.ComputeListBindUniformSet(list, set, 0);
                device.ComputeListDispatch(list, 1, 1, layers);
            }
            finally { device.ComputeListEnd(); }
        }
        finally { device.FreeRID(shader); }
    }
}
