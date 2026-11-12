using System.Runtime.InteropServices;
using Electron2D;
using SDL = SDL3.SDL;

internal static unsafe class NativeTextureHandleTests
{
    internal static void Run(string profile)
    {
        Reject<InvalidOperationException>(() => RenderingServer.TextureGetNativeHandle(default));
        var gpu = profile is "vulkan" or "gpu-direct3d12" or "gpu-direct3d12-preceding";
        var unsupported = profile is "software" or "gpu-direct3d12-preceding";
        var driver = profile switch
        {
            "gpu-direct3d12" or "gpu-direct3d12-preceding" => "direct3d12",
            "compatibility-vulkan" => "vulkan",
            _ => profile
        };
        ProjectSettings.Set(ProjectSettings.RenderingMethod, gpu ? "gpu" : "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);

        using var original = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8);
        original.Fill(Colors.Red);
        using var update = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8);
        update.Fill(Colors.Lime);
        using var larger = Image.CreateEmpty(16, 8, false, Image.Format.Rgba8);
        larger.Fill(Colors.Blue);
        using var image = ImageTexture.CreateFromImage(original);
        using var cold = ImageTexture.CreateFromImage(original);
        using var empty = new ImageTexture();
        using var channels = new CanvasTexture { DiffuseTexture = image };
        using var root = new Window { Title = "Native texture export", Size = new(48, 32) };
        root.AddChild(new TextureRect { Texture = image, Size = new(8, 4), TextureFilter = TextureFilter.Nearest });
        var viewport = new SubViewport { Size = new(12, 10), RenderTargetUpdateMode = ViewportUpdateMode.Disabled };
        viewport.AddChild(new ColorRect { Size = new(20, 20), Color = Colors.Yellow });
        root.AddChild(viewport);
        using var target = viewport.GetTexture();
        using var rootTexture = root.GetTexture();

