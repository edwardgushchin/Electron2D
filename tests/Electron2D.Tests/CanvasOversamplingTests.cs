using Electron2D;

internal static class CanvasOversamplingTests
{
    private sealed class ExitProbe : Entity
    {
        internal Action? Exited;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationExitCanvas) Exited?.Invoke();
        }
    }
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var dpi = DPITexture.CreateFromString("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="8"><rect width="8" height="8" fill="red"/><rect x="8" width="8" height="8" fill="blue"/></svg>""");
        using var font = new FontFile { Data = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/IBMPlexSans-Regular.ttf")), AllowSystemFallback = false };
        Engine.MaxFPS = 120;
        using var explicitFont = new FontFile { Data = font.Data, AllowSystemFallback = false };
        using var authorFont = new FontFile { Data = font.Data, AllowSystemFallback = false };
        using (var page = Image.CreateEmpty(256, 256, false, Image.Format.Rgba8))
        {
            page.SetPixel(0, 0, Colors.Red); authorFont.SetTextureImage(0, new(16, 0), 0, page);
            authorFont.SetTextureOffsets(0, new(16, 0), 0, [0, 100, 256, 156]);
        }
        using var msdf = new FontFile { Data = font.Data, AllowSystemFallback = false, MultichannelSignedDistanceField = true };
        using var window = new Window { Size = new(480, 256), OversamplingOverride = 1.25f, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var parent = new Entity { Scale = new(2, 2), OversamplingWithScale = OversamplingWithScale.Enabled };
        var child = new CollisionShape { Scale = new(1.5f, 1.5f), Position = new(8, 8) };
        var twin = new Entity { Scale = new(1.5f, 1.5f), Position = new(90, 8) };
        var bridge = new Node(); var boundary = new Entity { Scale = new(3, 3) };
        window.AddChild(parent); parent.AddChild(child); parent.AddChild(twin); parent.AddChild(bridge); bridge.AddChild(boundary);
        child.Owner = parent; twin.Owner = parent; bridge.Owner = parent; boundary.Owner = parent;
        var layer = new CanvasLayer { Transform = Transform.Identity.Scaled(new(9, 9)) };
        var ui = new Control { Scale = new(7, 7), Position = new(1000, 1000), OversamplingWithScale = OversamplingWithScale.Enabled };
        window.AddChild(layer); layer.AddChild(ui);
        using var exitProbe = new ExitProbe { Scale = new(5, 5), Position = new(10000, 10000), OversamplingWithScale = OversamplingWithScale.Enabled };
        window.AddChild(exitProbe); RID exitRaster = default;
        exitProbe.Draw += item => { exitRaster = dpi.GetScaledRID(); item.DrawTexture(dpi, Vector2.Zero); };
        RID uiRaster = default;
        ui.Draw += item => { uiRaster = dpi.GetScaledRID(); item.DrawTexture(dpi, Vector2.Zero); };
        RID current = default, shared = default, first = default, five = default, broken = default;
        var allocate = Environment.GetEnvironmentVariable("ELECTRON2D_OVERSAMPLING_ALLOCATE") == "1";
        var draws = 0; var phase = 0; var samples = 0; long begin = 0, allocated = -1;
        var source = font.GetRID();
        child.Draw += item => { draws++; current = dpi.GetScaledRID(); item.DrawTexture(dpi, Vector2.Zero); item.DrawString(font, new(.25f, 32.25f), "Scale", fontSize: 16); item.DrawChar(explicitFont, new(30, 12), "E", 12, oversampling: 2); item.DrawChar(authorFont, new(55, 12), "A", 16); if (backend == "gpu") item.DrawChar(msdf, new(40, 12), "M", 12); };
        twin.Draw += item => { shared = dpi.GetScaledRID(); item.DrawTexture(dpi, Vector2.Zero); item.DrawChar(font, new(.25f, 32.25f), "S", 16); };
        boundary.Draw += _ => broken = dpi.GetScaledRID();
        using var target = window.GetTexture();
        Check(child.GetPropertyList().Single(p => p.Name == nameof(CanvasItem.OversamplingWithScale)).IsStored, "stored descriptor");
        void Check(bool ok, string label) => Assert(ok, "Item oversampling: " + label);
        void Raster(RID rid, int width) { using var image = RenderingServer.Texture2DGet(rid)!; Check(image.Width == width && dpi.GetWidth() == 16, "actual raster width/logical size " + width); }
        void Retired(RID rid)
        {
            try { using var image = RenderingServer.Texture2DGet(rid); if (image is null) return; }
            catch (ArgumentException) { return; }
            throw new InvalidOperationException("Retired item raster is still live.");
        }
        bool HasScale(float factor) => TextServerManager.GetPrimaryInterface().FontGetSizeCacheInfo(source).Any(s => s.RasterScales.Contains(factor));
        void Capture()
        {
            phase++;
            switch (phase)
            {
                case 1:
                    Check(TextServerManager.GetPrimaryInterface().FontGetSizeCacheInfo(explicitFont.GetRID()).SelectMany(s => s.RasterScales).Distinct().SequenceEqual([2f]), "explicit font factor overrides item scale");
                    if (backend == "gpu") Check(TextServerManager.GetPrimaryInterface().FontGetSizeCacheInfo(msdf.GetRID()).SelectMany(s => s.RasterScales).Distinct().SequenceEqual([1f]), "MSDF ignores item scale");
                    Check(authorFont.GetGlyphUVRect(0, new(16, 0), authorFont.GetGlyphIndex(16, 'A', 0)).Position.Y >= 100, "author shelf receives the scaled native glyph");
                    Raster(uiRaster, 140);
                    first = current; Check(current == shared && current != broken, "direct inheritance and neutral boundary"); Raster(first, 60); Raster(broken, 20); Check(HasScale(3.75f), "shaped and direct font consumers use global scale");
                    using (var image = target.GetImage()!) { Assert(image.GetPixel(28, 28).IsEqualApprox(Colors.Red), "actual SVG pixel"); var lit = 0; for (var y = 72; y < 120; y++) for (var x = 16; x < 150; x++) if (image.GetPixel(x, y).R > .1f) lit++; Check(lit > 50, "actual scaled glyph pixels"); }
                    child.Scale = new(2, 2); break;
                case 2:
                    five = current; Raster(five, 80); Raster(first, 60); Check(HasScale(5) && HasScale(3.75f), "concurrent font levels"); twin.OversamplingWithScale = OversamplingWithScale.Disabled; break;
                case 3:
                    Check(!TextServerManager.GetPrimaryInterface().FontGetSizeCacheInfo(authorFont.GetRID()).SelectMany(s => s.RasterScales).Contains(3.75f), "author-page old raster record retires");
                    using (var page = authorFont.GetTextureImage(0, new(16, 0), 0)!) Check(page.GetPixel(0, 0).IsEqualApprox(Colors.Red), "author pixels survive level retirement");
                    Retired(first); Check(!HasScale(3.75f) && HasScale(5), "last-owner font retirement"); child.TopLevel = true; break;
                case 4:
                    Raster(current, 20); Retired(five); Check(!HasScale(5), "TopLevel retires inherited level"); child.OversamplingWithScale = OversamplingWithScale.Enabled; child.Scale = new(3, 2); break;
                case 5:
                    Raster(current, 60); Check(current != first, "new level generation"); child.Scale = new(2, -4); break;
                case 6:
                    Raster(current, 40); child.TopLevel = false; child.OversamplingWithScale = OversamplingWithScale.ParentNode; parent.OversamplingWithScale = OversamplingWithScale.Disabled; break;
                case 7:
                    Raster(current, 20); parent.OversamplingWithScale = OversamplingWithScale.Enabled; child.Scale = new(1.5f, 1.5f); break;
                case 8:
                    Raster(current, 60); first = current; child.Hide(); child.Scale = new(4, 4); break;
                case 9:
                    Raster(first, 60); child.Show(); break;
                case 10:
                    Raster(current, 160); Retired(first); child.Scale = new(1.5f, 1.5f); break;
                case 11:
                    Raster(current, 60); window.OversamplingOverride = 2; break;
                case 12:
                    Raster(current, 96); Check(HasScale(6), "viewport factor multiplies item factor");
                    using (var packed = new PackedScene()) { packed.Pack(parent); using var copy = (Entity)packed.Instantiate(); Check(copy.OversamplingWithScale == OversamplingWithScale.Enabled && ((Entity)copy.GetChild(0)).OversamplingWithScale == OversamplingWithScale.ParentNode, "packed policy"); }
                    using (var packed = new PackedScene())
                    {
                        packed.Pack(parent);
                        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-oversampling-" + Guid.NewGuid().ToString("N") + ".e2dscene");
                        try
                        {
                            ResourceSaver.Save(packed, path);
                            using var loaded = ResourceLoader.Load<PackedScene>(path)!;
                            using var restored = (Entity)loaded.Instantiate();
                            Check(restored.OversamplingWithScale == OversamplingWithScale.Enabled && ((Entity)restored.GetChild(0)).OversamplingWithScale == OversamplingWithScale.ParentNode, "file archive policy");
                        }
                        finally { File.Delete(path); }
                    }
                    parent.RemoveChild(child); window.AddChild(child); break;
                case 13:
                    Raster(current, 32); child.OversamplingWithScale = OversamplingWithScale.Enabled; break;
                case 14:
                    Raster(current, 48); first = current; window.RemoveChild(child); parent.AddChild(child); break;
                case 15:
                    Raster(current, 96); Retired(first); child.OversamplingWithScale = OversamplingWithScale.ParentNode; break;
            }
            if (phase == 16)
            {
                var observedExit = false;
                exitProbe.Exited = () => { Raster(exitRaster, 160); observedExit = true; };
                window.RemoveChild(exitProbe); Check(observedExit, "exit callback runs before raster retirement");
                Retired(exitRaster); exitProbe.Exited = null;
            }
            if (phase < 32) return;
            if (phase == 32) { begin = GC.GetAllocatedBytesForCurrentThread(); return; }
            if (allocate) GC.KeepAlive(new byte[128]);
            if (++samples < 64) return;
            allocated = GC.GetAllocatedBytesForCurrentThread() - begin;
            RenderingServer.FramePostDraw -= Capture;
            Raster(current, 96); Check(allocated == 0, "64 warmed full frames bytes=" + allocated);
            Console.WriteLine($"Canvas oversampling {backend}: inheritance/boundaries/signed scale/lifecycle, actual SVG/font pixels, cache retirement, packed policy, {draws} recordings, {samples} warm frames, {allocated} B.");
            window.Tree!.Quit();
        }
        Reject<ArgumentOutOfRangeException>(() => child.OversamplingWithScale = OversamplingWithScale.Max);
        Reject<ArgumentOutOfRangeException>(() => child.OversamplingWithScale = (OversamplingWithScale)(-1));
        window.Ready += _ => { Task.Run(() => Reject<InvalidOperationException>(() => { var value = child.OversamplingWithScale; GC.KeepAlive(value); })).GetAwaiter().GetResult(); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Capture; };
        try { Engine.Run(window); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        Check(samples == 64 && allocated == 0, "completed strict warm gate");
        Check(TextServerManager.GetPrimaryInterface().FontGetSizeCacheInfo(explicitFont.GetRID()).SelectMany(s => s.RasterScales).Contains(2f), "explicit raster outlives viewport scale owners");
        child.Dispose();
        Reject<ObjectDisposedException>(() => child.OversamplingWithScale = OversamplingWithScale.Enabled);
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
