using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private sealed class ProfileLogger : Logger
    {
        internal readonly List<(string Message, TimeSpan Elapsed)> Reports = [];
        internal readonly Stopwatch Clock = new();

        protected override void OnLogMessage(string message, bool error)
        {
            if (!message.StartsWith("GPU profile", StringComparison.Ordinal)) return;
            Reports.Add((message, Clock.Elapsed));
            throw new InvalidOperationException("Profile logger failure fixture.");
        }
    }

    internal static void RunGPUFrameProfile(string mode)
    {
        var unsupported = mode == "unsupported";
        var active = mode is "setting" or "switch";
        var setting = ProjectSettings.PrintGPUProfile;
        Check(setting.Name == "debug/settings/stdout/print_gpu_profile" && !ProjectSettings.Get(setting), "The GPU profile is a built-in, disabled setting.");
        if (mode is "setting" or "editor" or "unsupported")
            ProjectSettings.SetFeatureOverride(setting, "windows", true);
        Engine.IsEditorHint = mode == "editor";
        Engine.MaxFPS = 60;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, unsupported ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.DisableSTDOUT, true);

        using var stdout = new StringWriter();
        using var logger = new ProfileLogger();
        var previousWriter = Console.Out;
        Console.SetOut(stdout);
        Electron2D.OS.AddLogger(logger);
        try
        {
            RunProfileWindow(logger, active ? 2 : 0, unsupported);
            Check(!stdout.ToString().Contains("GPU profile", StringComparison.Ordinal), "Physical stdout suppression preserves logger delivery.");
            if (mode == "setting")
            {
                ProjectSettings.ClearFeatureOverride(setting, "windows");
                logger.Reports.Clear();
                RunProfileWindow(logger, 0, unsupported: false);
            }
        }
        finally
        {
            Electron2D.OS.RemoveLogger(logger);
            Console.SetOut(previousWriter);
        }
        Console.WriteLine($"GPU frame profile passed ({mode}).");
    }

    private static void RunProfileWindow(ProfileLogger logger, int reports, bool unsupported)
    {
        using var root = new Window { Size = new(192, 144), Title = "GPU frame profile" };
        root.AddChild(new ColorRect { Color = Colors.Green, Size = new(192, 144) });
        var frames = 0;
        root.Ready += _ =>
        {
            logger.Clock.Restart();
            RenderingServer.ForceDraw(swapBuffers: false);
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (reports > 0 && logger.Reports.Count < reports && logger.Clock.Elapsed < TimeSpan.FromSeconds(8)) return;
                if (reports == 0 && logger.Clock.Elapsed < TimeSpan.FromSeconds(1.2)) return;
                using var image = root.GetTexture().GetImage();
                Check(image is not null && image.GetPixel(32, 32).G > .9f, "The profiled renderer produces window viewport pixels.");
                root.Tree!.Quit();
            };
        };
        Check(Engine.Run(root) == 0 && frames > 5, "Profiling preserves the window run and frame submission.");

        if (unsupported)
        {
            Check(logger.Reports.Count == 1 && logger.Reports[0].Message.StartsWith("GPU profile unavailable on ", StringComparison.Ordinal),
                "An unsupported renderer reports unavailability once and continues to draw.");
            return;
        }

        Check(logger.Reports.Count == reports, $"Expected {reports} completed GPU reports, got {string.Join(", ", logger.Reports.Select(report => report.Message))}.");
        var previous = TimeSpan.Zero;
        foreach (var (message, elapsed) in logger.Reports)
        {
            var match = Regex.Match(message, @"^GPU profile \(ms\): frame ([0-9.]+), average setup ([0-9.]+), canvas ([0-9.]+), presentation ([0-9.]+) \(([0-9]+) frames\)$");
            Check(match.Success && double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) > 0 &&
                double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) > 0 && int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) > 1,
                "Completed profiles include nonzero GPU frame and canvas durations and multiple samples.");
            Check(elapsed - previous >= TimeSpan.FromMilliseconds(850), "Reports use real-time windows.");
            previous = elapsed;
        }
    }
}
