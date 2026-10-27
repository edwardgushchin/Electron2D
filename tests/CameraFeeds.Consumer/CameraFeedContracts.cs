using Electron2D;
using IOPath = System.IO.Path;

internal static class CameraFeedContracts
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    internal static void Run(string profile)
    {
        var output = Environment.GetEnvironmentVariable("E2D_CAMERA_OUTPUT") ?? IOPath.Combine(IOPath.GetTempPath(), "e2d-camera-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try
        {
            if (profile == "reload")
            {
                using var restored = ResourceLoader.Load<CameraTexture>(IOPath.Combine(output, "camera.e2dres"), ResourceLoader.CacheMode.Ignore);
                Check(restored.CameraFeedID > 0 && restored.WhichFeed == CameraServer.FeedImage.CBCRImage && !restored.CameraIsActive && restored.GetImage() is null, "Fresh-process camera view preserves selectors without opening a source");
                Console.WriteLine("Camera texture fresh-process storage passed."); return;
            }
            Managed(output);
            if (profile == "discovery")
            {
                try
                {
                    CameraServer.MonitoringFeeds = true; CameraServer.Process();
                    foreach (var feed in CameraServer.Feeds())
                    {
                        Check(!feed.FeedIsActive, "Enumeration must not activate capture");
                        Console.WriteLine($"Native camera metadata: {feed.GetName()}, {feed.Formats.Length} formats, active={feed.FeedIsActive}");
                    }
                    Console.WriteLine($"Native camera enumeration: {CameraServer.GetFeedCount()} sources; no frame acquisition requested.");
                }
                finally { CameraServer.MonitoringFeeds = false; }
            }
            else if (profile != "managed") Native(profile, output);
        }
        finally { if (Environment.GetEnvironmentVariable("E2D_CAMERA_OUTPUT") is null) Directory.Delete(output, true); }
    }
    private sealed class Producer : CameraFeed
    {
        internal bool Accept = true, FailStop, Reenter;
        internal int Starts, Stops;
        internal CameraFeedFormat[] Definitions = [new(2, 2, "Authored RGBA", 30, 1)];
        internal CameraFeedOutput Selected;
        protected override bool OnActivateFeed()
        {
            Starts++;
            if (Reenter) { Reject<InvalidOperationException>(() => FeedIsActive = false); Reject<InvalidOperationException>(() => _ = Formats); Reject<InvalidOperationException>(Dispose); }
            return Accept;
        }
        protected override void OnDeactivateFeed() { Stops++; if (FailStop) throw new IOException("fixture cleanup"); }
        protected override CameraFeedFormat[] OnGetFormats() => Definitions;
        protected override bool OnSetFormat(int index, CameraFeedOutput output) { Selected = output; return true; }
    }
    private static void Managed(string output)
    {
        Check(!CameraServer.MonitoringFeeds && CameraServer.GetFeedCount() == 0, "Default camera service is inactive");
        using var feed = new Producer(); using var other = new CameraFeed();
        Check(feed.GetID() > 0 && other.GetID() != feed.GetID() && feed.GetName() == "???" && feed.GetPosition() == CameraFeed.FeedPosition.Unspecified && feed.GetDatatype() == CameraFeed.FeedDataType.RGB && !feed.FeedIsActive, "Feed defaults and process identities");
        Check(feed.FeedTransform == new Transform(1, 0, 0, -1, 0, 1), "Normalized image transform default");
        feed.SetName("Fixture"); feed.SetPosition(CameraFeed.FeedPosition.Back); feed.FeedTransform = Transform.Identity;
        Reject<ArgumentNullException>(() => feed.SetName(null!)); Reject<ArgumentOutOfRangeException>(() => feed.SetPosition((CameraFeed.FeedPosition)9));
        Reject<ArgumentException>(() => feed.FeedTransform = new Transform(float.NaN, 0, 0, 1, 0, 0));
        var formats = feed.Formats; formats[0] = default; Check(feed.Formats[0].Width == 2, "Format snapshots are independent");
        feed.Definitions = [default]; Reject<InvalidOperationException>(() => _ = feed.Formats); feed.Definitions = [new(2, 2, "RGBA", 30, 1)];
        Check(feed.SetFormat(0, CameraFeedOutput.Separate) && feed.Selected == CameraFeedOutput.Separate, "Typed format hook dispatch");
        Reject<ArgumentOutOfRangeException>(() => feed.SetFormat(-1)); Reject<ArgumentOutOfRangeException>(() => feed.SetFormat(0, (CameraFeedOutput)99));
        feed.Accept = false; feed.FeedIsActive = true; Check(!feed.FeedIsActive, "Activation rejection preserves inactivity");
        feed.Accept = feed.Reenter = true; feed.FeedIsActive = true; feed.FeedIsActive = true; Check(feed.Starts == 2, "Successful activation and idempotence");
        Reject<InvalidOperationException>(() => feed.SetFormat(0));
        feed.FailStop = true; Reject<IOException>(() => feed.FeedIsActive = false); Check(!feed.FeedIsActive && feed.Stops == 1, "Failed deactivation still commits inactive"); feed.FailStop = false;
        using var rgb = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); rgb.Fill(Colors.Red);
        feed.SetRGBImage(rgb); Check(feed.GetDatatype() == CameraFeed.FeedDataType.RGB, "Inactive submit is ignored");
        using var view = new CameraTexture { CameraFeedID = feed.GetID() };
        Check(view.GetImage() is null && view.GetSize() == Vector2.Zero && !view.CameraIsActive && !view.HasAlpha, "Unknown feed view is empty");
        var added = 0; var removed = 0;
        Action<int> onAdd = id => { Check(CameraServer.GetFeed(0) == feed && id == feed.GetID(), "Add event sees committed source"); added++; Reject<InvalidOperationException>(() => CameraServer.RemoveFeed(feed)); };
        Action<int> onRemove = id => { Check(CameraServer.GetFeedCount() == 0 && view.GetImage() is null, "Remove event sees cleared view"); removed++; };
        CameraServer.CameraFeedAdded += onAdd; CameraServer.CameraFeedRemoved += onRemove;
        CameraServer.AddFeed(feed); CameraServer.AddFeed(feed); Check(added == 1 && CameraServer.Feeds().Length == 1, "Identity deduplication");
        Reject<InvalidOperationException>(feed.Dispose);
        var order = new List<string>(); var callbackThread = 0; var changes = 0;
        Action format = () => { order.Add("format"); Check(feed.Formats.Length == 1, "Format events can query supported formats"); callbackThread = Environment.CurrentManagedThreadId; Reject<InvalidOperationException>(() => CameraServer.RemoveFeed(feed)); Reject<InvalidOperationException>(feed.Dispose); };
        Action frame = () => order.Add("frame"); feed.FormatChanged += format; feed.FrameChanged += frame;
        view.Changed += _ => changes++;
        try
        {
            view.CameraIsActive = true;
            Task.Run(() => feed.SetRGBImage(rgb)).GetAwaiter().GetResult(); rgb.Fill(Colors.Blue);
            using (var copy = view.GetImage()) Check(copy is not null && copy.GetPixel(0, 0) == Colors.Red, "Producer pixels are copied before deferred notifications");
            Check(order.Count == 0, "Producer thread does not deliver callbacks"); CameraServer.Process();
            Check(order.SequenceEqual(new[] { "format", "frame" }) && callbackThread == Environment.CurrentManagedThreadId && changes == 1, "Owner event order and texture invalidation");
            using (var first = view.GetImage()) { first!.Fill(Colors.Green); using var second = view.GetImage(); Check(second!.GetPixel(0, 0) == Colors.Red, "Exported images are independent"); }
            var rid = view.GetRID(); order.Clear(); feed.SetRGBImage(rgb); feed.SetRGBImage(rgb); CameraServer.Process();
            Check(order.SequenceEqual(new[] { "frame" }) && view.GetRID() == rid, "Same-format frames coalesce and preserve view RID");
            using var y = Image.CreateEmpty(4, 2, false, Image.Format.R8); y.Fill(new(.25f, 0, 0));
            using var cbcr = Image.CreateEmpty(2, 1, false, Image.Format.Rg8); cbcr.Fill(new(.5f, .75f, 0));
            feed.SetYCBCRImages(y, cbcr); CameraServer.Process(); Check(feed.GetDatatype() == CameraFeed.FeedDataType.YCBCRSeparate && view.GetSize() == new Vector2(4, 2), "Separate planes publish as one full frame");
            view.WhichFeed = CameraServer.FeedImage.CBCRImage;
            using (var c = view.GetImage()) Check(c is not null && c.Width == 2 && c.Height == 1 && c.PixelFormat == Image.Format.Rg8 && c.GetData().SequenceEqual(cbcr.GetData()), "Chroma keeps its actual sampling dimensions and bytes");
            using var bad = new Image();
            Reject<ArgumentException>(() => feed.SetYCBCRImages(y, bad)); using (var c = view.GetImage()) Check(c!.Width == 2, "Invalid second plane preserves both old planes");
            Reject<ArgumentNullException>(() => feed.SetRGBImage(null!)); Reject<ArgumentOutOfRangeException>(() => view.WhichFeed = (CameraServer.FeedImage)2);
            using (var copy = (CameraTexture)view.Duplicate()) Check(copy.CameraFeedID == feed.GetID() && copy.WhichFeed == view.WhichFeed && copy.GetSize() == view.GetSize() && copy.GetRID() != rid, "Duplicate borrows source and owns a distinct view identity");
            Check(!view.GetPropertyList().Single(p => p.Name == nameof(CameraTexture.CameraIsActive)).IsStored, "Activation is runtime-only");
            ResourceSaver.Save(view, IOPath.Combine(output, "camera.e2dres"));
            using (var loaded = Task.Run(() => ResourceLoader.Load<CameraTexture>(IOPath.Combine(output, "camera.e2dres"), ResourceLoader.CacheMode.Ignore)).GetAwaiter().GetResult()) Check(loaded.CameraFeedID == feed.GetID() && loaded.WhichFeed == view.WhichFeed, "Worker file construction and view binding");
            view.WhichFeed = CameraServer.FeedImage.RGBAImage;
            using (var hdr = Image.CreateEmpty(2, 2, false, Image.Format.Rgbaf))
            {
                hdr.Fill(new(2, -.5f, .25f, 1)); feed.SetRGBImage(hdr); CameraServer.Process();
                using var retained = view.GetImage(); Check(retained!.PixelFormat == Image.Format.Rgbaf && retained.GetPixel(0, 0).R == 2, "Authored color feeds preserve supported floating-point pixels");
            }
            using (var packed = Image.CreateEmpty(2, 2, false, Image.Format.Rg8))
            {
                feed.SetYCBCRImage(packed); CameraServer.Process(); Check(feed.GetDatatype() == CameraFeed.FeedDataType.YCBCR, "Packed image submission keeps its separate data role");
            }
            Action broken = () => throw new IOException("fixture frame observer");
            feed.FrameChanged += broken;
            using var afterFailure = new CameraTexture { CameraFeedID = feed.GetID() }; var later = 0; afterFailure.Changed += _ => later++;
            feed.SetRGBImage(rgb); Reject<AggregateException>(CameraServer.Process); Check(later == 1, "Failing user observer cannot suppress later views"); feed.FrameChanged -= broken;
            Reject<InvalidOperationException>(() => Task.Run(() => feed.FeedIsActive = false).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(CameraServer.Process).GetAwaiter().GetResult());
        }
        finally
        {
            feed.FormatChanged -= format; feed.FrameChanged -= frame;
            CameraServer.RemoveFeed(feed); CameraServer.CameraFeedAdded -= onAdd; CameraServer.CameraFeedRemoved -= onRemove;
        }
        Check(removed == 1 && view.GetWidth() == 0 && feed.FeedIsActive, "Removal detaches views without owning custom source activity");
        view.CameraIsActive = true; Check(!view.CameraIsActive, "Absent source activation has no target");
        Reject<ArgumentException>(() => CameraServer.RemoveFeed(feed)); feed.FeedIsActive = false;
        feed.Dispose(); Reject<ObjectDisposedException>(() => feed.SetRGBImage(rgb)); Reject<ObjectDisposedException>(() => _ = feed.Formats);
        Console.WriteLine("Camera managed: typed hooks, ownership, copied/coherent planes, coalescing, callback failures, worker input/storage and view retirement passed.");
    }
    private sealed class Picture(CameraTexture texture) : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawTextureRect(texture, new(8, 8, 48, 32), false); }
    }
    private static void Native(string renderer, string output)
    {
        Engine.IsEditorHint = false; Engine.MaxFPS = 120;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var feed = new CameraFeed(); CameraServer.AddFeed(feed);
        using var texture = new CameraTexture { CameraFeedID = feed.GetID() };
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.Red);
        feed.FeedIsActive = true; feed.SetRGBImage(image); CameraServer.Process();
        using var world = new World(PhysicsServer.Backend.CPU); using var window = new Window { Size = new(80, 60), World = world, Title = "Electron2D camera pixel verification" };
        var picture = new Picture(texture); window.AddChild(picture);
        var frame = 0; var quietDraws = 0; var complete = false; Exception? failed = null; long before = 0;
        window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Drawn; };
        try { var exit = Engine.Run(window); if (failed is not null) throw failed; Check(exit == 0 && complete, "Camera render sequence completed"); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Drawn; CameraServer.RemoveFeed(feed); }
        void Drawn()
        {
            try
            {
                frame++;
                if (frame == 1 || frame == 3)
                {
                    using var result = window.GetTexture().GetImage()!;
                    var expected = frame == 1 ? Colors.Red : Colors.Blue;
                    var pixel = result.GetPixel(20, 20); Check(Math.Abs(pixel.R - expected.R) < .02f && Math.Abs(pixel.B - expected.B) < .02f, "Camera pixels reach " + renderer);
                    result.SavePNG(IOPath.Combine(output, renderer + "-" + frame + ".png"));
                }
                if (frame == 1) { image.Fill(Colors.Blue); feed.SetRGBImage(image); }
                if (frame == 4) quietDraws = picture.Draws;
                if (frame == 31) Check(picture.Draws == quietDraws, "Settled source frames retain canvas commands");
                if (frame == 32) before = GC.GetAllocatedBytesForCurrentThread();
                if (frame == 96) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; Check(bytes == 0, "Prepared camera owner frames allocated " + bytes); complete = true; window.Tree!.Quit(); }
            }
            catch (Exception error) { failed = error; window.Tree!.Quit(1); }
        }
        Console.WriteLine($"Camera {renderer}: red/blue actual pixels, stable texture/canvas and 64 prepared frames with zero managed owner bytes passed.");
    }
}
