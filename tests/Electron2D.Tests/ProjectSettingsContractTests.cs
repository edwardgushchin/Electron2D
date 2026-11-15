using Electron2D;

internal static class ProjectSettingsContractTests
{
    internal static void Run()
    {
        var root = Directory.CreateTempSubdirectory("e2d-settings-");
        try
        {
            using var registry = new ProjectSettingsRegistry(root.FullName);
            VerifyValues(registry);
            VerifyDelivery(registry);
            VerifyEngineDelivery();
            Console.WriteLine("ProjectSettings public values, registration, concurrent delivery and Engine frame checks passed.");
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private static void VerifyValues(ProjectSettingsRegistry registry)
    {
        var source = new[] { 1 };
        var setting = new ProjectSetting<int[]>("contract/values", source, value => value is [> 0]);
        source[0] = 0;
        registry.Register(setting);
        Check(registry.Get(setting) is [1], "The default must be snapshotted at construction.");
        var impostor = new ProjectSetting<string>(setting.Name, "other");
        Check(!registry.HasSetting(impostor), "An exact name with another value type is not the registered identity.");
        Reject<InvalidOperationException>(() => registry.Get(impostor));
        Reject<InvalidOperationException>(() => registry.Set(impostor, null!));
        Reject<InvalidOperationException>(() => registry.SetInitialValue(impostor, "other"));
        Reject<InvalidOperationException>(() => registry.Reset(impostor));
        Reject<InvalidOperationException>(() => registry.Unregister(impostor));

        var version = registry.Version;
        var initial = new[] { 2 };
        Reject<ArgumentOutOfRangeException>(() => registry.SetInitialValue(setting, [0]));
        registry.SetInitialValue(setting, initial);
        initial[0] = 0;
        Check(registry.Get(setting) is [1] && registry.Version == version && !registry.FlushChanges(),
            "Changing the initial value must preserve an implicit current value and notification state.");
        registry.Reset(setting);
        Check(registry.Get(setting) is [2] && registry.FlushChanges(), "Reset must expose the independent initial snapshot.");
        var value = new[] { 3 };
        registry.Set(setting, value);
        value[0] = 0;
        registry.Get(setting)[0] = 0;
        Check(registry.Get(setting) is [3], "Writes and reads must not expose mutable stored values.");

        var guardedLateRegistration = false;
        var late = new ProjectSetting<int>("contract/late", 1, number =>
        {
            if (number == 2)
            {
                Reject<InvalidOperationException>(() => registry.Reset(setting));
                Reject<InvalidOperationException>(() => registry.Unregister(setting));
                guardedLateRegistration = true;
                registry.Set(setting, [99]);
            }
            return true;
        });
        using var file = new ConfigFile();
        var baseKey = new ConfigKey<int>("contract", "late");
        var overrideKey = new ConfigKey<string>("contract", "late.probe");
        file.SetValue(baseKey, 2);
        file.SetValue(overrideKey, "wrong type");
        file.Save(registry.ProjectFilePath);
        registry.Load();
        registry.Set(setting, [3]);
        version = registry.Version;
        var nextOrder = registry.GetOrder(setting) + 1;
        Reject<InvalidOperationException>(() => registry.Register(late));
        Check(guardedLateRegistration, "Reset and Unregister must reject mutation during registration validation.");
        var valid = new ProjectSetting<int>(late.Name, 1);
        Reject<InvalidDataException>(() => registry.Register(valid));
        Check(!registry.HasSetting(late) && registry.Version == version && registry.Get(setting) is [3] &&
              registry.GetChangedSettings().SequenceEqual([setting.Name]),
            "Failed preloaded registration must preserve values, version and pending names.");
        file.SetValue(new ConfigKey<int>("contract", "late.probe"), 3);
        file.Save(registry.ProjectFilePath);
        registry.Load();
        registry.Register(valid);
        Check(registry.GetOrder(valid) == nextOrder && registry.Get(valid) == 2 &&
              registry.GetWithOverride(valid, ["probe"]) == 3 && !registry.FlushChanges(),
            "Validated preloaded values must publish together without consuming order on failure.");
        registry.SetInitialValue(valid, 2);
        registry.Unregister(valid);
        registry.Save();
        using var saved = new ConfigFile();
        saved.Load(registry.ProjectFilePath);
        Check(!saved.HasSectionKey(baseKey) && !saved.HasSectionKey(overrideKey) &&
              registry.GetChangedSettings().SequenceEqual([valid.Name]), "Unregister removes base and overrides and queues the name.");
        registry.FlushChanges();

        file.SetValue(baseKey, 1);
        file.Save(registry.ProjectFilePath);
        registry.Load();
        registry.Register(valid);
        Reject<DirectoryNotFoundException>(() =>
            registry.SaveCustom(System.IO.Path.Combine(registry.ProjectRoot, "missing", "project.cfg")));
        registry.Reset(valid);
        Check(registry.Get(valid) == 1 && registry.GetChangedSettings().SequenceEqual([valid.Name]),
            "A failed save must not remove an explicit preloaded base before Reset can observe it.");
        registry.FlushChanges();
    }

    private static void VerifyDelivery(ProjectSettingsRegistry registry)
    {
        var first = new ProjectSetting<int>("contract/first", 0);
        var second = new ProjectSetting<int>("contract/second", 0);
        registry.Register(first);
        registry.Register(second);
        registry.Set(first, 1);
        registry.Set(first, 2);
        var before = registry.GetChangedSettings();
        var expectedFailure = new InvalidOperationException("Expected settings handler failure.");
        var secondHandlerCalls = 0;
        void Fail(ProjectSettingsRegistry sender)
        {
            Check(ReferenceEquals(sender, registry) && before.SequenceEqual([first.Name]), "Delivery retains sender and coalesces names.");
            var edit = Task.Run(() =>
            {
                sender.Set(second, 1);
                sender.Set(first, 3);
                return sender.FlushChanges();
            });
            Check(edit.Wait(TimeSpan.FromSeconds(10)) && !edit.Result, "Concurrent edits complete outside the callback lock without recursive delivery.");
            Check(sender.GetChangedSettings().SequenceEqual([first.Name, second.Name]) &&
                  sender.CheckChangedSettingsInGroup("contract/s") && !sender.CheckChangedSettingsInGroup("Contract"),
                "Both queries see the same ordinal union during delivery.");
            throw expectedFailure;
        }

        void Observe(ProjectSettingsRegistry _) => secondHandlerCalls++;
        registry.SettingsChanged += Fail;
        registry.SettingsChanged += Observe;
        Check(ReferenceEquals(Reject<InvalidOperationException>(() => registry.FlushChanges()), expectedFailure) &&
              secondHandlerCalls == 0, "A callback failure preserves normal multicast exception behavior.");
        registry.SettingsChanged -= Fail;
        Check(registry.GetChangedSettings().SequenceEqual([first.Name, second.Name]) &&
              registry.FlushChanges() && secondHandlerCalls == 1 && !registry.FlushChanges() &&
              before.SequenceEqual([first.Name]), "The next batch survives failure and earlier snapshots remain stable.");
        registry.Dispose();
        registry.SettingsChanged -= Observe;
        Reject<ObjectDisposedException>(() => registry.SettingsChanged += Observe);
        Reject<ObjectDisposedException>(() => registry.GetChangedSettings());
        Reject<ObjectDisposedException>(() => registry.CheckChangedSettingsInGroup(""));
        Reject<ObjectDisposedException>(() => registry.FlushChanges());
    }

    private static void VerifyEngineDelivery()
    {
        using var loop = new SettingsLoop();
        ProjectSettings.FlushChanges();
        ProjectSettings.Register(loop.Setting);
        var sender = Engine.GetSingleton<ProjectSettings>(nameof(ProjectSettings));
        var calls = 0;
        void Changed(ProjectSettingsRegistry actual)
        {
            Check(ReferenceEquals(actual, sender) && ProjectSettings.GetChangedSettings().SequenceEqual([loop.Setting.Name]) &&
                  ProjectSettings.CheckChangedSettingsInGroup("contract/") && ProjectSettings.Get(loop.Setting) == 2,
                "Static queries must observe the completed Process callback and the retained sender.");
            calls++;
        }

        ProjectSettings.SettingsChanged += Changed;
        Engine.Start(loop);
        try
        {
            Reject<InvalidOperationException>(() => Engine.AdvanceFrame(0));
            Check(calls == 0 && ProjectSettings.GetChangedSettings().SequenceEqual([loop.Setting.Name]),
                "A failed process callback must leave its pending changes queued.");
            Engine.AdvanceFrame(0);
            Engine.AdvanceFrame(0);
            Check(calls == 1 && !ProjectSettings.CheckChangedSettingsInGroup(""),
                "One successful frame flushes the batch and an idle frame emits nothing.");
            ProjectSettings.SetInitialValue(loop.Setting, 5);
            Check(ProjectSettings.HasSetting(loop.Setting) && ProjectSettings.Get(loop.Setting) == 2, "Static initial changes preserve the current value.");
            ProjectSettings.Reset(loop.Setting);
            Check(ProjectSettings.Get(loop.Setting) == 5, "Static Reset exposes the updated initial value.");
        }
        finally
        {
            Engine.Stop();
            ProjectSettings.SettingsChanged -= Changed;
            ProjectSettings.Unregister(loop.Setting);
            ProjectSettings.FlushChanges();
        }
    }

    private sealed class SettingsLoop : MainLoop
    {
        internal readonly ProjectSetting<int> Setting = new("contract/engine", 0);
        private int _frames;

        protected override bool OnProcess(double delta)
        {
            _frames++;
            if (_frames <= 2)
                ProjectSettings.Set(Setting, _frames);
            if (_frames == 1)
                throw new InvalidOperationException("Expected process failure.");
            return false;
        }
    }

    private static T Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T error) { return error; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
