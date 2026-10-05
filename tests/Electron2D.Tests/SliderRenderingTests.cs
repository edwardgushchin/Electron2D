using Electron2D;
using SDL = SDL3.SDL;

internal static partial class RenderingRuntimeTests
{
    private static void VerifySliders(string backend)
    {
        using var skin = new SliderTests.Skin();
        var window = new Window { Size = new(200, 140) };
        var h = new HSlider { Name = "Horizontal", Theme = skin.Theme, Position = new(10, 10), Size = new(100, 20), Value = 50, TickCount = 3, TextureFilter = TextureFilter.Nearest, MouseFilter = MouseFilter.Ignore };
        var v = new VSlider { Name = "Vertical", Theme = skin.Theme, Position = new(130, 10), Size = new(20, 100), Value = 25, TickCount = 3, TextureFilter = TextureFilter.Nearest, MouseFilter = MouseFilter.Ignore };
        var baseline = new HSlider { Name = "Default", Position = new(10, 60), Size = new(100, 30), Value = 50, MouseFilter = MouseFilter.Ignore };
        window.AddChild(h); window.AddChild(v); window.AddChild(baseline);
        var frames = 0; var starts = 0; var ended = false; var startSawOldValue = false;
        h.DragStarted += () => { starts++; startSawOldValue = h.Value == 50; }; h.DragEnded += changed => ended = changed;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            var nativeWindows = SDL.GetWindows(out var count); Check(count == 1 && nativeWindows is { Length: 1 }, "Slider input needs one native window.");
            var nativeWindow = nativeWindows![0]; var windowID = SDL.GetWindowID(nativeWindow);
            var mouseScale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(nativeWindow) : 1f;
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                try
                {
                    switch (frames)
                    {
                        case 1:
                            Pixel(pixels, 20, 19, Colors.Red); Pixel(pixels, 90, 19, Colors.Blue); Pixel(pixels, 59, 17, Colors.White);
                            Pixel(pixels, 139, 20, Colors.Blue); Pixel(pixels, 139, 100, Colors.Red); Pixel(pixels, 139, 82, Colors.White);
                            Pixel(pixels, 59, 24, Colors.Magenta); Pixel(pixels, 60, 25, Colors.White);
                            Check(pixels.GetPixel(60, 75).R > .5f && baseline.GetMinimumSize().Y >= 16, "Default slider skin supplies visible geometry and a real grabber minimum.");
                            h.MouseFilter = v.MouseFilter = MouseFilter.Stop; SliderMotion(windowID, mouseScale, new(60, 20)); break;
                        case 2:
                            Pixel(pixels, 20, 19, Colors.Yellow); Pixel(pixels, 56, 15, Colors.Cyan); Pixel(pixels, 90, 19, Colors.Blue);
                            SliderButton(windowID, mouseScale, new(105, 20)); break;
                        case 3:
                            Check(starts == 1 && startSawOldValue && h.Value == 100, "Actual GUI press emits DragStarted before changing the value.");
                            Pixel(pixels, 102, 15, Colors.Cyan); Pixel(pixels, 98, 19, Colors.Yellow);
                            SliderMotion(windowID, mouseScale, new(5, 20)); break;
                        case 4:
                            Check(h.Value == 0 && h.HasFocus() && !h.HasFocus(ignoreHiddenFocus: true), "Pressed GUI capture delivers outside dragging while pointer focus remains hidden.");
                            Pixel(pixels, 12, 17, Colors.White); Pixel(pixels, 30, 19, Colors.Blue);
                            SliderButton(windowID, mouseScale, new(5, 20), false);
                            h.GrabFocus(); SliderRightKey(windowID); break;
                        case 5:
                            Check(ended && h.Value == 1 && h.HasFocus(ignoreHiddenFocus: true), "Native release completes the drag before visible keyboard focus steps the slider.");
                            Pixel(pixels, 12, 15, Colors.Cyan);
                            h.LayoutDirection = LayoutDirection.RTL; h.Position = new(10, 10); h.Value = 25; h.TicksPosition = Slider.TickPosition.Both; h.TicksOnBorders = true; break;
                        case 6:
                            Pixel(pixels, 80, 15, Colors.Cyan); Pixel(pixels, 20, 19, Colors.Blue); Pixel(pixels, 100, 19, Colors.Yellow);
                            Pixel(pixels, 14, 24, Colors.Magenta); Pixel(pixels, 14, 14, Colors.Yellow); Pixel(pixels, 104, 24, Colors.Magenta);
                            h.Editable = false; break;
                        case 7:
                            Pixel(pixels, 83, 18, new(.5f, .5f, .5f, 1)); Pixel(pixels, 100, 19, Colors.Red);
                            SliderButton(windowID, mouseScale, new(50, 20), button: MouseButton.WheelUp);
                            SliderMotion(windowID, mouseScale, new(140, 15)); SliderButton(windowID, mouseScale, new(140, 15)); SliderButton(windowID, mouseScale, new(140, 15), false); break;
                        default:
                            Check(h.Value == 25 && v.Value == 100, "Noneditable sliders ignore native wheel input while vertical clicks map the top edge to the upper endpoint.");
                            Pixel(pixels, 137, 12, Colors.Cyan); Pixel(pixels, 139, 100, Colors.Yellow);
                            File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-sliders-{backend}.png"), pixels.SavePNGToBuffer()); window.Tree!.Quit(); break;
                    }
                }
                catch (Exception error) { throw new InvalidOperationException($"Slider native {backend}, frame {frames}.", error); }
            };
        };
        Engine.Run(window); Released(window);
        VerifySliderWarm(backend, skin);
        Console.WriteLine($"Sliders native default/themed artwork, GUI drag/keyboard/wheel, RTL, tick flips, disabled and vertical states plus warmed frames passed: {backend}.");
    }

    private static void SliderMotion(uint windowID, float scale, Vector2 position)
    {
        var input = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = windowID, Which = 987, X = position.X / scale, Y = position.Y / scale } };
        Check(SDL.PushEvent(ref input), "Queue native slider motion for the next engine event pump.");
    }
    private static void SliderButton(uint windowID, float scale, Vector2 position, bool pressed = true, MouseButton button = MouseButton.Left)
    {
        var input = button == MouseButton.WheelUp
            ? new SDL.Event { Wheel = new SDL.MouseWheelEvent { Type = SDL.EventType.MouseWheel, WindowID = windowID, Which = 987, Y = 1, MouseX = position.X / scale, MouseY = position.Y / scale } }
            : new SDL.Event { Button = new SDL.MouseButtonEvent { Type = pressed ? SDL.EventType.MouseButtonDown : SDL.EventType.MouseButtonUp, WindowID = windowID, Which = 987, Button = 1, Down = pressed, X = position.X / scale, Y = position.Y / scale } };
        Check(SDL.PushEvent(ref input), "Queue native slider button or wheel input for the next engine event pump.");
    }
    private static void SliderRightKey(uint windowID)
    {
        var input = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = windowID, Key = SDL.Keycode.Right, Scancode = SDL.Scancode.Right, Down = true } };
        Check(SDL.PushEvent(ref input), "Queue native slider key press.");
        input.Key.Type = SDL.EventType.KeyUp; input.Key.Down = false;
        Check(SDL.PushEvent(ref input), "Queue native slider key release.");
    }

    private static void VerifySliderWarm(string backend, SliderTests.Skin skin)
    {
        var window = new Window { Size = new(180, 130) };
        var h = new HSlider { Theme = skin.Theme, Position = new(10, 10), Size = new(100, 20), TickCount = 7, TicksOnBorders = true, TicksPosition = Slider.TickPosition.Both, MouseFilter = MouseFilter.Ignore };
        var v = new VSlider { Theme = skin.Theme, Position = new(140, 10), Size = new(20, 100), TickCount = 5, TicksPosition = Slider.TickPosition.Center, MouseFilter = MouseFilter.Ignore };
        window.AddChild(h); window.AddChild(v); var frames = 0; var hDraws = 0; var vDraws = 0; long before = 0, allocated = 0;
        h.Draw += _ => hDraws++; v.Draw += _ => vDraws++;
        window.Ready += _ =>
        {
            var tree = window.Tree!;
            tree.ProcessFrameStarted += frameTree =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                h.Value = frames % 2 == 0 ? 25 : 75; v.Value = frames % 2 == 0 ? 75 : 25;
                h.Editable = frames % 2 == 0; v.Editable = frames % 2 != 0;
                h.Size = frames % 2 == 0 ? new(100, 20) : new(102, 22);
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames == 128) tree.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(frames == 128 && hDraws == 128 && vDraws == 128 && allocated == 0, $"Warmed {backend} slider value/state/size mutation, recording and render allocated {allocated} bytes over 64 ProcessFrameStarted-to-FramePostDraw frames.");
    }
}
