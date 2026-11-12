using System.Runtime.InteropServices;
using Electron2D;
using DeviceFormat = Electron2D.RenderingDevice.DataFormat;
using DeviceShape = Electron2D.RenderingDevice.TextureType;
using DriverResource = Electron2D.RenderingDevice.DriverResource;
using Usage = Electron2D.RenderingDevice.TextureUsageBits;

internal static unsafe partial class NativeTextureImportTests
{
    private const Usage GPUTransfer = Usage.Sampling | Usage.CanCopyFrom | Usage.CanCopyTo | Usage.CanUpdate | Usage.CPURead;
    private const uint Levels = 3;

    private static void RunGPUImport(string profile, nint fixture, bool debug)
    {
        if (debug)
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<int>)NativeLibrary.GetExport(fixture, "EnableDebug"))());
        if (profile == "gpu-direct3d12") VerifyGPUCanvas(fixture, debug);
        else if (profile == "gpu-direct3d12-sampled-abi") VerifyGPUWritableRefusal(fixture, debug);
        else
        {
            var preceding = profile == "gpu-direct3d12-preceding";
            VerifyGPUDevice(fixture, debug, preceding);
            if (!preceding) VerifyGPUWritable(fixture, debug);
        }
    }

    private static nint CreateGPUResource(RenderingDevice device, nint fixture, uint layers, uint levels, uint options = 0)
    {
        var nativeDevice = (nint)device.GetDriverResource(DriverResource.LogicalDevice, default, 0);
        var create = (delegate* unmanaged[Cdecl]<nint, uint, uint, uint, nint*, int>)NativeLibrary.GetExport(fixture, "CreateGPUSample");
        nint resource;
        Marshal.ThrowExceptionForHR(create(nativeDevice, layers, levels, options, &resource));
        return resource;
    }

    private static void RewriteGPUResource(nint fixture, nint resource, uint phase) =>
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<nint, uint, int>)NativeLibrary.GetExport(fixture, "RewriteGPUSample"))(resource, phase));

    private static void CheckGPUDebug(nint fixture, nint resource, bool debug)
    {
        if (!debug) return;
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<nint, int>)NativeLibrary.GetExport(fixture, "CheckDebug"))(resource));
        Console.WriteLine("Direct3D 12 debug layer reported no errors or corruption.");
    }

    private static RID ImportGPU(RenderingDevice device, nint resource, uint layers = 2, uint levels = Levels,
        uint width = Width, DeviceFormat format = DeviceFormat.R8G8B8A8UNORM, Usage usage = GPUTransfer) =>
        device.TextureCreateFromExtension(layers == 1 ? DeviceShape.Texture : DeviceShape.TextureArray,
            format, RenderingDevice.TextureSamples.Samples1, usage, (ulong)resource, width, Height, 1, layers, levels);

    private static void VerifyGPUDevice(nint fixture, bool debug, bool preceding)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        var resource = CreateGPUResource(device, fixture, 2, Levels);
        RID imported = default;
        try
        {
            if (preceding)
            {
                Reject<NotSupportedException>(() => ImportGPU(device, resource));
                var buffer = device.StorageBufferCreate(4, [1, 2, 3, 4]);
                Check(device.BufferGetData(buffer).AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 4 }), "Refusal preserves native device operations.");
                return;
            }

            Reject<ArgumentException>(() => ImportGPU(device, resource, width: Width + 1));
            Reject<ArgumentException>(() => ImportGPU(device, resource, levels: 2));
            Reject<ArgumentException>(() => ImportGPU(device, resource, layers: 1));
            Reject<ArgumentException>(() => ImportGPU(device, resource, format: DeviceFormat.R8G8B8A8SRGB));
            Reject<NotSupportedException>(() => ImportGPU(device, resource, usage: Usage.Sampling | Usage.ColorAttachment));
            Reject<InvalidOperationException>(() => Task.Run(() => ImportGPU(device, resource)).GetAwaiter().GetResult());
            device.ComputeListBegin();
            try { Reject<InvalidOperationException>(() => ImportGPU(device, resource)); }
            finally { device.ComputeListEnd(); }
            foreach (var options in new uint[] { 1, 2 }) // Shared heap and simultaneous-access resource.
            {
                var incompatible = CreateGPUResource(device, fixture, 2, Levels, options);
                try { Reject<ArgumentException>(() => ImportGPU(device, incompatible)); }
                finally { Marshal.Release(incompatible); }
            }
            var foreignCreate = (delegate* unmanaged[Cdecl]<nint, uint, uint, int, nint*, int>)NativeLibrary.GetExport(fixture, "CreateSample");
            nint nativeForeign;
            Marshal.ThrowExceptionForHR(foreignCreate(0, 28, 2, 1, &nativeForeign));
            var foreign = nativeForeign;
            try { Reject<ArgumentException>(() => ImportGPU(device, foreign, levels: 1)); }
            finally { Marshal.Release(foreign); }

            imported = ImportGPU(device, resource);
            Reject<ArgumentException>(() => ImportGPU(device, resource));
            using (var other = device.CreateLocalDevice())
                Reject<ArgumentException>(() => ImportGPU(other, resource));

            Check(device.GetDriverResource(DriverResource.Texture, imported, 0) == (ulong)resource &&
                device.GetDriverResource(DriverResource.TextureDataFormat, imported, 0) == 28 &&
                device.GetDriverResource(DriverResource.TextureView, imported, 0) != 0, "The wrapped array exposes the producer allocation and an SDL-owned SRV.");
            Check(device.TextureGetData(imported, 0).AsSpan().SequenceEqual(GPUMipColors(0)) &&
                device.TextureGetData(imported, 1).AsSpan().SequenceEqual(GPUMipColors(2)), "All imported layers and mips keep producer pixels.");
            using var format = new RDTextureFormat
            {
                Width = Width >> 1,
                Height = Height >> 1,
                Format = DeviceFormat.R8G8B8A8UNORM,
                UsageBits = GPUTransfer,
            };
            using var view = new RDTextureView();
            var copy = device.TextureCreate(format, view);
            try
            {
                device.TextureCopy(imported, copy, default, default, new(Width >> 1, Height >> 1, 1), 1, 0, 1, 0);
                Check(device.TextureGetData(copy, 0).AsSpan().SequenceEqual(GPUMipColors(2).AsSpan(Width * Height * 4, (Width >> 1) * (Height >> 1) * 4)),
                    "Native copies address the imported nonzero layer and mip.");
                device.TextureCopy(copy, imported, default, default, new(Width >> 1, Height >> 1, 1), 0, 1, 0, 0);
                Check(device.TextureGetData(imported, 0).AsSpan(Width * Height * 4, (Width >> 1) * (Height >> 1) * 4)
                    .SequenceEqual(device.TextureGetData(copy, 0)), "Native copies update a selected imported mip without cycling its allocation.");
            }
            finally { device.FreeRID(copy); }

            var updated = GPUMipColors(1);
            device.TextureUpdate(imported, 1, updated);
            byte[]? retiredReadback = null;
            device.TextureGetDataAsync(imported, 1, data => retiredReadback = data);
            device.FreeRID(imported);
            Check(!device.TextureIsValid(imported) && retiredReadback is not null && retiredReadback.AsSpan().SequenceEqual(updated),
                "Free drains the imported update and async readback before returning the allocation.");
            imported = default;

            RewriteGPUResource(fixture, resource, 3);
            imported = ImportGPU(device, resource);
            Check(device.TextureGetData(imported, 0).AsSpan().SequenceEqual(GPUMipColors(3)), "The producer rewrites and reimports after retirement.");
            CheckGPUDebug(fixture, resource, debug);
            Marshal.Release(resource);
            resource = 0;
            Check(device.TextureGetData(imported, 1).AsSpan().SequenceEqual(GPUMipColors(1)), "The SDL wrapper retains the COM resource after producer release.");
        }
        finally
        {
            if (imported.IsValid() && device.TextureIsValid(imported)) device.FreeRID(imported);
            if (resource != 0) Marshal.Release(resource);
        }
    }

    private static void VerifyGPUWritableRefusal(nint fixture, bool debug)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        var resource = CreateGPUResource(device, fixture, 1, 1, options: 4);
        try
        {
            Reject<NotSupportedException>(() => ImportGPU(device, resource, 1, 1, usage: GPUTransfer | Usage.ColorAttachment));
            var sampled = ImportGPU(device, resource, 1, 1);
            try { Check(device.TextureGetData(sampled, 0).AsSpan().SequenceEqual(GPUMipColors(0).AsSpan(0, Width * Height * 4)), "The sampled-only ABI still imports and reads the same resource."); }
            finally { device.FreeRID(sampled); }
            CheckGPUDebug(fixture, resource, debug);
        }
        finally { Marshal.Release(resource); }
    }

    private static void VerifyGPUWritable(nint fixture, bool debug)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        VerifyGPUWritable(device, fixture, debug, layers: 1);
        VerifyGPUWritable(device, fixture, debug, layers: 2);
        VerifyGPUWritable(device, fixture, debug, layers: 1, color: false);
    }

    private static void VerifyGPUWritable(RenderingDevice device, nint fixture, bool debug, uint layers, bool color = true)
    {
        var usage = GPUTransfer;
        if (color) usage |= Usage.ColorAttachment;
        if (layers == 1) usage |= Usage.Storage;
        var levels = color ? 1u : Levels;
        const uint nativeRenderTarget = 4, nativeUAV = 8;
        var resource = CreateGPUResource(device, fixture, layers, levels, nativeRenderTarget | nativeUAV);
        RID imported = default;
        try
        {
            if (layers > 1)
                Reject<NotSupportedException>(() => ImportGPU(device, resource, layers, levels, usage: usage | Usage.Storage));
            else if (color)
            {
                var noTarget = CreateGPUResource(device, fixture, 1, levels, nativeUAV);
                var noUAV = CreateGPUResource(device, fixture, 1, levels, nativeRenderTarget);
                try
                {
                    Reject<ArgumentException>(() => ImportGPU(device, noTarget, 1, levels, usage: usage));
                    Reject<ArgumentException>(() => ImportGPU(device, noUAV, 1, levels, usage: usage));
                }
                finally { Marshal.Release(noTarget); Marshal.Release(noUAV); }
            }

            imported = ImportGPU(device, resource, layers, levels, usage: usage);
            Check(device.GetDriverResource(DriverResource.Texture, imported, 0) == (ulong)resource, "Writable operations must target the producer's allocation.");
            using (var format = device.TextureGetFormat(imported))
                Check(format.UsageBits == usage, "The imported RID retains its exact usage flags.");
            if (color)
            {
                var framebuffer = device.FramebufferCreate([imported], viewCount: (int)layers);
                try
                {
                    device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColor0, [Colors.Magenta]);
                    device.DrawListEnd();
                }
                finally { device.FreeRID(framebuffer); }
            }

            if (layers == 1) VulkanTextureImportTests.WriteImportedImage(device, imported, layers);
            for (uint layer = 0; layer < layers; layer++)
            {
                var expected = GPUMipColors((int)layer * 2).AsSpan();
                if (color) expected = expected[..(Width * Height * 4)];
                var texels = MemoryMarshal.Cast<byte, uint>(expected);
                if (color) texels.Fill(0xffff00ffu);
                if (layers == 1) texels[0] = color ? 0xff0000ffu : 0xff000000u;
                Check(device.TextureGetData(imported, layer).AsSpan().SequenceEqual(expected),
                    "Native framebuffer/compute writes must preserve untouched mips.");
            }

            device.FreeRID(imported);
            imported = default;
            RewriteGPUResource(fixture, resource, 3);
            imported = ImportGPU(device, resource, layers, levels, usage: usage);
            var rewritten = GPUMipColors(3).AsSpan();
            if (color) rewritten = rewritten[..(Width * Height * 4)];
            Check(device.TextureGetData(imported, 0).AsSpan().SequenceEqual(rewritten), "Retirement must restore the producer's sampled state.");
            CheckGPUDebug(fixture, resource, debug);
        }
        finally
        {
            if (imported.IsValid()) device.FreeRID(imported);
            Marshal.Release(resource);
        }
    }

    private static byte[] GPUMipColors(int phase)
    {
        ReadOnlySpan<uint> colors = [0xff0000ff, 0xff00ff00, 0xffff0000, 0xff00ffff];
        var pixels = new uint[Width * Height + (Width >> 1) * (Height >> 1) + (Width >> 2)];
        var offset = 0;
        for (var mip = 0; mip < Levels; mip++)
        {
            var length = Math.Max(1, Width >> mip) * Math.Max(1, Height >> mip);
            pixels.AsSpan(offset, length).Fill(colors[(phase + mip) % colors.Length]);
            offset += length;
        }
        return MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray();
    }

    private static void VerifyGPUCanvas(nint fixture, bool debug)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(48, 20), Title = "Direct3D 12 GPU texture import" };
        using var output = root.GetTexture();
        using var source = Image.CreateEmpty(Width, Height, false, Image.Format.Rgba8);
        source.Fill(Colors.Yellow);
        using var detail = ImageTexture.CreateFromImage(source);
        using var shader = TextureArrayTests.Load("ArrayGLSL");
        var complete = false;
        root.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingDriverName() == "direct3d12", "Expected GPU Direct3D 12.");
            var device = RenderingServer.GetRenderingDevice()!;
            var resources = new List<nint>();
            var rids = new List<RID>();
            try
            {
                var ordinary = CreateTrackedResource(1);
                var array = CreateTrackedResource(2);
                Reject<ArgumentException>(() => Import((ulong)array));
                var imageRID = Track(Import((ulong)ordinary));
                var arrayRID = Track(Import((ulong)array, layers: 2, type: RenderingServer.TextureType.Layered));
                var singleLayer = Track(Import((ulong)CreateTrackedResource(1), type: RenderingServer.TextureType.Layered));
                using (var pixels = RenderingServer.Texture2DLayerGet(singleLayer, 0))
                    Check(pixels?.GetPixel(0, 0) == Colors.Red, "An array with one layer retains its array view and producer pixels.");
                Reject<ArgumentOutOfRangeException>(() => RenderingServer.Texture2DLayerGet(singleLayer, 1));
                var proxy = Track(RenderingServer.TextureProxyCreate(imageRID));
                var layeredProxy = Track(RenderingServer.TextureProxyCreate(arrayRID));
                var imageDevice = RenderingServer.TextureGetRDTexture(proxy);
                var arrayDevice = RenderingServer.TextureGetRDTexture(layeredProxy);
                Check(RenderingServer.TextureGetNativeHandle(proxy) == (ulong)ordinary &&
                    RenderingServer.TextureGetNativeHandle(layeredProxy) == (ulong)array, "Both canvas proxies retain the producer allocation.");
                Reject<ArgumentException>(() => Import((ulong)ordinary));
                Reject<InvalidOperationException>(() => device.FreeRID(arrayDevice));
                var oldCPU = Track(RenderingServer.Texture2DCreate(source));
                var oldHandle = RenderingServer.TextureGetNativeHandle(oldCPU);
                Reject<ArgumentException>(() => Import(oldHandle));

                var canvas = Track(RenderingServer.CanvasCreate());
                var imageItem = Track(RenderingServer.CanvasItemCreate());
                var arrayItem = Track(RenderingServer.CanvasItemCreate());
                var shaderRID = Track(RenderingServer.ShaderCreate());
                var material = Track(RenderingServer.MaterialCreate());
                RenderingServer.ShaderSetSPIRV(shaderRID, shader.GetSPIRV());
                using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
                white.Fill(Colors.White);
                detail.SetImage(white);
                RenderingServer.ShaderSetDefaultTextureParameter(shaderRID, "detailMap", detail.GetRID());
                RenderingServer.MaterialSetShader(material, shaderRID);
                RenderingServer.MaterialSetParam(material, "layers", layeredProxy);
                RenderingServer.MaterialSetParam(material, "selectedLayer", 1f);
                RenderingServer.ViewportAttachCanvas(root.GetViewportRID(), canvas);
                RenderingServer.CanvasItemSetParent(imageItem, canvas);
                RenderingServer.CanvasItemSetParent(arrayItem, canvas);
                RenderingServer.CanvasItemSetMaterial(arrayItem, material);
                RenderingServer.CanvasItemAddTextureRect(imageItem, new(0, 0, 20, 12), proxy);
                RenderingServer.CanvasItemAddRect(arrayItem, new(24, 0, 20, 12), Colors.White);

                Draw(Colors.Red, Colors.Blue);
                using (var layer = RenderingServer.Texture2DLayerGet(layeredProxy, 1))
                    Check(layer?.GetPixel(0, 0) == Colors.Blue, "Layered readback samples the producer's second layer.");
                RenderingServer.Texture2DUpdate(imageRID, source);
                RenderingServer.Texture2DUpdate(arrayRID, source, 1);
                Draw(Colors.Yellow, Colors.Yellow);

                RenderingServer.TextureReplace(oldCPU, imageRID);
                rids.Remove(imageRID);
                Check(RenderingServer.TextureGetRDTexture(proxy) == imageDevice, "Replacing a canvas texture transfers the imported device root and redirects aliases.");
                for (var i = 0; i < 24; i++) RenderingServer.ForceDraw(swapBuffers: false);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) RenderingServer.ForceDraw(swapBuffers: false);
                Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 prepared Direct3D 12 frames allocate no managed owner bytes.");

                RenderingServer.FreeRID(oldCPU);
                rids.Remove(oldCPU);
                RenderingServer.FreeRID(arrayRID);
                rids.Remove(arrayRID);
                Check(!device.TextureIsValid(imageDevice) && !device.TextureIsValid(arrayDevice), "Canvas retirement returns both producer allocations.");
                RewriteGPUResource(fixture, ordinary, 1);
                var again = Track(Import((ulong)ordinary));
                using (var pixels = RenderingServer.Texture2DGet(again))
                    Check(pixels?.GetPixel(0, 0) == Colors.Green, "The producer writes the same image after canvas retirement.");
                CheckGPUDebug(fixture, ordinary, debug);
            }
            finally
            {
                for (var i = rids.Count - 1; i >= 0; i--) RenderingServer.FreeRID(rids[i]);
                foreach (var resource in resources) Marshal.Release(resource);
            }
            complete = true;
            root.Tree!.Quit();

            nint CreateTrackedResource(uint layers)
            {
                var resource = CreateGPUResource(device, fixture, layers, 1);
                resources.Add(resource);
                return resource;
            }

            RID Track(RID rid)
            {
                rids.Add(rid);
                return rid;
            }

            void Draw(Color image, Color layer)
            {
                RenderingServer.ForceDraw();
                using var frame = output.GetImage();
                Check(frame is not null && frame.GetPixel(3, 3) == image && frame.GetPixel(27, 3) == layer,
                    "The native canvas samples the external ordinary image and the array layer.");
            }
        };
        Check(Engine.Run(root) == 0 && complete, "The Direct3D 12 native canvas import completed.");
    }
}
