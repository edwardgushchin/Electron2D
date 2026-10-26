using System.Diagnostics;
using Electron2D;

internal static class WindowInputRegionNativeTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D input-region probe"); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Disabled);
        var root = new Window { Title = "Input region underlay", Mode = WindowMode.Maximized, Borderless = true };
        var top = new Window { Title = "Input region overlay", Transient = true, AlwaysOnTop = true, Borderless = true, Visible = false, ForceNative = true };
        var underlay = new ColorRect { Color = Colors.Blue, MouseFilter = MouseFilter.Ignore }; root.AddChild(underlay); underlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var overlay = new ColorRect { Color = Colors.Red, MouseFilter = MouseFilter.Ignore }; top.AddChild(overlay); overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); root.AddChild(top);
        var rootDown = 0; var topDown = 0; var rootWheel = 0; var topWheel = 0; var rootUp = 0; var topUp = 0; var topMotionCount = 0; var topMotionMask = MouseButtonMask.None;
        root.WindowInput += e => { Check(e is not InputEventFromWindow tagged || tagged.WindowID == root.GetWindowID(), "Native input retains its actual source identity."); if (e is InputEventMouseButton b) { if (b.ButtonIndex == MouseButton.Left) { if (b.Pressed) rootDown++; else rootUp++; } else if (b.Pressed) rootWheel++; } };
        top.WindowInput += e => { Check(e is not InputEventFromWindow tagged || tagged.WindowID == top.GetWindowID(), "Native child input retains its actual source identity."); if (e is InputEventMouseMotion motion) { topMotionCount++; topMotionMask = motion.ButtonMask; } if (e is InputEventMouseButton b) { if (b.ButtonIndex == MouseButton.Left) { if (b.Pressed) topDown++; else topUp++; } else if (b.Pressed) topWheel++; } };
        Process? client = null; Task<string?>? command = null; Task<string>? errors = null;
        var unsupported = false; var frames = 0; var measuring = false; var complete = false; var clientDone = false; var post = 0; long before = 0, idle = 0, active = 0; Vector2[] polygon = [];
        using var texture = top.GetTexture(); var captured = false;
        void Reply()
        {
            var position = DisplayServer.MouseGetPosition();
            client!.StandardInput.WriteLine($"{top.Size.X}|{top.Size.Y}|{position.X}|{position.Y}|{rootDown}|{topDown}|{rootWheel}|{topWheel}|{rootUp}|{topUp}|{top.Position.X}|{top.Position.Y}|{(Input.IsMouseButtonPressed(MouseButton.Left) ? 1 : 0)}|{(int)topMotionMask}|{topMotionCount}"); client.StandardInput.Flush();
        }
        void Command(string text)
        {
            switch (text)
            {
                case "sample": break;
                case "polygon": top.MousePassthroughPolygon = polygon; break;
                case "bow":
                    var bowSize = top.Size;
                    top.MousePassthroughPolygon = [Vector2.Zero, new(bowSize.X, bowSize.Y), new(bowSize.X, 0), new(0, bowSize.Y)]; break;
                case "pass": top.MousePassthrough = true; Check(DisplayServer.WindowGetFlag(WindowFlag.MousePassthrough, top.GetWindowID()), "The native service retains the accepted full passthrough flag."); break;
                case "restore": top.MousePassthrough = false; break;
                case "clear": top.MousePassthroughPolygon = []; break;
                case "degenerate": top.MousePassthroughPolygon = [Vector2.Zero, Vector2.One]; break;
                case "service": top.MousePassthroughPolygon = []; DisplayServer.WindowSetMousePassthrough(polygon, top.GetWindowID()); Check(top.MousePassthroughPolygon.Length == 0, "A direct service mask does not rewrite the authored polygon."); break;
                case "reopen":
                    top.MousePassthroughPolygon = polygon; var old = top.GetWindowID(); top.Hide(); top.Show(); Check(top.GetWindowID() != old && top.MousePassthroughPolygon.AsSpan().SequenceEqual(polygon), "Reopening replaces native identity and reapplies the authored input polygon."); Reject<ArgumentException>(() => DisplayServer.WindowSetMousePassthrough([], old)); break;
                case "done":
                    var pointer = DisplayServer.MouseGetPosition(); var size = top.Size;
                    top.MousePassthroughPolygon = pointer.X < size.X / 2
                        ? [new(size.X / 2, 0), new(size.X, 0), new(size.X, size.Y), new(size.X / 2, size.Y)]
                        : polygon;
                    complete = true; Console.WriteLine("Native input: compositor client completed"); break;
                default: throw new InvalidOperationException("Unknown native input-region command");
            }
            Reply();
        }
        root.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The actual requested renderer executes.");
            Reject<ArgumentException>(() => DisplayServer.WindowSetMousePassthrough([], 99999));
            Task.Run(() => Reject<InvalidOperationException>(() => DisplayServer.WindowSetMousePassthrough([]))).GetAwaiter().GetResult();
            if (DisplayServer.GetName() == "Wayland")
            {
                unsupported = true;
                Reject<NotSupportedException>(() => root.MousePassthrough = true);
                Reject<NotSupportedException>(() => root.MousePassthroughPolygon = [Vector2.Zero, Vector2.One]);
                Reject<NotSupportedException>(() => DisplayServer.WindowSetMousePassthrough([Vector2.Zero]));
                Check(!root.MousePassthrough && root.MousePassthroughPolygon.Length == 0 && !DisplayServer.WindowGetFlag(WindowFlag.MousePassthrough), "Unsupported native edits preserve authored and service state.");
                root.MousePassthrough = false; DisplayServer.WindowSetMousePassthrough([]);
                root.Tree!.Quit(); return;
            }
            Check(DisplayServer.GetName() == "X11", "The physical routing fixture uses the X11 native adapter.");
            DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled);
            root.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96 && post < 160); if (measuring) before = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 80) top.MousePassthrough = (post & 1) == 0;
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (client is not null && !captured && top.MousePassthroughPolygon.Length != 0)
            {
                captured = true; using var image = texture.GetImage()!; var pixel = image.GetPixel(image.Width * 3 / 4, image.Height / 2); Check(pixel.R > .9f && pixel.B < .1f, "Rendered overlay pixels remain outside its input region.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, "native-input-region-" + backend + ".png")); }
            }
            if (complete && ++post == 160) Check(idle == 0 && active == 0, $"Native region 64 idle/64 active owner-frame managed bytes {idle}/{active}.");
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) =>
            {
                frames++;
                if (frames == 12) { top.Size = new(600, 400); top.Position = root.Position; top.Show(); DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled, top.GetWindowID()); }
                if (frames == 30)
                {
                    Check(top.GetWindowID() > 0 && top.Size == new Vector2i(600, 400), "The transient native overlay has its requested client extent."); var size = top.Size;
                    polygon = [Vector2.Zero, new(size.X / 2, 0), new(size.X / 2, size.Y), new(0, size.Y)];
                    Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetMousePassthrough([new(float.NaN, 0)], top.GetWindowID())); Check(top.MousePassthroughPolygon.Length == 0, "Invalid native polygon changes do not publish authored state.");
                    Reject<ArgumentOutOfRangeException>(() => top.MousePassthroughPolygon = [new(40000, 0)]);
                    top.MousePassthrough = true;
                    Reject<ArgumentOutOfRangeException>(() => top.MousePassthroughPolygon = [new(40000, 0), new(40001, 0), new(40000, 1)]);
                    top.MousePassthrough = false;
                    Check(top.MousePassthroughPolygon.Length == 0, "Native integer limits apply to overridden and degenerate source polygons without publication.");
                    var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(System.IO.Path.GetFullPath("tests/WindowInputRegionClient/probe.py"));
                    client = Process.Start(start)!; command = client.StandardOutput.ReadLineAsync(); errors = client.StandardError.ReadToEndAsync();
                }
                if (command?.IsCompleted == true) { var text = command.GetAwaiter().GetResult(); if (text is not null) Command(text); command = text is null ? null : client!.StandardOutput.ReadLineAsync(); }
                if (!clientDone && client?.HasExited == true) { Console.Write(errors!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && complete, "Independent kernel input/compositor client completes."); clientDone = true; }
                if (clientDone && post >= 160) root.Tree!.Quit();
                if (Stopwatch.GetElapsedTime(started).TotalSeconds > 150) throw new TimeoutException("Native input-region routing timed out.");
            }, root) == 0, "Native input-region host closes.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        if (unsupported)
        {
            Reject<NotSupportedException>(() => Engine.Run(new Window { MousePassthrough = true }));
            Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Unsupported startup retires native owners.");
            Console.WriteLine($"PASS {_checks} explicit Wayland capability/failure/rollback contracts on {backend}."); return;
        }
        var fresh = new Window { Size = new(200, 140), MousePassthrough = true, MousePassthroughPolygon = [Vector2.Zero, new(50, 0), new(0, 50)] };
        fresh.Ready += _ => { Check(DisplayServer.WindowGetFlag(WindowFlag.MousePassthrough), "A fresh native display applies preconfigured passthrough."); fresh.MousePassthrough = false; Check(!DisplayServer.WindowGetFlag(WindowFlag.MousePassthrough), "A fresh native display restores its configured polygon."); fresh.Tree!.Quit(); };
        Check(Engine.Run(fresh) == 0, "Native input-region resources survive balanced display reopening.");
        Console.WriteLine($"PASS {_checks} native input-region/source/lifetime contracts on {backend}; 64 idle/64 active {idle}/{active} managed bytes.");
    }
}
