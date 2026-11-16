using Electron2D;

internal static class ProjectFeatureQueryTests
{
    internal static void Run()
    {
        var root = Directory.CreateTempSubdirectory("e2d-features-");
        try
        {
            using var registry = new ProjectSettingsRegistry(root.FullName);
            var setting = new ProjectSetting<int[]>("features/query", [1]);
            registry.Register(setting);
            Check(registry.GetWithOverride(setting) is [1], "Default active query.");
            Check(registry.GetWithOverride(setting, []) is [1], "Default explicit query.");
            registry.Set(setting, [2]);
            registry.SetFeatureOverride(setting, "First", [3]);
            registry.SetFeatureOverride(setting, "second", [4]);
            registry.SetFeatureOverride(setting, "FIRST", [5]);
            foreach (string[] features in new string[][] { ["first", "second"], ["SECOND", "FIRST"], ["second", "first", "first"] })
                Check(registry.GetWithOverride(setting, features) is [5], "Stored order dominates request order and duplicates.");
            registry.AddCustomFeature("SECOND");
            Check(registry.GetWithOverride(setting) is [4], "Custom activation.");
            registry.AddCustomFeature("FIRST");
            Check(registry.GetWithOverride(setting) is [5], "Competing active features.");
            Check(registry.GetWithOverride(setting, []) is [2], "Empty explicit set replaces active features.");
            registry.GetWithOverride(setting)[0] = 99;
            registry.GetWithOverride(setting, ["first"])[0] = 99;
            Check(registry.GetWithOverride(setting) is [5], "Both reads return independent mutable snapshots.");
            registry.Save();
            using (var loaded = new ProjectSettingsRegistry(root.FullName))
            {
                loaded.Register(setting);
                loaded.Load();
                Check(loaded.GetWithOverride(setting, ["second", "first"]) is [5], "Saved order survives load.");
                Check(loaded.GetWithOverride(setting) is [2], "Custom activation is registry-local.");
            }
            registry.ClearFeatureOverride(setting, "first");
            registry.SetFeatureOverride(setting, "first", [6]);
            Check(registry.GetWithOverride(setting, ["first", "second"]) is [4], "Reinserted override moves behind surviving entries.");
            registry.RemoveCustomFeature("second");
            Check(registry.GetWithOverride(setting) is [6], "Live custom removal.");
            registry.RemoveCustomFeature("first");
            Check(registry.GetWithOverride(setting) is [2], "Base fallback.");
            var builtin = registry.GetActiveFeatures().First();
            registry.SetFeatureOverride(setting, builtin, [7]);
            Check(registry.GetWithOverride(setting) is [7] && registry.GetWithOverride(setting, []) is [2], "Built-in activation is absent from explicit empty lookup.");
            registry.FlushChanges();
            var version = registry.Version;
            var notifications = 0;
            registry.SettingsChanged += _ => notifications++;
            Reject<ArgumentNullException>(() => registry.GetWithOverride<int[]>(null!));
            Reject<ArgumentNullException>(() => registry.GetWithOverride(setting, null!));
            Reject<ArgumentNullException>(() => registry.GetWithOverride(setting, [null!]));
            foreach (var invalid in new[] { "", " first", "first ", ".first", "a..b", "é" })
                Reject<ArgumentException>(() => registry.GetWithOverride(setting, [invalid]));
            foreach (var impostor in new[] { new ProjectSetting<int[]>(setting.Name, [1]), new ProjectSetting<int[]>("features/missing", [1]) })
            {
                if (impostor.Name == setting.Name)
                {
                    Reject<InvalidOperationException>(() => registry.GetWithOverride(impostor));
                    Reject<InvalidOperationException>(() => registry.GetWithOverride(impostor, []));
                }
                else
                {
                    Reject<KeyNotFoundException>(() => registry.GetWithOverride(impostor));
                    Reject<KeyNotFoundException>(() => registry.GetWithOverride(impostor, []));
                }
            }
            Reject<InvalidOperationException>(() => registry.GetWithOverride(new ProjectSetting<string>(setting.Name, "wrong"), []));
            Reject<ApplicationException>(() => registry.GetWithOverride(setting, FailingFeatures()));
            Parallel.For(0, 1000, _ =>
            {
                Check(registry.GetWithOverride(setting) is [7], "Concurrent active reads.");
                Check(registry.GetWithOverride(setting, ["first"]) is [6], "Concurrent explicit reads.");
            });
            Check(registry.Version == version && !registry.FlushChanges() && notifications == 0, "Successful and failed reads do not mutate or notify.");
            Check(registry.GetWithOverride(setting, ReentrantFeatures(registry, setting)) is [8], "Enumeration occurs outside selection lock and selection sees committed reentrant changes.");
            var writer = Task.Run(() => { for (var i = 0; i < 1000; i++) registry.SetFeatureOverride(setting, "first", [i % 2 + 8]); });
            for (var i = 0; i < 1000; i++)
                Check(registry.GetWithOverride(setting, ["first"]) is [8] or [9], "Concurrent writes publish whole snapshots.");
            writer.GetAwaiter().GetResult();
            registry.Dispose();
            Reject<ObjectDisposedException>(() => registry.GetWithOverride(setting));
            Reject<ObjectDisposedException>(() => registry.GetWithOverride(setting, []));
            VerifyGlobal();
            Console.WriteLine("Feature query selection, snapshots, persistence, failures, reentry, concurrency and Engine passed.");
        }
        finally { root.Delete(recursive: true); }
    }

