using Electron2D;
using SDL = SDL3.SDL;

internal static class CanvasCameraBackgroundTests
{
    private static readonly Vector2i Size = new(96, 64);

    internal static void Run(string profile)
    {
        if (profile == "managed") Managed();
        else Native(profile == "compatibility");
    }

    private static void Managed()
    {
        using var environment = new CanvasEnvironment { ResourceLocalToScene = true };
        Check(environment.BackgroundCameraFeedID == 1, "Initial camera ID.");
        try { environment.BackgroundCameraFeedID = -1; throw new InvalidOperationException("Accepted a negative camera ID."); }
        catch (ArgumentOutOfRangeException) { }
        environment.BackgroundMode = CanvasBackgroundMode.CameraFeed;
        environment.BackgroundCameraFeedID = 120;
        using var world = new WorldEnvironment { Environment = environment };
        using var packed = new PackedScene();
        packed.Pack(world);
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-camera-background-{Guid.NewGuid():N}.e2dscene");
        try
        {
            ResourceSaver.Save(packed, file);
            using var loaded = ResourceLoader.Load<PackedScene>(file, ResourceLoader.CacheMode.IgnoreDeep);
            using var instance = (WorldEnvironment)loaded.Instantiate();
            using var copy = instance.Environment!;
            Check(copy.BackgroundMode == CanvasBackgroundMode.CameraFeed && copy.BackgroundCameraFeedID == 120 &&
                !ReferenceEquals(copy, environment), "Typed scene copies the mode and process-local ID.");
        }
        finally { File.Delete(file); }
        Console.WriteLine("Camera background managed: defaults, validation and typed scene round trip passed.");
    }

    private static void Native(bool compatibility)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var feed = new CameraFeed();
        using var environment = new CanvasEnvironment
        {
            BackgroundMode = CanvasBackgroundMode.CameraFeed,
            BackgroundCameraFeedID = feed.GetID()
        };
        using var root = new Window { Title = "Canvas camera background", Size = Size, UseHDR2D = !compatibility };
        var child = new SubViewport { Size = Size, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        if (!compatibility) child.MSAA2D = Viewport.MSAA.Samples4X;
        root.AddChild(new WorldEnvironment { Environment = environment });
        root.AddChild(new ColorRect { Position = new(40, 24), Size = new(16, 16), Color = Colors.Yellow });
        root.AddChild(child);
        child.AddChild(new WorldEnvironment { Environment = environment });
        using var windowTexture = root.GetTexture();
        using var childTexture = child.GetTexture();
        using var colors = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        colors.SetPixel(0, 0, Colors.Red);
        colors.SetPixel(1, 0, Colors.Lime);
        colors.SetPixel(0, 1, Colors.Blue);
        colors.SetPixel(1, 1, Colors.White);
        using var y = Image.CreateFromData(2, 2, false, Image.Format.R8, [100, 100, 100, 100]);
        using var cbcr = Image.CreateFromData(1, 1, false, Image.Format.Rg8, [64, 192]);
        using var packed = Image.CreateFromData(2, 2, false, Image.Format.Rg8, [100, 64, 100, 192, 100, 64, 100, 192]);
        var finished = false;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Magenta);
            RenderingServer.ForceDraw();
            Expect(windowTexture, 16, 16, Colors.Magenta, "Unregistered source falls back to the session clear");
            CameraServer.AddFeed(feed);
            RenderingServer.ForceDraw();
            Check(!feed.FeedIsActive, "Rendering and registration never activate a camera.");
            Expect(childTexture, 16, 16, Colors.Magenta, "Inactive source falls back");

            feed.FeedIsActive = true;
            RenderingServer.ForceDraw();
            Expect(childTexture, 16, 16, Colors.Magenta, "Active source waits for its first frame");
            feed.SetRGBImage(colors);
            CameraServer.Process();
            RenderingServer.ForceDraw();
            Expect(windowTexture, 16, 16, Colors.Blue, "Default vertical feed transform");
            Expect(childTexture, 16, 16, Colors.Blue, "Shared camera background reaches the independent viewport");
            Expect(windowTexture, 48, 32, Colors.Yellow, "Foreground draws after the camera");

            child.RenderTargetUpdateMode = ViewportUpdateMode.Disabled;
            RenderingServer.ForceDraw();
            RenderingServer.ForceDraw();
            Check(!RenderingServer.HasChanged(), "The camera does not require redraws between delivered frames.");
            feed.SetRGBImage(colors);
            CameraServer.Process();
            Check(RenderingServer.HasChanged(), "A delivered camera frame wakes an otherwise idle viewport.");
            child.RenderTargetUpdateMode = ViewportUpdateMode.Always;
            feed.FeedTransform = Transform.Identity;
            RenderingServer.ForceDraw();
            Expect(childTexture, 16, 16, Colors.Red, "Identity samples the top source row");
            Expect(childTexture, 80, 16, Colors.Lime, "The camera fills the viewport width");
            feed.FeedTransform = new(1, 0, 0, 1, -.25f, 0);
            RenderingServer.ForceDraw();
            Expect(childTexture, 8, 16, Colors.Red, "Camera UVs clamp beyond the frame edge");
            feed.FeedTransform = Transform.Identity;

