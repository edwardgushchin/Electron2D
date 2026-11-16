using Electron2D;
using IOPath = System.IO.Path;

internal static class ProjectPathTests
{
    internal static void Run()
    {
        var directory = Directory.CreateTempSubdirectory("e2d-paths-");
        var oldProject = ProjectSettings.ProjectRoot;
        var oldUser = ProjectSettings.UserDataRoot;
        try
        {
            var project = Directory.CreateDirectory(IOPath.Combine(directory.FullName, "project")).FullName;
            var user = IOPath.Combine(directory.FullName, "user");
            using var registry = new ProjectSettingsRegistry(project, user);
            var version = registry.Version;
            Verify(registry.GlobalizePath, registry.LocalizePath, project, user);
            Check(registry.GetChangedSettings().Count == 0 && registry.Version == version && !Directory.Exists(user), "Conversion must not create directories or settings changes.");
            ProjectSettings.ConfigurePaths(project, user);
            version = ProjectSettings.Version;
            Verify(ProjectSettings.GlobalizePath, ProjectSettings.LocalizePath, project, user);
            Check(ProjectSettings.Version == version && ProjectSettings.GetChangedSettings().Count == 0, "Static conversion must be observational.");
            var nested = IOPath.Combine(project, "nested");
            registry.ConfigurePaths(project, nested);
            Check(registry.LocalizePath("res://nested/file") == "user://file", "The deeper user root must win.");
            Directory.CreateDirectory(nested);
            registry.ConfigurePaths(nested, project);
            Check(registry.LocalizePath("user://nested/file") == "res://file", "The deeper project root must win.");
            Reject<ArgumentException>(() => registry.ConfigurePaths(project, project + IOPath.DirectorySeparatorChar));
            Check(registry.GlobalizePath("res://file") == IOPath.Combine(nested, "file"), "Rejected coincident roots must preserve configuration.");
            using (var volume = new ProjectSettingsRegistry(IOPath.GetPathRoot(project)!, user))
                Check(volume.LocalizePath(volume.GlobalizePath("res://")) == "res://", "Native volume roots must round trip.");
            registry.ConfigurePaths(project, user);
            Parallel.Invoke(
                () => { for (var i = 0; i < 10000; i++) registry.ConfigurePaths(i % 2 == 0 ? project : nested, user); },
                () => { for (var i = 0; i < 10000; i++) Check(registry.LocalizePath("res://file") == "res://file", "Localization must use one root pair during reconfiguration."); });
            registry.Dispose();
            Reject<ObjectDisposedException>(() => registry.GlobalizePath("res://file"));
            Reject<ObjectDisposedException>(() => registry.LocalizePath("res://file"));
            Console.WriteLine("Project path matrix: static/independent, nested/rejected coincident roots, reconfiguration, disposal passed.");
        }
        finally
        {
            ProjectSettings.ConfigurePaths(oldProject, oldUser);
            directory.Delete(true);
        }
    }

    private static void Verify(Func<string, string> global, Func<string, string> local, string project, string user)
    {
        foreach (var (prefix, root) in new[] { ("res://", project), ("user://", user) })
        {
            foreach (var (suffix, expected) in new[] { ("", ""), (".", ""), ("a/../missing", "missing"), ("a\\..\\missing", "missing"), ("a//b", "a/b") })
            {
                var absolute = IOPath.GetFullPath(IOPath.Combine(root, expected.Replace('/', IOPath.DirectorySeparatorChar)));
                Check(global(prefix + suffix) == absolute, "Virtual normalization failed.");
                Check(local(prefix + suffix) == prefix + expected && local(absolute) == prefix + expected, "Reverse conversion failed.");
                Check(global(local(absolute)) == absolute, "Round trip failed.");
            }
            foreach (var suffix in new[] { "../escape", "..\\escape", "/absolute", "\\absolute" })
            {
                Reject<UnauthorizedAccessException>(() => global(prefix + suffix));
                Reject<UnauthorizedAccessException>(() => local(prefix + suffix));
            }
            if (OperatingSystem.IsWindows())
                Reject<UnauthorizedAccessException>(() => global(prefix + "C:\\absolute"));
            Check(local(root + IOPath.DirectorySeparatorChar) == prefix, "Root trailing separators must localize.");
            if (OperatingSystem.IsWindows())
                Check(local(root.ToUpperInvariant()) == prefix, "Windows root matching must ignore case.");
            var sibling = root + "-sibling";
            Check(local(sibling) == IOPath.GetFullPath(sibling), "Sibling prefix must remain outside.");
        }
        Check(global("missing/../file") == IOPath.GetFullPath("file"), "Relative paths must use the process working directory.");
        Check(local("missing/../file") == local(IOPath.GetFullPath("file")), "Relative localization must use the same native normalization.");
        foreach (var convert in new[] { global, local })
        {
            Reject<ArgumentNullException>(() => convert(null!));
            Reject<ArgumentException>(() => convert(""));
            Reject<NotSupportedException>(() => convert("remote://file"));
        }
        var id = ResourceUID.CreateID();
        ResourceUID.AddID(id, "res://missing");
        try
        {
            var uid = ResourceUID.IDToText(id);
            Check(global(uid) == IOPath.Combine(project, "missing"), "Registered UID globalization failed.");
            Reject<NotSupportedException>(() => local(uid));
        }
        finally { ResourceUID.RemoveID(id); }
        Reject<KeyNotFoundException>(() => global(ResourceUID.IDToText(id)));
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
