using Electron2D;

internal static partial class RenderingRuntimeTests
{
    internal static void RunCanvasScalePreview(string backend)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            Reject<InvalidOperationException>(() => RenderingServer.CanvasSetDisableScale(true));
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            VerifyCanvasScalePreview(backend); VerifyCanvasScaleReset();
            Reject<InvalidOperationException>(() => RenderingServer.CanvasSetDisableScale(false));
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }

    private static void VerifyCanvasScalePreview(string backend)
    {
        // Decorations can raise the native minimum width and change the tested center pivot.
        var window = new Window { Borderless = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") != "dummy", Size = new(128, 96), CanvasTransform = new(0, new(4, 2)), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var layer = new CanvasLayer { Offset = new(20, 20), Scale = new(2, 1), FollowViewportEnabled = true, FollowViewportScale = 2 };
        var mark = new CanvasNode { Position = new(4, 4), Scale = new(1, 2), DrawAction = node => { Reject<InvalidOperationException>(() => RenderingServer.CanvasSetDisableScale(true)); node.DrawRect(new(0, 0, 4, 3), Colors.Red); } };
        layer.AddChild(mark); window.AddChild(layer);
        window.AddChild(new CanvasNode { Position = new(100, 70), DrawAction = node => node.DrawRect(new(0, 0, 4, 4), Colors.Blue) });
        var fixedLayer = new CanvasLayer(); window.AddChild(fixedLayer);
        fixedLayer.AddChild(new ColorRect { Position = new(80, 8), Scale = new(2, 3), Size = new(3, 2), Color = Colors.Yellow, MouseFilter = MouseFilter.Ignore });
        var child = new SubViewport { Size = new(64, 64), CanvasTransform = new(0, new(2, 3)), RenderTargetUpdateMode = ViewportUpdateMode.Always, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var childLayer = new CanvasLayer { Offset = new(12, 14), Scale = new(1, 2), FollowViewportEnabled = true, FollowViewportScale = 2 };
        childLayer.AddChild(new CanvasNode { Position = new(8, 0), DrawAction = node => node.DrawRect(new(0, 0, 4, 3), Colors.Green) }); child.AddChild(childLayer); window.AddChild(child);
        var childTexture = child.GetTexture();
        window.AddChild(new CanvasNode { DrawAction = node => node.DrawTextureRect(childTexture, new(0, 0, 1, 1), false) });
        var warmup = 0; var phase = 0; var retained = 0; var cycles = 0; long activeStart = 0, idleStart = 0, activeBytes = -1, idleBytes = -1;
        RID rawView = default, rawTexture = default; Transform logical = default;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.CanvasSetDisableScale(true)).GetAwaiter().GetResult());
            rawView = RenderingServer.ViewportCreate(); RenderingServer.ViewportSetSize(rawView, 64, 64); RenderingServer.ViewportSetActive(rawView, true); RenderingServer.ViewportSetUpdateMode(rawView, ViewportUpdateMode.Always);
            RenderingServer.ViewportAttachCanvas(rawView, layer.GetCanvas()); RenderingServer.ViewportSetCanvasTransform(rawView, layer.GetCanvas(), new(0, new(5, 6))); rawTexture = RenderingServer.ViewportGetTexture(rawView);
            logical = layer.GetFinalTransform(); RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            if (warmup++ < 16) { retained = mark.Draws; return; }
            if (phase < 14)
            {
                using var pixels = RenderingServer.Service!.Readback(); using var nested = childTexture.GetImage()!; using var raw = RenderingServer.Texture2DGet(rawTexture)!;
                try
                {
                    Pixel(pixels, phase < 10 ? 105 : 102, phase < 10 ? 73 : 72, Colors.Blue); Pixel(pixels, 81, 9, Colors.Yellow); Pixel(pixels, 85, 13, Colors.Yellow);
                    Pixel(raw, 10, 11, Colors.Red); Pixel(raw, 30, 20, Colors.Black);
                    switch (phase)
                    {
                        case 0:
                            Check(window.Size == new Vector2i(128, 96), "The fixture retains its requested native pivot size.");
                            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } baseline) { Directory.CreateDirectory(baseline); pixels.SavePNG(System.IO.Path.Combine(baseline, "canvas-scale-normal-" + backend + ".png")); }
                            Pixel(pixels, 2, 6, Colors.Red); Pixel(pixels, 34, 28, Colors.Black); Pixel(nested, 14, 4, Colors.Green); Pixel(nested, 24, 19, Colors.Black);
                            RenderingServer.CanvasSetDisableScale(true); break;
                        case 1:
                            Pixel(pixels, 2, 6, Colors.Black); Pixel(pixels, 34, 28, Colors.Red); Pixel(pixels, 38, 30, Colors.Red); Pixel(nested, 14, 4, Colors.Black); Pixel(nested, 24, 19, Colors.Green);
                            Check(layer.GetFinalTransform() == logical && layer.Transform.Scale == new Vector2(2, 1) && mark.Scale == new Vector2(1, 2), "Preview preserves logical matrices and authored own scales.");
                            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } path) { Directory.CreateDirectory(path); pixels.SavePNG(System.IO.Path.Combine(path, "canvas-scale-preview-" + backend + ".png")); }
                            layer.FollowViewportScale = childLayer.FollowViewportScale = 0; RenderingServer.CanvasSetDisableScale(false); break;
                        case 2:
                            Pixel(pixels, 34, 28, Colors.Black); Pixel(nested, 24, 19, Colors.Black); RenderingServer.CanvasSetDisableScale(true); break;
                        case 3:
                            Pixel(pixels, 34, 28, Colors.Red); Pixel(nested, 24, 19, Colors.Green);
                            layer.FollowViewportScale = childLayer.FollowViewportScale = -1; RenderingServer.CanvasSetDisableScale(false); break;
                        case 4:
                            Pixel(pixels, 90, 66, Colors.Red); Pixel(nested, 40, 43, Colors.Green); RenderingServer.CanvasSetDisableScale(true); break;
                        case 5:
                            Pixel(pixels, 34, 28, Colors.Red); Pixel(nested, 24, 19, Colors.Green);
                            layer.FollowViewportScale = childLayer.FollowViewportScale = .5f; RenderingServer.CanvasSetDisableScale(false); break;
                        case 6:
                            Pixel(pixels, 49, 38, Colors.Red); Pixel(nested, 27, 26, Colors.Green); RenderingServer.CanvasSetDisableScale(true); break;
                        case 7:
                            Pixel(pixels, 34, 28, Colors.Red); Pixel(nested, 24, 19, Colors.Green);
                            layer.FollowViewportEnabled = childLayer.FollowViewportEnabled = false; RenderingServer.CanvasSetDisableScale(false); break;
                        case 8:
                            Pixel(pixels, 30, 26, Colors.Red); Pixel(nested, 22, 16, Colors.Green); RenderingServer.CanvasSetDisableScale(true); break;
                        case 9:
                            Pixel(pixels, 30, 26, Colors.Red); Pixel(nested, 22, 16, Colors.Green);
                            layer.FollowViewportEnabled = childLayer.FollowViewportEnabled = true; layer.FollowViewportScale = childLayer.FollowViewportScale = 2;
                            window.SnapTransformsToPixel = child.SnapTransformsToPixel = true; window.CanvasTransform = child.CanvasTransform = new(0, new(.75f, .75f)); break;
                        case 10:
                            Check(window.GetCanvasRenderTransform(layer).Origin == new Vector2(20.5f, 20.5f) && child.GetCanvasRenderTransform(childLayer).Origin == new Vector2(12.5f, 14.5f), "Preview still rounds parent translation in authored follow-scale units.");
                            Pixel(pixels, 30, 26, Colors.Red); Pixel(nested, 22, 16, Colors.Green); RenderingServer.CanvasSetDisableScale(false); break;
                        case 11:
                            Check(window.GetCanvasRenderTransform(layer).Origin == new Vector2(-23, -7), "Restored follow scaling retains the viewport-center pivot.");
                            Pixel(pixels, 0, 3, Colors.Red); Pixel(nested, 11, 1, Colors.Green);
                            RenderingServer.ViewportSetCanvasTransform(window.GetViewportRID(), layer.GetCanvas(), new(0, new(50, 10))); RenderingServer.CanvasSetDisableScale(true); break;
                        case 12:
                            Pixel(pixels, 55, 15, Colors.Red); RenderingServer.CanvasSetDisableScale(false); break;
                        case 13:
                            Pixel(pixels, 55, 15, Colors.Red); layer.Transform = layer.Transform; RenderingServer.CanvasSetDisableScale(true); break;
                    }
                }
                catch (Exception error)
                {
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } path) { Directory.CreateDirectory(path); pixels.SavePNG(System.IO.Path.Combine(path, "failed-" + backend + "-" + phase + ".png")); }
                    Console.WriteLine($"Scale fixture size={window.Size} visible={window.GetVisibleRect()} render={window.GetCanvasRenderTransform(layer)} logical={layer.GetFinalTransform()} driver={RenderingServer.GetCurrentRenderingDriverName()}");
                    throw new InvalidOperationException($"Canvas scale preview {backend}, phase {phase}.", error);
                }
                phase++; return;
            }
            if (cycles == 64) activeStart = GC.GetAllocatedBytesForCurrentThread();
            if (cycles == 128) { activeBytes = GC.GetAllocatedBytesForCurrentThread() - activeStart; idleStart = GC.GetAllocatedBytesForCurrentThread(); }
            if (cycles == 192)
            {
                idleBytes = GC.GetAllocatedBytesForCurrentThread() - idleStart;
                Check(activeBytes == 0 && idleBytes == 0 && mark.Draws == retained, "64 active and 64 idle prepared frames allocate zero owner bytes and reuse retained commands.");
                RenderingServer.CanvasSetDisableScale(true); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit(); return;
            }
            if (cycles < 128) RenderingServer.CanvasSetDisableScale((cycles & 1) == 0);
            else if (cycles == 128) RenderingServer.CanvasSetDisableScale(true);
            cycles++;
        }
        Check(Engine.Run(window) == 0, "Scale preview host exits successfully."); Released(window);
        Check(phase == 14 && activeBytes == 0 && idleBytes == 0, "All canvas scale cases and prepared intervals completed.");
        Console.WriteLine($"Canvas scale preview {backend}: 14 pixel phases, root/nested/explicit RID canvases, guards, 64 active/64 idle frames {activeBytes}/{idleBytes} B passed.");
    }

    private static void VerifyCanvasScaleReset()
    {
        var window = new Window { Borderless = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") != "dummy", Size = new(64, 64) }; var layer = new CanvasLayer { Offset = new(24, 24), FollowViewportEnabled = true, FollowViewportScale = 2 };
        layer.AddChild(new CanvasNode { DrawAction = node => node.DrawRect(new(0, 0, 4, 4), Colors.Red) }); window.AddChild(layer);
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Capture;
            void Capture() { using var image = RenderingServer.Service!.Readback(); Pixel(image, 17, 17, Colors.Red); Pixel(image, 25, 25, Colors.Black); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit(); }
        };
        Check(Engine.Run(window) == 0, "A fresh renderer restores enabled follow scaling."); Released(window);
    }
}
