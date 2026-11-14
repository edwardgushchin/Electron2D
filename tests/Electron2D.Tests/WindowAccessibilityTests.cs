using System.Diagnostics;
using System.Globalization;
using Electron2D;
using static TextureArrayTests;

internal static class WindowAccessibilityTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Window Accessibility Probe");
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
        using var english = new Translation { Locale = "en" };
        using var french = new Translation { Locale = "fr" };
        english.AddMessage("Window name key", "Accessible workspace");
        english.AddMessage("Window description key", "Workspace description");
        french.AddMessage("Window name key", "Espace de travail");
        french.AddMessage("Window description key", "Description locale");
        var domain = TranslationServer.GetOrAddDomain("window-accessibility");
        domain.AddTranslation(english);
        domain.AddTranslation(french);
        using var root = new Window
        {
            Title = "Workspace caption", Size = new(240, 120), GUIEmbedSubwindows = true,
            TranslationDomain = "window-accessibility", AccessibilityName = "Window name key",
            AccessibilityDescription = "Window description key",
        };
        var embedded = new Window
        {
            Title = "Embedded caption", AccessibilityName = "Embedded dialog",
            AccessibilityDescription = "Embedded details", Size = new(64, 48), Borderless = true,
        };
        var native = new Window
        {
            Title = "Native caption", AccessibilityName = "Native dialog",
            AccessibilityDescription = "Native details", Size = new(64, 48), ForceNative = true,
        };
        root.AddChild(embedded);
        root.AddChild(native);

        Process? client = null;
        Task<string?>? command = null;
        var phase = 0;
        var startTime = Stopwatch.GetTimestamp();
        root.Ready += _ =>
        {
            Check(AccessibilityServer.IsSupported() && RenderingServer.GetCurrentRenderingMethod() == backend, "Actual native bridge and requested backend.");
            Check(embedded.IsEmbedded() && embedded.GetWindowID() < 0 && native.GetWindowID() > 0, "Separate embedded and native window hosts.");
            Check(root.Title == "Workspace caption" && root.AccessibilityName == "Window name key", "Authored name and ordinary title remain independent.");
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true };
            start.ArgumentList.Add("-B");
            start.ArgumentList.Add(System.IO.Path.GetFullPath("tests/AccessibilityClient/window_metadata.py"));
            client = Process.Start(start) ?? throw new InvalidOperationException("Unable to launch the AT-SPI client.");
            command = client.StandardOutput.ReadLineAsync();
        };
        try
        {
            Check(Engine.Run((_, _) =>
            {
                if (Stopwatch.GetElapsedTime(startTime) > TimeSpan.FromSeconds(20))
                    throw new TimeoutException("Window accessibility probe timed out.");
                if (command?.IsCompleted != true) return;
                switch (command.GetAwaiter().GetResult(), phase)
                {
                    case ("translate", 0):
                        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
                        break;
                    case ("clear", 1):
                        root.AccessibilityName = "";
                        root.AccessibilityDescription = "";
                        root.RemoveChild(embedded);
                        embedded.Dispose();
                        root.RemoveChild(native);
                        native.Dispose();
                        break;
                    case (null, 2):
                        if (!client!.HasExited) return;
                        Check(client.ExitCode == 0, "Independent AT-SPI assertions passed.");
                        root.Tree!.Quit();
                        return;
                    default:
                        throw new InvalidOperationException("Unexpected AT-SPI client response.");
                }
                phase++;
                command = client!.StandardOutput.ReadLineAsync();
            }, root) == 0 && phase == 2, "Window accessibility session completed.");
        }
        finally
        {
            if (client is { HasExited: false }) client.Kill(true);
            client?.Dispose();
        }
        Console.WriteLine("PASS native window metadata, locale change, fallback, clearing and child retirement.");
    }
}
