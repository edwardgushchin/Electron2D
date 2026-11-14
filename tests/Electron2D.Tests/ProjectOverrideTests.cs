using Electron2D;
using static TextureArrayTests;
using IOPath = System.IO.Path;

internal static class ProjectOverrideTests
{
    internal static void Run(bool packed = false)
    {
        var project = Directory.CreateTempSubdirectory("electron2d-project-overrides-");
        var user = project.CreateSubdirectory("user");
        var pathKey = new ConfigKey<string>("application", "config/project_settings_override");
        var disableKey = new ConfigKey<bool>("application", "config/disable_project_settings_override");
        var featureKey = new ConfigKey<bool>("application", "config/disable_project_settings_override.overrides_test");
        var nameKey = new ConfigKey<string>("application", "config/name");

        try
        {
            using var registry = new ProjectSettingsRegistry(project.FullName, user.FullName);
            Check(registry.Get(ProjectSettings.ProjectSettingsOverride) == "" &&
                !registry.Get(ProjectSettings.DisableProjectSettingsOverride), "Automatic override defaults.");
            using var projectFile = new ConfigFile();
            using var conventional = new ConfigFile();
            using var configured = new ConfigFile();
            var customPath = IOPath.Combine(user.FullName, "profile.cfg");

            projectFile.SetValue(nameKey, "project");
            projectFile.SetValue(pathKey, "missing.cfg");
            projectFile.Save(registry.ProjectFilePath);
            conventional.SetValue(nameKey, "conventional");
            conventional.SetValue(pathKey, "user://profile.cfg");
            conventional.Save(registry.OverrideFilePath);
            configured.SetValue(nameKey, "configured");
            configured.SetValue(pathKey, "res://nested.cfg");
            configured.Save(customPath);
            File.WriteAllText(IOPath.Combine(project.FullName, "nested.cfg"), "[application]\nconfig/name=\"nested\"");
            registry.Load();
            Check(registry.Get(ProjectSettings.ApplicationName) == "configured" &&
                registry.Get(ProjectSettings.ProjectSettingsOverride) == "res://nested.cfg",
                "The conventional layer selects the final path. That file is applied once.");

            conventional.SetValue(featureKey, true);
            conventional.Save(registry.OverrideFilePath);
            registry.AddCustomFeature("overrides_test");
            registry.Load();
            Check(registry.Get(ProjectSettings.ApplicationName) == "conventional",
                "The conventional layer can disable the final layer for an active feature.");
            registry.RemoveCustomFeature("overrides_test");

            projectFile.SetValue(disableKey, true);
            projectFile.Save(registry.ProjectFilePath);
            registry.Load();
            Check(registry.Get(ProjectSettings.ApplicationName) == "project", "Base disable skips both automatic layers.");
            registry.LoadCustom("user://profile.cfg");
            Check(registry.Get(ProjectSettings.ApplicationName) == "configured", "Explicit merging remains available.");

            projectFile.SetValue(disableKey, false);
            projectFile.Save(registry.ProjectFilePath);
            File.WriteAllText(customPath, "[application]\nconfig/name=2");
            registry.Set(ProjectSettings.ApplicationName, "unsaved");
            Reject<InvalidDataException>(registry.Load);
            Check(registry.Get(ProjectSettings.ApplicationName) == "unsaved" &&
                registry.GetChangedSettings().Contains(ProjectSettings.ApplicationName.Name), "A failed layer preserves values and pending changes.");
            File.Delete(customPath);
            registry.Load();
            Check(registry.Get(ProjectSettings.ApplicationName) == "conventional" && registry.GetChangedSettings().Count == 0,
                "A missing optional file leaves the earlier layer and successful load clears changes.");

            configured.Save(customPath);
            conventional.SetValue(pathKey, customPath);
            conventional.Save(registry.OverrideFilePath);
            registry.Load();
            Check(registry.Get(ProjectSettings.ApplicationName) == "configured", "An absolute override uses the selected file.");

            conventional.SetValue(pathKey, "../escape.cfg");
            conventional.Save(registry.OverrideFilePath);
            Reject<UnauthorizedAccessException>(registry.Load);
            Check(registry.Get(ProjectSettings.ApplicationName) == "configured", "A relative path cannot escape the project.");

            conventional.SetValue(pathKey, "nested.cfg");
            conventional.Save(registry.OverrideFilePath);
            registry.Load();
            Check(registry.Get(ProjectSettings.ApplicationName) == "nested", "Relative overrides resolve under the project root.");

            if (packed) VerifyPacked(project.FullName, user.FullName, projectFile, nameKey, pathKey);
            Console.WriteLine($"Project override precedence, features, optional files, rollback and paths passed (packed={packed}).");
        }
        finally { project.Delete(recursive: true); }
    }

    private static void VerifyPacked(string project, string user, ConfigFile projectFile, ConfigKey<string> nameKey, ConfigKey<string> pathKey)
    {
        var resourcePath = "res://user/override-test-" + IOPath.GetFileName(project) + ".cfg";
        projectFile.SetValue(pathKey, resourcePath);
        projectFile.Save(IOPath.Combine(project, ProjectSettings.ProjectFileName));
        File.Delete(IOPath.Combine(project, ProjectSettings.OverrideFileName));
        using var config = new ConfigFile();
        config.SetValue(nameKey, "packed");
        var archive = IOPath.Combine(project, "overrides.pck");
        using (var pack = new PCKPacker())
        {
            pack.PCKStart(archive);
            pack.AddFileFromBuffer(resourcePath, System.Text.Encoding.UTF8.GetBytes(config.EncodeToText()));
            pack.Flush();
        }
        var oldProject = ProjectSettings.ProjectRoot;
        var oldUser = ProjectSettings.UserDataRoot;
        var uid = ResourceUID.CreateID();
        ResourceUID.AddID(uid, resourcePath);
        try
        {
            ProjectSettings.ConfigurePaths(project, user);
            Check(ProjectSettings.LoadResourcePack(archive), "Mount configured override.");
            ProjectSettings.Load();
            Check(ProjectSettings.Get(ProjectSettings.ApplicationName) == "packed", "An explicit res path stays in the resource namespace even within the user-data subtree.");
            using var independent = new ProjectSettingsRegistry(project, user);
            independent.Load();
            Check(independent.Get(ProjectSettings.ApplicationName) == "project", "Independent registries do not inherit process mounts.");

            projectFile.SetValue(pathKey, ResourceUID.IDToText(uid));
            projectFile.Save(ProjectSettings.ProjectFilePath);
            ProjectSettings.Load();
            Check(ProjectSettings.Get(ProjectSettings.ApplicationName) == "packed", "A registered UID can select a packed override.");
        }
        finally
        {
            ProjectSettings.ConfigurePaths(oldProject, oldUser);
            ResourceUID.RemoveID(uid);
        }
    }
}
