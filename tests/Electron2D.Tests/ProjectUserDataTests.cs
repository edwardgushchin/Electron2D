using Electron2D;
using static TextureArrayTests;
using FileAccess = Electron2D.FileAccess;
using IOPath = System.IO.Path;

internal static class ProjectUserDataTests
{
    internal static void Run()
    {
        var project = Directory.CreateTempSubdirectory("electron2d-user-data-");
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (localData.Length == 0) localData = IOPath.GetTempPath();
        var data = OS.GetDataDir();
        var vendor = $"Electron2D-tests-{Guid.NewGuid():N}";
        var customRoot = IOPath.Combine(data, vendor);
        var oldProject = ProjectSettings.ProjectRoot;
        var oldUser = ProjectSettings.UserDataRoot;
        try
        {
            using var source = new ProjectSettingsRegistry(project.FullName, IOPath.Combine(project.FullName, "explicit"));
            Check(!source.Get(ProjectSettings.UseCustomUserDir) && source.Get(ProjectSettings.CustomUserDirName) == "", "Both directory settings have built-in defaults.");
            source.Set(ProjectSettings.ApplicationName, vendor);
            source.Set(ProjectSettings.CustomUserDirName, $"{vendor}/unused");
            source.Save();

            using var automatic = new ProjectSettingsRegistry(project.FullName);
            automatic.Load();
            Check(automatic.UserDataRoot == IOPath.Combine(localData, "Electron2D", vendor),
                "A named project uses the engine namespace by default.");
            source.Set(ProjectSettings.UseCustomUserDir, true);
            source.Set(ProjectSettings.CustomUserDirName, "");
            source.Save();
            automatic.Load();
            Check(automatic.UserDataRoot == customRoot, "An empty custom name uses the application name.");
            source.Set(ProjectSettings.CustomUserDirName, $"{vendor}/Profiles...\\../CON");
            source.SetFeatureOverride(ProjectSettings.CustomUserDirName, "custom_test", $"{vendor}/feature");
            source.Save();
            automatic.Load();
            Check(automatic.UserDataRoot == IOPath.Combine(customRoot, "Profiles", "-", "_CON"),
                "Nested custom paths normalize separators, trailing periods, traversal and reserved names.");
            automatic.AddCustomFeature("custom_test");
            automatic.Load();
            Check(automatic.UserDataRoot == IOPath.Combine(customRoot, "feature"), "Active features select a custom data directory.");
            source.Load();
            Check(source.UserDataRoot == IOPath.Combine(project.FullName, "explicit"), "An explicitly configured path wins over the loaded settings.");

            VerifyProcess(project.FullName, source, vendor, customRoot);
        }
        finally
        {
            ProjectSettings.ConfigurePaths(oldProject, oldUser);
            if (Directory.Exists(customRoot)) Directory.Delete(customRoot, recursive: true);
            project.Delete(recursive: true);
        }

        Console.WriteLine("Automatic user data: settings, normalized names, feature overrides, explicit roots, user override lookup and retained I/O passed.");
    }

    private static void VerifyProcess(string project, ProjectSettingsRegistry source, string vendor, string customRoot)
    {
        var before = IOPath.Combine(customRoot, "before");
        var after = IOPath.Combine(customRoot, "after");
        source.Set(ProjectSettings.CustomUserDirName, $"{vendor}/before");
        source.Set(ProjectSettings.ProjectSettingsOverride, "user://profile.cfg");
        source.Save();
        Directory.CreateDirectory(before);
        using var overrideFile = new ConfigFile();
        overrideFile.SetValue(new ConfigKey<string>("application", "config/custom_user_dir_name"), $"{vendor}/after");
        overrideFile.Save(IOPath.Combine(before, "profile.cfg"));

        ProjectSettings.ConfigurePaths(project);
        ProjectSettings.Load();
        Check(OS.GetUserDataDir() == after && ProjectSettings.UserDataRoot == after &&
            ProjectSettings.GlobalizePath("user://slot.txt") == IOPath.Combine(after, "slot.txt"),
            "The configured override is found in the candidate root and the final root publishes with the settings.");
        Check(!Directory.Exists(after), "Resolving user data creates no directories.");
        DirAccess.MakeDirRecursiveAbsolute("user://");
        using (var file = FileAccess.Open("user://slot.txt", FileAccessModeFlags.Write))
            file.WriteString("save in final root");
        Check(File.ReadAllText(IOPath.Combine(after, "slot.txt")) == "save in final root", "Actual save goes to the final root.");
        using var oldDirectory = DirAccess.Open("user://");

        File.WriteAllText(IOPath.Combine(before, "profile.cfg"), "[application]\nconfig/custom_user_dir_name=false");
        var version = ProjectSettings.Version;
        Reject<InvalidDataException>(ProjectSettings.Load);
        Check(ProjectSettings.UserDataRoot == after && ProjectSettings.Version == version &&
            FileAccess.GetFileAsString("user://slot.txt") == "save in final root",
            "A failed load preserves the selected directory, document and filesystem access.");

        File.Delete(IOPath.Combine(before, "profile.cfg"));
        ProjectSettings.Load();
        Check(OS.GetUserDataDir() == before && oldDirectory.FileExists("slot.txt") &&
            !FileAccess.FileExists("user://slot.txt"), "Reload selects the new root, while open directories retain their original scope.");
        source.Set(ProjectSettings.CustomUserDirName, $"{vendor}/../relative");
        source.Save();
        ProjectSettings.Load();
        Check(OS.GetUserDataDir() == IOPath.Combine(customRoot, "-", "relative"), "Reload also consumes changes in the main file.");
    }
}
