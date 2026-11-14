using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;
using IOPath = System.IO.Path;
using Stretch = Electron2D.RenderingServer.SplashStretchMode;

internal static class ProjectBootSplashTests
{
    private static readonly Color Background = Colors.Cyan;
    private const string CustomPath = "res://startup/splash.png";

    internal static void Run(string backend)
    {
        var project = Directory.CreateTempSubdirectory("electron2d-boot-splash-");
        var user = project.CreateSubdirectory("user");
        var oldProject = ProjectSettings.ProjectRoot;
        var oldUser = ProjectSettings.UserDataRoot;
        try
        {
            using (var settings = new ProjectSettingsRegistry(project.FullName, user.FullName))
            {
                Check(settings.Get(ProjectSettings.BootSplashShowImage) &&
                    settings.Get(ProjectSettings.BootSplashUseFilter) &&
                    settings.Get(ProjectSettings.BootSplashImage) == "" &&
                    settings.Get(ProjectSettings.BootSplashMinimumDisplayTimeMSEC) == 0 &&
                    settings.Get(ProjectSettings.BootSplashStretchMode) == Stretch.Keep &&
                    settings.Get(ProjectSettings.BootSplashBackgroundColor) == new Color(.14f, .14f, .14f),
                    "Boot splash defaults are present in isolated registries.");
                Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.BootSplashStretchMode, (Stretch)6));
                Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.BootSplashMinimumDisplayTimeMSEC, -1));
                Reject<System.Text.Json.JsonException>(() => settings.Set(ProjectSettings.BootSplashBackgroundColor, new(float.NaN, 0, 0)));
                settings.Set(ProjectSettings.BootSplashBackgroundColor, Background);
                settings.Set(ProjectSettings.BootSplashImage, "res://missing.png");
                settings.SetFeatureOverride(ProjectSettings.BootSplashImage, "template", CustomPath);
                settings.Set(ProjectSettings.BootSplashStretchMode, Stretch.Ignore);
                settings.Set(ProjectSettings.BootSplashUseFilter, false);
                settings.Save();
            }

            ProjectSettings.ConfigurePaths(project.FullName, user.FullName);
            ProjectSettings.Load();
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            ProjectSettings.Set(ProjectSettings.MainScene, "res://game.e2dscene");
            ProjectSettings.Set(ProjectSettings.DefaultClearColor, Colors.Black);
            using (var scene = new ColorRect { Size = new(32, 32), Color = Colors.Green })
                ResourceSaver.SaveScene(scene, "res://game.e2dscene");

            var packPath = IOPath.Combine(project.FullName, "splash.pck");
            using (var image = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8))
            using (var pack = new PCKPacker())
            {
                image.SetPixel(0, 0, Colors.Red);
                image.SetPixel(1, 0, Colors.Blue);
                pack.PCKStart(packPath);
                pack.AddFileFromBuffer(CustomPath, image.SavePNGToBuffer());
                pack.AddFileFromBuffer("res://startup/bad.png", [0, 1, 2]);
                pack.Flush();
            }
            Check(ProjectSettings.LoadResourcePack(packPath), "Mount the project boot image.");
            Check(ProjectSettings.GetWithOverride(ProjectSettings.BootSplashImage) == CustomPath, "Saved feature override supplies the packed path.");

            Verify(backend, (image, _) =>
            {
                Pixel(image, 16, 16, Colors.Red);
                Pixel(image, image.Width / 2 - 1, image.Height / 2, Colors.Red);
                Pixel(image, image.Width - 16, 16, Colors.Blue);
            });

            ProjectSettings.Set(ProjectSettings.BootSplashMinimumDisplayTimeMSEC, 250);
            ProjectSettings.Set(ProjectSettings.BootSplashUseFilter, true);
            ProjectSettings.Set(ProjectSettings.BootSplashStretchMode, Stretch.KeepWidth);
            Verify(backend, (image, driver) =>
            {
                Pixel(image, 0, 0, Background);
                var edge = image.GetPixel(image.Width / 2 - 1, image.Height / 2);
                if (driver == "software") Check(edge.R > .98f && edge.B < .02f, "Software uses nearest sampling.");
                else Check(edge.R is > .4f and < .6f && edge.B is > .4f and < .6f, $"Linear sampling mixes the boundary texels: {edge}.");
            }, minimumMsec: 250, asynchronous: true);

            ProjectSettings.Set(ProjectSettings.BootSplashMinimumDisplayTimeMSEC, 0);
            ProjectSettings.Set(ProjectSettings.BootSplashShowImage, false);
            Verify(backend, (image, _) =>
            {
                Pixel(image, 0, 0, Background);
                Pixel(image, image.Width / 2, image.Height / 2, Background);
            });

            ProjectSettings.Set(ProjectSettings.BootSplashShowImage, true);
            Check(ProjectSettings.ClearFeatureOverride(ProjectSettings.BootSplashImage, "template"), "Remove the packed override.");
            ProjectSettings.Set(ProjectSettings.BootSplashImage, "res://startup/bad.png");
            Verify(backend, (image, _) =>
            {
                Pixel(image, 0, 0, Background);
                Check(!image.GetPixel(image.Width / 2, image.Height / 2).IsEqualApprox(Background), "Malformed PNG falls back to the built-in image.");
            });
        }
        finally
        {
            ProjectSettings.Save();
            ProjectSettings.ConfigurePaths(oldProject, oldUser);
            project.Delete(recursive: true);
        }

        Console.WriteLine($"Project boot splash {backend}: typed persistence, packed override, nearest/linear, background, fallback, duration and scene handoff passed.");
    }

    private static void Verify(string backend, Action<Image, string> readSplash, int minimumMsec = 0, bool asynchronous = false)
    {
        using var window = new Window { Size = new(256, 192) };
        long readyAt = 0;
        var drawn = Engine.GetFramesDrawn();
        window.Ready += _ =>
        {
            readyAt = Stopwatch.GetTimestamp();
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested backend is active.");
            Check(window.Tree!.CurrentScene is { IsNodeReady: true } &&
                window.GetTexture().GetImage() is null && Engine.GetFramesDrawn() == drawn,
                "The splash precedes a ready game scene's first viewport render.");
            using var image = RenderingServer.Service!.Readback();
            readSplash(image, RenderingServer.GetCurrentRenderingDriverName());
            RenderingServer.FramePostDraw += () =>
            {
                using var rendered = RenderingServer.Service.Readback();
                Pixel(rendered, 16, 16, Colors.Green);
                Pixel(rendered, rendered.Width - 16, rendered.Height - 16, Colors.Black);
                if (minimumMsec > 0)
                    Check(Stopwatch.GetElapsedTime(readyAt).TotalMilliseconds >= minimumMsec / 2,
                        "After readiness, the host holds the visible splash for the remaining startup interval.");
                window.Tree.Quit(23);
            };
        };
        var result = asynchronous ? RunAsync(window) : Engine.RunProject(window);
        Check(result == 23 && readyAt != 0 && window.IsDisposed && !RenderingServer.IsAvailable, "The owned project host retires after the scene frame.");
    }

    private static int RunAsync(Window window)
    {
        var previous = SynchronizationContext.Current;
        var context = new MovieWriterTests.PumpContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var pending = Engine.RunProjectAsync(window);
            var timeout = Stopwatch.StartNew();
            while (!pending.IsCompleted)
            {
                if (timeout.Elapsed.TotalSeconds > 15) throw new TimeoutException("The project splash session did not quit.");
                context.Pump();
            }
            return pending.GetAwaiter().GetResult();
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y);
        Check(actual.IsEqualApprox(expected), $"Pixel {x},{y} is {actual}, expected {expected}.");
    }
}
