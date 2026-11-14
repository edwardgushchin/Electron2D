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
            VerifyProjectWindow(project.FullName, user.FullName, main, backend);

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

            ProjectSettings.Set(ProjectSettings.MainScene, main);
            ProjectSettings.Set(ProjectSettings.ProjectSettingsOverride, "user://profile.cfg");
            ProjectSettings.Save();
            using (var config = new ConfigFile())
            {
                config.SetValue(new ConfigKey<string>("application", "run/main_scene"), alternate);
                config.Save(Path.Combine(user.FullName, "profile.cfg"));
            }
            ProjectSettings.Load();
            VerifyRendered("Alternate", Colors.Blue, asynchronous: false);

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
        Console.WriteLine($"Project main scene {backend}: persistence, path and UID, sync/async startup, window geometry, pixels, failure cleanup and restart passed.");
    }

    private static void VerifyProjectWindow(string project, string user, string scenePath, string backend)
    {
        var width = ProjectSettings.ViewportWidth;
        var height = ProjectSettings.ViewportHeight;
        var clientWidth = ProjectSettings.WindowWidthOverride;
        var clientHeight = ProjectSettings.WindowHeightOverride;
        var mode = ProjectSettings.WindowStretchMode;
        var aspect = ProjectSettings.WindowStretchAspect;
        var scale = ProjectSettings.WindowStretchScale;
        var scaleMode = ProjectSettings.WindowStretchScaleMode;
        using (var saved = new ProjectSettingsRegistry(project, user))
        {
            Check((saved.Get(width), saved.Get(height), saved.Get(clientWidth), saved.Get(clientHeight)) == (1152, 648, 0, 0),
                "Viewport and desktop client defaults are registered.");
            Check((saved.Get(mode), saved.Get(aspect), saved.Get(scale), saved.Get(scaleMode)) ==
                (ContentScaleMode.Disabled, ContentScaleAspect.Keep, 1f, ContentScaleStretch.Fractional), "Stretch defaults are registered.");
            Reject<ArgumentOutOfRangeException>(() => saved.Set(width, 0));
            Reject<ArgumentOutOfRangeException>(() => saved.Set(clientHeight, -1));
            Reject<ArgumentOutOfRangeException>(() => saved.Set(mode, (ContentScaleMode)3));
            Reject<ArgumentException>(() => saved.Set(scale, float.NaN));
            saved.Set(ProjectSettings.MainScene, scenePath);
            saved.SetFeatureOverride(ProjectSettings.MainScene, Feature, "res://alternate.e2dscene");
            saved.Set(ProjectSettings.RenderingMethod, backend);
            saved.Set(ProjectSettings.RenderingFallback, false);
            saved.Set(ProjectSettings.BootSplashShowImage, false);
            saved.Set(width, 80);
            saved.Set(height, 80);
            saved.Set(clientWidth, 240);
            saved.Set(clientHeight, 160);
            saved.Set(mode, ContentScaleMode.Viewport);
            saved.Set(aspect, ContentScaleAspect.Expand);
            saved.SetFeatureOverride(aspect, Feature, ContentScaleAspect.Keep);
            saved.Set(scale, 2.75f);
            saved.Set(scaleMode, ContentScaleStretch.Integer);
            saved.Save();
        }
        ProjectSettings.Load();
        Check(ProjectSettings.Get(mode) == ContentScaleMode.Viewport && ProjectSettings.Get(aspect) == ContentScaleAspect.Expand &&
            ProjectSettings.GetWithOverride(aspect, [Feature]) == ContentScaleAspect.Keep, "Typed enum settings and feature values survive a project-file round trip.");

        using var untouched = new Window();
        using var untouchedScene = new PackedScene();
        untouchedScene.Pack(untouched);
        using var fitted = (Window)untouchedScene.Instantiate();
        ProjectSettings.AddCustomFeature(Feature);
        ProjectSettings.ClearFeatureOverride(ProjectSettings.MainScene, Feature);
        var frames = 0;
        fitted.Ready += _ =>
        {
            Check(fitted.Size == new Vector2i(240, 160) && DisplayServer.WindowGetSize() == fitted.Size, "The native client uses both desktop overrides.");
            Check(fitted.ContentScaleSize == new Vector2i(80, 80) && fitted.ContentScaleMode == ContentScaleMode.Viewport &&
                fitted.ContentScaleAspect == ContentScaleAspect.Keep && fitted.ContentScaleFactor == 2 &&
                fitted.ContentScaleStretch == ContentScaleStretch.Integer && fitted.GetVisibleRect().Size == new Vector2(40, 40),
                "Project settings are effective before readiness and choose the logical raster.");
            using var input = new InputEventMouseMotion { Position = new(60, 20) };
            using var local = (InputEventMouseMotion)fitted.MakeViewportInputLocal(input);
            Check(local.Position == new Vector2(5, 5), "Input uses the fitted project's client-to-logical transform.");
            RenderingServer.FramePostDraw += () =>
            {
                using var viewport = fitted.GetTexture().GetImage()!;
                using var presented = RenderingServer.Service!.Readback();
                Check(viewport.Size == new Vector2i(40, 40) && presented.Size == new Vector2i(240, 160) &&
                    presented.GetPixel(60, 20).IsEqualApprox(Colors.Red) && presented.GetPixel(10, 20).IsEqualApprox(Colors.Black),
                    "The project scene fills the fitted raster and the native presentation has black side margins.");
                frames++;
                fitted.Tree!.Quit();
            };
        };
        Check(RunAsync(fitted) == 0 && frames == 1, "The configured asynchronous host renders the fitted project.");
        ProjectSettings.RemoveCustomFeature(Feature);
        ProjectSettings.SetFeatureOverride(ProjectSettings.MainScene, Feature, "res://alternate.e2dscene");

        using var authored = new Window
        {
            Size = new(100, 100),
            ContentScaleSize = Vector2i.Zero,
            ContentScaleMode = ContentScaleMode.Disabled,
            ContentScaleAspect = ContentScaleAspect.Ignore,
            ContentScaleFactor = 1,
            ContentScaleStretch = ContentScaleStretch.Fractional
        };
        using var authoredScene = new PackedScene();
        authoredScene.Pack(authored);
        using var restored = (Window)authoredScene.Instantiate();
        restored.Ready += _ =>
        {
            Check(restored.Size == new Vector2i(100, 100) && restored.ContentScaleSize == Vector2i.Zero &&
                restored.ContentScaleMode == ContentScaleMode.Disabled && restored.ContentScaleAspect == ContentScaleAspect.Ignore &&
                restored.ContentScaleFactor == 1 && restored.ContentScaleStretch == ContentScaleStretch.Fractional,
                "Explicit values equal to detached defaults retain precedence through packing.");
            restored.Tree!.Quit();
        };
        Check(Engine.RunProject(restored) == 0, "An explicitly configured root keeps its startup policy.");

        ProjectSettings.Set(clientHeight, 0);
        ProjectSettings.Set(mode, ContentScaleMode.CanvasItems);
        ProjectSettings.Set(aspect, ContentScaleAspect.Ignore);
        ProjectSettings.Set(scale, 1f);
        ProjectSettings.Set(scaleMode, ContentScaleStretch.Fractional);
        using var singleAxis = new Window();
        singleAxis.Ready += _ =>
        {
            Check(singleAxis.Size == new Vector2i(240, 80) && singleAxis.GetVisibleRect().Size == new Vector2(80, 80) &&
                singleAxis.GetStretchTransform().Scale == new Vector2(3, 1), "A zero client override falls back only on that axis.");
            singleAxis.Tree!.Quit();
        };
        Check(Engine.RunProject(singleAxis) == 0, "The configured host applies a single client-axis override.");

        using var ordinary = new Window();
        ordinary.Ready += _ =>
        {
            Check(ordinary.Size == new Vector2i(100, 100) && ordinary.ContentScaleMode == ContentScaleMode.Disabled &&
                ordinary.ContentScaleSize == Vector2i.Zero, "Engine.Run retains the detached window's defaults.");
            ordinary.Tree!.Quit();
        };
        Check(Engine.Run(ordinary) == 0, "Explicit run retains its own geometry.");

        ProjectSettings.Reset(width);
        ProjectSettings.Reset(height);
        ProjectSettings.Reset(clientWidth);
        ProjectSettings.Reset(clientHeight);
        ProjectSettings.Reset(mode);
        ProjectSettings.Reset(aspect);
        ProjectSettings.Reset(scale);
        ProjectSettings.Reset(scaleMode);
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
