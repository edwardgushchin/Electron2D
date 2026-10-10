using System.Reflection;
using Electron2D;

internal static class StaticServiceTests
{
    internal static void Run()
    {
        Type[] services = [typeof(Engine), typeof(OS), typeof(WorkerThreadPool), typeof(ProjectSettings), typeof(Input), typeof(InputMap), typeof(ThemeDB), typeof(AudioServer), typeof(PhysicsServer), typeof(Performance), typeof(DisplayServer), typeof(RenderingServer)];
        foreach (var type in services)
        {
            Check(type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static) is null, $"{type.Name} hides its service accessor.");
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            Check(methods.Length > 0 && methods.All(method => method.IsStatic), $"{type.Name} declares only static operations and accessors.");
        }
        var engine = Engine.GetSingleton<Engine>(nameof(Engine));
        Check(ReferenceEquals(engine, Engine.GetSingleton(nameof(Engine))), "Typed lookup retains engine identity.");
        var descriptor = engine.GetPropertyList().OfType<PropertyDescriptor<Engine, double>>().Single(property => property.Name == nameof(Engine.TimeScale));
        var prior = Engine.TimeScale;
        try
        {
            Engine.TimeScale = .75;
            Check(descriptor.GetValue(engine) == .75, "Static writes reach the retained property state.");
            descriptor.SetValue(engine, .5);
            Check(Engine.TimeScale == .5, "Descriptor writes reach the static read surface.");
        }
        finally { Engine.TimeScale = prior; ProjectSettings.FlushChanges(); }

        using var first = new ProjectSettingsRegistry(Environment.CurrentDirectory, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-static-first"));
        using var second = new ProjectSettingsRegistry(Environment.CurrentDirectory, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-static-second"));
        first.Set(ProjectSettings.ApplicationName, "isolated");
        var events = 0;
        first.SettingsChanged += sender => { Check(ReferenceEquals(sender, first), "Isolated event sender retains its registry."); events++; };
        first.FlushChanges();
        Check(events == 1 && first.Get(ProjectSettings.ApplicationName) == "isolated" && second.Get(ProjectSettings.ApplicationName) != "isolated" && ProjectSettings.Get(ProjectSettings.ApplicationName) != "isolated", "Isolated registries remain independent of each other and the static runtime.");
        Check(ReferenceEquals(Engine.GetSingleton<ProjectSettings>(nameof(ProjectSettings)), ProjectSettings.Service), "Global project settings retain their object identity and named registration.");
        Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native services start unavailable.");
        Reject(() => DisplayServer.GetName());
        Reject(() => RenderingServer.GetCurrentRenderingMethod());
        Reject(() => RenderingServer.FramePostDraw += Noop);
        Reject(() => RenderingServer.FramePostDraw -= Noop);
        Console.WriteLine("Static service API checks passed: eleven services, retained state and isolated registries.");
    }

    internal static void RunNative()
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var fps = Engine.MaxFPS;
        var frames = new int[2];
        RenderingServer? prior = null;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            Engine.MaxFPS = 0;
            for (var run = 0; run < 2; run++)
            {
                var current = run;
                var window = new Window { Title = "Static service session check", Visible = false, Size = new(32, 32) };
                window.Ready += _ =>
                {
                    Check(DisplayServer.IsAvailable && RenderingServer.IsAvailable, "Ready observes active native services.");
                    if (prior is not null)
                    {
                        try { prior.GetCurrentRenderingMethodCore(); throw new InvalidOperationException("A prior renderer must stay retired."); }
                        catch (ObjectDisposedException) { }
                    }
                    prior = RenderingServer.Service;
                    RenderingServer.SetDefaultClearColor(Colors.Black);
                    RenderingServer.FramePostDraw += () => { frames[current]++; window.Tree!.Quit(); };
                };
                Check(Engine.Run(window) == 0 && window.IsDisposed, "Static Run owns and disposes its scene.");
                Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native services are unpublished after teardown.");
            }
            Check(frames.SequenceEqual(new[] { 1, 1 }), "Native event subscriptions stay in their original session.");
            var failing = new Window { Visible = false, Size = new(32, 32) };
            var expected = new InvalidOperationException("Expected static service activation failure.");
            failing.Ready += _ => throw expected;
            try { Engine.Run(failing); throw new InvalidOperationException("Activation failure must propagate."); }
            catch (AggregateException error) when (error.Flatten().InnerExceptions.Count == 1 && ReferenceEquals(error.Flatten().InnerExceptions[0], expected)) { }
            Check(failing.IsDisposed && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Failed activation cleans up both native services.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback);
            Engine.MaxFPS = fps;
        }
        Console.WriteLine("Static native service reopening, event lifetime and failure cleanup checks passed.");
    }

    private static void Noop() { }
    private static void Reject(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("An unavailable native service operation must reject.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
