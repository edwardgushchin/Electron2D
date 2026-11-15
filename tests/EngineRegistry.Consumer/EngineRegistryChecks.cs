using Electron2D;

internal static class EngineRegistryChecks
{
    private const string Primary = "EngineRegistry.Primary";
    private const string Alias = "engineRegistry.Primary";
    private const string Missing = "EngineRegistry.Missing";
    private static readonly string[] PermanentNames =
    [
        nameof(Engine), nameof(OS), nameof(TextServerManager), nameof(Electron2D.Time), nameof(WorkerThreadPool),
        nameof(ProjectSettings), nameof(IP), nameof(Input), nameof(InputMap), nameof(ResourceLoader), nameof(ResourceSaver),
        nameof(ResourceUID), nameof(ExtensionManager), nameof(NavigationServer), nameof(CameraServer), nameof(AudioServer),
        nameof(PhysicsServer), nameof(Performance), nameof(TranslationServer)
    ];

    internal static void Run()
    {
        using var service = new Probe();
        using var other = new Probe();
        var baseline = Engine.GetSingletonList();
        Check(baseline.SequenceEqual(PermanentNames), "All permanent names retain their original order.");
        foreach (var name in PermanentNames)
        {
            var current = Engine.GetSingleton(name);
            Reject<InvalidOperationException>(() => Engine.UnregisterSingleton(name));
            Reject<InvalidOperationException>(() => Engine.RegisterSingleton(name, service));
            Check(Engine.HasSingleton(name) && ReferenceEquals(Engine.GetSingleton<ElectronObject>(name), current) &&
                  current.ClassName == name && !current.IsDisposed, $"Permanent {name} preserves its live registered identity.");
        }
        Check(Engine.GetSingletonList().SequenceEqual(baseline), "Rejected mutations leave permanent order unchanged.");
        Check(ReferenceEquals(Engine.GetSingleton<ProjectSettingsRegistry>(nameof(ProjectSettings)),
                              Engine.GetSingleton<ProjectSettings>(nameof(ProjectSettings))), "Typed lookup accepts a service base type.");

        foreach (var invalid in new string?[] { null, "", " \t", "\u2003" })
        {
            var expected = invalid is null ? typeof(ArgumentNullException) : typeof(ArgumentException);
            Reject(expected, () => Engine.RegisterSingleton(invalid!, service));
            Reject(expected, () => Engine.UnregisterSingleton(invalid!));
            Reject(expected, () => Engine.GetSingleton(invalid!));
            Reject(expected, () => Engine.GetSingleton<Probe>(invalid!));
            Reject(expected, () => Engine.HasSingleton(invalid!));
        }
        Reject<ArgumentNullException>(() => Engine.RegisterSingleton(Primary, null!));
        Check(!Engine.HasSingleton(Missing), "Presence checks return false for absent names.");
        Reject<KeyNotFoundException>(() => Engine.GetSingleton(Missing));
        Reject<KeyNotFoundException>(() => Engine.GetSingleton<Probe>(Missing));
        Reject<KeyNotFoundException>(() => Engine.UnregisterSingleton(Missing));

        try
        {
            Engine.RegisterSingleton(Primary, service);
            Engine.RegisterSingleton(Alias, service);
            var snapshot = Engine.GetSingletonList();
            Check(snapshot.SequenceEqual(baseline.Concat([Primary, Alias])), "Aliases and ordinal case distinctions retain insertion order.");
            if (snapshot is IList<string> list)
                Reject<NotSupportedException>(() => list[0] = "corrupted");
            Reject<InvalidOperationException>(() => Engine.RegisterSingleton(Primary, other));
            Reject<InvalidCastException>(() => Engine.GetSingleton<Window>(Primary));
            Check(ReferenceEquals(Engine.GetSingleton<Probe>(Primary), service), "Rejected duplicate and cast preserve identity.");

            Engine.UnregisterSingleton(Primary);
            Check(!service.IsDisposed && ReferenceEquals(Engine.GetSingleton(Alias), service), "Removal leaves the object and its alias live.");
            Engine.RegisterSingleton(Primary, other);
            Check(Engine.GetSingletonList().SequenceEqual(baseline.Concat([Alias, Primary])), "Re-registration appends the name.");
            Check(snapshot.SequenceEqual(baseline.Concat([Primary, Alias])), "Old snapshots remain detached.");
            service.Dispose();
            Check(Engine.HasSingleton(Alias) && ReferenceEquals(Engine.GetSingleton<Probe>(Alias), service),
                  "Caller disposal leaves a registration until explicit removal.");
            Reject<ObjectDisposedException>(() => Engine.RegisterSingleton(Missing, service));
        }
        finally
        {
            RemoveIfRegistered(Primary);
            RemoveIfRegistered(Alias);
        }
        Check(!other.IsDisposed && Engine.GetSingletonList().SequenceEqual(baseline), "Removal restores names and leaves ownership with the caller.");
        VerifyConcurrent(baseline);
        Console.WriteLine($"Engine registry managed checks passed for all {PermanentNames.Length} permanent names, name/type errors, snapshots, disposal and concurrent operations.");
    }

