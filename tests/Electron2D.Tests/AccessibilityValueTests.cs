using System.Diagnostics;
using Path = System.IO.Path;
using Electron2D;
using ActionKind = Electron2D.AccessibilityServer.AccessibilityAction;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;

internal static class AccessibilityValueTests
{
    private static int _checks;
    private static void Check(bool condition, string message) { _checks++; if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Numeric Accessibility Probe");
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(360, 280) };
        var slider = new HSlider { Name = "Slider", AccessibilityName = "Numeric slider", MinValue = -10, MaxValue = 100, Step = .5, Page = 10, Value = 20, Position = new(12, 12), Size = new(240, 28) };
        var peer = new VSlider { AccessibilityName = "Shared slider", Position = new(270, 12), Size = new(24, 120) };
        var progress = new ProgressBar { AccessibilityName = "Progress", ShowPercentage = false, MinValue = -10, MaxValue = 100, Position = new(12, 54), Size = new(240, 28) };
        var textureProgress = new TextureProgressBar { AccessibilityName = "Texture progress", Position = new(300, 12) };
        var horizontal = new HScrollBar { AccessibilityName = "Horizontal scroll", Position = new(12, 96), Size = new(240, 20), Page = 20, Value = 10, CustomStep = 7 };
        var vertical = new VScrollBar { AccessibilityName = "Vertical scroll", Position = new(310, 54), Size = new(20, 120) };
        var spin = new SpinBox { AccessibilityName = "Numeric spin", Position = new(12, 132), Size = new(180, 36), Step = .25, Value = 3, Prefix = "$", Suffix = "kg", CustomArrowStep = 9 };
        var retiring = new HSlider { AccessibilityName = "Retiring slider", Position = new(12, 180), Size = new(240, 24) };
        foreach (var node in new Control[] { slider, peer, progress, textureProgress, horizontal, vertical, spin, retiring }) window.AddChild(node);
        slider.Share(peer);
        RID manual = default;
        var notifications = 0; var peerNotifications = 0; var policies = 0; var spinFocuses = 0;
        spin.GetLineEdit().FocusEntered += () => spinFocuses++;
        slider.ValueChanged += value => { notifications++; progress.Value = value; if (value == 44) slider.Value = 42; };
        peer.ValueChanged += _ => peerNotifications++;
        var policy = new Button("Policy") { AccessibilityName = "Policy", Position = new(12, 224), Size = new(110, 32) }; window.AddChild(policy);
        policy.Pressed += () =>
        {
            policies++;
            switch (policies)
            {
                case 1: slider.Editable = spin.Editable = false; break;
                case 2: slider.Editable = spin.Editable = true; break;
                case 3: slider.Hide(); break;
                case 4: slider.Show(); var count = notifications; slider.SetValueNoSignal(32); Check(notifications == count, "Silent writes publish state without ValueChanged."); break;
                case 5: window.RemoveChild(retiring); retiring.Dispose(); break;
                case 6: AccessibilityServer.UpdateSetFlag(manual, Flag.Disabled, true); break;
                case 7: AccessibilityServer.UpdateSetFlag(manual, Flag.Disabled, false); AccessibilityServer.UpdateSetFlag(manual, Flag.ReadOnly, true); break;
                case 8: AccessibilityServer.UpdateSetFlag(manual, Flag.ReadOnly, false); AccessibilityServer.UpdateSetNumStep(manual, 0); AccessibilityServer.UpdateSetNumJump(manual, 0); break;
            }
        };
        Process? client = null; Task<string>? output = null, errors = null; RID id = default;
        var complete = false; var frame = 0; var post = 0; var measured = false; long startBytes = 0, idle = 0, active = 0;
        using var texture = window.GetTexture();
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Actual renderer and real native bridge.");
            id = slider.GetAccessibilityElement();
            Reject<KeyNotFoundException>(() => AccessibilityServer.UpdateSetNumValue(default, 1));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetNumRange(id, 2, 1));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetNumRange(id, double.NaN, 1));
            foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, -1d })
            {
                Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetNumStep(id, invalid));
                Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetNumJump(id, invalid));
            }
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateAddAction(id, ActionKind.SetValue, () => { }));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateAddAction(id, ActionKind.Click, (double _) => { }));
            Reject<ArgumentNullException>(() => AccessibilityServer.UpdateAddAction(id, ActionKind.SetValue, (Action<double>)null!));
            Task.Run(() => Reject<InvalidOperationException>(() => AccessibilityServer.UpdateSetNumStep(id, 1))).GetAwaiter().GetResult();
            manual = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.SpinButton);
            AccessibilityServer.UpdateSetName(manual, "Custom numeric");
            AccessibilityServer.UpdateSetBounds(manual, new(260, 220, 24, 24));
            AccessibilityServer.UpdateSetNumRange(manual, -5, 50); AccessibilityServer.UpdateSetNumStep(manual, .25); AccessibilityServer.UpdateSetNumJump(manual, 8);
            AccessibilityServer.UpdateSetNumValue(manual, double.NaN);
            AccessibilityServer.UpdateAddAction(manual, ActionKind.SetValue, value => AccessibilityServer.UpdateSetNumValue(manual, Math.Clamp(value, -5, 50)));
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/values.py"));
            client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); errors = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measured = complete && post >= 16 && (post < 80 || post >= 96);
                if (measured) startBytes = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 2) AccessibilityServer.UpdateSetNumValue(manual, double.NaN);
                if (complete && post >= 80) slider.Value = (post & 1) == 0 ? 20 : 60;
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measured) { var bytes = GC.GetAllocatedBytesForCurrentThread() - startBytes; if (post < 80) idle += bytes; else active += bytes; }
            if (!complete) return;
            post++;
            if (post == 8)
            {
                var folder = Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR");
                if (folder is not null) { Directory.CreateDirectory(folder); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(folder, "numeric-" + backend + ".png")); }
            }
            if (post == 160) { Check(idle == 0 && active == 0, $"Numeric prepared frames allocate idle={idle}, active={active} owner bytes."); window.Tree!.Quit(); }
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) =>
            {
                frame++;
                if (!complete && client?.HasExited == true)
                {
                    var text = output!.GetAwaiter().GetResult(); var error = errors!.GetAwaiter().GetResult(); Console.Write(text); Console.Write(error);
                    Check(client.ExitCode == 0, "Independent numeric client failed.");
                    Check(notifications >= 6 && peerNotifications >= 6 && policies == 8 && slider.Value == 32 && peer.Value == 32, "Native writes, shared peers, silent update and reentry use ordinary range policy.");
                    Check(progress.Value == 42 && horizontal.Value == 12 && spin.Value == 3.25 && spinFocuses == 1, "Native actions reached ordinary range consumers.");
                    complete = true;
                }
                if (Stopwatch.GetElapsedTime(started).TotalSeconds > 40) throw new TimeoutException("Numeric native client timed out.");
            }, window) == 0, "Numeric host completed.");
        }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw;
            if (client is { HasExited: false }) client.Kill(true); client?.Dispose();
        }
        Check(!AccessibilityServer.HasElement(id) && !AccessibilityServer.HasElement(manual), "Display retirement releases numeric callbacks and native registrations.");
        Failure(backend);
        Console.WriteLine($"PASS {_checks} numeric accessibility contracts on {backend}; {frame} frames, 64 idle/64 active, {idle}/{active} managed bytes.");
    }
    private static void Failure(string backend)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Numeric Failure Probe");
        var window = new Window { Size = new(120, 80) }; Process? client = null; Task<string>? output = null, errors = null; RID id = default;
        var expected = new InvalidOperationException("Numeric callback failure"); var failed = false;
        window.Ready += _ =>
        {
            id = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.SpinButton);
            AccessibilityServer.UpdateSetName(id, "Failing numeric"); AccessibilityServer.UpdateSetNumValue(id, 1);
            AccessibilityServer.UpdateAddAction(id, ActionKind.SetValue, (double _) => throw expected);
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/values.py")); start.ArgumentList.Add("--failure");
            client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); errors = client.StandardError.ReadToEndAsync();
        };
        var started = Stopwatch.GetTimestamp();
        try { Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Numeric failure probe timed out."); }, window); }
        catch (AggregateException error) when (error.Flatten().InnerExceptions.Any(item => ReferenceEquals(item, expected))) { failed = true; }
        finally
        {
            if (client is not null)
            {
                if (!client.WaitForExit(5000)) client.Kill(true);
                Console.Write(output!.GetAwaiter().GetResult()); Console.Write(errors!.GetAwaiter().GetResult());
                Check(client.ExitCode == 0, "Native numeric callback failure returned a wire error."); client.Dispose();
            }
        }
        Check(failed && !AccessibilityServer.HasElement(id), "The original numeric callback exception aggregates on the owner and retires its identity.");
        var reopened = new Window { Size = new(120, 80) };
        reopened.Ready += _ =>
        {
            Check(AccessibilityServer.IsSupported() && !AccessibilityServer.HasElement(id), "Native numeric bridge reopens with no old registrations.");
            reopened.Tree!.Quit();
        };
        Check(Engine.Run(reopened) == 0, "Reopened numeric display completes.");
    }

}
