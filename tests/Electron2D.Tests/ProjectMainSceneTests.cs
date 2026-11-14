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
            VerifyProjectPlacement(project.FullName, user.FullName);
            VerifyProjectChrome(project.FullName, user.FullName);
            if (OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") != "dummy")
                VerifyProjectTransparency(project.FullName, user.FullName, backend);

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

    private static void VerifyProjectPlacement(string project, string user)
    {
        if (!OperatingSystem.IsWindows() || Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy")
            return;

        var policy = ProjectSettings.WindowInitialPositionType;
        var position = ProjectSettings.WindowInitialPosition;
        var screen = ProjectSettings.WindowInitialScreen;
        var mode = ProjectSettings.WindowInitialMode;
        var feature = "root_placement_test";
        var origin = new Vector2i(130, 120);
        ProjectSettings.Save();
        using (var saved = new ProjectSettingsRegistry(project, user))
        {
            saved.Load();
            Check(saved.Get(policy) == Window.WindowInitialPosition.CenterPrimaryScreen && saved.Get(position) == Vector2i.Zero &&
                saved.Get(screen) == 0 && saved.Get(mode) == WindowMode.Windowed, "Built-in startup placement and mode defaults.");
            Reject<ArgumentOutOfRangeException>(() => saved.Set(policy, (Window.WindowInitialPosition)6));
            Reject<ArgumentOutOfRangeException>(() => saved.Set(mode, (WindowMode)5));
            Reject<ArgumentOutOfRangeException>(() => saved.Set(screen, -1));
            saved.Set(policy, Window.WindowInitialPosition.Absolute);
            saved.Set(position, new(-10, -20));
            saved.SetFeatureOverride(position, feature, origin);
            saved.Set(screen, int.MaxValue);
            saved.Set(mode, WindowMode.Windowed);
            saved.SetFeatureOverride(mode, feature, WindowMode.Maximized);
            saved.Save();
        }
        ProjectSettings.Load();
        ProjectSettings.AddCustomFeature(feature);
        try
        {
            Check(ProjectSettings.Get(position) == new Vector2i(-10, -20) &&
                ProjectSettings.GetWithOverride(position) == origin && ProjectSettings.GetWithOverride(mode) == WindowMode.Maximized,
                "Signed coordinates and placement/mode feature overrides round-trip through a project file.");

            using var absolute = new Window { Size = new(192, 144), Mode = WindowMode.Windowed, Borderless = true };
            RunPlacement(absolute, () =>
            {
                Check(absolute.InitialPosition == Window.WindowInitialPosition.Absolute && absolute.Position == origin &&
                    absolute.Mode == WindowMode.Windowed, "Absolute position uses the active feature; explicit Windowed overrides the project mode.");
            });

            using var maximized = new Window { Size = new(192, 144), Borderless = true };
            RunPlacement(maximized, () =>
            {
                Check(maximized.Mode == WindowMode.Maximized && DisplayServer.WindowGetMode() == WindowMode.Maximized,
                    "The project mode reaches the native window.");
            });

            ProjectSettings.Set(mode, WindowMode.Windowed);
            ProjectSettings.ClearFeatureOverride(mode, feature);
            ProjectSettings.Reset(policy);
            using var centered = new Window { Size = new(192, 144), Borderless = true };
            RunPlacement(centered, () => Check(centered.InitialPosition == Window.WindowInitialPosition.CenterPrimaryScreen &&
                centered.Position == Center(centered, DisplayServer.ScreenPrimary), "The default project policy centers on the primary display."));

            ProjectSettings.Set(policy, Window.WindowInitialPosition.CenterOtherScreen);
            using (var invalid = new Window { Size = new(192, 144) })
            {
                Reject<ArgumentOutOfRangeException>(() => Engine.RunProject(invalid));
                Check(invalid.IsDisposed && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable && Engine.MainLoop is null,
                    "An unavailable startup screen fails before scene entry and retires the host.");
            }
            ProjectSettings.Set(screen, 0);
            using var indexed = new Window { Size = new(192, 144), Borderless = true };
            RunPlacement(indexed, () => Check(indexed.Position == Center(indexed, 0) && indexed.CurrentScreen == 0,
                "Other-screen placement uses the configured display index."));

            ProjectSettings.Set(mode, WindowMode.Maximized);
            using var authored = new Window
            {
                Size = new(192, 144), Borderless = true, Position = new(180, 170),
                InitialPosition = Window.WindowInitialPosition.Absolute, Mode = WindowMode.Windowed
            };
            using var packed = new PackedScene();
            packed.Pack(authored);
            using var restored = (Window)packed.Instantiate();
            RunPlacement(restored, () => Check(restored.Position == new Vector2i(180, 170) &&
                restored.InitialPosition == Window.WindowInitialPosition.Absolute && restored.Mode == WindowMode.Windowed,
                "Packed explicit defaults retain precedence over startup placement and mode settings."));
        }
        finally
        {
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.ClearFeatureOverride(position, feature);
            ProjectSettings.ClearFeatureOverride(mode, feature);
            ProjectSettings.Reset(policy);
            ProjectSettings.Reset(position);
            ProjectSettings.Reset(screen);
            ProjectSettings.Reset(mode);
        }

        static Vector2i Center(Window window, int display) =>
            DisplayServer.ScreenGetPosition(display) + (DisplayServer.ScreenGetSize(display) - window.Size) / 2;

        static void RunPlacement(Window window, Action verify)
        {
            window.Ready += _ => { verify(); window.Tree!.Quit(); };
            Check(Engine.RunProject(window) == 0 && window.IsDisposed, "The placement host retires after verification.");
        }
    }

    private static void VerifyProjectChrome(string project, string user)
    {
        var resizable = ProjectSettings.WindowResizable;
        var borderless = ProjectSettings.WindowBorderless;
        var top = ProjectSettings.WindowAlwaysOnTop;
        var focus = ProjectSettings.WindowNoFocus;
        var corners = ProjectSettings.WindowSharpCorners;
        const string feature = "root_chrome_test";

        ProjectSettings.Save();
        using (var saved = new ProjectSettingsRegistry(project, user))
        {
            saved.Load();
            Check(saved.Get(resizable) && !saved.Get(borderless) && !saved.Get(top) &&
                !saved.Get(focus) && !saved.Get(corners), "Built-in window chrome defaults.");
            saved.Set(resizable, false);
            saved.SetFeatureOverride(borderless, feature, true);
            saved.Set(top, true);
            saved.Set(focus, true);
            saved.Set(corners, true);
            saved.Save();
        }
        ProjectSettings.Load();
        ProjectSettings.AddCustomFeature(feature);

        try
        {
            using var window = new Window { Size = new(240, 160) };
            RunChrome(window, () =>
            {
                if (DisplayServer.GetName() == "headless")
                {
                    Check(!window.Unresizable && !window.Borderless && !window.AlwaysOnTop &&
                        !window.Unfocusable && !window.SharpCorners, "The headless host has no window chrome.");
                    return;
                }

                Check(window.Unresizable && window.Borderless, "Saved resize and border settings reach the root before readiness.");
                if (OperatingSystem.IsWindows())
                {
                    Check(window.AlwaysOnTop && window.Unfocusable, "Configured stacking and focus flags reach the root.");
                    Check(DisplayServer.WindowGetFlag(WindowFlag.ResizeDisabled) &&
                        DisplayServer.WindowGetFlag(WindowFlag.Borderless) &&
                        DisplayServer.WindowGetFlag(WindowFlag.AlwaysOnTop) &&
                        DisplayServer.WindowGetFlag(WindowFlag.NoFocus), "The live native window has the four requested SDL flags.");
                    if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
                        Check(window.SharpCorners && DisplayServer.WindowGetFlag(WindowFlag.SharpCorners), "The live Windows corner preference is sharp.");
                }
                else if (DisplayServer.GetName() == "Wayland")
                {
                    Check(!window.AlwaysOnTop && !window.Unfocusable, "Wayland startup keeps compositor-owned stacking and focus.");
                }
                if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) || DisplayServer.GetName() == "headless")
                    Check(!window.SharpCorners, "Startup uses the host default when Windows 11 corner control is absent.");
            });

            using var authored = new Window
            {
                Size = new(240, 160), Unresizable = false, Borderless = false, AlwaysOnTop = false,
                Unfocusable = false, SharpCorners = false,
            };
            using var packed = new PackedScene();
            packed.Pack(authored);
            using var restored = (Window)packed.Instantiate();
            RunChrome(restored, () =>
            {
                Check(!restored.Unresizable && !restored.Borderless && !restored.AlwaysOnTop &&
                    !restored.Unfocusable && !restored.SharpCorners, "Packed explicit false flags override their project defaults.");
                if (OperatingSystem.IsWindows() && DisplayServer.GetName() != "headless")
                {
                    Check(!DisplayServer.WindowGetFlag(WindowFlag.ResizeDisabled) &&
                        !DisplayServer.WindowGetFlag(WindowFlag.Borderless) &&
                        !DisplayServer.WindowGetFlag(WindowFlag.AlwaysOnTop) &&
                        !DisplayServer.WindowGetFlag(WindowFlag.NoFocus), "Explicit packed values reach the native window.");
                    if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
                        Check(!DisplayServer.WindowGetFlag(WindowFlag.SharpCorners), "Explicit false preserves the default DWM corner policy.");
                }
            });

            using var ordinary = new Window { Size = new(240, 160) };
            ordinary.Ready += _ =>
            {
                Check(!ordinary.Unresizable && !ordinary.Borderless && !ordinary.AlwaysOnTop &&
                    !ordinary.Unfocusable && !ordinary.SharpCorners, "Ordinary Engine.Run uses the caller's flags.");
                ordinary.Tree!.Quit();
            };
            Check(Engine.Run(ordinary) == 0, "Ordinary window retires cleanly.");
        }
        finally
        {
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.ClearFeatureOverride(borderless, feature);
            ProjectSettings.Reset(resizable);
            ProjectSettings.Reset(borderless);
            ProjectSettings.Reset(top);
            ProjectSettings.Reset(focus);
            ProjectSettings.Reset(corners);
        }

        static void RunChrome(Window window, Action verify)
        {
            window.Ready += _ => { verify(); window.Tree!.Quit(); };
            Check(Engine.RunProject(window) == 0 && window.IsDisposed && !DisplayServer.IsAvailable,
                "The window chrome host retires cleanly.");
        }
    }

    private static void VerifyProjectTransparency(string project, string user, string backend)
    {
        var allowed = ProjectSettings.AllowPerPixelTransparency;
        var native = ProjectSettings.WindowTransparent;
        var clear = ProjectSettings.ViewportTransparentBackground;
        const string feature = "root_alpha_test";

        ProjectSettings.Save();
        using (var saved = new ProjectSettingsRegistry(project, user))
        {
            saved.Load();
            Check(!saved.Get(allowed) && !saved.Get(native) && !saved.Get(clear), "All three alpha settings default to false.");
            saved.Set(native, true);
            saved.Set(clear, true);
            saved.SetFeatureOverride(allowed, feature, true);
            saved.Save();
        }
        ProjectSettings.Load();
        try
        {
            Verify(new Window(), backend, nativeAlpha: false, transparentClear: true);
            ProjectSettings.AddCustomFeature(feature);
            if (backend == "gpu")
            {
                using var unsupported = new Window { Size = new(192, 144) };
                Reject<NotSupportedException>(() => Engine.RunProject(unsupported));
                Check(unsupported.IsDisposed && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable && Engine.MainLoop is null,
                    "Strict GPU refusal releases the configured transparent host.");
                ProjectSettings.Set(ProjectSettings.RenderingFallback, true);
            }

            Verify(new Window(), "compatibility", nativeAlpha: true, transparentClear: true);
            ProjectSettings.Set(native, false);
            Verify(new Window(), backend, nativeAlpha: false, transparentClear: true);
            ProjectSettings.Set(native, true);
            ProjectSettings.Set(clear, false);
            Verify(new Window(), "compatibility", nativeAlpha: true, transparentClear: false);
            ProjectSettings.Set(clear, true);

            using var packed = new PackedScene();
            using var authored = new Window { Transparent = false, TransparentBG = false };
            packed.Pack(authored);
            Verify((Window)packed.Instantiate(), backend, nativeAlpha: false, transparentClear: false);

            ProjectSettings.RemoveCustomFeature(feature);
            Verify(new Window { Transparent = true }, "compatibility", nativeAlpha: true, transparentClear: true);
        }
        finally
        {
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.ClearFeatureOverride(allowed, feature);
            ProjectSettings.Reset(allowed);
            ProjectSettings.Reset(native);
            ProjectSettings.Reset(clear);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        }

        static void Verify(Window window, string renderer, bool nativeAlpha, bool transparentClear)
        {
            using (window)
            {
                window.Size = new(192, 144);
                window.ContentScaleSize = new(96, 96);
                window.ContentScaleMode = ContentScaleMode.Viewport;
                window.ContentScaleAspect = ContentScaleAspect.Keep;
                var observed = false;
                window.Ready += _ =>
                {
                    Check(window.Transparent == nativeAlpha && DisplayServer.WindowGetFlag(WindowFlag.Transparent) == nativeAlpha &&
                        window.TransparentBG == transparentClear && RenderingServer.GetCurrentRenderingMethod() == renderer,
                        "The native window flag, viewport clear policy and renderer are independently observable.");
                    RenderingServer.FramePostDraw += () =>
                    {
                        using var viewport = window.GetTexture().GetImage()!;
                        using var presentation = RenderingServer.Service!.Readback();
                        Check(viewport.GetPixel(10, 10).IsEqualApprox(Colors.Red) && presentation.GetPixel(39, 15).IsEqualApprox(Colors.Red),
                            "Opaque scene content reaches the viewport and the fitted native presentation.");
                        Check(viewport.GetPixel(90, 90).A == (transparentClear ? 0 : 1), "The viewport clear carries the configured alpha.");
                        Check(presentation.GetPixel(2, 2).A == (nativeAlpha ? 0 : 1), "Fitted margins reflect native window alpha.");
                        if (nativeAlpha)
                            Check(presentation.GetPixel(150, 126).A == (transparentClear ? 0 : 1), "The native presentation copy preserves viewport alpha.");
                        observed = true;
                        window.Tree!.Quit();
                    };
                };
                Check(Engine.RunProject(window) == 0 && observed && window.IsDisposed, "The transparency host renders and retires.");
            }
        }
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