    private static void VerifyConcurrent(IReadOnlyList<string> baseline)
    {
        var names = Enumerable.Range(0, 32).Select(index => $"EngineRegistry.Concurrent.{index}").ToArray();
        var instances = names.Select(_ => new Probe()).ToArray();
        var winner = -1;
        try
        {
            Parallel.For(0, names.Length, index =>
            {
                Engine.RegisterSingleton(names[index], instances[index]);
                try
                {
                    Engine.RegisterSingleton(Primary, instances[index]);
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                Check(Interlocked.CompareExchange(ref winner, index, -1) == -1, "A racing duplicate must have exactly one winner.");
            });
            var published = Engine.GetSingletonList();
            Check(winner >= 0 && ReferenceEquals(Engine.GetSingleton(Primary), instances[winner]), "The winning object is published once.");
            Check(published.Count == baseline.Count + names.Length + 1 &&
                  published.Take(baseline.Count).SequenceEqual(baseline) &&
                  published.Skip(baseline.Count).ToHashSet(StringComparer.Ordinal).SetEquals(names.Append(Primary)),
                  "Concurrent publication keeps the permanent prefix and every distinct name.");
            Parallel.For(0, names.Length, index =>
            {
                Check(Engine.HasSingleton(names[index]) && ReferenceEquals(Engine.GetSingleton<Probe>(names[index]), instances[index]),
                      "Concurrent lookups see the registered identities.");
                Engine.UnregisterSingleton(names[index]);
                var snapshot = Engine.GetSingletonList();
                Check(snapshot.Take(baseline.Count).SequenceEqual(baseline) &&
                      !snapshot.Contains(names[index]) && snapshot.Distinct(StringComparer.Ordinal).Count() == snapshot.Count,
                      "Snapshots captured during removal are coherent.");
            });
            Check(published.Count == baseline.Count + names.Length + 1 && names.All(published.Contains),
                  "Concurrent removal does not mutate a prior snapshot.");
        }
        finally
        {
            RemoveIfRegistered(Primary);
            for (var index = 0; index < names.Length; index++)
            {
                RemoveIfRegistered(names[index]);
                Check(!instances[index].IsDisposed, "A concurrent registry removal must not dispose the object.");
                instances[index].Dispose();
            }
        }
        Check(Engine.GetSingletonList().SequenceEqual(baseline), "Concurrent cleanup restores the original registry.");
    }

    internal static void RunWindowed(string backend)
    {
        Check(backend is "gpu" or "compatibility", "Specify gpu or compatibility.");
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 60;
        using var shared = new Probe();
        var baseline = Engine.GetSingletonList();
        var manager = Engine.GetSingleton<TextServerManager>(nameof(TextServerManager));
        var translation = Engine.GetSingleton<TranslationServer>(nameof(TranslationServer));
        Engine.RegisterSingleton(Primary, shared);
        try
        {
            for (var pass = 1; pass <= 2; pass++)
            {
                using var root = new Window { Size = new(96, 80), Borderless = true };
                root.Ready += _ => Check(ReferenceEquals(Engine.GetSingleton(nameof(TranslationServer)), translation),
                    "The window loop observes the same permanent translation service.");
                var scene = new RegistryScene(root, shared, backend);
                root.AddChild(scene);
                Check(Engine.Run(root) == 0 && scene.Rendered && scene.Exited && root.IsDisposed,
                      "The real scene rendered and all lifecycle callbacks completed.");
                Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable && Engine.MainLoop is null,
                      "Each run retires display, renderer and scene loop.");
                Check(!Engine.HasSingleton(Alias) && ReferenceEquals(Engine.GetSingleton<Probe>(Primary), shared) && !shared.IsDisposed &&
                      ReferenceEquals(Engine.GetSingleton<TextServerManager>(nameof(TextServerManager)), manager) &&
                      ReferenceEquals(Engine.GetSingleton<TranslationServer>(nameof(TranslationServer)), translation),
                      "Shutdown preserves caller-owned and permanent process registrations.");
                Console.WriteLine($"Run {pass}: {backend} / {scene.Driver} / {scene.Adapter}; red pixel read back; scene and renderer retired.");
            }
        }
        finally
        {
            Engine.UnregisterSingleton(Primary);
        }
        Check(!shared.IsDisposed && Engine.GetSingletonList().SequenceEqual(baseline), "Explicit removal leaves the owner responsible after repeated runs.");
    }

