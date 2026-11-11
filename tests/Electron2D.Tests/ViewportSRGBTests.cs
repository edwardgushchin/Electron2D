using Electron2D;
using static TextureArrayTests;

internal static class ViewportSRGBTests
{
    private sealed class Fill(Color color) : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 16, 8), color);
        internal void SetColor(Color value)
        {
            color = value;
            QueueRedraw();
        }
    }

    private sealed class Sample(Texture texture) : Entity
    {
        protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 12, 12), false);
    }

    internal static void Run(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var sampled = new TextureRD();
        var root = new Window { Size = new(40, 24) };
        var viewport = new SubViewport { Size = new(8, 8), RenderTargetUpdateMode = ViewportUpdateMode.Always };
        var fill = new Fill(new(64 / 255f, 128 / 255f, 192 / 255f));
        viewport.AddChild(fill);
        root.AddChild(viewport);
        var finished = false;

        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!;
            Check(RenderingServer.GetCurrentRenderingDriverName() == (profile == "preceding" ? "vulkan" : profile), "The requested GPU driver is active.");
            var canvas = viewport.GetTexture().GetRID();
            Check(!RenderingServer.TextureGetRDTexture(canvas, true).IsValid(), "An unrendered viewport has no completed image.");
            RID linear = default, SRGB = default, shared = default;
            var queued = new byte[8 * 8 * 4];
            var delivered = false;
            var frame = 0;
            RenderingServer.FramePostDraw += AfterFrame;

            void AfterFrame()
            {
                switch (frame++)
                {
                    case 0:
                        // Export a previously drawn viewport, with the linear RID requested first.
                        linear = RenderingServer.TextureGetRDTexture(canvas);
                        PixelBytes(device.TextureGetData(linear, 0), 64, 128, 192);
                        if (profile != "vulkan")
                        {
                            Reject<NotSupportedException>(() => RenderingServer.TextureGetRDTexture(canvas, true));
                            Check(RenderingServer.TextureGetRDTexture(canvas) == linear, "Distinct-view refusal preserves linear identity.");
                            Finish();
                            return;
                        }
                        using (var view = new RDTextureView())
                            shared = device.TextureCreateSharedFromSlice(view, linear, 0, 0);
                        SRGB = RenderingServer.TextureGetRDTexture(canvas, true);
                        Check(device.TextureIsShared(SRGB) && SRGB != linear, "The viewport exports a distinct shared view.");
                        using (var description = device.TextureGetFormat(SRGB))
                            Check(description.UsageBits == (RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyFrom |
                                RenderingDevice.TextureUsageBits.CanCopyTo | RenderingDevice.TextureUsageBits.CanUpdate), "The view exposes sampling and raw transfers.");
                        PixelBytes(device.TextureGetData(SRGB, 0), 64, 128, 192);
                        sampled.TextureRDRID = SRGB;
                        root.AddChild(new Sample(viewport.GetTexture()));
                        root.AddChild(new Sample(sampled) { Position = new(12, 4) });
                        device.TextureGetDataAsync(SRGB, 0, queued.AsMemory(), () => delivered = true);
                        fill.SetColor(new(128 / 255f, 64 / 255f, 192 / 255f));
                        return;
                    case 1:
                        Check(delivered, "The queued read completed before the next frame callback.");
                        PixelBytes(queued, 64, 128, 192);
                        Check(RenderingServer.TextureGetRDTexture(canvas) == linear && RenderingServer.TextureGetRDTexture(canvas, true) == SRGB, "The completed buffer swap keeps both RIDs.");
                        PixelBytes(device.TextureGetData(shared, 0), 128, 64, 192);
                        PixelBytes(device.TextureGetData(SRGB, 0), 128, 64, 192);
                        SamplePixel(root, 55, 13, 134);
                        viewport.RenderTargetUpdateMode = ViewportUpdateMode.Disabled;
                        using (var update = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8))
                        {
                            update.Fill(new(192 / 255f, 128 / 255f, 64 / 255f));
                            device.TextureUpdate(SRGB, 0, update.GetData());
                        }
                        return;
                    case 2:
                        PixelBytes(device.TextureGetData(linear, 0), 192, 128, 64);
                        SamplePixel(root, 134, 55, 13);
                        delivered = false;
                        device.TextureGetDataAsync(SRGB, 0, queued.AsMemory(), () => delivered = true);
                        sampled.TextureRDRID = default;
                        viewport.Size = new(16, 8);
                        viewport.MSAA2D = Viewport.MSAA.Samples4X;
                        viewport.RenderTargetUpdateMode = ViewportUpdateMode.Always;
                        return;
                    case 3:
                        Check(delivered && !device.TextureIsValid(SRGB) && !device.TextureIsValid(linear) && !device.TextureIsValid(shared),
                            "Resize preserves pending reads and expires the previous pair and views.");
                        PixelBytes(queued, 192, 128, 64);
                        device.FreeRID(linear);
                        var resolved = RenderingServer.TextureGetRDTexture(canvas, true);
                        using (var format = device.TextureGetFormat(resolved))
                            Check(format.Width == 16 && device.TextureIsShared(resolved), "The resolved multisample frame gets a new single-sample sRGB view.");
                        PixelBytes(device.TextureGetData(resolved, 0), 128, 64, 192);
                        device.FreeRID(resolved);
                        Check(RenderingServer.TextureGetRDTexture(canvas, true) != resolved, "Freeing a view allows it to be exported again.");
                        viewport.MSAA2D = Viewport.MSAA.Disabled;
                        viewport.UseHDR2D = true;
                        return;
                    default:
                        Reject<NotSupportedException>(() => RenderingServer.TextureGetRDTexture(canvas, true));
                        var hdr = RenderingServer.TextureGetRDTexture(canvas);
                        using (var format = device.TextureGetFormat(hdr))
                            Check(format.Format == RenderingDevice.DataFormat.R16G16B16A16SFLOAT && device.TextureGetData(hdr, 0).Length == 16 * 8 * 8,
                                "Linear HDR export retains its half-float storage.");
                        Finish();
                        return;
                }
            }

            void Finish()
            {
                RenderingServer.FramePostDraw -= AfterFrame;
                finished = true;
                root.Tree!.Quit();
            }
        };
        Check(Engine.Run(root) == 0 && finished, "The viewport sRGB fixture completed.");
        Console.WriteLine($"Viewport sRGB export verified ({profile}).");
    }

    private static void PixelBytes(byte[] bytes, byte red, byte green, byte blue)
    {
        Check(bytes[0] == red && bytes[1] == green && bytes[2] == blue && bytes[3] == 255,
            $"Encoded viewport pixel ({bytes[0]}, {bytes[1]}, {bytes[2]}, {bytes[3]}). Expected ({red}, {green}, {blue}, 255).");
    }

    private static void SamplePixel(Window root, byte red, byte green, byte blue)
    {
        using var image = root.GetTexture().GetImage()!;
        var color = image.GetPixel(18, 10);
        Check(Math.Abs(color.R * 255 - red) < 2 && Math.Abs(color.G * 255 - green) < 2 &&
            Math.Abs(color.B * 255 - blue) < 2, $"Sampled viewport pixel {color}. Expected ({red}, {green}, {blue}).");
    }
}
