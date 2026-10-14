using Electron2D;
using SDL = SDL3.SDL;

internal static class TouchControlNativeTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.DebugCollisionShapeColor, Colors.Cyan);
        using var shape = new RectangleShape { Size = new(6, 6) };
        InputMap.AddAction("native-touch"); Input.EmulateMouseFromTouch = false;
        using var normalImage = Image.CreateEmpty(24, 24, false, Image.Format.Rgba8); normalImage.Fill(Colors.Blue);
        using var pressedImage = Image.CreateEmpty(24, 24, false, Image.Format.Rgba8); pressedImage.Fill(Colors.Red);
        using var normal = ImageTexture.CreateFromImage(normalImage); using var pressed = ImageTexture.CreateFromImage(pressedImage);
        using var theme = new Theme(); using var baseStyle = new StyleBoxFlat { BGColor = Colors.Green, AntiAliasing = false }; using var tipStyle = new StyleBoxFlat { BGColor = Colors.Yellow, AntiAliasing = false };
        foreach (var name in new[] { "normal_joystick", "pressed_joystick" }) theme.SetStyleBox(name, "VirtualJoystick", baseStyle);
        foreach (var name in new[] { "normal_tip", "pressed_tip" }) theme.SetStyleBox(name, "VirtualJoystick", tipStyle);
        var window = new Window { Size = new(300, 160), Title = "Touch game controls" };
        var button = new TouchScreenButton { Position = new(10, 10), TextureNormal = normal, TexturePressed = pressed, Action = "native-touch" };
        var joystick = new VirtualJoystick { Position = new(120, 20), Size = new(100, 100), JoystickSize = 80, TipSize = 20, Theme = theme };
        window.AddChild(button); window.AddChild(joystick); using var target = window.GetTexture(); using var motion = new InputEventScreenDrag { Index = 1 };
        var frames = 0; long before = 0, bytes = 0, idle = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ => { if (frames >= 14 && frames < 88) { before = GC.GetAllocatedBytesForCurrentThread(); motion.Position = new(50 + ((frames & 1) == 0 ? 10 : 20), 50); joystick.DispatchGUIInput(motion); } else if (frames >= 88) before = GC.GetAllocatedBytesForCurrentThread(); };
            RenderingServer.FramePostDraw += Capture;
        };
        void Native(SDL.EventType type, long finger, Vector2 position)
        {
            var size = DisplayServer.WindowGetSize();
            var input = new SDL.Event { TFinger = new SDL.TouchFingerEvent { Type = type, WindowID = SDL.GetWindowID(SDL.GetWindows(out var count)![0]), TouchID = 99, FingerID = unchecked((ulong)finger), X = position.X / size.X, Y = position.Y / size.Y, Pressure = .5f, Timestamp = SDL.GetTicksNS() } };
            Check(SDL.PushEvent(ref input), "SDL accepts touch event.");
        }
        void Pixel(Image image, int x, int y, Color color)
        { var actual = image.GetPixel(x, y); Check(MathF.Abs(actual.R - color.R) < .02 && MathF.Abs(actual.G - color.G) < .02 && MathF.Abs(actual.B - color.B) < .02, $"Touch pixel ({x},{y})={actual}, expected {color}."); }
        void Capture()
        {
            if (frames >= 24 && frames < 88) bytes += GC.GetAllocatedBytesForCurrentThread() - before; else if (frames >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before; frames++;
            if (frames == 4) { using var image = target.GetImage()!; Pixel(image, 18, 18, Colors.Blue); Pixel(image, 170, 70, Colors.Yellow); Native(SDL.EventType.FingerDown, 101, new(18, 18)); Native(SDL.EventType.FingerDown, 102, new(170, 70)); Native(SDL.EventType.FingerMotion, 102, new(190, 70)); }
            if (frames == 6)
            {
                Check(button.IsPressed() && Input.IsActionPressed("native-touch") && MathF.Abs(Input.GetActionStrength("ui_right") - .5f) < .001f, "Native distinct contacts publish button and analog actions.");
                using var image = target.GetImage()!; Pixel(image, 18, 18, Colors.Red); Pixel(image, 190, 70, Colors.Yellow); Pixel(image, 170, 70, Colors.Green);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory) { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "touch-" + backend + ".png")); }
                Native(SDL.EventType.FingerUp, 101, new(18, 18)); Native(SDL.EventType.FingerUp, 102, new(190, 70));
            }
            if (frames == 8) { Check(!button.IsPressed() && !Input.IsActionPressed("native-touch") && !Input.IsActionPressed("ui_right"), "Native releases retire both sources."); Native(SDL.EventType.FingerDown, 103, new(170, 70)); motion.Index = 1; }
            if (frames == 10) { Native(SDL.EventType.FingerMotion, 103, new(190, 70)); }
            if (frames == 12) { Check(Input.IsActionPressed("ui_right"), "Reused native contact remains routed."); button.Shape = shape; window.Tree!.DebugCollisionsHint = true; button.QueueRedraw(); }
            if (frames == 14) { using var image = target.GetImage()!; Pixel(image, 22, 22, Colors.Cyan); Pixel(image, 12, 12, Colors.Blue); button.ShapeCentered = false; }
            if (frames == 16) { using var image = target.GetImage()!; Pixel(image, 10, 10, Colors.Cyan); Pixel(image, 22, 22, Colors.Blue); button.ShapeVisible = false; joystick.VisibilityMode = JoystickVisibility.WhenTouched; }
            if (frames == 18) { using var image = target.GetImage()!; Pixel(image, 10, 10, Colors.Blue); }
            if (frames == 88) { Check(bytes == 0, "64 warmed prepared local motion/layout/render frames: " + bytes + " B."); joystick.Visible = false; Check(!Input.IsActionPressed("ui_right"), "Native hide cancels contact."); }
            if (frames == 90) joystick.Visible = true;
            if (frames == 92) { using var image = target.GetImage()!; Pixel(image, 170, 70, Colors.Black); window.Tree!.EditedSceneRoot = joystick; joystick.QueueRedraw(); }
            if (frames == 94) { using var image = target.GetImage()!; Pixel(image, 170, 70, Colors.Yellow); window.Tree!.EditedSceneRoot = null; joystick.QueueRedraw(); }
            if (frames == 160) { Check(idle == 0, "64 warmed idle owner frames: " + idle + " B."); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit(); }
        }
        try { Check(Engine.Run(window) == 0 && frames == 160, "Native touch workflow completed."); }
        finally { InputMap.EraseAction("native-touch"); }
        Console.WriteLine("Touch " + backend + ": SDL multi-contact button/joystick input, action strengths, rendered states, cancellation and 64 warmed prepared local motion/render=" + bytes + " B, idle=" + idle + " B passed.");
    }
}