    private static void RemoveIfRegistered(string name)
    {
        if (Engine.HasSingleton(name))
            Engine.UnregisterSingleton(name);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action operation) where T : Exception => Reject(typeof(T), operation);

    private static void Reject(Type expected, Action operation)
    {
        try { operation(); }
        catch (Exception error) when (error.GetType() == expected) { return; }

        throw new InvalidOperationException($"Expected {expected.Name}.");
    }

    private sealed class Probe : ElectronObject;

    private sealed class RegistryScene(Window root, Probe shared, string backend) : Entity
    {
        internal bool Rendered { get; private set; }
        internal bool Exited { get; private set; }
        internal string Driver { get; private set; } = "";
        internal string Adapter { get; private set; } = "";

        protected override void OnReady()
        {
            Check(ReferenceEquals(Engine.MainLoop, Tree) && !Engine.HasSingleton(Alias), "Ready observes the published scene loop.");
            Engine.RegisterSingleton(Alias, shared);
            Check(ReferenceEquals(Engine.GetSingleton(Alias), shared), "Ready can register a scene-scoped alias.");
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested native renderer is active.");
            Driver = RenderingServer.GetCurrentRenderingDriverName();
            Adapter = RenderingServer.GetVideoAdapterName();
            RenderingServer.FramePostDraw += Finish;
        }

        protected override void OnDraw()
        {
            Check(ReferenceEquals(Engine.GetSingleton<Probe>(Primary), shared), "Drawing resolves the retained process registration.");
            DrawRect(new(0, 0, 48, 48), Colors.Red);
        }

        private void Finish()
        {
            using var image = root.GetTexture().GetImage();
            Check(image is not null && image.GetPixel(16, 16).R >= 0.95f && image.GetPixel(16, 16).G <= 0.05f,
                  "The native backend must deliver the scene's red rectangle.");
            Rendered = true;
            Tree!.Quit();
        }

        protected override void OnExitTree()
        {
            RenderingServer.FramePostDraw -= Finish;
            Check(ReferenceEquals(Engine.GetSingleton<Probe>(Alias), shared), "Scene teardown sees its borrowed alias.");
            Engine.UnregisterSingleton(Alias);
            Exited = true;
        }
    }
}
