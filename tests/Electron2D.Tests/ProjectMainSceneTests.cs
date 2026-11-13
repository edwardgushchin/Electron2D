using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;

internal static class ProjectMainSceneTests
{
    private const string Feature = "main_scene_test";

    internal static void Run(string backend)
    {
        var project = Directory.CreateTempSubdirectory("electron2d-main-scene-");
        var user = project.CreateSubdirectory("user");
        var previousProject = ProjectSettings.ProjectRoot;
        var previousUser = ProjectSettings.UserDataRoot;
        const string main = "res://main.e2dscene";
        const string alternate = "res://alternate.e2dscene";
        try
        {
            using (var settings = new ProjectSettingsRegistry(project.FullName, user.FullName))
            {
                Check(settings.Get(ProjectSettings.MainScene) == "", "An independent project starts without a main scene.");
                Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.MainScene, " "));
                settings.Set(ProjectSettings.MainScene, main);
                settings.SetFeatureOverride(ProjectSettings.MainScene, Feature, alternate);
                settings.Save();
            }
            using (var settings = new ProjectSettingsRegistry(project.FullName, user.FullName))
            {
                settings.Load();
                Check(settings.Get(ProjectSettings.MainScene) == main, "The configured scene path persists.");
                settings.AddCustomFeature(Feature);
                Check(settings.GetWithOverride(ProjectSettings.MainScene) == alternate, "The feature scene path persists.");
            }

            ProjectSettings.ConfigurePaths(project.FullName, user.FullName);
            ProjectSettings.Load();
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            SaveScene(main, "Primary", Colors.Red);
            SaveScene(alternate, "Alternate", Colors.Blue);

            ProjectSettings.Reset(ProjectSettings.MainScene);
            CheckFailed<InvalidOperationException>();
            ProjectSettings.Set(ProjectSettings.MainScene, "res://missing.e2dscene");
            CheckFailed<FileNotFoundException>();
            using (var wrong = new Gradient())
                ResourceSaver.Save(wrong, "res://wrong.e2dres");
            ProjectSettings.Set(ProjectSettings.MainScene, "res://wrong.e2dres");
            CheckFailed<InvalidDataException>();

            ProjectSettings.Set(ProjectSettings.MainScene, main);
            using (var broken = new Window { Size = new(96, 72) })
            {
                Node? scene = null;
                broken.Ready += _ =>
                {
                    scene = broken.Tree!.CurrentScene;
                    throw new ArithmeticException("Project root ready failure");
                };
                Reject<AggregateException>(() => Engine.RunProject(broken));
                Check(broken.IsDisposed && scene is { IsDisposed: true } && !RenderingServer.IsAvailable && Engine.MainLoop is null,
                    "Ready failure disposes the scene, window and renderer.");
            }

            VerifyRendered("Primary", Colors.Red, asynchronous: false);
            ProjectSettings.AddCustomFeature(Feature);
            VerifyRendered("Alternate", Colors.Blue, asynchronous: true);
            ProjectSettings.RemoveCustomFeature(Feature);
            Check(!ResourceLoader.HasCached(main) && !ResourceLoader.HasCached(alternate), "Startup-owned templates retire with their scene instances.");
            var uid = ResourceSaver.GetResourceIDForPath(main);
            Check(uid != ResourceUID.InvalidID, "The archived scene has a resource UID.");
            ProjectSettings.Set(ProjectSettings.MainScene, ResourceUID.IDToText(uid));
            VerifyRendered("Primary", Colors.Red, asynchronous: false);

            using var explicitWindow = new Window { Size = new(96, 72) };
            explicitWindow.Ready += _ =>
            {
                Check(explicitWindow.Tree!.CurrentScene is null, "Explicit Run leaves project selection to the caller.");
                explicitWindow.Tree.Quit(14);
            };
            Check(Engine.Run(explicitWindow) == 14, "The ordinary host retains its explicit-scene behavior.");
        }
        finally
        {
            ProjectSettings.RemoveCustomFeature(Feature);
            ProjectSettings.Save();
            ProjectSettings.ConfigurePaths(previousProject, previousUser);
            project.Delete(recursive: true);
        }
        Console.WriteLine($"Project main scene {backend}: persistence, path and UID, sync/async startup, pixels, failure cleanup and restart passed.");
    }

    private static void SaveScene(string path, string name, Color color)
    {
        using var scene = new ColorRect { Name = name, Size = new(48, 48), Color = color };
        ResourceSaver.SaveScene(scene, path, SaverFlags.Compress);
    }

    private static void VerifyRendered(string name, Color color, bool asynchronous)
    {
        using var window = new Window { Size = new(96, 72) };
        using var overlay = new Node();
        window.AddChild(overlay);
        using var texture = window.GetTexture();
        Node? scene = null;
        var owner = Environment.CurrentManagedThreadId;
        var frames = 0;
        overlay.Ready += _ =>
        {
            scene = window.Tree!.CurrentScene;
            Check(scene is not null && scene.Name == name && !scene.IsNodeReady && ReferenceEquals(scene.Parent, window),
                "The current scene is selected before child readiness and follows existing window children.");
            Check(ReferenceEquals(Engine.MainLoop, window.Tree), "The window tree is published before readiness.");
        };
        window.Ready += _ =>
        {
            Check(scene is { IsNodeReady: true } && ReferenceEquals(scene, window.Tree!.CurrentScene), "The initial scene is ready before the root.");
            RenderingServer.FramePostDraw += () =>
            {
                Check(Environment.CurrentManagedThreadId == owner, "Drawing remains on the window owner.");
                using var image = texture.GetImage()!;
                Check(image.GetPixel(16, 16).IsEqualApprox(color), "The configured scene renders the expected pixel.");
                if (++frames == 2) window.Tree!.Quit(42);
            };
        };
        var code = asynchronous ? RunAsync(window) : Engine.RunProject(window);
        Check(code == 42 && frames == 2 && window.IsDisposed && scene is { IsDisposed: true } && !RenderingServer.IsAvailable && Engine.MainLoop is null,
            "The owned host returns the exit code and disposes the scene and native resources.");
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
                if (timeout.Elapsed.TotalSeconds > 15) throw new TimeoutException("The configured asynchronous scene did not quit.");
                context.Pump();
            }
            return pending.GetAwaiter().GetResult();
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private static void CheckFailed<T>() where T : Exception
    {
        using var window = new Window { Size = new(96, 72) };
        var ready = false;
        window.Ready += _ => { ready = true; window.Tree!.Quit(); };
        Reject<T>(() => Engine.RunProject(window));
        Check(!ready && window.IsDisposed && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable && Engine.MainLoop is null,
            "A failed startup retires all owned state before delivering Ready.");
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
