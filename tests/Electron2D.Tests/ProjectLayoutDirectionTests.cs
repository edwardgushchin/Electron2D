using System.Globalization;
using Electron2D;
using IOPath = System.IO.Path;

internal static class ProjectLayoutDirectionTests
{
    internal static void Run()
    {
        var rootMode = ProjectSettings.Get(ProjectSettings.RootNodeLayoutDirection);
        var force = ProjectSettings.Get(ProjectSettings.ForceRightToLeftLayoutDirection);
        var culture = TranslationServer.Culture;
        var system = CultureInfo.CurrentUICulture;
        const string domainName = "project-layout-direction-tests";
        try
        {
            Persistence();
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar");
            var domain = TranslationServer.GetOrAddDomain(domainName);
            using var catalog = new Translation { Locale = "ar" };
            domain.AddTranslation(catalog);
            ProjectSettings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, false);
            for (var mode = 0; mode < 4; mode++)
            {
                ProjectSettings.Set(ProjectSettings.RootNodeLayoutDirection, mode);
                using var root = new TestViewport { TranslationDomain = domainName };
                var control = new Control { Position = new(10, 4), Size = new(20, 10) };
                root.AddChild(control); control.SetBegin(new(10, 4)); control.SetEnd(new(30, 14));
                using var tree = new SceneTree(root);
                var expected = mode is 2 or 3;
                Check(control.IsLayoutRTL() == expected && control.Position.X == (expected ? 170 : 10), "Root policy drives actual control rectangles before ready.");
                ProjectSettings.Set(ProjectSettings.RootNodeLayoutDirection, expected ? 1 : 2);
                ProjectSettings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, true);
                Check(control.IsLayoutRTL() == expected, "Each tree retains its project-policy snapshot.");
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 128; i++) Check(control.IsLayoutRTL() == expected, "Warmed direction stays stable.");
                Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed direction queries allocate no managed bytes.");
                ProjectSettings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, false);
            }
            ProjectSettings.Set(ProjectSettings.RootNodeLayoutDirection, 1);
            using var window = new Window { TranslationDomain = domainName, LayoutDirection = LayoutDirection.RTL };
            using var bridge = new Entity();
            var child = new Control(); window.AddChild(bridge); bridge.AddChild(child);
            Check(child.IsLayoutRTL(), "Inheritance crosses ordinary nodes to the same-domain window.");
            child.TranslationDomain = domainName + "-boundary";
            Check(!child.IsLayoutRTL(), "A domain boundary falls back to project root policy.");
            child.SetTranslationDomainInherited();
            Check(child.IsLayoutRTL(), "Restoring domain inheritance restores the window direction.");
            foreach (var direction in new[] { LayoutDirection.Inherited, LayoutDirection.ApplicationLocale, LayoutDirection.LTR, LayoutDirection.RTL, LayoutDirection.SystemLocale })
            {
                child.LayoutDirection = direction; window.LayoutDirection = direction;
                ProjectSettings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, true);
                Check(child.IsLayoutRTL() == (direction != LayoutDirection.LTR) && window.IsLayoutRTL() == (direction != LayoutDirection.LTR), "Forced policy is shared by windows and controls, except explicit LTR.");
                ProjectSettings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, false);
            }
            ProjectSettings.Set(ProjectSettings.RootNodeLayoutDirection, 0);
            using var viewport = new TestViewport { TranslationDomain = domainName };
            var parent = new Control { Position = new(10, 4), Size = new(100, 40) };
            var leaf = new Control { Position = new(5, 2), Size = new(20, 10) };
            viewport.AddChild(parent); parent.AddChild(leaf);
            using var active = new SceneTree(viewport);
            TranslationServer.Culture = CultureInfo.GetCultureInfo("ar");
            viewport.PropagateNotification(Node.NotificationTranslationChanged);
            Check(parent.Position.X == 90 && leaf.Position.X == 75, "Translation notifications reflow the whole inherited subtree.");
            parent.LayoutDirection = LayoutDirection.LTR;
            Check(parent.Position.X == 10 && leaf.Position.X == 5, "Explicit changes reflow descendants immediately.");
            using var packed = new PackedScene(); packed.Pack(parent);
            using var copy = (Control)packed.Instantiate();
            Check(copy.LayoutDirection == LayoutDirection.LTR, "Packed scenes preserve authored direction, independently of project defaults.");
            var wrongThread = Task.Run(() => Reject<InvalidOperationException>(() => parent.IsLayoutRTL())); wrongThread.GetAwaiter().GetResult();
            using var disposed = new Control(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.IsLayoutRTL());
            Check(!Enum.GetNames<LayoutDirection>().Contains("Locale"), "Only current direction names are public.");
            domain.LocaleOverride = "en"; viewport.PropagateNotification(Node.NotificationTranslationChanged);
            leaf.LayoutDirection = LayoutDirection.ApplicationLocale;
            Check(!leaf.IsLayoutRTL(), "Application locale resolves the node's domain override.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RootNodeLayoutDirection, rootMode);
            ProjectSettings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, force);
            TranslationServer.Culture = culture; CultureInfo.CurrentUICulture = system;
            TranslationServer.RemoveDomain(domainName); TranslationServer.RemoveDomain(domainName + "-boundary");
        }
    }

    private static void Persistence()
    {
        var directory = IOPath.Combine(IOPath.GetTempPath(), "electron2d-layout-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(IOPath.Combine(directory, "project"));
        Directory.CreateDirectory(IOPath.Combine(directory, "user"));
        try
        {
            using (var settings = new ProjectSettingsRegistry(IOPath.Combine(directory, "project"), IOPath.Combine(directory, "user")))
            {
                Check(settings.Get(ProjectSettings.RootNodeLayoutDirection) == 0 && !settings.Get(ProjectSettings.ForceRightToLeftLayoutDirection), "Permanent setting defaults are registered.");
                Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.RootNodeLayoutDirection, -1));
                Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.RootNodeLayoutDirection, 4));
                Reject<InvalidOperationException>(() => settings.Unregister(ProjectSettings.RootNodeLayoutDirection));
                Reject<InvalidOperationException>(() => settings.Unregister(ProjectSettings.ForceRightToLeftLayoutDirection));
                settings.Set(ProjectSettings.RootNodeLayoutDirection, 2); settings.Set(ProjectSettings.ForceRightToLeftLayoutDirection, true);
                settings.SetFeatureOverride(ProjectSettings.RootNodeLayoutDirection, "layout-test", 3);
                settings.SetFeatureOverride(ProjectSettings.ForceRightToLeftLayoutDirection, "layout-test", false); settings.Save();
            }
            using var loaded = new ProjectSettingsRegistry(IOPath.Combine(directory, "project"), IOPath.Combine(directory, "user")); loaded.Load();
            Check(loaded.Get(ProjectSettings.RootNodeLayoutDirection) == 2 && loaded.Get(ProjectSettings.ForceRightToLeftLayoutDirection), "Settings survive save and load.");
            Check(loaded.GetWithOverride(ProjectSettings.RootNodeLayoutDirection, ["layout-test"]) == 3 && !loaded.GetWithOverride(ProjectSettings.ForceRightToLeftLayoutDirection, ["layout-test"]), "Both settings retain feature overrides.");
        }
        finally { Directory.Delete(directory, true); }
    }

    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(Vector2.Zero, new(200, 100)); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
