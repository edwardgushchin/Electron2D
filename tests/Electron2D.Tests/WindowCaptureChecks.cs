using Electron2D;
using System.Diagnostics;

internal static class WindowCaptureChecks
{
    private static int _checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { _checks++; return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    private static void Authoring()
    {
        using var window = new Window();
        Check(!window.ExcludeFromCapture && !window.GetFlag(WindowFlag.ExcludeFromCapture), "Capture defaults.");
        window.ExcludeFromCapture = true;
        Check(window.GetFlag(WindowFlag.ExcludeFromCapture), "Property and flag share storage.");
        using var packed = new PackedScene(); packed.Pack(window);
        using var restored = (Window)packed.Instantiate();
        using var duplicate = (Window)window.Duplicate();
        Check(restored.ExcludeFromCapture && duplicate.ExcludeFromCapture, "Stored scene and hierarchy duplication retain the policy.");
        window.SetFlag(WindowFlag.ExcludeFromCapture, false);
        Check(!window.ExcludeFromCapture && restored.ExcludeFromCapture, "Authored copies are independent.");
        Reject<ArgumentOutOfRangeException>(() => window.SetFlag(WindowFlag.Max, true));
        window.Dispose();
        Reject<ObjectDisposedException>(() => _ = window.ExcludeFromCapture);
        Reject<ObjectDisposedException>(() => window.ExcludeFromCapture = true);
    }

    internal static void Unavailable()
    {
        Authoring();
        using (var display = DisplayServer.Open("Unavailable capture policy", new(64, 64), hidden: true))
        {
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.ScreenExcludeFromCapture), "Unavailable producer is not advertised.");
            Reject<NotSupportedException>(() => DisplayServer.WindowSetFlag(WindowFlag.ExcludeFromCapture, true));
            Reject<NotSupportedException>(() => DisplayServer.WindowSetFlag(WindowFlag.ExcludeFromCapture, false));
            Reject<NotSupportedException>(() => DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture));
        }
        var root = new Window { Size = new(64, 64), ExcludeFromCapture = true };
        Reject<NotSupportedException>(() => Engine.Run(root));
        Check(!DisplayServer.IsAvailable, "Rejected startup releases the native display.");
        root.Dispose();
        var active = new Window { Size = new(64, 64) };
        active.Ready += _ =>
        {
            Reject<NotSupportedException>(() => active.ExcludeFromCapture = true);
            Check(!active.ExcludeFromCapture, "Rejected native mutation preserves authored state.");
            active.Tree!.Quit();
        };
        Engine.Run(active);
        Console.WriteLine($"Window capture unavailable: {_checks} checks passed.");
    }

    internal static void Run(string backend, string probe)
    {
        Authoring();
        var oldMethod = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Title = "Electron2D capture root", Size = new(192, 128), Borderless = true, ExcludeFromCapture = true };
        var child = new Window { Title = "Electron2D capture child", Size = new(96, 64), Position = new(500, 300), Borderless = true };
        var embeddedHost = new SubViewport { Size = new(48, 48), GUIEmbedSubwindows = true };
        var embedded = new Window { Size = new(24, 24), ExcludeFromCapture = true };
        embeddedHost.AddChild(embedded); root.AddChild(embeddedHost); root.AddChild(child);
        root.AddChild(new ColorRect { Size = new(192, 128), Color = Colors.Red });
        child.AddChild(new ColorRect { Size = new(96, 64), Color = Colors.Red });
        var popup = new Popup { Size = new(64, 48), ExcludeFromCapture = true };
        popup.AddChild(new ColorRect { Size = new(64, 48), Color = Colors.Red }); root.AddChild(popup);
        var frames = 0; var phase = 0; var retired = -1;
        void Captured()
        {
            if (++frames < 3) return;
            frames = 0;
            Check(embedded.IsEmbedded() && embedded.ExcludeFromCapture, "Embedded request remains authored without mutating its host.");
            using (var image = root.GetTexture().GetImage()) Check(image is not null && image.GetPixel(10, 10).R > .95f, "Engine texture readback remains available.");
            if (phase == 0)
            {
                Check(DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture), "Preconfigured root policy executes before rendering.");
                Capture(root, true, probe); Capture(child, false, probe);
                root.ExcludeFromCapture = false; child.ExcludeFromCapture = true;
            }
            else if (phase == 1)
            {
                Capture(root, false, probe); Capture(child, true, probe);
                retired = child.GetWindowID(); child.Hide(); child.Show();
                Check(child.GetWindowID() != retired && child.ExcludeFromCapture, "Recreated child retains authored policy with a fresh native ID.");
                Reject<ArgumentException>(() => DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture, retired));
            }
            else if (phase == 2)
            {
                Capture(child, true, probe);
                child.ExcludeFromCapture = false;
                DisplayServer.WindowSetFlag(WindowFlag.ExcludeFromCapture, true, child.GetWindowID());
                Check(!child.ExcludeFromCapture && DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture, child.GetWindowID()), "Direct native requests do not rewrite scene authoring.");
                DisplayServer.WindowSetFlag(WindowFlag.ExcludeFromCapture, false, child.GetWindowID());
            }
            else if (phase == 3)
            {
                Capture(child, false, probe);
                popup.PopupOnParent(new(16, 16, 64, 48));
            }
            else
            {
                Check(DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture, popup.GetWindowID()), "Native popup applies its own capture policy.");
                Capture(popup, true, probe); popup.Hide();
                Check(Task.Run(() => { try { root.ExcludeFromCapture = true; return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult(), "Scene capture mutation enforces its owner.");
                Check(Task.Run(() => { try { DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture); return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult(), "Display capture observation enforces its owner.");
                Allocations(root);
                root.Tree!.Quit();
            }
            phase++;
        }
        root.Ready += _ =>
        {
            Check(DisplayServer.HasFeature(DisplayServer.Feature.ScreenExcludeFromCapture), "Native capture exclusion is available.");
            RenderingServer.FramePostDraw += Captured;
        };
        try { Engine.Run(root); }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldMethod);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback);
        }
        Check(root.IsDisposed && child.IsDisposed && !DisplayServer.IsAvailable, "All window and capture-policy ownership retires.");
        Reject<InvalidOperationException>(() => DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture));
        using (var display = DisplayServer.Open("Capture fresh lifetime", new(64, 64), hidden: true))
            Check(!DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture), "New native lifetime defaults to capturable.");
        Console.WriteLine($"Window capture {backend}: {_checks} checks passed.");
    }

    private static void Allocations(Window window)
    {
        foreach (var active in new[] { true, false })
        {
            for (var i = 0; i < 32; i++) Observe(active);
            var allBefore = GC.GetTotalAllocatedBytes(true);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) Observe(active);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            var allBytes = GC.GetTotalAllocatedBytes(true) - allBefore;
            Check(bytes == 0 && allBytes == 0, $"128 warmed capture cycles (active={active}) allocate {bytes}/{allBytes} owner/all-thread bytes.");
            Console.WriteLine($"128 warmed capture cycles active={active}: {bytes}/{allBytes} managed owner/all-thread bytes.");
        }
        void Observe(bool active)
        {
            if (active) window.ExcludeFromCapture = true;
            _ = window.ExcludeFromCapture;
            _ = DisplayServer.WindowGetFlag(WindowFlag.ExcludeFromCapture);
            _ = DisplayServer.HasFeature(DisplayServer.Feature.ScreenExcludeFromCapture);
            if (active) window.ExcludeFromCapture = false;
        }
    }

    private static void Capture(Window window, bool excluded, string probe)
    {
        var start = new ProcessStartInfo(probe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowID()).ToString());
        start.ArgumentList.Add(excluded ? "excluded" : "captured");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start capture probe.");
        if (!process.WaitForExit(15000)) { process.Kill(); throw new TimeoutException("Native window capture did not finish."); }
        var output = process.StandardOutput.ReadToEnd(); var errors = process.StandardError.ReadToEnd();
        Console.Write(output);
        Check(process.ExitCode == 0, $"Independent capture ({excluded}): {output} {errors}");
    }
}
