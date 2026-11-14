using System.Diagnostics;
using Electron2D;
using static MovieWriterTests;

internal static class WindowDrawabilityTests
{
    internal static void Run(string profile)
    {
        Reject<InvalidOperationException>(() => DisplayServer.WindowCanDraw());
        using (var detached = new Window()) Check(!detached.CanDraw(), "A detached window has no presentation host.");
        using (var window = new SubViewport { GUIEmbedSubwindows = true })
        {
            using var child = new Window();
            window.AddChild(child);
            using var tree = new SceneTree(window);
            Check(child.IsEmbedded() && child.CanDraw(), "An embedded window can compose into an attached viewport.");
            child.Hide();
            Check(!child.CanDraw(), "A hidden embedded window is unavailable.");
            tree.Dispose();
            Reject<ObjectDisposedException>(() => child.CanDraw());
        }

        using (DisplayServer.Open("Display-only drawability", new(160, 120)))
            Check(!DisplayServer.WindowCanDraw(), "A display-only host has no renderer.");

        var headless = profile == "headless";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, profile == "gpu" ? "gpu" : "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 60;
        using var root = new Window { Size = new(192, 144), GUIEmbedSubwindows = false };
        using var childWindow = new Window { Size = new(128, 96), Title = "Drawability child" };
        var canvas = new ColorRect { Size = new(192, 144), Color = Colors.Green };
        root.AddChild(canvas);
        childWindow.AddChild(new ColorRect { Size = new(128, 96), Color = Colors.Blue });
        if (!headless) root.AddChild(childWindow);
        var phases = 0;
        var elapsed = new Stopwatch();
        root.Ready += _ =>
        {
            var renderer = RenderingServer.Service!;
            Check(root.CanDraw() == !headless && DisplayServer.WindowCanDraw() == !headless, "The native screen and root share eligibility.");
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowCanDraw(-1));
            Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => DisplayServer.WindowCanDraw());
                Reject<InvalidOperationException>(() => root.CanDraw());
            }).GetAwaiter().GetResult();
            if (headless)
            {
                RenderingServer.ForceDraw(false);
                using var image = root.GetTexture().GetImage();
                Check(image?.GetPixel(8, 8).IsEqualApprox(Colors.Green) == true, "Headless software still draws a viewport texture.");
                root.Tree!.Quit();
                return;
            }

            var id = childWindow.GetWindowID();
            Check(id > 0 && childWindow.CanDraw() && DisplayServer.WindowCanDraw(id), "A shown native child is drawable.");
            RenderingServer.RenderLoopEnabled = false;
            Check(!root.CanDraw() && !DisplayServer.WindowCanDraw(id), "Automatic rendering gates native drawability.");
            RenderingServer.ForceDraw(false);
            Check(!renderer.WasNativeWindowPresented(0), "Explicit texture drawing can avoid presentation.");
            RenderingServer.RenderLoopEnabled = true;
            childWindow.Hide();
            Check(!childWindow.CanDraw(), "Hiding a native child retires its presentation.");
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowCanDraw(id));
            elapsed.Start();
            root.Tree!.ProcessFrameStarted += _ =>
            {
                Check(elapsed.Elapsed < TimeSpan.FromSeconds(12), "Native minimize/restore did not complete.");
                switch (phases)
                {
                    case 0:
                        if (!renderer.WasNativeWindowPresented(0)) return;
                        root.Mode = WindowMode.Minimized;
                        phases++;
                        break;
                    case 1:
                        if (root.Mode != WindowMode.Minimized) return;
                        Check(!root.CanDraw() && !DisplayServer.WindowCanDraw(), "Native minimization suspends both queries.");
                        canvas.Color = Colors.Red;
                        var device = RenderingServer.GetRenderingDevice();
                        if (device is not null)
                        {
                            device.DrawListBeginForScreen(clearColor: Colors.Blue);
                            device.DrawListEnd();
                        }
                        RenderingServer.ForceDraw();
                        Check(!renderer.WasNativeWindowPresented(0), "A minimized window does not submit presentation.");
                        using (var image = root.GetTexture().GetImage())
                            Check(image?.GetPixel(8, 8).IsEqualApprox(Colors.Red) == true, "Minimized viewport renders its new contents.");
                        Check(device is null || device.HasPendingScreens, "Minimization preserves pending device-screen output.");
                        root.Mode = WindowMode.Windowed;
                        phases++;
                        break;
                    case 2:
                        if (!root.CanDraw() || !renderer.WasNativeWindowPresented(0)) return;
                        Check(RenderingServer.GetRenderingDevice()?.HasPendingScreens != true, "Restored presentation consumes pending device output.");
                        phases++;
                        root.Tree!.Quit();
                        break;
                }
            };
        };

        Check(Engine.Run(root) == 0 && (headless || phases == 3), "Window drawability follows the real window lifecycle.");
        Reject<InvalidOperationException>(() => DisplayServer.WindowCanDraw());
        Console.WriteLine($"Window drawability passed ({profile}).");
    }
}
