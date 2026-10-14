using Electron2D;
using SDL = SDL3.SDL;

internal static class LinkButtonRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(240, 90), Title = "Link text runtime" };
        var link = new LinkButton("MMMMMMMM") { Name = "Link", Position = new(12, 12), Size = new(180, 50), FocusMode = FocusMode.All };
        link.AddThemeFontSizeOverride("font_size", 18); link.AddThemeColorOverride("font_color", Colors.White);
        link.AddThemeColorOverride("font_hover_color", Colors.Green); link.AddThemeColorOverride("font_pressed_color", Colors.Red);
        link.AddThemeColorOverride("font_disabled_color", Colors.Blue); link.AddThemeColorOverride("font_focus_color", Colors.Cyan);
        using var focus = new StyleBoxEmpty(); link.AddThemeStyleBoxOverride("focus", focus);
        window.AddChild(link); using var target = window.GetTexture();
        var frames = 0; var activations = 0; var baselineInk = 0; long before = 0, warm = 0;
        link.Pressed += () => activations++;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); if (frames >= 9) link.SelfModulate = (frames & 1) == 0 ? Colors.White : new(.8f, .8f, .8f); };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            if (frames >= 73) warm += GC.GetAllocatedBytesForCurrentThread() - before;
            frames++;
            if (frames > 9)
            {
                if (frames < 137) return;
                Check(warm == 0, "64 warm LinkButton full rendered frame bytes=" + warm);
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit(); return;
            }
            using var image = target.GetImage() ?? throw new InvalidOperationException("Link readback.");
            var ink = Count(image, new(12, 12, 180, 50), frames switch { 4 => Colors.Green, 5 => Colors.Red, 6 => Colors.Green, 7 => Colors.Blue, 8 => Colors.Cyan, _ => Colors.White });
            Check(ink > 50, "Real shaped glyph/state pixels, phase=" + frames);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, "link-" + backend + "-" + frames + ".png")); }
            switch (frames)
            {
                case 1: baselineInk = ink; link.Underline = LinkButton.UnderlineMode.Never; break;
                case 2: Check(ink < baselineInk - 40, "Never removes actual underline pixels."); link.Underline = LinkButton.UnderlineMode.OnHover; break;
                case 3:
                    Check(ink < baselineInk - 40, "OnHover omits idle underline.");
                    NativeMotion(new(20, 20)); break;
                case 4:
                    NativeButton(true); break;
                case 5:
                    NativeButton(false); break;
                case 6:
                    Check(activations == 1, "Rendered native pointer release activates exactly once."); link.Disabled = true; break;
                case 7:
                    link.Disabled = false; link.Underline = LinkButton.UnderlineMode.Always;
                    NativeMotion(new(230, 80));
                    link.GrabFocus(); break;
                case 8:
                    link.ReleaseFocus(); link.LayoutDirection = LayoutDirection.RTL; link.TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce; link.Size = new(35, 50); break;
                case 9:
                    var right = checked((int)Math.Ceiling(link.GetGlobalRect().End.X)) + 1;
                    Check(link.Size.X == 35 && Count(image, new(right, 12, 240 - right, 50), Colors.White) == 0, "RTL ellipsis stays within the actual resized control bounds.");
                    link.TextOverrunBehavior = TextOverrunBehavior.NoTrimming; link.Size = new(180, 50); break;
            }
        }
        try { Check(Engine.Run(window) == 0 && frames == 137, "Link native host completed."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        Console.WriteLine("LinkButton " + backend + ": nine actual glyph/underline/hover/press/disabled/focus/RTL-ellipsis phases, activation and 64 warm rendered frames=0 owner bytes passed.");
    }
    private static void NativeMotion(Vector2 point)
    {
        var input = new SDL.Event { Type = (uint)SDL.EventType.MouseMotion };
        input.Motion.WindowID = SDL.GetWindowID(SDL.GetWindows(out _)![0]); input.Motion.X = point.X; input.Motion.Y = point.Y; input.Motion.XRel = 1;
        Check(SDL.PushEvent(ref input), "Native motion enqueued.");
    }
    private static void NativeButton(bool pressed)
    {
        var input = new SDL.Event { Type = (uint)(pressed ? SDL.EventType.MouseButtonDown : SDL.EventType.MouseButtonUp) };
        input.Button.WindowID = SDL.GetWindowID(SDL.GetWindows(out _)![0]); input.Button.Which = 987; input.Button.X = 20; input.Button.Y = 20; input.Button.Button = SDL.ButtonLeft; input.Button.Down = pressed;
        Check(SDL.PushEvent(ref input), "Native button edge enqueued.");
    }
    private static int Count(Image image, Rect2i region, Color target)
    {
        var count = 0;
        for (var y = region.Position.Y; y < region.End.Y; y++) for (var x = region.Position.X; x < region.End.X; x++)
            {
                var c = image.GetPixel(x, y); if (Math.Abs(c.R - target.R) < .12f && Math.Abs(c.G - target.G) < .12f && Math.Abs(c.B - target.B) < .12f) count++;
            }
        return count;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
