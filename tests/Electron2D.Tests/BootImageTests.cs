using Electron2D;
using static TextureArrayTests;
using Stretch = Electron2D.RenderingServer.SplashStretchMode;

internal static class BootImageTests
{
    internal static void Managed()
    {
        Reject<InvalidOperationException>(() => RenderingServer.SetBootImageWithStretch(null, Colors.Black, Stretch.Disabled));
        Check(typeof(RenderingServer).GetMethod("SetBootImage") is null, "The deprecated loading method has no public mirror.");
        Check(Enum.GetValues<Stretch>().Select(x => (int)x).SequenceEqual(Enumerable.Range(0, 6)), "The complete six-mode loading domain retains numeric identity.");
        Console.WriteLine("Boot image unavailable-session and applicable public family passed.");
    }
    private sealed class Scene : Entity
    { protected override void OnDraw() => DrawRect(new(4, 4, 20, 20), Colors.White); }
    internal static void Native(string backend)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback); var fps = Engine.MaxFPS;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 0;
            foreach (var size in new[] { new Vector2i(256, 192), new Vector2i(256, 384), new Vector2i(256, 256) }) Session(backend, size);
            ScaledScene();
            if (backend == "compatibility") Transparent();
            else
            {
                using var rejected = new Window { Size = new(256, 192), Transparent = true, Borderless = true }; Reject<NotSupportedException>(() => Engine.Run(rejected)); Check(rejected.IsDisposed && !RenderingServer.IsAvailable && !DisplayServer.IsAvailable, "Strict GPU transparency rejects and retires its attempted main-window lifetime.");
                ProjectSettings.Set(ProjectSettings.RenderingFallback, true); Transparent(); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            }
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
        Managed(); Console.WriteLine(backend + ": native boot geometry/filter/alpha, immediate readiness output, scene/clock/callback isolation, temporary storage, failure recovery and zero prepared scene bytes passed.");
    }
    private static Image Pattern(int width, int height)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) image.SetPixel(x, y, y < height / 2 ? x < width / 2 ? Colors.Red : Colors.Green : x < width / 2 ? Colors.Yellow : Colors.Magenta);
        return image;
    }
    private static void Session(string backend, Vector2i size)
    {
        using var source = Pattern(2, 2); using var retained = ImageTexture.CreateFromImage(source);
        using var root = new Window { Size = size, Borderless = true, Title = "Electron2D loading frame" }; root.AddChild(new Scene()); root.AddChild(new Sprite { Texture = retained, Position = new(100, 100) });
        root.Ready += _ =>
        {
            var requested = size; size = root.Size; Check(size.X == requested.X && Math.Abs(size.Y - requested.Y) <= 1, "Native test clients retain the selected aspect fixture, allowing one-pixel density rounding.");
            RenderingServer.RenderLoopEnabled = false; using var first = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); first.Fill(Colors.Red); var initialFrames = Engine.GetFramesDrawn();
            RenderingServer.SetBootImageWithStretch(first, Colors.Black, Stretch.Ignore, false); using (var initial = RenderingServer.Service!.Readback()) Check(initial.GetPixel(8, 8).R > .99f, "Readiness can present before any scene target has been created."); Check(Engine.GetFramesDrawn() == initialFrames && root.GetTexture().GetImage() is null, "The first loading image creates no scene frame or viewport image.");
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.ForceDraw();
            using var view = root.GetTexture(); using var previous = view.GetImage()!; var scenePixels = previous.GetData();
            var exported = backend == "gpu" ? RenderingServer.TextureGetRDTexture(retained.GetRID()) : default;
            var drawn = Engine.GetFramesDrawn(); var process = Engine.ProcessFrames; var physics = Engine.PhysicsFrames; var pre = 0; var post = 0; var callbacks = 0;
            Action before = () => pre++; Action after = () => post++; RenderingServer.FramePreDraw += before; RenderingServer.FramePostDraw += after; RenderingServer.RequestFrameDrawnCallback(() => callbacks++);
            var native = typeof(RenderingServer).GetField("_backend", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(RenderingServer.Service)!;
            var cache = (System.Collections.IDictionary)native.GetType().GetField("_textures", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(native)!; var cachedBefore = cache.Count;
            using var wide = Pattern(64, 32); using var tall = Pattern(16, 32); using var odd = Pattern(9, 5);
            foreach (var image in new[] { wide, tall, odd })
                foreach (var mode in image == odd ? new[] { Stretch.Disabled } : Enum.GetValues<Stretch>())
                {
                    RenderingServer.SetBootImageWithStretch(image, Colors.Blue, mode, false);
                    using var capture = RenderingServer.Service!.Readback(); Check(capture.Size == size, $"Loading client {capture.Size}; requested {size}; authored {root.Size}.");
                    var rectangle = Expected(image.Size, size, mode); CheckPixels(capture, image, rectangle);
                    Check(cache.Count <= cachedBefore && cache.Keys.Cast<Texture>().All(t => !t.IsDisposed), "Loading images leave only the existing live native texture cache entries.");
                    using var unchanged = view.GetImage()!; Check(unchanged.GetData().SequenceEqual(scenePixels), "Loading output preserves the completed viewport texture.");
                    Check(Engine.GetFramesDrawn() == drawn && Engine.ProcessFrames == process && Engine.PhysicsFrames == physics && pre == 0 && post == 0 && callbacks == 0, "Boot presentation neither advances counters nor consumes scene callbacks.");
                    if (backend == "gpu") Check(RenderingServer.TextureGetRDTexture(retained.GetRID()) == exported, "Loading does not retire a borrowed scene-image device export.");
                    Check(RenderingServer.HasChanged(), "Loading output leaves pending scene work for a normal replacement.");
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_BOOT_IMAGE_OUTPUT") is { } output) { Directory.CreateDirectory(output); capture.SavePNG(System.IO.Path.Combine(output, $"{backend}-{size.X}x{size.Y}-{image.Width}x{image.Height}-{mode}.png")); }
                }
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.SetBootImageWithStretch(wide, Colors.Blue, Stretch.Ignore))).GetAwaiter().GetResult();
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.SetBootImageWithStretch(wide, Colors.Blue, (Stretch)6)); Reject<ArgumentException>(() => RenderingServer.SetBootImageWithStretch(wide, new(float.NaN, 0, 0), Stretch.Ignore));
            using var disposed = Pattern(2, 2); disposed.Dispose(); Reject<ObjectDisposedException>(() => RenderingServer.SetBootImageWithStretch(disposed, Colors.Blue, Stretch.Ignore));
            using var unchangedBefore = RenderingServer.Service!.Readback(); RenderingServer.SetBootImageWithStretch(null, Colors.Black, Stretch.Ignore); using var empty = new Image(); RenderingServer.SetBootImageWithStretch(empty, Colors.Black, Stretch.Ignore); using var unchangedAfter = RenderingServer.Service.Readback(); Check(unchangedAfter.GetData().SequenceEqual(unchangedBefore.GetData()), "Null/empty inputs preserve the already presented image.");
            using var alpha = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); alpha.Fill(new(1, 0, 0, .5f)); RenderingServer.SetBootImageWithStretch(alpha, new(0, 0, 1, .2f), Stretch.Ignore, false); alpha.Fill(Colors.Green);
            using (var pixels = RenderingServer.Service.Readback()) { var color = pixels.GetPixel(size.X / 2, size.Y / 2); Check(Math.Abs(color.R - .5f) < .02 && Math.Abs(color.B - .5f) < .02 && color.A > .99f, "Copied alpha samples blend over an opaque native background."); }
            Filtering(size);
            using var integer = Image.CreateEmpty(2, 2, false, Image.Format.Rgba16I); Reject<NotSupportedException>(() => RenderingServer.SetBootImageWithStretch(integer, Colors.Black, Stretch.Ignore)); RenderingServer.SetBootImageWithStretch(wide, Colors.Blue, Stretch.Ignore, false);
            var particle = default(RID); RenderingParticleState? pending = null;
            if (backend == "gpu")
            {
                particle = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particle, 1); RenderingServer.ParticlesRequestProcessTime(particle, .125f);
                pending = ((Dictionary<RID, RenderingParticleState>)typeof(RenderingServer).GetField("_particles", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(RenderingServer.Service)!)[particle];
                var phase = pending.Phase; var requestedTime = pending.RequestTime; var lastFrame = pending.LastFrame;
                RenderingServer.SetBootImageWithStretch(wide, Colors.Blue, Stretch.Ignore, false);
                Check(pending.GPU is null && pending.Requested && pending.Phase == phase && pending.RequestTime == requestedTime && pending.LastFrame == lastFrame, "Boot presentation preserves pending particle work without native simulation.");
            }
            Action reentry = () => Reject<InvalidOperationException>(() => RenderingServer.SetBootImageWithStretch(wide, Colors.Blue, Stretch.Ignore)); RenderingServer.FramePreDraw += reentry;
            try { RenderingServer.ForceDraw(); } finally { RenderingServer.FramePreDraw -= reentry; }
            if (pending is not null) { Check(pending.GPU is { Initialized: true } && !pending.Requested && pending.RequestTime == 0, "The subsequent scene submission consumes the preserved particle request."); RenderingServer.FreeRID(particle); }
            Check(pre == 1 && post == 1 && callbacks == 1 && Engine.GetFramesDrawn() == drawn + 1, "The subsequent scene frame delivers its original callbacks once after a rejected reentry.");
            using (var restored = RenderingServer.Service.Readback()) { Check(restored.GetPixel(8, 8).R > .99f && restored.GetPixel(40, 40).R < .01f, "Normal scene output replaces the loading frame."); }
            for (var i = 0; i < 16; i++) RenderingServer.ForceDraw(); var start = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) RenderingServer.ForceDraw(); Check(GC.GetAllocatedBytesForCurrentThread() == start, "Prepared scene rendering after loading allocates zero owner bytes.");
            RenderingServer.FramePreDraw -= before; RenderingServer.FramePostDraw -= after; root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && root.IsDisposed, "Loading session retires cleanly.");
    }
    private static void ScaledScene()
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.Red);
        using var root = new Window { Size = new(256, 192), Borderless = true, ContentScaleMode = ContentScaleMode.Viewport, ContentScaleSize = new(64, 64) }; root.AddChild(new Scene());
        root.Ready += _ =>
        {
            RenderingServer.RenderLoopEnabled = false; RenderingServer.ForceDraw(); using var before = root.GetTexture().GetImage()!; Check(before.Size == new Vector2i(64, 64), "The authored scene has an independently scaled raster.");
            RenderingServer.SetBootImageWithStretch(image, Colors.Black, Stretch.Ignore, false); using var output = RenderingServer.Service!.Readback(); Check(output.Size == root.Size && output.GetPixel(128, 128).R > .99f, "Loading fills the native client independently of the scaled scene raster and letterbox.");
            using var after = root.GetTexture().GetImage()!; Check(after.GetData().SequenceEqual(before.GetData()), "Scaled viewport contents remain unchanged by loading output."); root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && root.IsDisposed, "Scaled loading scene retires.");
    }
    private static void Transparent()
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(new(1, 0, 0, .5f));
        using var root = new Window { Size = new(256, 192), Borderless = true, Transparent = true };
        root.Ready += _ =>
        {
            root.Transparent = true; Reject<NotSupportedException>(() => root.Transparent = false); Check(root.Transparent, "Rejected live native alpha changes preserve the configured flag.");
            Check(RenderingServer.GetCurrentRenderingMethod() == "compatibility" && DisplayServer.WindowGetFlag(WindowFlag.Transparent), "Transparent main output uses an actually transparent compatibility surface.");
            RenderingServer.RenderLoopEnabled = false; RenderingServer.SetBootImageWithStretch(image, new(0, 0, 1, .2f), Stretch.Ignore, false);
            using var output = RenderingServer.Service!.Readback(); var pixel = output.GetPixel(32, 32); Check(Math.Abs(pixel.R - .5f) < .02 && Math.Abs(pixel.B - .5f) < .02 && Math.Abs(pixel.A - .6f) < .02, "Transparent native loading preserves standard source-alpha composition over the supplied background alpha."); root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && root.IsDisposed, "Transparent loading session retires.");
    }
    private static Rect2 Expected(Vector2i image, Vector2i client, Stretch mode)
    {
        if (image.X == 9) return client.Y switch { 192 => new(123, 93, 9, 5), 193 => new(123, 94, 9, 5), 384 => new(123, 189, 9, 5), 385 => new(123, 190, 9, 5), 256 => new(123, 125, 9, 5), _ => new(123, 126, 9, 5) };
        // Golden pixel rectangles cover both aspect orientations and the square-window Keep rule.
        Rect2[] expected = (image.X, client.Y) switch
        {
            (64, 192) => [new(96, 80, 64, 32), new(-64, 0, 384, 192), new(0, 32, 256, 128), new(-64, 0, 384, 192), new(-64, 0, 384, 192), new(0, 0, 256, 192)],
            (64, 384) => [new(96, 176, 64, 32), new(0, 128, 256, 128), new(0, 128, 256, 128), new(-256, 0, 768, 384), new(-256, 0, 768, 384), new(0, 0, 256, 384)],
            (64, 256) => [new(96, 112, 64, 32), new(0, 64, 256, 128), new(0, 64, 256, 128), new(-128, 0, 512, 256), new(-128, 0, 512, 256), new(0, 0, 256, 256)],
            (16, 192) => [new(120, 80, 16, 32), new(80, 0, 96, 192), new(0, -160, 256, 512), new(80, 0, 96, 192), new(0, -160, 256, 512), new(0, 0, 256, 192)],
            (16, 384) => [new(120, 176, 16, 32), new(0, -64, 256, 512), new(0, -64, 256, 512), new(32, 0, 192, 384), new(0, -64, 256, 512), new(0, 0, 256, 384)],
            (16, 256) => [new(120, 112, 16, 32), new(0, -128, 256, 512), new(0, -128, 256, 512), new(64, 0, 128, 256), new(0, -128, 256, 512), new(0, 0, 256, 256)],
            (64, 193) => [new(96, 80, 64, 32), new(-65, 0, 386, 193), new(0, 32.5f, 256, 128), new(-65, 0, 386, 193), new(-65, 0, 386, 193), new(0, 0, 256, 193)],
            (64, 385) => [new(96, 176, 64, 32), new(0, 128.5f, 256, 128), new(0, 128.5f, 256, 128), new(-257, 0, 770, 385), new(-257, 0, 770, 385), new(0, 0, 256, 385)],
            (64, 257) => [new(96, 112, 64, 32), new(0, 64.5f, 256, 128), new(0, 64.5f, 256, 128), new(-129, 0, 514, 257), new(-129, 0, 514, 257), new(0, 0, 256, 257)],
            (16, 193) => [new(120, 80, 16, 32), new(79.75f, 0, 96.5f, 193), new(0, -159.5f, 256, 512), new(79.75f, 0, 96.5f, 193), new(0, -159.5f, 256, 512), new(0, 0, 256, 193)],
            (16, 385) => [new(120, 176, 16, 32), new(0, -63.5f, 256, 512), new(0, -63.5f, 256, 512), new(31.75f, 0, 192.5f, 385), new(0, -63.5f, 256, 512), new(0, 0, 256, 385)],
            (16, 257) => [new(120, 112, 16, 32), new(0, -127.5f, 256, 512), new(0, -127.5f, 256, 512), new(63.75f, 0, 128.5f, 257), new(0, -127.5f, 256, 512), new(0, 0, 256, 257)],
            _ => throw new InvalidOperationException("The native client is outside this geometry fixture.")
        };
        return expected[(int)mode];
    }

    private static void CheckPixels(Image capture, Image source, Rect2 rectangle)
    {
        var bytes = capture.GetData();
        for (var y = 2; y < capture.Height; y += 7) for (var x = 2; x < capture.Width; x += 7)
            {
                var point = new Vector2(x + .5f, y + .5f); var expected = Colors.Blue;
                if (rectangle.HasPoint(point))
                {
                    var uv = (point - rectangle.Position) / rectangle.Size; expected = source.GetPixel(Math.Clamp((int)(uv.X * source.Width), 0, source.Width - 1), Math.Clamp((int)(uv.Y * source.Height), 0, source.Height - 1));
                }
                var offset = (y * capture.Width + x) * 4;
                if (Math.Abs(bytes[offset] / 255f - expected.R) > .02 || Math.Abs(bytes[offset + 1] / 255f - expected.G) > .02 || Math.Abs(bytes[offset + 2] / 255f - expected.B) > .02) throw new InvalidOperationException($"Boot pixel {x},{y} differs for rectangle {rectangle}.");
            }
    }
    private static void Filtering(Vector2i size)
    {
        using var source = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8); source.SetPixel(0, 0, Colors.Red); source.SetPixel(1, 0, Colors.Blue);
        RenderingServer.SetBootImageWithStretch(source, Colors.Black, Stretch.Ignore, false); using var nearest = RenderingServer.Service!.Readback(); var sharp = nearest.GetPixel(size.X / 2 - 1, size.Y / 2); Check(sharp.R > .99f && sharp.B < .01f, "Nearest loading sampling keeps a sharp texel boundary.");
        RenderingServer.SetBootImageWithStretch(source, Colors.Black, Stretch.Ignore); using var linear = RenderingServer.Service.Readback(); var smooth = linear.GetPixel(size.X / 2 - 1, size.Y / 2); Check(smooth.R is > .4f and < .6f && smooth.B is > .4f and < .6f, "Default loading sampling performs real linear interpolation.");
    }
}
