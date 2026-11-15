using Electron2D;
using Path = System.IO.Path;

internal static class ProjectSettingsMetadataTests
{
    internal static void Run()
    {
        var root = Directory.CreateTempSubdirectory("e2d-metadata-");
        try
        {
            var user = Path.Combine(root.FullName, "user");
            using var registry = new ProjectSettingsRegistry(root.FullName, user);
            var a = new ProjectSetting<int>("audit/a", 1);
            var b = new ProjectSetting<int>("audit/b", 2);
            registry.Register(b);
            registry.Register(a);
            registry.Save();
            registry.FlushChanges();
            var version = registry.Version;
            var notifications = 0;
            registry.PropertyListChanged += sender =>
            {
                Check(ReferenceEquals(sender, registry), "Exact metadata sender.");
                notifications++;
                registry.SetOrder(a, registry.GetOrder(a)); // Reentry with the committed value terminates.
            };
            registry.SetOrder(a, int.MinValue);
            registry.SetOrder(b, int.MinValue);
            registry.SetAsBasic(a, true);
            registry.SetAsInternal(a, true);
            registry.SetRestartIfChanged(a, true);
            Check(notifications == 5 && registry.Version == version && !registry.FlushChanges(),
                "Metadata changes notify tooling without value version or notification changes.");
            registry.SetOrder(a, int.MinValue);
            registry.SetAsBasic(a, true);
            registry.SetAsInternal(a, true);
            registry.SetRestartIfChanged(a, true);
            Check(notifications == 5 && registry.IsBasic(a) && registry.IsInternal(a) && registry.IsRestartRequired(a),
                "Repeated metadata values are silent.");
            var names = registry.GetSettingNames();
            Check(names.Take(2).SequenceEqual([a.Name, b.Name]) &&
                  !registry.GetSettingNames(false).Contains(a.Name) &&
                  registry.GetPropertyList().All(p => p.Name != a.Name), "Ordinal ties and internal discovery filtering.");
            registry.SetAsInternal(a, false);
            var descriptor = registry.GetPropertyList().OfType<PropertyDescriptor<ProjectSettingsRegistry, int>>()
                .Single(p => p.Name == a.Name);
            registry.SetAsBasic(a, false);
            registry.SetRestartIfChanged(a, false);
            Check(!registry.IsBasic(a) && !registry.IsRestartRequired(a), "Flags can be cleared.");
            using (var independent = new ProjectSettingsRegistry(root.FullName, user))
            {
                independent.Register(a);
                Check(independent.GetOrder(a) != int.MinValue && !independent.IsBasic(a) &&
                      !independent.IsRestartRequired(a), "Metadata is registry-local.");
            }
            foreach (var definition in new[] { new ProjectSetting<int>(a.Name, 1), new ProjectSetting<int>("audit/missing", 1) })
            {
                Action[] operations = [() => registry.GetOrder(definition), () => registry.SetOrder(definition, 0),
                    () => registry.SetAsBasic(definition, true), () => registry.SetAsInternal(definition, true),
                    () => registry.SetRestartIfChanged(definition, true)];
                foreach (var operation in operations)
                    if (definition.Name == a.Name) Reject<InvalidOperationException>(operation);
                    else Reject<KeyNotFoundException>(operation);
            }
            Reject<ArgumentNullException>(() => registry.GetOrder<int>(null!));
            Reject<ArgumentNullException>(() => registry.SetOrder<int>(null!, 0));
            Reject<ArgumentNullException>(() => registry.SetAsBasic<int>(null!, true));
            Reject<ArgumentNullException>(() => registry.SetAsInternal<int>(null!, true));
            Reject<ArgumentNullException>(() => registry.SetRestartIfChanged<int>(null!, true));
            Action<ElectronObject> fail = _ => throw new ApplicationException("metadata listener");
            registry.PropertyListChanged += fail;
            Reject<ApplicationException>(() => registry.SetOrder(a, -99));
            registry.PropertyListChanged -= fail;
            Check(registry.GetOrder(a) == -99, "Listener failure preserves committed metadata.");
            registry.SetOrder(a, int.MinValue);

            using (var unknown = new ConfigFile())
            {
                unknown.SetValue(new ConfigKey<string>("unknown", "entry"), "preserve");
                unknown.Save(Path.Combine(root.FullName, "unknown.cfg"));
            }
            registry.LoadCustom("res://unknown.cfg");
            registry.Set(a, 7);
            registry.SetFeatureOverride(a, "second", 9);
            registry.SetFeatureOverride(a, "first", 10);
            registry.Set(b, 8);
            registry.SetInitialValue(b, 8);
            registry.SetFeatureOverride(b, "second", 8);
            registry.Save();
            var original = File.ReadAllBytes(registry.ProjectFilePath);
            registry.Save();
            Check(original.SequenceEqual(File.ReadAllBytes(registry.ProjectFilePath)), "Repeated save is byte deterministic.");
            using (var saved = new ConfigFile())
            {
                saved.Load(registry.ProjectFilePath);
                Check(saved.GetSectionKeys("audit").SequenceEqual(["a", "a.second", "a.first", "b.second"]) &&
                      saved.GetValue(new ConfigKey<string>("unknown", "entry")) == "preserve",
                    "Save preserves override precedence and unknown entries while omitting current initial values.");
            }
            foreach (var path in new[] { "res://copy.cfg", "user://copy.cfg", Path.Combine(root.FullName, "physical.cfg") })
            {
                if (path.StartsWith("user://", StringComparison.Ordinal)) Directory.CreateDirectory(registry.UserDataRoot);
                registry.SaveCustom(path);
                using var loaded = new ProjectSettingsRegistry(root.FullName, user);
                loaded.Register(a);
                loaded.Register(b);
                loaded.LoadCustom(registry.GlobalizePath(path));
                Check(loaded.Get(a) == 7 && loaded.Get(b) == 2 &&
                      loaded.GetWithOverride(a, ["first", "second"]) == 9,
                    "Custom round-trip uses receiver defaults and stored override precedence.");
            }
            Check(registry.GetChangedSettings().Contains(a.Name), "Save leaves the pending notification intact.");
            registry.Set(a, 11);
            if (OperatingSystem.IsWindows())
            {
                using var locked = new FileStream(registry.ProjectFilePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
                try
                {
                    registry.Save();
                    throw new InvalidOperationException("A locked destination must reject replacement.");
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
                Check(original.SequenceEqual(File.ReadAllBytes(registry.ProjectFilePath)), "Failed replacement preserves previous bytes.");
            }
            Reject<InvalidOperationException>(() => registry.ConfigurePaths(root.FullName));
            Check(registry.Get(a) == 11 && registry.GetChangedSettings().Contains(a.Name) &&
                  !Directory.EnumerateFiles(root.FullName, ".*.tmp").Any(), "Failure preserves live state and removes temporary files.");
            Reject<DirectoryNotFoundException>(() => registry.SaveCustom("res://missing/file.cfg"));
            Reject<UnauthorizedAccessException>(() => registry.SaveCustom("res://../escape.cfg"));
            Reject<ArgumentNullException>(() => registry.SaveCustom(null!));
            Reject<ArgumentException>(() => registry.SaveCustom(""));
            registry.SaveCustom("res://copy.cfg");
            registry.ConfigurePaths(root.FullName); // Successful custom save clears dirty tracking.
            registry.Dispose();
            Reject<ObjectDisposedException>(() => registry.GetOrder(a));
            Reject<ObjectDisposedException>(() => registry.SetOrder(a, 0));
            Reject<ObjectDisposedException>(() => registry.SetAsBasic(a, true));
            Reject<ObjectDisposedException>(() => registry.SetAsInternal(a, true));
            Reject<ObjectDisposedException>(() => registry.SetRestartIfChanged(a, true));
            Reject<ObjectDisposedException>(registry.Save);
            Reject<ObjectDisposedException>(() => registry.SaveCustom("res://copy.cfg"));

            var global = new ProjectSetting<int>("audit/global_metadata", 1);
            ProjectSettings.Register(global);
            try
            {
                ProjectSettings.SetOrder(global, -1);
                ProjectSettings.SetAsBasic(global, true);
                ProjectSettings.SetAsInternal(global, true);
                ProjectSettings.SetRestartIfChanged(global, true);
                Check(ProjectSettings.GetOrder(global) == -1 && ProjectSettings.IsBasic(global) &&
                      ProjectSettings.IsInternal(global) && ProjectSettings.IsRestartRequired(global), "Static service delegates metadata.");
                ProjectSettings.SaveCustom(Path.Combine(root.FullName, "global.cfg"));
            }
            finally { ProjectSettings.Unregister(global); ProjectSettings.FlushChanges(); }
            Console.WriteLine("ProjectSettings metadata and atomic persistence checks passed.");
        }
        finally { root.Delete(recursive: true); }
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
