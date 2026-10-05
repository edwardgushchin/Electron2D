using Electron2D;
using SDL3;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyTooltips(string backend)
    {
        var settings = ProjectSettings.Service; var delay = ProjectSettings.Get(ProjectSettings.TooltipDelaySeconds);
        ProjectSettings.Set(ProjectSettings.TooltipDelaySeconds, 0);
        try
        {
            var window = new Window { Size = new(180, 110), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
            var cover = new Control { Name = "Cover", Size = new(180, 110), MouseFilter = MouseFilter.Stop };
            var owner = new NativeTooltipOwner { Name = "Owner", Position = new(10, 10), Size = new(60, 30), TooltipText = "Tooltip", MouseFilter = MouseFilter.Stop };
            owner.AddChild(new Node { Name = "Tooltip" });
            window.AddChild(cover); window.AddChild(owner);
            var top = new CanvasLayer { Name = "GameOverlay", Layer = int.MaxValue };
            top.AddChild(new TooltipSolid { Name = "Green", Size = new(180, 110), Tint = Colors.Green, MouseFilter = MouseFilter.Ignore });
            window.AddChild(top);
            var position = new Vector2(20, 20);
            var hits = 0; cover.GUIInput += input => { if (input is InputEventMouseButton { Pressed: true }) hits++; };
            var frame = 0;
            window.Ready += _ =>
            {
                var nativeWindow = SDL.GetWindows(out var windowCount)![0]; var windowID = SDL.GetWindowID(nativeWindow);
                var scale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(nativeWindow) : 1f;
                var tree = window.Tree!; var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    frame++; using var pixels = server.Readback();
                    if (frame == 1) { SliderMotion(windowID, scale, position); return; }
                    if (frame == 2)
                    {
                        var panel = tree.TooltipPanel ?? throw new InvalidOperationException("Default tooltip missing from native frame.");
                        var point = panel.Position + new Vector2(1, 1);
                        Pixel(pixels, (int)point.X, (int)point.Y, new Color(0, .5f, 0));
                        var bright = 0;
                        for (var y = (int)panel.Position.Y; y < panel.Position.Y + panel.Size.Y; y++)
                            for (var x = (int)panel.Position.X; x < panel.Position.X + panel.Size.X; x++)
                                if (pixels.GetPixel(x, y).R > .6f) bright++;
                        Check(bright > 10, "Default tooltip renders real visible font glyphs above an int.MaxValue game layer.");
                        File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-tooltip-{backend}-default.png"), pixels.SavePNGToBuffer());
                        position = new(179, 109); SliderMotion(windowID, scale, position); return;
                    }
                    if (frame == 3)
                    {
                        Check(tree.TooltipPanel is null, "Leaving the tooltip target closes its native presentation.");
                        owner.UseCustom = true; owner.Position = new(145, 85); owner.Size = new(35, 25);
                        position = new(160, 95); SliderMotion(windowID, scale, position); return;
                    }
                    if (frame == 4)
                    {
                        var panel = tree.TooltipPanel ?? throw new InvalidOperationException("Custom tooltip missing from native frame.");
                        Check(panel.Position.X < position.X && panel.Position.Y < position.Y, "Tooltip flips at both viewport edges.");
                        var point = panel.Position + new Vector2(10, 4);
                        Pixel(pixels, (int)point.X, (int)point.Y, Colors.Blue);
                        File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-tooltip-{backend}-custom.png"), pixels.SavePNGToBuffer());
                        SliderButton(windowID, scale, point); SliderButton(windowID, scale, point, false); return;
                    }
                    Check(hits == 1 && tree.TooltipPanel is null, "Tooltip contents remain transparent to real routed pointer input during cancellation.");
                    tree.Quit();
                };
            };
            Engine.Run(window); Released(window);
            VerifyTooltipWarm(backend);
            Console.WriteLine($"Native tooltip glyphs, overlay ordering, custom content, edge placement, pointer transparency and warmed frames passed: {backend}.");
        }
        finally { ProjectSettings.Set(ProjectSettings.TooltipDelaySeconds, delay); }
    }

    private static void VerifyTooltipWarm(string backend)
    {
        var window = new Window { Size = new(180, 110), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var owner = new Control { Size = new(80, 30), TooltipText = "Warm tooltip", MouseFilter = MouseFilter.Stop };
        window.AddChild(owner);
        var position = new Vector2(10, 10);
        var frame = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            var nativeWindow = SDL.GetWindows(out var windowCount)![0]; var windowID = SDL.GetWindowID(nativeWindow);
            var scale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(nativeWindow) : 1f;
            var tree = window.Tree!; var server = RenderingServer.Service!;
            tree.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (tree.TooltipPanel is { } panel) panel.SelfModulate = (frame & 1) == 0 ? Colors.White : new Color(.5f, .5f, .5f);
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame == 0) SliderMotion(windowID, scale, position);
                if (++frame == 128) tree.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(allocated == 0, $"Tooltip {backend} 64 warmed active modulation frames allocate {allocated} bytes from ProcessFrameStarted through FramePostDraw.");
    }

    private sealed class NativeTooltipOwner : Control
    {
        internal bool UseCustom;
        protected override Control? OnMakeCustomTooltip(string forText) => UseCustom ? new TooltipSolid { CustomMinimumSize = new(32, 18), Tint = Colors.Blue, FocusMode = FocusMode.All, MouseBehaviorRecursive = RecursiveBehavior.Enabled } : null;
    }
    private sealed class TooltipSolid : Control
    {
        internal Color Tint;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationDraw) DrawRect(new(Vector2.Zero, Size), Tint); }
    }
}
