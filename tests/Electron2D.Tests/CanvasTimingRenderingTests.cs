using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasTiming(string backend, string? fixture = null)
    {
        var settings = ProjectSettings.Service; var oldRollover = ProjectSettings.Get(ProjectSettings.RenderingTimeRolloverSeconds); var oldScale = Engine.TimeScale;
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(176, 112) }; var draws = 0; var frames = 0; var ticks = 0; var disabledTicks = 0;
        var seenRed = false; var seenGreen = false; var changed = false; var lastStep = 0d; var previousTime = 0d;
        var node = new CanvasNode
        {
            Material = material,
            DrawAction = n =>
        {
            draws++;
            n.DrawAnimationSlice(0.08, 0, 0.04); n.DrawSetTransform(new(8, 0)); n.DrawRect(new(0, 0, 8, 8), changed ? Colors.White : Colors.Red);
            n.DrawAnimationSlice(0.08, 0.04, 0.08); n.DrawRect(new(0, 0, 8, 8), Colors.Green);
            n.DrawEndAnimation(); n.DrawRect(new(24, 0, 8, 8), Colors.Blue); n.DrawSetTransform(Vector2.Zero);
            n.DrawRect(new(8, 24, 12, 8), Colors.Red);
            n.DrawRect(new(40, 24, 16, 12), Colors.Green with { A = 0.5f }, false, 4);
            n.DrawRect(new(70, 24, 4, 12), Colors.Blue, false, 4);
            n.DrawRect(new(92, 24, 0, 10), Colors.Magenta, false, 4);
            n.DrawRect(new(124, 36, -12, -10), Colors.Yellow);
            n.DrawRect(new(8, 60, 16, 12), Colors.White, antialiased: true);
            n.DrawSetTransform(new(48, 60), scale: new(1, 2)); n.DrawRect(new(0, 0, 16, 8), Colors.White, antialiased: true);
            n.DrawSetTransform(Vector2.Zero); n.DrawRect(new(100, 60, 20, 12), Colors.White, false, 2, true);
            n.DrawRect(new(140.5f, 60.5f, 16, 12), Colors.Cyan, false, -1, true);
        }
        };
        window.AddChild(node);
        window.AddChild(new CanvasNode { Name = "outside", DrawAction = n => n.DrawRect(new(152, 0, 8, 8), Colors.Magenta) });
        var probe = new CanvasTimeProbe
        {
            ProcessMode = ProcessMode.Always,
            Tick = step =>
        {
            ticks++; lastStep = step;
            if (frames == 6 && ++disabledTicks == 2) RenderingServer.RenderLoopEnabled = true;
        }
        };
        probe.ProcessEnabled = true; window.AddChild(probe);
        try
        {
            Engine.TimeScale = 1; ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, 3600d);
            window.Ready += _ =>
            {
                var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
                Check(server.CanvasTime == 0, "Each renderer starts its clock at zero.");
                var software = RenderingServer.GetCurrentRenderingDriverName() == "software";
                RenderingServer.FramePreDraw += () => Check(server.CanvasTime == previousTime, "Pre-draw sees the previous render time.");
                RenderingServer.FramePostDraw += () =>
                {
                    frames++; var time = server.CanvasTime; var rollover = ProjectSettings.GetWithOverride(ProjectSettings.RenderingTimeRolloverSeconds);
                    Check(Math.Abs(time - (previousTime + lastStep) % rollover) < 1e-12, "Render time consumes the scheduled process step and active rollover.");
                    if (frames == 2) Check(time == previousTime && lastStep == 0, "TimeScale zero freezes intervals.");
                    if (frames == 3) Check(window.Tree!.Paused && lastStep > 0 && time > previousTime, "Paused scene still advances scaled rendering time.");
                    if (frames == 7) Check(disabledTicks == 2 && ticks > frames, "Disabled render frames do not advance the clock.");
                    previousTime = time;
                    var red = time % 0.08 < 0.04; seenRed |= red; seenGreen |= !red;
                    using var image = server.Readback();
                    Pixel(image, 4, 4, red ? Colors.Black : Colors.Green); Pixel(image, 12, 4, red ? (changed ? Colors.White : Colors.Red) : Colors.Black);
                    Pixel(image, 28, 4, red ? Colors.Black : Colors.Blue); Pixel(image, 36, 4, red ? Colors.Blue : Colors.Black);
                    Pixel(image, 156, 4, Colors.Magenta);
                    Pixel(image, 12, 28, Colors.Red); Pixel(image, 40, 24, new(0, 0.5f, 0, 1)); Pixel(image, 48, 30, Colors.Black);
                    Pixel(image, 72, 30, Colors.Blue); Pixel(image, 92, 30, Colors.Magenta); Pixel(image, 116, 30, Colors.Yellow);
                    var feather = image.GetPixel(software ? 23 : 24, 65).R;
                    Check(feather is > 0 and < 0.9f, "Filled local feather has partial alpha at the backend boundary.");
                    if (software) Pixel(image, 24, 65, Colors.Black);
                    var scaled = image.GetPixel(55, 59).R; Check(scaled is > 0 and < 1, "Filled feather scales outside the original rectangle.");
                    var outline = image.GetPixel(98, 65).R; Check(outline is > 0 and < 1, "Outline uses compensated polyline feather.");
                    Pixel(image, 110, 65, Colors.Black); Pixel(image, 148, 60, Colors.Cyan); Pixel(image, 148, 61, Colors.Black);
                    if (frames == 1) Engine.TimeScale = 0;
                    if (frames == 2) { Engine.TimeScale = 2; window.Tree!.Paused = true; }
                    if (frames == 3) { Engine.TimeScale = 1; window.Tree!.Paused = false; ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, 0.025); }
                    if (frames == 4) { ProjectSettings.AddCustomFeature("canvas-time-test"); ProjectSettings.SetFeatureOverride(ProjectSettings.RenderingTimeRolloverSeconds, "canvas-time-test", 0.035); }
                    if (frames == 6) RenderingServer.RenderLoopEnabled = false;
                    if (frames == 7) { ProjectSettings.ClearFeatureOverride(ProjectSettings.RenderingTimeRolloverSeconds, "canvas-time-test"); ProjectSettings.RemoveCustomFeature("canvas-time-test"); ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, 3600d); }
                    if (frames == 9) { Check(draws == 1, "Clock and backend state changes do not rerecord geometry."); changed = true; node.QueueRedraw(); }
                    if (frames >= 12 && seenRed && seenGreen) { Check(draws == 2, "Only explicit redraw rerecords intervals."); window.Tree!.Quit(); }
                    if (frames == 90) throw new InvalidOperationException("Both animation phases must become visible.");
                };
            };
            Engine.Run(window); Released(window);
            Console.WriteLine($"Canvas rectangle and animation-clock pixels passed: {backend}/{fixture ?? "default"} ({frames} frames).");
        }
        finally
        {
            ProjectSettings.ClearFeatureOverride(ProjectSettings.RenderingTimeRolloverSeconds, "canvas-time-test"); ProjectSettings.RemoveCustomFeature("canvas-time-test");
            ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, oldRollover); Engine.TimeScale = oldScale;
        }
    }
    private sealed class CanvasTimeProbe : Node { internal Action<double>? Tick; protected override void OnProcess(double delta) => Tick?.Invoke(delta); }
}
