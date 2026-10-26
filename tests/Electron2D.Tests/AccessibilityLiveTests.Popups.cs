using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;

internal static partial class AccessibilityLiveTests
{
    private static void RunPopupSurfaces(string backend)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Native Live Probe"); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(620, 360), GUIEmbedSubwindows = false };
        var line = new LineEdit { AccessibilityName = "Native line", Text = "Native text", Position = new(10, 10), Size = new(300, 36) };
        var choices = new OptionButton { AccessibilityName = "Native choices", Position = new(10, 60), Size = new(300, 36) }; choices.AddItem("Native first", 10); choices.AddItem("Native second", 20);
        var next = new Button("Finish native popups") { Position = new(10, 260), Size = new(300, 36) }; var selected = 0; var changes = 0; var complete = false; var captured = false;
        choices.ItemSelected += _ => selected++; line.TextChanged += _ => changes++;
        foreach (var control in new Control[] { line, choices, next }) window.AddChild(control);
        var fieldMenu = line.GetMenu(); fieldMenu.Title = "Native context"; var choiceMenu = choices.GetPopup(); choiceMenu.Title = "Native dropdown";
        foreach (var popup in new[] { fieldMenu, choiceMenu }) { popup.Transparent = false; popup.TransparentBG = false; }
        next.Pressed += () => { Check(line.Text == "" && changes == 1 && choices.Selected == 1 && selected == 1, "Native popup callbacks mutate real source state once."); complete = true; };
        Process? client = null; Task<string>? output = null, error = null;
        window.Ready += _ =>
        {
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/live.py")); start.ArgumentList.Add("--native"); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (captured || !choiceMenu.Visible) return;
            Check(choiceMenu.GetWindowID() >= 0 && choiceMenu.GetWindowID() != window.GetWindowID(), "Dropdown owns a distinct real native surface.");
            captured = true;
            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture)
            { Directory.CreateDirectory(capture); using var texture = choiceMenu.GetTexture(); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(capture, "live-native-popup-" + backend + ".png")); }
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) => { if (client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && complete && captured, "Independent client completes native popup surface execution."); window.Tree!.Quit(); } if (Stopwatch.GetElapsedTime(started).TotalSeconds > 60) throw new TimeoutException("Native popup surface client timed out."); }, window) == 0, "Native popup host closes.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        Console.WriteLine($"PASS {_checks} native popup surface contracts on {backend}.");
    }
}
