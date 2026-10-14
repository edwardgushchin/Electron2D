using Electron2D;
using SDL = SDL3.SDL;

internal static class FoldableNativeTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Pixel(Image image, Control node, Vector2 local, Color expected)
    {
        var point = node.GetGlobalTransformWithCanvas() * local; var actual = image.GetPixel((int)point.X, (int)point.Y);
        Check(MathF.Abs(actual.R - expected.R) < .02f && MathF.Abs(actual.G - expected.G) < .02f && MathF.Abs(actual.B - expected.B) < .02f, $"Pixel {point}: expected {expected}, actual {actual}.");
    }
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var theme = new Theme();
        using var normal = new StyleBoxFlat { BGColor = Colors.Blue, AntiAliasing = false }; normal.SetContentMarginAll(4);
        using var folded = new StyleBoxFlat { BGColor = Colors.Red, AntiAliasing = false }; folded.SetContentMarginAll(4);
        using var content = new StyleBoxFlat { BGColor = Colors.Green, AntiAliasing = false }; content.SetContentMarginAll(4);
        using var border = new StyleBoxFlat { DrawCenter = false, BorderColor = Colors.Yellow, AntiAliasing = false }; border.SetBorderWidthAll(2);
        foreach (var name in new[] { "title_panel", "title_hover_panel" }) theme.SetStyleBox(name, "FoldableContainer", normal);
        foreach (var name in new[] { "title_collapsed_panel", "title_collapsed_hover_panel" }) theme.SetStyleBox(name, "FoldableContainer", folded);
        theme.SetStyleBox("panel", "FoldableContainer", content); theme.SetStyleBox("panel", "Panel", content); theme.SetStyleBox("focus", "FoldableContainer", border);
        using var group = new FoldableGroup(); var window = new Window { Size = new(340, 180), Title = "Foldable controls validation" };
        var a = new FoldableContainer("A") { Name = "A", Position = new(4, 4), Size = new(140, 120), Theme = theme, FoldableGroup = group }; var b = new FoldableContainer("B") { Name = "B", Position = new(180, 4), Size = new(140, 120), Theme = theme, FoldableGroup = group };
        var bodyA = new Panel { Name = "Body", CustomMinimumSize = new(30, 20) }; var bodyB = new Panel { Name = "Body", CustomMinimumSize = new(30, 20) }; a.AddChild(bodyA); b.AddChild(bodyB);
        var extra = new Button("+") { Name = "Extra" }; a.AddTitleBarControl(extra); window.AddChild(a); window.AddChild(b);
        using var target = window.GetTexture(); var frames = 0; long before = 0, bytes = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); window.Tree!.ProcessFrameStarted += _ =>
            {
                if (frames >= 12) { before = GC.GetAllocatedBytesForCurrentThread(); if ((frames & 1) == 0) a.Expand(); else b.Expand(); }
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            if (frames >= 24) bytes += GC.GetAllocatedBytesForCurrentThread() - before; frames++;
            if (frames is >= 4 and <= 6)
            {
                using var image = target.GetImage()!;
                if (frames == 4) { Pixel(image, a, new(70, 10), Colors.Blue); Pixel(image, a, new(30, 70), Colors.Green); Pixel(image, b, new(70, 10), Colors.Red); Pixel(image, b, new(30, 70), Colors.Black); Check(extra.Visible, "Title button visible."); }
                if (frames == 5) { Pixel(image, a, new(30, 70), Colors.Black); Pixel(image, b, new(30, 70), Colors.Green); }
                if (frames == 6) { Pixel(image, b, new(70, 110), Colors.Blue); Pixel(image, b, new(30, 40), Colors.Green); }
                if (frames == 4 && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory) { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "foldable-" + backend + ".png")); }
            }
            if (frames == 4) b.Expand();
            if (frames == 5) { b.TitlePosition = BarPosition.Bottom; b.LayoutDirection = LayoutDirection.RTL; }
            if (frames == 6) { b.GrabFocus(); b.TitleTextOverrunBehavior = TextOverrunBehavior.TrimEllipsis; b.Title = "Long translated title with ellipsis"; }
            if (frames == 8)
            {
                using var image = target.GetImage()!; Pixel(image, b, new(1, 1), Colors.Yellow);
                var input = new SDL.Event { Type = (uint)SDL.EventType.KeyDown }; input.Key.WindowID = SDL.GetWindowID(SDL.GetWindows(out var count)![0]); input.Key.Scancode = SDL.Scancode.Return; input.Key.Key = SDL.Keycode.Return; input.Key.Down = true; Check(SDL.PushEvent(ref input), "Native queue accepts action key.");
                b.Title = "B";
            }
            if (frames == 10) Check(!b.Folded, "Native focused key respects last-expanded group policy.");
            if (frames == 88) { Check(bytes == 0, "64 warm fold/group/layout/render frames: " + bytes + " B."); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit(); }
        }
        Check(Engine.Run(window) == 0 && frames == 88, "Native foldable workflow completed.");
        Console.WriteLine("Foldable " + backend + ": headers/content/group, bottom/RTL, focus/action/title controls and 64 warmed owner frames=" + bytes + " B passed.");
    }
}
