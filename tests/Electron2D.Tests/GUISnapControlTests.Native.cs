using Electron2D;
using Path = System.IO.Path;

internal static partial class GUISnapControlTests
{
    private sealed class Paint : Control
    {
        internal int Draws;
        internal Color Ink = Colors.Red;
        protected override void OnDraw() { Draws++; DrawRect(new(Vector2.Zero, Size), Ink); }
    }
    private sealed class Mark : Entity
    {
        internal Color Ink;
        internal Vector2 Extent = new(1, 1);
        protected override void OnDraw() => DrawRect(new(Vector2.Zero, Extent), Ink);
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var scale = new Transform(0, new Vector2(4, 4), 0, Vector2.Zero);
        var root = new Window { Size = new(96, 80), CanvasTransform = scale };
        var subject = new Paint { Position = new(2.25f, 2.25f), Size = new(2, 2) }; root.AddChild(subject);
        subject.AddChild(new Mark { Position = new(3.25f, .25f), Ink = Colors.Blue });
        root.AddChild(new Mark { Position = new(2.25f, 5.25f), Extent = new(2, 2), Ink = Colors.Green });
        var offset = new Paint { Position = new(2.5f, 10.5f), Size = Vector2.One, Ink = Colors.Magenta, OffsetTransformEnabled = true, OffsetTransformPosition = new(.25f, .25f) }; root.AddChild(offset);
        var layer = new CanvasLayer { Layer = 1, Transform = new(0, new Vector2(4, 4), 0, new Vector2(48.25f, .25f)) }; root.AddChild(layer);
        layer.AddChild(new Paint { Position = new(.5f, 2.5f), Size = new(1.5f, .75f), Ink = Colors.Yellow });
        var nested = new SubViewport { Size = new(32, 24), RenderTargetUpdateMode = ViewportUpdateMode.Always, GUISnapControlsToPixels = false, CanvasTransform = scale }; root.AddChild(nested);
        nested.AddChild(new Paint { Position = new(2.25f, 2.25f), Size = new(2, 2), Ink = Colors.Cyan });
        var phase = 0; var frames = 0; var draws = 0; long before = 0, allocated = 0; byte[]? rotated = null;
        void Pixel(Image image, int x, int y, Color expected, string message)
        { var actual = image.GetPixel(x, y); Check(MathF.Abs(actual.R - expected.R) < .02f && MathF.Abs(actual.G - expected.G) < .02f && MathF.Abs(actual.B - expected.B) < .02f, backend + " phase " + phase + " " + message + " at " + x + "," + y + " actual " + actual); }
        byte[] Region(Image image)
        {
            var data = new byte[12 * 12 * 3]; var at = 0;
            for (var y = 6; y < 18; y++) for (var x = 6; x < 18; x++) { var pixel = image.GetPixel(x, y); data[at++] = (byte)Mathf.Round(pixel.R * 255); data[at++] = (byte)Mathf.Round(pixel.G * 255); data[at++] = (byte)Mathf.Round(pixel.B * 255); }
            return data;
        }
        void BeforeFrame(SceneTree _)
        {
            if (phase < 6) return;
            if (frames >= 32) before = GC.GetAllocatedBytesForCurrentThread();
            if (phase == 6) root.GUISnapControlsToPixels = (frames & 1) == 0;
        }
        void Captured()
        {
            if (phase >= 6)
            {
                if (frames >= 32) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames == 32) draws = subject.Draws;
                if (frames < 96) return;
                Check(allocated == 0 && subject.Draws == draws, "Prepared GUI " + (phase == 6 ? "active" : "idle") + " native frames allocate " + allocated + " owner bytes and retain recorded commands.");
                Console.WriteLine(backend + " GUI snap: 64 " + (phase == 6 ? "active policy" : "idle retained") + " full native frames, 0 managed owner bytes; no extra OnDraw.");
                if (phase == 6) { phase++; frames = 0; allocated = 0; } else root.Tree!.Quit(); return;
            }
            if (++frames < 3) return;
            using var image = root.GetTexture().GetImage()!; using var subImage = nested.GetTexture().GetImage()!;
            var directory = Environment.GetEnvironmentVariable("ELECTRON2D_GUI_SNAP_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); image.SavePNG(Path.Combine(directory, backend + "-" + phase + ".png")); subImage.SavePNG(Path.Combine(directory, backend + "-nested-" + phase + ".png")); }
            switch (phase)
            {
                case 0:
                    Pixel(image, 8, 12, Colors.Red, "default GUI translation rounds before canvas scaling"); Pixel(image, 16, 12, Colors.Black, "snapped right edge"); Pixel(image, 21, 11, Colors.Blue, "spatial child inherits the visual control transform");
                    Pixel(image, 9, 24, Colors.Green, "unrelated Entity remains fractional"); Pixel(image, 8, 24, Colors.Black, "Entity is not GUI-snapped");
                    Pixel(image, 14, 48, Colors.Magenta, "visual-only offset follows GUI snapping"); Pixel(image, 12, 46, Colors.Black, "visual-only offset is not rounded afterwards");
                    Pixel(image, 53, 13, Colors.Yellow, "CanvasLayer consumes locally snapped controls"); Pixel(subImage, 8, 12, Colors.Black, "nested viewport keeps its independent false flag"); Pixel(subImage, 16, 12, Colors.Cyan, "nested fractional right edge");
                    draws = subject.Draws; subject.YSortEnabled = true; root.GUISnapControlsToPixels = false; nested.GUISnapControlsToPixels = true; break;
                case 1:
                    Pixel(image, 8, 12, Colors.Black, "disabled GUI snapping restores fractional origin"); Pixel(image, 16, 12, Colors.Red, "fractional right edge"); Pixel(image, 21, 11, Colors.Black, "child follows fractional control parent");
                    Pixel(image, 11, 44, Colors.Magenta, "unsnapped offset transform"); Pixel(image, 50, 11, Colors.Yellow, "layer fractional translation"); Pixel(subImage, 8, 12, Colors.Cyan, "nested true policy draws independently");
                    Check(subject.Draws == draws, "A GUI policy change reuses retained draw commands."); subject.Rotation = .1f; root.GUISnapControlsToPixels = true; break;
                case 2:
                    rotated = Region(image); Check(rotated.Any(value => value != 0), "Rotated comparison contains actual geometry."); root.GUISnapControlsToPixels = false; break;
                case 3:
                    Check(Region(image).SequenceEqual(rotated!), "An ineligible primary rotation has identical native pixels with either GUI policy."); subject.Rotation = 0; root.GUISnapControlsToPixels = true;
                    RenderingServer.CanvasItemSetTransform(subject.GetCanvasItem(), new Transform(0, new Vector2(4.25f, 2.25f))); break;
                case 4:
                    Pixel(image, 17, 12, Colors.Red, "explicit server transform keeps its independent fractional override"); Pixel(image, 8, 12, Colors.Black, "GUI policy does not replace a server override"); subject.Position = new(2.26f, 2.25f); break;
                case 5:
                    Pixel(image, 8, 12, Colors.Red, "authored transform publication retires the server override and resumes GUI snapping");
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true); GC.WaitForPendingFinalizers(); break;
            }
            phase++; frames = 0;
        }
        root.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); root.Tree!.ProcessFrameStarted += BeforeFrame; RenderingServer.FramePostDraw += Captured; };
        Engine.Run(root); Check(root.IsDisposed && RenderingServer.GetRenderingDevice() is null, "GUI native windows and renderer resources retire fully.");
        Console.WriteLine(backend + " GUI native local/child/layer/offset/nested/rotation/server-override pixels passed.");
    }
}