    private static void VerifyGlobal()
    {
        var setting = new ProjectSetting<int[]>("features/public", [1]);
        ProjectSettings.Register(setting);
        var timing = ProjectSettings.PhysicsTicksPerSecond;
        var previous = ProjectSettings.Get(timing);
        try
        {
            Check(ProjectSettings.GetWithOverride(setting) is [1] && ProjectSettings.GetWithOverride(setting, []) is [1], "Public default queries.");
            ProjectSettings.Set(setting, [2]);
            ProjectSettings.SetFeatureOverride(setting, "query-test", [3]);
            ProjectSettings.AddCustomFeature("QUERY-TEST");
            ProjectSettings.GetWithOverride(setting)[0] = 99;
            Check(ProjectSettings.GetWithOverride(setting) is [3] && ProjectSettings.GetWithOverride(setting, []) is [2] &&
                ProjectSettings.GetWithOverride(setting, ["QUERY-TEST", "query-test"]) is [3], "Public activation, normalization and independent snapshots.");
            ProjectSettings.Set(timing, 60);
            ProjectSettings.SetFeatureOverride(timing, "query-test", 120);
            Check(Engine.PhysicsTicksPerSecond == 120, "Engine consumes active override.");
            ProjectSettings.RemoveCustomFeature("query-test");
            Check(Engine.PhysicsTicksPerSecond == 60 && ProjectSettings.GetWithOverride(setting) is [2], "Engine observes active feature removal.");
        }
        finally
        {
            ProjectSettings.RemoveCustomFeature("query-test");
            ProjectSettings.ClearFeatureOverride(timing, "query-test");
            ProjectSettings.Set(timing, previous);
            ProjectSettings.Unregister(setting);
            ProjectSettings.FlushChanges();
        }
    }

    private static IEnumerable<string> FailingFeatures()
    {
        yield return "first";
        throw new ApplicationException("enumerator failure");
    }

    private static IEnumerable<string> ReentrantFeatures(ProjectSettingsRegistry registry, ProjectSetting<int[]> setting)
    {
        registry.SetFeatureOverride(setting, "first", [8]);
        Check(registry.GetWithOverride(setting, []) is [2], "Enumerator can reenter reads.");
        yield return "first";
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
