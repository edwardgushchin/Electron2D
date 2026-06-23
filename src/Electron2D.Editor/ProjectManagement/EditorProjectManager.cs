/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using Electron2D.ProjectSystem;

namespace Electron2D.Editor.ProjectManagement;

internal sealed class EditorProjectManager
{
    private const int MaxRecentProjects = 10;
    private readonly string _templateRoot;

    public EditorProjectManager(string templateRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateRoot);
        _templateRoot = Path.GetFullPath(templateRoot);
    }

    public EditorProjectManagerSmokeResult RunSmoke(string workRoot, string userSettingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(userSettingsPath);

        var creation = CreateProject(new EditorProjectCreateOptions(
            "ProjectManagerSmoke",
            Path.GetFullPath(workRoot),
            Electron2D.Electron2DRendererProfileSetting.Compatibility));
        var openResult = OpenProject(creation.ProjectPath, userSettingsPath);
        if (!openResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, openResult.Diagnostics));
        }

        var sdkCheck = EditorSdkChecker.Check();
        return new EditorProjectManagerSmokeResult(
            openResult.ProjectName,
            openResult.ProjectPath,
            openResult.ProjectSettingsPath,
            openResult.MainScenePath,
            openResult.RendererProfile,
            Path.GetFullPath(userSettingsPath),
            sdkCheck,
            openResult.RecentProjectCount);
    }

    public EditorProjectCreationResult CreateProject(EditorProjectCreateOptions options)
    {
        var projectName = ValidateProjectName(options.ProjectName);
        var projectsRoot = Path.GetFullPath(RequirePath(options.ProjectsRoot, "Projects root"));
        var projectPath = Path.Combine(projectsRoot, projectName);

        ValidateTemplateRoot();
        if (Directory.Exists(projectPath) && Directory.EnumerateFileSystemEntries(projectPath).Any())
        {
            throw new InvalidOperationException($"Project directory is not empty: {projectPath}");
        }

        var createdProject = ProjectTemplateCreator.Create(new ProjectTemplateCreateOptions(
            _templateRoot,
            projectName,
            projectsRoot,
            options.RendererProfile.ToString(),
            InitializeGit: true));

        var projectSettingsPath = createdProject.ProjectSettingsPath;
        var loadResult = Electron2D.Electron2DSettingsStore.LoadProject(projectSettingsPath);
        if (!loadResult.Succeeded || loadResult.Settings is null)
        {
            throw new InvalidOperationException(FormatDiagnostics(loadResult.Diagnostics));
        }

        var mainScenePath = ResolveMainScenePath(projectPath, loadResult.Settings.MainScene);
        return new EditorProjectCreationResult(
            loadResult.Settings.Name,
            projectPath,
            projectSettingsPath,
            mainScenePath,
            loadResult.Settings.RendererProfile);
    }

    public EditorProjectOpenResult OpenProject(string projectPathOrSettingsPath, string userSettingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPathOrSettingsPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(userSettingsPath);

        var (projectPath, projectSettingsPath) = ResolveProjectPaths(projectPathOrSettingsPath);
        var loadResult = Electron2D.Electron2DSettingsStore.LoadProject(projectSettingsPath);
        if (!loadResult.Succeeded || loadResult.Settings is null)
        {
            return EditorProjectOpenResult.Failure(projectPath, projectSettingsPath, FormatDiagnostics(loadResult.Diagnostics));
        }

        var settings = loadResult.Settings;
        var mainScenePath = ResolveMainScenePath(projectPath, settings.MainScene);
        if (!File.Exists(mainScenePath))
        {
            return EditorProjectOpenResult.Failure(
                projectPath,
                projectSettingsPath,
                $"Project main scene was not found: {settings.MainScene}");
        }

        var normalizedProjectPath = Path.GetFullPath(projectPath);
        var userSettings = LoadUserSettingsOrDefault(userSettingsPath);
        userSettings.LastProjectPath = normalizedProjectPath;
        userSettings.RecentProjects = PromoteRecentProject(userSettings.RecentProjects, normalizedProjectPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(userSettingsPath)) ?? ".");
        Electron2D.Electron2DSettingsStore.SaveUser(userSettingsPath, userSettings);

        return EditorProjectOpenResult.Success(
            settings.Name,
            normalizedProjectPath,
            projectSettingsPath,
            mainScenePath,
            settings.RendererProfile,
            userSettings.RecentProjects.Length);
    }

    private static string ValidateProjectName(string projectName)
    {
        var normalized = RequirePath(projectName, "Project name").Trim();
        if (normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Project name contains characters that cannot be used in a directory name.", nameof(projectName));
        }

        return normalized;
    }

    private static string RequirePath(string value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{description} must be a non-empty string.");
        }

        return value;
    }

    private void ValidateTemplateRoot()
    {
        if (!Directory.Exists(_templateRoot))
        {
            throw new InvalidOperationException($"Project template directory was not found: {_templateRoot}");
        }

        var projectSettings = Directory.EnumerateFiles(_templateRoot, "*.e2d", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault() ?? Path.Combine(_templateRoot, Electron2D.ProjectFileLocator.LegacyProjectFileName);
        if (!File.Exists(projectSettings))
        {
            throw new InvalidOperationException($"Project template manifest was not found: {projectSettings}");
        }
    }

    private static (string ProjectPath, string ProjectSettingsPath) ResolveProjectPaths(string projectPathOrSettingsPath)
    {
        var fullPath = Path.GetFullPath(projectPathOrSettingsPath);
        if (Directory.Exists(fullPath))
        {
            return (fullPath, Electron2D.ProjectFileLocator.ResolveProjectFilePath(fullPath));
        }

        if (Electron2D.ProjectFileLocator.IsProjectFilePath(fullPath))
        {
            return (Path.GetDirectoryName(fullPath) ?? ".", fullPath);
        }

        return (fullPath, Electron2D.ProjectFileLocator.ResolveProjectFilePath(fullPath));
    }

    private static string ResolveMainScenePath(string projectPath, string mainScene)
    {
        return Path.GetFullPath(Path.Combine(
            projectPath,
            mainScene.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static Electron2D.Electron2DUserSettings LoadUserSettingsOrDefault(string userSettingsPath)
    {
        var fullPath = Path.GetFullPath(userSettingsPath);
        if (!File.Exists(fullPath))
        {
            return new Electron2D.Electron2DUserSettings();
        }

        var result = Electron2D.Electron2DSettingsStore.LoadUser(fullPath);
        if (result.Succeeded && result.Settings is not null)
        {
            return result.Settings;
        }

        throw new InvalidOperationException(FormatDiagnostics(result.Diagnostics));
    }

    private static string[] PromoteRecentProject(string[] currentProjects, string projectPath)
    {
        var projects = new List<string> { projectPath };
        foreach (var currentProject in currentProjects)
        {
            if (string.IsNullOrWhiteSpace(currentProject) ||
                currentProject.Equals(projectPath, StringComparison.Ordinal))
            {
                continue;
            }

            projects.Add(currentProject);
            if (projects.Count >= MaxRecentProjects)
            {
                break;
            }
        }

        return projects.ToArray();
    }

    private static string FormatDiagnostics(IEnumerable<Electron2D.Electron2DSettingsDiagnostic> diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
