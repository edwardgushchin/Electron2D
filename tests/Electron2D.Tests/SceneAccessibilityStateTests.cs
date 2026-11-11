using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;

internal static class SceneAccessibilityStateTests
{
    private sealed class Source : Button
    {
        internal int Updates { get; private set; }

        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationAccessibilityUpdate) Updates++;
        }
    }

    internal static void Run(string profile)
    {
        Headless();
        if (profile == "managed") return;
        Check(profile is "always" or "auto-on" or "auto-off" or "disabled" or "dummy" or "unavailable",
            "Select a documented native accessibility policy.");
        var automatic = profile is "always" or "auto-on";
        var supported = automatic || profile == "auto-off";
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, profile switch
        {
            "disabled" => AccessibilitySupport.Disabled,
            "auto-on" or "auto-off" => AccessibilitySupport.Auto,
            _ => AccessibilitySupport.Always
        });
        ProjectSettings.Set(ProjectSettings.AccessibilityDriver, profile == "dummy" ? "dummy" : "atspi");
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Scene Accessibility Probe");
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 120;
        Native(profile, supported, automatic);
    }

    private static void Headless()
    {
        using var tree = new SceneTree(new Node());
        Check(!tree.IsAccessibilitySupported() && !tree.IsAccessibilityEnabled(), "A headless tree has no native output.");
        Task.Run(() =>
        {
            Reject<InvalidOperationException>(() => tree.IsAccessibilitySupported());
            Reject<InvalidOperationException>(() => tree.IsAccessibilityEnabled());
        }).GetAwaiter().GetResult();
        tree.Dispose();
        Reject<ObjectDisposedException>(() => tree.IsAccessibilitySupported());
        Reject<ObjectDisposedException>(() => tree.IsAccessibilityEnabled());
        Console.WriteLine("PASS headless, owner and retirement guards.");
    }

    private static void Native(string profile, bool supported, bool automatic)
    {
        var window = new Window { Title = "Scene accessibility", Size = new(320, 160) };
        var button = new Source { Text = "Automatic scene button", Position = new(12, 12), Size = new(180, 44) };
        window.AddChild(button);
        SceneTree? tree = null;
        Process? client = null;
        Task<string>? output = null;
        RID manual = default;
        var complete = false;
        var started = Stopwatch.GetTimestamp();

        window.Ready += _ =>
        {
            tree = window.Tree!;
            VerifyPolicy(tree);
            using var unrelated = new SceneTree(new Node());
            Check(!unrelated.IsAccessibilitySupported() && !unrelated.IsAccessibilityEnabled(),
                "An independent tree cannot borrow the running window's bridge.");
            if (profile == "auto-off")
            {
                var windowID = window.GetWindowID();
                manual = AccessibilityServer.CreateElement(windowID, Role.Button);
                AccessibilityServer.UpdateSetName(manual, "Manual button");
                AccessibilityServer.UpdateAddChild(AccessibilityServer.GetWindowRoot(windowID), manual);
                VerifyPolicy(tree);
            }
            ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Disabled);
            tree.Paused = true;
            VerifyPolicy(tree);
        };

        try
        {
            Check(Engine.Run((_, frame) =>
            {
                Check(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(20), "Timed out waiting for the independent AT-SPI client.");
                if (frame < 5) return;
                VerifyPolicy(tree!);
                Check(automatic ? button.Updates > 0 : button.Updates == 0,
                    "The paused tree's semantic scheduling follows its captured output policy.");
                if (supported)
                {
                    if (client is null)
                    {
                        var launch = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true };
                        launch.ArgumentList.Add("-B");
                        launch.ArgumentList.Add(System.IO.Path.GetFullPath("tests/AccessibilityClient/scene_tree_state.py"));
                        launch.ArgumentList.Add(automatic ? "automatic" : "manual");
                        client = Process.Start(launch) ?? throw new InvalidOperationException("Unable to launch the native client.");
                        output = client.StandardOutput.ReadToEndAsync();
                        return;
                    }
                    if (!client.HasExited || output?.IsCompletedSuccessfully != true) return;
                    Console.Write(output.Result);
                    Check(client.ExitCode == 0, "An independent AT-SPI client sees the expected element.");
                }
                complete = true;
                tree!.Quit();
            }, window) == 0 && complete, "The native policy probe completed.");
        }
        finally
        {
            if (client is { HasExited: false }) { client.Kill(true); client.WaitForExit(2000); }
            client?.Dispose();
            if (!window.IsDisposed) window.Dispose();
        }

        Check(!AccessibilityServer.IsSupported() && (!manual.IsValid() || !AccessibilityServer.HasElement(manual)),
            "The display teardown retires its bridge and explicit elements.");
        Reject<ObjectDisposedException>(() => tree!.IsAccessibilitySupported());
        Reject<ObjectDisposedException>(() => tree!.IsAccessibilityEnabled());
        Console.WriteLine($"PASS {profile}: supported={supported}, automatic={automatic}, scene updates={button.Updates}.");

        void VerifyPolicy(SceneTree current)
        {
            Check(current.IsAccessibilitySupported() == supported && current.IsAccessibilityEnabled() == automatic,
                $"Expected native support={supported} and automatic scene output={automatic} for {profile}.");
        }
    }
}
