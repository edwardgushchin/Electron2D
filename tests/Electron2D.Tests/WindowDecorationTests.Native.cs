using Electron2D;
using SDL = SDL3.SDL;
using Path = System.IO.Path;

internal static unsafe partial class WindowDecorationTests
{
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var skin = new Skin(); var root = new Window { Size = new(360, 240), GUIEmbedSubwindows = true, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var window = new Window { Name = "Themed", Theme = skin.Theme, Title = "Title אב", Position = new(40, 90), Size = new(160, 80), Transparent = true, TransparentBG = true, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var other = new Window { Name = "Other", Theme = skin.Theme, Title = "Other", Position = new(235, 90), Size = new(80, 60), Transparent = true, TransparentBG = true }; root.AddChild(window); root.AddChild(other);
        uint windowID = 0; float density = 1;
        void NativeButton(Vector2 point, bool pressed)
        {
            var input = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = pressed ? SDL.EventType.MouseButtonDown : SDL.EventType.MouseButtonUp, WindowID = windowID, Button = (byte)MouseButton.Left, Down = pressed, X = point.X / density, Y = point.Y / density } };
            Check(SDL.PushEvent(ref input), "Native pointer button is queued.");
        }
        void NativeMotion(Vector2 point)
        {
            var input = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = windowID, State = SDL.MouseButtonFlags.Left, X = point.X / density, Y = point.Y / density } };
            Check(SDL.PushEvent(ref input), "Native pointer motion is queued.");
        }
        var phase = 0; var frames = 0; var closed = 0; long before = 0, allocated = 0; window.CloseRequested += () => closed++;
        void BeforeFrame(SceneTree _)
        {
            if (phase < 8) return;
            if (frames >= 32) before = GC.GetAllocatedBytesForCurrentThread();
            if (phase == 8) window.AddThemeColorOverride("title_color", (frames & 1) == 0 ? Colors.White : Colors.Cyan);
        }
        void Pixel(Image image, int x, int y, Color expected, string contract)
        {
            var actual = image.GetPixel(x, y);
            Check(MathF.Abs(actual.R - expected.R) < .02f && MathF.Abs(actual.G - expected.G) < .02f && MathF.Abs(actual.B - expected.B) < .02f, backend + " phase " + phase + " " + contract + " actual " + actual);
        }
        int Count(Image image, Func<Color, bool> test)
        {
            var count = 0; for (var y = 50; y < 89; y++) for (var x = 55; x < 163; x++) if (test(image.GetPixel(x, y))) count++; return count;
        }
        void Captured()
        {
            if (phase >= 8)
            {
                if (frames >= 32) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames < 96) return;
                var operation = phase == 8 ? "active theme/redraw/render" : "retained idle render";
                Check(allocated == 0, "Prepared " + operation + " chrome frames allocate " + allocated + " managed owner bytes.");
                Console.WriteLine(backend + " Window chrome: 64 prepared " + operation + " frames, " + allocated + " managed owner bytes.");
                if (phase == 8) { phase++; frames = 0; allocated = 0; } else root.Tree!.Quit();
                return;
            }
            // Let initial compositor focus/enter/motion events settle before queuing the gesture.
            if (++frames < (phase == 0 ? 30 : 3)) return;
            using var image = root.GetTexture().GetImage()!;
            var directory = Environment.GetEnvironmentVariable("ELECTRON2D_WINDOW_CHROME_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); image.SavePNG(Path.Combine(directory, "phase-" + phase + ".png")); }
            switch (phase)
            {
                case 0:
                    Pixel(image, 36, 75, Colors.Green, "focused expanded border"); Pixel(image, 80, 120, Colors.Green, "content background draws beneath a transparent viewport"); Pixel(image, 176, 69, Colors.Blue, "actual normal close texture");
                    Check(Count(image, c => c.R > .9f && c.G > .9f && c.B > .9f) > 0, "The actual shaped title is rendered.");
                    NativeButton(ClosePoint(window), true); Check(window.Visible && closed == 0, "Native close is pending until release."); break;
                case 1:
                    Check(closed == 0 && window.Visible, "A delivered native down event does not close the window.");
                    Pixel(image, 176, 69, Colors.Red, "pressed texture"); Pixel(image, 187, 77, Colors.Red, "pressed texture uses its own dimensions");
                    NativeMotion(new(10, 10)); break;
                case 2:
                    Pixel(image, 176, 69, Colors.Blue, "leaving pressed bounds restores the ordinary icon"); Check(closed == 0 && window.Position == new Vector2i(40, 90), "Native canceled close never moves the window.");
                    NativeMotion(ClosePoint(window)); NativeButton(ClosePoint(window), false); break;
                case 3:
                    Check(closed == 1 && window.Visible, "Native release requests close and preserves application-owned visibility.");
                    Pixel(image, 176, 69, Colors.Blue, "release clears pressed visuals"); other.GrabFocus(); break;
                case 4:
                    Pixel(image, 36, 75, Colors.Yellow, "focus loss chooses the unfocused style");
                    window.LayoutDirection = LayoutDirection.RTL; window.AddThemeConstantOverride("title_outline_size", 2);
                    NativeButton(ClosePoint(window), true); var focus = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowFocusLost, WindowID = windowID } }; Check(SDL.PushEvent(ref focus), "Native root focus-loss is queued after a close press."); break;
                case 5:
                    Check(Count(image, c => c.R > .9f && c.G < .1f && c.B < .1f) > 0, "RTL title draws its independently colored outline.");
                    Pixel(image, 176, 69, Colors.Blue, "root focus loss cancels the descendant close capture"); Check(closed == 1, "Focus loss leaves the window open."); NativeButton(ClosePoint(window), false);
                    using (var replacement = Image.CreateEmpty(18, 12, false, Image.Format.Rgba8)) { replacement.Fill(Colors.Magenta); skin.Close.SetImage(replacement); }
                    window.BeginBulkThemeOverride(); window.AddThemeConstantOverride("close_h_offset", 70); window.AddThemeConstantOverride("close_v_offset", 40); window.AddThemeConstantOverride("title_height", 54); window.EndBulkThemeOverride(); break;
                case 6:
                    Pixel(image, 136, 55, Colors.Magenta, "resource replacement and offsets change actual native icon pixels"); Pixel(image, 176, 69, Colors.Green, "old icon position is retired");
                    window.Borderless = true; break;
                case 7:
                    Pixel(image, 36, 75, Colors.Black, "borderless removes expanded decoration"); Pixel(image, 136, 55, Colors.Black, "borderless removes the close image");
                    window.Borderless = false; window.GrabFocus(); window.LayoutDirection = LayoutDirection.LTR; window.RemoveThemeConstantOverride("title_outline_size");
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true); GC.WaitForPendingFinalizers(); break;
            }
            phase++; frames = 0;
        }
        root.Ready += _ => { var native = SDL.GetWindows(out var count); Check(count == 1, "One native host owns embedded chrome."); windowID = SDL.GetWindowID(native![0]); density = SDL.GetWindowPixelDensity(native[0]); Input.EmulateTouchFromMouse = false; RenderingServer.SetDefaultClearColor(Colors.Black); window.GrabFocus(); root.Tree!.ProcessFrameStarted += BeforeFrame; RenderingServer.FramePostDraw += Captured; };
        Engine.Run(root); Check(root.IsDisposed && RenderingServer.GetRenderingDevice() is null, "The native chrome owner and renderer retire completely.");
        Console.WriteLine(backend + " Window chrome native focused/unfocused borders, pressed/canceled/released icons, title/RTL/outline, resource refresh and borderless pixels passed.");
    }
}
