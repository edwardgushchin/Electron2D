using Electron2D;
using IOPath = System.IO.Path;

internal static class WindowContentScaleNativeTests
{
    internal static void Run()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_CONTENT_BACKEND") ?? "compatibility";
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        try { ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); Pixels(backend); EdgePixels(backend); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); }
        Console.WriteLine($"Window content pixels, GUI input, embedded target, raster policy and warmed frames passed: {backend}.");
    }
    private static void Pixels(string backend)
    {
        var window = new Window { Size = new(240, 160), GUIEmbedSubwindows = true, ContentScaleSize = new(80, 80), ContentScaleAspect = ContentScaleAspect.Keep, ContentScaleMode = ContentScaleMode.Viewport };
        var box = new Box { MouseFilter = MouseFilter.Stop }; box.SetBegin(new(10, 10)); box.SetEnd(new(20, 20)); window.AddChild(box);
        var child = new Window { Size = new(40, 40), Position = new(25, 30), Borderless = true, ContentScaleSize = new(20, 20), ContentScaleMode = ContentScaleMode.Viewport };
        var childBox = new Box { MouseFilter = MouseFilter.Stop }; childBox.SetBegin(new(2, 2)); childBox.SetEnd(new(6, 6)); child.AddChild(childBox); window.AddChild(child);
        var text = new Text(); window.AddChild(text);
        var frame = 0; long allocated = 0, start = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested native backend runs without fallback.");
            RenderingServer.SetDefaultClearColor(Colors.Blue);
            var renderer = RenderingServer.Service!;
            window.Tree!.ProcessFrameStarted += _ => { if (frame >= 40) start = GC.GetAllocatedBytesForCurrentThread(); if (frame is >= 40 and < 104) box.Position = new(10 + (frame & 1), 10); };
            RenderingServer.FramePostDraw += () =>
            {
                frame++;
                if (frame > 40) allocated += GC.GetAllocatedBytesForCurrentThread() - start;
                if (frame <= 4)
                {
                    using var image = renderer.Readback(); using var viewport = window.GetTexture().GetImage();
                    Check(image.Size == new Vector2i(240, 160), "Presentation stays at the client pixel size.");
                    var expected = frame == 1 ? 80 : frame == 2 ? 160 : frame == 3 ? 40 : 160;
                    Check(viewport is not null && viewport.Size == new Vector2i(expected, expected), "Completed viewport image follows its actual raster mode/factor.");
                    var x = frame == 3 ? 100 : 70; var y = frame == 3 ? 60 : 30;
                    Red(image, x, y); Check(image.GetPixel(10, 10).R < .01f && image.GetPixel(10, 10).B < .01f, "Presentation margins stay black.");
                    using var input = new InputEventMouseMotion { Position = new(x, y), Relative = new(2, 2) };
                    using var local = (InputEventMouseMotion)window.MakeViewportInputLocal(input);
                    Check(local.Position == new Vector2(15, 15), "Native client coordinates remove the completed content transform.");
                    if (frame <= 2)
                    {
                        using var childImage = child.GetTexture().GetImage(); Check(childImage is not null && childImage.Size == new Vector2i(frame == 1 ? 20 : 40, frame == 1 ? 20 : 40), "Embedded raster density follows the parent canvas mode.");
                        Red(image, 102, 72);
                    }
                    if (frame != 3)
                        for (var py = 132; py < 158; py++) for (var px = 48; px < 68; px++)
                                Check(image.GetPixel(px, py) == image.GetPixel(px + 130, py), "Automatic glyph pixels equal explicit raster oversampling.");
                    var output = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_CONTENT_OUTPUT") ?? IOPath.GetTempPath(); Directory.CreateDirectory(output);
                    File.WriteAllBytes(IOPath.Combine(output, $"content-{backend}-{frame}.png"), image.SavePNGToBuffer());
                    if (frame == 1) { text.ReferenceOversampling = 2; window.ContentScaleMode = ContentScaleMode.CanvasItems; }
                    else if (frame == 2) { child.Hide(); window.ContentScaleMode = ContentScaleMode.Viewport; window.ContentScaleFactor = 2; }
                    else if (frame == 3) { window.ContentScaleMode = ContentScaleMode.CanvasItems; window.ContentScaleFactor = 1; }
                }
                if (frame == 168) { Check(allocated == 0, $"64 active and64 idle content frames allocated {allocated} managed bytes."); window.Tree!.Quit(); }
                if (frame > 170) throw new InvalidOperationException("Content test did not finish.");
            };
        };
        Engine.Run(window); Check(window.IsDisposed && RenderingServer.Service is null && DisplayServer.Service is null, "Native teardown releases all content and presentation targets.");
    }
    private static void EdgePixels(string backend)
    {
        var window = new Window { Size = new(32, 24), Borderless = true, ContentScaleSize = new(80, 80), ContentScaleMode = ContentScaleMode.Viewport, ContentScaleAspect = ContentScaleAspect.Keep, ContentScaleStretch = ContentScaleStretch.Integer };
        var box = new Box(); box.SetBegin(Vector2.Zero); box.SetEnd(new(80, 80)); window.AddChild(box);
        var frame = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Edge profile uses the requested backend.");
            var renderer = RenderingServer.Service!;
            RenderingServer.FramePostDraw += () =>
            {
                using var image = renderer.Readback(); frame++;
                Check(image.Size == window.Size, $"Small-window presentation follows the observed client: image={image.Size}, client={window.Size}.");
                if (frame == 1)
                {
                    var origin = window.ContentLayout.ScreenRect.Position;
                    Red(image, origin.X + 5, origin.Y + 5); Red(image, origin.X + 10, Math.Min(image.Height - 1, origin.Y + 23));
                    window.ContentScaleStretch = ContentScaleStretch.Fractional; window.ContentScaleFactor = 1000;
                }
                else
                {
                    Check(window.GetTexture().GetSize() == Vector2.Zero && window.GetTexture().GetImage() is null, "Zero raster owns no visible image.");
                    if (backend == "gpu") Check(!RenderingServer.TextureGetRDTexture(window.GetTexture().GetRID()).IsValid(), "Zero raster exposes no native borrowed view.");
                    Check(image.GetPixel(5, 5) == Colors.Black, "Zero raster clears native presentation.");
                    var output = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_CONTENT_OUTPUT") ?? IOPath.GetTempPath(); Directory.CreateDirectory(output);
                    File.WriteAllBytes(IOPath.Combine(output, $"content-{backend}-zero.png"), image.SavePNGToBuffer());
                    window.Tree!.Quit();
                }
            };
        };
        Engine.Run(window);
    }
    private sealed class Box : Control { protected override void OnDraw() { base.OnDraw(); DrawRect(new(Vector2.Zero, Size), Colors.Red); } }
    private sealed class Text : Entity
    {
        internal float ReferenceOversampling = 1;
        protected override void OnDraw()
        {
            base.OnDraw(); var font = GetWindow()!.GetThemeFont("font", "Label")!;
            DrawChar(font, new(5, 70), "A", fontSize: 12);
            DrawChar(font, new(70, 70), "A", fontSize: 12, oversampling: ReferenceOversampling);
        }
    }
    private static void Red(Image image, int x, int y) { var p = image.GetPixel(x, y); Check(p.R > .95f && p.G < .05f && p.B < .05f, $"Expected red at{x},{y}, got {p}."); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
