using Electron2D;
using SDL = SDL3.SDL;

internal static partial class RenderingRuntimeTests
{
    private static void VerifySplitContainers(string backend)
    {
        using var skin = new SplitContainerTests.Skin(); using var red = SplitPanelTheme(Colors.Red); using var blue = SplitPanelTheme(Colors.Blue); using var green = SplitPanelTheme(Colors.Green);
        using var redStyle = red.GetStyleBox("panel", "Panel")!; using var blueStyle = blue.GetStyleBox("panel", "Panel")!; using var greenStyle = green.GetStyleBox("panel", "Panel")!;
        if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy")
        {
            VerifySplitSoftware(backend, skin, red, blue); VerifySplitWarm(backend, skin, red, blue);
            Console.WriteLine("Split software layout/touch-image/collapse/orientation and warm frames passed; dummy has no native system cursors."); return;
        }
        var window = new Window { Size = new(200, 140), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var split = new SplitContainer { Theme = skin.Theme, Position = new(10, 10), Size = new(100, 60) };
        var a = SplitPanel("A", red); var b = SplitPanel("B", blue); split.AddChild(a); split.AddChild(b); var host = new Control(); window.AddChild(host); host.AddChild(split);
        var frames = 0; var starts = 0; var ends = 0; split.DragStarted += () => starts++; split.DragEnded += () => ends++;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            var windows = SDL.GetWindows(out var count); Check(count == 1, "Split input needs one native window.");
            var native = windows![0]; var id = SDL.GetWindowID(native); var scale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(native) : 1;
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                switch (frames)
                {
                    case 1:
                        Pixel(pixels, 20, 20, Colors.Red); Pixel(pixels, 80, 20, Colors.Blue); Pixel(pixels, 59, 39, Colors.Green);
                        SliderMotion(id, scale, new(60, 40)); SliderButton(id, scale, new(60, 40)); break;
                    case 2:
                        Check(starts == 1, "Native press starts the internal drag area."); SliderMotion(id, scale, new(70, 40)); break;
                    case 3:
                        Check(split.SplitOffset == 10 && a.Size.X == 54, "Native GUI capture adjusts the panel width."); Pixel(pixels, 60, 20, Colors.Red); Pixel(pixels, 80, 20, Colors.Blue);
                        SliderButton(id, scale, new(70, 40), false); split.GetDragAreaControl().FocusMode = FocusMode.All; split.GetDragAreaControl().GrabFocus(); SliderRightKey(id); break;
                    case 4:
                        Check(ends == 1 && split.SplitOffset == 20, "Native release and focused keyboard step execute."); split.LayoutDirection = LayoutDirection.RTL; split.Position = new(10, 10); break;
                    case 5:
                        Pixel(pixels, 20, 20, Colors.Blue); Pixel(pixels, 80, 20, Colors.Red); split.TouchDraggerEnabled = true; break;
                    case 6:
                        Check(pixels.GetPixel(30, 30).R > .2f && pixels.GetPixel(48, 30).B > .2f, "Touch image overlaps both panels with default modulation."); SliderMotion(id, scale, new(40, 40)); break;
                    case 7:
                        Check(pixels.GetPixel(40, 40).R > .5f, "Native touch-image hover uses its theme color."); SliderButton(id, scale, new(40, 40)); break;
                    case 8:
                        Check(starts == 2 && pixels.GetPixel(40, 40).R > .95f, "Touch image forwards native press to the real dragger."); SliderMotion(id, scale, new(30, 40)); break;
                    case 9:
                        Check(split.SplitOffset == 30, "Touch-image capture applies RTL movement."); SliderButton(id, scale, new(30, 40), false); break;
                    case 10:
                        Check(ends == 2, "Touch image release ends the forwarded drag."); split.DraggingEnabled = false; break;
                    case 11:
                        Check(!((Control)split.GetDragAreaControl().GetChild(0, true)).Visible, "Disabling dragging hides the touch image."); split.Collapsed = true; break;
                    case 12:
                        Check(a.Size.X == 44 && b.Size.X == 44 && split.SplitOffset == 30, "Collapse uses defaults while retaining stored offsets.");
                        split.Vertical = true; split.Size = new(60, 100); break;
                    case 13:
                        Pixel(pixels, 20, 20, Colors.Red); Pixel(pixels, 20, 80, Colors.Blue); split.Collapsed = false; split.DraggingEnabled = true; split.TouchDraggerEnabled = false; break;
                    case 14:
                        Pixel(pixels, 20, 70, Colors.Red); Pixel(pixels, 20, 100, Colors.Blue); split.AddChild(SplitPanel("C", green)); break;
                    case 15:
                        Pixel(pixels, 20, 20, Colors.Red); Pixel(pixels, 20, 80, Colors.Blue); Pixel(pixels, 20, 105, Colors.Green); window.Tree!.Quit(); break;
                }
                var profile = SDL.GetCurrentVideoDriver() == "dummy" ? backend + "-dummy" : backend; pixels.SavePNG(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-split-{profile}-{frames}.png"));
            };
        };
        Engine.Run(window); Released(window); Check(frames == 15, "All split native phases completed."); VerifySplitIntersection(backend, skin, red, blue, green); VerifySplitWarm(backend, skin, red, blue);
        Console.WriteLine($"Split native layout, drag/keyboard, RTL/touch, collapse/orientation/multi-panel and warm frame checks passed ({backend}).");
    }
    private static void VerifySplitSoftware(string backend, SplitContainerTests.Skin skin, Theme red, Theme blue)
    {
        var window = new Window { Size = new(160, 120), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var host = new Control(); window.AddChild(host);
        var split = new SplitContainer { Theme = skin.Theme, Position = new(10, 10), Size = new(100, 60) }; var a = SplitPanel("A", red); var b = SplitPanel("B", blue); split.AddChild(a); split.AddChild(b); host.AddChild(split); var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                switch (frames)
                {
                    case 1: Pixel(pixels, 20, 20, Colors.Red); Pixel(pixels, 80, 20, Colors.Blue); Pixel(pixels, 59, 39, Colors.Green); split.SplitOffset = 10; break;
                    case 2: Check(a.Size.X == 54, "Software split offset changes panel width."); Pixel(pixels, 60, 20, Colors.Red); split.LayoutDirection = LayoutDirection.RTL; split.Position = new(10, 10); break;
                    case 3: Pixel(pixels, 20, 20, Colors.Blue); Pixel(pixels, 80, 20, Colors.Red); split.TouchDraggerEnabled = true; break;
                    case 4: Check(pixels.GetPixel(40, 30).R > .2f, "Software draws the overlapping touch image."); split.Collapsed = true; break;
                    case 5: Check(a.Size.X == 44 && !split.GetDragAreaControl().Visible, "Software collapse uses defaults and hides drag areas."); split.Vertical = true; split.Size = new(60, 100); break;
                    case 6: Pixel(pixels, 20, 20, Colors.Red); Pixel(pixels, 20, 80, Colors.Blue); window.Tree!.Quit(); break;
                }
                pixels.SavePNG(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-split-{backend}-dummy-{frames}.png"));
            };
        };
        Engine.Run(window); Released(window); Check(frames == 6, "All software split phases completed.");
    }
    private static void VerifySplitIntersection(string backend, SplitContainerTests.Skin skin, Theme red, Theme blue, Theme green)
    {
        var window = new Window { Size = new(160, 120), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var host = new Control(); window.AddChild(host);
        var outer = new SplitContainer { Theme = skin.Theme, Position = new(10, 10), Size = new(100, 80), DragNestedIntersections = true }; host.AddChild(outer);
        var left = SplitPanel("Left", red); outer.AddChild(left); outer.AddChild(SplitPanel("Right", red)); left.AddChild(new Node { Name = "Unrelated" });
        var nested = new SplitContainer { Name = "Nested", Theme = skin.Theme, Vertical = true, DragNestedIntersections = true }; nested.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); left.AddChild(nested);
        nested.AddChild(SplitPanel("Top", green)); nested.AddChild(SplitPanel("Bottom", blue));
        var frames = 0; var outerEnds = 0; var innerEnds = 0; outer.DragEnded += () => outerEnds++; nested.DragEnded += () => innerEnds++;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black); var windows = SDL.GetWindows(out var nativeCount); var native = windows![0]; var id = SDL.GetWindowID(native); var scale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(native) : 1;
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                if (frames == 2) { Pixel(pixels, 20, 20, Colors.Green); Pixel(pixels, 20, 80, Colors.Blue); SliderMotion(id, scale, new(58, 48)); SliderButton(id, scale, new(58, 48)); }
                else if (frames == 3) SliderMotion(id, scale, new(68, 58));
                else if (frames == 4)
                {
                    Check(outer.SplitOffset == 10 && nested.SplitOffset == 10, "Actual intersection GUI bubbling drags both orthogonal split axes."); Pixel(pixels, 60, 20, Colors.Green); Pixel(pixels, 20, 50, Colors.Green); SliderButton(id, scale, new(68, 58), false);
                }
                else if (frames == 5) { Check(outerEnds == 1 && innerEnds == 1, "Intersection release completes both drags once."); nested.DragNestedIntersections = false; }
                else if (frames == 6) { Check(!outer.GetDragAreaControl().GetChildren(true).Any(n => n.Name.StartsWith("_split_intersection_")), "Native nested disable removes the joint pointer target."); window.Tree!.Quit(); }
                if (frames == 4) pixels.SavePNG(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-split-intersection-{backend}.png"));
            };
        };
        Engine.Run(window); Released(window); Check(frames == 6, "Native intersection phases completed.");
    }
    private static Theme SplitPanelTheme(Color color)
    {
        var theme = new Theme(); var style = new StyleBoxFlat { BGColor = color }; theme.SetStyleBox("panel", "Panel", style); return theme;
    }
    private static Panel SplitPanel(string name, Theme theme) => new() { Name = name, Theme = theme, CustomMinimumSize = new(10, 10), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
    private static void VerifySplitWarm(string backend, SplitContainerTests.Skin skin, Theme red, Theme blue)
    {
        var window = new Window { Size = new(160, 100), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var split = new SplitContainer { Theme = skin.Theme, Size = new(120, 60) }; split.AddChild(SplitPanel("A", red)); split.AddChild(SplitPanel("B", blue)); window.AddChild(split);
        var frames = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); if (frames < 84) { split.SplitOffset = frames % 2; split.Size = new(frames % 2 == 0 ? 120 : 122, 60); } };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                if (frames is >= 20 and < 84) active += bytes; if (frames >= 84) idle += bytes;
                if (++frames == 148) window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window); Check(active == 0 && idle == 0, $"Split warmed frame intervals allocated {active}/{idle} managed bytes ({backend}).");
    }
}