        var complete = false;
        root.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingDriverName() == driver, $"Expected the {driver} driver.");
            var rid = image.GetRID();
            if (unsupported)
            {
                Reject<NotSupportedException>(() => RenderingServer.TextureGetNativeHandle(rid));
                RenderingServer.ForceDraw();
                ExpectPixel(rootTexture, Colors.Red, "Refusal preserves ordinary rendering.");
                Finish();
                return;
            }

            Reject<ArgumentException>(() => RenderingServer.TextureGetNativeHandle(root.GetViewportRID()));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.TextureGetNativeHandle(rid)).GetAwaiter().GetResult());
            Check(RenderingServer.TextureGetNativeHandle(empty.GetRID()) == 0, "Uninitialized texture has no allocation.");
            Check(RenderingServer.TextureGetNativeHandle(target.GetRID()) == 0, "Unrendered viewport has no completed texture.");

            var first = RenderingServer.TextureGetNativeHandle(rid);
            Check(first != 0 && RenderingServer.TextureGetNativeHandle(rid, SRGB: true) == first,
                "Both encodings select the same native allocation.");
            Check(RenderingServer.TextureGetNativeHandle(channels.GetRID()) == first, "The diffuse channel shares its actual storage.");
            var idle = RenderingServer.TextureGetNativeHandle(cold.GetRID());
            RenderingServer.ForceDraw();
            ExpectPixel(rootTexture, Colors.Red, "The exported texture still draws its authored pixels.");
            Check(RenderingServer.TextureGetNativeHandle(cold.GetRID()) == idle, "An explicitly exported undrawn upload survives a frame.");
            Inspect(driver, gpu, first, 8, 4);
            for (var i = 0; i < 64; i++) RenderingServer.TextureGetNativeHandle(rid);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.TextureGetNativeHandle(rid);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared export allocates no managed owner bytes.");

            if (gpu)
            {
                var device = RenderingServer.GetRenderingDevice()!;
                var linear = RenderingServer.TextureGetRDTexture(rid);
                Check(RenderingServer.TextureGetRDTexture(channels.GetRID()) == linear &&
                    device.GetDriverResource(RenderingDevice.DriverResource.Texture, linear, 0) == first,
                    "Server, diffuse alias and device export share the same allocation.");
                if (driver == "vulkan")
                {
                    var srgb = RenderingServer.TextureGetRDTexture(rid, true);
                    Check(linear != srgb && device.GetDriverResource(RenderingDevice.DriverResource.Texture, srgb, 0) == first &&
                        device.GetDriverResource(RenderingDevice.DriverResource.TextureDataFormat, srgb, 0) == 43,
                        "The Vulkan export shares the VkImage with a real sRGB view.");
                }
                var wrapper = RenderingServer.TextureRDCreate(linear);
                Check(RenderingServer.TextureGetNativeHandle(wrapper) == first, "A device-backed wrapper shares the native image.");
                RenderingServer.FreeRID(wrapper);
                Check(RenderingServer.TextureGetNativeHandle(rid) == first, "Freeing a wrapper preserves its source.");

                using var array = new TextureArray();
                array.CreateFromImages([original, update]);
                var arrayRID = array.GetRID();
                var nativeArray = RenderingServer.TextureGetNativeHandle(arrayRID);
                var arrayView = RenderingServer.TextureGetRDTexture(arrayRID, SRGB: driver == "vulkan");
                Check(nativeArray != 0 && device.GetDriverResource(RenderingDevice.DriverResource.Texture, arrayView, 0) == nativeArray,
                    "An independent image array exports its native allocation and view.");
                if (driver == "direct3d12") DriverResourceTests.InspectDirect3DTexture(nativeArray, 8, 4, 2);
            }
            else
            {
                using var array = new TextureArray();
                array.CreateFromImages([original]);
                Reject<NotSupportedException>(() => RenderingServer.TextureGetNativeHandle(array.GetRID()));
            }

            image.Update(update);
            RenderingServer.ForceDraw();
            Check(RenderingServer.TextureGetNativeHandle(rid) == first, "A compatible pixel update keeps the allocation.");
            ExpectPixel(rootTexture, Colors.Lime, "Updated pixels use the exported allocation.");
            image.SetImage(larger);
            var replacement = RenderingServer.TextureGetNativeHandle(rid);
            Check(replacement != first && replacement != 0, "Storage replacement exports a different native allocation.");
            Inspect(driver, gpu, replacement, 16, 8);
            RenderingServer.ForceDraw();
            ExpectPixel(rootTexture, Colors.Blue, "Replacement pixels still draw.");
            var expired = cold.GetRID();
            cold.Dispose();
            Reject<ArgumentException>(() => RenderingServer.TextureGetNativeHandle(expired));

            viewport.RenderTargetUpdateMode = ViewportUpdateMode.Always;
            RenderingServer.ForceDraw();
            var completed = RenderingServer.TextureGetNativeHandle(target.GetRID());
            Inspect(driver, gpu, completed, 12, 10);
            RenderingServer.ForceDraw();
            var next = RenderingServer.TextureGetNativeHandle(target.GetRID(), true);
            Check(next != completed && next != 0, "The viewport exports the new completed image.");
            RenderingServer.ForceDraw();
            Check(RenderingServer.TextureGetNativeHandle(target.GetRID()) == completed, "Completed images alternate without exporting the pending image.");
            viewport.Size = new(16, 14);
            RenderingServer.ForceDraw();
            var resized = RenderingServer.TextureGetNativeHandle(target.GetRID());
            Check(resized != 0, "A resized target publishes a native allocation.");
            Inspect(driver, gpu, resized, 16, 14);
            ExpectPixel(target, Colors.Yellow, "The resized target still renders its viewport.");
            if (gpu)
            {
                viewport.UseHDR2D = true;
                RenderingServer.ForceDraw();
                var hdr = RenderingServer.TextureGetNativeHandle(target.GetRID());
                Check(hdr != 0 && RenderingServer.TextureGetNativeHandle(target.GetRID(), true) == hdr,
                    "An HDR allocation without an alternate sRGB view still exports its actual image.");
            }
            else
            {
                RenderingServer.ViewportSetRenderDirectToScreen(root.GetViewportRID(), true);
                Check(RenderingServer.TextureGetNativeHandle(rootTexture.GetRID()) == 0,
                    "Direct presentation releases the texture-backed allocation.");
            }
            Finish();

            void Finish()
            {
                complete = true;
                root.Tree!.Quit();
            }
        };
        Check(Engine.Run(root) == 0 && complete, "Native texture export scenario completed.");
        Console.WriteLine($"Native texture export {profile}: {(unsupported ? "explicit refusal and ordinary rendering" : "native allocation, updates, aliasing, completed viewport images and rendering")} passed.");
    }

    private static void Inspect(string driver, bool gpu, ulong value, uint width, uint height)
    {
        Check(value != 0, "The native object is available.");
        if (driver == "direct3d11")
        {
            // ID3D11Texture2D::GetDesc, after IUnknown, ID3D11DeviceChild and ID3D11Resource.
            var texture = (nint)value;
            var getDescription = (delegate* unmanaged<nint, TextureDescription*, void>)(*(nint**)texture)[10];
            TextureDescription description = default;
            getDescription(texture, &description);
            Check(description.Width == width && description.Height == height && description.MipLevels != 0 &&
                description.ArraySize == 1 && description.Format != 0 && description.SampleCount == 1,
                "The D3D11 texture object reports its actual dimensions, format and samples.");
        }
        else if (gpu && driver == "direct3d12")
            DriverResourceTests.InspectDirect3DTexture(value, width, height, 1);
        else if (driver is "opengl" or "opengles2")
        {
            var isTexture = (delegate* unmanaged<uint, byte>)SDL.GLGetProcAddress("glIsTexture");
            Check(isTexture != null && isTexture(checked((uint)value)) != 0, "The active GL context recognizes the exported texture name.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextureDescription
    {
        internal uint Width, Height, MipLevels, ArraySize, Format, SampleCount, SampleQuality, Usage, Bind, CPUAccess, Flags;
    }

    private static void ExpectPixel(ViewportTexture texture, Color color, string message)
    {
        using var image = texture.GetImage()!;
        Check(image.GetPixel(2, 2) == color, message);
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