            var expected = new Color(190 / 255f, 76 / 255f, 0, 1);
            feed.SetYCBCRImages(y, cbcr);
            RenderingServer.ForceDraw();
            Expect(childTexture, 16, 16, expected, "Separate full-range BT.601 with shared chroma");
            using (var oddLuma = Image.CreateFromData(3, 1, false, Image.Format.R8, [100, 100, 100]))
            using (var oddChroma = Image.CreateFromData(2, 1, false, Image.Format.Rg8, [128, 128, 64, 192]))
            {
                feed.SetYCBCRImages(oddLuma, oddChroma);
                RenderingServer.ForceDraw();
                Expect(childTexture, 48, 16, expected, "Nearest chroma samples normalized luminance pixel centers");
            }
            feed.SetYCBCRImage(packed);
            RenderingServer.ForceDraw();
            Expect(childTexture, 80, 16, expected, "Packed YUYV shares chroma between adjacent pixels");

            using (var channels = Image.CreateFromData(1, 1, false, Image.Format.Rgb8, [100, 64, 192]))
            {
                feed.SetYCBCRImage(channels);
                RenderingServer.ForceDraw();
                Expect(childTexture, 16, 16, expected, "Packed Y, Cb and Cr channels");
            }
            using (var odd = Image.CreateFromData(1, 1, false, Image.Format.Rg8, [100, 64]))
            {
                feed.SetYCBCRImage(odd);
                RenderingServer.ForceDraw();
                Expect(childTexture, 16, 16, Colors.Magenta, "Incomplete YUYV pair falls back to clear");
            }
            using (var limited = Image.CreateFromData(2, 2, false, Image.Format.Rg8, [16, 128, 235, 128, 16, 128, 235, 128]))
            {
                var prepared = TexturePixels.FromImage(limited);
                feed.SubmitPrepared(prepared, null, CameraFeed.FeedDataType.YCBCR, SDL.Colorspace.BT709Limited);
                RenderingServer.ForceDraw();
                Expect(childTexture, 16, 16, Colors.Black, "Limited-range metadata maps studio black");
                Expect(childTexture, 80, 16, Colors.White, "Limited-range metadata maps studio white");
                for (var i = 0; i < 48; i++) RenderingServer.ForceDraw();
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++)
                {
                    feed.SubmitPrepared(prepared, null, CameraFeed.FeedDataType.YCBCR, SDL.Colorspace.BT709Limited);
                    RenderingServer.ForceDraw();
                }
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                Check(bytes == 0, $"Prepared camera updates allocated {bytes} owner bytes.");
                Console.WriteLine($"Camera background 64 live prepared frames ({bytes} owner bytes).");
            }

            feed.FeedIsActive = false;
            RenderingServer.ForceDraw();
            Expect(childTexture, 16, 16, Colors.Magenta, "Deactivation stops using the frame");
            feed.FeedIsActive = true;
            RenderingServer.ForceDraw();
            Expect(childTexture, 16, 16, Colors.Magenta, "Reactivation waits for fresh pixels");
            feed.SetRGBImage(colors);
            var rootRID = root.GetViewportRID();
            RenderingServer.ViewportSetEnvironmentMode(rootRID, RenderingServer.ViewportEnvironmentMode.Disabled);
            RenderingServer.ForceDraw();
            Expect(childTexture, 16, 16, Colors.Magenta, "Inherited environment bypass");
            RenderingServer.ViewportSetEnvironmentMode(rootRID, RenderingServer.ViewportEnvironmentMode.Enabled);
            if (!compatibility) environment.TonemapExposure = .25f;
            RenderingServer.ForceDraw();
            using (var image = windowTexture.GetImage()!)
            {
                var red = image.GetPixel(16, 16).R;
                Check(MathF.Abs(red - (compatibility ? 1 : .25f)) < .005f, "Camera enters the scene before tone mapping.");
            }
            environment.TonemapExposure = 1;
            CameraServer.RemoveFeed(feed);
            RenderingServer.ForceDraw();
            Expect(childTexture, 16, 16, Colors.Magenta, "Removed source stops supplying pixels.");
            Check(feed.FeedIsActive, "A caller-owned removed feed retains its activity.");
            CameraServer.AddFeed(feed);
            RenderingServer.ForceDraw();
            Expect(childTexture, 16, 16, Colors.Red, "Re-registered live source supplies its current frame.");
            environment.BackgroundCameraFeedID = 0;
            RenderingServer.ForceDraw();
            Expect(childTexture, 16, 16, Colors.Magenta, "Zero feed ID selects no source");

            finished = true;
            root.Tree!.Quit();
        };
        try { Check(Engine.Run(root) == 0 && finished, "Native camera background scenario completed."); }
        finally
        {
            if (CameraServer.Contains(feed)) CameraServer.RemoveFeed(feed);
            feed.FeedIsActive = false;
        }
        Console.WriteLine($"Camera background {(compatibility ? "Compatibility" : "GPU")}: pixels, transforms, live formats, composition, and feed lifecycle passed.");
    }

    private static void Expect(ViewportTexture texture, int x, int y, Color color, string label)
    {
        using var image = texture.GetImage()!;
        if (image.PixelFormat == Image.Format.Rgbah) color = color.SRGBToLinear();
        var actual = image.GetPixel(x, y);
        const float epsilon = .012f;
        Check(MathF.Abs(actual.R - color.R) < epsilon && MathF.Abs(actual.G - color.G) < epsilon &&
            MathF.Abs(actual.B - color.B) < epsilon && MathF.Abs(actual.A - color.A) < epsilon, $"{label}: expected {color}, read {actual}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
