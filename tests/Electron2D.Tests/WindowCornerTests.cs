using System.Runtime.InteropServices;
using Electron2D;
using Path = System.IO.Path;

internal static partial class WindowCornerTests
{
    internal static void Run(string profile)
    {
        Authoring();
        if (profile == "unsupported")
            Unsupported();
        else if (profile is "gpu" or "compatibility")
            Native(profile);
        else if (profile != "managed")
            throw new ArgumentException("Unknown window corner test profile.", nameof(profile));
    }

    private static void Authoring()
    {
        using var window = new Window();
        Check(!window.SharpCorners, "The default uses the system's corner policy.");
        window.SetFlag(WindowFlag.SharpCorners, true);
        Check(window.SharpCorners && window.GetFlag(WindowFlag.SharpCorners), "Property and flag share the authored request.");
        using var duplicate = (Window)window.Duplicate();
        using var scene = new PackedScene();
        scene.Pack(window);
        var file = Path.Combine(Path.GetTempPath(), "window-corners-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(scene, file);
            using var loaded = ResourceLoader.Load<PackedScene>(file, ResourceLoader.CacheMode.Ignore);
            using var restored = (Window)loaded.Instantiate();
            window.SharpCorners = false;
            Check(restored.SharpCorners && duplicate.SharpCorners && !window.GetFlag(WindowFlag.SharpCorners),
                "Saved scene and duplicate retain independent requests.");
        }
        finally
        {
            File.Delete(file);
        }
        window.Dispose();
        Reject<ObjectDisposedException>(() => _ = window.SharpCorners);
        Reject<ObjectDisposedException>(() => window.SharpCorners = true);
        Console.WriteLine("Window corner authoring, saved scene, duplication and disposal passed.");
    }

    private static void Unsupported()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using (var display = DisplayServer.Open("Unavailable corner policy", new(96, 64), hidden: true))
        {
            Reject<NotSupportedException>(() => DisplayServer.WindowGetFlag(WindowFlag.SharpCorners));
            Reject<NotSupportedException>(() => DisplayServer.WindowSetFlag(WindowFlag.SharpCorners, true));
            Reject<NotSupportedException>(() => DisplayServer.WindowSetFlag(WindowFlag.SharpCorners, false));
        }
        using var configured = new Window { Size = new(96, 64), SharpCorners = true };
        Reject<NotSupportedException>(() => Engine.Run(configured));
        Check(!DisplayServer.IsAvailable, "Rejected startup releases the native display.");
        using var root = new Window { Size = new(96, 64) };
        root.Ready += _ =>
        {
            Reject<NotSupportedException>(() => root.SharpCorners = true);
            Check(!root.SharpCorners, "A rejected native request preserves the authored value.");
            root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && !DisplayServer.IsAvailable, "The unsupported profile retires cleanly.");
        Console.WriteLine("Unavailable window corner policy rejected without publishing changes.");
    }

    private static void Native(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window
        {
            Title = "Electron2D root corners",
            Position = new(320, 240),
            Size = new(240, 160),
            SharpCorners = true
        };
        var child = new Window { Title = "Electron2D child corners", Position = new(620, 260), Size = new(160, 100) };
        var host = new SubViewport { Size = new(64, 64), GUIEmbedSubwindows = true };
        var embedded = new Window { Size = new(32, 32), SharpCorners = true };
        host.AddChild(embedded);
        root.AddChild(host);
        root.AddChild(child);
        var popup = new Popup { Size = new(72, 56), SharpCorners = true };
        root.AddChild(popup);
        var completed = false;

        root.Ready += _ => RenderingServer.FramePostDraw += CheckCorners;
        Check(Engine.Run(root) == 0 && completed && !DisplayServer.IsAvailable, "Window lifetimes retire after native corner checks.");
        Reject<InvalidOperationException>(() => DisplayServer.WindowGetFlag(WindowFlag.SharpCorners));
        using var fresh = DisplayServer.Open("Fresh corner policy", new(96, 64), hidden: true);
        Check(!DisplayServer.WindowGetFlag(WindowFlag.SharpCorners), "A fresh window uses its default corner policy.");
        Console.WriteLine($"Native per-window DWM corner policy passed ({backend}).");

        void CheckCorners()
        {
            RenderingServer.FramePostDraw -= CheckCorners;
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested renderer is active.");
            Observe(root, 1);
            Observe(child, 0);
            Check(embedded.IsEmbedded() && embedded.SharpCorners, "The embedded request stays authored.");

            var childID = child.GetWindowID();
            DisplayServer.WindowSetFlag(WindowFlag.SharpCorners, true, childID);
            Observe(child, 1);
            Check(!child.SharpCorners, "Direct native policy leaves scene authoring intact.");
            DisplayServer.WindowSetFlag(WindowFlag.SharpCorners, false, childID);
            Observe(child, 0);
            child.SharpCorners = true;
            Observe(child, 1);

            root.SharpCorners = false;
            Observe(root, 0);
            embedded.SharpCorners = false;
            embedded.SharpCorners = true;
            Observe(root, 0);
            Observe(child, 1);

            child.Hide();
            Reject<ArgumentException>(() => DisplayServer.WindowGetFlag(WindowFlag.SharpCorners, childID));
            child.Show();
            Check(child.GetWindowID() != childID && child.SharpCorners, "Child recreation retains the request and allocates a new window ID.");
            Observe(child, 1);

            var rounding = 2;
            Check(DwmSetWindowAttribute(Handle(child), 33, in rounding, sizeof(int)) == 0, "Independent DWM writer requests rounding.");
            Observe(child, 2);
            Check(child.SharpCorners, "External DWM changes leave the stored scene preference intact.");
            child.SharpCorners = false;
            Observe(child, 0);

            popup.PopupOnParent(new(16, 16, 72, 56));
            Observe(popup, 1);
            popup.Hide();
            Task.Run(() => Reject<InvalidOperationException>(() => root.SharpCorners = true)).GetAwaiter().GetResult();
            Task.Run(() => Reject<InvalidOperationException>(() => DisplayServer.WindowGetFlag(WindowFlag.SharpCorners))).GetAwaiter().GetResult();
            completed = true;
            root.Tree!.Quit();
        }
    }

    private static void Observe(Window window, int expected)
    {
        Check(DwmGetWindowAttribute(Handle(window), 33, out var actual, sizeof(int)) == 0 && actual == expected,
            $"DWM corner preference for {window.Title}: expected {expected}, observed {actual}.");
        Check(DisplayServer.WindowGetFlag(WindowFlag.SharpCorners, window.GetWindowID()) == (expected == 1),
            "The DisplayServer flag reads the current DWM preference.");
    }

    private static nint Handle(Window window) =>
        DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowID());

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    [LibraryImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DwmGetWindowAttribute(nint window, int attribute, out int value, uint size);

    [LibraryImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DwmSetWindowAttribute(nint window, int attribute, in int value, uint size);
}
