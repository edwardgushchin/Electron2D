using System.Diagnostics;
using Electron2D;
using SDL3;

internal static class PortalColorPickerTests
{
    internal static void Run(string profile, string[] args)
    {
        Reject<InvalidOperationException>(() => DisplayServer.ColorPicker((_, _) => { }));
        if (profile == "unavailable")
        {
            using var display = DisplayServer.Open("Unavailable color portal", new(64, 64), hidden: true);
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.NativeColorPicker), "This session has no native color picker.");
            Check(!DisplayServer.ColorPicker((_, _) => throw new InvalidOperationException("Unexpected color callback.")), "An unavailable picker refuses to start.");
            Reject<ArgumentNullException>(() => DisplayServer.ColorPicker(null!));
            Console.WriteLine("Unsupported native color picker passed.");
            return;
        }

        var fixture = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_WAYLAND_PRIVATE_DISPLAY");
        Check(profile == "wayland" && OperatingSystem.IsLinux() && !string.IsNullOrEmpty(fixture) &&
            fixture == Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") && args.Length == 1,
            "Use the private Wayland portal fixture and provide its input actor.");
        Scene(args[0]);
    }

    private static void Scene(string actorPath)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 30;
        var root = new Window { Title = "Electron2D Wayland capture", Size = new(480, 320), Borderless = true, GUIEmbedSubwindows = false, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        root.AddChild(new ColorRect { Position = new(68, 66), Size = new(104, 60), Color = Colors.Red, MouseFilter = MouseFilter.Ignore });
        var picker = new ColorPicker { Position = new(212, 10), Color = Colors.Blue };
        root.AddChild(picker);
        var pick = (Control)ColorPickerTests.Find(picker, "Pick");
        var changes = 0;
        picker.ColorChanged += _ => changes++;
        var button = new ColorPickerButton { Position = new(355, 12), Size = new(100, 40), Color = Colors.Blue };
        root.AddChild(button);
        var popup = button.GetPopup();
        popup.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
        var buttonPick = (Control)ColorPickerTests.Find(button.GetPicker(), "Pick");
        var buttonChanges = 0;
        button.ColorChanged += _ => buttonChanges++;
        var owner = Environment.CurrentManagedThreadId;
        var target = new Vector2i(920, 96);
        var external = new Vector2i(120, 96);
        var clock = Stopwatch.StartNew();
        var selected = 0;
        var canceled = 0;
        var phase = 0;
        var frames = 0;
        Process? actor = null;
        root.Ready += _ => RenderingServer.FramePostDraw += Drawn;

        void Accept(bool ok, Color color)
        {
            Check(ok && Environment.CurrentManagedThreadId == owner && color == Colors.Red, "The portal supplies the opaque rendered marker on the display owner thread.");
            selected++;
        }

        void Drawn()
        {
            Check(clock.Elapsed < TimeSpan.FromSeconds(24), $"The color picker did not complete phase {phase}.");
            if (++frames < 10)
                return;
            if (actor is { HasExited: true })
            {
                Check(actor.ExitCode == 0, "The portal input actor completed.");
                actor.Dispose();
                actor = null;
            }
            switch (phase)
            {
                case 0 when DisplayServer.ScreenGetPixel(target) == Colors.Red:
                    Check(DisplayServer.HasFeature(DisplayServer.Feature.NativeColorPicker), "The native screenshot portal is available.");
                    Reject<ArgumentNullException>(() => DisplayServer.ColorPicker(null!));
                    Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.ColorPicker(Accept)).GetAwaiter().GetResult());
                    Check(DisplayServer.ColorPicker(Accept), "The first picker starts.");
                    Check(DisplayServer.ColorPicker((ok, color) =>
                    {
                        Check(!ok && color == Colors.Black && Environment.CurrentManagedThreadId == owner, "Cancellation completes on the owner without a color.");
                        canceled++;
                    }), "A second request has independent completion.");
                    actor = Act(actorPath, "pick", target);
                    phase++;
                    break;
                case 1 when actor is null && selected == 1:
                    actor = Act(actorPath, "cancel", target);
                    phase++;
                    break;
                case 2 when actor is null && canceled == 1:
                    if (!DisplayServer.ColorPicker(Accept)) break;
                    actor = Act(actorPath, "pick", target);
                    phase++;
                    break;
                case 3 when actor is null && selected == 2:
                    Check(DisplayServer.ScreenGetPixel(external) == Colors.Black, "The other output contains a known pixel outside the application.");
                    var center = pick.GetGlobalTransformWithCanvas() * (pick.Size / 2);
                    actor = Act(actorPath, "click", new(800 + (int)center.X, (int)center.Y));
                    phase++;
                    break;
                case 4 when actor is null:
                    actor = Act(actorPath, "pick", external);
                    phase++;
                    break;
                case 5 when actor is null && changes == 1:
                    Check(picker.Color == Colors.Black && picker.GetRecentPresets().Contains(Colors.Black), "The control commits the external portal color and its recent swatch.");
                    var buttonCenter = button.GetGlobalTransformWithCanvas() * (button.Size / 2);
                    actor = Act(actorPath, "click", new(800 + (int)buttonCenter.X, (int)buttonCenter.Y));
                    phase++;
                    break;
                case 6 when actor is null && popup.Visible:
                    QueuePopupClick(buttonPick);
                    actor = Act(actorPath, "pick", external);
                    phase++;
                    break;
                case 7 when actor is null && buttonChanges == 1:
                    Check(!popup.Visible && button.Color == Colors.Black && button.GetPicker().GetRecentPresets().Contains(Colors.Black), "The native popup releases input and its owning button commits the desktop color.");
                    Check(DisplayServer.ColorPicker((_, _) => throw new InvalidOperationException("A retired display must not deliver a color.")), "A final request starts before display shutdown.");
                    RenderingServer.FramePostDraw -= Drawn;
                    root.Tree!.Quit();
                    phase++;
                    break;
            }
        }

        try
        {
            Check(Engine.Run(root) == 0 && phase == 8 && selected == 2 && canceled == 1 && changes == 1 && buttonChanges == 1, "The native color portal scene retired successfully.");
            Reject<InvalidOperationException>(() => DisplayServer.ColorPicker((_, _) => { }));
            Console.WriteLine("Native portal color selection, cancellation, GUI sampling and retirement passed (Wayland).");
        }
        finally
        {
            if (actor is { HasExited: false }) actor.Kill();
            actor?.Dispose();
        }
    }

    private static Process Act(string executable, string action, Vector2i point)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false };
        start.ArgumentList.Add(action);
        start.ArgumentList.Add(point.X.ToString(System.Globalization.CultureInfo.InvariantCulture));
        start.ArgumentList.Add(point.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Process.Start(start) ?? throw new InvalidOperationException("Could not start the Wayland input actor.");
    }

    private static void QueuePopupClick(Control control)
    {
        var windows = SDL.GetWindows(out _) ?? throw new InvalidOperationException("The display has no native windows.");
        var handle = windows.Single(window => SDL.GetWindowParent(window) != 0);
        var id = SDL.GetWindowID(handle);
        var center = control.GetGlobalTransformWithCanvas() * (control.Size / 2);
        var motion = new SDL.Event { Type = (uint)SDL.EventType.MouseMotion };
        motion.Motion.WindowID = id;
        motion.Motion.X = center.X;
        motion.Motion.Y = center.Y;
        SDL.PushEvent(ref motion);
        var button = new SDL.Event { Type = (uint)SDL.EventType.MouseButtonDown };
        button.Button.WindowID = id;
        button.Button.X = center.X;
        button.Button.Y = center.Y;
        button.Button.Button = SDL.ButtonLeft;
        button.Button.Down = true;
        SDL.PushEvent(ref button);
        button.Type = (uint)SDL.EventType.MouseButtonUp;
        button.Button.Down = false;
        SDL.PushEvent(ref button);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
