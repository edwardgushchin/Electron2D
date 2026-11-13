using System.Diagnostics;
using Electron2D;

internal static class ProjectMainLoopTests
{
    private const string Feature = "custom_loop_test";
    private const string ExtensionID = "test:main-loop-extension";
    private const string ExtensionClass = "test:main-loop-class";

    internal static void Run(string backend)
    {
        var setting = ProjectSettings.MainLoopType;
        using (var project = DirAccess.CreateTemp())
        using (var user = DirAccess.CreateTemp())
        using (var settings = new ProjectSettingsRegistry(project.GetCurrentDir(), user.GetCurrentDir()))
        {
            Check(settings.Get(setting) == nameof(SceneTree) && !settings.IsBasic(setting), "Default typed setting and metadata.");
            Reject<ArgumentOutOfRangeException>(() => settings.Set(setting, " \t"));
            settings.Set(setting, "Unknown");
            settings.SetFeatureOverride(setting, Feature, nameof(ProjectLoopProbe));
            settings.Save();
            using var loaded = new ProjectSettingsRegistry(project.GetCurrentDir(), user.GetCurrentDir());
            loaded.Load();
            Check(loaded.Get(setting) == "Unknown" && loaded.GetWithOverride(setting, [Feature]) == nameof(ProjectLoopProbe), "Persisted selector and override.");
        }

        Script.RegisterObject("test:project-loop", ProjectLoopProbe.SourcePath, CreateScriptLoop);
        Script.RegisterObject<ProjectLoopWithoutFactory>("test:loop-no-factory", ProjectLoopWithoutFactory.SourcePath, null);
        Script.RegisterObject("test:wrong-loop", ProjectLoopWrongType.SourcePath, CreateWrongType);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.MainScene, "res://does-not-exist.e2dscene");

        foreach (var name in new[] { "Unknown", "projectloopprobe", nameof(ProjectLoopWithoutFactory), nameof(ProjectLoopWrongType) })
        {
            ProjectSettings.Set(setting, name);
            using var window = NewWindow();
            Reject<InvalidOperationException>(() => Engine.RunProject(window));
            CheckRetired(window);
        }

        ProjectSettings.SetFeatureOverride(setting, Feature, nameof(ProjectLoopProbe));
        ProjectSettings.AddCustomFeature(Feature);
        try { RunProbe(asynchronous: false); }
        finally { ProjectSettings.RemoveCustomFeature(Feature); }
        ProjectSettings.Set(setting, nameof(ProjectLoopProbe));
        RunProbe(asynchronous: true);
        RunProbe(asynchronous: false, close: true);

        foreach (var stage in new[] { "initialize", "process", "finalize" })
        {
            using var window = NewWindow();
            ProjectLoopProbe.Reset(window);
            ProjectLoopProbe.Fail = stage;
            Reject<ArithmeticException>(() => Engine.RunProject(window));
            var loop = ProjectLoopProbe.Last!;
            Check(loop.IsDisposed && loop.Finalized == (stage == "initialize" ? 0 : 1), "Lifecycle failure retires the loop exactly once.");
            CheckRetired(window);
        }

        ExtensionManager.LoadExtensionFromFunction(ExtensionID, OpenExtension);
        var extension = ExtensionManager.GetExtension(ExtensionID)!;
        try
        {
            ProjectSettings.Set(setting, ExtensionClass);
            using var window = NewWindow();
            Check(Engine.RunProject(window) == 0 && ExtensionLoop.Last is { IsDisposed: true, Finalized: true, Bound: false }, "Registered extension class executes and retires.");
            CheckRetired(window);
        }
        finally { ExtensionManager.UnloadExtension(ExtensionID); extension.Dispose(); }

        using var explicitWindow = NewWindow();
        explicitWindow.Ready += _ => explicitWindow.Tree!.Quit(27);
        Check(Engine.Run(explicitWindow) == 27, "Explicit Run retains its ordinary tree with a configured custom selector.");
        CheckRetired(explicitWindow);
        ProjectSettings.Set(setting, "");
        ProjectSettings.Reset(ProjectSettings.MainScene);
        using var sceneWindow = NewWindow();
        Reject<InvalidOperationException>(() => Engine.RunProject(sceneWindow));
        CheckRetired(sceneWindow);
        Console.WriteLine($"Configured MainLoop {backend}: selection, native canvas pixels, lifecycle failures and recovery passed.");
    }

    private static ProjectLoopProbe CreateScriptLoop() => new();
    private static ProjectLoopWrongType CreateWrongType() => new();
    private static ExtensionLoop CreateExtensionLoop() => new();
    private static ExtensionInitialization? OpenExtension(Extension module) =>
        new(ExtensionInitializationLevel.Scene, level =>
        {
            if (level == ExtensionInitializationLevel.Scene)
                module.RegisterClass(ExtensionClass, CreateExtensionLoop, static loop => loop.Bound = true, static loop => loop.Bound = false);
        }, static _ => { });

    private static Window NewWindow() => new() { Size = new(64, 48) };
    private static void RunProbe(bool asynchronous, bool close = false)
    {
        using var window = NewWindow();
        using var child = new Node();
        using var texture = window.GetTexture();
        window.AddChild(child);
        window.Ready += _ => throw new InvalidOperationException("A custom loop must not enter the window tree.");
        ProjectLoopProbe.Reset(window, texture, close);
        var result = asynchronous ? RunAsync(window) : Engine.RunProject(window);
        var loop = ProjectLoopProbe.Last!;
        Check(result == 0 && loop.Finalized == 1 && loop.IsDisposed && loop.Physics > 0 && loop.Drawn == (close ? 7 : 6) &&
            loop.Source is { IsDisposed: true } && child.IsDisposed, "The owned host drives and retires a fresh custom loop and window.");
        CheckRetired(window);
    }

    private static int RunAsync(Window window)
    {
        var previous = SynchronizationContext.Current;
        var context = new MovieWriterTests.PumpContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var task = Engine.RunProjectAsync(window);
            var timeout = Stopwatch.StartNew();
            while (!task.IsCompleted)
            {
                if (timeout.Elapsed.TotalSeconds > 15) throw new TimeoutException("The configured asynchronous loop did not quit.");
                context.Pump();
            }
            return task.GetAwaiter().GetResult();
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private static void CheckRetired(Window window) => Check(window.IsDisposed && Engine.MainLoop is null &&
        !RenderingServer.IsAvailable && !DisplayServer.IsAvailable, "The owned window, display, renderer and engine registration retire.");

    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    private sealed class ExtensionLoop : MainLoop
    {
        internal static ExtensionLoop? Last;
        internal bool Bound, Finalized;
        protected override void OnInitialize()
        {
            Check(Bound && ReferenceEquals(this, Engine.MainLoop), "Extension binding precedes initialization.");
            Last = this;
        }
        protected override bool OnProcess(double delta) => true;
        protected override void OnFinalize()
        {
            Check(Bound, "Extension binding survives finalization.");
            Finalized = true;
        }
    }
}
